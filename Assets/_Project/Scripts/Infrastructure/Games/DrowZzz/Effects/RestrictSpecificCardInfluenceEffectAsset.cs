using System;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Domain.Cards;
using UnityEngine;

namespace Drowsy.Infrastructure.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="RestrictSpecificCardInfluenceEffect"/>(No.04「静寂を纏う」)を Unity SO で表現する POCO。
    /// </summary>
    /// <remarks>
    /// `PlayerInfluence.TickEffect` として保有され、`IsLegalPlayCard` で <see cref="TargetCardTypeId"/> 一致のカードプレイを
    /// illegal 化する判別用 marker(`UsageRestrictionMarkerEffect` と同パターン、ただし「全カード使用禁止」ではなく「特定カード使用禁止」)。
    /// SO 上は <see cref="CardTypeId"/> を string 値で保持(`CardId.Of` と同パターン、Inspector 編集可能性を確保)。
    /// <para>
    /// <b>本 PR ② 時点の用途</b>:現状の No.04「静寂を纏う」では本 effect は <c>runtime 動的構築</c>(`PlayCardAction.TargetCardId` から
    /// `EffectInterpreter.ApplyApplyTargetedRestriction` 内で生成)で利用するため、`DrowZzzCardCatalog.asset` の effects 列に
    /// 直接 SerializeReference ノードを持たない。本 SO Asset は <b>将来の汎用性</b> のために対称設計(`Domain` ↔ `EffectAsset`)を維持しており、
    /// 「特定カード使用禁止を初期 catalog で直接 Designer 表現したい」要件が出てきた時点で SerializeReference 直接登録が可能。
    /// `ToDomain()` は <see cref="Drowsy.Infrastructure.Persistence.Converters.EffectJsonConverter"/> 経由の JSON deserialize 経路でも
    /// 呼ばれる(セッション永続化:`PlayerInfluence.TickEffect` の Newtonsoft 経由 round-trip、JSON 上は同名 discriminator
    /// `"RestrictSpecificCardInfluence"` で表現される、PR ② code-reviewer W-3 反映 2026-05-17)。
    /// </para>
    /// </remarks>
    [Serializable]
    public sealed class RestrictSpecificCardInfluenceEffectAsset : EffectAsset
    {
        [SerializeField] private string _targetCardTypeIdValue;

        /// <summary>使用禁止対象のカード型 ID(string 値、Inspector で編集可能)。</summary>
        public string TargetCardTypeIdValue => _targetCardTypeIdValue;

        /// <summary>テスト用 ctor。</summary>
        internal RestrictSpecificCardInfluenceEffectAsset(string targetCardTypeIdValue)
        {
            _targetCardTypeIdValue = targetCardTypeIdValue;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><see cref="TargetCardTypeIdValue"/> が null</exception>
        /// <exception cref="ArgumentException"><see cref="TargetCardTypeIdValue"/> が <see cref="CardTypeId"/> 不正値</exception>
        public override IEffect ToDomain()
        {
            if (_targetCardTypeIdValue is null)
            {
                throw new ArgumentNullException(nameof(TargetCardTypeIdValue),
                    "RestrictSpecificCardInfluenceEffectAsset.TargetCardTypeIdValue が null です。");
            }
            return new RestrictSpecificCardInfluenceEffect(CardTypeId.Of(_targetCardTypeIdValue));
        }
    }
}
