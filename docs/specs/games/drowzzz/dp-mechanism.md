# DrowZzz DP 機構 (M2-PR3 / SDP 範囲)

DrowZzz の「持ち点(眠気)」を構成する 3 種の DP(FDP / DDP / SDP)のうち、M2-PR3 で **SDP**(Second Drowsy Point、公開情報・初期値 0・行動で変動)を `DrowZzzGameSession` に導入する。

## 概要

ADR-0009 §「DP 種別」で確定した DP 構造のうち、本機能では SDP のみを扱う:

| DP 種別 | 性質 | 実装状況 |
| ---- | ---- | ---- |
| FDP | First Drowsy Point: ゲーム開始時に `IGameConfig.FdpPool` から抽選、隠し情報、不変 | M1-PR3 実装済(ADR-0006) |
| **SDP** | **Second Drowsy Point: 各プレイヤーの行動で変動、公開情報、初期値 0** | **本機能(M2-PR3)** |
| DDP | Draw Drowsy Point: 2 時間ごと(計 5 回)に共有プールから抽選、隠し情報、累積式 | M2-PR4 候補 |

ADR-0009 §「持ち点」: **持ち点 = FDP + DDP + SDP**(計算式)。本 PR (M2-PR3) 起票時点では DDP がまだ存在しなかったため `TotalPoints(PlayerId)` の実装は **FDP + SDP** で完結していたが、**M2-PR4 で DDP が追加され、`TotalPoints` は 3 項合計に拡張済**(`dp-mechanism-ddp.md` DZ-138 参照)。本仕様 §「事象駆動要件」DZ-103 も M2-PR4 で 3 項版に整合更新済。

## actor 概念 (ADR-0007 §1.4 JIT 確定事項)

効果 record が「自分」「相手」のどちらを対象に作用するかを表現するため、本 PR で `SdpTarget enum`(`Self` / `Opponent`)を導入する(ADR-0007 §1.4「他者影響系 actor 拡張」の JIT 確定、M2-PR3 で正式採用)。設計判断:

- **enum を効果 record の引数に取る**(record 数最小化、将来 DDP/FDP target 拡張時にも対応容易)
- 案 b(actor ごとに record 分離: `AddSelfSdpEffect` / `AddOpponentSdpEffect` ...)は record 数が指数関数的に膨らむため不採用
- 案 c(EffectContext を渡す)は M2 範囲では過剰、不採用

詳細は ADR-0007 §1.4 訂正反映(本 PR で起票):「最初の他者影響系効果(AdjustSdpEffect)の登場時に enum 引数方式を採用」。

## 普遍要件 (Ubiquitous)

- [DZ-099] [Ubiquitous] The `DrowZzzGameSession` shall expose a `SecondDrowsyPoints` (`IReadOnlyDictionary<PlayerId, int>`) init-only property whose key set is structurally equal to `GameState.Players.Select(p => p.Id)`.
- [DZ-105] [Ubiquitous] The `StartGameUseCase` shall initialize `SecondDrowsyPoints` to `0` for every player on game start.
- [DZ-106] [Ubiquitous] The `SdpTarget` shall be an enum with members `Self` and `Opponent`, declared in the `Drowsy.Application.Games.DrowZzz.Effects` namespace.

## 事象駆動要件 (Event-driven)

- [DZ-100] When `DrowZzzGameSession` is constructed with valid `SecondDrowsyPoints`, the property value shall be retained (defensive copy of the source dictionary).
- [DZ-103] When `DrowZzzGameSession.TotalPoints(playerId)` is computed (M2-PR4 以降 DDP 実装下), it shall return `FirstDrowsyPoints[playerId] + DrawDrowsyPoints[playerId] + SecondDrowsyPoints[playerId]`(M2-PR3 段階では DDP が未存在で 2 項合計だったが、本式は M2-PR4 で 3 項に拡張、`dp-mechanism-ddp.md` DZ-138 を参照)。
- [DZ-104] When `DrowZzzGameSession.TotalPoints(playerId)` is called with a `PlayerId` not present in the session, it shall throw `ArgumentException`.

## 異常要件 (Unwanted)

- [DZ-101] If `DrowZzzGameSession` is constructed with `null` `SecondDrowsyPoints`, then it shall throw `ArgumentNullException`.
- [DZ-102] If `DrowZzzGameSession` is constructed with `SecondDrowsyPoints` whose key set does not match `GameState.Players.Select(p => p.Id)`, then it shall throw `ArgumentException`.
- [DZ-107] If `with { SecondDrowsyPoints = null }` is applied to an existing `DrowZzzGameSession`, then it shall throw `ArgumentNullException`.
- [DZ-108] If `with { SecondDrowsyPoints = ... }` whose key set does not match the existing `GameState.Players` `PlayerId` set is applied, then it shall throw `ArgumentException`.

## 任意要件 (Optional)

- [DZ-109] [Optional] SDP values may be negative (no 0 floor): SDP は本 PR では **負値許容** とし、0 で floor しない。ADR-0009 §「戦略示唆」で「持ち点低い方が勝ち」と整合させるための判断(0 floor すると相手 SDP を継続的に下げる戦略の幅が失われる)。将来 0 floor が望ましいとの仕様変更があれば別 PR / ADR で再評価。

## 定数依存

| 定数 | 階層 | 由来 |
| ---- | ---- | ---- |
| SDP 初期値 0 | L2 | `StartGameUseCase` 内の SDP 初期化(ADR-0009 §「DP 種別」§SDP の「初期値 0」) |

`0` は CLAUDE.md §9 自明リテラル例外(`0`/`1`/`-1`/`""`/`null`)に該当するため、`<Module>Constants` には切り出さない。

## 関連

- ADR: [`docs/adr/0009-m2-m3-dp-and-victory-conditions.md`](../../../adr/0009-m2-m3-dp-and-victory-conditions.md) — DP 機構 / 持ち点 / 早期勝利の根拠
- ADR: [`docs/adr/0007-m2-detail-card-effects.md`](../../../adr/0007-m2-detail-card-effects.md) §1.4 — 他者影響系 actor 拡張の JIT 判断ポイント、本 PR で `SdpTarget` enum 方式に確定
- ADR: [`docs/adr/0006-m1-detail-application-interfaces.md`](../../../adr/0006-m1-detail-application-interfaces.md) §M1-PR3 — `FirstDrowsyPoints` 既存実装パターン(本機能の `SecondDrowsyPoints` がこれに整合)
- 実装 (本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzGameSession.cs`(`SecondDrowsyPoints` プロパティ + `TotalPoints` メソッド + Equals / GetHashCode / with 式の対応)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/StartGameUseCase.cs`(SDP 初期化)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/SdpTarget.cs`(enum)
- テスト (本 PR):
  - `DrowZzzGameSessionTests`: DZ-099 / DZ-100 / DZ-101 / DZ-102 / DZ-103 / DZ-104 / DZ-107 / DZ-108 / DZ-109
  - `StartGameUseCaseTests`: DZ-105
- シナリオ: `dp-mechanism.feature`

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-099 | (テスト免除: Ubiquitous) | `record class` の init-only プロパティ宣言で構造的に保証 |
| DZ-100 | `Given_有効なSDP_When_DrowZzzGameSessionを生成_Then_SecondDrowsyPointsが入力と一致する` | コンストラクタ値保持 |
| DZ-101 | `Given_SDPにnull_When_DrowZzzGameSessionを生成_Then_ArgumentNullExceptionを投げる` | null 防御 |
| DZ-102 | `Given_SDPのキーがPlayersと不一致_When_DrowZzzGameSessionを生成_Then_ArgumentExceptionを投げる` | cross-field 検証 |
| DZ-103 | `Given_FDP100SDP10のSession_When_TotalPointsを取得_Then_110を返す` 等(2 件) | TotalPoints 計算 |
| DZ-104 | `Given_PlayersにいないPlayerId_When_TotalPointsを取得_Then_ArgumentExceptionを投げる` | TotalPoints 異常系 |
| DZ-105 | `Given_StartGameUseCase_When_Execute_Then_全プレイヤーのSDPが0で初期化される` | 初期化検証 |
| DZ-106 | (テスト免除: Ubiquitous) | `enum SdpTarget { Self, Opponent }` 宣言で構造的に保証 |
| DZ-107 | `Given_既存Session_When_with_SDPにnull_Then_ArgumentNullExceptionを投げる` | with 式 null 防御 |
| DZ-108 | `Given_既存Session_When_with_SDPをキー不一致に変更_Then_ArgumentExceptionを投げる` | with 式 cross-field 検証 |
| DZ-109 | `Given_負のSDP値_When_DrowZzzGameSessionを生成_Then_保持される` | 0 floor なしの確認 |
