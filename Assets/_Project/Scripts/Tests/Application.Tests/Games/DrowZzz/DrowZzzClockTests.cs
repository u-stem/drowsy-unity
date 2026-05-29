using Drowsy.Application.Games.DrowZzz;
using NUnit.Framework;

namespace Drowsy.Application.Tests.Games.DrowZzz
{
    /// <summary>
    /// <see cref="DrowZzzClock"/> の単体テスト。
    /// `record class DrowZzzClock(int RoundNumber)` の
    /// `Hour` / `Minute` / `IsNight` / `IsMorning` computed プロパティを境界値で網羅する。
    /// コメント内では「ターン (30 分単位)」と表現する。
    /// </summary>
    [TestFixture]
    public sealed class DrowZzzClockTests
    {
        // ===== DZ-090 / DZ-094: Hour 計算式の境界値カバレッジ =====
        // 式: Hour = (21 + (RoundNumber - 1) / 2) % 24
        // Round 1=21:00 / Round 2=21:30 / Round 16=04:30 (夜の終端) / Round 17=05:00 (朝の始端) / Round 21=07:00 (最終ターン)

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-090"), Property("Requirement", "DZ-094")]
        public void Given_RoundNumberが1_When_Hourを取得_Then_21を返す()
        {
            // Given(Round 1 = 21:00、夜の始端 / ゲーム開始時刻、DZ-094 境界)
            var clock = new DrowZzzClock(1);
            // When / Then
            Assert.That(clock.Hour, Is.EqualTo(21));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-090")]
        public void Given_RoundNumberが2_When_Hourを取得_Then_21を返す()
        {
            // Given(Round 2 は 21:30、Hour 部分は 21 のまま。Minute 桁上がり境界の準備テスト)
            var clock = new DrowZzzClock(2);
            // When / Then
            Assert.That(clock.Hour, Is.EqualTo(21));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-090"), Property("Requirement", "DZ-094")]
        public void Given_RoundNumberが16_When_Hourを取得_Then_4を返す()
        {
            // Given(Round 16 = 04:30、夜の終端、DZ-094 境界)
            var clock = new DrowZzzClock(16);
            // When / Then
            Assert.That(clock.Hour, Is.EqualTo(4));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-090"), Property("Requirement", "DZ-094")]
        public void Given_RoundNumberが17_When_Hourを取得_Then_5を返す()
        {
            // Given(Round 17 = 05:00、朝の始端、DZ-094 境界)
            var clock = new DrowZzzClock(17);
            // When / Then
            Assert.That(clock.Hour, Is.EqualTo(5));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-090"), Property("Requirement", "DZ-094")]
        public void Given_RoundNumberが21_When_Hourを取得_Then_7を返す()
        {
            // Given(Round 21 = 07:00、最終プレイ可能ターン、DZ-094 境界)
            var clock = new DrowZzzClock(21);
            // When / Then
            Assert.That(clock.Hour, Is.EqualTo(7));
        }

        // ===== DZ-091 / DZ-094: Minute 計算式の境界値カバレッジ =====
        // 式: Minute = ((RoundNumber - 1) % 2) * 30

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-091"), Property("Requirement", "DZ-094")]
        public void Given_RoundNumberが1_When_Minuteを取得_Then_0を返す()
        {
            // Given(Round 1 = 21:00、DZ-094 境界)
            var clock = new DrowZzzClock(1);
            // When / Then
            Assert.That(clock.Minute, Is.EqualTo(0));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-091")]
        public void Given_RoundNumberが2_When_Minuteを取得_Then_30を返す()
        {
            // Given(Round 2 = 21:30、奇数 RoundNumber で Minute が 30 になる境界)
            var clock = new DrowZzzClock(2);
            // When / Then
            Assert.That(clock.Minute, Is.EqualTo(30));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-091"), Property("Requirement", "DZ-094")]
        public void Given_RoundNumberが16_When_Minuteを取得_Then_30を返す()
        {
            // Given(Round 16 = 04:30、夜の終端、DZ-094 境界)
            var clock = new DrowZzzClock(16);
            // When / Then
            Assert.That(clock.Minute, Is.EqualTo(30));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-091"), Property("Requirement", "DZ-094")]
        public void Given_RoundNumberが17_When_Minuteを取得_Then_0を返す()
        {
            // Given(Round 17 = 05:00、朝の始端、DZ-094 境界)
            var clock = new DrowZzzClock(17);
            // When / Then
            Assert.That(clock.Minute, Is.EqualTo(0));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-091"), Property("Requirement", "DZ-094")]
        public void Given_RoundNumberが21_When_Minuteを取得_Then_0を返す()
        {
            // Given(Round 21 = 07:00、最終プレイ可能ターン、DZ-094 境界)
            var clock = new DrowZzzClock(21);
            // When / Then
            Assert.That(clock.Minute, Is.EqualTo(0));
        }

        // ===== DZ-092 / DZ-095: IsNight 境界(夜 = Round 1〜16) =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-092")]
        public void Given_RoundNumberが1_When_IsNightを取得_Then_true()
        {
            // Given(夜の始端)
            var clock = new DrowZzzClock(1);
            // When / Then
            Assert.That(clock.IsNight, Is.True);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-092")]
        public void Given_RoundNumberが16_When_IsNightを取得_Then_true()
        {
            // Given(夜の終端 04:30)
            var clock = new DrowZzzClock(16);
            // When / Then
            Assert.That(clock.IsNight, Is.True);
        }

        [Test, Category("Small"), Category("SemiNormal"), Property("Requirement", "DZ-095")]
        public void Given_RoundNumberが17_When_IsNightを取得_Then_false()
        {
            // Given(朝の始端 = 夜ではない)
            var clock = new DrowZzzClock(17);
            // When / Then
            Assert.That(clock.IsNight, Is.False);
        }

        // ===== DZ-093 / DZ-096: IsMorning 境界(朝 = Round 17〜21) =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-093")]
        public void Given_RoundNumberが17_When_IsMorningを取得_Then_true()
        {
            // Given(朝の始端 05:00)
            var clock = new DrowZzzClock(17);
            // When / Then
            Assert.That(clock.IsMorning, Is.True);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-093")]
        public void Given_RoundNumberが21_When_IsMorningを取得_Then_true()
        {
            // Given(朝の終端 = 最終ターン 07:00)
            var clock = new DrowZzzClock(21);
            // When / Then
            Assert.That(clock.IsMorning, Is.True);
        }

        [Test, Category("Small"), Category("SemiNormal"), Property("Requirement", "DZ-096")]
        public void Given_RoundNumberが16_When_IsMorningを取得_Then_false()
        {
            // Given(夜の終端 = 朝ではない)
            var clock = new DrowZzzClock(16);
            // When / Then
            Assert.That(clock.IsMorning, Is.False);
        }

        // ===== DZ-098: RoundNumber > 21 での過渡的防御値 =====
        // 時計仕様上 07:30 (Round 22 相当) は存在しないが、
        // computed プロパティとして数学的計算結果は返るため、夜・朝判定は両方 false を返すことで誤読を防ぐ。

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-098")]
        public void Given_RoundNumberが22_When_IsNightを取得_Then_false()
        {
            // Given(M3 でガード予定の過渡的「夜でも朝でもない」範囲)
            var clock = new DrowZzzClock(22);
            // When / Then
            Assert.That(clock.IsNight, Is.False);
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-098")]
        public void Given_RoundNumberが22_When_IsMorningを取得_Then_false()
        {
            // Given(M3 でガード予定の過渡的「夜でも朝でもない」範囲)
            var clock = new DrowZzzClock(22);
            // When / Then
            Assert.That(clock.IsMorning, Is.False);
        }

        // ===== DZ-089: record の auto-generated Equals / GetHashCode 構造保証の regression guard =====
        // positional record `DrowZzzClock(int RoundNumber)` の値同値性は C# 言語仕様で構造的に保証されるが、
        // 将来 `DrowZzzClock` が positional から手動 Equals 実装に変わるケースの regression guard を 2 件残す。

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-089")]
        public void Given_同じRoundNumberの2つのDrowZzzClock_When_等価比較_Then_等価()
        {
            // Given(同 RoundNumber で別 instance を 2 つ生成)
            var a = new DrowZzzClock(7);
            var b = new DrowZzzClock(7);
            // When / Then
            Assert.That(a, Is.EqualTo(b));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-089")]
        public void Given_異なるRoundNumberの2つのDrowZzzClock_When_等価比較_Then_非等価()
        {
            // Given(異なる RoundNumber)
            var a = new DrowZzzClock(7);
            var b = new DrowZzzClock(8);
            // When / Then
            Assert.That(a, Is.Not.EqualTo(b));
        }
    }
}
