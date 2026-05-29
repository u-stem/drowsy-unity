using System;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Games.DrowZzz.Influences;
using UnityEngine;

namespace Drowsy.Infrastructure.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="PlayerInfluence"/> record を Unity SO で表現するための中間 POCO(INF-020 / INF-021)。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="ApplyInfluenceEffectAsset"/> が <c>[SerializeField] PlayerInfluenceAsset _influence</c> として保持し、
    /// <see cref="ToDomain"/> で <see cref="PlayerInfluence"/> に変換する。<see cref="TickEffect"/> は再帰的に
    /// <see cref="EffectAsset"/> 経由で表現するため <c>[SerializeReference]</c>(polymorphic serialization)。
    /// </para>
    /// <para>
    /// <see cref="PlayerInfluence"/> record の防御(TickEffect null チェック / RemainingCount >= 1)は ToDomain で
    /// record ctor 側に委譲する。null TickEffectAsset / RemainingCount = 0 等は <see cref="ArgumentException"/> 系を
    /// 上位 <see cref="ScriptableObjectCardCatalog.BuildEffectsFromAssets"/> が catch して skip + LogError(INF-019)。
    /// </para>
    /// </remarks>
    [Serializable]
    public sealed class PlayerInfluenceAsset
    {
        [SerializeField] private InfluenceTrigger _trigger;
        [SerializeReference] private EffectAsset _tickEffect;
        [SerializeField] private int _remainingCount;

        /// <summary>影響の発動トリガー(<see cref="PlayerInfluence.Trigger"/>)。</summary>
        public InfluenceTrigger Trigger => _trigger;

        /// <summary>Tick 時の効果(<see cref="PlayerInfluence.TickEffect"/>、SO 経路では <see cref="EffectAsset"/>)。</summary>
        public EffectAsset TickEffect => _tickEffect;

        /// <summary>残発動回数(<see cref="PlayerInfluence.RemainingCount"/>、1 以上が record 側で要求される)。</summary>
        public int RemainingCount => _remainingCount;

        /// <summary>テスト用 ctor。</summary>
        internal PlayerInfluenceAsset(InfluenceTrigger trigger, EffectAsset tickEffect, int remainingCount)
        {
            _trigger = trigger;
            _tickEffect = tickEffect;
            _remainingCount = remainingCount;
        }

        /// <summary>
        /// 本 SO 表現を <see cref="PlayerInfluence"/> ドメインモデルに変換する。
        /// <see cref="TickEffect"/> が null / TickEffect.ToDomain が失敗した場合は <see cref="ArgumentException"/> 系が伝播する
        /// (上位 <see cref="ScriptableObjectCardCatalog.BuildEffectsFromAssets"/> で catch + skip、INF-019)。
        /// </summary>
        /// <exception cref="ArgumentNullException"><see cref="TickEffect"/> が null</exception>
        /// <exception cref="ArgumentOutOfRangeException"><see cref="RemainingCount"/> が 0 以下</exception>
        public PlayerInfluence ToDomain()
        {
            if (_tickEffect is null)
            {
                throw new ArgumentNullException(nameof(TickEffect),
                    "PlayerInfluenceAsset.TickEffect が null です。");
            }
            return new PlayerInfluence(_trigger, _tickEffect.ToDomain(), _remainingCount);
        }
    }
}
