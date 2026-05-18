using System;
using System.Collections.Generic;
using Drowsy.Application.Catalog;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Games.DrowZzz.Influences;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Game;
using Drowsy.Domain.Players;
using NUnit.Framework;

namespace Drowsy.Application.Tests.Games.DrowZzz
{
    /// <summary>
    /// <see cref="AssociateAction"/> の合法性判定(`IsLegalMove`)と状態遷移(`Apply`)を検証する
    /// (DZ-205 / DZ-206)。ADR-0011 §1 / M3-PR4 で導入。
    /// </summary>
    [TestFixture]
    public sealed class AssociateActionTests
    {
        // ===== ヘルパー =====

        // 連想用カードの CardId(本テストで catalog に登録する)
        private static readonly CardId DreamCardId = CardId.Of(CardTypeId.Of("dream"), 0);

        // 連想可能カード(AssociatableMarkerEffect を効果列に持つ)を含む rule
        private static DrowZzzRule NewRuleWithAssociatable()
        {
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(DreamCardId.TypeId, new CardData("夢", new Dictionary<string, int>())),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    DreamCardId.TypeId,
                    new IEffect[] { new AssociatableMarkerEffect() }),
            };
            return new DrowZzzRule(new InMemoryCardCatalog(entries, effects), new EffectInterpreter());
        }

        // マーカー effect を持たないカード(catalog には登録、効果列なし)を含む rule
        private static DrowZzzRule NewRuleWithNonAssociatable()
        {
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(DreamCardId.TypeId, new CardData("非連想カード", new Dictionary<string, int>())),
            };
            return new DrowZzzRule(new InMemoryCardCatalog(entries), new EffectInterpreter());
        }

        // catalog 完全空の rule(action.Card 未登録の合法性チェック用)
        private static DrowZzzRule NewRuleWithEmptyCatalog()
        {
            return new DrowZzzRule(
                new InMemoryCardCatalog(new KeyValuePair<CardTypeId, CardData>[0]),
                new EffectInterpreter());
        }

        // 現プレイヤー p1 が手札 1 枚(existing)を持つセッション
        // fdpP1 で TotalPoints(p1) = fdp + ddp + sdp = fdpP1 を制御(他 DP は 0 固定)
        // 2026-05-17 SessionFactory 統合 第 3 弾:内部実装を SessionFactory.NewSession 呼び出しに置換し
        // FDP / DDP / SDP / Influences / BedDamages の dictionary 直接構築を排除した
        // (呼び出し側 API は維持、phase / fdpP1 / initialHand 引数のセマンティクスはそのまま)。
        private static DrowZzzGameSession NewSession(
            DrowZzzPhaseState phase = DrowZzzPhaseState.WaitingForDraw,
            int fdpP1 = 80,
            int initialHand = 1)
        {
            var p1HandCards = new CardId[initialHand];
            for (int i = 0; i < initialHand; i++)
            {
                p1HandCards[i] = CardId.Of(CardTypeId.Of($"existing{i + 1}"), 0);
            }
            return Stubs.SessionFactory.NewSession(
                phase: phase,
                p0Hand: new Hand(p1HandCards),
                fdp: Stubs.SessionFactory.Dp(p1: fdpP1, p2: 0));
        }

        // ===== DZ-205: IsLegalMove の合法条件(全 PhaseState + 80 以上 + 連想可能カード登録)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-205")]
        public void Given_WaitingForDraw_80以上_連想可能カード_When_IsLegalMove_Then_true()
        {
            var rule = NewRuleWithAssociatable();
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForDraw, fdpP1: 80);
            Assert.That(rule.IsLegalMove(session, new AssociateAction(DreamCardId)), Is.True);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-205")]
        public void Given_WaitingForPlay_80以上_連想可能カード_When_IsLegalMove_Then_true()
        {
            var rule = NewRuleWithAssociatable();
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForPlay, fdpP1: 80);
            Assert.That(rule.IsLegalMove(session, new AssociateAction(DreamCardId)), Is.True);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-205")]
        public void Given_WaitingForEndTurn_80以上_連想可能カード_When_IsLegalMove_Then_true()
        {
            var rule = NewRuleWithAssociatable();
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForEndTurn, fdpP1: 80);
            Assert.That(rule.IsLegalMove(session, new AssociateAction(DreamCardId)), Is.True);
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-205")]
        public void Given_TotalPoints79_When_IsLegalMove_Then_false()
        {
            // 閾値 80 未満は不可(JIT 確定 2026-05-13:「80 以上」を採用、79 で false が境界)
            var rule = NewRuleWithAssociatable();
            var session = NewSession(fdpP1: 79);
            Assert.That(rule.IsLegalMove(session, new AssociateAction(DreamCardId)), Is.False);
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-205")]
        public void Given_未登録カード_When_IsLegalMove_Then_false()
        {
            var rule = NewRuleWithEmptyCatalog();
            var session = NewSession(fdpP1: 100);
            Assert.That(rule.IsLegalMove(session, new AssociateAction(DreamCardId)), Is.False);
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-205")]
        public void Given_マーカーなしカード_When_IsLegalMove_Then_false()
        {
            // catalog 登録はあるが、AssociatableMarkerEffect が効果列に含まれない → 連想不可
            var rule = NewRuleWithNonAssociatable();
            var session = NewSession(fdpP1: 100);
            Assert.That(rule.IsLegalMove(session, new AssociateAction(DreamCardId)), Is.False);
        }

        // ===== DZ-206: Apply の状態遷移(手札 +1、PhaseState 不変、他不変)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-206")]
        public void Given_Apply完了_When_現プレイヤー手札を取得_Then_action_Cardが追加されている()
        {
            var rule = NewRuleWithAssociatable();
            var session = NewSession(fdpP1: 80, initialHand: 1);
            var next = rule.Apply(session, new AssociateAction(DreamCardId));
            Assert.That(next.GameState.Players[0].Hand.Contains(DreamCardId), Is.True);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-206")]
        public void Given_Apply完了_When_現プレイヤー手札枚数を取得_Then_1枚増えている()
        {
            // 手札 1 → 2(existing1 + dream)
            var rule = NewRuleWithAssociatable();
            var session = NewSession(fdpP1: 80, initialHand: 1);
            var next = rule.Apply(session, new AssociateAction(DreamCardId));
            Assert.That(next.GameState.Players[0].Hand.Count, Is.EqualTo(2));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-206")]
        public void Given_Apply完了_When_PhaseStateを取得_Then_現フェーズが維持される()
        {
            // 連想は割り込み式 → PhaseState は不変(WaitingForDraw のまま)
            var rule = NewRuleWithAssociatable();
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForDraw, fdpP1: 80);
            var next = rule.Apply(session, new AssociateAction(DreamCardId));
            Assert.That(next.PhaseState, Is.EqualTo(DrowZzzPhaseState.WaitingForDraw));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-206")]
        public void Given_Apply完了_When_山札を取得_Then_不変()
        {
            // 連想は catalog 直接生成 → Deck / Discard / Field は完全不変
            var rule = NewRuleWithAssociatable();
            var session = NewSession(fdpP1: 80);
            var next = rule.Apply(session, new AssociateAction(DreamCardId));
            Assert.That(next.GameState.Deck, Is.EqualTo(session.GameState.Deck));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-206")]
        public void Given_Apply完了_When_捨て札を取得_Then_不変()
        {
            var rule = NewRuleWithAssociatable();
            var session = NewSession(fdpP1: 80);
            var next = rule.Apply(session, new AssociateAction(DreamCardId));
            Assert.That(next.GameState.Discard, Is.EqualTo(session.GameState.Discard));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-206")]
        public void Given_Apply完了_When_相手プレイヤー手札を取得_Then_不変()
        {
            // 連想は現プレイヤーのみに作用 → p2 は完全不変
            var rule = NewRuleWithAssociatable();
            var session = NewSession(fdpP1: 80);
            var next = rule.Apply(session, new AssociateAction(DreamCardId));
            Assert.That(next.GameState.Players[1], Is.EqualTo(session.GameState.Players[1]));
        }

        // W-2 反映:仕様 DZ-206「他全フィールド不変」の網羅(BedDamages / SDP / Outcome)
        // 現実装は session に何も影響しないが、将来 ApplyAssociate に誤って DP 操作や BedDamages 更新を
        // 追加した場合に検出するためのリグレッションテスト。association.md の不変フィールドリストと 1:1 対応。

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-206")]
        public void Given_Apply完了_When_SecondDrowsyPointsを取得_Then_不変()
        {
            var rule = NewRuleWithAssociatable();
            var session = NewSession(fdpP1: 80);
            var next = rule.Apply(session, new AssociateAction(DreamCardId));
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(session.SecondDrowsyPoints[PlayerId.Of("p1")]));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-206")]
        public void Given_Apply完了_When_BedDamagesを取得_Then_不変()
        {
            // ベッド破損率を 0 以外で初期化したセッションで連想 Apply 後も値が変わらないことを確認
            var rule = NewRuleWithAssociatable();
            var session = NewSessionWithBedDamage(fdpP1: 80, bedP1: 40);
            var next = rule.Apply(session, new AssociateAction(DreamCardId));
            Assert.That(next.BedDamages[PlayerId.Of("p1")], Is.EqualTo(40));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-206")]
        public void Given_Apply完了_When_Outcomeを取得_Then_null維持()
        {
            // 連想は Outcome を設定しない(早期勝利は EarlyWinTriggerEffect 経路、終了判定は ApplyEndTurn 経路、ADR-0010)
            var rule = NewRuleWithAssociatable();
            var session = NewSession(fdpP1: 80);
            var next = rule.Apply(session, new AssociateAction(DreamCardId));
            Assert.That(next.Outcome, Is.Null);
        }

        // W-3 反映:DZ-205 仕様「terminated sessions は all actions illegal(ADR-0010 §6)」の検証
        // 実装上は DrowZzzRule.IsLegalMove L89 で session.IsTerminated チェックが IsLegalAssociate の手前で
        // 行われる構造的保証。本テストはその保証が連想にも適用されることを明示。

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-205")]
        public void Given_終了済session_When_IsLegalMove_Then_false()
        {
            var rule = NewRuleWithAssociatable();
            var baseSession = NewSession(fdpP1: 80);
            // p1 を勝者として確定した終了済 session(早期勝利を直接模倣、ADR-0010 §5)
            var session = baseSession with { Outcome = new WinnerOutcome(PlayerId.Of("p1")) };
            Assert.That(rule.IsLegalMove(session, new AssociateAction(DreamCardId)), Is.False);
        }

        // ベッド破損率を指定可能な NewSession 派生(W-2 BedDamages 不変テスト用)。
        // 通常 NewSession は bedP1=0 固定だが、本テスト群で 40% の状態を作るために別ヘルパーを用意。
        // 2026-05-17 SessionFactory 統合 第 3 弾。
        private static DrowZzzGameSession NewSessionWithBedDamage(int fdpP1, int bedP1) =>
            Stubs.SessionFactory.NewSession(
                phase: DrowZzzPhaseState.WaitingForDraw,
                p0Hand: new Hand(new[] { CardId.Of(CardTypeId.Of("existing1"), 0) }),
                fdp: Stubs.SessionFactory.Dp(p1: fdpP1, p2: 0),
                bedDamages: Stubs.SessionFactory.Dp(p1: bedP1, p2: 0));

        // ===== Apply 防御例外(IsLegalMove 違反時の明示)=====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-205")]
        public void Given_TotalPoints79_When_AssociateActionをApply_Then_InvalidOperationException()
        {
            var rule = NewRuleWithAssociatable();
            var session = NewSession(fdpP1: 79);
            Assert.Throws<InvalidOperationException>(() =>
                rule.Apply(session, new AssociateAction(DreamCardId)));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-205")]
        public void Given_未登録カード_When_AssociateActionをApply_Then_InvalidOperationException()
        {
            var rule = NewRuleWithEmptyCatalog();
            var session = NewSession(fdpP1: 100);
            Assert.Throws<InvalidOperationException>(() =>
                rule.Apply(session, new AssociateAction(DreamCardId)));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-205")]
        public void Given_マーカーなしカード_When_AssociateActionをApply_Then_InvalidOperationException()
        {
            var rule = NewRuleWithNonAssociatable();
            var session = NewSession(fdpP1: 100);
            Assert.Throws<InvalidOperationException>(() =>
                rule.Apply(session, new AssociateAction(DreamCardId)));
        }

        // C-1 post-Phase2 レビュー反映:対象 CardId が既に Hand に含まれている場合は IsLegalMove で弾く
        // (後段 Hand.Add の ArgumentException が呼び出し側に生 throw されないよう IsLegalMove で構造的に防ぐ)

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-205")]
        public void Given_既に手札にある連想可能カード_When_IsLegalMove_Then_false()
        {
            // Given: catalog に DreamCardId 登録 + 連想可能 + TotalPoints 100、
            //       かつ現プレイヤーの Hand に "dream#0"(= DreamCardId)を直接持たせる。
            var rule = NewRuleWithAssociatable();
            var sessionWithDreamInHand = NewSessionWithCardAlreadyInHand(
                inHand: DreamCardId,
                fdpP1: 100,
                phase: DrowZzzPhaseState.WaitingForDraw);
            // When / Then
            Assert.That(rule.IsLegalMove(sessionWithDreamInHand, new AssociateAction(DreamCardId)), Is.False);
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-205")]
        public void Given_既に手札にある連想可能カード_When_AssociateActionをApply_Then_InvalidOperationException()
        {
            // Given
            var rule = NewRuleWithAssociatable();
            var sessionWithDreamInHand = NewSessionWithCardAlreadyInHand(
                inHand: DreamCardId,
                fdpP1: 100,
                phase: DrowZzzPhaseState.WaitingForDraw);
            // When / Then: Apply は IsLegalMove false → InvalidOperationException 経路
            //              (Hand.Add の生 ArgumentException が露出しないことを構造的に保証)
            Assert.Throws<InvalidOperationException>(() =>
                rule.Apply(sessionWithDreamInHand, new AssociateAction(DreamCardId)));
        }

        // 任意 CardId を手札に含むセッションを構築するヘルパー(C-1 検証用)
        // 2026-05-17 SessionFactory 統合 第 3 弾。
        private static DrowZzzGameSession NewSessionWithCardAlreadyInHand(
            CardId inHand,
            int fdpP1,
            DrowZzzPhaseState phase) =>
            Stubs.SessionFactory.NewSession(
                phase: phase,
                p0Hand: new Hand(new[] { inHand }),
                fdp: Stubs.SessionFactory.Dp(p1: fdpP1, p2: 0));

        // ===== AssociateAction の null 防御(positional ctor / with 式 両経路)=====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-204")]
        public void Given_null_When_AssociateActionを生成_Then_ArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new AssociateAction(Card: null));
            Assert.That(ex!.ParamName, Is.EqualTo("Card"));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-204")]
        public void Given_AssociateActionにwith_Card_null_Then_ArgumentNullException()
        {
            var action = new AssociateAction(DreamCardId);
            var ex = Assert.Throws<ArgumentNullException>(() => _ = action with { Card = null });
            Assert.That(ex!.ParamName, Is.EqualTo("Card"));
        }

        // ===== DZ-256: ApplyAssociate で AssociatedCardIds に card が追加される(ADR-0019、PR ①)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-256")]
        public void Given_AssociateAction_When_ApplyAssociate_Then_AssociatedCardIdsにcardが追加される()
        {
            // Given(初期 AssociatedCardIds = 空集合)
            var rule = NewRuleWithAssociatable();
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForPlay, fdpP1: 80);
            Assume.That(session.AssociatedCardIds.Count, Is.EqualTo(0));
            // When
            var next = rule.Apply(session, new AssociateAction(DreamCardId));
            // Then(連想で引いた CardId が永続記録される)
            Assert.That(next.IsAssociated(DreamCardId), Is.True);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-256")]
        public void Given_AssociateAction_When_ApplyAssociate_Then_AssociatedCardIdsの件数が1になる()
        {
            // Given
            var rule = NewRuleWithAssociatable();
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForPlay, fdpP1: 80);
            // When
            var next = rule.Apply(session, new AssociateAction(DreamCardId));
            // Then(初期 0 → +1)
            Assert.That(next.AssociatedCardIds.Count, Is.EqualTo(1));
        }

        // ===== DZ-257: IsAssociated の O(1) lookup + null 防御 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-257")]
        public void Given_AssociatedCardIdsに含まれるcard_When_IsAssociated_Then_true()
        {
            // Given(セッション直接構築で AssociatedCardIds に DreamCardId を渡す)
            var rule = NewRuleWithAssociatable();
            var session = Stubs.SessionFactory.NewSession(
                phase: DrowZzzPhaseState.WaitingForDraw,
                associatedCardIds: new[] { DreamCardId });
            // When / Then
            Assert.That(session.IsAssociated(DreamCardId), Is.True);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-257")]
        public void Given_AssociatedCardIdsに含まれないcard_When_IsAssociated_Then_false()
        {
            // Given
            var session = Stubs.SessionFactory.NewSession(phase: DrowZzzPhaseState.WaitingForDraw);
            // When / Then(空集合 → 任意 CardId は未登録)
            Assert.That(session.IsAssociated(DreamCardId), Is.False);
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-257")]
        public void Given_null_When_IsAssociated_Then_ArgumentNullException()
        {
            // Given
            var session = Stubs.SessionFactory.NewSession(phase: DrowZzzPhaseState.WaitingForDraw);
            // When / Then
            var ex = Assert.Throws<ArgumentNullException>(() => session.IsAssociated(null));
            Assert.That(ex!.ParamName, Is.EqualTo("card"));
        }
    }
}
