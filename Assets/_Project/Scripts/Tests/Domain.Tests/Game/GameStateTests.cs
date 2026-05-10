using System;
using System.Collections.Generic;
using NUnit.Framework;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Game;
using Drowsy.Domain.Players;

namespace Drowsy.Domain.Tests.Game
{
    [TestFixture]
    public class GameStateTests
    {
        // ===== ヘルパー =====

        private static PlayerState Player(string id) =>
            new PlayerState(PlayerId.Of(id), Hand.Empty);

        private static PlayerState PlayerWithHand(string id, params string[] cards)
        {
            var hand = Hand.Empty;
            foreach (var c in cards)
            {
                hand = hand.Add(CardId.Of(c));
            }
            return new PlayerState(PlayerId.Of(id), hand);
        }

        private static GameState NewState(
            IReadOnlyList<PlayerState> players = null,
            Pile deck = null,
            Pile discard = null,
            Pile field = null,
            TurnState turn = null)
        {
            return new GameState(
                players ?? new[] { Player("p1") },
                deck ?? Pile.Empty,
                discard ?? Pile.Empty,
                field ?? Pile.Empty,
                turn ?? TurnState.Initial(0));
        }

        // 普遍要件 GS-001 / GS-002 は record class + init-only + IReadOnlyList で構造保証

        // ===== GS-003: コンストラクタの値保持(1 テスト 1 アサーションで 5 フィールド分離)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "GS-003")]
        public void Given_有効な5引数_When_コンストラクタ_Then_Playersが入力と同じ()
        {
            // Given
            var players = new[] { Player("p1") };
            // When
            var state = new GameState(players, Pile.Empty, Pile.Empty, Pile.Empty, TurnState.Initial(0));
            // Then
            Assert.That(state.Players, Is.EqualTo(players));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "GS-003")]
        public void Given_有効な5引数_When_コンストラクタ_Then_Deckが入力と同じ()
        {
            // Given
            var deck = new Pile(new[] { CardId.Of("a") });
            // When
            var state = new GameState(new[] { Player("p1") }, deck, Pile.Empty, Pile.Empty, TurnState.Initial(0));
            // Then
            Assert.That(state.Deck, Is.EqualTo(deck));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "GS-003")]
        public void Given_有効な5引数_When_コンストラクタ_Then_Discardが入力と同じ()
        {
            // Given
            var discard = new Pile(new[] { CardId.Of("b") });
            // When
            var state = new GameState(new[] { Player("p1") }, Pile.Empty, discard, Pile.Empty, TurnState.Initial(0));
            // Then
            Assert.That(state.Discard, Is.EqualTo(discard));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "GS-003")]
        public void Given_有効な5引数_When_コンストラクタ_Then_Fieldが入力と同じ()
        {
            // Given
            var field = new Pile(new[] { CardId.Of("c") });
            // When
            var state = new GameState(new[] { Player("p1") }, Pile.Empty, Pile.Empty, field, TurnState.Initial(0));
            // Then
            Assert.That(state.Field, Is.EqualTo(field));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "GS-003")]
        public void Given_有効な5引数_When_コンストラクタ_Then_Turnが入力と同じ()
        {
            // Given
            var turn = new TurnState(3, 0);
            // When
            var state = new GameState(new[] { Player("p1") }, Pile.Empty, Pile.Empty, Pile.Empty, turn);
            // Then
            Assert.That(state.Turn, Is.EqualTo(turn));
        }

        // ===== GS-004: 値同値性(N=1 / N=2 / 5 フィールド異 / 順序異 / Players 数異 / RefEq / null) =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "GS-004")]
        public void Given_全フィールド一致N1_When_Equals_Then_等価()
        {
            var x = NewState();
            var y = NewState();
            Assert.That(x, Is.EqualTo(y));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "GS-004")]
        public void Given_2人で全フィールド一致_When_Equals_Then_等価()
        {
            var players = new[] { Player("p1"), Player("p2") };
            var x = NewState(players: players);
            var y = NewState(players: new[] { Player("p1"), Player("p2") });
            Assert.That(x, Is.EqualTo(y));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "GS-004")]
        public void Given_Players順序異_When_Equals_Then_非等価()
        {
            var x = NewState(players: new[] { Player("p1"), Player("p2") });
            var y = NewState(players: new[] { Player("p2"), Player("p1") });
            Assert.That(x, Is.Not.EqualTo(y));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "GS-004")]
        public void Given_Players数異_When_Equals_Then_非等価()
        {
            var x = NewState(players: new[] { Player("p1") });
            var y = NewState(players: new[] { Player("p1"), Player("p2") });
            Assert.That(x, Is.Not.EqualTo(y));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "GS-004")]
        public void Given_Deck異_When_Equals_Then_非等価()
        {
            var x = NewState(deck: new Pile(new[] { CardId.Of("a") }));
            var y = NewState(deck: new Pile(new[] { CardId.Of("b") }));
            Assert.That(x, Is.Not.EqualTo(y));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "GS-004")]
        public void Given_Discard異_When_Equals_Then_非等価()
        {
            var x = NewState(discard: new Pile(new[] { CardId.Of("a") }));
            var y = NewState(discard: Pile.Empty);
            Assert.That(x, Is.Not.EqualTo(y));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "GS-004")]
        public void Given_Field異_When_Equals_Then_非等価()
        {
            var x = NewState(field: new Pile(new[] { CardId.Of("a") }));
            var y = NewState(field: Pile.Empty);
            Assert.That(x, Is.Not.EqualTo(y));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "GS-004")]
        public void Given_Turn異_When_Equals_Then_非等価()
        {
            // Given: Turn だけが異なる(TurnNumber 異)
            var x = NewState(turn: new TurnState(1, 0));
            var y = NewState(turn: new TurnState(2, 0));
            Assert.That(x, Is.Not.EqualTo(y));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "GS-004")]
        public void Given_同一インスタンス_When_Equals_Then_等価()
        {
            var state = NewState();
            Assert.That(state.Equals(state), Is.True);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "GS-004")]
        public void Given_Equalsにnull_When_EqualsGameState_Then_false()
        {
            var state = NewState();
            GameState other = null;
            Assert.That(state.Equals(other), Is.False);
        }

        // ===== GS-005: GetHashCode =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "GS-005")]
        public void Given_等価な2つのGameState_When_GetHashCode_Then_同じ値を返す()
        {
            var x = NewState();
            var y = NewState();
            Assert.That(x.GetHashCode(), Is.EqualTo(y.GetHashCode()));
        }

        // ===== GS-006: operator== / operator!= =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "GS-006")]
        public void Given_等価な2つのGameState_When_operator_等価_Then_true()
        {
            var x = NewState();
            var y = NewState();
            Assert.That(x == y, Is.True);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "GS-006")]
        public void Given_非等価な2つのGameState_When_operator_等価_Then_false()
        {
            var x = NewState(deck: new Pile(new[] { CardId.Of("a") }));
            var y = NewState();
            Assert.That(x == y, Is.False);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "GS-006")]
        public void Given_非等価な2つのGameState_When_operator_非等価_Then_true()
        {
            var x = NewState(deck: new Pile(new[] { CardId.Of("a") }));
            var y = NewState();
            Assert.That(x != y, Is.True);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "GS-006")]
        public void Given_両方null_When_operator_等価_Then_true()
        {
            GameState x = null;
            GameState y = null;
            Assert.That(x == y, Is.True);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "GS-006")]
        public void Given_片方nullで他方非null_When_operator_等価_Then_false()
        {
            GameState x = null;
            var y = NewState();
            Assert.That(x == y, Is.False);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "GS-006")]
        public void Given_左側非nullで右側null_When_operator_等価_Then_false()
        {
            var x = NewState();
            GameState y = null;
            Assert.That(x == y, Is.False);
        }

        // ===== GS-007: Equals(object) =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "GS-007")]
        public void Given_null_When_Equalsオブジェクト_Then_false()
        {
            var state = NewState();
            Assert.That(state.Equals((object)null), Is.False);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "GS-007")]
        public void Given_異なる型_When_Equalsオブジェクト_Then_false()
        {
            var state = NewState();
            Assert.That(state.Equals((object)"not a GameState"), Is.False);
        }

        // ===== GS-008: with 式(3 観点に分割)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "GS-008")]
        public void Given_GameState_When_with式でDeckを差し替え_Then_新インスタンスのDeckが新値()
        {
            var original = NewState();
            var newDeck = new Pile(new[] { CardId.Of("a") });
            var updated = original with { Deck = newDeck };
            Assert.That(updated.Deck, Is.EqualTo(newDeck));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "GS-008")]
        public void Given_GameState_When_with式でDeckを差し替え_Then_Playersは不変()
        {
            // Given: Deck 差し替え時に Players が元と同じであることを確認
            var original = NewState(players: new[] { Player("p1") });
            // When
            var updated = original with { Deck = new Pile(new[] { CardId.Of("a") }) };
            // Then
            Assert.That(updated.Players, Is.EqualTo(original.Players));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "GS-008")]
        public void Given_GameState_When_with式でDeckを差し替え_Then_Discardは不変()
        {
            // Given: Deck 差し替え時に Discard が元と同じであることを確認
            var original = NewState(discard: new Pile(new[] { CardId.Of("d") }));
            // When
            var updated = original with { Deck = new Pile(new[] { CardId.Of("a") }) };
            // Then
            Assert.That(updated.Discard, Is.EqualTo(original.Discard));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "GS-008")]
        public void Given_GameState_When_with式でDeckを差し替え_Then_Fieldは不変()
        {
            // Given: Deck 差し替え時に Field が元と同じであることを確認
            var original = NewState(field: new Pile(new[] { CardId.Of("f") }));
            // When
            var updated = original with { Deck = new Pile(new[] { CardId.Of("a") }) };
            // Then
            Assert.That(updated.Field, Is.EqualTo(original.Field));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "GS-008")]
        public void Given_GameState_When_with式でDeckを差し替え_Then_元インスタンスは不変()
        {
            var original = NewState();
            _ = original with { Deck = new Pile(new[] { CardId.Of("a") }) };
            Assert.That(original.Deck, Is.EqualTo(Pile.Empty));
        }

        // ===== GS-009: 防御コピー =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "GS-009")]
        public void Given_生成後にソースリストを変更_When_Players参照_Then_影響を受けない()
        {
            // Given
            var source = new List<PlayerState> { Player("p1") };
            var state = new GameState(source, Pile.Empty, Pile.Empty, Pile.Empty, TurnState.Initial(0));
            // When
            source.Add(Player("p2"));
            // Then
            Assert.That(state.Players.Count, Is.EqualTo(1));
        }

        // ===== GS-010 〜 013: コンストラクタの 5 フィールドのうち 4 フィールド null(Turn null は GS-020) =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "GS-010")]
        public void Given_nullPlayers_When_コンストラクタ_Then_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new GameState(null, Pile.Empty, Pile.Empty, Pile.Empty, TurnState.Initial(0)));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "GS-011")]
        public void Given_nullDeck_When_コンストラクタ_Then_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new GameState(new[] { Player("p1") }, null, Pile.Empty, Pile.Empty, TurnState.Initial(0)));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "GS-012")]
        public void Given_nullDiscard_When_コンストラクタ_Then_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new GameState(new[] { Player("p1") }, Pile.Empty, null, Pile.Empty, TurnState.Initial(0)));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "GS-013")]
        public void Given_nullField_When_コンストラクタ_Then_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new GameState(new[] { Player("p1") }, Pile.Empty, Pile.Empty, null, TurnState.Initial(0)));
        }

        // ===== GS-014: players に null 要素 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "GS-014")]
        public void Given_null要素を含むplayers_When_コンストラクタ_Then_ArgumentException()
        {
            var players = new PlayerState[] { Player("p1"), null };
            Assert.Throws<ArgumentException>(
                () => new GameState(players, Pile.Empty, Pile.Empty, Pile.Empty, TurnState.Initial(0)));
        }

        // ===== GS-015: players に重複 PlayerId =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "GS-015")]
        public void Given_重複PlayerIdを含むplayers_When_コンストラクタ_Then_ArgumentException()
        {
            var players = new[] { Player("p1"), PlayerWithHand("p1", "a") };
            Assert.Throws<ArgumentException>(
                () => new GameState(players, Pile.Empty, Pile.Empty, Pile.Empty, TurnState.Initial(0)));
        }

        // ===== GS-016 〜 019: with 式での null 防御 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "GS-016")]
        public void Given_GameState_When_with式でPlayersをnullに_Then_ArgumentNullException()
        {
            var state = NewState();
            Assert.Throws<ArgumentNullException>(() => _ = state with { Players = null });
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "GS-017")]
        public void Given_GameState_When_with式でDeckをnullに_Then_ArgumentNullException()
        {
            var state = NewState();
            Assert.Throws<ArgumentNullException>(() => _ = state with { Deck = null });
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "GS-018")]
        public void Given_GameState_When_with式でDiscardをnullに_Then_ArgumentNullException()
        {
            var state = NewState();
            Assert.Throws<ArgumentNullException>(() => _ = state with { Discard = null });
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "GS-019")]
        public void Given_GameState_When_with式でFieldをnullに_Then_ArgumentNullException()
        {
            var state = NewState();
            Assert.Throws<ArgumentNullException>(() => _ = state with { Field = null });
        }

        // ===== GS-020 / GS-021 / GS-022: PR-5 で追加した Turn 関連の異常系 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "GS-020")]
        public void Given_nullTurn_When_コンストラクタ_Then_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new GameState(new[] { Player("p1") }, Pile.Empty, Pile.Empty, Pile.Empty, null));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "GS-021")]
        public void Given_GameState_When_with式でTurnをnullに_Then_ArgumentNullException()
        {
            var state = NewState();
            Assert.Throws<ArgumentNullException>(() => _ = state with { Turn = null });
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "GS-022")]
        public void Given_TurnのCurrentPlayerIndexがPlayers範囲外_When_コンストラクタ_Then_ArgumentException()
        {
            // Given: 1 人しかいないが Turn が CurrentPlayerIndex=1 を指す(範囲外)
            var players = new[] { Player("p1") };
            var outOfRangeTurn = new TurnState(1, 1);
            // When / Then
            Assert.Throws<ArgumentException>(
                () => new GameState(players, Pile.Empty, Pile.Empty, Pile.Empty, outOfRangeTurn));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "GS-022")]
        public void Given_GameState_When_with式でTurnを範囲外に_Then_ArgumentException()
        {
            // Given: with 式経由でも GS-022 の範囲検証が動くか
            var state = NewState(players: new[] { Player("p1") });
            var outOfRangeTurn = new TurnState(1, 5);
            // When / Then
            Assert.Throws<ArgumentException>(() => _ = state with { Turn = outOfRangeTurn });
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "GS-022")]
        public void Given_GameState_When_with式でPlayersを縮小して既存Turnが範囲外に_Then_ArgumentException()
        {
            // Given: 2 人 + Index=1 の Turn → Players を 1 人に縮小すると既存 Turn が範囲外になる
            var state = NewState(
                players: new[] { Player("p1"), Player("p2") },
                turn: new TurnState(1, 1));
            // When / Then
            Assert.Throws<ArgumentException>(
                () => _ = state with { Players = new[] { Player("p1") } });
        }
    }
}
