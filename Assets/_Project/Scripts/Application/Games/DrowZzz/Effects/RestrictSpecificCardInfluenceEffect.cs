using System;
using Drowsy.Domain.Cards;

namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="Drowsy.Application.Games.DrowZzz.Influences.PlayerInfluence.TickEffect"/> として使われ、
    /// <see cref="DrowZzzRule.IsLegalMove"/> で「<see cref="TargetCardTypeId"/> 一致のカードプレイを illegal 化」する
    /// マーカー effect(ADR-0019 / No.04「静寂を纏う」、PR ② で導入)。
    /// </summary>
    /// <param name="TargetCardTypeId">使用禁止対象のカード型 ID(null 不可)</param>
    /// <remarks>
    /// <para>
    /// 既存 <see cref="UsageRestrictionMarkerEffect"/> が「全カードの使用禁止」(連想後の使用制限機構)を表すのに対し、
    /// 本 effect は <b>特定 <see cref="CardTypeId"/> 一個に限定した使用禁止</b>を表現する。
    /// No.04「静寂を纏う」が「相手の手札から 1 枚選んで使用禁止」する機構の実装基盤。
    /// </para>
    /// <para>
    /// <see cref="EffectInterpreter"/> 経由の評価では <b>session 不変返却(no-op)</b>
    /// (<see cref="UsageRestrictionMarkerEffect"/> と同パターン:識別用に効果列 / Tick 時に置かれるだけで状態は変えない)。
    /// 実際の使用禁止判定は <see cref="DrowZzzRule.IsLegalMove"/> 内の Influence walk で本 effect を検出して行う。
    /// </para>
    /// <para>
    /// <see cref="PlayerInfluence.RemainingCount"/> の Tick 減算は <c>DrowZzzRule.TickInfluences</c> の責務で、
    /// 本 effect 自体は影響しない(`UsageRestrictionMarkerEffect` と完全対称、`RemainingCount` 経由で 2 フェーズ等の寿命を表現)。
    /// </para>
    /// </remarks>
    public sealed record RestrictSpecificCardInfluenceEffect(CardTypeId TargetCardTypeId) : IEffect
    {
        // null 防御の二重ガード(positional ctor 経由)
        private readonly CardTypeId _targetCardTypeId = TargetCardTypeId ?? throw new ArgumentNullException(nameof(TargetCardTypeId));

        /// <summary>使用禁止対象のカード型 ID。null 不可。</summary>
        public CardTypeId TargetCardTypeId
        {
            get => _targetCardTypeId;
            init => _targetCardTypeId = value ?? throw new ArgumentNullException(nameof(TargetCardTypeId));
        }
    }
}
