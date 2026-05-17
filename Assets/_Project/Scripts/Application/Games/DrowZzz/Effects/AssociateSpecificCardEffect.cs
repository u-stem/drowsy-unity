using System;
using Drowsy.Domain.Cards;

namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// プレイヤー(actor = current player)の Hand に指定 <see cref="CardTypeId"/> のカードを **自動連想** で追加する effect
    /// (No.13/14/15「最後の砦Ⅰ/Ⅱ/Ⅲ」、2026-05-17 で導入)。
    /// </summary>
    /// <param name="TargetCardTypeId">連想する対象のカード型 ID(null 不可、例:No.13 → "14")</param>
    /// <remarks>
    /// <para>
    /// 既存 <see cref="Drowsy.Application.Games.DrowZzz.AssociateAction"/>(M3-PR4、ADR-0011 §1)が
    /// 「プレイヤーの能動アクションによる連想」(TotalPoints>=80 + AssociatableMarkerEffect 必須)だったのに対し、
    /// 本 effect は **カード効果として自動発動する連想** で、合法性条件(threshold / marker)は **課さない**
    /// (オーナー JIT 確定 2026-05-17:「カード効果として自動連想、他條件不要」)。
    /// </para>
    /// <para>
    /// 評価動作(`EffectInterpreter` 内):
    /// <list type="number">
    /// <item>current player の Hand 内で TargetCardTypeId の未使用 Instance 番号を探索(0 から順に試行)</item>
    /// <item>`CardId.Of(TargetCardTypeId, newInstance)` を新規生成して Hand.Add</item>
    /// <item>session.AssociatedCardIds に新 CardId を追加(ADR-0019 と整合、連想由来として永続記録)</item>
    /// </list>
    /// </para>
    /// <para>
    /// **Hand 重複対応**(オーナー JIT 確認 2026-05-17):同一 TypeId のカードが既に Hand にある場合(例:
    /// 「最後の砦Ⅰ」を 2 回プレイして「最後の砦Ⅱ」が 2 枚連想される場合)、Instance 番号を 0/1/2... と
    /// 増やして unique な CardId を生成し、Hand.Add 重複検出(HAND-005 / ADR-0018)を回避する。
    /// </para>
    /// <para>
    /// <b>本 effect は `PlayCardAction` の効果列で使用</b>(`AssociateAction` とは別経路)。
    /// `PlayerInfluence.TickEffect` としての使用は意図されていない(actor 解決の semantics が異なる)。
    /// </para>
    /// </remarks>
    public sealed record AssociateSpecificCardEffect(CardTypeId TargetCardTypeId) : IEffect
    {
        // null 防御の二重ガード(positional ctor 経由 + init setter 経由、`IEffect.cs` §「二重ガードパターン」参照)
        // CS8907 回避 + `record + with` 経由でも null 検証が確実に走るための既存パターン
        // (`RestrictSpecificCardInfluenceEffect` / `PlayCardAction` / `TimeOfDayBranchEffect` と同パターン、
        // code-reviewer W-2 反映 2026-05-17)
        private readonly CardTypeId _targetCardTypeId = TargetCardTypeId ?? throw new ArgumentNullException(nameof(TargetCardTypeId));

        /// <summary>連想する対象のカード型 ID。null 不可。</summary>
        public CardTypeId TargetCardTypeId
        {
            get => _targetCardTypeId;
            init => _targetCardTypeId = value ?? throw new ArgumentNullException(nameof(TargetCardTypeId));
        }
    }
}
