# EffectInterpreter

`Drowsy.Application.Games.DrowZzz.Effects.EffectInterpreter` の振る舞いを記述する。
配置先: `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/EffectInterpreter.cs`

要件トレーサビリティ ID 規約は [`docs/testing-strategy.md`](../../testing-strategy.md) §4 を参照。

---

## 概要

`EffectInterpreter` は DrowZzz のカード効果(`IEffect` 派生型)を `DrowZzzGameSession` に適用する純関数を提供する。`DrowZzzRule.PlayCardAction.Apply` から呼び出され、`ICardCatalog<IEffect>.GetEffects(CardId)` で取得した効果列を左から順に逐次評価する(ADR-0007 §1.3 / §3)。

M2-PR1 段階では具体的な `IEffect` 派生型は未追加(`IEffect` は空 marker)であり、`Apply` は防御例外のみを扱う最小骨格として導入する。M2-PR2 以降で個別効果 record(例: `DrawCardsEffect`)が追加されるごとに switch case を拡張する(ADR-0007 §6 PR 分割粒度)。

## 普遍要件 (Ubiquitous)

- [APP-031] [Ubiquitous] The `EffectInterpreter` shall be a sealed class in the `Drowsy.Application.Games.DrowZzz.Effects` namespace.
- [APP-032] [Ubiquitous] The `EffectInterpreter.Apply` shall be a pure function: same `(session, effect)` inputs produce the same `DrowZzzGameSession` output, no mutation of inputs, no I/O side effects.

## 事象駆動要件 (Event-driven)

- [APP-033] When `Apply` is called with `session = null`, the `EffectInterpreter` shall throw `ArgumentNullException` whose `ParamName` is `"session"`.
- [APP-034] When `Apply` is called with `effect = null`, the `EffectInterpreter` shall throw `ArgumentNullException` whose `ParamName` is `"effect"`.
- [APP-035] When `Apply` is called with an `IEffect` derived type that is not matched by any switch case (i.e. an unknown / future effect type), the `EffectInterpreter` shall throw `NotImplementedException` whose `Message` includes the runtime type name of the offending effect.

## 定数依存

なし(本機能は L1 / L2 / L3 / L4 の定数に依存しない)。

## 関連

- [ADR-0007 §1.3 EffectInterpreter](../../adr/0007-m2-detail-card-effects.md) — 設計判断、`_` ケース例外型の選択根拠(`NotImplementedException` で `DrowZzzRule._` ケースと整合)、namespace 配置根拠
- [ADR-0006 §M1 進行中の学び](../../adr/0006-m1-detail-application-interfaces.md) — `_` ケースカバレッジ確保のための `UnknownXxx` ダミー型パターン(本仕様 [APP-035] のテストで `UnknownEffect` を採用する経緯)
- `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/IEffect.cs` — Effect 階層のマーカー interface
- `Assets/_Project/Scripts/Tests/Application.Tests/Stubs/UnknownEffect.cs` — [APP-035] テスト用の dummy 派生型
- 後続: `docs/specs/games/drowzzz/effect-mechanism.md`(DZ-082〜、M2-PR1 後半の `DrowZzzRule.PlayCardAction.Apply` 実装と同時に新設予定)
