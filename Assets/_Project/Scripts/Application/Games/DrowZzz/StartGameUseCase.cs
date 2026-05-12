using System;
using System.Collections.Generic;
using System.Linq;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Games.DrowZzz.Influences;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Configuration;
using Drowsy.Domain.Game;
using Drowsy.Domain.Players;
using Drowsy.Domain.Random;

namespace Drowsy.Application.Games.DrowZzz
{
    /// <summary>
    /// セッションを新規生成する特殊 UseCase。
    /// セッション未生成状態からゲームを開始するため、<see cref="IGameRule{TAction, TSession}.Apply"/> 系統とは
    /// 独立した API として提供する(ADR-0006 §3 / §M1-PR3)。
    /// </summary>
    /// <remarks>
    /// 構築手順(M1 範囲):
    /// <list type="number">
    /// <item>引数検証(<c>players</c> null → <c>initialDeck</c> null → <c>players</c> empty → null 要素/重複 PlayerId → FdpPool 数 → 山札枚数 の順)</item>
    /// <item>先後ランダム決定: <see cref="IRandomSource"/> で <c>players</c> を Fisher-Yates シャッフル</item>
    /// <item>FDP 抽選: <see cref="IGameConfig.FdpPool"/> を Fisher-Yates シャッフルし、先頭 N 個をシャッフル後の Players 順に割り当て</item>
    /// <item>初期手札配布: シャッフル後 Players 順に 1 枚ずつ交互、5 サイクル(計 5 × N 枚)</item>
    /// <item><see cref="DrowZzzGameSession"/> 構築 (<see cref="DrowZzzPhaseState.WaitingForDraw"/>)</item>
    /// </list>
    /// <para>
    /// <see cref="ICardCatalog{TEffect}"/> は本 UseCase では参照しない(<see cref="CardId"/> の移動のみ、
    /// <see cref="CardData"/> 解決は <c>PlayCardAction.Apply</c> 実装で利用され、本 UseCase の内部実装は
    /// 型引数 <c>IEffect</c> に依存しない)。constructor injection は ADR-0006 §3 通り維持し、
    /// M2-PR1 で <see cref="ICardCatalog"/> → <see cref="ICardCatalog{TEffect}"/> ジェネリック化に追随する
    /// 型引数結合(ADR-0007 §3「設計上の割り切り」)を本 UseCase で受容する。
    /// </para>
    /// </remarks>
    public sealed class StartGameUseCase
    {
        // 初期手札枚数 (DrowZzz の最小ルール、ADR-0006 §M1)。
        // L2 定数として UseCase 内に直接埋め込み。M1 範囲では IGameConfig への移管は見送り、Phase 2 後半で要検討
        // (定数管理方針は CLAUDE.md §9 / docs/architecture/constants-management.md)。
        private const int InitialHandSize = 5;

        private readonly IRandomSource _rng;
        private readonly ICardCatalog<IEffect> _catalog;
        private readonly IGameConfig _config;

        /// <exception cref="ArgumentNullException">いずれかの引数が null</exception>
        public StartGameUseCase(IRandomSource rng, ICardCatalog<IEffect> catalog, IGameConfig config)
        {
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// 新規 <see cref="DrowZzzGameSession"/> を生成する。
        /// </summary>
        /// <param name="players">参加プレイヤーの <see cref="PlayerId"/> 列(順序は無関係、内部で Shuffle 後の順を採用)</param>
        /// <param name="initialDeck">初期山札(Top 順、配布に必要な <c>5 × players.Count</c> 枚以上)</param>
        /// <exception cref="ArgumentNullException">players または initialDeck が null</exception>
        /// <exception cref="ArgumentException">
        /// players が空 / null 要素 / 重複 PlayerId、または initialDeck が配布枚数に満たない
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// <see cref="IGameConfig.FdpPool"/> の要素数が <paramref name="players"/> 数より少ない
        /// </exception>
        public DrowZzzGameSession Execute(IReadOnlyList<PlayerId> players, Pile initialDeck)
        {
            ValidateArguments(players, initialDeck, out var fdpPool);

            // 1. 先後ランダム決定: Players を Fisher-Yates Shuffle
            var shuffledPlayers = ShuffleList(players, _rng);

            // 2. FDP 抽選: FdpPool を Shuffle して先頭 N 個をシャッフル後 Players 順に割り当て
            var shuffledFdp = ShuffleList(fdpPool, _rng);
            var fdpDict = new Dictionary<PlayerId, int>(shuffledPlayers.Count);
            for (int i = 0; i < shuffledPlayers.Count; i++)
            {
                fdpDict[shuffledPlayers[i]] = shuffledFdp[i];
            }

            // 3. 1 枚ずつ交互に 5 サイクル配布
            var hands = new Hand[shuffledPlayers.Count];
            for (int i = 0; i < hands.Length; i++)
            {
                hands[i] = Hand.Empty;
            }
            var deck = initialDeck;
            for (int cycle = 0; cycle < InitialHandSize; cycle++)
            {
                for (int i = 0; i < shuffledPlayers.Count; i++)
                {
                    var (drawn, remaining) = deck.Draw();
                    hands[i] = hands[i].Add(drawn);
                    deck = remaining;
                }
            }

            // 4. PlayerState 構築
            var playerStates = new PlayerState[shuffledPlayers.Count];
            for (int i = 0; i < shuffledPlayers.Count; i++)
            {
                playerStates[i] = new PlayerState(shuffledPlayers[i], hands[i]);
            }

            // 5. SDP / DDP 初期化(全プレイヤー 0、ADR-0009 §「DP 種別」)
            // SDP: M2-PR3 で追加、初期値 0(公開情報)
            // DDP: M2-PR4 で追加、初期値 0(隠し情報、Turn 5/9/13/17/21 開始時に DdpPool から累積)
            var sdpDict = new Dictionary<PlayerId, int>(shuffledPlayers.Count);
            var ddpDict = new Dictionary<PlayerId, int>(shuffledPlayers.Count);
            // M2-PR5: Influences 初期化(全プレイヤー空 list、ADR-0007 §1.5)
            var influencesDict = new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>(shuffledPlayers.Count);
            for (int i = 0; i < shuffledPlayers.Count; i++)
            {
                sdpDict[shuffledPlayers[i]] = 0;
                ddpDict[shuffledPlayers[i]] = 0;
                influencesDict[shuffledPlayers[i]] = Array.Empty<PlayerInfluence>();
            }

            // 6. DdpPool 初期化(IGameConfig.DdpPool を Fisher-Yates Shuffle、ADR-0009 §3 / DZ-140)
            var ddpPool = new DdpPool(_config.DdpPool).Shuffle(_rng);

            // 7. GameState + DrowZzzGameSession 構築
            var gameState = new GameState(
                playerStates,
                deck,
                Pile.Empty,
                Pile.Empty,
                TurnState.Initial(0));

            return new DrowZzzGameSession(
                gameState,
                fdpDict,
                ddpDict,
                sdpDict,
                ddpPool,
                influencesDict,
                DrowZzzPhaseState.WaitingForDraw,
                // M3-PR1: 新規セッションは未終了(ADR-0010 §3)
                outcome: null);
        }

        // 引数検証を 1 メソッドに集約(out で fdpPool を返し本体ロジックの再アクセスを省略)
        private void ValidateArguments(
            IReadOnlyList<PlayerId> players,
            Pile initialDeck,
            out IReadOnlyList<int> fdpPool)
        {
            if (players is null)
            {
                throw new ArgumentNullException(nameof(players));
            }
            if (initialDeck is null)
            {
                throw new ArgumentNullException(nameof(initialDeck));
            }
            if (players.Count == 0)
            {
                throw new ArgumentException("players は 1 人以上必要です", nameof(players));
            }

            // 重複 PlayerId 検証 (null 要素も同時に検出)
            var seen = new HashSet<PlayerId>();
            foreach (var pid in players)
            {
                if (pid is null)
                {
                    throw new ArgumentException("players に null 要素を含めることはできません", nameof(players));
                }
                if (!seen.Add(pid))
                {
                    throw new ArgumentException(
                        $"players に重複 PlayerId が含まれます: {pid.Value}",
                        nameof(players));
                }
            }

            // FdpPool は IGameConfig から取得、null 安全に対処
            fdpPool = _config.FdpPool;
            if (fdpPool is null || fdpPool.Count < players.Count)
            {
                throw new InvalidOperationException(
                    $"FdpPool ({fdpPool?.Count ?? 0} 個) が players ({players.Count} 人) に対して不足しています");
            }

            // 山札枚数チェック (配布に必要な最小限のみ)
            int requiredCards = InitialHandSize * players.Count;
            if (initialDeck.Count < requiredCards)
            {
                throw new ArgumentException(
                    $"initialDeck の枚数 ({initialDeck.Count}) が配布に必要な枚数 ({requiredCards} = {InitialHandSize} × {players.Count}) に不足しています",
                    nameof(initialDeck));
            }
        }

        // Fisher-Yates シャッフル (input の防御コピーを返す純関数)
        // Pile.Shuffle は CardId[] 専用のため再利用不可、ここで generic 版を実装
        private static List<T> ShuffleList<T>(IReadOnlyList<T> source, IRandomSource rng)
        {
            var copy = source.ToList();
            for (int i = copy.Count - 1; i > 0; i--)
            {
                int j = rng.NextInt(0, i + 1);
                (copy[i], copy[j]) = (copy[j], copy[i]);
            }
            return copy;
        }
    }
}
