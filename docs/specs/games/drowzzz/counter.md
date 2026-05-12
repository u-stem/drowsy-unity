# 反撃機構(`CounterAction` + `PassCounterAction` + `WaitingForCounterResponse`)(M3-PR5b)

ADR-0011 §4.3 / §4.5 で確定した「反撃」キーワード機構の仕様。相手プレイヤーが直前にプレイしたカードに対し、本プレイヤーが Counter キーワード持ち効果列のカードを使って反撃(無効化)する。

## 概要

| 観点 | 値 |
| ---- | ---- |
| 新規 PhaseState | `DrowZzzPhaseState.WaitingForCounterResponse`(enum 末尾追加、serialize 互換性確保)|
| 反撃 Action | `CounterAction(CardId Counter, CardId Target)` |
| パス Action | `PassCounterAction`(WaitingForCounterResponse で「反撃しない」明示宣言)|
| 効果無効化 | (C) target カードを捨て札(Discard)へ(JIT 確定 2026-05-13)|
| Frenzy vs Counter | illegal-move で不可(JIT 確定 2026-05-13)|
| 反撃機構の実装方式 | (i) `WaitingForCounterResponse` PhaseState 追加(JIT 確定 2026-05-13)|

## 本 PR で確定した JIT 項目(2026-05-13)

ADR-0011 §「M3-PR5 着手時の JIT 確認項目」のうち、本 PR で 3 項目を確定:

| 項目 | 確定内容 |
| ---- | ---- |
| 効果無効化のセマンティクス | **(C) target カードを捨て札へ**(プレイ済だが効果列は走らず、カードは Discard に移動)|
| 「狂乱を反撃で打ち消そう」とした場合 | **illegal-move で不可**(`CounterAction.IsLegalMove` で false、Frenzy 持ち target に反撃はできない)|
| 相手ターン中の反撃プレイ機構 | **(i) `WaitingForCounterResponse` PhaseState 追加**(初期推奨案を採用)|

## 普遍要件 (Ubiquitous)

- [DZ-214.0] [Ubiquitous] `DrowZzzPhaseState.WaitingForCounterResponse` は `Drowsy.Application.Games.DrowZzz` namespace の enum 末尾値として宣言される。既存値(`WaitingForDraw` / `WaitingForPlay` / `WaitingForEndTurn`)の順序は維持される。
- [DZ-216.0] [Ubiquitous] `CounterAction` は `sealed record` で `(CardId Counter, CardId Target)` を持ち、`DrowZzzAction` の派生型である。両プロパティは null 不可(positional ctor / `with` 式の両経路で `ArgumentNullException` で防御)。
- [DZ-217.0] [Ubiquitous] `PassCounterAction` は `sealed record` で、フィールドなしの marker action である。`DrowZzzAction` の派生型。

## 状態遷移要件

- [DZ-215] When `PlayCardAction` is applied successfully(effect 評価後の `PhaseState` が `WaitingForEndTurn`), the rule shall check the opponent player's hand for any card whose effect list contains `KeywordedEffect` with `Keyword.Counter`(再帰判定、`DrowZzzRule.HasCounterCardInHand`)。If found, the resulting session's `PhaseState` shall be `WaitingForCounterResponse`. Otherwise, `WaitingForEndTurn` is preserved(既存テスト破壊回避)。
  - 但し、`session.IsTerminated == true`(`EarlyWinTriggerEffect` 等で勝利確定済)の場合は遷移しない

## 構造要件(null 防御)

- [DZ-216] When `CounterAction(counter, target)` is constructed via positional ctor or assigned via `with` expression with `counter == null` or `target == null`, `ArgumentNullException` shall be thrown(`PlayCardAction.Card` と同じ二重ガードパターン)。

## 合法性判定(`IsLegalMove`)

- [DZ-218] When `CounterAction(counter, target)` is evaluated, `IsLegalMove` shall return `true` if and only if all of the following hold:
  - `session.PhaseState == WaitingForCounterResponse`
  - 反撃側プレイヤー(N=2 で `currentPlayerIndex` の相手側、`counterPlayerIndex = 1 - currentIndex`)の手札に `counter` が含まれる
  - `counter` の効果列(catalog 経由)に `KeywordedEffect` で `Keyword.Counter` を含む(再帰判定)。catalog 未登録の `counter` カードは `GetEffects` が空列を返すため Counter キーワード判定が `false` となり、本条件は不成立(P-4 反映:`ICardCatalog.GetEffects` の「未登録 = 空列」設計を本仕様で利用)
  - `session.GameState.Field.Cards[0]` が `target` と一致(直近プレイされた Field 先頭、ADR-0006 §M1-PR5 補足 AddTop)
  - `target` の効果列に `Keyword.Frenzy` を含む `KeywordedEffect` がない(DZ-221、JIT 確定 2026-05-13)
- [DZ-220] `PassCounterAction.IsLegalMove` shall return `true` iff `session.PhaseState == WaitingForCounterResponse`. Otherwise `false`.
- [DZ-221] Frenzy 持ち target に対する `CounterAction.IsLegalMove` は **false**(illegal-move、`InvalidOperationException` で Apply 防御も対称)。

## 状態遷移(`Apply`)

- [DZ-219] When `CounterAction(counter, target)` is applied:
  - 反撃側プレイヤーの `Hand` から `counter` を `Remove`
  - `Field.Draw()` で先頭 `target` を取り出す(`Pile.Remove(_)` API なし、`IsLegalCounter` で Field[0] == target 検証済のため Draw は安全)
  - `Discard` に `target` → `counter` の順で `AddTop`(Discard.Cards[0] = counter / Cards[1] = target)
  - `PhaseState` を `WaitingForEndTurn` に遷移
  - 元カード `target` の効果は **既に Apply 済**(本 PR では「効果無効化」は Field → Discard 移動と effect 履歴の遡及はせず、effect 状態変化は遡って戻さない)。ADR-0011 §4.4 の「反撃の反撃 + 元カード遡及発動」は M3-PR5c で `PendingCounteredEffects` 経由で実装予定。
- [DZ-220] When `PassCounterAction` is applied, only `PhaseState` is transitioned to `WaitingForEndTurn`. All other state(`Hand` / `Field` / `Discard` / DP / 影響 / Outcome / BedDamages)は不変。

## 不採用案

| 案 | 不採用理由 |
| ---- | ---- |
| 効果無効化 (A) Field に残したまま効果列のみ skip | プレイ済が Field に居続けて捨て札規律が複雑化、JIT 確定で C 採用 |
| 効果無効化 (B) target を手札に戻す | プレイ自体を取り消し、「無効化」より「キャンセル」に近い軽いヒット振り返し、JIT 確定で C 採用 |
| Frenzy vs Counter で「合法だが no-op」 | プレイ可能だが target 不変は UX が複雑、JIT 確定で illegal-move 採用 |
| (ii) `CounterableEffectStack` プロパティで Session 拡張(MTG 風スタック)| 構造変更大、N=2 想定の本ゲームでは (i) で十分 |
| (iii) PhaseState を変えず `CounterAction.IsLegalMove` 独自合法条件 | 合法条件が複雑化、`WaitingForCounterResponse` でフェーズ明示化が筋 |

## 本 PR 範囲外(M3-PR5c / 別 PR で対応)

| 項目 | 委譲先 |
| ---- | ---- |
| `KeywordedEffect.Apply` で Counter キーワード持ち effect を PlayCardAction 経路で skip | 別 PR(ADR-0011 §4.3.1「自ターン通常プレイで Counter 非付与の効果のみ発動」、本 PR では Counter 付き効果も通常 Apply される暫定挙動)|
| 「反撃の反撃」+ 元カード A の効果遡及発動 | **M3-PR5c**(`DrowZzzGameSession` に `PendingCounteredEffects` 追加、ADR-0011 §4.4)|
| ADR-0006「自分のターン中のみカードプレイ可能」原則の更新形式(別 ADR? 補足追記?)| M3-PR5b 完成記録 PR で正式化 |
| `AssociateAction.IsLegalMove` で `WaitingForCounterResponse` を許可するか | **本 PR で確定:不可**(`WaitingForCounterResponse` は相手ターン中の反撃応答フェーズ、ADR-0011 §1 / ADR-0006「自ターン中のみ」原則の延長で連想も不可、W-2 反映 2026-05-13)。`DrowZzzRule.IsLegalAssociate` の排他リストで自ターン 3 値のみ許可する明示記述により設計意図を保存 |

## 「反撃」キーワード(`Keyword.Counter`)の機能化

M3-PR5a で `Keyword` enum に `Counter` 値を導入したが、機能化は本 PR(M3-PR5b)で実施:
- `CounterAction.IsLegalMove` で「反撃側カードの効果列に `Counter` 持ち」を検証(DZ-218)
- `DrowZzzRule.HasCounterCardInHand` で「相手手札の Counter 持ち」を検出し、PlayCardAction 後の PhaseState 分岐に利用(DZ-215)
- 「反撃を受けない」属性としての `Frenzy` も本 PR で機能化(DZ-221)

## 定数依存

なし(本機構は PhaseState / Action / 判定ロジックのみで数値定数を持たない)。

## 関連

- ADR: [`docs/adr/0011-m3-dream-card-and-game-mechanics-expansion.md`](../../adr/0011-m3-dream-card-and-game-mechanics-expansion.md) §4.3 / §4.5
- 関連: [`keyword-abilities.md`](keyword-abilities.md)(キーワード能力全体)/ [`abandon.md`](abandon.md)(Instinct と同様の Keyword 機能化の先行 PR)
- 後続関連: M3-PR5c で「反撃の反撃」+ 元カード遡及発動、M3-PR6 で「夢」カード(Frenzy + Instinct + Counter の組み合わせ)統合
- 実装(本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzPhaseState.cs`(`WaitingForCounterResponse` 追加)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzAction.cs`(`CounterAction` / `PassCounterAction` 追加)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzRule.cs`(`IsLegalCounter` / `ApplyCounter` / `ApplyPassCounter` / `HasCounterCardInHand` / `HasKeywordInEffects` 汎用化)
- テスト(本 PR): `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/CounterActionTests.cs`

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-214 | [Ubiquitous]:PhaseState enum 値はコンパイル時保証、各テストで `WaitingForCounterResponse` を直接利用 | enum 値追加 |
| DZ-215 | PlayCardAction 後分岐 2 件(相手手札に Counter 持ち / なし)| PhaseState 分岐 |
| DZ-216 | CounterAction null 防御 2 件(Counter / Target null)| 二重ガード |
| DZ-217 | [Ubiquitous]:PassCounterAction の sealed record は構造的保証、各 IsLegalMove / Apply テストで利用 | marker action |
| DZ-218 | IsLegalMove 関連 4 件(true / WaitingForPlay false / 手札なし false / Field 不一致 false)| 合法性 |
| DZ-219 | Apply 関連 4 件(Field 空 / Discard 追加 / 手札除去 / PhaseState 遷移)| 状態遷移 |
| DZ-220 | PassCounterAction IsLegalMove / Apply 関連 4 件 | スキップ動作 |
| DZ-221 | Frenzy 持ち target に対する IsLegalMove false 1 件 | Frenzy vs Counter |
