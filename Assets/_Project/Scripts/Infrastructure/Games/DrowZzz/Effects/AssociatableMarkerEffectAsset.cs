using System;
using Drowsy.Application.Games.DrowZzz.Effects;

namespace Drowsy.Infrastructure.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="AssociatableMarkerEffect"/>(M3-PR4、ADR-0011 §1)を Unity SO で表現する POCO
    /// (M4-PR3 で導入、INF-033 / INF-034)。フィールドなし marker。
    /// </summary>
    /// <remarks>
    /// 「カードが連想可能カード(`AssociateAction` の対象)であること」を効果列で示すマーカー。
    /// <see cref="EffectInterpreter"/> では no-op、本 Asset も <see cref="ToDomain"/> で domain marker を返すだけ。
    /// </remarks>
    [Serializable]
    public sealed class AssociatableMarkerEffectAsset : EffectAsset
    {
        /// <summary>テスト用 ctor。</summary>
        internal AssociatableMarkerEffectAsset() { }

        /// <inheritdoc />
        public override IEffect ToDomain() => new AssociatableMarkerEffect();
    }
}
