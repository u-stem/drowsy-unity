using System;
using Drowsy.Application.Games.DrowZzz.Effects;

namespace Drowsy.Infrastructure.Games.DrowZzz.Effects
{
    /// <summary>
    /// Unity SO で <see cref="IEffect"/> 派生 record 群を表現するための <c>[Serializable]</c> POCO 基底
    /// (M4-PR2 で導入、ADR-0012 §3 案 (a) `[Serializable]` POCO + 変換層、JIT 確定 2026-05-13)。
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unity Inspector は <see cref="IEffect"/> マーカー interface を直接シリアライズできない(record の値同値性 /
    /// immutability と SO の参照同一性 / Inspector 編集が衝突する)。本 abstract class を継承した派生 sealed class
    /// (例: <see cref="AdjustSdpEffectAsset"/>)を `[Serializable]` で配列要素として保持し、Designer は
    /// <c>[SerializeReference] EffectAsset[]</c> 経由で polymorphic 編集する。
    /// </para>
    /// <para>
    /// 評価経路:<see cref="ScriptableObjectCardCatalog.GetEffects"/> は <c>RebuildCache</c> 時に各 entry の
    /// <c>EffectAsset[]</c> を walk して <see cref="ToDomain"/> を呼び、結果の <see cref="IEffect"/> をキャッシュする
    /// (CardData と同パターン)。<see cref="EffectInterpreter"/> 側は本 PR 範囲では変更なし(既存 record 群を
    /// そのまま受け取る、ADR-0012 §3 案 (a) の利点)。
    /// </para>
    /// <para>
    /// 派生型は M4-PR2 で <see cref="AdjustSdpEffectAsset"/>(最初の 1 派生型)、M4-PR3 で残り 11 派生型を順次対応する
    /// (ADR-0012 §9 PR 分割計画)。wrapper effect(<c>TimeOfDayBranchEffect</c> / <c>ChoiceEffect</c> /
    /// <c>KeywordedEffect</c>)の Inner 表現方式は M4-PR3 着手時に JIT 確定する(ADR-0012 §「M4-PR3 着手時の JIT 確認項目」)。
    /// </para>
    /// </remarks>
    [Serializable]
    public abstract class EffectAsset
    {
        /// <summary>
        /// 本 SO 表現を Application 層の <see cref="IEffect"/> ドメインモデルに変換する
        /// (Infrastructure → Application の Ports &amp; Adapters 経路、INF-013 / INF-016)。
        /// </summary>
        /// <returns>対応する <see cref="IEffect"/> 派生 record の新しいインスタンス</returns>
        /// <exception cref="ArgumentException">
        /// 派生型のフィールド値が <see cref="IEffect"/> 派生 record の ctor 防御に違反する場合
        /// (例:null 必須引数 / 範囲外数値)。<see cref="ScriptableObjectCardCatalog.RebuildCache"/> は
        /// 本例外を catch して skip + <c>Debug.LogError</c> する(INF-019 graceful)。
        /// </exception>
        public abstract IEffect ToDomain();
    }
}
