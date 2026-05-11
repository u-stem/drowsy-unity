using System;
using System.Collections.Generic;
using System.Linq;
using Drowsy.Domain.Random;

namespace Drowsy.Application.Games.DrowZzz
{
    /// <summary>
    /// DrowZzz の DDP (Draw Drowsy Point) 共有プールを表す不変値オブジェクト。
    /// 先頭が次の抽選対象(Top)、末尾が Bottom。全変更操作は新インスタンスを返す純関数。
    /// </summary>
    /// <remarks>
    /// ADR-0009 §「DDP プールの構造」/ §3「DDP プールの構造と保持」で確定したプール表現。本クラスは
    /// <see cref="Drowsy.Domain.Cards.Pile"/> と同パターン(Shuffle / Draw / 順序付きシーケンス同値の
    /// Equals / GetHashCode)を、`int` 値列向けに再実装する。
    /// <para>
    /// <b>専用型を新設した理由</b>: ADR-0009 §3 では「Pile 型を再利用」と書かれているが、<c>Pile</c> は
    /// <c>CardId[]</c> を保持するカード山札専用型で、整数プール (-30〜+30) を直接持つには semantic 違反
    /// (<c>CardId</c> は <c>string Value</c> ベースの識別子型)。型シグネチャから「DDP プール」が明示され、
    /// <c>Pile</c>(カード山札)との取り違いを構造的に防ぐ。詳細は <c>dp-mechanism-ddp.md</c> §「設計判断」。
    /// </para>
    /// <para>
    /// 等値性は順序付きシーケンス同値(<see cref="Pile"/> と同パターン、ADR-0002 §「Domain 集合型の値同値性方針」
    /// と整合)。<c>System.Collections.Immutable</c> が Unity 6 で internal アクセシビリティのため利用不可、
    /// 代替として <c>int[]</c> を private で保持し <see cref="Values"/> プロパティ経由で
    /// <see cref="IReadOnlyList{T}"/> として読み取り専用公開する(<see cref="Pile"/> と同設計)。
    /// </para>
    /// </remarks>
    public sealed class DdpPool : IEquatable<DdpPool>
    {
        private readonly int[] _values;

        /// <summary>残プール値の列(先頭が次の抽選対象、読み取り専用)。</summary>
        public IReadOnlyList<int> Values => _values;

        /// <summary>残プールの枚数。</summary>
        public int Count => _values.Length;

        /// <summary>プールが空かどうか。</summary>
        public bool IsEmpty => _values.Length == 0;

        /// <summary>空 DdpPool のシングルトン。</summary>
        public static DdpPool Empty { get; } = new DdpPool(Array.Empty<int>());

        /// <summary>値列から DdpPool を生成する(防御コピーで保持)。</summary>
        /// <param name="values">プール初期値の列(先頭が Top)</param>
        /// <exception cref="ArgumentNullException">values が null</exception>
        public DdpPool(IEnumerable<int> values)
        {
            if (values is null)
            {
                throw new ArgumentNullException(nameof(values));
            }
            _values = values.ToArray();
        }

        // 内部用: 既に所有権を持つ配列を直接ラップする(防御コピーを省略)。
        // Shuffle / Draw など本クラス内の純関数経路でのみ呼ぶ。
        private DdpPool(int[] values)
        {
            _values = values;
        }

        /// <summary>
        /// 先頭から 1 枚抽選した結果と、残プールを返す。
        /// </summary>
        /// <returns>抽選値と残プールのタプル</returns>
        /// <exception cref="InvalidOperationException">空のプールに対する呼び出し</exception>
        public (int Drawn, DdpPool Remaining) Draw()
        {
            if (IsEmpty)
            {
                throw new InvalidOperationException("空の DdpPool からは Draw できません");
            }
            var drawn = _values[0];
            var remaining = new int[_values.Length - 1];
            Array.Copy(_values, 1, remaining, 0, remaining.Length);
            return (drawn, new DdpPool(remaining));
        }

        /// <summary>
        /// Fisher-Yates シャッフルした新 DdpPool を返す。<paramref name="rng"/> が決定的なら結果も決定的。
        /// </summary>
        /// <exception cref="ArgumentNullException">rng が null</exception>
        public DdpPool Shuffle(IRandomSource rng)
        {
            if (rng is null)
            {
                throw new ArgumentNullException(nameof(rng));
            }
            var array = (int[])_values.Clone();
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = rng.NextInt(0, i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }
            return new DdpPool(array);
        }

        /// <summary>順序付きシーケンス同値で比較する。</summary>
        public bool Equals(DdpPool other)
        {
            if (other is null)
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            if (_values.Length != other._values.Length)
            {
                return false;
            }
            for (int i = 0; i < _values.Length; i++)
            {
                if (_values[i] != other._values[i])
                {
                    return false;
                }
            }
            return true;
        }

        public override bool Equals(object obj) => obj is DdpPool other && Equals(other);

        /// <summary>値同値で == 比較する。両方 null は等価、片方のみ null は非等価。</summary>
        public static bool operator ==(DdpPool left, DdpPool right) =>
            left is null ? right is null : left.Equals(right);

        /// <summary>値同値で != 比較する。</summary>
        public static bool operator !=(DdpPool left, DdpPool right) => !(left == right);

        /// <summary>順序依存ハッシュ。<see cref="HashCode"/> struct に各値を順次合成する。</summary>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            for (int i = 0; i < _values.Length; i++)
            {
                hash.Add(_values[i]);
            }
            return hash.ToHashCode();
        }
    }
}
