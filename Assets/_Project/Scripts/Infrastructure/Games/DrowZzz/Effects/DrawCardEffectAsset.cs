using System;
using Drowsy.Application.Games.DrowZzz.Effects;
using UnityEngine;

namespace Drowsy.Infrastructure.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="DrawCardEffect"/> を Unity SO で表現する POCO(INF-023 / INF-024)。
    /// </summary>
    /// <remarks>
    /// 「コップ一杯の脅威」(No.01)夜効果「自分が山札から手段を 1 枚引く」のため M2-PR3 で導入された
    /// <see cref="DrawCardEffect"/> の SO 対応。<see cref="AdjustSdpEffectAsset"/> と同パターンで
    /// <see cref="SdpTarget"/> + <see cref="int"/> の 2 フィールド構成。
    /// <para>
    /// <b>Count の検証ポリシー</b>(M4-PR3 code-reviewer P-1 反映 2026-05-13):
    /// <see cref="DrawCardEffect"/> record は <c>Count</c> に防御を持たない(graceful degradation、
    /// 山札枯渇は <see cref="EffectInterpreter"/> が「引ける分だけ引く」で吸収する(DZ-117)。
    /// 本 Asset も同方針で <c>Count = 0</c> や負値を Designer 設定可能化(Inspector で 0 設定は no-op として
    /// 通過、デバッグ用途等で許容)。<see cref="DamageBedEffect"/>(<c>Percent</c> 5 の倍数 + 正値検証)
    /// との非対称は意図的:DamageBed は L3 ゲームバランス値で厳格、Draw は engine 側 graceful。
    /// </para>
    /// </remarks>
    [Serializable]
    public sealed class DrawCardEffectAsset : EffectAsset
    {
        [SerializeField] private SdpTarget _target;
        [SerializeField] private int _count;

        /// <summary>引くプレイヤー(<see cref="DrawCardEffect.Target"/>)。</summary>
        public SdpTarget Target => _target;

        /// <summary>引く枚数(<see cref="DrawCardEffect.Count"/>、graceful 山札枯渇は <see cref="EffectInterpreter"/> 側)。</summary>
        public int Count => _count;

        /// <summary>テスト用 ctor。本番経路では Unity Inspector が <see cref="SerializeField"/> private field を直接初期化。</summary>
        internal DrawCardEffectAsset(SdpTarget target, int count)
        {
            _target = target;
            _count = count;
        }

        /// <inheritdoc />
        public override IEffect ToDomain() => new DrawCardEffect(_target, _count);
    }
}
