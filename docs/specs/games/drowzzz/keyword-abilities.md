# キーワード能力(`Keyword` enum + `KeywordedEffect` ラッパー)(M3-PR5a / 5b / 5c)

ADR-0011 §4 で確定したキーワード能力(狂乱 / 本能 / 反撃)の仕様。効果単位で 0 個以上のキーワードを付与する設計を、`KeywordedEffect(IReadOnlyList<Keyword>, IEffect)` ラッパー record で表現する。

## 概要

| 観点 | 値 |
| ---- | ---- |
| キーワード列挙 | `enum Keyword { Frenzy, Instinct, Counter }`(M3-PR5a で 3 値すべて定義、機能化は段階的)|
| 付与方式 | `KeywordedEffect(IReadOnlyList<Keyword>, IEffect)` ラッパー record(`TimeOfDayBranchEffect` / `ChoiceEffect` と同パターン)|
| 付与単位 | 効果単位(`CardData` レベル / カードレベルではない、JIT 確定 2026-05-12)|
| 拡張性 | 将来未開示キーワードで enum 末尾追加可能(JIT 確定 2026-05-12)|

## 各キーワードのセマンティクスと実装ステータス

| Keyword | セマンティクス(ADR-0011 §4) | M3-PR5a | M3-PR5b | M3-PR5c |
| ---- | ---- | ---- | ---- | ---- |
| `Frenzy`(狂乱) | 反撃を受けない | enum 値のみ宣言 | **機能化済**(`CounterAction.IsLegalMove` で Frenzy 持ち target → false、DZ-221)| — |
| `Instinct`(本能) | 手段の放棄を受け付けない | **機能化済**(`AbandonAction.IsLegalMove` で除外、DZ-213) | — | — |
| `Counter`(反撃) | 相手のカードを無効化 | enum 値のみ宣言 | **機能化済**(`CounterAction` / `PassCounterAction` / `WaitingForCounterResponse` PhaseState、DZ-214〜220)| 反撃の反撃 + 元カード遡及発動 |

## 普遍要件 (Ubiquitous)

- [DZ-209.0] [Ubiquitous] `Keyword` enum は `Drowsy.Application.Games.DrowZzz.Effects` namespace に `Frenzy` / `Instinct` / `Counter` の 3 値を持つ。declaration order は `Frenzy = 0` / `Instinct = 1` / `Counter = 2`(serialize 互換性のため後続追加は末尾)。
- [DZ-209] [Optional] When new keywords are revealed in the future via JIT communication, they shall be appended to the end of the `Keyword` enum, preserving existing values' ordinal positions for serialization compatibility(ADR-0011 §4 JIT 確定 2026-05-12 / Future Extensibility)。本要件は未来の拡張規約のため M3-PR5a 範囲ではテスト不可、`[Optional]` マーカーで明示。

## 「Instinct を含むカード」の判定ルール(M3-PR5a)

カードの効果列を再帰的に走査し、`KeywordedEffect` で `Keyword.Instinct` を含むものを探す。判定は `DrowZzzRule.IsLegalAbandon` 内の `HasInstinctKeyword(IReadOnlyList<IEffect>)` で行う:

| 効果型 | 判定 |
| ---- | ---- |
| `KeywordedEffect kw` | `kw.HasKeyword(Keyword.Instinct)` が true なら確定 true、false なら `kw.Inner` を再帰判定(nested KeywordedEffect 対応)|
| `TimeOfDayBranchEffect tod` | `tod.NightEffects` / `tod.MorningEffects` の両方を再帰判定(夜効果のみ Instinct がある「夢」カードのパターンを ADR-0011 §6 で想定)|
| `ChoiceEffect c` | 全 `c.Branches[i]` を再帰判定 |
| その他の effect(`AdjustSdpEffect` / `DrawCardEffect` / `DamageBedEffect` / `AssociatableMarkerEffect` / `EarlyWinTriggerEffect` / `ApplyInfluenceEffect` / `RemoveInfluenceEffect`)| 内部に effect を持たないため判定対象外、`false` 返却 |

## 不採用案

| 案 | 不採用理由(ADR-0011 §4 配置の判断、JIT 確定 2026-05-12)|
| ---- | ---- |
| (a) 効果 record の属性として持つ(`AdjustSdpEffect(SdpTarget, int Delta, KeywordSet Keywords)`)| 全 effect record に field 追加で影響大、キーワード不要効果にも null check が必要 |
| (c) カードレベルで持つ(`CardData.Keywords`)| 効果単位の付与とずれる(夜効果のみ「狂乱・本能」を持つ夢カードのケースで困難)|
| (d) `KeywordAttribute` enum を `CardData.Attributes` に格納 | 汎用 dict 流用で型安全性低、ADR-0007 §1 と矛盾 |

## 後続 PR との関係

- 本 PR(M3-PR5a)範囲は **`Keyword` enum + `KeywordedEffect` 基盤 + Instinct 機能化** までを実装。Frenzy / Counter は enum 値のみ宣言。
- **M3-PR5b**(Counter 機構): `CounterAction(CardId Counter, CardId Target)` + `DrowZzzPhaseState.WaitingForCounterResponse` 追加、Frenzy の「反撃を受けない」機能化(`CounterAction.IsLegalMove` で対象判定)
- **M3-PR5c**(反撃の反撃 + 遡及発動): `DrowZzzGameSession` に `PendingCounteredEffects` 追加、反撃の反撃成立時に元カード効果を遡及 Apply
- `ADR-0006`「自分のターン中のみカードプレイ可能」原則の更新は M3-PR5b で実施(本 ADR §4.3.2)

## 定数依存

なし(本機構は enum / record 定義のみで数値定数を持たない、L1〜L5 階層モデル非該当)。

## 関連

- ADR: [`docs/adr/0011-m3-dream-card-and-game-mechanics-expansion.md`](../../adr/0011-m3-dream-card-and-game-mechanics-expansion.md) §4
- 関連: [`effects/keyworded-effect.md`](effects/keyworded-effect.md)(ラッパー record の詳細仕様)/ [`abandon.md`](abandon.md)(Instinct を含むカードの CardIndex 除外)
- 後続関連: M3-PR5b / 5c で Counter / Frenzy 機能化、M3-PR6 で「夢」カード(Frenzy + Instinct の夜効果)統合
- 実装(本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/Keyword.cs`(新規)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/KeywordedEffect.cs`(新規)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/EffectInterpreter.cs`(case 追加)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzRule.cs`(`HasInstinctKeyword` ヘルパー + `IsLegalAbandon` / `ApplyAbandon` の Instinct 検証)
- テスト(本 PR):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/Effects/KeywordedEffectTests.cs`
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/AbandonActionTests.cs`(DZ-213 を追記)

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-209 | [Ubiquitous] / [Optional]:enum 構造はコンパイル時保証、`KeywordedEffectTests` の `Keyword.Frenzy / Instinct / Counter` 使用が暗黙テスト。未来拡張規約は [Optional] で test 免除 | enum 3 値 + 拡張規約 |
| DZ-210〜212 | `KeywordedEffectTests`(12 件) | 詳細トレーサビリティは [`effects/keyworded-effect.md`](effects/keyworded-effect.md) §トレーサビリティを参照 |
| DZ-213 | `AbandonActionTests`(DZ-213 行 4 件:Instinct top-level / 非 Instinct OK / Apply 例外 / TimeOfDayBranch nest / ChoiceEffect nest) | 詳細は [`abandon.md`](abandon.md) §トレーサビリティを参照 |
| DZ-214〜221 | `CounterActionTests`(M3-PR5b で導入)| 詳細は [`counter.md`](counter.md) §トレーサビリティを参照 |
