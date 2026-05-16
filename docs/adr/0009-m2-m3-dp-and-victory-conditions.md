# ADR-0009: M2-M3 — DP 機構(FDP / DDP / SDP)と持ち点・勝利条件

> **Note(M2 / M3 完結の帰結、2026-05-16)**:本 ADR で定義した DP 機構と勝利条件は M2-PR3〜PR5(SDP / DDP)+ M3-PR1〜PR6(勝利条件 / 終了判定 / 早期勝利)で実装が完結し、Phase 2 完結時点で M2 / M3 ステータスを **完結扱い** とする(`CLAUDE.md` §11 / [ADR-0005 §7](0005-phase2-roadmap-drowzzz.md) / [ADR-0016 §11 M5-PR8 完成記録](0016-m5-bootstrap-presentation.md))。

| 項目 | 値 |
| ---- | ---- |
| Status | Accepted |
| Date | 2026-05-11 |
| Decider | プロジェクトオーナー |

## Context

ADR-0007 で M2 効果インフラを、ADR-0008 で M2 Clock 概念を確定した。M2-PR2(Clock 実装)着手前に、プロジェクトオーナーから「DrowZzz のゲームコンセプト」「DP 機構(FDP / DDP / SDP)と持ち点」「勝利条件(早期勝利 / 終了時勝利)」「Clock 仕様の境界訂正(計 20 ターン → 21 ターン、Turn 21 = 07:00 は最終プレイ可能ターン)」「ボードゲーム一般的用語(ターン / フェーズ / PhaseState)への用語規約整理」が JIT 共有された。

### DrowZzz のゲームコンセプト(プロジェクトオーナー JIT 共有 2026-05-11)

DrowZzz は **「眠気と戦う心理戦カードゲーム」**。テーマ性が DP 機構 / 勝利条件 / 戦略を一貫した世界観で結びつける。

| 時間帯 | 自分の目標 | 自分の戦略 | 相手への戦略 |
| ---- | ---- | ---- | ---- |
| **夜**(21:00〜04:30、Turn 1〜16) | 相手より先に「寝る」 = 持ち点 100 で就寝 | **寝ようとする**(自分の DP を上げる)| **寝かせまいとする**(相手の DP を下げる / 抑える)|
| **朝**(05:00〜07:00、Turn 17〜21)| 「寝ない」 = 寝ると起きれなくなるため起きていたい | **眠気を抑える**(自分の DP を下げる)| **眠くさせる**(相手の DP を上げる)|

戦略は夜と朝で **完全に反転**する。同じカード効果でも、夜にプレイすれば自分有利、朝にプレイすれば相手有利になる設計余地が生まれる(M2-PR3+ の effect record 設計でこの非対称性を活かす)。

### 用語規約(ボードゲーム一般用語に整理、ADR-0006 §M1-PR2 用語からの訂正)

ADR-0006 §M1-PR2 で「ラウンド / サブターン」を導入したが、M2 着手中にプロジェクトオーナーから「ボードゲーム一般用語に揃えたい」と訂正共有された。本 ADR で確定:

| 階層 | 新用語 | 意味 | 旧用語(ADR-0006 / ADR-0008)| 実装上の名前 |
| ---- | ---- | ---- | ---- | ---- |
| **大単位** | **ターン**(Turn)| 30 分 = N フェーズ = 全プレイヤー 1 巡 | ラウンド(Round)| `Clock.RoundNumber` / `DrowZzzGameSession.CurrentRound`(実装名は別 PR で `TurnNumber` / `CurrentTurn` に追随予定、`docs/todo.md` 追跡)|
| **中単位** | **フェーズ**(Phase)| 1 プレイヤー 1 行動(Draw → Play → EndTurn の一連)| サブターン(SubTurn)| `TurnState.TurnNumber`(Domain 汎用名、改名は別 ADR で慎重判断、`docs/todo.md` 追跡)|
| **小単位** | **PhaseState** | フェーズ内の待ち状態(WaitingForDraw / WaitingForPlay / WaitingForEndTurn)| `DrowZzzTurnPhase`(同型名)| `DrowZzzTurnPhase` → **`DrowZzzPhaseState`** に本 ADR 同 PR でリネーム |

**ボードゲーム用語の標準性に関する注記**: TCG 系(MTG 等)では「Round = 全 Turn 1 巡」「Turn = 1 プレイヤー 1 巡」「Phase = Turn 内の段階」とする慣例がある一方、Eurogame 系(カタン等)では「Round = 全プレイヤー 1 巡」を中心概念にし「Turn」を Round と同義に使うことがある。DrowZzz は「1 ターン = 30 分の時間単位」というプロジェクトオーナーの定義から **Eurogame 寄りの『ターン = 大単位』** を採用する。本 ADR §X 用語規約で正式採用、過去 ADR(ADR-0006 / 0007 / 0008)の旧用語記述はそのまま維持し、訂正履歴は本 ADR §7 + git 履歴で追跡する。



本 ADR は以下を確定する:

1. DP 種別(FDP / DDP / SDP)の意味と保持
2. 持ち点の定義(= FDP + DDP + SDP の合計)
3. DDP プール / DDP 抽選タイミング
4. SDP の初期値と公開性
5. 勝利条件(早期勝利 / 終了時勝利)
6. M2 / M3 境界の再整理(DP 機構は M2、勝敗判定の本格実装は M3 候補)
7. ADR-0007 / ADR-0008 の境界訂正(Clock 21 ラウンド化、IsMorning 範囲拡張、山札枯渇計算更新)

ADR-0005 / ADR-0006 / ADR-0007 / ADR-0008 で既に決まっている前提:

- 縦串で本命ゲーム DrowZzz、N=2 想定(ADR-0005 / ADR-0006)
- 既存 `FirstDrowsyPoints`(FDP): プレイヤーごと、ゲーム開始時に `IGameConfig.FdpPool` から抽選で確定、隠し情報、以降不変(ADR-0006、M1-PR3 実装済)
- 効果インフラ: `IEffect` / `EffectInterpreter` / `ICardCatalog<IEffect>`(ADR-0007、M2-PR1 実装済)
- Clock: `DrowZzzClock(int RoundNumber)`、Session の computed プロパティ、TurnNumber 単一情報源(ADR-0008)

### プロジェクトオーナーから JIT 共有された仕様前提(2026-05-11)

#### 勝利条件

| 項目 | 値 |
| ---- | ---- |
| 早期勝利 | **夜の間(Turn 1〜16、`IsNight = true`)** に、先に持ち点 ≥ 100 にして「**就寝カード**」(夜に寝るための特定効果タイプのカード、カード ID / 効果詳細は M2-PR3+ で JIT 確定)をプレイしたプレイヤーが勝利 |
| 終了時勝利 | Turn 21 完了直後(時計値 07:00 = 最終プレイ可能ターン完了)に、相手より **持ち点が低い** プレイヤーが勝利(= 朝まで起きていられた方が勝ち)|
| 戦略示唆 | **夜:自分の DP を上げる + 相手の DP を下げる**(寝かせまいとする)/ **朝:自分の DP を下げる + 相手の DP を上げる**(眠くさせる)|
| 早期勝利可能領域 | **Turn 1〜16(= 夜のすべて = `Clock.IsNight`)** 。Turn 17 開始(05:00 = 朝になった瞬間)以降は早期勝利不可 |
| 早期勝利の論理式 | `session.Clock.IsNight && session.TotalPoints[player] >= 100 && cardEffect is EarlyWinTrigger(就寝カード効果タイプ)` |
| 引き分けの扱い | M3 着手 ADR で JIT 確定(本 ADR では保留)|

#### DP 種別

| 略称 | 正式名(仮、変更余地あり)| 性質 |
| ---- | ---- | ---- |
| **FDP** | First Drowsy Point | ゲーム開始時に `IGameConfig.FdpPool` から抽選、隠し情報、不変(ADR-0006 既存)|
| **DDP** | Draw Drowsy Point | 2 時間ごと(計 5 回)に共有プールから 1 枚抽選、隠し情報、累積式 |
| **SDP** | Second Drowsy Point | 各プレイヤーの行動などによって変動、公開情報、初期値 0 |

#### DDP プールの構造

| 項目 | 値 |
| ---- | ---- |
| プール枚数 | 39 枚(13 種 × 3 枚) ※ 起票時「36 枚」と書いたが計算誤記、M2-PR4 PR で訂正 |
| 値の種類 | -30, -25, -20, -15, -10, -5, 0, 5, 10, 15, 20, 25, 30(13 種、5 刻み、±30 範囲)|
| プール保持 | プレイヤー間で共有(1 名が抽選した値は他名が同じ値を引けない、ただしプール内には複数枚あるため同じ値の重複は可)|
| 抽選方式 | `IRandomSource.NextInt` で残プールから 1 枚をランダム抽選、抽選後はプールから除外 |

#### DDP 抽選タイミング

| 時刻 | ラウンド開始 | 備考 |
| ---- | ---- | ---- |
| 23:00 | Round 5 開始時 | 1 回目 |
| 01:00 | Round 9 開始時 | 2 回目 |
| 03:00 | Round 13 開始時 | 3 回目 |
| 05:00 | Round 17 開始時(朝の最初)| 4 回目 |
| 07:00 | Round 21 開始時(最終プレイラウンド)| 5 回目 |

→ **計 5 回**。各タイミングで N=2 プレイヤー × 1 枚 = 2 枚を共有プールから抽選。総抽選 10 枚 ≤ プール 39 枚で余裕 29 枚(本 ADR 起票時「36 枚 / 余裕 26」と書いたが「13 種 × 3 枚 = 39」の計算誤記、M2-PR4 PR で訂正同梱)。

#### 持ち点

| 観点 | 値 |
| ---- | ---- |
| 計算式 | **持ち点 = FDP + DDP + SDP** |
| 自分の持ち点 | 全 DP を見られるため完全に計算可能 |
| 相手の持ち点 | SDP のみ既知。FDP / DDP は隠しのため、相手の真の持ち点は推測のみ |
| 戦術上の含意 | SDP は「相手から見える持ち点指標」、FDP / DDP は「自分のみ見える調整要素」。情報の非対称性が戦略に直結 |

### Clock 仕様の境界訂正(ADR-0008 / ADR-0007 への影響)

JIT 共有過程で「計 20 ターン」が「**計 21 ターン**(Round 1〜21、Round 21 = 07:00 = 最終プレイラウンド)」に訂正された:

| 項目 | ADR-0008 起票時 | 本 ADR で訂正 |
| ---- | ---- | ---- |
| プレイ可能ラウンド | Round 1〜20 | **Round 1〜21** |
| Round 21 の扱い | ゲーム終了境界、プレイされない | **最終プレイラウンド、プレイされる** |
| Round 21 完了直後の時計値 | 07:00 | **07:30 は仕様上存在しない、ゲーム終了処理に入る** |
| `IsMorning` 範囲 | Round 17〜20 | **Round 17〜21** |
| ADR-0007 §「山札枯渇」MaxRound | 20 | **21** |
| ADR-0007 §「山札枯渇」総 Draw | 40 枚 | **42 枚** |
| ADR-0007 §「山札枯渇」余裕 | 6 枚 | **4 枚** |

訂正の同梱方針: ADR-0008 / ADR-0007 を直接編集する形で訂正履歴を git 履歴に残し、Status は両者とも `Accepted` のまま維持する(ADR-0001 「合意後に書き換える」運用、ADR-0007 §Related 末尾を ADR-0008 起票 PR で訂正した先例と同パターン)。本 ADR が訂正の根拠を提供する。

## Decision

### 1. DP 種別の定義と保持(Domain / Application 配置)

#### DP の意味(眠気メタファー)

DP(Drowsy Point)は **「眠気の蓄積度合い」** を表す。持ち点 100 = **「就寝閾値」**(これに達すると寝てしまうライン)。各種 DP は眠気の異なる成分を表す:

| 略称 | 意味(メタファー)|
| ---- | ---- |
| **FDP**(First Drowsy Point)| 各プレイヤーの「**眠りやすさの初期値**」。体質や疲労度のような前提値、ゲーム開始時に決まり以降不変 |
| **DDP**(Draw Drowsy Point)| 「**自然に襲ってくる眠気の波**」。2 時間ごと(23:00 / 01:00 / 03:00 / 05:00 / 07:00)に時間経過で変動、隠し情報(自分の眠気はわかるが、外見からは読み取れない)|
| **SDP**(Second Drowsy Point)| 「**行動による眠気の変動**」。プレイヤーの行動(カードプレイ等)で動く公開情報(相手の行動の結果は周りから観測可能)|

ゲームコンセプト(§Context)と整合: 夜は DP を 100 に近づけて寝るのを目指し、朝は DP を低く保って起きていることを目指す。

#### 保持構造

ADR-0006 §2.2 で既存の `FirstDrowsyPoints`(FDP)は `DrowZzzGameSession.FirstDrowsyPoints` として `IReadOnlyDictionary<PlayerId, int>` で保持済。DDP / SDP も同様の構造で `DrowZzzGameSession` に追加する。

```csharp
// DrowZzzGameSession.cs (M2-PR3+ で追加、本 ADR 起票時点では設計確定のみ)
public sealed record DrowZzzGameSession
{
    // 既存 (ADR-0006 M1-PR3)
    public IReadOnlyDictionary<PlayerId, int> FirstDrowsyPoints { get; init; }  // FDP

    // 新規 (M2-PR3+ で実装)
    public IReadOnlyDictionary<PlayerId, int> DrawDrowsyPoints { get; init; }  // DDP 累積値
    public IReadOnlyDictionary<PlayerId, int> SecondDrowsyPoints { get; init; }  // SDP 累積値
    public Pile DdpPool { get; init; }  // 残 DDP プール(プレイヤー間共有、抽選で減る)
}
```

設計指針:
- `IReadOnlyDictionary<PlayerId, int>` で **キー集合は GameState.Players の PlayerId 集合と一致**(cross-field 検証、既存 FDP と同パターン、ADR-0006 §2.2 / DrowZzzGameSession の `EnsureKeysMatchPlayers`)
- `DdpPool` は **`Pile` 型を再利用**(M1-PR3 で確立した山札と同じ「順序つき immutable 列 + Shuffle / Draw API」、Phase 1 Domain)。プールサイズ・初期値は `IGameConfig.DdpPool : IReadOnlyList<int>` で定義(M2-PR3+ で `IGameConfig` を拡張、本 ADR §6)
- `record class` + `init` + `value ?? throw` 二重ガード(ADR-0004 / ADR-0006 §M1 進行中の学び)
- 内部に `Dictionary` を持つため `Equals` / `GetHashCode` を override(順序非依存マルチセット同値、ADR-0006 §M1 進行中の学びを継承)

### 2. 持ち点の computed プロパティ

`DrowZzzGameSession` に持ち点を computed として提供する(ADR-0008 §2 の Clock と同じ「真の単一情報源は構成要素、合成値は computed」方針)。

```csharp
public IReadOnlyDictionary<PlayerId, int> TotalPoints =>
    _firstDrowsyPoints.Keys.ToDictionary(
        id => id,
        id => _firstDrowsyPoints[id] + _drawDrowsyPoints[id] + _secondDrowsyPoints[id]);
```

設計指針:
- 完全可視オラクル: `DrowZzzGameSession` 自体は隠し情報も含む完全状態(ADR-0006 §2.2)、相手視点のフィルタは Presentation 層(M5)で適用
- 自分視点では `TotalPoints[selfId]` で完全把握、相手視点では `SecondDrowsyPoints[opponentId]` のみが既知

不採用案:
- 持ち点を `PlayerState` に独立フィールドとして持たせる → Clock 設計と矛盾、DRY 違反、ADR-0006 §2.2 で確定した「FDP / DDP / SDP はプレイヤーごとのスカラを `IReadOnlyDictionary<PlayerId, int>` で session に集約」原則を維持
- 持ち点 = SDP のみ → ADR-0006 既存 FDP / 本 ADR DDP との合算ロジックがコードから消えて勝敗判定が SDP 単独に偏り、戦略の非対称性が薄れる

### 3. DDP プールの構造と保持

```csharp
// IGameConfig.cs (M2-PR3+ で拡張)
public interface IGameConfig
{
    IReadOnlyList<int> FdpPool { get; }  // 既存 (ADR-0006)
    IReadOnlyList<int> DdpPool { get; }  // 新規: -30..+30 で 5 刻み × 3 枚 = 39 枚相当の値列
}
```

`IGameConfig.DdpPool` は「13 種 × 3 枚」の構造で 39 要素を持つ静的設定(`StubGameConfig` のデフォルト値で初期化)。`StartGameUseCase` で session に渡す際に `Pile.Shuffle(IRandomSource)` でランダム順に整列し、`DrowZzzGameSession.DdpPool` として保持する。

DDP 抽選時は `session.DdpPool.Draw()` で先頭から 1 枚を取得し、`session = session with { DdpPool = remaining }` で残プールを更新。プレイヤーの DDP 累積値に加算する。

### 4. DDP 抽選タイミングと進行ロジック

抽選タイミング: **Round 5 / 9 / 13 / 17 / 21 の開始時**(計 5 回、23:00 / 01:00 / 03:00 / 05:00 / 07:00 に相当)。

実装方式(M2-PR3+ で詳細 JIT、本 ADR では設計骨子のみ確定):

| 案 | 説明 | 評価 |
| ---- | ---- | ---- |
| 案 A: `EndTurnAction.Apply` 内でラウンド境界判定 + DDP 抽選 | 既存 EndTurn の延長で実装、ADR-0008 §2 案 X(computed Clock)と整合 | **採用** |
| 案 B: 専用 Action(`DrawDdpAction`)を追加 | フェーズ遷移が増え、Action 階層が肥大化、ADR-0006 §M1 の Action 種別 4 種を維持する方針と矛盾 | 不採用 |
| 案 C: 効果 record(`DrawDdpEffect`)として表現 | DDP 抽選はカード効果ではなく時計駆動の自動進行、`IEffect` の責務(プレイヤーのカードプレイによる発動)と齟齬 | 不採用 |

採用案 A の擬似コード:

```csharp
// EndTurnAction.Apply 内(M2-PR3+ で実装、ADR-0008 §2 の computed Clock を参照)
var newTurn = gameState.Turn.Next(playerCount);
var nextSession = session with { GameState = gameState with { Turn = newTurn } };
// ターン境界 (CurrentPlayerIndex == 0 になった瞬間 = 全プレイヤー 1 巡完了) で DDP 抽選タイミングを判定
if (newTurn.CurrentPlayerIndex == 0)
{
    int newTurnNumber = nextSession.Clock.RoundNumber;  // 実装名は RoundNumber、概念は「ターン番号」
    if (IsDdpDrawTurn(newTurnNumber))  // Turn in { 5, 9, 13, 17, 21 }
    {
        nextSession = DrawDdpForAllPlayers(nextSession, _rng);
    }
}
return nextSession with { PhaseState = DrowZzzPhaseState.WaitingForDraw };
```

**ターン境界の検出根拠**: `newTurn.CurrentPlayerIndex == 0` のとき、Domain `TurnState.TurnNumber`(= フェーズ番号)は `2 × (N - 1) + 1`(= ターン N の先頭フェーズ)になっており、`Clock.RoundNumber = (TurnState.TurnNumber + 1) / 2 = N` が成立する。つまり「ターン N の先頭フェーズ開始時 = `Clock.RoundNumber == N` の瞬間」を `CurrentPlayerIndex == 0` で安全に検出できる。N>2 拡張時は `(TurnState.TurnNumber + N - 1) / N == ターン番号` への一般化が必要(`docs/todo.md` の N>2 対応 TODO で追跡)。

DDP 抽選では `IRandomSource` を `DrowZzzRule` または `EndTurnAction` の経路に注入する必要が出る(現状 `DrowZzzRule` は `IRandomSource` を持たない、ADR-0007 §3)。これは M2-PR3+ で `DrowZzzRule` constructor に `IRandomSource` を追加する破壊的変更を伴うため、ADR-0007 §3 の delta として本 ADR §6 で M3 候補へ送る判断もあり得る。実装時に JIT 判断。

### 5. 勝利条件の判定タイミング(M3 候補で本格実装)

#### 戦略上の含意(コンセプト §Context と整合)

| 時間帯 | 自分の DP に対する動き | 相手の DP に対する動き | カード効果の典型例(M2-PR3+ で具体化)|
| ---- | ---- | ---- | ---- |
| **夜**(Turn 1〜16、`IsNight`)| 上げる(寝るのを目指す)| 下げる / 抑える(寝かせまいとする)| 自分 SDP +N / 相手 SDP -N / 就寝カード(持ち点 100 で早期勝利)|
| **朝**(Turn 17〜21、`IsMorning`)| 下げる(眠気を抑える)| 上げる(眠くさせる)| 自分 SDP -N / 相手 SDP +N |

同じ「相手の SDP を +N」効果でも、夜にプレイすれば不利(相手を寝かせる = 早期勝利を許す)、朝にプレイすれば有利(相手を寝かせる = 終了時に低い持ち点を取らせない)になる。**夜 / 朝で効果の利害が反転する**設計が DrowZzz の戦術核心。

#### 判定タイミング

| 勝利種別 | 判定タイミング | 判定基準 |
| ---- | ---- | ---- |
| 早期勝利 | `PlayCardAction.Apply` 内、就寝カード(特定効果タイプを持つカード)のプレイ完了直後 | `session.Clock.IsNight && session.TotalPoints[currentPlayer] >= 100 && cardEffect is EarlyWinTrigger(就寝カード効果タイプ)`(早期勝利可能領域 = Turn 1〜16、ADR-0008 §1 `IsNight` をそのまま使用)|
| 終了時勝利 | `EndTurnAction.Apply` 内、Turn 21 完了直後(`newTurn.CurrentPlayerIndex == 0 && newTurn.TurnNumber > 21 × N` 等の境界判定) | 双方の `session.TotalPoints` を比較、低い方が勝利(= 朝まで起きていられた方が勝ち)|
| 引き分け | 終了時に両者の `TotalPoints` が等しい場合 | 仕様未確定(M3 着手 ADR で JIT)|

判定実装の責務分担:

| 責務 | 配置 | M2 / M3 |
| ---- | ---- | ---- |
| 就寝カード(早期勝利トリガー、特定効果タイプを持つカード)の effect record | `Drowsy.Application.Games.DrowZzz.Effects` 内 | M2-PR3+(1 PR = 1 effect record、ADR-0007 §6、カード ID / カード名 / effect record 名は JIT。仮称: `GoToSleepEffect` / `SleepCardEffect` 等)|
| `IGameRule.IsTerminated(session) : bool` | `DrowZzzRule` | **M3** |
| `IGameRule.GetWinner(session) : PlayerId?` | `DrowZzzRule` | **M3** |
| Round 21 完了でゲーム終了処理に入る、Round 22 への遷移をブロック | `EndTurnAction.Apply` の M3 拡張 | **M3** |

→ **M2 では DP 機構(FDP/DDP/SDP + 持ち点 computed)と SDP 変動効果 record まで**を扱い、勝敗判定の本格実装は M3。

### 6. M2 / M3 境界の再整理

ADR-0007 §8 / ADR-0008 §5 で「勝敗判定 = M3」と決めた境界を本 ADR で細分化:

| 観点 | M2 範囲 | M3 範囲 |
| ---- | ---- | ---- |
| FDP 保持 | ○(既存、ADR-0006)| — |
| DDP / SDP 保持(`DrowZzzGameSession` フィールド追加)| ○(M2-PR3+)| — |
| `TotalPoints` computed プロパティ | ○ | — |
| DDP プール構造(`IGameConfig.DdpPool`)| ○(M2-PR3+ で `IGameConfig` 拡張)| — |
| DDP 抽選タイミング判定(Round 5/9/13/17/21)| ○(M2-PR3+ で `EndTurnAction.Apply` 拡張)| — |
| SDP 変動効果(`SdpChangeEffect` 等)| ○(M2-PR3+ で 1 PR = 1 effect record)| — |
| 早期勝利トリガー effect record | ○(M2-PR3+、`session.TotalPoints` を読むのみ、勝利判定自体は M3 で IsTerminated と合体)| — |
| **`IGameRule.IsTerminated` の本格実装** | ✕ | ○ |
| **`IGameRule.GetWinner` の本格実装** | ✕ | ○ |
| **Round 21 完了でゲーム終了処理 / Round 22 への遷移ブロック** | ✕ | ○ |
| **引き分けの判定仕様** | ✕ | ○(M3 着手 ADR で JIT)|

### 6.5. 用語規約の確定と既存実装のリネーム(本 ADR 同 PR で実施)

#### 用語規約(再掲、§Context 参照)

「ターン(30 分単位)/ フェーズ(1 プレイヤー 1 行動)/ PhaseState(待ち状態)」をボードゲーム一般用語(Eurogame 寄り)に整理。

#### 本 ADR 同 PR で実施するリネーム(限定的)

| 対象 | 旧 | 新 | 理由 |
| ---- | ---- | ---- | ---- |
| Application 層型 | `DrowZzzTurnPhase`(enum)| **`DrowZzzPhaseState`** | 「フェーズ内の状態」を表す型として用語規約と整合(ユーザー指定)|
| `DrowZzzGameSession` プロパティ | `TurnPhase`(型: `DrowZzzTurnPhase`)| **`PhaseState`**(型: `DrowZzzPhaseState`)| 上記改名に追随 |
| EARS / Gherkin の用語 | 「TurnPhase」「サブターン」 | 「PhaseState」「フェーズ」 | 仕様文書も用語規約と整合(主要箇所のみ、機械的リネーム残部は別 PR)|

リネーム対象の影響範囲は実装 5 ファイル + Tests 5 ファイル + EARS 3 ファイル程度。enum 値(`WaitingForDraw` / `WaitingForPlay` / `WaitingForEndTurn`)は意味的に十分明示的なため **維持**。

#### 別 PR で実施するリネーム(`docs/todo.md` で追跡)

| 対象 | 旧 | 新案 | 理由 |
| ---- | ---- | ---- | ---- |
| Application 層プロパティ | `DrowZzzGameSession.CurrentRound` | `CurrentTurn` | 用語規約(ターン)整合 |
| Application 層プロパティ | `DrowZzzClock.RoundNumber` | `TurnNumber` | 用語規約(ターン)整合、ただし Domain `TurnState.TurnNumber` と命名衝突するため要設計再評価 |
| Domain 層意味付け | `TurnState.TurnNumber`(現状: サブターン番号)| 意味は「フェーズ番号」と再解釈(改名は ADR-0002 Domain ゲーム非依存原則と要相談)| Domain 型自体の改名は影響範囲が広いため別 ADR で慎重判断 |
| EARS / Gherkin 全体 | 「Round」「ラウンド」「サブターン」全箇所 | 「Turn」「ターン」「フェーズ」 | 仕様文書の用語統一(機械的)|

これらは ADR-0001 「合意後に書き換える」運用と整合する小規模 chore のため、M2 進行中の別 PR で機械的に実施する(本 ADR §Implementation Notes §「TODO 登録」参照)。

#### 過去 ADR(ADR-0006 / 0007)の旧用語記述は維持

ADR-0006 §M1-PR2 / §M1-PR4 等で「ラウンド」「サブターン」「`DrowZzzTurnPhase`」と書かれた箇所は、当時の意思決定記録としてそのまま維持する。読み返した際に新用語との対応がつくよう、本 ADR §Context §用語規約の対応表を参照させる。これは ADR-0001 「Accepted は不必要にいじらない」原則と整合(ADR-0007 §Related 末尾を ADR-0008 起票 PR で訂正した先例は「単純な誤記訂正」、本件は用語体系の刷新で意思決定の変更を伴うため過去 ADR の Decision 内容には触れない)。

### 7. ADR-0007 / ADR-0008 の境界訂正(本 ADR 起票 PR 同梱)

本 ADR 起票 PR で以下を訂正する(ADR-0008 §「ADR-0009 起票時の Clock 訂正」を併設):

#### ADR-0008 訂正項目

- §Context 表「総ターン数 20 → プレイ可能ラウンド数 21」
- §Context 前提箇条書き「初期配布 10 + 総 Draw 40 = 50 ≤ 56」→「初期配布 10 + 総 Draw 42 = 52 ≤ 56」(ADR-0007 §7 の数値訂正に追随)
- §1 `IsMorning` 計算式 `>= 17 and <= 20 → >= 17 and <= 21`
- §1 朝の定義「Round 17〜20 → Round 17〜21」
- §5「`RoundNumber > 20` への振る舞い → `RoundNumber > 21`、時計値 07:30 は仕様上存在しない、Round 22 への遷移は M3 IsTerminated でガード」
- §6 用語整理「ラウンド = ターン(1〜20)→ (1〜21)」、「サブターン 1〜40 → 1〜42」(N=2 × 21 ラウンド)
- §Negative「`RoundNumber > 20` → `RoundNumber > 21`」
- §Alternatives Considered 内の「`RoundNumber > 20` で例外 / 凍結」を「> 21」に
- §Related 末尾「後続: ADR-0009(M3 詳細)→ ADR-0009 (本 ADR、M2-M3 DP + 勝利条件) + ADR-0010 候補 (M3 IsTerminated 本格実装)」

#### ADR-0007 訂正項目

- §Context 「山札枯渇 / 手札 0 枚」表:
  - `MaxRoundNumber` 20 → **21**
  - 総 Draw 数 = 2 × 20 = 40 → **2 × 21 = 42**
  - 初期配布 + 総 Draw = 10 + 40 = 50 ≤ 56(余裕 6 枚)→ **10 + 42 = 52 ≤ 56(余裕 4 枚)**
- §7「現状の数値前提下では発生しない(N=2 × 20 ラウンド × 1 Draw + 初期配布 10 = 50 ≤ 山札 56)」→ 「21 ラウンド × ... = 52 ≤ 56」
- §Related 末尾「後続: ADR-0009(M3 詳細)→ ADR-0009 (本 ADR、M2-M3 DP + 勝利条件、§「山札枯渇」数値訂正同梱) + ADR-0010 候補 (M3 IsTerminated 本格実装)」

両 ADR の Status は **`Accepted` のまま維持**(訂正履歴は git 履歴で追跡、`Superseded by` 別 ADR への分離はしない、ADR-0007 §Related 末尾を ADR-0008 起票 PR で訂正した先例と同パターン)。

## Consequences

### Positive

- DP 機構(FDP / DDP / SDP)を `DrowZzzGameSession` に集約することで、戦略上の情報の非対称性(自分のみ見える / 相手にも見える)が型シグネチャから読み取れる
- 持ち点を computed プロパティとして提供することで、状態の真の単一情報源(FDP + DDP + SDP の各個別フィールド)を保ち、不整合を構造的に排除(ADR-0008 §2 案 X と同じパターン)
- DDP プールを `Pile` 型で再利用することで、Phase 1 Domain の山札ロジック(Shuffle / Draw / Equals)をそのまま活用、新規 type 追加コストを最小化
- 勝利条件を「持ち点 ≥ 100 + 特定カード」「終了時の持ち点比較」の 2 種類に確定したことで、M3 着手時の `IsTerminated` / `GetWinner` 設計が明確化
- ADR-0008 / ADR-0007 の Clock 21 ラウンド化を本 ADR 起票 PR で同梱訂正することで、git 履歴と ADR 本文の整合性を保ちつつ意思決定を一元化
- 「夜は持ち点を上げ、朝は下げる」戦略示唆が IsNight / IsMorning と勝利条件の組み合わせで自然に表現される

### Negative

- `DrowZzzGameSession` に positional フィールドが 3 つ追加される(`DrawDrowsyPoints` / `SecondDrowsyPoints` / `DdpPool`)、既存 record 構築箇所がすべて影響を受ける
  - **緩和**: M1 完成時点で `DrowZzzGameSession` 構築は `StartGameUseCase.Execute` 1 箇所に集約済(ADR-0006 §M1-PR3)。Tests fixture も `NewSession` ヘルパー 3 箇所に集中するため修正点は明確
- `IGameConfig` に `DdpPool` を追加することで ADR-0006 §1.4 の `IGameConfig` API を破壊的に変更する(既存 `StubGameConfig` 実装の更新が必要)
  - **緩和**: `StubGameConfig` は Tests / ScriptableObject 実装(M4 候補)のみ、影響範囲は限定的
- DDP 抽選で `IRandomSource` を `DrowZzzRule` / `EndTurnAction` の経路に注入する必要があり、ADR-0007 §3 で確定した「`DrowZzzRule` constructor 引数は `ICardCatalog<IEffect>` / `EffectInterpreter` のみ」が破壊される可能性
  - **緩和**: M2-PR3+ で実装着手時に JIT 判断、`DrowZzzRule` に `IRandomSource` を追加するか、`EndTurnAction` 側で別経路を作るかの選択肢を残す
- 早期勝利の「特定の効果タイプを持つカード」の具体仕様(カード ID / 効果 record 名 / 効果フィールド)が ADR-0009 段階で確定していないため、M2-PR3+ 着手時点で再度 JIT 共有が必要
  - **緩和**: ADR-0007 §6 の「1 PR = 1 effect record」運用と整合、M2 着手中の JIT 共有方式継続
- ADR-0008 / ADR-0007 を直接編集する形での訂正は、ADR-0001 「合意後に書き換える」原則の境界事例(本来は新規 ADR で `Superseded by`)
  - **緩和**: 訂正範囲が境界値の数値調整(ラウンド数 20 → 21、各種計算式の更新)に限られ、根本決定(computed Clock、`record class DrowZzzClock(int RoundNumber)`、山札 56 枚 / 初期配布 10 枚 / 1 ラウンド 1 Draw)を覆さないため、`Accepted` 直接編集が筋。git 履歴で訂正経緯を追跡可能、本 ADR §7 が訂正の根拠を提供

### Neutral

- DP 機構の保持 / 計算ロジックは ADR-0006 §2.2 で確立した「`IReadOnlyDictionary<PlayerId, int>` + cross-field 検証」パターンを継承
- SDP の値域 / 変動ルール / 早期勝利カードの具体仕様は M2-PR3+ で JIT 確定(本 ADR では「存在と公開性」のみ確定)
- 引き分けの判定仕様は M3 着手 ADR(ADR-0010 候補)で JIT 確定
- DDP プール枯渇は現状想定下で発生しない(総抽選 10 ≤ プール 39)が、将来効果で「DDP を追加抽選する」効果が登場した場合は再計算が必要(`docs/todo.md` で追跡)

## Alternatives Considered

| 案 | 不採用理由 |
| ---- | ---- |
| DP を 1 種類(SDP)のみに統一 | プロジェクトオーナー仕様(FDP / DDP / SDP の 3 種別、隠し / 公開の非対称性)と矛盾、戦略性が失われる |
| 持ち点を `PlayerState` のフィールドとして持つ | ADR-0006 §2.2 の「FDP は `DrowZzzGameSession` に集約」と整合しない、cross-field 検証の責務が分散 |
| 持ち点を独立フィールドとして保持(`TotalPoints` を init で持つ)| FDP + DDP + SDP との二重管理リスク、ADR-0008 §2 案 X(computed プロパティ)と同じ理由で computed に統一 |
| DDP プールをプレイヤーごと独立保持 | プロジェクトオーナー JIT 確定「プール共有」と矛盾、プレイヤー間で同じ値を引ける確率が変わり戦略性に影響 |
| DDP 抽選を専用 Action(`DrawDdpAction`)で行う | Action 階層が肥大化、ADR-0006 §M1 の 4 種 Action(StartGame / DrawCard / PlayCard / EndTurn)を維持する方針と矛盾、自動進行は EndTurn の延長で実装する方が自然 |
| DDP 抽選を効果 record(`DrawDdpEffect`)として実装 | DDP 抽選はカード効果ではなく時計駆動の自動進行、`IEffect` の責務(プレイヤーのカードプレイによる発動)と齟齬 |
| 早期勝利を ADR-0009 で完全実装 | カード ID / 効果詳細が未確定、ADR-0007 §6「1 PR = 1 effect record」の JIT 共有方式を維持する方が筋、M2-PR3+ で JIT |
| 勝敗判定(IsTerminated / GetWinner)を ADR-0009 で本格実装 | ADR-0007 §8 / ADR-0008 §5 で「勝敗判定 = M3」と境界を引いており、本 ADR で M3 まで取り込むとスコープが肥大化、M3 着手 ADR(ADR-0010 候補)で扱う方が筋 |
| Clock 21 ラウンド化のために ADR-0008 を `Superseded by 0009` に分離 | 境界値の数値訂正(20 → 21)であり根本決定(computed Clock、`record class`、TurnNumber 単一情報源)を覆さない、ADR-0007 §Related 末尾を ADR-0008 起票 PR で訂正した先例と同パターンで直接編集が筋 |
| DDP プールを `IReadOnlyList<int>` 等の不変列で保持し、抽選インデックスを別途追跡 | `Pile` 型(Phase 1 Domain)の Shuffle / Draw API が既に「順序付き immutable 列の Top 1 枚を抽選 → 残列」を提供しており再利用が筋、新規 type 追加コストを避ける |
| DDP 抽選タイミングを「2 時間ごと」ではなく「ラウンドごと」「夜の終わりに 1 回」等の別タイミング | プロジェクトオーナー JIT 確定「23:00 / 01:00 / 03:00 / 05:00 / 07:00 の 5 回」と矛盾、仕様外 |
| SDP を隠し情報にする | プロジェクトオーナー JIT 確定「SDP は相手にも開示」と矛盾、戦略上の情報非対称性が変わる |
| FDP / DDP / SDP の名称を本 ADR で確定 | プロジェクトオーナーから「いずれも名前の変更可能性あり」と共有済、本 ADR では仮称として確定し、変更時は別 PR / 別 ADR で扱う |

## Implementation Notes

### M2 着手 PR 群(本 ADR 起票後)

ADR-0007 §Implementation Notes §「M2 着手 PR 群」と統合して再整理:

1. **本 ADR-0009 起票 PR**(独立、本 PR):
   - `docs/adr/0009-m2-m3-dp-and-victory-conditions.md` 新規
   - `docs/adr/0008-m2-drowzzz-clock-and-night-morning.md` 訂正(§7 ADR-0008 訂正項目)
   - `docs/adr/0007-m2-detail-card-effects.md` 訂正(§7 ADR-0007 訂正項目)
   - `docs/adr/README.md` インデックス追加
   - `CLAUDE.md` §11 確立済 ADR 列に「ADR-0009」追加
   - `docs/todo.md` に「DDP プール枯渇可能性チェック」「早期勝利カード仕様 JIT 確定待ち」TODO 追加

2. **M2-PR2 実装 PR**(ADR-0008 / 本 ADR マージ後):
   - `DrowZzzClock.cs` 新規(`record class DrowZzzClock(int RoundNumber)` + Hour / Minute / IsNight / IsMorning、本 ADR §7 で訂正された IsMorning 範囲 17〜21 を反映)
   - `DrowZzzGameSession.cs` に `Clock` computed プロパティ追加
   - `docs/specs/games/drowzzz/clock.{md,feature}` 新規(DZ-089〜)
   - 単体テスト + 統合テスト
   - **DP 機構は M2-PR2 では実装しない**(本 ADR で確定した DDP / SDP / DdpPool / TotalPoints は M2-PR3+ で順次追加)

3. **M2-PR3 実装 PR**(`DrowZzzGameSession` 拡張):
   - `DrowZzzGameSession.cs` に `DrawDrowsyPoints` / `SecondDrowsyPoints` / `DdpPool` フィールド追加 + cross-field 検証 + `TotalPoints` computed
   - `IGameConfig.cs` に `DdpPool : IReadOnlyList<int>` 追加 + `StubGameConfig` 拡張
   - `StartGameUseCase.cs` で session 構築時に `DdpPool = Pile.Shuffle(config.DdpPool, rng)` で初期化、`DrawDrowsyPoints` / `SecondDrowsyPoints` = 全プレイヤー 0
   - 単体テスト + 統合テスト(DP 構造、TotalPoints 計算)
   - EARS / .feature(`docs/specs/games/drowzzz/dp-mechanism.{md,feature}` 新規、DZ-XXX〜)

4. **M2-PR4 実装 PR**(DDP 自動抽選機構):
   - `EndTurnAction.Apply` 拡張: Round 5/9/13/17/21 開始時の DDP 抽選ロジック
   - `IRandomSource` の `DrowZzzRule` / `EndTurnAction` 経路への注入(JIT 判断)
   - 統合テスト(5 回の抽選タイミング、各プレイヤーの DDP 累積、プール残量)

5. **M2-PR5 以降**(個別効果 record):
   - 1 PR = 1 effect record(ADR-0007 §6)、SDP を変動させる効果 / 「夜だけ / 朝だけ発動」効果 / 早期勝利トリガー効果
   - JIT 共有方式継続

6. **M3 着手 ADR(ADR-0010 候補)**:
   - `IGameRule.IsTerminated` / `GetWinner` の本格実装
   - Round 21 完了でゲーム終了処理 / Round 22 への遷移ブロック
   - 引き分けの判定仕様
   - `MaxRoundNumber = 21` を `IGameConfig` に移行(現状コード内ハードコード予定)

### 影響範囲(M2-PR3 で同時更新するファイル、本 ADR 起票時点での見込み)

| ファイル | 変更 |
| ---- | ---- |
| `Application/Games/DrowZzz/DrowZzzGameSession.cs` | `DrawDrowsyPoints` / `SecondDrowsyPoints` / `DdpPool` フィールド追加 + cross-field 検証拡張 + `TotalPoints` computed + Equals / GetHashCode 更新 |
| `Application/Games/DrowZzz/StartGameUseCase.cs` | DP 初期化(SDP = 0、DdpPool = Shuffle、DDP = 0)|
| `Domain/Configuration/IGameConfig.cs` | `DdpPool : IReadOnlyList<int>` 追加 |
| `Tests/Application.Tests/Stubs/StubGameConfig.cs` | `DdpPool` デフォルト値(13 種 × 3 枚 = 39 要素)|
| `Tests/Application.Tests/Games/DrowZzz/DrowZzzGameSessionTests.cs` | DP / TotalPoints テスト追加 |
| `Tests/Application.Tests/Games/DrowZzz/StartGameUseCaseTests.cs` | DP 初期値テスト追加 |
| `docs/specs/games/drowzzz/dp-mechanism.{md,feature}`(新規)| DP 機構の EARS / Gherkin |
| `docs/specs/domain/configuration/game-config.md` | `DdpPool` 仕様追加 |

### 要件 ID prefix(M2-M3 範囲)

ADR-0007 §「要件 ID prefix」を継承し、本 ADR の効果対象範囲では以下を新規採番開始:

| Prefix | 範囲 | 配置 |
| ---- | ---- | ---- |
| `DZ-` | DP 機構 / 持ち点 / DDP 抽選 / 勝利条件 | `docs/specs/games/drowzzz/dp-mechanism.md` ほか |
| `CFG-` | `IGameConfig.DdpPool` の振る舞い | `docs/specs/domain/configuration/game-config.md`(既存 CFG-001〜の続き) |

M2-PR1 完成時点で APP- は 001〜038、DZ- は 001〜088。ADR-0008 関連の `clock.md`(DZ-089〜)が M2-PR2 で採番開始、M2-PR3 以降で本 ADR 関連の DP 機構が続番採番。

### TODO 登録(本 ADR 起票 PR 同梱)

`docs/todo.md` に以下を追加:

1. **DDP プール枯渇可能性チェック**(`priority: medium`):本 ADR 起票時点では「5 回 × N=2 = 10 枚抽選 ≤ プール 36 枚で余裕 26 枚」と書いていたが、計算誤記で正しくは「プール 39 枚で余裕 29 枚」(M2-PR4 PR で訂正)。将来「DDP を追加抽選する効果」が登場した場合に再計算が必要。M2 各 PR で効果が DDP 抽選回数を増やす変動を含む場合、Self-Review で「総抽選 ≤ プール 39」確認
2. **早期勝利カードの仕様 JIT 確定待ち**(`priority: medium`):本 ADR では「特定の効果タイプを持つカード」と保留。M2-PR3+ で effect record 着手時にプロジェクトオーナーから JIT 共有予定。カード ID / カード名 / 効果フィールド / 効果意味を確定する
3. **DDP / SDP / FDP の正式名変更可能性**(`priority: low`):プロジェクトオーナーから「いずれも名前の変更可能性あり」と共有済。M2 中の任意のタイミングで JIT 確定。識別子(record / フィールド名)の変更を伴う場合は別 PR で機械的リファクタ
4. **`IRandomSource` の `DrowZzzRule` / `EndTurnAction` 経路への注入判断**(`priority: medium`):M2-PR4(DDP 抽選機構)着手時に JIT 判断。ADR-0007 §3 で確定した「`DrowZzzRule` constructor 引数は `ICardCatalog<IEffect>` / `EffectInterpreter` のみ」を破壊する場合は別 ADR 候補

### Phase 2 の進捗バナー更新(本 ADR 起票 PR では行わない)

ADR-0007 / ADR-0008 と同じく、本 ADR 起票 PR では `README.md` / `CLAUDE.md` §11 の Phase 2 バナーは「M2 着手中」表示のまま維持し、CLAUDE.md §11 の確立済 ADR 列に「ADR-0009」を追加するのみ。M2 完成 PR(M2-PR-N)で「M2 完成」に切り替える。

### M2-PR3 完成記録(2026-05-11、SDP 機構 + 最初の効果カードを実装)

**完成 PR**: PR #35 `feat(app): SDP 機構 + 効果 record とカード「コップ一杯の脅威」を実装 (M2-PR3)`(merged `d03c135`、ADR-0007 / ADR-0008 §M2-PR3 完成記録と相互参照)

#### Definition of Done 達成項目(ADR-0009 で確定済の仕様のうち、M2-PR3 範囲)

| スコープ項目 | 達成状況 | 備考 |
| ---- | ---- | ---- |
| SDP(`SecondDrowsyPoints`)プロパティを `DrowZzzGameSession` に追加 | ✓ | 初期値 0、公開情報、cross-field 検証(`Players` キー集合一致)、Equals/GetHashCode で順序非依存マルチセット同値 |
| `StartGameUseCase` で SDP を全プレイヤー 0 に初期化 | ✓ | DZ-105 |
| `TotalPoints(PlayerId)` 計算メソッド | ✓ | 本 ADR §「持ち点」の「FDP + DDP + SDP」のうち **FDP + SDP のみ実装**(DDP は M2-PR4 で加算予定、本 ADR §4「DDP 抽選タイミング」未実装) |
| SDP 0 floor なし(負値許容) | ✓ | DZ-109、本 ADR「持ち点低い方が勝ち」と整合する判断 |
| カード No.01「コップ一杯の脅威」(2 枚) | ✓ | 夜=甲SDP-4/ドロー1/乙SDP-10、朝=甲SDP-4/乙SDP+10、本 ADR §「戦略示唆」夜=寝かせまい/朝=眠くさせる の最初の体現 |
| DDP 機構(プール 39 枚 / Turn 5/9/13/17/21 自動抽選) | **✗ M2-PR3 範囲外** | M2-PR4 で別途実装、本 ADR §3 / §4(本 ADR 起票時「36 枚」と書いていたが計算誤記、M2-PR4 PR で 39 枚に訂正同梱) |
| `IGameRule.IsTerminated` / `GetWinner` 本格実装 | **✗ M3 範囲** | 本 ADR §5「勝利条件の判定タイミング」、M3 ADR(候補)で扱う |

#### 本 ADR 内で確定 / 維持された JIT 仕様(M2-PR3 で実装に反映)

| §項目 | M2-PR3 での扱い |
| ---- | ---- |
| §「DP 種別」§SDP(初期値 0 / 公開 / 行動で変動) | ✓ 実装済 |
| §「持ち点」(= FDP + DDP + SDP の合計) | △ FDP + SDP のみ実装、DDP は M2-PR4 |
| §「戦略示唆」(夜=寝かせまい / 朝=眠くさせる) | ✓ 「コップ一杯の脅威」夜・朝効果で体現、`TimeOfDayBranchEffect`(ADR-0008 §8 JIT 確定)経由 |
| §「早期勝利の論理式」(`session.Clock.IsNight && TotalPoints >= 100 && EarlyWinTrigger`) | ✗ M3 範囲(本 ADR §5、`EarlyWinTriggerEffect` も未実装) |

#### M2-PR3 進行中の JIT 共有(本 ADR が確定していなかった事項を追加確定)

- **カード「コップ一杯の脅威」(No.01、2 枚)の効果数値**: プロジェクトオーナーが本 PR 着手時に JIT 共有(夜は「使用者の SDP -4 / 使用者がカードを 1 枚ドロー / 被使用者の SDP -10」/ 朝は「使用者の SDP -4 / 被使用者の SDP +10」、数字は「使用者 / 被使用者」の順、サブ名「コーヒー」「ホットミルク」は最終的に排除)。これは本 ADR §「DP 種別」の数字感(SDP 範囲)の最初の実例となり、後続カードの数値設計の暗黙基準となる
- **使用者 / 被使用者の用語**: プロジェクトオーナー用語「甲」「乙」を踏襲しつつ、コード identifier は英語の `Self` / `Opponent`(`SdpTarget` enum)に統一(ADR-0006 §1.1 / CLAUDE.md §1 識別子英語規約と整合)

### M2-PR4 完成記録(2026-05-11、DDP 機構 + 自動抽選機構 + ADR 計算誤記訂正同梱)

**完成 PR**: PR #37 `feat(app): DDP 機構 + DdpPool 値オブジェクト + 自動抽選機構を実装 (M2-PR4)`(merged `84966ef`、ADR-0007 §M2-PR4 完成記録と相互参照)

#### Definition of Done 達成項目(本 ADR で確定済の仕様のうち、M2-PR4 範囲)

| スコープ項目 | 達成状況 | 備考 |
| ---- | ---- | ---- |
| `DrowZzzGameSession.DrawDrowsyPoints`(DDP)プロパティを追加 | ✓ | 初期値 0、隠し情報、cross-field 検証(`Players` キー集合一致)、Equals/GetHashCode で順序非依存マルチセット同値(seed=1 で SDP との XOR 衝突回避)、DZ-128 / DZ-130〜132 / DZ-134〜135 / DZ-137 |
| `DrowZzzGameSession.DdpPool`(`DdpPool` 値オブジェクト)プロパティを追加 | ✓ | プレイヤー間共有、本 ADR §3 構造、DZ-129 / DZ-133 / DZ-136 |
| 専用 `DdpPool` 値オブジェクト新設(Application 層) | ✓ | 本 ADR §3 では「Pile 型を再利用」と書いていたが、`Pile` は `CardId[]` 専用で整数プールには semantic 違反のため `Drowsy.Application.Games.DrowZzz.DdpPool` を新設、Pile と同 API パターン(Shuffle/Draw/Equals 順序付きシーケンス同値)、DZ-146〜152 |
| `DdpPoolConstants`(L2 const 集約) | ✓ | CLAUDE.md §9 マジックナンバー禁止に従い MinValue/MaxValue/Step/CopiesPerValue/DistinctValueCount/TotalPoolSize/DrawRounds を集約、`DrowZzzClockConstants` と同パターン |
| `IGameConfig.DdpPool` 追加(`StubGameConfig` デフォルト 39 要素) | ✓ | CFG-103、DZ-154、本 ADR §3 で追加確定済の Domain interface 拡張 |
| `StartGameUseCase` で DDP を 0 初期化 + DdpPool を Shuffle | ✓ | DZ-139 / DZ-140、`new DdpPool(_config.DdpPool).Shuffle(_rng)` |
| `EndTurnAction.Apply` で Turn 5/9/13/17/21 開始時の自動抽選 | ✓ | 本 ADR §4 採用案 A、ターン境界(`CurrentPlayerIndex == 0`)+ 新ターン番号 ∈ DrawRounds で N=2 枚抽選、DZ-141(5 ケース)/ DZ-142(5 ケース)/ DZ-143 / DZ-144 |
| `TotalPoints(PlayerId)` を 3 項合計(FDP + DDP + SDP)に拡張 | ✓ | 本 ADR §「持ち点」整合、M2-PR3 段階の 2 項合計から拡張、DZ-138(4 ケース)、`dp-mechanism.md` DZ-103 も整合更新 |
| DDP 0 floor なし(負値許容) | ✓ | DZ-137、SDP と同パターン、本 ADR「持ち点低い方が勝ち」と整合 |
| `DrowZzzRule` constructor は 2 引数(ADR-0007 §3)を維持 | ✓ | 本 ADR §4 で挙げた「rng を Rule に注入する案」は本 PR で「`StartGameUseCase` の事前 Shuffle で十分」と再評価し採用しない、ADR-0007 §3 のシグネチャを破壊せず維持 |
| `IGameRule.IsTerminated` / `GetWinner` 本格実装 | **✗ M3 範囲** | 本 ADR §5、ADR-0010 候補で扱う |
| `EarlyWinTriggerEffect`(就寝カード効果型) | **✗ M3 範囲** | 本 ADR §5、ADR-0010 候補で扱う |
| 引き分けの判定仕様 | **✗ M3 範囲** | 本 ADR §6、M3 着手 ADR で JIT |

#### M2-PR4 進行中の JIT 確定・訂正同梱

##### 1. DDP プール枚数の計算誤記訂正(36 → 39)

- **経緯**: 本 ADR 起票時 §「DDP プールの構造」で「13 種 × 3 枚 = 36 枚」「総抽選 10 枚 ≤ プール 36 枚で余裕 26 枚」と書いていたが、数学的に 13 × 3 = 39 で**計算誤記**
- **発覚**: 実装着手後の NUnit テスト失敗(`Expected 36, but was 39`)で検出
- **JIT 確認**: プロジェクトオーナー JIT 確認(2026-05-11)で「39 枚が正、ADR 表記を訂正」と確定
- **訂正範囲**: 本 PR で ADR-0009 / `docs/adr/README.md` / 仕様 / 実装 xmldoc / テスト / `docs/todo.md` の「36 枚」を一括 39 に訂正同梱
- **訂正運用**: 経緯は「起票時 36 枚と書いた」注記で各箇所に残す(ADR-0001「Accepted 直接編集」運用、ADR-0007 / ADR-0008 の境界訂正同梱と同パターン)

##### 2. `DrowZzzRule` constructor への `IRandomSource` 注入を採用しない判断

- **判断**: 本 ADR §4 で「rng を Rule に注入する案」が JIT 判断候補とされていたが、本 PR で**不採用**と確定
- **根拠**: `StartGameUseCase` で `DdpPool` を 1 回事前 Shuffle 済のため Rule 内 rng は不要と再評価
- **影響**: ADR-0007 §3「`DrowZzzRule` constructor 引数は `ICardCatalog<IEffect>` / `EffectInterpreter` のみ」を破壊せずに済む
- **記録**: `dp-mechanism-ddp.md` §「設計判断」§「`DrowZzzRule` constructor」に経緯記録

##### 3. `DdpPool` 専用値オブジェクトの新設(Pile 流用案を不採用)

- **判断**: 本 ADR §3 では「`Pile` 型を再利用」と書いていたが、本 PR で**専用型新設**と確定
- **根拠**: `Pile` は `CardId[]` 専用で整数プール (-30〜+30) には semantic 違反のため
- **配置**: Application 層に `Drowsy.Application.Games.DrowZzz.DdpPool` を新設(`Pile` と同 API パターン: Shuffle / Draw / Equals / GetHashCode)
- **記録**: `dp-mechanism-ddp.md` §「設計判断」§「`DdpPool` 値オブジェクト」に根拠記録

#### code-reviewer subagent 反映(警告 4 / 提案 5 → 7 件反映)

警告(W):

- **W-1**: `dp-mechanism-ddp.md` §「設計判断」§「`DrowZzzRule` constructor」で「採用する」と書いていた誤記を「採用しない判断」に訂正(実装と整合)
- **W-2**: `DdpPoolTests` の `IdentityRandom` 説明を「`maxExclusive - 1` を返す → Fisher-Yates で `j=i` = no-op」に正確化
- **W-3**: `DrowZzzRuleTests` の `[Category]/[Property]` を `[TestCase]` の前に配置(NUnit 標準慣例)
- **W-4**: `IGameConfig.cs` の `DdpPool` xmldoc から Application 層具体クラス名(`DdpPoolConstants.BuildDefaultPool`)を削除

提案(P)反映分:

- **P-1**: TestName から冗長な時刻表現(例: 「23:00 23時抽選」)を簡素化
- **P-2**: `Assert.Multiple` 採用根拠コメントを追記(後に Unity NUnit 制約で個別 `Assert.That` 並列に変更)
- **P-3**: テストパラメータ単位(`TurnNumber` vs `Round`)を Given コメントで明示

#### Unity Test Framework 制約に伴う実装上の調整

- **問題**: Unity Test Framework の NUnit は `Assert.Multiple` 未対応
- **対応**: DZ-141/142/143/144 の複合不変条件検証は個別 `Assert.That` を並列に並べる(最初の失敗で停止)
- **記録**: `DrowZzzRuleTests` 内コメントで「1 ドメインイベントの複合不変条件」採用根拠と Unity NUnit 制約を明記

#### NUnit Property 増加(実測)

| ファイル | 追加 Property 数 | 内訳 |
| ---- | ---- | ---- |
| `DdpPoolTests`(新規) | 9 | DZ-148〜152 |
| `DrowZzzGameSessionTests` | 12 | DZ-130〜138 |
| `DrowZzzRuleTests` | 4 | DZ-141 / DZ-142 / DZ-143 / DZ-144(各 1 メソッド、DZ-141/142 は `[TestCase]` 5 ケース展開) |
| `StartGameUseCaseTests` | 4 | DZ-139 × 2 / DZ-140 × 2 |
| `StubGameConfigTests`(新規) | 4 | DZ-154(4 ケース) |
| **合計** | **+33 件** | TestCase 展開で実行ケースは +41 |

### M2-M3 完成記録の追記タイミング(後続 PR で実施)

M2-PR3 / M2-PR4 完成記録は §直上に追記済。**M2-PR5(継続影響 + カード No.02「緑の侵攻」、PR #39 / merged `ffc72f2`、2026-05-12)** は本 ADR のスコープ(DP 機構)に直接の変更が無いため本 ADR への完成記録追記は無し(ADR-0007 §M2-PR5 完成記録を参照)。M2-PR6+ (後続効果カード) / M3-PR (IsTerminated / GetWinner) 完成時に、それぞれ本 ADR or 関連 ADR に追記する(ADR-0007 / ADR-0008 §M2 完成記録の追記タイミング と同パターン)。本 ADR は DP 機構 + 持ち点 + 勝利条件のスコープに閉じ、M2 全体の完成記録は ADR-0007 §M2 完成記録(全体)で集約する。

## Related

- 前提: [ADR-0001 ADR Operations](0001-adr-operations.md)
- 前提: [ADR-0002 Phase 1 Domain 拡張の集約境界と概念モデル](0002-phase1-domain-boundaries.md) — Domain ゲーム非依存原則、record + init + with、Dictionary を持つ record の Equals override
- 前提: [ADR-0003 TODO 運用](0003-todo-operations.md) — 本 ADR §Implementation Notes §「TODO 登録」
- 前提: [ADR-0004 IsExternalInit polyfill](0004-init-setter-polyfill.md) — `record class` + `init` パターンを DP 機構でも利用
- 前提: [ADR-0005 Phase 2 Roadmap](0005-phase2-roadmap-drowzzz.md) — Phase 2 / M2 の位置づけ
- 前提: [ADR-0006 M1 詳細](0006-m1-detail-application-interfaces.md) §2.2 / §M1-PR3 — 既存 `FirstDrowsyPoints`(FDP)構造、`DrowZzzGameSession` の cross-field 検証パターン、`IGameConfig.FdpPool` 既存 API
- 前提: [ADR-0007 M2 詳細](0007-m2-detail-card-effects.md) §1 / §3 / §6 / §8 — 効果インフラ、JIT 共有方式、M2 範囲外リスト、本 ADR §7 で §「山札枯渇」数値訂正同梱
- 前提: [ADR-0008 M2 DrowZzzClock 概念](0008-m2-drowzzz-clock-and-night-morning.md) §1 / §2 / §5 — Clock 値オブジェクト、computed プロパティ、本 ADR §7 で 21 ラウンド化境界訂正同梱
- 関連: [`docs/specs/games/drowzzz/clock.md`](../specs/games/drowzzz/clock.md) — M2-PR2 で新規作成、Clock 仕様 21 ラウンド化を反映
- 関連: [`docs/specs/games/drowzzz/dp-mechanism.md`](../specs/games/drowzzz/dp-mechanism.md) — M2-PR3+ で新規作成、DP 機構 / 持ち点 / DDP 抽選の仕様
- 関連: [`docs/specs/domain/configuration/game-config.md`](../specs/domain/configuration/game-config.md) — M2-PR3+ で `DdpPool` 仕様追加
- 関連規約: [`CLAUDE.md`](../../CLAUDE.md) §5 アーキテクチャ依存ルール / §6 テスト方針 / §9 定数管理方針 / §11 ADR 運用 / §12 TODO 追跡
- 後続: M2-PR2(Clock 実装)→ M2-PR3(DP 機構)→ M2-PR4(DDP 抽選)→ M2-PR5+(個別効果 record、JIT 共有方式)→ M2 完成 PR
- 後続: ADR-0010 候補(M3 詳細 — `IsTerminated` / `GetWinner` 本格実装、Round 21 完了処理、引き分け仕様)
