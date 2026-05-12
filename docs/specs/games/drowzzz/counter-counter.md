# 反撃の反撃(`PendingCounteredEffect` + `CounterAction` 経路 2 + 遡及発動 + 自ターン終了一括クリア)(M3-PR5c)

ADR-0011 §4.4 で確定した「反撃の反撃 + 元カード遡及発動」の仕様。反撃カード B が元カード A をカウンタしたあと、自分のターン中に反撃カード C をプレイして B を打ち消すことで、A の効果が遡及発動する機構を扱う。

## 概要

| 観点 | 値 |
| ---- | ---- |
| 新規 record | `sealed record PendingCounteredEffect(CardId CounterCard, CardId OriginalCard, IReadOnlyList<IEffect> OriginalEffects)` |
| 新規プロパティ | `DrowZzzGameSession.PendingCounteredEffects: IReadOnlyList<PendingCounteredEffect>`(空 list 初期化、ctor 9 引数 → 10 引数化)|
| `CounterAction` の経路 | 経路 1(`WaitingForCounterResponse` で B 成立、M3-PR5b 確定)/ 経路 2(自ターン `WaitingForEndTurn` で C 成立、本 PR)|
| 遡及発動の `EffectContext` | `EffectContext.Default`(元 A プレイ時の Choice / InfluenceRemovalIndex は保存していないため、N=2 想定で十分)|
| クリアタイミング | (a) C 成立で対応ペアを即削除(LIFO の最後エントリ)、(b) `EndTurnAction.Apply` で未消化分を一括破棄(自ターン終了時)|
| N=2 超対応 | 本 PR では N=2 のみ、N>2 拡張は Phase 3 候補(ADR-0011 §4.4) |

## 本 PR で確定した JIT 項目(2026-05-13)

ADR-0011 §「M3-PR5 着手時の JIT 確認項目」のうち、本 PR で 3 項目を確定:

| 項目 | 確定内容 |
| ---- | ---- |
| `PendingCounteredEffects` のデータ構造 | 新規 `sealed record PendingCounteredEffect`(3 フィールド構成、ADR §312 候補例 2 フィールドから「B 識別」用途で `CounterCard` を追加)|
| クリアタイミング | (a) C 成立でペア即削除 + (b) 自ターン終了で未消化分を一括破棄(JIT 確定 2026-05-13)|
| 「反撃の反撃」C の Action 表現 | 既存 `CounterAction` を拡張(PhaseState で経路 1 / 経路 2 を switch 分岐、Action 階層を増やさない)|

## 普遍要件 (Ubiquitous)

- [DZ-222.0] [Ubiquitous] `PendingCounteredEffect` は `Drowsy.Application.Games.DrowZzz` namespace の `sealed record` で、3 プロパティ(`CounterCard: CardId` / `OriginalCard: CardId` / `OriginalEffects: IReadOnlyList<IEffect>`)を持つ。すべて null 不可(positional ctor / `with` 式の両経路で `ArgumentNullException` で防御)。
- [DZ-223.0] [Ubiquitous] `DrowZzzGameSession.PendingCounteredEffects` は `IReadOnlyList<PendingCounteredEffect>` 型で、ctor 10 引数(`gameState`, `fdp`, `ddp`, `sdp`, `ddpPool`, `influences`, `phaseState`, `outcome`, `bedDamages`, `pendingCounteredEffects`)の末尾引数として渡される。null list / null 要素は不可、空 list は許容。Players キー集合との独立性を持つ(セッション単位の状態)。

## 構造要件(null 防御 / 防御コピー)

- [DZ-222] When `PendingCounteredEffect(counterCard, originalCard, originalEffects)` is constructed via positional ctor or assigned via `with` expression with any of `counterCard == null` / `originalCard == null` / `originalEffects == null`, `ArgumentNullException` shall be thrown(`PlayCardAction.Card` と同じ二重ガードパターン)。`originalEffects` 内に null 要素を含む場合は `ArgumentException` を thrown する(空 list は許容)。
- [DZ-223] When `DrowZzzGameSession` ctor is called with `pendingCounteredEffects == null`, `ArgumentNullException` shall be thrown. `pendingCounteredEffects` 内に null 要素を含む場合は `ArgumentException` を thrown する(空 list は許容、`PlayerInfluence` と同パターン)。内部表現は `PendingCounteredEffect[]` で防御コピーされ、`IReadOnlyList<PendingCounteredEffect>` として公開される。

## 経路 1 拡張(`ApplyCounter` での Pending 登録、M3-PR5b の挙動 + 本 PR で追加)

- [DZ-224] When `CounterAction(counter, target)` is applied in `WaitingForCounterResponse` phase(経路 1、M3-PR5b で確立), the resulting `PendingCounteredEffects` shall append a new `PendingCounteredEffect(CounterCard: counter, OriginalCard: target, OriginalEffects: catalog.GetEffects(target))` at the end of the list(LIFO で「最後に登録された B」が経路 2 の照合対象になる)。

## 合法性判定(経路 2、`IsLegalCounter`)

- [DZ-225] When `CounterAction(counter, target)` is evaluated with `session.PhaseState == WaitingForEndTurn`(経路 2、反撃の反撃 C), `IsLegalMove` shall return `true` if and only if all of the following hold:
  - `PendingCounteredEffects` 非空、かつ `PendingCounteredEffects[Count - 1].CounterCard == target`(LIFO の最後エントリ照合)
  - `counter` が現プレイヤー(`Players[CurrentPlayerIndex]`、= 元 A プレイヤー、自ターン中)の手札に存在
  - `counter` の効果列に `KeywordedEffect` で `Keyword.Counter` を含む
  - `target`(= B)の効果列に `KeywordedEffect` で `Keyword.Frenzy` を含まない(対称設計、B が Frenzy 持ちなら反撃の反撃も不可)
- `PhaseState` がそれ以外(`WaitingForDraw` / `WaitingForPlay`)の場合は `false`(経路 1 / 経路 2 のどちらにも該当しない)。

## 状態遷移(経路 2、`Apply`)

- [DZ-226] When `CounterAction(counter, target)` is applied in `WaitingForEndTurn` phase(経路 2):
  - 現プレイヤー(`Players[CurrentPlayerIndex]`)の `Hand` から `counter` を `Remove` する
  - `Discard` に `counter` を `AddTop` する
  - `Field` は変更しない(B / A はすでに経路 1 で Discard 済)
  - `PendingCounteredEffects` から最後エントリ(= 打ち消し対象 B のエントリ)を削除する
  - 削除したエントリの `OriginalEffects` を `EffectInterpreter.Apply(currentSession, effect, EffectContext.Default)` で順次評価する(A の効果遡及発動、ADR-0011 §4.4)
  - `PhaseState` は `WaitingForEndTurn` を維持(C プレイで Pending 消化、続いて `EndTurnAction` 待ち)

## クリアタイミング(`EndTurnAction.Apply`)

- [DZ-227] When `EndTurnAction` is applied(`session.PhaseState == WaitingForEndTurn` 前提), the resulting session's `PendingCounteredEffects` shall be an empty `IReadOnlyList<PendingCounteredEffect>` (= `Array.Empty<PendingCounteredEffect>()`)。これは `Turn.Next` でターン進行する **前** に行う(「このターンに残った Pending を破棄してから次ターンへ」の意味論)。`PendingCounteredEffects.Count == 0` の場合は session 不変返却(no-op、graceful)。

## 不採用案

| 案 | 不採用理由 |
| ---- | ---- |
| `PendingCounteredEffect` を value tuple `(CardId Card, IReadOnlyList<IEffect> Effects)` で表現 | `sealed record 必須` プロジェクト規約 + Equals / GetHashCode 制御性 + 名前付きフィールドの可読性で record 採用 |
| 2 フィールド `(Card, Effects)` 構成(ADR §312 候補例)| C の `Target` 照合に「B カード」が必要なため `CounterCard` を追加し 3 フィールドに拡張(JIT 確定 2026-05-12 / 本 PR 着手時に確定) |
| クリアタイミング: Round 終了時に一括クリアのみ | Round 中に Pending が滞留して N=2 想定の挙動を予測しにくい、自ターン終了で破棄が直感的 |
| 反撃の反撃を新規 `CounterCounterAction` で表現 | `CounterAction` の自然な拡張(PhaseState 分岐)で表現可能、Action 階層を増やさない、ADR §284-287「`CounterAction` という別 action」の精神を保つ |
| 自ターン中の専用 PhaseState 追加(例:`WaitingForCounterCounterResponse`) | enum 値が増え状態爆発の懸念、`WaitingForEndTurn` 内で `CounterAction` を経路 2 として合法化する設計でカバー可能 |
| 遡及発動時の `EffectContext` に元 A プレイ時の値を保存(Choice / InfluenceRemovalIndex) | 構造大幅変更、N=2 想定で context 依存の動的効果(RemoveInfluenceEffect 等)を遡及発動に巻き込むケースは想定外、`EffectContext.Default` で十分(将来必要なら ADR §4.4 拡張で context スナップショット保存を検討)|
| N=2 超対応(複数ペアの同時 Pending を一般化) | 本 PR では N=2 のみ、N>2 拡張は Phase 3 候補(ADR-0011 §4.4 / `DetermineEndOfGameOutcome` と同じ取扱い)|

## 本 PR 範囲外(M3-PR6 / 別 PR で対応)

| 項目 | 委譲先 |
| ---- | ---- |
| `KeywordedEffect.Apply` で Counter キーワード持ち effect を `PlayCardAction` 経路で skip | 別 PR(ADR-0011 §4.3.1「自ターン通常プレイで Counter 非付与の効果のみ発動」、本 PR でも未実装、暫定挙動)|
| カード No.00「夢」(Frenzy + Instinct + Counter + `EarlyWinTriggerEffect` の組み合わせ) | M3-PR6 |
| 「反撃の反撃の反撃」(N=4 段以上の連鎖) | 本 PR では Pending 最後エントリ削除 → 空 Pending で経路 2 illegal なので N=2 まで。N>2 拡張は Phase 3 候補 |

## 動作シナリオ(N=2 想定の代表ケース)

```
1. p1 ターン(WaitingForPlay)
   p1.Play(A) where A.effects = [E_A] (Counter キーワードなし、Frenzy なし)
   → A は Field 先頭に AddTop
   → E_A が EffectInterpreter で Apply
   → p2 の手札に Counter 持ちあれば PhaseState を WaitingForCounterResponse へ
   PendingCounteredEffects: [] (空)

2. p2 が反撃(WaitingForCounterResponse、経路 1)
   p2.Counter(B, target=A) where B.effects = [Counter(E_B)]
   → B → Discard、A → Discard、A は Field から消失
   → PendingCounteredEffects: [(CounterCard=B, OriginalCard=A, OriginalEffects=[E_A])]
   → PhaseState を WaitingForEndTurn へ(p1 のターン進行に戻る)

3. p1 が反撃の反撃(WaitingForEndTurn、経路 2、本 PR)
   p1.Counter(C, target=B) where C.effects = [Counter(E_C)]
   → C → Discard
   → Field 変化なし(B / A はすでに Discard)
   → PendingCounteredEffects: [] (最後エントリ削除)
   → E_A が EffectInterpreter で Apply(A の効果遡及発動、ADR-0011 §4.4)
   → PhaseState は WaitingForEndTurn 維持

4. p1 が EndTurn(WaitingForEndTurn → WaitingForDraw、ターン進行)
   → PendingCounteredEffects を空 list 上書き(no-op、すでに空、本 PR DZ-227)
   → Turn.Next で p2 のターンへ
```

## 定数依存

なし(本機構は record / プロパティ / 判定ロジック / 状態遷移のみで数値定数を持たない)。

## 関連

- ADR: [`docs/adr/0011-m3-dream-card-and-game-mechanics-expansion.md`](../../adr/0011-m3-dream-card-and-game-mechanics-expansion.md) §4.4(反撃の反撃 + 遡及発動)/ §「M3-PR5 着手時の JIT 確認項目」
- 先行関連: [`counter.md`](counter.md)(反撃機構の経路 1、M3-PR5b)/ [`keyword-abilities.md`](keyword-abilities.md)(キーワード能力全体)
- 後続関連: M3-PR6 で「夢」カード(Frenzy + Instinct + Counter + `EarlyWinTriggerEffect` の組み合わせ)統合
- 実装(本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/PendingCounteredEffect.cs`(新規 sealed record)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzGameSession.cs`(`PendingCounteredEffects` プロパティ + ctor 10 引数化 + Equals / GetHashCode 拡張)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzRule.cs`(`ApplyCounterAsCounter` で Pending 登録 / `IsLegalCounterAsCounterCounter` / `ApplyCounterAsCounterCounter` 新規 / `ApplyEndTurn` で Pending クリア)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/StartGameUseCase.cs`(空 list で初期化)
- テスト(本 PR):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/CounterCounterTests.cs`(新規、経路 2 + 遡及発動 + Pending クリアの検証)
  - 既存 78 箇所の `new DrowZzzGameSession` を機械挿入で 10 引数化(`DrowZzzGameSessionTests` / `CounterActionTests` 等)

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-222 | `PendingCounteredEffect` の null 防御 3 件(CounterCard / OriginalCard / OriginalEffects null)+ null 要素検出 1 件 + [Ubiquitous] は構造的保証 | record 構造 |
| DZ-223 | `DrowZzzGameSession` の `pendingCounteredEffects: null` で `ArgumentNullException` + null 要素検出 + 空 list 許容 + [Ubiquitous] は構造的保証 | Session プロパティ |
| DZ-224 | 経路 1 Apply 後の `PendingCounteredEffects` に `(B, A, A の効果列)` 1 件追加 | Pending 登録 |
| DZ-225 | 経路 2 IsLegalMove 関連 5 件(true / Pending 空 false / 最後エントリ不一致 false / Counter 手札になし false / Frenzy target false) | 合法性。`WaitingForPlay` 等の経路 1 / 経路 2 以外の PhaseState での false は DZ-218(`CounterActionTests.Given_WaitingForPlay_When_CounterActionのIsLegalMove_Then_false`)で暗黙カバー(W-3 反映 2026-05-12)|
| DZ-226 | 経路 2 Apply 関連 4 件(Counter 手札除去 + Discard 追加 / Pending 最後削除 / OriginalEffects 遡及発動 / PhaseState WaitingForEndTurn 維持) | 状態遷移 |
| DZ-227 | EndTurnAction.Apply 後の `PendingCounteredEffects` が空(Pending あり → 空 / Pending 空 → no-op) | クリア |
