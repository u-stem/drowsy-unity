using System;
using System.Collections.Generic;
using Drowsy.Application.Games.DrowZzz.Effects;
using UnityEngine;

namespace Drowsy.Infrastructure.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="KeywordedEffect"/> を Unity SO で表現する POCO(INF-043 / INF-044)。
    /// wrapper effect、<see cref="Inner"/> を <c>[SerializeReference]</c> で再帰保持。
    /// </summary>
    /// <remarks>
    /// 「夢」カード(No.00)の夜効果列(`KeywordedEffect([Frenzy, Instinct], EarlyWinTriggerEffect)`)等で使用される
    /// キーワード能力ラッパー。<see cref="Inner"/> が null の場合は <see cref="ToDomain"/> で <see cref="ArgumentNullException"/>
    /// が伝播し、上位 <see cref="ScriptableObjectCardCatalog.BuildEffectsFromAssets"/> で graceful skip(INF-019)。
    /// 本経路は INF-019 を Optional から通常 EARS に昇格させる本格テストの対象(M4-PR2 から M4-PR3 への持ち越し
    /// `docs/todo.md` エントリを本 PR で完了済へ移動)。
    /// </remarks>
    [Serializable]
    public sealed class KeywordedEffectAsset : EffectAsset
    {
        [SerializeField] private Keyword[] _keywords;
        [SerializeReference] private EffectAsset _inner;

        /// <summary>付与するキーワード(<see cref="KeywordedEffect.Keywords"/>、1 件以上 record 側で検証)。</summary>
        public IReadOnlyList<Keyword> Keywords =>
            _keywords ?? Array.Empty<Keyword>();

        /// <summary>キーワードを付与する対象の効果(<see cref="KeywordedEffect.Inner"/>、SO 経路では <see cref="EffectAsset"/>)。</summary>
        public EffectAsset Inner => _inner;

        /// <summary>テスト用 ctor。</summary>
        internal KeywordedEffectAsset(Keyword[] keywords, EffectAsset inner)
        {
            _keywords = keywords;
            _inner = inner;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><see cref="Inner"/> が null</exception>
        /// <exception cref="ArgumentException"><see cref="Keywords"/> が空 list(<see cref="KeywordedEffect"/> 側で検証)</exception>
        public override IEffect ToDomain()
        {
            if (_inner is null)
            {
                throw new ArgumentNullException(nameof(Inner),
                    "KeywordedEffectAsset.Inner が null です(SerializeReference の missing reference 等)。");
            }
            return new KeywordedEffect(Keywords, _inner.ToDomain());
        }
    }
}
