# DrowZzz M1 統合シナリオ (M1-PR7)

`StartGameUseCase` → `ApplyActionUseCase` の連鎖で N=2 のターン進行ループを end-to-end で検証する統合シナリオ。M1 Definition of Done(ADR-0005 §M1)達成の証明。

## 概要

ADR-0006 §M1-PR7 の決定に基づく。M1-PR1〜PR6 で実装した部品(`StartGameUseCase` / `DrowZzzRule` / `ApplyActionUseCase` / `DrowZzzAction` 4 種 / `DrowZzzGameSession` / `DrowZzzPhaseState`)が組み合わさってターン進行ループを成立させることを確認する。各部品の単体テストは M1-PR1〜PR6 で完備済のため、本 PR では「組み合わせの正しさ」と「数ラウンド累積の正しさ」に焦点を絞る。

本 PR では以下を検証範囲外とする(ADR-0006 §6 通り):

- カード効果(M2 以降)
- 勝敗判定 / `MaxRoundNumber` (M3 以降)
- 山札枯渇(M2 以降)
- 「省略」相当の Action 種別(将来 PR / ADR)

## 普遍要件 (Ubiquitous)

(本 PR で構造的不変量は追加なし)

## 事象駆動要件 (Event-driven)

- [DZ-077] When `StartGameUseCase.Execute(players, deck)` is called with valid arguments, then the resulting session shall have `PhaseState == WaitingForDraw`、`Turn.TurnNumber == 1`、`Turn.CurrentPlayerIndex == 0`、各プレイヤーの `Hand.Count == 5`。
- [DZ-078] When `ApplyActionUseCase.Execute` is called sequentially with `DrawCardAction` → `PlayCardAction(card)` → `EndTurnAction` for one phase, then the session shall traverse `WaitingForDraw → WaitingForPlay → WaitingForEndTurn → WaitingForDraw` and `Turn.TurnNumber` shall increase by 1.
- [DZ-079] When 2 phases (= 1 turn for N=2) are played, then `Turn.TurnNumber` shall equal initial + 2 and `Turn.CurrentPlayerIndex` shall return to the starting player (= 0).
- [DZ-080] When 3 turns (= 6 phases for N=2) are played, then `Turn.TurnNumber` shall equal initial + 6, the cumulative cards in `Field` shall equal 6 (1 PlayCardAction per phase), and Players' `Hand.Count` shall equal `5 - 3 + 3 = 5` (5 initial − 3 played + 3 drawn = 5)。
- [DZ-081] When two end-to-end runs are executed with identical inputs and identical-seed `IRandomSource` instances, then both runs shall produce equal final sessions (Deterministic Replay)。

## 異常要件 (Unwanted)

(本 PR では追加なし、各部品の異常系テストは M1-PR1〜PR6 で完備済)

## 定数依存

| 定数 | 階層 | 由来 |
| ---- | ---- | ---- |
| 初期手札枚数 = 5 | L2 | `StartGameUseCase.InitialHandSize`(M1-PR3 で導入) |
| FdpPool | L3 | `StubGameConfig` (Tests/Stubs、M1-PR3 で導入) |

## Implementation Notes

- **`IdentityRandom` 採用**: 統合テストでは結果を予測可能化するため `IdentityRandom`(Tests/Stubs、M1-PR3 で導入)を主に使用。`StartGameUseCase` の Players Shuffle / FdpPool Shuffle が no-op 化され、Players 順 / FDP 割当が入力順のままになる。Deterministic Replay テスト (DZ-081) のみ `XorShiftRandom(seed)` を使う。
- **山札サイズ**: テストで 30 枚程度のダミー山札 (`CardId.Of("c1")〜("c30")`) を使う。3 ラウンド × 2 フェーズ = 6 ドロー + 5×2 = 10 初期配布 = 計 16 枚消費。30 枚あれば余裕あり。
- **Hand 削減 / Field 累積の検証**: PlayCardAction で「現プレイヤー Hand から 1 枚」を `Field` に出すため、`Hand.Count` は Draw 1 回 + Play 1 回 = ±0 で 5 維持(各フェーズ後)。`Field` は累積で増える。
- **共通ヘルパーの新設**: 統合テストで `PlayOnePhase(useCase, session)` ヘルパー(手札 Top カードを自動使用、ADR-0009 用語規約に従い旧 `PlayOneSubturn` から改名)と `PlayPhases(useCase, session, count)` ヘルパーを使ってループを簡潔化。

## 関連

- 実装(本 PR、新規追加なし、既存実装を組み合わせるテストのみ):
  - 利用: `StartGameUseCase` (M1-PR3) / `ApplyActionUseCase` (M1-PR6) / `DrowZzzRule` (M1-PR4〜PR6) / 全 Action 種別 (M1-PR2〜PR5)
- テスト(本 PR):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Integration/M1IntegrationTests.cs`
- ADR: [`docs/adr/0006-m1-detail-application-interfaces.md`](../../../adr/0006-m1-detail-application-interfaces.md) §M1-PR7
- ADR: [`docs/adr/0005-phase2-roadmap-drowzzz.md`](../../../adr/0005-phase2-roadmap-drowzzz.md) §M1 Definition of Done
- 関連: [`setup.md`](setup.md)、[`draw.md`](draw.md)、[`play.md`](play.md)、[`end-turn.md`](end-turn.md)、[`apply-action-usecase.md`](../application/apply-action-usecase.md)
- 後続: M2 着手 PR (カード効果)、M3 着手 PR (`MaxRoundNumber` + 勝敗判定)

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-077 | 1 ID 4 テスト分割: `Then_PhaseStateがWaitingForDraw` / `Then_TurnNumberが1` / `Then_CurrentPlayerIndexが0` / `Then_各プレイヤーHandが5枚`(全て `Given_有効な引数_When_StartGameUseCase_Execute_Then_...`) | StartGameUseCase 直後の 4 不変条件を統合的視点で検証(各単体は M1-PR3 でカバー済) |
| DZ-078 | 1 フェーズ完走の状態遷移を 4 アサーションに分離: `Then_Draw後にWaitingForPlay` / `Then_Play後にWaitingForEndTurn` / `Then_EndTurn後にWaitingForDraw` / `Then_TurnNumberが1増える` | 各 PhaseState 遷移を独立に検証 |
| DZ-079 | 1 ラウンド完走 (N=2 フェーズ) の状態を 2 アサーションに分離: `Then_TurnNumberが+2` / `Then_CurrentPlayerIndexがプレイヤー0に戻る` | |
| DZ-080 | 3 ラウンド完走の状態を 3 アサーションに分離: `Then_TurnNumberが+6` / `Then_FieldCountが6` / `Then_HandCountが5維持` | |
| DZ-081 | `Given_同一引数と同一seed_When_M1完走を2回実行_Then_最終セッションが等価` | Deterministic Replay |
