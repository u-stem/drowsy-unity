using System;

namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// プレイ時に <see cref="PlayCardAction.TargetCardId"/> を読んで <see cref="Target"/> プレイヤーに
    /// <see cref="Drowsy.Application.Games.DrowZzz.Influences.PlayerInfluence"/>(`OwnPhaseStart`,
    /// <see cref="RestrictSpecificCardInfluenceEffect"/>(TargetCardId.TypeId), <see cref="RemainingCount"/>)
    /// を付与する効果(No.04「静寂を纏う」)。
    /// </summary>
    /// <param name="Target">影響を付与する対象プレイヤー(No.04 では <see cref="SdpTarget.Opponent"/>)</param>
    /// <param name="RemainingCount">付与する影響の残発動回数(1 以上必須、No.04 では 2)</param>
    /// <remarks>
    /// <para>
    /// 既存 <see cref="ApplyInfluenceEffect"/> がカード設計時に <see cref="Drowsy.Application.Games.DrowZzz.Influences.PlayerInfluence"/>
    /// を全フィールド静的に持つのに対し、本 effect は <b>付与する影響の <c>TargetCardTypeId</c> がプレイ時の
    /// <see cref="PlayCardAction.TargetCardId"/> に応じて動的に決まる</b>ため、専用の動的構築 effect として導入する。
    /// </para>
    /// <para>
    /// <see cref="EffectInterpreter"/> 経由で評価時、<see cref="EffectContext.TargetCardId"/> から
    /// <see cref="Drowsy.Domain.Cards.CardId.TypeId"/> を読み、
    /// <c>new RestrictSpecificCardInfluenceEffect(typeId)</c> を <see cref="Drowsy.Application.Games.DrowZzz.Influences.PlayerInfluence"/>
    /// の TickEffect として包んで Target プレイヤーに付与する。<see cref="EffectContext.TargetCardId"/> が null の場合は
    /// プレイ時の <see cref="DrowZzzRule.IsLegalMove"/> で事前防御されている前提だが、interpreter 内でも
    /// <see cref="InvalidOperationException"/> でフェイルファストする(防御的設計)。
    /// </para>
    /// </remarks>
    public sealed record ApplyTargetedRestrictionEffect(SdpTarget Target, int RemainingCount) : IEffect
    {
        // RemainingCount >= 1 の二重ガード(positional ctor 経由)
        // PlayerInfluence.RemainingCount の不変条件(>=1)を effect 構築時点で前倒し検証する(後段 PlayerInfluence
        // ctor も再検証するため二重ガード、ArgumentOutOfRangeException 統一)。
        private readonly int _remainingCount = RemainingCount >= 1
            ? RemainingCount
            : throw new ArgumentOutOfRangeException(
                nameof(RemainingCount),
                $"RemainingCount は 1 以上である必要があります(0 / 負値は影響として無意味): {RemainingCount}");

        /// <summary>付与する影響の残発動回数。1 以上必須。</summary>
        public int RemainingCount
        {
            get => _remainingCount;
            init => _remainingCount = value >= 1
                ? value
                : throw new ArgumentOutOfRangeException(
                    nameof(RemainingCount),
                    $"RemainingCount は 1 以上である必要があります(0 / 負値は影響として無意味): {value}");
        }
    }
}
