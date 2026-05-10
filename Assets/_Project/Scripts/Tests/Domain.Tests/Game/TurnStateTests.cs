using System;
using NUnit.Framework;
using Drowsy.Domain.Game;

namespace Drowsy.Domain.Tests.Game
{
    [TestFixture]
    public class TurnStateTests
    {
        // 普遍要件 TURN-001 / TURN-002 は record + init-only で構造保証

        // ===== TURN-003: コンストラクタの値保持(1 テスト 1 アサーション) =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "TURN-003")]
        public void Given_有効な値_When_コンストラクタ_Then_TurnNumberが入力と同じ()
        {
            var turn = new TurnState(3, 1);
            Assert.That(turn.TurnNumber, Is.EqualTo(3));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "TURN-003")]
        public void Given_有効な値_When_コンストラクタ_Then_CurrentPlayerIndexが入力と同じ()
        {
            var turn = new TurnState(3, 1);
            Assert.That(turn.CurrentPlayerIndex, Is.EqualTo(1));
        }

        // ===== TURN-004: Initial factory =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "TURN-004")]
        public void Given_playerIndex0_When_Initial_Then_TurnNumber1とCurrentPlayerIndex0()
        {
            var turn = TurnState.Initial(0);
            Assert.That(turn, Is.EqualTo(new TurnState(1, 0)));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "TURN-004")]
        public void Given_playerIndex2_When_Initial_Then_TurnNumber1とCurrentPlayerIndex2()
        {
            var turn = TurnState.Initial(2);
            Assert.That(turn, Is.EqualTo(new TurnState(1, 2)));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "TURN-004")]
        public void Given_引数なし_When_Initial_Then_デフォルトでplayerIndex0と等価()
        {
            // Given: Initial() のデフォルト引数(playerIndex = 0)経路をカバー
            var defaultInitial = TurnState.Initial();
            var explicitInitial = TurnState.Initial(0);
            Assert.That(defaultInitial, Is.EqualTo(explicitInitial));
        }

        // ===== TURN-005: Next - TurnNumber +1 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "TURN-005")]
        public void Given_TurnState_When_Next_Then_TurnNumberが1増える()
        {
            // Given
            var turn = new TurnState(3, 0);
            // When
            var next = turn.Next(2);
            // Then
            Assert.That(next.TurnNumber, Is.EqualTo(4));
        }

        // ===== TURN-006: Next - CurrentPlayerIndex 循環 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "TURN-006")]
        public void Given_中間プレイヤー_When_Next_Then_次のIndex()
        {
            // Given: 3 人中 0 番から進める
            var turn = new TurnState(1, 0);
            // When
            var next = turn.Next(3);
            // Then
            Assert.That(next.CurrentPlayerIndex, Is.EqualTo(1));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "TURN-006")]
        public void Given_最終プレイヤー_When_Next_Then_Indexが0に戻る()
        {
            // Given: 3 人中 2 番(最終)から進める → 巻き戻り
            var turn = new TurnState(1, 2);
            // When
            var next = turn.Next(3);
            // Then
            Assert.That(next.CurrentPlayerIndex, Is.EqualTo(0));
        }

        // ===== TURN-007: 値同値性 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "TURN-007")]
        public void Given_同じTurnNumberとIndex_When_Equals_Then_等価()
        {
            var x = new TurnState(3, 1);
            var y = new TurnState(3, 1);
            Assert.That(x, Is.EqualTo(y));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "TURN-007")]
        public void Given_異なるTurnNumber_When_Equals_Then_非等価()
        {
            var x = new TurnState(3, 1);
            var y = new TurnState(4, 1);
            Assert.That(x, Is.Not.EqualTo(y));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "TURN-007")]
        public void Given_異なるIndex_When_Equals_Then_非等価()
        {
            var x = new TurnState(3, 1);
            var y = new TurnState(3, 2);
            Assert.That(x, Is.Not.EqualTo(y));
        }

        // ===== TURN-008: GetHashCode =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "TURN-008")]
        public void Given_等価な2つのTurnState_When_GetHashCode_Then_同じ値を返す()
        {
            var x = new TurnState(3, 1);
            var y = new TurnState(3, 1);
            Assert.That(x.GetHashCode(), Is.EqualTo(y.GetHashCode()));
        }

        // ===== TURN-009 〜 012: 異常系 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "TURN-009")]
        public void Given_turnNumber0_When_コンストラクタ_Then_ArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TurnState(0, 0));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "TURN-010")]
        public void Given_currentPlayerIndex負_When_コンストラクタ_Then_ArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TurnState(1, -1));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "TURN-011")]
        public void Given_playerIndex負_When_Initial_Then_ArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TurnState.Initial(-1));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "TURN-012")]
        public void Given_playerCount0_When_Next_Then_ArgumentOutOfRangeException()
        {
            var turn = TurnState.Initial(0);
            Assert.Throws<ArgumentOutOfRangeException>(() => turn.Next(0));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "TURN-012")]
        public void Given_playerCount負_When_Next_Then_ArgumentOutOfRangeException()
        {
            var turn = TurnState.Initial(0);
            Assert.Throws<ArgumentOutOfRangeException>(() => turn.Next(-1));
        }
    }
}
