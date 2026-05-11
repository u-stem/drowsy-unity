# DrowZzz カード効果メカニズム (M2-PR1)

`DrowZzzRule.PlayCardAction.Apply` 中の効果発動メカニズムと、`DrowZzzRule` の `ICardCatalog<IEffect>` / `EffectInterpreter` 依存契約を記述する。M2-PR1 段階では効果 0 個での M1 互換動作のみを扱い、具体的な効果 record(`DrawCardsEffect` 等)の追加は M2-PR2 以降の別 PR で 1 PR = 1 record の粒度で進める。

要件トレーサビリティ ID 規約は [`docs/testing-strategy.md`](../../testing-strategy.md) §4 を参照。

---

## 概要

ADR-0007 §3 / §1 の決定に基づく。M1-PR5 で実装した `PlayCardAction.Apply` の「手札 → Field 移動 + `PhaseState = WaitingForEndTurn`」の末尾に、catalog から取得した効果列 (`IReadOnlyList<IEffect>`) を `EffectInterpreter.Apply` で **左から順に逐次評価** する処理を追加する。

設計上のポイント:

- 効果は `ICardCatalog<IEffect>.GetEffects(action.Card)` で取得(catalog が効果の所有者)
- 評価は `effects.Aggregate(afterPlay, (s, e) => _interpreter.Apply(s, e))` で左から順(Linq の `Aggregate` overload で初期値あり)
- 効果 0 個なら `afterPlay` がそのまま返り、M1-PR5 と完全互換になる(M2-PR1 段階の保証)
- 効果は `GameState` を変更可能だが、M2 範囲では `PhaseState` を変えない原則(変える効果は M2 範囲外、ADR-0007 §3 / §8)
- `DrowZzzRule` constructor は `(ICardCatalog<IEffect>, EffectInterpreter)` の 2 引数を要求し、いずれも null で `ArgumentNullException`(ADR-0007 §3 「DrowZzzRule の依存追加」)

`StartGameUseCase` / `ApplyActionUseCase` の挙動は本仕様の影響を受けない:

- `StartGameUseCase` は constructor の `ICardCatalog` → `ICardCatalog<IEffect>` への型引数変更のみ受け、本体実装は不変(ADR-0007 §3 「設計上の割り切り」)
- `ApplyActionUseCase` は `DrowZzzRule` を constructor injection するため、間接的に依存型が拡張されるのみ

## 普遍要件 (Ubiquitous)

- [DZ-082] [Ubiquitous] The `DrowZzzRule` shall require `ICardCatalog<IEffect>` and `EffectInterpreter` via constructor injection.
- [DZ-085] [Ubiquitous] When `DrowZzzRule.Apply(session, PlayCardAction(card))` is called with an effect list `[e1, e2, ..., en]`, the effects shall be applied left-to-right via `Aggregate(afterPlay, (s, e) => _interpreter.Apply(s, e))` (`e1` applied to `afterPlay`, `e2` applied to the result, …)。
   `EffectInterpreter` が `sealed` のため M2-PR1 段階で order 検証用 spy を差し込めない。実 effect record が登場する M2-PR2 で本要件は「最初の effect が記録 / 反映され、続く effect が累積する」観測可能な挙動として再検証する。本 PR では `Aggregate(seed, fn)` という構造で「左から順」が保証されている。
- [DZ-086] [Ubiquitous] During M2 scope, no `IEffect` derived type shall alter `PhaseState`(effects modify `GameState` only)。M2-PR2 以降で PhaseState を変える効果が登場した場合は本要件を見直し、ADR-0007 §3 後継として別 ADR を起票する。

## 事象駆動要件 (Event-driven)

- [DZ-083] When `DrowZzzRule.Apply(session, PlayCardAction(card))` is called and `IsLegalMove` returns `true`, then `_catalog.GetEffects(card)` shall be invoked exactly once to obtain the effect list for the played card.
- [DZ-084] When `DrowZzzRule.Apply(session, PlayCardAction(card))` is called and the effect list returned by `_catalog.GetEffects(card)` is empty, then `_interpreter.Apply` shall not be invoked and the method shall complete without throwing (M2-PR1 全カード共通の M1 完全互換挙動)。

## 異常要件 (Unwanted)

- [DZ-087] If `new DrowZzzRule(null, interpreter)` is called, then it shall throw `ArgumentNullException` whose `ParamName` is `"catalog"`.
- [DZ-088] If `new DrowZzzRule(catalog, null)` is called, then it shall throw `ArgumentNullException` whose `ParamName` is `"interpreter"`.

## 定数依存

なし(本機能は L1 / L2 / L3 / L4 の定数に依存しない)。

## Implementation Notes

- **Aggregate の初期値**: `effects.Aggregate(afterPlay, ...)` の `afterPlay` は M1-PR5 互換の中間セッション。空列で Aggregate を呼ぶと初期値がそのまま返るため、M2-PR1 段階の `InMemoryCardCatalog.GetEffects` が常に空列を返す設計と組み合わさり、M2-PR1 完成時点でも実挙動が M1-PR5 と完全に一致する。
- **constructor null 防御**: M1-PR4 で確立した「すべての引数 null を `ArgumentNullException` で弾く」原則を constructor にも適用(ADR-0006 §M1 進行中の学び §「null 防御の二重ガード」を参考に、bind-only フィールドへの代入で `?? throw` 1 段ガード)。
- **`StartGameUseCase` への影響範囲**: 型引数変更のみで Execute 本体ロジックは不変(ADR-0007 §3 「設計上の割り切り」)。`StartGameUseCase` から `ICardCatalog` 依存削除可否は将来別 PR / 別 ADR(`docs/todo.md` の既存 TODO エントリ)。

## 関連

- 実装(本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzRule.cs`(constructor 2 引数 / `ApplyPlayCard` 末尾の効果評価)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/StartGameUseCase.cs`(型引数変更のみ)
  - `Assets/_Project/Scripts/Application/Catalog/InMemoryCardCatalog.cs`(`GetEffects` 実装、常に空列)
  - `Assets/_Project/Scripts/Application/ICardCatalog.cs`(`ICardCatalog<TEffect>` ジェネリック化)
- テスト(本 PR):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/DrowZzzRuleTests.cs`(DZ-083 / DZ-084 / DZ-087 / DZ-088 を追加。DZ-082 / DZ-085 / DZ-086 は [Ubiquitous] でテスト免除)
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/EffectInterpreterTests.cs`(APP-031〜APP-035、既存)
  - `Assets/_Project/Scripts/Tests/Application.Tests/Catalog/InMemoryCardCatalogTests.cs`(APP-037 / APP-038、`GetEffects` の空列契約)
- ADR: [`docs/adr/0007-m2-detail-card-effects.md`](../../../adr/0007-m2-detail-card-effects.md) §1 / §2 / §3 / §5 / §6
- 関連 EARS: [`effect-interpreter.md`](../../application/effect-interpreter.md)(APP-031〜APP-035) / [`card-catalog.md`](../../application/card-catalog.md)(APP-036) / [`in-memory-card-catalog.md`](../../application/in-memory-card-catalog.md)(APP-037 / APP-038)
- 関連: [`play.md`](play.md)(M1-PR5 で実装した `PlayCardAction` の本体仕様) / [`integration.md`](integration.md)(M1 統合シナリオ)

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-082 | (テスト免除: Ubiquitous) | `DrowZzzRule` の constructor signature で構造的に保証(挙動は DZ-087 / DZ-088 で検証) |
| DZ-083 | `Given_合法状態_When_PlayCardActionをApply_Then_catalog_GetEffectsが1回呼ばれる` | spy catalog で 1 回呼ばれること(および引数 CardId)を記録 / 検証 |
| DZ-084 | `Given_効果空のcatalog_When_PlayCardActionをApply_Then_例外を投げずに完走する` | 効果 0 個では Interpreter.Apply が呼ばれず NotImplementedException も出ないことを確認 |
| DZ-085 | (テスト免除: Ubiquitous) | `Aggregate(seed, fn)` の構造で左から順が保証される。M2-PR2 で実 effect record 登場時に観測可能な挙動として再検証 |
| DZ-086 | (テスト免除: Ubiquitous) | M2 範囲設計原則の表明。M2-PR2 以降で違反が発生した場合は本 ID と ADR-0007 §3 を改訂 |
| DZ-087 | `Given_catalogにnull_When_DrowZzzRule生成_Then_ArgumentNullException_ParamName_catalog_を投げる` | |
| DZ-088 | `Given_interpreterにnull_When_DrowZzzRule生成_Then_ArgumentNullException_ParamName_interpreter_を投げる` | |
