using System;
using Drowsy.Application.Games.DrowZzz.Effects;
using UnityEngine;

namespace Drowsy.Infrastructure.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="AdjustSdpEffect"/>(M2-PR3、ADR-0007 §1.4)を Unity SO で表現する POCO
    /// (M4-PR2 で導入、ADR-0012 §3、INF-014 / INF-016)。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 最初の SO 対応 effect として本 PR で導入。理由(ADR-0012 §9 / JIT 確定 2026-05-13):
    /// <list type="bullet">
    /// <item>最もシンプルな構造(<see cref="SdpTarget"/> enum + <see cref="int"/> の 2 フィールド)</item>
    /// <item>wrapper なし(Inner effect の再帰表現が不要)</item>
    /// <item>Marker でない(no-op 評価との切り分けが不要)</item>
    /// </list>
    /// 本 PR で <see cref="EffectAsset"/> 基底設計を検証し、M4-PR3 で残り 11 派生型を一気に展開する。
    /// </para>
    /// <para>
    /// テスト経路では <c>Drowsy.Infrastructure</c> asmdef の <c>InternalsVisibleTo</c> 属性
    /// (`AssemblyInfo.cs`)経由で internal ctor を呼んでインスタンスを構築する(Inspector の
    /// <see cref="SerializeField"/> private field を経由しないテスト構築パターン、
    /// <see cref="CardEntryAsset"/> / <see cref="AttributeEntry"/> と同パターン)。
    /// </para>
    /// </remarks>
    [Serializable]
    public sealed class AdjustSdpEffectAsset : EffectAsset
    {
        [SerializeField] private SdpTarget _target;
        [SerializeField] private int _delta;

        /// <summary>対象プレイヤー(<see cref="SdpTarget.Self"/> / <see cref="SdpTarget.Opponent"/>)。</summary>
        public SdpTarget Target => _target;

        /// <summary>SDP 増減量(正値=増、負値=減、0=no-op、0 floor なし、DZ-109 と整合)。</summary>
        public int Delta => _delta;

        /// <summary>
        /// テスト用 ctor。本番経路では Unity Inspector が <see cref="SerializeField"/> private field を直接初期化するため、
        /// 本 ctor は <c>internal</c> でテスト asmdef からのみアクセス可能。
        /// </summary>
        internal AdjustSdpEffectAsset(SdpTarget target, int delta)
        {
            _target = target;
            _delta = delta;
        }

        /// <inheritdoc />
        public override IEffect ToDomain() => new AdjustSdpEffect(_target, _delta);
    }
}
