using System;
using System.Collections.Generic;
using System.Linq;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Games.DrowZzz.Influences;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Game;
using Drowsy.Domain.Players;

namespace Drowsy.Infrastructure.Persistence.Models
{
    /// <summary>
    /// schemaVersion 1 の <see cref="DrowZzzGameSession"/> 永続化用 DTO。
    /// </summary>
    /// <remarks>
    /// schemaVersion 1 の flat 構造:ルート直下に <see cref="SchemaVersion"/> + <see cref="DrowZzzGameSession"/> ctor 10 引数を 1:1 配置。
    /// <para>
    /// <b>DTO 介在の理由</b>: <see cref="DrowZzzGameSession"/> は <see cref="IReadOnlyDictionary{TKey, TValue}"/> で
    /// <see cref="PlayerId"/> をキーとして持つ(FDP / DDP / SDP / Influences / BedDamages の 5 dictionary)。
    /// Newtonsoft.Json はデフォルトで <see cref="Dictionary{TKey, TValue}"/> のキー型に <c>TypeConverter</c> ベースの
    /// 変換を要求するため、Domain 型に Newtonsoft 依存を持ち込まずに済む DTO 介在を採用する(<see cref="PlayerId"/> を
    /// 直接 key にせず、文字列キーの dictionary に変換)。
    /// </para>
    /// <para>
    /// <c>internal</c> としているのは「永続化 schema は Infrastructure 内部の関心事」+「テストからは
    /// <c>InternalsVisibleTo("Drowsy.Infrastructure.Tests")</c>(`AssemblyInfo.cs` 既存)で参照可能」のため。
    /// 将来的な migration ロジック実装時(Phase 3 候補)も <c>PersistedSessionV2</c> として段階的に追加できる。
    /// </para>
    /// </remarks>
    internal sealed record PersistedSessionV1
    {
        /// <summary>schema バージョン。本 record では常に 1。</summary>
        public int SchemaVersion { get; init; } = 1;

        /// <summary>Domain ルート集約。</summary>
        public GameState GameState { get; init; }

        /// <summary>FDP(プレイヤー識別子文字列 → 値)。<see cref="DrowZzzGameSession.FirstDrowsyPoints"/> から変換。</summary>
        public Dictionary<string, int> FirstDrowsyPoints { get; init; }

        /// <summary>DDP(プレイヤー識別子文字列 → 値)。<see cref="DrowZzzGameSession.DrawDrowsyPoints"/> から変換。</summary>
        public Dictionary<string, int> DrawDrowsyPoints { get; init; }

        /// <summary>SDP(プレイヤー識別子文字列 → 値)。<see cref="DrowZzzGameSession.SecondDrowsyPoints"/> から変換。</summary>
        public Dictionary<string, int> SecondDrowsyPoints { get; init; }

        /// <summary>残 DDP プール(順序付きシーケンス)。</summary>
        public DdpPool DdpPool { get; init; }

        /// <summary>各プレイヤーの継続影響 list(キーはプレイヤー識別子文字列)。</summary>
        public Dictionary<string, IReadOnlyList<PlayerInfluence>> Influences { get; init; }

        /// <summary>ターン内フェーズ。</summary>
        public DrowZzzPhaseState PhaseState { get; init; }

        /// <summary>ゲーム終了状態(<c>null</c> = 未終了、<see cref="WinnerOutcome"/> / <see cref="DrawOutcome"/>)。</summary>
        public GameOutcome Outcome { get; init; }

        /// <summary>各プレイヤーのベッド破損率(0〜100、キーはプレイヤー識別子文字列)。</summary>
        public Dictionary<string, int> BedDamages { get; init; }

        /// <summary>「無効化された効果」遡及発動保留 list。</summary>
        public IReadOnlyList<PendingCounteredEffect> PendingCounteredEffects { get; init; }

        /// <summary>
        /// AssociateAction で連想された CardId の永続記録。
        /// </summary>
        /// <remarks>
        /// **後方互換性**:旧 v1 JSON(本フィールドなし)読み込み時は null として deserialize され、
        /// <see cref="ToDomain"/> 内で `?? Array.Empty<CardId>()` 経由で空集合に正規化される(schemaVersion bump 不要)。
        /// 内部は <see cref="List{CardId}"/>(順序情報なし、Set としての意味のみ)で `DrowZzzGameSession` 側 ctor で
        /// <see cref="HashSet{CardId}"/> に防御コピーされる。
        /// </remarks>
        public List<CardId> AssociatedCardIds { get; init; }

        /// <summary>Domain の <see cref="DrowZzzGameSession"/> から DTO を生成する。</summary>
        /// <exception cref="ArgumentNullException">session が null</exception>
        public static PersistedSessionV1 FromDomain(DrowZzzGameSession session)
        {
            if (session is null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            return new PersistedSessionV1
            {
                SchemaVersion = 1,
                GameState = session.GameState,
                FirstDrowsyPoints = ToStringKeyed(session.FirstDrowsyPoints),
                DrawDrowsyPoints = ToStringKeyed(session.DrawDrowsyPoints),
                SecondDrowsyPoints = ToStringKeyed(session.SecondDrowsyPoints),
                DdpPool = session.DdpPool,
                Influences = ToStringKeyedInfluences(session.Influences),
                PhaseState = session.PhaseState,
                Outcome = session.Outcome,
                BedDamages = ToStringKeyed(session.BedDamages),
                PendingCounteredEffects = session.PendingCounteredEffects,
                // HashSet<CardId> → List<CardId> 変換(順序は意味を持たないが JSON 表現上は順序付きリスト)。
                // 空集合は空 list で保存される(`AssociatedCardIds.Count == 0` の場合も Field 自体は serialize される)。
                AssociatedCardIds = session.AssociatedCardIds.ToList(),
            };
        }

        /// <summary>DTO から Domain の <see cref="DrowZzzGameSession"/> を再構築する。</summary>
        /// <exception cref="InvalidOperationException">必須 property が欠落している場合(deserialize 後の整合性違反)</exception>
        public DrowZzzGameSession ToDomain()
        {
            EnsureNotNull(GameState, nameof(GameState));
            EnsureNotNull(FirstDrowsyPoints, nameof(FirstDrowsyPoints));
            EnsureNotNull(DrawDrowsyPoints, nameof(DrawDrowsyPoints));
            EnsureNotNull(SecondDrowsyPoints, nameof(SecondDrowsyPoints));
            EnsureNotNull(DdpPool, nameof(DdpPool));
            EnsureNotNull(Influences, nameof(Influences));
            EnsureNotNull(BedDamages, nameof(BedDamages));
            EnsureNotNull(PendingCounteredEffects, nameof(PendingCounteredEffects));

            // 旧 v1 JSON(本フィールドなし)読み込み時は AssociatedCardIds = null となるため、
            // `?? Array.Empty<CardId>()` で空集合に正規化する(schemaVersion bump 不要の後方互換性経路)。
            // DrowZzzGameSession ctor 側でも null → 空 set へ正規化されるが、明示的に Array.Empty を渡して意図を表明する。
            return new DrowZzzGameSession(
                gameState: GameState,
                firstDrowsyPoints: FromStringKeyed(FirstDrowsyPoints),
                drawDrowsyPoints: FromStringKeyed(DrawDrowsyPoints),
                secondDrowsyPoints: FromStringKeyed(SecondDrowsyPoints),
                ddpPool: DdpPool,
                influences: FromStringKeyedInfluences(Influences),
                phaseState: PhaseState,
                outcome: Outcome,
                bedDamages: FromStringKeyed(BedDamages),
                pendingCounteredEffects: PendingCounteredEffects,
                associatedCardIds: AssociatedCardIds ?? (IReadOnlyCollection<CardId>)Array.Empty<CardId>());
        }

        // --- helpers ---

        private static Dictionary<string, int> ToStringKeyed(IReadOnlyDictionary<PlayerId, int> source)
        {
            var result = new Dictionary<string, int>(source.Count);
            foreach (var kv in source)
            {
                result[kv.Key.Value] = kv.Value;
            }
            return result;
        }

        private static Dictionary<PlayerId, int> FromStringKeyed(Dictionary<string, int> source)
        {
            var result = new Dictionary<PlayerId, int>(source.Count);
            foreach (var kv in source)
            {
                result[PlayerId.Of(kv.Key)] = kv.Value;
            }
            return result;
        }

        private static Dictionary<string, IReadOnlyList<PlayerInfluence>> ToStringKeyedInfluences(
            IReadOnlyDictionary<PlayerId, IReadOnlyList<PlayerInfluence>> source)
        {
            var result = new Dictionary<string, IReadOnlyList<PlayerInfluence>>(source.Count);
            foreach (var kv in source)
            {
                result[kv.Key.Value] = kv.Value;
            }
            return result;
        }

        private static Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>> FromStringKeyedInfluences(
            Dictionary<string, IReadOnlyList<PlayerInfluence>> source)
        {
            var result = new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>(source.Count);
            foreach (var kv in source)
            {
                result[PlayerId.Of(kv.Key)] = kv.Value;
            }
            return result;
        }

        private static void EnsureNotNull(object value, string name)
        {
            if (value is null)
            {
                throw new InvalidOperationException(
                    $"PersistedSessionV1.{name} が null です(JSON deserialize で必須プロパティが欠落)");
            }
        }
    }
}
