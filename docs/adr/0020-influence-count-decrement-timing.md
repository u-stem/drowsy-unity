# ADR-0020: Influence の RemainingCount 減算タイミングを EndTurn へ移行 — カウント1 Marker 機能化

- Status: Accepted
- Date: 2026-05-17
- Decider: -

---

## Context

カード No.09「強引過ぎる一手」(オーナー JIT 確定 2026-05-17)が以下の仕様を持つ:

- 乙(相手)に **カウント1 の継続影響**「**存在時:手段の使用や放棄をすることができない**」を付与する
- 「使用」は `PlayCardAction` / `CounterAction` の両方を含む(オーナー JIT 確認、2026-05-17)
- 「放棄」は `AbandonAction` を指す
- `AssociateAction` / `EndTurnAction` は影響対象外(進行不能化を避けるため)

これは「`IsLegalMove` で**保有時**に効果を発揮する Marker 系 Influence」のカウント1 適用が、現行 Tick 仕様(M2-PR5、ADR-0007 §1.5 確立)では **構造的に機能しない**問題を顕在化させた。

### 現行 Tick 仕様の構造的問題

`DrowZzzRule.TickInfluencesForCurrentPlayer`(`Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzRule.cs:1394` 周辺、M2-PR5 確立)は **1 メソッド内で以下 2 操作を一括実行**:

1. 影響保有者の自フェーズ開始時に `TickEffect` を `EffectInterpreter` で適用
2. 直後に `RemainingCount -= 1` を計算、0 到達なら list から除去

この順序により、カウント1 の Marker 系 Influence(`TickEffect` が no-op、`IsLegalMove` で「存在時」効果を発揮)は以下の経路をたどる:

```
[Player A の EndTurnAction で Marker Influence を Player B に count=1 付与]
  → A の EndTurn 終了
[Player B のフェーズ b 開始時]
  → ベッド破損計算(B の Influences を「Has..Influence」で walk、ここでは marker 検出可)
  → Tick: TickEffect 適用(no-op) → count 1→0 → 除去
[Player B のフェーズ b 中]
  → IsLegalMove で B の Influences を walk しても **Marker は除去済み** → 効果機能せず
[Player B の EndTurnAction]
  → 何の制限もなく実行可能
```

結果として「カウント1 の Marker 系 Influence は **付与した瞬間〜次の自フェーズ開始時の Tick 直前まで** しか保有されず、IsLegalMove チェックの瞬間にはすでに消えている」という意味論になる。これは EARS で記述する「カウント N = N 回機能」のうち、**Marker 系における N=1 のみ機能しないエッジケース** を構造的に内包する設計上のバグ。

### 既存テストでの固定化

`DZ-285`(untouchable-realm.md)は「`Given_p2が2xInfluenceカウント1保有_p1current_When_p1EndTurnでp2フェーズへ_Then_p2のInfluences件数が0`」と、本問題を**仕様として固定化**してしまっていた。No.06「牙の届かぬ領域」はカウント 4 で導入されたため、カウント 1 境界が「除去のみ確認」で済んでしまい、機能性の検証経路が存在しなかった。

### TickEffect 型(SDP -5 等の能動効果)では発覚しない理由

No.02「緑の侵攻」のような能動 TickEffect 型(`AdjustSdpEffect(Self, -5)`)は、Tick で 1 回適用された後に除去されるため、カウント1 でも 1 回機能する(`DZ-177` テスト経路)。Marker 系のように「保有していること自体に意味」を持つ Influence のみ、本問題に直面する。

## Decision

`PlayerInfluence.RemainingCount` の **-1 操作を `ApplyEndTurn` 内(`Turn.Next` 前)に移動** する。Tick(`TickInfluencesForCurrentPlayer`)は `TickEffect` 適用のみに縮小する。

### 1. 新しい順序

`DrowZzzRule.ApplyEndTurn` の処理順序を以下に変更:

```
1. PendingCounteredEffects クリア(既存、ADR-0011 §4.4)
2. DecrementInfluencesForCurrentPlayer(旧 current = 自フェーズ終了プレイヤーの Influences すべて count -1、0 で除去)  ← 新規追加
3. Turn.Next(新 current = 次プレイヤーへ rotate)— 既存
4. ApplyBedDamageToCurrentPlayer(新 current のベッド破損 SDP マイナス、ADR-0011 §3)
5. DrawDdpForAllPlayers(該当ラウンドのみ、ADR-0009 §4)
6. TickInfluencesForCurrentPlayer(新 current の TickEffect 適用のみ、count 不変)  ← count -1 削除
7. Round 21 完了検出 → Outcome 設定(ADR-0010 §4)
```

### 2. カウント N の意味論

| カウント | 新仕様での発動回数 / 機能フェーズ数 | 旧仕様との差 |
| ---- | ---- | ---- |
| 1 | **次の自フェーズ全体で 1 回機能**(b 開始時 Tick で TickEffect 適用、b 中ずっと保有、b 終了時 -1 で除去) | TickEffect 型: 不変(1 回 Tick)。**Marker 型: 0 回 → 1 回(改善)** |
| 2 | 次の自フェーズ + その次の自フェーズ(2 回機能) | 不変 |
| 3 | 自フェーズ 3 回 | 不変 |
| N | 自フェーズ N 回 | 不変 |

カウント 2 以上は発動回数が変わらない(b 開始時 Tick + b 終了時 -1、c 開始時 Tick + c 終了時 -1、… を N 回繰り返して 0 で除去)。

### 3. 影響を受ける既存カードと挙動

| カード | カウント | 種別 | 新仕様での挙動 |
| ---- | ---- | ---- | ---- |
| No.02「緑の侵攻」 | 3 | TickEffect 型(SDP -5/Tick) | 不変(3 回 Tick) |
| No.04「静寂を纏う」 | 2 | Marker 型(`IsLegalPlayCard` で `RestrictSpecificCardInfluenceEffect` 参照) | 不変(b 中 + c 中で機能、count=2 維持) |
| No.06「牙の届かぬ領域」 | 4 | Marker 型(`ApplyBedDamageToCurrentPlayer` で `Has...Influence` 参照) | 不変(BedDamage 計算は Tick 直前で count ≥ 1 のため機能) |
| No.07「知恵の及ばぬ領域」 | 4 | Marker 型(No.08 使用禁止) | 不変 |
| No.08「廻るための知恵」 | 永続(`int.MaxValue` 相当の超長 count)| Marker 型(ベッド破損 SDP 反転) | 不変 |
| No.00「夢」連想後使用制限 | 1 | Marker 型(`UsageRestrictionMarkerEffect`) | **改善**:現状は「次の自フェーズ Tick で即除去 → b 中チェック不可」だったが、b 中ずっと保有して機能 |
| **No.09「強引過ぎる一手」(本 ADR で追加)** | **1** | **Marker 型**(`RestrictAllUsageAndAbandonInfluenceMarkerEffect`) | **新規(本仕様変更が前提)** |

### 4. ADR-0011 §5「順序保証」との関係

ADR-0011 §5 で確立した順序「ベッド破損 → DDP 抽選 → 影響 Tick → Outcome 設定」は **本 ADR で部分更新**:

- 「影響 Tick」の意味を **「TickEffect 適用のみ」** に限定(count -1 を含まない)
- `Turn.Next` 前に **「現プレイヤー(旧 current)の Influences count -1」** ステップを追加
- ADR-0011 §5 を Supersede せず、本 ADR で **拡張更新**(ADR-0006 / ADR-0011 が ADR-0007 / ADR-0010 を扱った様式と同じ)

### 5. ADR-0007 §1.5「継続影響(Influence)」との関係

ADR-0007 §1.5 で確立した **「発動回数ベース、0 到達で除去」** の意味論は **不変**。本 ADR は「N 回機能」を保つために減算タイミングのみ変更する(意味論レベルでは「カウント N = 自フェーズ N 回機能」が初めて Marker 型でも成立する)。

## Consequences

### Positive

- カウント1 の Marker 系 Influence が初めて正しく機能(No.09 含む将来カードに不可欠)
- 「カウント N = 自フェーズ N 回機能」が **全 Influence 種別(TickEffect 型 / Marker 型)で統一**
- ApplyEndTurn の処理順序が「副作用の主体プレイヤー」で明示的に分離(decrement は旧 current、tick / bed damage / DDP / outcome は新 current)、可読性向上
- 既存 EARS「DZ-176 `count=3 → count=2`」のような中間状態 assertion は新仕様で「count はまだ 3」となるが、**最終的な発動回数 / 除去タイミングは不変** のため戦略的影響なし

### Negative

- **既存テスト 6 件以上の中間状態 assertion 更新**(DZ-176 / DZ-177(タイミング)/ DZ-283 / DZ-285 / GoodForBody DZ-216 / RealmBeyondWisdom DZ-289 等)
- `DZ-285` の「カウント1 → 0 除去境界」テストは本 ADR 後は **「カウント1 が 1 フェーズ機能してから除去される」** に意味反転(テスト名・assertion を再設計)
- 既存 EARS / .feature の Tick 説明文 5 ファイル + ADR-0007 §1.5 / ADR-0011 §5 順序保証の更新が必要

### Neutral

- 既存 SO catalog .asset / Persistence schema(`PersistedSessionV1`)に影響なし(`Influences` の構造は不変、減算タイミングのみが行動評価ロジック内で変わる)
- `PlayerInfluence` record 自体は不変(`RemainingCount` の不変条件 `≥ 1` 維持、0 到達時除去の暗黙契約も不変)

## Implementation Notes

### No.00「夢」連想後使用制限への副作用(code-reviewer S-1 反映 2026-05-17)

ADR-0020 後、No.00「夢」連想時に付与される `PlayerInfluence(OwnPhaseStart, UsageRestrictionMarkerEffect, 1)` の除去タイミングが「翌自フェーズ開始時の Tick 直後」から「翌自フェーズ終了時の `EndTurnAction` 冒頭の Decrement」に移動する。

**戦略的影響は無し**:プレイヤー体験として「次に Card "00" を使えるようになる自フェーズ」は両仕様とも `N+2` フェーズ目(連想直後のフェーズが N、次の自フェーズが N+2、N=2 ホットシート想定)で変わらない。除去されるタイミングだけが旧仕様の「N+2 フェーズ開始直後」から新仕様の「N フェーズ終了時 = N+1 フェーズ開始前の状態保持」に移動する形となり、Influence の保有期間が結果的に 1 フェーズ短くなるが、「N+2 フェーズ目で合法」という最終結果は同じ。

DZ-232 の要件文言は本 PR で更新済(`00-dream.md`)。No.04 / No.06 / No.07 / No.08 など count ≥ 2 のカードは「カウント数 = 自フェーズ機能回数」が両仕様で完全一致するため副作用なし。

### 既存テスト更新内訳

本 PR で更新された既存テスト件数の内訳:
- `GreenInvasionCardTests`:DZ-176 第 2 / DZ-177 / DZ-178 の 3 件更新 + DZ-179 新規追加(計 4 件、Decrement 経路の独立検証を新設)
- `UntouchableRealmCardTests`:DZ-283 第 2 / DZ-285 の 2 件更新
- `SoundOfSilenceCardTests`:DZ-268 の 1 件更新(意味反転:カウント1 除去 → カウント1 残存)
- `GoodForBodyCardTests`:DZ-253 永続影響テストのうち 3 件更新(`Perpetual - 1` の由来が Tick → Decrement に変わる)
- `DreamCardTests`:DZ-232 のコメントのみ整合更新(`Influence なし` を直接構築するアプローチのため assertion は不変)

合計:更新 9 件 + 新規 1 件 = 10 件のテスト変更。

## Alternatives Considered

### 不採用案 A: Tick の **内部順序** を逆転(`count -1 → 0 ならスキップ、>= 1 なら TickEffect 適用`)

```
[b 開始時 Tick]
  → count 1→0 → スキップ → 除去
  → TickEffect 適用なし
```

| 観点 | 評価 |
| ---- | ---- |
| 既存テスト影響 | △ 中間状態 assertion(count=N-1)はそのまま、ただし「カウント1 で 1 回も機能しない」になり No.09 仕様を満たせない |
| カウント1 の意味 | ✗ 「付与直後消える」というさらに非直感的な意味になる |
| TickEffect 型カードへの影響 | ✗ カウント 1 でも一度も Tick されない、`DZ-177` の意味が変わる(`SDP-5` が一度も適用されない) |

→ No.09 仕様を満たせず TickEffect 型カードを破壊する、不採用。

### 不採用案 B: Marker 系 Influence のみ別ライフサイクル(`MarkerInfluence` 派生型導入)

`PlayerInfluence` の派生型として `MarkerInfluence` を新設、カウント減算ロジックを派生型ごとに分岐。

| 観点 | 評価 |
| ---- | ---- |
| 型階層複雑化 | ✗ `PlayerInfluence` を単一 record で保つ ADR-0006 / ADR-0007 設計と乖離 |
| Persistence 影響 | ✗ JsonConverter で派生型 discriminator が追加で必要、INF-095〜133 テスト群への波及 |
| 設計の正当性 | △ 「Marker と TickEffect は別概念」という分類自体は妥当だが、本 ADR の問題解決のために型階層を増やすのは過剰 |
| `IsLegalMove` ロジック | △ 派生型判定が増えるだけで構造的解決にならない(本質はタイミングの問題) |

→ 型階層複雑化に対する利得が小さい、不採用。

### 不採用案 C: Marker 系 Influence のみ、`TickInfluencesForCurrentPlayer` の **count -1 をスキップ**

```csharp
if (inf.TickEffect is IInfluenceMarkerEffect) {
    rebuilt.Add(inf); // count 維持、Marker は別途除去する
    continue;
}
// 既存 TickEffect 適用 + count -1 → 0 で除去
```

| 観点 | 評価 |
| ---- | ---- |
| 「別途除去」の仕組み | ✗ Marker をいつ除去するかが結局未解決(b 終了時? → 本 ADR と同じ結論)|
| 型タグの導入コスト | ✗ `IInfluenceMarkerEffect` インターフェース新設 + 5 既存 Marker effect の修飾 + Persistence 影響 |
| 設計の明示性 | ✗ 「Marker は特別扱い」という暗黙ルールがコード散在、Tick 仕様の意味論が分裂 |

→ 結局 b 終了時 -1 が必要になり、本 ADR の決定案に収束する。

## Related

- カード仕様: [`docs/specs/games/drowzzz/cards/force-play.md`](../specs/games/drowzzz/cards/force-play.md)(本 PR で同時起票、No.09「強引過ぎる一手」本体実装)
- ADR-0007 §1.5「継続影響(Influence)」: 発動回数ベース、0 到達で除去の意味論を確立(本 ADR で減算タイミングのみ更新、意味論は不変)
- ADR-0009 §4「DDP 抽選」: ターン境界での副作用ステップ確立
- ADR-0010 §4「順序保証」: フェーズ進行内の副作用順序確立、Round 21 終了検出
- ADR-0011 §5「順序保証」: ベッド破損 → DDP 抽選 → 影響 Tick → Outcome 設定の順序、本 ADR §1 で部分更新
- M2-PR5 確立 Tick 機構: `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzRule.cs` `TickInfluencesForCurrentPlayer` / `ApplyEndTurn`
- 既存 EARS 更新対象: `docs/specs/games/drowzzz/cards/green-invasion.md` DZ-176〜178 / `untouchable-realm.md` DZ-283〜285 / `sound-of-silence.md` / `realm-beyond-wisdom.md` / `circulating-wisdom.md` / `good-for-body.md` / `00-dream.md`(連想後使用制限)
