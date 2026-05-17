using System;
using Drowsy.Application.Games.DrowZzz.Effects;
using UnityEngine;

namespace Drowsy.Infrastructure.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="StackHandCardOnDeckTopEffect"/>(No.05「喧騒を纏う」、2026-05-17 で導入)を
    /// Unity SO で表現する POCO。
    /// </summary>
    /// <remarks>
    /// SO 上は <see cref="Source"/>(<see cref="SdpTarget"/> 列挙値、Inspector 編集可能)のみ保持。
    /// 動的部分(TargetCardId)は runtime に決まるため SO には含めない
    /// (<see cref="ApplyTargetedRestrictionEffectAsset"/> と同パターン)。
    /// </remarks>
    [Serializable]
    public sealed class StackHandCardOnDeckTopEffectAsset : EffectAsset
    {
        [SerializeField] private SdpTarget _source;

        /// <summary>対象カードを取り出すプレイヤー(<see cref="StackHandCardOnDeckTopEffect.Source"/>)。</summary>
        public SdpTarget Source => _source;

        /// <summary>テスト用 ctor。</summary>
        internal StackHandCardOnDeckTopEffectAsset(SdpTarget source)
        {
            _source = source;
        }

        /// <inheritdoc />
        public override IEffect ToDomain()
            => new StackHandCardOnDeckTopEffect(_source);
    }
}
