using System;

namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// 指定プレイヤー(<see cref="SdpTarget.Self"/> / <see cref="SdpTarget.Opponent"/>)のベッド破損率を
    /// <see cref="Percent"/> だけ増加させる効果。
    /// </summary>
    /// <param name="Target">破損を与える対象プレイヤー</param>
    /// <param name="Percent">破損率の増加幅(常に <see cref="DrowZzzBedConstants.BedDamageRatePerSdp"/> の正の倍数、ADR-0011 §3 JIT 確定 2026-05-12)</param>
    /// <remarks>
    /// ADR-0011 §3「破損率増加トリガー」JIT 確定の効果 record として M3-PR2 で導入。
    /// 「特定のカードによって破損率は増加します、パーセンテージはカード固有ですが、常に 5 の倍数です」
    /// (プロジェクトオーナー JIT 共有 2026-05-12)に基づき、本 record は <see cref="Percent"/> が
    /// 5 の倍数かつ正値であることを positional ctor で検証する。
    /// <para>
    /// 評価は <see cref="EffectInterpreter.Apply"/> で行う。対象プレイヤーの <c>BedDamages</c> を
    /// <see cref="Percent"/> 分増加させ、<see cref="DrowZzzBedConstants.MaxBedDamagePercent"/>(100%)で上限クランプ。
    /// 修繕は <c>AbandonAction(AbandonChoice.RepairBed)</c>(M3-PR3 で実装予定)で別途行う。
    /// </para>
    /// <para>
    /// 値型 <see cref="int"/> は record の二重ガード null 防御の対象外だが、<see cref="Percent"/> の
    /// 5 の倍数 / 正値検証は init setter 内で行う(`with { Percent = ... }` 経由の不正値も同検証で防ぐ)。
    /// </para>
    /// </remarks>
    public sealed record DamageBedEffect : IEffect
    {
        private readonly int _percent;

        /// <summary>破損を与える対象プレイヤー。</summary>
        public SdpTarget Target { get; init; }

        /// <summary>破損率の増加幅(常に 5 の正の倍数、ADR-0011 §3 JIT 確定)。</summary>
        public int Percent
        {
            get => _percent;
            init => _percent = ValidatePercent(value);
        }

        /// <exception cref="ArgumentException"><paramref name="Percent"/> が 5 の正の倍数でない場合</exception>
        public DamageBedEffect(SdpTarget Target, int Percent)
        {
            this.Target = Target;
            this.Percent = Percent;
        }

        // Percent の不変条件検証: 5 の倍数 + 正値(0 / 負値は不採用)
        // 0 を許容しない理由: 「破損率を上げる」効果なので 0 は意味的に無効
        // 負値を許容しない理由: 修繕は AbandonChoice.RepairBed の責務、effect として混在させない
        // paramName は呼び出し元(positional ctor / `with { Percent = ... }` init setter 両方)に関わらず
        // 固定で "Percent" を使う(C# のコンパイル時文字列 nameof(Percent))。
        private static int ValidatePercent(int value)
        {
            if (value <= 0)
            {
                throw new ArgumentException(
                    $"DamageBedEffect.Percent は正値である必要があります(現在: {value})。修繕は AbandonAction で行う",
                    nameof(Percent));
            }
            if (value % DrowZzzBedConstants.BedDamageRatePerSdp != 0)
            {
                throw new ArgumentException(
                    $"DamageBedEffect.Percent は {DrowZzzBedConstants.BedDamageRatePerSdp} の倍数である必要があります(現在: {value}、ADR-0011 §3 JIT 確定)",
                    nameof(Percent));
            }
            return value;
        }
    }
}
