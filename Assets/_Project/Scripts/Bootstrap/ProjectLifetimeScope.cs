using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Drowsy.Application;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Persistence;
using Drowsy.Domain.Configuration;
using Drowsy.Domain.Random;
using Drowsy.Infrastructure.Configuration;
using Drowsy.Infrastructure.Games.DrowZzz;
using Drowsy.Infrastructure.Persistence;
using Drowsy.Infrastructure.Settings;

namespace Drowsy.Bootstrap
{
    /// <summary>
    /// アプリ全体寿命の VContainer LifetimeScope(M5-PR3 で Configure 実装)。
    /// </summary>
    /// <remarks>
    /// ADR-0016 §1「VContainer LifetimeScope 階層構造」+ §2「登録対象と寿命」+ §7.2「Bootstrap への注入経路」で確定。
    /// 本スコープは <c>DontDestroyOnLoad</c> / Application 寿命の Singleton 群を登録する。
    /// <para>
    /// <b>SerializeField 注入</b>:<see cref="DrowZzzGameConfigAsset"/>(<c>DrowZzzGameConfig.asset</c>)と
    /// <see cref="ScriptableObjectCardCatalog"/>(<c>DrowZzzCardCatalog.asset</c>)を Inspector で割り当てる
    /// (M4-PR7 で実 <c>.asset</c> 配置確立済、ADR-0016 §7.1)。<see cref="Configure"/> の前段で null チェックし、
    /// Inspector 注入忘れを Boot 時点で <see cref="InvalidOperationException"/> に昇格する(Build 後の沈黙 fail 回避)。
    /// </para>
    /// <para>
    /// <b>登録方針</b>:ScriptableObject は <c>RegisterInstance</c>(<c>RegisterComponentInHierarchy</c> は
    /// MonoBehaviour 検索 API のため SO に適用不可)。<see cref="IRandomSource"/> は ADR-0016 §2 通り M5 では
    /// 時刻ベース seed の <see cref="XorShiftRandom"/> を <c>RegisterInstance</c>(<c>XorShiftRandom</c> ctor が
    /// <c>uint seed</c> を要求し VContainer の標準解決対象外のため <c>Register&lt;,&gt;</c> ではなく
    /// <c>RegisterInstance</c> を使う)。<see cref="EffectInterpreter"/> は <see cref="DrowZzzRule"/> ctor の
    /// 第 2 引数として必要なため明示的に Register する(VContainer は未登録の具象型を自動解決しない)。
    /// </para>
    /// <para>
    /// <b>セーブパス</b>:<see cref="DrowZzzGameSessionSerializer.DefaultSavePath()"/> は <c>static</c> で
    /// <c>Application.persistentDataPath</c> を内部参照するため、<see cref="Configure"/> 内のメインスレッドで
    /// 評価し <c>string</c> を Project Singleton として <c>RegisterInstance</c> する(ADR-0016 §5.2 / §7.2、
    /// ワーカースレッドからの <c>persistentDataPath</c> 参照を回避)。
    /// </para>
    /// </remarks>
    public sealed class ProjectLifetimeScope : LifetimeScope
    {
        /// <summary>ゲームバランス設定 SO(<c>DrowZzzGameConfig.asset</c>、ADR-0016 §7.1)。Inspector で割り当てる。</summary>
        [SerializeField] private DrowZzzGameConfigAsset _gameConfig;

        /// <summary>カードカタログ SO(<c>DrowZzzCardCatalog.asset</c>、ADR-0016 §7.1)。Inspector で割り当てる。</summary>
        [SerializeField] private ScriptableObjectCardCatalog _cardCatalog;

        /// <inheritdoc />
        protected override void Configure(IContainerBuilder builder)
        {
            // Inspector 注入忘れの Boot 時点検出(Build 後の沈黙 fail 回避、ADR-0016 §7.2)
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
        }
    }
}
