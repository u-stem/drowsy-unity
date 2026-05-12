# 連想機構(`AssociateAction`)(M3-PR4)

ADR-0011 §1 で確定した「連想(特殊ドロー)」機構の仕様。通常の山札 Draw とは別の経路で、`ICardCatalog` から直接手札にカードを追加する汎用ドロー機構(「夢」専用ではなく、後続カードも共通利用する設計)。

## 概要

| 観点 | 値 |
| ---- | ---- |
| Action | `AssociateAction(CardId Card)` |
| 合法フェーズ | `WaitingForDraw` / `WaitingForPlay` / `WaitingForEndTurn`(自ターン中のいずれか)|
| 必要条件 | 現プレイヤーの `TotalPoints` ≥ 80 + `action.Card` が連想可能カード(`AssociatableMarkerEffect` を効果列に持つ)|
| カードの取得元 | `ICardCatalog`(初期山札に含まれず、catalog 経由で直接生成)|
| Apply 後フェーズ | 不変(現フェーズ維持、割り込み式)|
| Apply 後の他状態 | `Deck` / `Discard` / `Field` / `DDP` / `SDP` / `BedDamages` / `Influences` / `Outcome` すべて不変 |

## 本 PR で確定した JIT 項目(2026-05-13)

ADR-0011 §1 起票時点では「JIT 確認待ち」だった 4 項目のうち、3 項目を本 PR で確定する(残 1 項目「連想可能カードの判別方式」は ADR-0011 §1 起票時から (i) マーカー effect 方式が初期推奨として確定済 → 本 PR で採用):

| 項目 | 確定内容 |
| ---- | ---- |
| 連想対象の領域 | **(c)/(d) `ICardCatalog` から直接生成 / 別 Pile**。連想可能カードは初期山札に含まれず、catalog 経由のみで手札に追加される(山札 / 捨て札 / 場 / 影響 / DP / Outcome / ベッド破損は不変) |
| FDS 境界 | **80 以上**(80+ で発動可、`DrowZzzAssociationConstants.AssociationThreshold = 80`)。「FDS」は `TotalPoints`(= FDP + DDP + SDP)の用語規約 |
| 連想タイミング | **自ターン中のみ**(`WaitingForDraw` / `WaitingForPlay` / `WaitingForEndTurn` の 3 種すべてで合法、ADR-0006「自分のターン中のみカードプレイ可能」原則を破壊しない) |
| 連想可能カードの判別 | **(i) マーカー effect 方式**(`AssociatableMarkerEffect` を効果列に持つカードのみ)。ADR-0011 §1 起票時の「初期推奨」を本 PR で採用 |

## 普遍要件 (Ubiquitous)

- [DZ-204.0] [Ubiquitous] `AssociateAction` は `sealed record` で `(CardId Card)` を持ち、`DrowZzzAction` の派生型である。

## 構造要件(null 防御)

- [DZ-204] When `AssociateAction(card)` is constructed via positional ctor or assigned via `with` expression with `card == null`, `ArgumentNullException` shall be thrown(`PlayCardAction.Card` と同じ二重ガードパターン)。

## 合法性判定(`IsLegalMove`)

- [DZ-205] When `AssociateAction(card)` is evaluated, `IsLegalMove` shall return `true` if and only if all of the following hold:
  - `session.PhaseState` is one of `WaitingForDraw` / `WaitingForPlay` / `WaitingForEndTurn`
  - `session.TotalPoints[currentPlayer] >= DrowZzzAssociationConstants.AssociationThreshold`(= 80)
  - `card` is registered in the rule's `ICardCatalog`
  - The effects of `card`(via `catalog.GetEffects(card)`)contain at least one `AssociatableMarkerEffect`
- Otherwise `false`. Furthermore, terminated sessions(`session.IsTerminated == true`)render all actions illegal(ADR-0010 §6 / M3-PR1)。

## 状態遷移(`Apply`)

- [DZ-206] When `AssociateAction(card)` is applied, the current player's `Hand` shall gain `card` via `Hand.Add(card)`. The `PhaseState` and every other field of the session (`Deck`, `Discard`, `Field`, `Turn`, `DDP`, `SDP`, `DdpPool`, `Influences`, `BedDamages`, `Outcome`, and the opponent's `PlayerState`) shall remain unchanged.

## 不採用案

| 案 | 不採用理由 |
| ---- | ---- |
| `DrawCardAction(bool Associate = false)` で通常 Draw と統合 | 通常ドローと特殊ドローの semantic が混在、`IsLegalMove` 条件が複雑化(ADR-0011 §1)|
| `DrowZzzPhaseState` に `WaitingForAssociation` を追加 | 連想は **割り込み式**(現フェーズ維持)で「待ち状態」フェーズ化と合わない(ADR-0011 §1)|
| `AssociateCardEffect`(効果 record)として実装 | 連想はプレイヤー宣言の action、効果列(カードプレイ副作用)とは別軸(ADR-0011 §1)|
| 「夢」専用 `AssociateDreamAction` | 連想は「夢以外にも登場する」と JIT 確定(ADR-0011 §1)、汎用機構として設計 |
| 連想領域 (a) 山札 top から / (b) 山札の特定位置 | 通常 Draw と区別がつきにくい / 山札全件探索のコスト、(c)/(d) 採用で「特殊な手段」を表現 |
| FDS 境界「81 以上(80 超)」 | JIT 確定 2026-05-13 で「80 以上」を採用、境界の混在(`> 80`(81+) vs `>= 80`(80+))を「以上」側に確定 |

## 後続 PR との関係

- 本 PR(M3-PR4)範囲は **「カード追加」までを実装**。連想で引いたカードに「次の自分のターン以降使用可能」(ADR-0011 §6)制約は M3-PR6(夢カード統合)で別途設計する(`PlayerInfluence` 流用 or 専用フィールドの選択は M3-PR6 JIT 確認)。
- 「本能(Instinct)」キーワードによる連想の制約は ADR-0011 §4 / M3-PR5 で実装される予定。本 PR 範囲では制限なし(キーワード能力機構自体が未実装)。
- 最初の連想可能カードの仕様は本 PR では確定しない。M2-PR6+ で JIT 共有される運用(ADR-0011 §1 Neutral / ADR-0010 §「Implementation Notes」と同パターン)。

## 定数依存

- `DrowZzzAssociationConstants.AssociationThreshold = 80`(L2、本 PR で新設)

## 関連

- ADR: [`docs/adr/0011-m3-dream-card-and-game-mechanics-expansion.md`](../../adr/0011-m3-dream-card-and-game-mechanics-expansion.md) §1
- 関連: [`effects/associatable-marker-effect.md`](effects/associatable-marker-effect.md)(連想可能マーカー effect)/ [`abandon.md`](abandon.md)(放棄機構)/ M3-PR6(「夢」カード統合 + 使用制限機構)
- 実装(本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzAction.cs`(`AssociateAction` 追加)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzRule.cs`(`IsLegalAssociate` / `ApplyAssociate`)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzAssociationConstants.cs`(新規)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/AssociatableMarkerEffect.cs`(新規)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/EffectInterpreter.cs`(case 追加、no-op)
- テスト(本 PR):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/AssociateActionTests.cs`
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/Effects/AssociatableMarkerEffectTests.cs`

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-204 | `AssociateAction` null 防御 2 件(positional ctor / `with` 式) | sealed record の null 二重ガード |
| DZ-205 | `IsLegalMove` 関連 6 件(3 PhaseState × true / 79 false / 未登録 false / マーカーなし false)+ 終了済 session 1 件 + Apply 防御例外 3 件 | 合法性 |
| DZ-206 | Apply 関連 9 件(手札に追加 / 枚数 +1 / PhaseState 不変 / Deck 不変 / Discard 不変 / 相手プレイヤー不変 / SDP 不変 / BedDamages 不変 / Outcome null 維持) | 状態遷移 + 他フィールド網羅 |
