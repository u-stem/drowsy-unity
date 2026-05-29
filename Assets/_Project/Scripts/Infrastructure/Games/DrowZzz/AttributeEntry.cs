using System;
using UnityEngine;

namespace Drowsy.Infrastructure.Games.DrowZzz
{
    /// <summary>
    /// <see cref="CardEntryAsset.Attributes"/> の要素として <c>Dictionary&lt;string, int&gt;</c> を
    /// Unity Inspector で編集可能に表現する <c>[Serializable]</c> POCO。
    /// </summary>
    /// <remarks>
    /// Unity Inspector は <see cref="System.Collections.Generic.Dictionary{TKey, TValue}"/> を直接
    /// シリアライズできないため、key / value 1 ペアを保持する小型 record 風 class で配列化する。
    /// <para>
    /// テスト経路では <c>Drowsy.Infrastructure</c> asmdef の <c>InternalsVisibleTo</c> 属性
    /// (<c>AssemblyInfo.cs</c> ファイルで `Drowsy.Infrastructure.Tests` に許可)で内部 ctor を呼び、
    /// インスタンスを直接構築する(Inspector の <see cref="SerializeField"/> private field を経由しない
    /// テスト構築パターン、INF-003、M4-PR1 code-reviewer P-4 反映 2026-05-14)。
    /// </para>
    /// </remarks>
    [Serializable]
    public sealed class AttributeEntry
    {
        [SerializeField] private string _key;
        [SerializeField] private int _value;

        /// <summary>属性 key(non-null / 非空白を <see cref="CardData"/> 構築時に検証)。</summary>
        public string Key => _key;

        /// <summary>属性 value(任意 int)。</summary>
        public int Value => _value;

        /// <summary>
        /// テスト用 ctor。本番経路では Unity Inspector が <see cref="SerializeField"/> private field を直接初期化するため、
        /// 本 ctor は <c>internal</c> でテスト asmdef からのみアクセス可能。
        /// </summary>
        internal AttributeEntry(string key, int value)
        {
            _key = key;
            _value = value;
        }
    }
}
