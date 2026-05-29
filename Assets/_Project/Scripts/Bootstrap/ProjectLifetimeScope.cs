using System;
using System.Collections.Generic;
using Drowsy.Application;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Persistence;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Configuration;
using Drowsy.Domain.Players;
using Drowsy.Domain.Random;
using Drowsy.Infrastructure.Configuration;
using Drowsy.Infrastructure.Games.DrowZzz;
using Drowsy.Infrastructure.Persistence;
using Drowsy.Infrastructure.Settings;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Drowsy.Bootstrap
{
    /// <summary>
    /// アプリ全体寿命の VContainer LifetimeScope(M5-PR3 で Configure 実装、M5-PR4 で対戦初期値を追加)。
    /// </summary>
    /// <remarks>
    /// <c>DontDestroyOnLoad</c> / Application 寿命の Singleton 群を登録する。
    /// <para>
    /// <b>SerializeField 注入</b>:<see cref="DrowZzzGameConfigAsset"/>(<c>DrowZzzGameConfig.asset</c>)と
    /// <see cref="ScriptableObjectCardCatalog"/>(<c>DrowZzzCardCatalog.asset</c>)を Inspector で割り当てる。
    /// <see cref="Configure"/> の前段で null チェックし、
    /// Inspector 注入忘れを Boot 時点で <see cref="InvalidOperationException"/> に昇格する(Build 後の沈黙 fail 回避)。
    /// </para>
    /// <para>
    /// <b>登録方針</b>:ScriptableObject は <c>RegisterInstance</c>(<c>RegisterComponentInHierarchy</c> は
    /// MonoBehaviour 検索 API のため SO に適用不可)。<see cref="IRandomSource"/> は
    /// 時刻ベース seed の <see cref="XorShiftRandom"/> を <c>RegisterInstance</c>(<c>XorShiftRandom</c> ctor が
    /// <c>uint seed</c> を要求し VContainer の標準解決対象外のため <c>Register&lt;,&gt;</c> ではなく
    /// <c>RegisterInstance</c> を使う)。<see cref="EffectInterpreter"/> は <see cref="DrowZzzRule"/> ctor の
    /// 第 2 引数として必要なため明示的に Register する(VContainer は未登録の具象型を自動解決しない)。
    /// </para>
    /// <para>
    /// <b>対戦初期値</b>:新規対戦の <c>players</c> / <c>initialDeck</c> を本スコープで構築し
    /// <c>RegisterInstance</c> する。<see cref="DrowZzzGamePresenter"/> ctor がこの 2 値を受け取り、
    /// <see cref="DrowZzzGamePresenter"/> の <c>BootAsync</c> 新規対戦経路で <see cref="StartGameUseCase.Execute"/>
    /// に渡す。<c>initialDeck</c> は catalog 登録カードを並べた簡易デッキで、本物の 56 枚デッキ構成は Phase 3。
    /// </para>
    /// <para>
    /// <b>セーブパス</b>:<see cref="DrowZzzGameSessionSerializer.DefaultSavePath()"/> は <c>static</c> で
    /// <c>Application.persistentDataPath</c> を内部参照するため、<see cref="Configure"/> 内のメインスレッドで
    /// 評価し <c>string</c> を Project Singleton として <c>RegisterInstance</c> する
    /// (ワーカースレッドからの <c>persistentDataPath</c> 参照を回避)。
    /// </para>
    /// </remarks>
    public sealed class ProjectLifetimeScope : LifetimeScope
    {
        /// <summary>
        /// M5 動作確認用の簡易デッキで catalog 登録カード 1 種あたりに並べる枚数。
        /// </summary>
        /// <remarks>
        /// catalog 登録カード(21 種:No.00〜No.20)を各
        /// <see cref="CopiesPerCardForM5Deck"/> 枚並べて <c>initialDeck</c> を組み立てる。
        /// ただし `AssociateToFirstPlayerOnGameStartEffect` 持ちのカード(No.19「絶対障壁」)は共通山札から除外
        /// (先行プレイヤーの開始時自動連想のみが入手経路、後手は引けない)。
        /// N=2 × MaxRound 21 × 1 Draw = 42 + 初期配布 10 = 52 枚を賄える値。20 種(No.19 除外後)× 20 枚 = 400 枚で余裕 348 枚。
        /// 本物の 56 枚デッキ構成(<c>docs/specs/games/drowzzz/setup.md</c> §定数依存、カードごとの初期山札枚数を
        /// honor する形式)は Phase 3 でカードデータが揃った時点で別途実装する。
        /// <para>
        /// <b>カード追加時のコメント更新コスト</b>:本コメントは catalog 登録カード列挙形式のため、新規カード追加
        /// ごとに手動更新が必要。Phase 3 の本物デッキ実装で「`registered.Count × CopiesPerCardForM5Deck`」の
        /// 機械導出形式に置き換える方針(Bootstrap 自体が Phase 3 で再実装されるため、本暫定実装の存続期間中は列挙形式を維持)。
        /// </para>
        /// </remarks>
        private const int CopiesPerCardForM5Deck = 20;

        /// <summary>
        /// M5 ホットシート対戦の先手プレイヤー Id 文字列。
        /// </summary>
        /// <remarks>
        /// View の <c>Render</c> で <c>PlayerId.Value</c> がそのまま UI 表示されるため、表示名の単一情報源として
        /// const 化する。Phase 3 でプレイヤー名入力 UI を導入する際に本 const は不要になる(M5-PR4 code-reviewer S-2 反映)。
        /// </remarks>
        private const string PlayerIdP1 = "p1";

        /// <summary>M5 ホットシート対戦の後手プレイヤー Id 文字列(<see cref="PlayerIdP1"/> 参照)。</summary>
        private const string PlayerIdP2 = "p2";

        /// <summary>ゲームバランス設定 SO(<c>DrowZzzGameConfig.asset</c>)。Inspector で割り当てる。</summary>
        [SerializeField] private DrowZzzGameConfigAsset _gameConfig;

        /// <summary>カードカタログ SO(<c>DrowZzzCardCatalog.asset</c>)。Inspector で割り当てる。</summary>
        [SerializeField] private ScriptableObjectCardCatalog _cardCatalog;

        /// <inheritdoc />
        protected override void Configure(IContainerBuilder builder)
        {
            // Inspector 注入忘れの Boot 時点検出(Build 後の沈黙 fail 回避)
            if (_gameConfig is null)
            {
                throw new InvalidOperationException(
                    "DrowZzzGameConfigAsset が Inspector で未設定です。" +
                    "ProjectLifetimeScope の _gameConfig フィールドに DrowZzzGameConfig.asset を割り当ててください。");
            }
            if (_cardCatalog is null)
            {
                throw new InvalidOperationException(
                    "ScriptableObjectCardCatalog が Inspector で未設定です。" +
                    "ProjectLifetimeScope の _cardCatalog フィールドに DrowZzzCardCatalog.asset を割り当ててください。");
            }

            // Settings / Catalog(ScriptableObject は RegisterInstance)
            builder.RegisterInstance<IGameConfig>(_gameConfig);
            builder.RegisterInstance<ICardCatalog<IEffect>>(_cardCatalog);

            // セーブパス(Application.persistentDataPath は Configure 内のメインスレッドで取得)
            builder.RegisterInstance(DrowZzzGameSessionSerializer.DefaultSavePath());

            // Infrastructure
            // Ticks(long)の下位 32bit を uint seed とする。unchecked で long → uint の桁溢れを意図的に許容
            // (seed=0 は XorShiftRandom 内部で 1 に補正されるため実用上問題なし)。
            builder.RegisterInstance<IRandomSource>(new XorShiftRandom(unchecked((uint)DateTime.UtcNow.Ticks)));
            builder.Register<IDrowZzzGameSessionSerializer, DrowZzzGameSessionSerializer>(Lifetime.Singleton);
            builder.Register<IUserSettings, PlayerPrefsUserSettings>(Lifetime.Singleton);

            // Application(DrowZzzRule は ctor で ICardCatalog<IEffect> + EffectInterpreter を要求)
            builder.Register<EffectInterpreter>(Lifetime.Singleton);
            builder.Register<DrowZzzRule>(Lifetime.Singleton);

            // 新規対戦の players / initialDeck(DrowZzzGamePresenter ctor が受け取り BootAsync 新規対戦経路で使用)
            // players は PlayerRoster wrapper で登録する(VContainer 1.x の
            // CollectionInstanceProvider.Match が IReadOnlyList<T> / IEnumerable<T> を予約型として扱い
            // RegisterInstance を上書きするため、IReadOnlyList<PlayerId> 直接登録は不可)。
            builder.RegisterInstance(new PlayerRoster(BuildPlayers()));
            builder.RegisterInstance(BuildInitialDeck(_cardCatalog));
        }

        /// <summary>
        /// 新規対戦のプレイヤー Id 列を構築する。現在は N=2 ホットシート固定。
        /// Phase 3 で N&gt;2 / プレイヤー名入力 UI に拡張する。
        /// </summary>
        private static IReadOnlyList<PlayerId> BuildPlayers()
            => new[] { PlayerId.Of(PlayerIdP1), PlayerId.Of(PlayerIdP2) };

        /// <summary>
        /// catalog 登録カード種別を各 <see cref="CopiesPerCardForM5Deck"/> 枚並べた M5 簡易デッキを構築する。
        /// </summary>
        /// <remarks>
        /// <see cref="CardId"/> は instance unique ID のため、本 deck の各 entry は
        /// `(CardTypeId, Instance=0..N-1)` の組として unique 化される。これにより Hand 配布時の
        /// 「同じ CardId を 2 枚以上 Add」エラーを構造的に回避する。
        /// </remarks>
        /// <exception cref="InvalidOperationException">catalog にカードが 1 枚も登録されていない</exception>
        private static Pile BuildInitialDeck(ScriptableObjectCardCatalog catalog)
        {
            var registered = catalog.RegisteredCardTypeIds;
            if (registered.Count == 0)
            {
                throw new InvalidOperationException(
                    "ScriptableObjectCardCatalog にカードが 1 枚も登録されていません。" +
                    "DrowZzzCardCatalog.asset の Entries を確認してください。");
            }
            var cards = new List<CardId>(registered.Count * CopiesPerCardForM5Deck);
            foreach (var typeId in registered)
            {
                // `AssociateToFirstPlayerOnGameStartEffect` 持ちのカード(No.19「絶対障壁」等)は
                // 共通山札に含めない(先行プレイヤーの開始時自動連想のみが入手経路、後手プレイヤーは引けない)。
                var effects = catalog.GetEffects(typeId);
                if (HasFirstPlayerAssociationEffectInTopLevel(effects))
                {
                    continue;
                }
                for (int i = 0; i < CopiesPerCardForM5Deck; i++)
                {
                    cards.Add(CardId.Of(typeId, i));
                }
            }
            return new Pile(cards);
        }

        // Bootstrap.BuildInitialDeck の除外フィルタ用ヘルパー。
        // StartGameUseCase.HasFirstPlayerAssociationEffectInTopLevel と同じ判定ロジックだが、レイヤ境界を尊重して
        // Bootstrap 側にも複製(Bootstrap → Application 内部 helper への依存を作らない)。
        // 走査スコープは最上位 effects のみ(wrapper 内側は再帰しない)。
        private static bool HasFirstPlayerAssociationEffectInTopLevel(IReadOnlyList<IEffect> effects)
        {
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i] is AssociateToFirstPlayerOnGameStartEffect)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
