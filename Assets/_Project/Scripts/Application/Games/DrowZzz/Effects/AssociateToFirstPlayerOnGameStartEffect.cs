namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// 「ゲーム開始時、先行プレイヤーがこの手段を連想する」効果マーカー record(ADR-0024、No.19「絶対障壁」で初導入)。
    /// </summary>
    /// <remarks>
    /// 本 effect は <see cref="StartGameUseCase.Execute"/> 内で catalog 全 entry の最上位 effects 列を scan して
    /// 検出される。検出されたカードは先行プレイヤー(<c>shuffledPlayers[0]</c>)の Hand に <c>CardId.Of(typeId, 0)</c> で
    /// 追加され、<see cref="DrowZzzGameSession.AssociatedCardIds"/> にも記録される(ADR-0019 連想由来記録と整合、
    /// No.04「静寂を纏う」系の `TargetCardId` 対象から除外される)。
    /// <para>
    /// <see cref="EffectInterpreter"/> に直接渡された場合は <c>no-op</c>(session 不変返却)。
    /// 評価はゲーム開始時の <see cref="StartGameUseCase"/> 経路のみ行う(`AssociatableMarkerEffect` /
    /// `UsageRestrictionMarkerEffect` / `ReuseInfluenceSourceEffect` と同じ「rule / use-case 評価層で unwrap される」パターン)。
    /// </para>
    /// <para>
    /// 入手経路ポリシー(ADR-0024 §4):本 effect 持ちカードは <c>Bootstrap.BuildInitialDeck</c> の共通山札からも
    /// 除外される(`HasFirstPlayerAssociationEffectInTopLevel` で最上位 scan)。1 ゲーム 1 枚の入手経路は
    /// 「先行プレイヤーの開始時自動連想のみ」。
    /// </para>
    /// </remarks>
    public sealed record AssociateToFirstPlayerOnGameStartEffect : IEffect;
}
