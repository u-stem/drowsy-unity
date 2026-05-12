# ADR-0011: M3 詳細(拡張)— 「夢」カード + ゲームメカニクス拡張(連想 / 放棄 / ベッド破損 / キーワード能力 / ターン構造詳細化)

| 項目 | 値 |
| ---- | ---- |
| Status | Accepted |
| Date | 2026-05-12 |
| Decider | プロジェクトオーナー |

## Context

ADR-0010 で M3 詳細(ゲーム終了判定 + 勝者決定 + 早期勝利 + 引き分け仕様)を確定し、M3-PR1 で `IGameRule.IsTerminated` / `GetWinner` + `GameOutcome` + `EarlyWinTriggerEffect` 機構を実装した(PR #42、merged `a1bf50c`)。

M3-PR1 完了時点で、`EarlyWinTriggerEffect` を効果列に持つ最初のカード(就寝カード)が **未実装** の状態にあった。ADR-0010 §「Implementation Notes」では「最初の利用カード(就寝カード No.X)は本 ADR では確定しない:カード仕様は M2-PR6+ で JIT 共有される運用」と保留されていた。

**2026-05-12 のプロジェクトオーナー JIT 共有** で、就寝カード(後述「夢」カード)の仕様を共有した際、付随する **6 つの新規ゲームメカニクス** が提示された:

1. **「連想」**(特殊ドロー)— 通常の山札 Draw とは別の方法で手段を引く機構
2. **「放棄」**(代替ターン行動)— 手段を使う代わりに、手段を捨てて SDP +5 または ベッド +20% 修繕を選択
3. **ベッド破損率**(Session 状態)— 0〜100% の破損率、自ターン開始時に SDP マイナス、行動による破損
4. **キーワード能力**(狂乱 / 本能 / 反撃)— 効果に付与する属性、効果のキャンセル機構
5. **ターン構造の詳細化** — 自ターン開始時にベッドダメージ → ドロー → 行動 → 副作用 → ターン終了
6. **カード No.00「夢」** — 連想で引く + 次の自分のターン以降使用可能 + 「夜・狂乱・本能」で FDS ≥ 100 なら勝利、「朝」で自分 SDP -80

これらは「就寝カード 1 枚の仕様」ではなく **ゲームシステム全体に複数の新規概念を導入する大きな拡張** で、ADR-0010 / ADR-0006 が暗黙に前提としていたターン構造・Action 階層・効果 record の枠組みを更新する必要がある。本 ADR は **この拡張をどう M3 範囲に取り込み、PR 分割でどう段階的に実装するか** を確定する。

### ADR-0010 との関係(Supersede ではなく拡張)

ADR-0010 が確定した基盤(`IGameRule.IsTerminated` / `GetWinner` / `GameOutcome` / Session.Outcome / `EarlyWinTriggerEffect` マーカー record)は **本 ADR でもそのまま利用** する。本 ADR は ADR-0010 §5 で確定した `EarlyWinTriggerEffect` の発動条件を:

- **旧(ADR-0010 §5)**: 夜(`Clock.IsNight`)+ 持ち点 ≥ 100 でカードプレイ即時発火
- **新(本 ADR §7)**: 「夢」カードを **連想で引いて** + **次の自分のターン以降** + 「**夜・狂乱・本能**」効果として **FDS ≥ 100** で発火

に拡張する。`EarlyWinTriggerEffect` 自体の役割(`Outcome = WinnerOutcome` を設定するトリガー)は変えない。ADR-0010 §5 の発動条件 §は本 ADR §7 で詳細化する形で更新する(ADR-0010 を `Superseded by` にはしない、運用パターンは ADR-0007 §1.4 → M2-PR3 JIT 確定 / ADR-0007 §1.5 → M2-PR5 JIT 確定 と同じ)。

### プロジェクトオーナーから JIT 共有された前提(2026-05-12 時点、原文に近い記述)

| 概念 | 仕様(JIT 共有原文の整理)|
| ---- | ---- |
| **連想** | FDS が 80 を超えた時、宣言をしたなら手段を連想する。連想とは特殊な方法で手段を引くことを指す |
| **狂乱** | 「反撃」を受けない効果 |
| **本能** | 手段の放棄を受け付けない効果 |
| **反撃** | **相手のターン中** に発動し、相手が使ったカードを無効化する。**自分が反撃された時、自分のターン中に反撃を使うことで「反撃の反撃」が可能**(JIT 確定 2026-05-12)|
| **キーワード能力の付与単位** | **効果単位**(単体 / 組み合わせ自由、JIT 確定 2026-05-12)。`Keyword` enum は **将来未開示の能力で拡張される前提**(JIT 共有)|
| **放棄** | プレイヤーはターン中、手段を使うか、放棄するか選択。放棄時:手段を捨てて SDP +5、または ベッドを 20% 修繕、のどちらかを選択 |
| **ベッド破損率** | 0〜100%。破損ダメージとして毎ターン SDP マイナス、**5% につき -1**(`bedDamage / 5` の整数除算、JIT 確定 2026-05-12)|
| **ターン構造** | 自ターン開始時:ベッド破損 SDP マイナス → 手段 1 枚引く → 行動選択 → 行動副作用(ベッドダメージ含む)→ ターン終了。ターン終了までの間に「夢」を連想可能 |
| **「夢」使用条件** | 次の自分のターン以降でしか使用できない、かつ FDS が 100 以上 |
| **No.00「夢」** | 連想で引く / 「夜・狂乱・本能」で FDS が 100 を超えているならゲーム勝利 / 「朝」-80 / ±0 |

### JIT 共有時点では未確定の項目(本 ADR で「JIT 確認待ち」と明示)

ADR-0011 起票時点で **方向性は確定するが詳細未確定** の項目を以下に集約する。各機構の §「JIT 確認待ち」サブセクションで個別に整理し、対応する実装 PR(M3-PR2 以降)着手時に確定する運用とする(ADR-0007 §1.4 / §1.5 で「最初の登場時に JIT 確定」とした運用と同じ)。

| 機構 | 未確定の詳細 |
| ---- | ---- |
| 連想 | 連想対象の領域(山札 top / 特定位置 / 別領域)、FDS 80「超」(81+)か「以上」(80+)か、連想タイミング(自ターン / 相手ターン)、「連想可能カード」の判別方式(マーカー effect (i) を初期推奨)|
| 反撃 | 「反撃」を「反撃」で打ち消した時の元カードの扱い、効果スタック順、相手ターン中のカードプレイ機構の有無 |
| 本能 | 「放棄を受け付けない」の正確な意味(放棄選択画面でこのカードを捨てられない? 別の意味?)|
| ベッド破損計算 | ダメージ式 / 増加トリガー / 5 の倍数制約はすべて **JIT 確定済**(本 ADR §3、`bedDamage / 5` で SDP マイナス、特定カード固有 Percent で増加、常に 5 の倍数)|
| キーワード能力 | 付与単位(効果単位 / カード単位 / カードの効果列内のエントリ単位)|
| ターン構造 | 連想タイミングの「ターン中いつでも」の境界、行動副作用としてのベッドダメージの発生条件 |
| 「夢」数値 | 連想条件「FDS 80 超」と使用条件「FDS 100 以上」と発火条件「FDS 100 以上」の境界・以下/以上の混在意図 |
| 朝「夢」効果 | 「-80 / ±0」は自分 SDP -80 / 相手 ±0 と解釈(M2-PR3 / M2-PR5 の SDP 表記慣例)、確認必須 |

## Decision

### 1. 連想(特殊ドロー)機構

#### 採用

新規 `IGameAction` 派生型 **`AssociateAction(CardId Card)`** を追加する。`DrowZzzRule.Apply` で評価し、指定 `CardId` を **特殊な経路** で現プレイヤーの手札に追加する。

#### 連想は「夢」専用ではなく汎用ドロー機構(JIT 確定 2026-05-12)

プロジェクトオーナー JIT 共有(2026-05-12)で「**連想とは特殊な方法で手段を引くことであり、これから夢以外にも登場する**」と確定。本機構は「夢」だけの専用機構ではなく、**今後追加される複数のカードが共通利用する汎用ドロー機構** として設計する。

これに伴い、「連想可能なカード」の判別機構が必要になる。判別方式の候補(JIT 確認待ち、最初の連想可能カード(夢以外)が登場した時点で確定):

| 案 | 評価 |
| ---- | ---- |
| (i) 連想可能カードに特定マーカー effect(`IAssociatableMarker : IEffect` 等)を効果列に持たせ、`ICardCatalog.GetEffects` でマーカー存在を判定 | 「就寝カード = `EarlyWinTriggerEffect` を効果列に持つ」(ADR-0010 §5)と同パターン、最も整合 |
| (ii) `CardData.Attributes` の特定キー(`"associatable" = 1` 等)で判別 | `Attributes` の汎用 dict 流用で型安全性低、ADR-0007 §1 / ADR-0010 §5 の「カード分類は effect record で表現」方針と矛盾 |
| (iii) `ICardCatalog` に専用 API(`bool IsAssociatable(CardId)` / `IReadOnlyList<CardId> GetAssociatableCards()`)を追加 | catalog responsibility が肥大化、効果単位の表現と非対称 |

**初期推奨**: (i) マーカー effect 方式。例:`sealed record AssociatableMarkerEffect : IEffect`(フィールドなし、`EarlyWinTriggerEffect` と同様のマーカー的役割)を効果列に持つカードが「連想可能」。

#### 発動条件(本 ADR で確定)

- 現プレイヤーの `TotalPoints` が **FDS 80 を超える**(JIT 確認待ち:「超える」= 81+ か 80+ か)
- プレイヤーが action 経由で宣言

#### `IsLegalMove` での合法性

`AssociateAction` は以下のすべてを満たすときのみ `true`:

- `PhaseState` は `WaitingForDraw` / `WaitingForPlay` / `WaitingForEndTurn` のいずれか(ターン中いつでも可能)
- 現プレイヤーの `TotalPoints >= 81`(JIT 確認次第で `>= 80` に訂正)
- **`action.Card` が「連想可能なカード」として登録されている**(上記マーカー方式 (i) 等で判別、M3 範囲では「夢」が最初の利用、後続カードも追加される予定)

#### 連想対象の領域(JIT 確認待ち)

| 候補 | 評価 |
| ---- | ---- |
| (a) 山札 top から | 通常 Draw と同じで「特殊」感が薄い |
| (b) 山札の特定位置(指定カードを探す)| 山札を全件探索、本当に「連想」概念に合致 |
| (c) 山札の外の特別領域(catalog / 別 Pile)| カードが山札に含まれない設計、連想専用カードを汎用的に扱える |
| (d) `ICardCatalog` から直接生成 | 連想可能カードが初期山札に含まれず、連想で初登場(M2-PR3〜PR5 で使った in-memory catalog を流用)|

**初期推奨**: (c) または (d)。**連想で引かれるカードは通常山札に紛れない方が「特殊な手段」という JIT 共有原文と整合し、複数の連想可能カードが今後追加される拡張性も担保**。M3-PR(連想実装)着手時に JIT 確定。

#### 不採用案

| 案 | 不採用理由 |
| ---- | ---- |
| `DrawCardAction` を拡張(`DrawCardAction(bool Associate = false)`) | 通常ドローと特殊ドローの semantic が混在、`IsLegalMove` 条件が複雑化 |
| `DrowZzzPhaseState` に `WaitingForAssociation` を追加 | フェーズは「待ち状態」、連想は **ターン中いつでも可能** で割り込み式のためフェーズ化と合わない |
| 効果 record(`AssociateCardEffect`)として実装 | プレイヤーが宣言する action なので、効果 record(カードプレイの副作用)とは別の軸 |
| 「夢」専用機構として実装(`AssociateDreamAction` 等の固有 Action)| **JIT 確定 2026-05-12 で「連想は夢以外にも登場する」と確定**、汎用機構として設計する必要あり |

### 2. 放棄(代替ターン行動)機構

#### 採用

新規 `IGameAction` 派生型 **`AbandonAction(AbandonChoice Choice)`** を追加。`AbandonChoice` は **`enum AbandonChoice { GainSdp, RepairBed }`** で、放棄選択時のサブアクションを表す。

#### 放棄の選択肢

| 選択 | 効果 |
| ---- | ---- |
| `AbandonChoice.GainSdp` | 現プレイヤーの SDP +5、手札からカードを 1 枚捨てる(JIT 確認待ち:具体的にどのカードを捨てるか、プレイヤー選択 / ランダム)|
| `AbandonChoice.RepairBed` | 現プレイヤーのベッド破損率 -20%(下限 0%)、手札からカードを 1 枚捨てる(同上)|

#### `IsLegalMove` での合法性

- `PhaseState == WaitingForPlay`(`PlayCardAction` と同じフェーズで選択可能、ADR-0006 §M1-PR5 で確定したフェーズ構造を維持)
- 手札に 1 枚以上のカードがある(捨てる対象が必要)

#### 「本能」属性との関係

「本能」を持つカードは **放棄選択時に捨てる対象として選べない**(JIT 共有「手段の放棄を受け付けない」の解釈、JIT 確認待ち)。

#### 不採用案

| 案 | 不採用理由 |
| ---- | ---- |
| `PlayCardAction` を拡張(`PlayCardAction(CardId, AbandonOption)`)| 「プレイ」と「放棄」は概念的に別行動、Action 階層分離が筋 |
| 放棄選択を 2 step に分割(`AbandonAction` → `ChooseGainOrRepairAction`)| Action 階層が肥大化、1 ターン 1 行動原則と矛盾、選択肢を action 構築時引数で十分 |
| `AbandonChoice` を string / int で表現 | 型安全性が下がる、enum で十分 |

### 3. ベッド破損率(Session 状態)

#### 採用

`DrowZzzGameSession` に **`BedDamages: IReadOnlyDictionary<PlayerId, int>`** を新規プロパティとして追加(プレイヤーごとのベッド破損率、0〜100%)。

#### 配置の判断

| 案 | 採用判断 |
| ---- | ---- |
| プレイヤー間共有 1 つのベッド | 不採用。各プレイヤーが「自分のベッド」を修繕する仕様と整合しない |
| **プレイヤーごとに保持(Dictionary<PlayerId, int>)** | **採用**。FDP / DDP / SDP / Influences と同パターン、cross-field 検証(キー集合 = Players)|
| Domain `PlayerState` に `BedDamage` フィールド | 不採用。DrowZzz 固有概念で Domain ゲーム非依存原則(ADR-0002)と矛盾 |

#### 自ターン開始時の SDP マイナス計算(JIT 確定 2026-05-12)

プロジェクトオーナー JIT 確認(2026-05-12)で「**5% につき -1**」と確定。実装は **整数除算** で表現:

```csharp
// DrowZzzBedConstants(新規 constants 集約クラスを M3-PR2 で新設、§9 と同パターン)
public const int BedDamageRatePerSdp = 5;  // 5% につき SDP -1

// 自ターン開始時の計算(M3-PR2 で実装、ApplyEndTurn 順序保証の先頭)
int damage = session.BedDamages[currentPlayer] / DrowZzzBedConstants.BedDamageRatePerSdp;
// SDP[currentPlayer] -= damage を適用
```

破損率と SDP マイナスの対応(整数除算で確定):

| `BedDamage` | SDP マイナス | 計算 |
| ---- | ---- | ---- |
| 0% | 0 | `0 / 5 = 0` |
| 5% | -1 | `5 / 5 = 1` |
| 20% | -4 | `20 / 5 = 4` |
| 40% | -8 | `40 / 5 = 8` |
| 100% | -20 | `100 / 5 = 20` |

破損率の増減幅は **常に 5 の倍数**(JIT 確定 2026-05-12、§3「破損率増加トリガー」と整合)で設計されるため、整数除算は数学的に常に綺麗に割れる(切り捨ては発生しない)。ただし防御的に整数除算で実装する(5 の倍数以外が将来 JIT 共有された場合の保険として、`bedDamage / 5` の挙動が変わらないため)。

#### 不採用案(計算式)

| 案 | 不採用理由 |
| ---- | ---- |
| (b) 累積式(20% で -4、40% で -12、60% で -24)| JIT 確定の「5% につき -1」と矛盾 |
| (c) 別の非線形式 | 同上 |

#### 破損率増加トリガー(JIT 確定 2026-05-12)

プロジェクトオーナー JIT 確認(2026-05-12)で「**特定のカードによって破損率は増加する。パーセンテージはカード固有だが、常に 5 の倍数**」と確定。実装方針:

- 新規効果 record **`DamageBedEffect(SdpTarget Target, int Percent)`** を Application 層に追加
- `Percent` は **5 の倍数のみ許容**(positional ctor で `Percent % 5 != 0` を `ArgumentException` で防御、`Percent > 0` を期待)
- 破損率は **0〜100% でクランプ**(`Math.Min(100, current + Percent)`、上限 100%)
- 評価は `EffectInterpreter.Apply` で:`session.BedDamages[targetId]` を `Percent` 分増加(`PlayerInfluence` の付与パターンと同じく、DP 群と同様の dict 更新)

```csharp
// Application/Games/DrowZzz/Effects/DamageBedEffect.cs(M3-PR2 で新設)
public sealed record DamageBedEffect(SdpTarget Target, int Percent) : IEffect
{
    // positional ctor で 5 の倍数 / 正値検証(record 二重ガード未対応の値型 int は positional のみ)
    public int Percent { get; init; } = Percent % DrowZzzBedConstants.BedDamageRatePerSdp == 0 && Percent > 0
        ? Percent
        : throw new ArgumentException(
            $"DamageBedEffect.Percent は {DrowZzzBedConstants.BedDamageRatePerSdp} の正の倍数である必要があります: {Percent}",
            nameof(Percent));
}
```

#### 不採用案(破損率増加トリガー)

| 案 | 不採用理由 |
| ---- | ---- |
| 候補 B(放棄時に破損)| JIT 確定「特定のカードによって」と矛盾、放棄選択は独立した行動 |
| 候補 C(一律ターン経過で破損)| JIT 確定「カード固有値で増加」と矛盾、自動進行ではない |
| `DamageBedEffect(SdpTarget, int Percent)` の `Percent` を任意 int で受ける | JIT 確定「常に 5 の倍数」を record 検証で強制する方が筋(CLAUDE.md §9 マジックナンバー禁止、`DrowZzzBedConstants.BedDamageRatePerSdp` 集約と整合)|

#### 修繕

- `AbandonAction(AbandonChoice.RepairBed)` で 20% 修繕(下限 0%)
- 上限 100%、下限 0% でクランプ

#### `DrowZzzGameSession` の ctor 拡張

M3-PR1 の 8 引数 ctor から **9 引数 ctor** に拡張(`BedDamages` 追加)。M2-PR5 で確立した `Inf()` ヘルパー伝播パターンを継続し、`bedDamages: Bed()`(N=2 で 0%/0%)を全テストファイルに Python 機械挿入。

### 4. キーワード能力(狂乱 / 本能 / 反撃)

#### 採用方針

3 つのキーワード能力を **`enum Keyword { Frenzy, Instinct, Counter }`** で表現し、**カードの効果列内の各効果エントリ** に付与する(効果単位の属性)。

#### 配置の判断(JIT 確定 2026-05-12)

プロジェクトオーナー JIT 確認(2026-05-12)で **「効果は単体で持つもの、複数のパターンを組み合わせたものがある」** と確定。**効果単位で 0 個以上のキーワードを付与する** 設計が必要。本 ADR では (b) `KeywordedEffect(IReadOnlyList<Keyword> Keywords, IEffect Inner)` ラッパー record で採用確定。

| 案 | 採用判断 |
| ---- | ---- |
| (a) 効果 record の属性として持つ(`AdjustSdpEffect(SdpTarget, int Delta, KeywordSet Keywords)`)| 不採用。全 effect record に field 追加で影響大、キーワードを持たない既存効果(SDP / Draw 等)にも null check が必要 |
| (b) **新規ラッパー record**(`KeywordedEffect(IReadOnlyList<Keyword> Keywords, IEffect Inner)`)で wrap | **採用**。`TimeOfDayBranchEffect` / `ChoiceEffect` と同パターン(既存 effect を wrap)、キーワード組み合わせも `IReadOnlyList<Keyword>` で表現可能 |
| (c) カードレベルで持つ(`CardData.Keywords`)| 不採用。効果単位の付与とずれる(夜効果のみ「狂乱・本能」を持つ夢カードのケースで困難)|
| (d) 別軸の属性(`KeywordAttribute` enum を `CardData.Attributes` に格納)| 不採用。`CardData.Attributes` の汎用 dict 流用で型安全性低、ADR-0007 §1 と矛盾 |

#### 未開示キーワードへの拡張性(JIT 確定 2026-05-12)

プロジェクトオーナー JIT 共有(2026-05-12)で **「他にもまだ開示していない効果が存在する」** と確定。`Keyword` enum は M3 範囲では `Frenzy` / `Instinct` / `Counter` の 3 種で実装するが、**将来未開示のキーワードが JIT 共有された時点で enum 値を追加する** 運用とする(ADR-0007 §1.4 / §1.5 で確立した「最初の登場時に JIT 確定」運用と同じ)。

`KeywordedEffect.Keywords` を `IReadOnlyList<Keyword>` で持つ設計は、将来のキーワード追加に対して **既存 effect record の interpreter switch case が破壊されない** 拡張性を担保する(新規キーワードは `EffectInterpreter` または `DrowZzzRule` 内で新規ケースとして処理される)。

#### 各キーワードのセマンティクス

##### 4.1. 狂乱(Frenzy)

「**反撃を受けない**」効果属性。プレイヤーが反撃を試みても、Frenzy を持つ効果に対しては反撃が成立しない。実装:`CounterAction` 評価時に target effect が Frenzy を持つなら illegal(JIT 確認待ち:illegal or 効果無効化?)。

##### 4.2. 本能(Instinct)

「**手段の放棄を受け付けない**」効果属性。`AbandonAction` で「捨てるカード」として Instinct を持つ効果を含むカードは選択不可。実装:`AbandonAction.IsLegalMove` で手札の各カードを scan し、Instinct を含むカードを除外。

##### 4.3. 反撃(Counter)

「**相手のターン中** に発動し、相手が使ったカードを無効化する」(JIT 確定 2026-05-12)。新規 `IGameAction` 派生型 **`CounterAction(CardId Counter, CardId Target)`** を追加。

**ADR-0006「自分のターン中のみカードプレイ可能」原則の更新**(本 ADR §5 で正式に書き換え):

- 既存原則(ADR-0006 §M1-PR5): カードプレイ(`PlayCardAction`)は自分の `WaitingForPlay` フェーズでのみ合法
- 新規原則(本 ADR §4 / §5): 上記に加え、**「反撃カード(Counter キーワードを持つ効果列のカード)」は相手のターン中の特定タイミングで合法プレイ可能**

#### 4.3.1. 相手ターン中の反撃プレイの実装方式(JIT 確認待ち、初期推奨案あり)

「相手のターン中に自分が反撃をプレイする」ためには、既存 `PhaseState` enum(`WaitingForDraw` / `WaitingForPlay` / `WaitingForEndTurn`)が「自分のターン中の状態」しか表現していない問題を解決する必要がある。候補:

| 案 | 評価 |
| ---- | ---- |
| (i) `PhaseState` に新規値 `WaitingForCounterResponse` を追加(相手がカードプレイ直後、両プレイヤーが反撃可能なタイミング)| シンプル、既存 enum 拡張で済む、`CounterAction.IsLegalMove` 条件が明確 |
| (ii) `DrowZzzGameSession` に「反撃機会フラグ」(`CounterableEffectStack: IReadOnlyList<CardId>` 等)を新規プロパティで保持 | 効果スタック型(MTG 風)、複数反撃の管理がしやすいが構造大幅変更 |
| (iii) `CounterAction` を「先制ターン外行動」として `IsLegalMove` で独自合法条件を定義 | `PhaseState` を変えずに済むが、合法条件が複雑化 |

**初期推奨**: (i) `WaitingForCounterResponse` 追加。M3-PR5(キーワード能力)着手時に JIT 確定。

##### 4.4. 「反撃の反撃」(JIT 確定 2026-05-12)

JIT 共有「**自分が反撃をされた時、自分のターン中に反撃を使うことで反撃に対する反撃ができる**」と確定。実装解釈:

1. 自分が攻撃カード A を自ターン中にプレイ
2. 相手が反撃カード B を相手ターン中相当のタイミングで合法プレイ → A の効果が無効化される
3. **次の自分のターン中** に反撃カード C をプレイ → B の効果(= A の無効化)を無効化する

「反撃の反撃」が成立した場合、元の攻撃カード A の効果はどうなるか **(残課題、JIT 確認待ち)**:

| 候補 | セマンティクス |
| ---- | ---- |
| (x) A の効果が遡及発動する(B の無効化が無効化されたので、A は元通り効果が走る)| 「無効化を無効化 = 元の状態に戻る」直感的解釈 |
| (y) A の効果は既に時間軸的に流れた後なので、C は B を打ち消すだけ、A 効果は復活しない | カード履歴管理が不要、シンプル |
| (z) C のテキストに「A 効果を遡及発動」と明示されていない限り、(y) と同じ挙動 | (x) と (y) のハイブリッド、効果記述次第 |

**初期推奨**: (x) 遡及発動。「無効化」を打ち消した時点で元の効果が成立するのが直感に合う。M3-PR5 着手時に JIT 確定。

##### 4.5. 反撃 vs 狂乱

- **狂乱は反撃を受けない**(JIT 共有原文)、つまり狂乱を持つ効果に対しては `CounterAction.IsLegalMove` で `false`(JIT 確認待ち:`IsLegalMove` で false → 不可、それとも合法だが no-op?)

#### 効果無効化のセマンティクス

「反撃」で効果無効化された場合、target カードの効果列はどうなるか:

- 候補 A: target カードはプレイ済(Field に移動済)だが効果列のみ skip
- 候補 B: target カードのプレイ自体を取り消し、手札に戻す
- 候補 C: target カードは捨て札へ(JIT 共有「無効化」の最も自然な解釈)

**初期推奨**: 候補 C。「無効化」= プレイ済だが効果は走らず、カードは捨てる、が直感に合う(JIT 確認待ち)。

### 5. ターン構造の詳細化

#### 採用

ADR-0006 §M1-PR2 で確立した「Draw → Play → EndTurn」3 フェーズを **本 ADR で拡張更新**(ADR-0006 を Supersede ではなく、本 ADR が新詳細を提供する形):

#### 拡張ターン構造

```
[自ターン開始]
  1. ベッド破損による SDP マイナス(`BedDamages[currentPlayer]` から計算、本 ADR §3)
  2. 影響 Tick(M2-PR5 確立、`OwnPhaseStart`)
  3. DDP 抽選(該当ラウンドのみ、M2-PR4 確立)
  4. 手段を 1 枚ドロー(`DrawCardAction`、`WaitingForPlay` へ)

[行動選択]
  5a. `PlayCardAction(card, choice, removalIndex)` でカードプレイ
       → 効果列評価、副作用としてベッドダメージが発生する場合あり(本 ADR §3 破損トリガー)
  5b. `AbandonAction(choice)` で放棄(本 ADR §2)
       → SDP +5 / ベッド +20% 修繕 を選択

[ターン終了]
  6. `EndTurnAction`(`WaitingForEndTurn` から `WaitingForDraw` へ rotate)
  7. ターン境界判定 → Round 21 完了で `Outcome` 設定(M3-PR1 確立)

[割り込み:連想]
  8. 自ターン中ならいつでも `AssociateAction(card)` で連想可能(JIT 確認待ち:相手ターンも可能か)
```

#### 順序保証

順序は **ADR-0010 §4「順序保証」を維持** しつつ、本 ADR §3 のベッド破損ダメージを最先頭に挿入:

1. **ベッド破損 SDP マイナス**(新規、本 ADR §3)
2. 新フェーズ確定(`Turn.Next`)— 既存
3. PhaseState 更新 — 既存
4. DDP 抽選(該当ラウンド)— M2-PR4
5. 影響 Tick — M2-PR5
6. Round 21 完了検出 → `Outcome` 設定 — M3-PR1

ベッド破損ダメージを最先頭に置く根拠:JIT 共有「自分のターン開始時、まずベッドの破損による SDP のマイナスが入ります」の「まず」と整合。

#### ADR-0006 §M1-PR2 ターン構造との関係

ADR-0006 で確立した最小 3 フェーズ(Draw → Play → EndTurn)は **そのまま維持**。本 ADR §5 はその上に「ベッド破損 / 影響 Tick / DDP 抽選 / Outcome 設定」の副作用ステップを正確に文書化する(ADR-0010 §4「順序保証」の延長線上)。`DrowZzzPhaseState` enum 自体は変えない(`WaitingForDraw` / `WaitingForPlay` / `WaitingForEndTurn` の 3 値維持)。

### 6. カード No.00「夢」

#### 採用

カード `CardId = "00"`、名前「夢」を **`EarlyWinTriggerEffect`**(ADR-0010 §5)+ **キーワード能力ラッパー**(本 ADR §4)+ **`TimeOfDayBranchEffect`**(M2-PR3、夜・朝分岐)を組み合わせて表現する。

#### 効果構造(JIT 共有原文の整理)

| 時刻 | 効果 |
| ---- | ---- |
| **夜**(`Clock.IsNight`、Round 1〜16)| 「狂乱・本能」付き `EarlyWinTriggerEffect`(FDS ≥ 100 なら勝利)|
| **朝**(`Clock.IsMorning`、Round 17〜21)| `AdjustSdpEffect(Self, -80)`(JIT 確認待ち:「-80 / ±0」の表記は M2-PR3 / M2-PR5 慣例で「自分 / 相手」の順、相手 ±0 で確定?)|

#### 連想で引く / 使用条件

- 連想で引く条件:現プレイヤーの `TotalPoints` が **FDS 80 を超える**(本 ADR §1)
- 使用条件:**次の自分のターン以降** に限り使用可能、かつ **FDS ≥ 100**(JIT 共有原文)

「次のターン以降」の実装(JIT 確認待ち):

- 候補 A: 連想時の現ラウンドを記録、`PlayCardAction.IsLegalMove` で現ラウンド > 連想時ラウンドを確認
- 候補 B: 連想で引いたカードに「使用不可フラグ」を持たせる、自ターン開始時に解除
- 候補 C: `PlayerInfluence` 機構を流用(M2-PR5)で「使用待機」影響を表現

**初期推奨**: 候補 A(現ラウンド記録、シンプル)or 候補 C(既存 Influence 機構を流用、Tick で解除)。M3-PR(夢実装)着手時に JIT 確定。

#### カードデータ表現(M3-PR(夢実装)で確定)

```csharp
// entries 側
new KeyValuePair<CardId, CardData>(CardId.Of("00"), new CardData("夢", new Dictionary<string, int>()))

// effects 側(KeywordedEffect で「狂乱・本能」をラップ、AssociatableMarkerEffect で連想可能を明示)
new KeyValuePair<CardId, IReadOnlyList<IEffect>>(CardId.Of("00"), new IEffect[]
{
    // §1 で確定したマーカー方式(初期推奨案 (i))に従い、連想可能カードであることを effect 列で明示。
    // これは「就寝カード = EarlyWinTriggerEffect を効果列に持つ」(ADR-0010 §5)と同パターン。
    new AssociatableMarkerEffect(),
    new TimeOfDayBranchEffect(
        nightEffects: new IEffect[]
        {
            new KeywordedEffect(
                new[] { Keyword.Frenzy, Keyword.Instinct },
                new EarlyWinTriggerEffect()),
        },
        morningEffects: new IEffect[]
        {
            new AdjustSdpEffect(SdpTarget.Self, -80),
        }),
})
```

「夢」は **M3 範囲で実装される最初の連想可能カード** だが、本 ADR §1 で確定した通り **「夢」専用機構ではない**:後続 PR / カードでも `AssociatableMarkerEffect` を効果列に持つカードは連想可能になる。具体的な後続カードは M2-PR6+ で JIT 共有(運用パターンは ADR-0010 §「Implementation Notes」と同じ)。

### 7. ADR-0010 §5 `EarlyWinTriggerEffect` の発動条件拡張(本 ADR で再仕様)

ADR-0010 §5 で確定した `EarlyWinTriggerEffect.Apply` の評価ロジック(夜 + `TotalPoints >= 100` で `Outcome = WinnerOutcome(currentPlayer)`)を **そのまま維持** する。本 ADR §6「夢」カードがこの効果を **連想 → 使用条件 → 夜効果として** プレイすることで発火させる構造になり、ADR-0010 §5 の API は壊さない。

つまり:

- **ADR-0010 §5**: 効果 record `EarlyWinTriggerEffect` の評価ロジック(夜 + 持ち点 ≥ 100 で発火)
- **本 ADR §7**: その効果を発火させるためのゲーム文脈(連想 + 次ターン使用 + 夢の夜効果として)

両者は階層が違い、本 ADR は ADR-0010 を覆さない。

#### `EarlyWinTriggerEffect` テストの確認

ADR-0010 / M3-PR1 で確立した `EarlyWinTriggerEffectTests`(DZ-183〜DZ-186)は本 ADR でも有効。`TimeOfDayBranchEffect` で wrap された場合の動作は M2-PR3 で確認済(夜効果列の中で `EarlyWinTriggerEffect` が評価される)。

### 8. 実装 PR 分割計画

本 ADR で確定した 6 機構を以下の M3-PR 群で段階的に実装する。1 PR = 1 機構 = 1 論理変更を原則とする(ADR-0005 §M2 / ADR-0007 §6 の分割パターン継承)。

| PR | 内容 | 主な追加・変更 | 依存 |
| ---- | ---- | ---- | ---- |
| **M3-PR2** | ベッド破損率(Session 状態 + 自ターン開始時のダメージ計算 + 破損トリガー効果)| `DrowZzzGameSession.BedDamages` 追加(9 引数 ctor 化)、`ApplyEndTurn` 内ダメージ計算ステップ追加、新規 `DrowZzzBedConstants.BedDamageRatePerSdp = 5` 集約、**`DamageBedEffect(SdpTarget, int Percent)` record 新設**(5 の倍数検証付き、JIT 確定済)| M3-PR1 |
| **M3-PR3** | 放棄機構(`AbandonAction` + 選択肢) | `AbandonAction(AbandonChoice)` + `AbandonChoice` enum + `IsLegalMove` / `Apply` 実装、放棄テスト | M3-PR2(`AbandonChoice.RepairBed` がベッド機構依存)|
| **M3-PR4** | 連想機構(`AssociateAction` + 特殊ドロー + 連想可能カード判別) | `AssociateAction(CardId)` + `IsLegalMove`(FDS > 80 + 対象カードが連想可能)+ `Apply`(特殊な手段で手札に追加)+ `AssociatableMarkerEffect` マーカー record(初期推奨案、§1 (i))+ 連想テスト。**汎用ドロー機構として実装**(夢以外のカードも将来連想可能になる、JIT 確定 2026-05-12)| M3-PR1 |
| **M3-PR5** | キーワード能力(狂乱 / 本能 / 反撃)+ 相手ターン中の反撃機構 | `Keyword` enum(`Frenzy` / `Instinct` / `Counter`、将来未開示キーワードで拡張前提)、`KeywordedEffect(IReadOnlyList<Keyword>, IEffect)` ラッパー record、`CounterAction(CardId Counter, CardId Target)` 追加、**`DrowZzzPhaseState` に `WaitingForCounterResponse` 追加候補**(JIT 確定済「反撃は相手ターン中」と「反撃の反撃は自ターン中」の両経路をフェーズで区別)、`AbandonAction.IsLegalMove` で Instinct チェック、ADR-0006「自分ターン中のみプレイ」原則の更新を本 ADR §5 で記録 | M3-PR3(放棄機構に Instinct 影響)|
| **M3-PR6**(M3 完成 PR) | カード No.00「夢」+ 統合テスト | `InMemoryCardCatalog` に「夢」登録、上記 5 機構の統合動作確認、`EarlyWinTriggerEffect` を「夢」効果列で発火させる end-to-end テスト | M3-PR2〜PR5 すべて |

PR 数の目安:**5 PR**(M3-PR2 〜 M3-PR6)+ 各 PR の完成記録 PR(分離パターン継続なら +5 PR)で計 **約 10 PR**。M1 / M2 と同等の規模。

### 9. 不採用案(全体)

| 案 | 不採用理由 |
| ---- | ---- |
| ADR-0010 を `Superseded by ADR-0011` に置き換え | ADR-0010 の確定内容(IsTerminated / GetWinner / GameOutcome / EarlyWinTriggerEffect 機構)は本 ADR でも前提として使う、覆さない |
| 6 機構を 1 PR(本 PR or M3-PR2)に全部詰め込む | レビュー粒度・テスト粒度・ロールバック粒度のすべてが破綻、過去 PR 規模の 5〜10 倍 |
| ADR-0011 で全機構の詳細を完全確定してから実装着手 | JIT 共有方式と矛盾、ADR-0007 §1.4 / §1.5 / ADR-0010 §「Implementation Notes」の運用と整合しない |
| Domain 層に `BedDamage` / `Keyword` 等を置く | DrowZzz 固有概念で Domain ゲーム非依存原則(ADR-0002)と矛盾 |
| 連想を `DrawCardAction` 拡張、放棄を `PlayCardAction` 拡張で表現 | semantic が混在し IsLegalMove 条件が複雑化、Action 階層分離が筋 |
| キーワード能力を `CardData.Attributes` dict で表現 | 効果単位の付与が必要で(夢の夜効果のみ「狂乱・本能」)、カード単位の attribute では表現不能 |
| 「反撃」を相手ターン中のカードプレイなしで実装(プレイされた効果を retroactive にキャンセル)| 既存 Action 階層と矛盾、JIT 共有「ターン中に使用された手段に対して **この手段を使用することで**」と整合しない |

## Consequences

### Positive

- M3 範囲の本命ゲームメカニクスが全機構列挙され、M3-PR2 以降の実装ロードマップが明確化
- 機構ごとの PR 分割で 1 PR = 1 論理変更原則を維持、レビュー粒度の破綻を回避
- ADR-0010 / ADR-0006 / ADR-0007 を覆さず拡張する形で、過去 ADR の意思決定履歴を保つ
- 「夢」カードの統合動作(連想 → 次ターン使用 → 夜効果として発火)が end-to-end で記述され、M3 完成基準(Definition of Done)が明確
- 「JIT 確認待ち」の詳細を §で明示することで、M3-PR2 以降の各 PR 着手時にプロジェクトオーナーから JIT 共有を引き出す構造が確立(ADR-0007 §1.4 / §1.5 で確立した JIT 確定運用と整合)

### Negative

- M3 範囲の規模が ADR-0010 起票時の想定より大きく(2〜4 ファイル想定 → 5 PR / 約 30〜50 ファイル想定)、M3 完成までの期間が伸びる可能性
  - **緩和**: PR 分割で進捗を可視化、各 PR 完成時点で動作する形に持っていく(M1 / M2 と同じ段階的縦串実装、ADR-0005)
- `DrowZzzGameSession` が 9 引数 ctor 化(M2-PR3:3→4 / M2-PR4:4→6 / M2-PR5:6→7 / M3-PR1:7→8 / M3-PR2:8→9 の **5 度目の breaking change**)
  - **緩和**: M2-PR5 で確立した `Inf()` ヘルパー伝播パターンを継続、Python 機械挿入で既存テスト修正は最小化
- 「JIT 確認待ち」項目が多く、M3-PR2 以降の各 PR で JIT 確認を都度求めるため、プロジェクトオーナーの負担が増える
  - **緩和**: 各 PR で必要な JIT 項目を本 ADR §で事前列挙、PR 着手時に該当項目のみ確認する運用
- 既存 ADR-0010 §5 で確定した `EarlyWinTriggerEffect` の発動条件解釈が文書間で 2 箇所に分かれる(ADR-0010 §5 = 効果 record 単体の発動 / 本 ADR §7 = 夢カード経由の文脈)
  - **緩和**: ADR-0010 §5 末尾に本 ADR §7 への参照を追加(本 ADR 同 PR で同梱)、読み手が両者の関係を辿れる構造

### Neutral

- 本 ADR は M3 範囲の詳細を確定するもので、M4(永続化 / SO 化)/ M5(Bootstrap / Presentation)には直接影響しない
- キーワード能力 enum(`Keyword`)は M3 で 3 種(`Frenzy` / `Instinct` / `Counter`)を確定、将来のカードで増える場合は本 ADR を「追加 JIT 確定」で延長する運用(ADR-0007 §1.4 / §1.5 と同じ)
- 「夢」カード以外の就寝カード(他の `EarlyWinTriggerEffect` 利用カード)は本 ADR では確定しない、必要になった時点で JIT 共有
- ScriptableObject 化(M4)は本 ADR の範囲外、`Keyword` enum / ベッド破損計算式の SO 移行は M4 で別 ADR で再評価

## Alternatives Considered

| 案 | 不採用理由 |
| ---- | ---- |
| 本 ADR を起票せず M3-PR2 以降の各 PR で個別判断 | 6 機構の整合性が PR 間で取れない、ADR-0010 §「Implementation Notes」で「就寝カードは JIT」と保留した範囲を超える大規模拡張 |
| ADR-0010 を Superseded by ADR-0011 に置き換え | ADR-0010 §1〜§9 の確定内容(API 契約 / GameOutcome 階層 / Session.Outcome / 引き分け仕様等)は本 ADR でも前提として使う、覆さない |
| 1 つの巨大 PR(M3-PR2)で 6 機構を実装 | レビュー破綻、過去 PR 規模(M2-PR5 で約 35 ファイル / +2,139 行)の 3〜5 倍、code-reviewer 警告が大量に出る |
| 6 機構を機構ごとに別 ADR(ADR-0011〜0016)に起票 | ADR 数が肥大化、機構間の依存関係(放棄 ⇄ ベッド ⇄ キーワード能力)を別 ADR で表現すると分散しすぎる、本 ADR で一括整理する方が筋 |
| カード No.00「夢」を M3-PR1 同梱でやり直す | PR #42 はマージ済(2026-05-12)、覆すコストが大、本 ADR で前向きに整理する方が筋 |
| キーワード能力を effect record の属性 field として直接持たせる(`AdjustSdpEffect(... , KeywordSet)`)| 全 effect record に field 追加、影響大、`KeywordedEffect` ラッパー方式が `TimeOfDayBranchEffect` / `ChoiceEffect` と整合 |
| 連想を `ICardCatalog` から直接 spawn(山札に「夢」を入れない)で実装 | 「特殊な手段で手段を引く」の「特殊な」が catalog 直接生成と一致するが、JIT 確認待ち項目で複数案候補のため決め打ちしない |
| ベッド破損を `DrowZzzGameSession.BedDamage`(プレイヤー間共有 1 つ)で持つ | 「自分のベッド」概念と矛盾、プレイヤーごとに保持(N 個)が JIT 共有原文と整合 |
| ターン構造を `DrowZzzPhaseState` enum に新フェーズ追加(`WaitingForAbandonOrPlay` 等)で表現 | フェーズ数が肥大化、Action 階層で表現する方が筋(`AbandonAction` / `PlayCardAction` の Action 単位選択)|
| 「反撃」をプレイされた効果を retroactive にキャンセル(`Outcome.Cancelled`)で表現 | 既存 Action 階層と矛盾、JIT 共有原文「この手段を使用することで」とも整合せず |
| 「本能」を `CardData.Attributes` に flag で表現 | 効果単位の付与が必要で(夢の夜効果のみ持つ)、カード単位の attribute では表現不能、`KeywordedEffect` ラッパーが筋 |

## Implementation Notes

### 本 ADR 起票 PR 同梱の変更

- `docs/adr/0010-m3-game-termination-and-victory-determination.md` §5 末尾に本 ADR §7 への参照を追加(`EarlyWinTriggerEffect` の発動条件拡張先を示す)
- `docs/adr/README.md` にインデックス 1 行追加
- `CLAUDE.md` §11「確立済み ADR 一覧」に ADR-0011 を 1 行追加(M2-PR5 完成記録 PR でスリム化済の構造を継続)
- `CLAUDE.md` §11「Phase 進捗」M3 行を「進行中 — ADR-0010 完成、ADR-0011 起票済、M3-PR2 着手予定」に更新

### M3-PR2 着手時の JIT 確認項目(本 ADR §3 関連)

- ベッド破損ダメージ計算式: **JIT 確定済**(5% につき -1、整数除算、本 ADR §3 確定済)。M3-PR2 で `DrowZzzBedConstants.BedDamageRatePerSdp = 5` を新規 constants クラスに集約
- ベッド破損率増加トリガーの確定(どの行動で / 何 % ずつ、5 の倍数以外も発生するか)
- ベッド修繕の上限・下限挙動(`AbandonChoice.RepairBed` が 0% 時の挙動)

### M3-PR3 着手時の JIT 確認項目(本 ADR §2 関連)

- 放棄時の捨て対象カード選択方式(プレイヤー選択 / ランダム / 最古)
- 「本能」を持つカードの放棄不可挙動の正確な意味

### M3-PR4 着手時の JIT 確認項目(本 ADR §1 関連)

- 連想対象の領域(山札 top / 特定位置 / 別領域 / catalog 直接生成)
- 連想タイミング(自ターン中のみ / 相手ターン中も可)
- FDS 80「超」(81+)か「以上」(80+)か
- 「連想可能カード」の判別方式の確定(マーカー effect 案 (i) を初期推奨、§1 表参照)。後続の連想可能カード(夢以外)が登場するタイミングと仕様も併せて JIT 共有予定

### M3-PR5 着手時の JIT 確認項目(本 ADR §4 関連)

JIT 確定済(本 PR 同梱):

- ✓ 相手ターン中の反撃発動(JIT 確定 2026-05-12)
- ✓ 「反撃の反撃」が自ターン中に可能(同上)
- ✓ キーワード能力の付与単位は効果単位、組み合わせ可能(同上)
- ✓ `Keyword` enum は将来未開示キーワードで拡張前提(同上)

残課題(M3-PR5 着手時に JIT 確認):

- 「反撃の反撃」で元カード A の効果が遡及発動するか(候補 x / y / z、初期推奨 (x) 遡及発動)
- 効果無効化のセマンティクス(候補 A/B/C のどれか、初期推奨 C 捨て札へ)
- 「狂乱を反撃で打ち消そうとした」場合の illegal-move 化か no-op 化
- 相手ターン中の反撃プレイ機構の実装方式(候補 i `WaitingForCounterResponse` / ii 効果スタック / iii 独自合法条件、初期推奨 (i))
- ADR-0006「自分ターン中のみプレイ」原則の正式更新形式(ADR 補足追記? 別 ADR?)

### M3-PR6 着手時の JIT 確認項目(本 ADR §6 関連)

- 「夢」朝効果「-80 / ±0」の解釈(M2-PR3 / M2-PR5 慣例「自分 / 相手」で確定?)
- 「次の自分のターン以降」の実装方式(候補 A / B / C)
- 連想条件「FDS 80 超」と使用条件「FDS 100 以上」の境界(「超」vs「以上」の混在意図)
- 「夢」が初期山札に含まれるか / 連想専用カードか

### M3 完成記録の追記タイミング

本 ADR の §M3-PR2〜PR6 完成記録は、各 PR 単位で本 ADR §M3-PR-N 完成記録(2026-MM-DD)として直前に追記する(ADR-0007 / ADR-0009 / ADR-0010 §「完成記録の追記タイミング」と同パターン)。M3 全体の完成時に §M3 完成記録(全体)を別途追加、Definition of Done 達成方法を集約する。

### 要件 ID prefix(M3 範囲)

| Prefix | 範囲 | 配置 |
| ---- | ---- | ---- |
| `APP-` | 汎用 Application 層 interface(本 ADR §1 / §2 で `IGameAction` 派生型追加に伴う APP-044+ 採番)| `docs/specs/application/game-action.md` 等 |
| `DZ-` | DrowZzz 固有(各機構の振る舞い / カード No.00「夢」)| `docs/specs/games/drowzzz/{bed-damage,abandon,association,keyword-abilities,dream-card}.md` 等 |
| `CFG-` | `IGameConfig` 関連(本 ADR では追加なし、ベッド破損計算の SO 化は M4 候補)| 該当なし |

M3-PR2〜PR6 着手時に最新採番状況を `grep -hroE "\b(APP|DZ|CFG)-[0-9]+\b" docs/specs/ | sort -u | tail` で再確認、連番継続。

### Phase 2 の進捗バナー更新(本 ADR 起票 PR 同梱)

CLAUDE.md §11「Phase 進捗」M3 行を以下に更新:

- 旧: `**M3**(勝利条件 / 終了処理): **進行中** — ADR-0010 起票済、M3-PR1(...)着手中`
- 新: `**M3**(勝利条件 / 終了処理 + ゲームメカニクス拡張): **進行中** — ADR-0010 完成(M3-PR1)、ADR-0011 起票済、M3-PR2(ベッド破損)着手予定`

## Related

- 前提: [ADR-0001 ADR Operations](0001-adr-operations.md)
- 前提: [ADR-0002 Phase 1 Domain 拡張](0002-phase1-domain-boundaries.md) — Domain ゲーム非依存原則(`BedDamage` / `Keyword` の Application 配置と整合)
- 前提: [ADR-0004 IsExternalInit polyfill](0004-init-setter-polyfill.md) — `KeywordedEffect` / `BedDamages` の record + init + with パターン
- 前提: [ADR-0005 Phase 2 Roadmap](0005-phase2-roadmap-drowzzz.md) — §M3 範囲、本 ADR で具体化
- 前提: [ADR-0006 M1 詳細](0006-m1-detail-application-interfaces.md) — `IGameAction` / `IGameRule` 最小 API、本 ADR で M3 範囲の Action 派生型を追加(`AssociateAction` / `AbandonAction` / `CounterAction`)
- 前提: [ADR-0007 M2 詳細(カード効果)](0007-m2-detail-card-effects.md) — `IEffect` + `EffectInterpreter` パターン、本 ADR の `KeywordedEffect` ラッパーは §1.5「継続影響」と同パターン
- 前提: [ADR-0008 M2 Clock + 夜・朝フェーズ](0008-m2-drowzzz-clock-and-night-morning.md) — Clock 構造、本 ADR §6「夢」の夜・朝分岐で利用
- 前提: [ADR-0009 M2-M3 DP 機構 + 勝利条件](0009-m2-m3-dp-and-victory-conditions.md) — 持ち点 = FDP+DDP+SDP の computed、本 ADR §1(FDS = TotalPoints)/ §6(夢の発動条件)で利用
- 前提: [ADR-0010 M3 詳細(ゲーム終了 + 勝者決定)](0010-m3-game-termination-and-victory-determination.md) — `EarlyWinTriggerEffect` / `GameOutcome` / `Session.Outcome` の基盤、本 ADR §7 で発動条件を拡張(覆さず)
- 関連規約: [`CLAUDE.md`](../../CLAUDE.md) §5 アーキテクチャ依存ルール / §6 テスト方針 / §9 定数管理方針 / §11 ADR 運用
- 関連: [`docs/specs/games/drowzzz/`](../specs/games/drowzzz/) — M3-PR2〜PR6 で各機構の EARS / .feature 追加
- 後続: M3-PR2(ベッド破損)/ M3-PR3(放棄)/ M3-PR4(連想)/ M3-PR5(キーワード能力)/ M3-PR6(夢カード + 統合)
- 後続: ADR-0012 候補(M4 永続化 / SO 化、本 ADR §「ScriptableObject 化」で留保した分の再評価)
