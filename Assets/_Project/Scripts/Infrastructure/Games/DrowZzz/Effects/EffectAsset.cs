using System;
using Drowsy.Application.Games.DrowZzz.Effects;

namespace Drowsy.Infrastructure.Games.DrowZzz.Effects
{
    /// <summary>
    /// Unity SO で <see cref="IEffect"/> 派生 record 群を表現するための <c>[Serializable]</c> POCO 基底。
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
    /// (CardData と同パターン)。<see cref="EffectInterpreter"/> 側は既存 record 群をそのまま受け取る。
    /// </para>
    /// <para>
    /// wrapper effect(<c>TimeOfDayBranchEffect</c> / <c>ChoiceEffect</c> /
    /// <c>KeywordedEffect</c>)は Inner 効果を <c>[SerializeReference]</c> で再帰的に保持する。
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
