# ADR-0010: M3 詳細 — ゲーム終了判定と勝者決定(`IsTerminated` / `GetWinner` / 早期勝利 / 引き分け)

| 項目 | 値 |
| ---- | ---- |
| Status | Accepted |
| Date | 2026-05-12 |
| Decider | プロジェクトオーナー |

## Context

ADR-0005 で Phase 2 のロードマップを M1〜M5 に分割し、ADR-0006(M1)/ ADR-0007(M2 効果)/ ADR-0008(M2 Clock)/ ADR-0009(M2-M3 DP + 勝利条件)で各マイルストーンの詳細を確定してきた。M2 までで以下が完成している(2026-05-12 時点):

- ターン進行 + カードプレイ最小骨格(M1)
- カード効果インフラ + サブセット 2 枚(No.01 / No.02、M2-PR1〜PR5)
- DrowZzzClock + 夜・朝フェーズ
- FDP / DDP / SDP 機構 + `TotalPoints` 3 項合計 computed プロパティ

ただし **ゲーム終了判定 / 勝者決定の本格実装は ADR-0009 §5 / §6 で M3 範囲と整理** され、未着手の状態にある。具体的に未確定 / 未実装の項目:

| 項目 | 現状 | 必要な確定事項 |
| ---- | ---- | ---- |
| `IGameRule.IsTerminated(session)` | interface 未定義 | API シグネチャ / 戻り値型 / 終了条件の評価方式 |
| `IGameRule.GetWinner(session)` | interface 未定義 | API シグネチャ / 戻り値型(`PlayerId?` 想定)/ 終了前呼び出し時の挙動 |
| 早期勝利トリガー | コンセプトのみ(ADR-0009 §5)| 効果 record の設計 / 評価タイミング / Session への結果反映方式 |
| Round 21 完了処理 | `EndTurnAction.Apply` で `newTurn.CurrentPlayerIndex == 0 && newRound > 21` の境界判定が未実装 | 検出方式 / Outcome の確定方式 |
| Round 22 への遷移ブロック | 未実装(現状 Clock は computed なので Round 22+ も数学的計算は通る、ADR-0008 §5 過渡的範囲)| `IsLegalMove` での termination 検出 / Action 拒否方式 |
| 引き分け仕様 | 未確定(ADR-0009 §6「M3 着手 ADR で JIT」と保留)| TotalPoints 等値時の扱い / tiebreaker の有無 |
| `MaxRoundNumber` の所在 | `DrowZzzClockConstants.MaxRoundNumber = 21` 存在、`IGameConfig` には未追加(コメントで「M3 着手 PR で追加」と予約)| 所在の最終確定(2 箇所重複を避ける) |
| 早期勝利閾値の所在 | コンセプト「持ち点 ≥ 100」のみ、定数集約先未確定 | 定数の置き場所 / 命名 |

本 ADR は **M3 詳細(ゲーム終了 + 勝者決定)の設計確定** を行う。ADR-0006 / ADR-0007 / ADR-0009 で確立した「JIT 共有 + 1 PR = 1 論理変更」の運用を継続。

### プロジェクトオーナーから JIT 共有された前提(2026-05-12 時点)

ADR-0009 §「コンセプト」+ §5 / §6 で確定済の仕様前提は以下:

| 観点 | 値 |
| ---- | ---- |
| プレイヤー数 | N=2(M1 から継続) |
| 早期勝利の条件 | 夜の間(Round 1〜16、`Clock.IsNight`)+ 持ち点 ≥ 100 + 「就寝カード」(特定効果タイプを持つカード)をプレイ |
| 終了時勝利の条件 | Round 21 完了直後(Round 22 への遷移時点)、持ち点が低い方が勝利(= 朝まで起きていられた方) |
| 早期勝利可能領域 | Round 1〜16(`IsNight = true`)。Round 17 以降(`IsMorning`)は早期勝利不可、終了時勝利のみ |
| 持ち点 | FDP + DDP + SDP の 3 項合計(ADR-0009、computed プロパティ `TotalPoints`)|
| 引き分け仕様 | **本 ADR で確定**(プロジェクトオーナー JIT 確認、2026-05-12:両者の TotalPoints が等しい場合は引き分け、tiebreaker は設けない)|

## Decision

### 1. `IGameRule<TAction, TSession>` への API 追加

`Drowsy.Application/IGameRule.cs` の汎用 interface に **2 メソッドを追加**(M1 で導入した最小 API への第一回拡張):

```csharp
namespace Drowsy.Application;

public interface IGameRule<TAction, TSession>
    where TAction : IGameAction
{
    bool IsLegalMove(TSession session, TAction action);
    TSession Apply(TSession session, TAction action);

    // M3 で追加:
    bool IsTerminated(TSession session);
    PlayerId? GetWinner(TSession session);
}
```

#### API 契約

| メソッド | 戻り値 | 契約 |
| ---- | ---- | ---- |
| `IsTerminated(session)` | `bool` | ゲームが終了しているなら `true`。純関数(副作用なし、session 同一なら同結果)|
| `GetWinner(session)` | `PlayerId?` | (a) `IsTerminated == false` で呼ばれた場合は `InvalidOperationException` を投げる、(b) 勝者がいる場合はその `PlayerId` を返す、(c) **引き分けの場合は `null` を返す** |

`GetWinner` の戻り値 `null` が「未終了」と「引き分け」の両方を意味すると曖昧になるため、本 ADR では **「未終了で呼ぶこと自体が利用側の不正」とし `InvalidOperationException` で防御** する。引き分け判定は `IsTerminated == true && GetWinner == null` の組み合わせで表現する(呼び出し側は必ず `IsTerminated` を先に確認する契約)。

#### 不採用の API シグネチャ

| 案 | 不採用理由 |
| ---- | ---- |
| `GameOutcome? GetOutcome(session)` 単一メソッドで `IsTerminated` も統合 | M1 で確立した「最小 API」精神に反する 1 段階の凝集化、`IGameRule` の generic な性質から見て個別メソッドの方が他ゲーム実装でも素直 |
| `bool TryGetWinner(session, out PlayerId? winner)` | `Try` パターンは「失敗が想定内」用途、ゲーム終了判定は contract 違反であり例外の方が筋 |
| `GetWinner` が未終了で `null` を返す | 引き分けと未終了が区別できなくなる |

### 2. `GameOutcome` 値オブジェクト(Domain 層、新規)

ゲーム終了状態を表現する値オブジェクトを `Drowsy.Domain.Game` 名前空間に新設:

```csharp
namespace Drowsy.Domain.Game;

public abstract record GameOutcome;
public sealed record WinnerOutcome(PlayerId Winner) : GameOutcome;
public sealed record DrawOutcome : GameOutcome;
```

#### 配置の判断(Domain か Application か)

| 観点 | 採用判断 |
| ---- | ---- |
| Domain に置く | **採用**。「ゲームに勝者がいる / 引き分け」はゲーム非依存の汎用概念で、`IGameRule` の generic 性質と整合(ADR-0002 Domain ゲーム非依存原則) |
| Application に置く | 不採用。`DrowZzz` 固有の終了概念ではない |
| `PlayerId?` のみで表現 | 不採用。引き分けと未終了の区別、将来のリッチ化(終了理由 / 最終スコア等の付帯情報)に拡張余地が必要 |

#### `GameOutcome` の不変条件

- `WinnerOutcome.Winner` は **null 不可**(positional record + 二重ガード null 防御、`PlayCardAction` / `PlayerInfluence` と同パターン)
- `DrawOutcome` はフィールドなし(マーカー的派生型)
- `GameOutcome` 抽象 record で auto-equals が派生型ごとに正しく動く(WinnerOutcome 同士は `Winner` 値比較、DrawOutcome 同士は常に等価)

### 3. `DrowZzzGameSession.Outcome` プロパティの追加(8 引数 ctor 化、breaking change)

`DrowZzzGameSession` に `Outcome: GameOutcome?` プロパティを追加し、ゲーム終了状態を保持する。

- `Outcome == null`: ゲーム進行中
- `Outcome != null`: ゲーム終了(WinnerOutcome なら勝者あり、DrawOutcome なら引き分け)

#### コンストラクタの拡張

```csharp
// 8 引数 ctor(M2-PR5 までの 7 引数から拡張):
public DrowZzzGameSession(
    GameState gameState,
    IReadOnlyDictionary<PlayerId, int> firstDrowsyPoints,
    IReadOnlyDictionary<PlayerId, int> drawDrowsyPoints,
    IReadOnlyDictionary<PlayerId, int> secondDrowsyPoints,
    DdpPool ddpPool,
    IReadOnlyDictionary<PlayerId, IReadOnlyList<PlayerInfluence>> influences,
    DrowZzzPhaseState phaseState,
    GameOutcome? outcome)  // ← 新規追加
```

`StartGameUseCase` 経由のゲーム開始時は `outcome = null` で初期化。

#### Equals / GetHashCode の拡張

- `Outcome` の差異を Equals に反映(`null` / `WinnerOutcome` / `DrawOutcome` の三値比較)
- GetHashCode に `Outcome` を合成(他フィールドとの XOR 衝突回避)

#### computed プロパティ(`IsTerminated` の薄いラッパー)

`Session` 単独で終了状態を問えるよう、`Session.IsTerminated => Outcome != null` を computed プロパティとして提供する(`DrowZzzRule.IsTerminated` の純関数性を保ったまま、UI 等からの直接参照を可能にする)。

### 4. 終了トリガーの 2 経路と Outcome 設定タイミング

#### (a) 早期勝利:`PlayCardAction.Apply` 内、`EarlyWinTriggerEffect` 評価時

| ステップ | 処理 |
| ---- | ---- |
| 1 | プレイヤーが就寝カード(`EarlyWinTriggerEffect` を効果列に持つカード)をプレイ |
| 2 | `DrowZzzRule.ApplyPlayCard` が catalog から効果列を取得、`EffectInterpreter.Apply` で順次評価 |
| 3 | `EarlyWinTriggerEffect` 評価時、`Clock.IsNight` かつ `TotalPoints[currentPlayer] >= EarlyWinScoreThreshold` を確認 |
| 4 | 条件成立: `session with { Outcome = new WinnerOutcome(currentPlayer) }` を返す |
| 5 | 条件不成立(朝にプレイ / 持ち点不足): no-op(session 不変返却)、カードプレイ自体は完了 |

#### (b) 終了時勝利:`EndTurnAction.Apply` 内、Round 21 完了検出時

| ステップ | 処理 |
| ---- | ---- |
| 1 | プレイヤーが Round 21 の最終フェーズ後 `EndTurnAction` を適用 |
| 2 | `DrowZzzRule.ApplyEndTurn` で `newTurn = gameState.Turn.Next(playerCount)` を計算 |
| 3 | 境界判定: `newTurn.CurrentPlayerIndex == 0 && nextSession.Clock.RoundNumber > MaxRoundNumber`(= 22 以上) |
| 4 | 各プレイヤーの `TotalPoints` を比較、低い方を勝者 PlayerId として確定 |
| 5 | 等値の場合: `DrawOutcome`、それ以外: `WinnerOutcome(lowerScorePlayer)` |
| 6 | `nextSession with { Outcome = outcome }` を返す |

#### 順序保証

ADR-0009 §4 で確定した DDP 抽選(Round 5/9/13/17/21 開始時)と M2-PR5 で確定した影響 Tick(自フェーズ開始時)は、終了判定よりも **前** に実行される。`ApplyEndTurn` 内の順序:

1. `gameState.Turn.Next` で新フェーズ確定
2. PhaseState を `WaitingForDraw` に更新
3. DDP 自動抽選(該当ラウンドなら)
4. 影響 Tick(新 current player の影響を FIFO 評価)
5. **Round 21 完了検出 + Outcome 設定**(新規、本 ADR)

Round 21 完了時の DDP 抽選(Round 21 開始時)+ 影響 Tick は Turn 21 の進行中に既に処理済のため、`newRound > 21` の境界で初めて Outcome が設定される構造。

### 5. `EarlyWinTriggerEffect` 効果 record(Application 層、新規)

```csharp
namespace Drowsy.Application.Games.DrowZzz.Effects;

public sealed record EarlyWinTriggerEffect : IEffect;
```

フィールドなしのマーカー的 record。閾値(100)は `DrowZzzVictoryConstants.EarlyWinScoreThreshold`(後述 §9)を `EffectInterpreter` 内で参照する形にし、effect record 自身は持たない。

#### `EffectInterpreter.Apply` での評価

```csharp
EarlyWinTriggerEffect _ => ApplyEarlyWinTrigger(session),

// 評価ロジック:
private static DrowZzzGameSession ApplyEarlyWinTrigger(DrowZzzGameSession session)
{
    if (!session.Clock.IsNight) return session;  // 朝以降は no-op
    var currentId = session.GameState.Players[session.GameState.Turn.CurrentPlayerIndex].Id;
    if (session.TotalPoints(currentId) < DrowZzzVictoryConstants.EarlyWinScoreThreshold) return session;
    return session with { Outcome = new WinnerOutcome(currentId) };
}
```

#### 不採用の設計

| 案 | 不採用理由 |
| ---- | ---- |
| `EarlyWinTriggerEffect(int Threshold)` で閾値を持つ | 閾値はカード固有ではなくゲームルール固有(全就寝カード共通)、`DrowZzzVictoryConstants` 集約が筋(CLAUDE.md §9 マジックナンバー禁止) |
| 「就寝カード」を `CardData.Attributes["isSleepCard"] = 1` で表現 | M1 から「カード効果は `IEffect` で表現、`Attributes` は属性のみ」と整理済(ADR-0006 §1.3 / ADR-0007 §1)、効果型として `EarlyWinTriggerEffect` を持つカードが就寝カード、という構造一貫 |
| `DrowZzzRule.ApplyPlayCard` で個別判定(effect record を作らない) | 「カード効果は effect record で表現」の ADR-0007 §1 設計と矛盾、新規効果種別ごとに rule が肥大化 |
| Domain 層に `EarlyWinTriggerEffect` を置く | DrowZzz 固有の概念(他ゲームに転用しない)、Application 層配置で Domain ゲーム非依存(ADR-0002)を維持 |

#### §5 の発動条件は ADR-0011 §7 で拡張(本 ADR を覆さず文脈追加)

ADR-0011(M3 詳細拡張、2026-05-12 起票)§7 で、`EarlyWinTriggerEffect` を発火させるためのゲーム文脈が拡張される:本 §5 の API(夜 + 持ち点 ≥ 100 で `Outcome = WinnerOutcome` 設定)はそのまま維持しつつ、「夢」カードを **連想で引いて + 次の自分のターン以降に + 夜効果としてプレイ + FDS ≥ 100** という多段階の発動経路が追加される。本 ADR は ADR-0011 を Superseded by にせず、効果 record 単体の評価ロジックを定義する基盤として継続有効。詳細は ADR-0011 §7 を参照。

### 6. Round 22 への遷移ブロック(IsLegalMove 拡張)

ゲーム終了後の `IsLegalMove` は **全 Action 種別に対して `false`** を返す:

```csharp
public bool IsLegalMove(DrowZzzGameSession session, DrowZzzAction action)
{
    if (session is null) throw new ArgumentNullException(nameof(session));
    if (action is null) throw new ArgumentNullException(nameof(action));

    // M3-PR1 新規: ゲーム終了後はいかなる Action も illegal
    if (session.Outcome is not null) return false;

    return action switch { /* 既存ロジック */ };
}
```

`Apply` 側でも防御的検証:`Outcome != null` の session に対する Apply 呼び出しは `InvalidOperationException`(`IsLegalMove` 違反時の既存方針と整合、ADR-0006 §3)。

#### Round 22 への到達経路の排除

ゲーム終了が起きるのは以下 2 経路のみで、いずれも Round 22 到達前:

- (a) 早期勝利: Round 1〜16 で `EarlyWinTriggerEffect` 発動 → Outcome 設定 → 以降 IsLegalMove で全 Action 拒否 → Round 17+ に進めない
- (b) 終了時勝利: Round 21 最終フェーズ後の `EndTurnAction.Apply` で `newRound > 21` を検出 → Outcome 設定 → 以降 IsLegalMove で全 Action 拒否

したがって `Clock.RoundNumber > 21` の状態が外部に観測される瞬間は (b) の `Outcome` 設定とほぼ同時で、観測されてもすぐに IsLegalMove で塞がる。ADR-0008 §5 の「過渡的範囲」(Round 22+ の no-op)は本 ADR で **明示的ガードに昇格** する。

### 7. 引き分け仕様

#### Decision: TotalPoints 等値で引き分け、tiebreaker なし

Round 21 完了直後に両プレイヤーの `TotalPoints` が等しい場合は **引き分け**(`DrawOutcome`)とする。tiebreaker(FDP / DDP / SDP の個別比較、プレイ順、その他)は設けない。

#### 不採用の tiebreaker 案

| 案 | 不採用理由 |
| ---- | ---- |
| SDP 単独比較 | 戦略次元を増やす一方で「公開情報 SDP の駆け引き」を歪める、シンプル性を優先 |
| FDP 比較(隠し情報で開始時固定)| プレイヤー操作不能な部分で勝敗が決まることになり、ゲーム性として不健全 |
| DDP 比較(隠し情報、抽選由来)| 同上、運要素で勝敗を決める tiebreaker は採用しない |
| 最後にカードを「プレイした方」「プレイしなかった方」勝ち | 行動順依存になり、Round 21 で意図的に行動を回避する meta が生まれうる |
| 「両者勝利」「両者敗北」 | 「勝者 1 名」「引き分け」の 2 値表現で十分、複雑な outcome 種別を増やさない |

引き分けは「両者が同点で朝まで起きていた」というコンセプト的に意味のある結末。tiebreaker を入れるとこの意味が薄れる。

#### `DrowZzzRule.GetWinner` の戻り値

| 状態 | 戻り値 |
| ---- | ---- |
| `Outcome is null`(未終了)| `InvalidOperationException` を投げる |
| `Outcome is WinnerOutcome(p)` | `p` を返す |
| `Outcome is DrawOutcome` | `null` を返す |

### 8. `MaxRoundNumber` の所在:`DrowZzzClockConstants` に維持、`IGameConfig` に追加しない

#### Decision

`MaxRoundNumber = 21` は **`DrowZzzClockConstants` の既存定数として維持** し、`IGameConfig` には **追加しない**。

#### 根拠

ADR-0009 §5 表の「`MaxRoundNumber` を `IGameConfig` に移行」記述および `IGameConfig.cs` の `// 後続追加予定: int MaxRoundNumber { get; }` コメントは、Clock 仕様が完全に設計される前の予約であり、本 ADR で再評価:

| 観点 | `IGameConfig` 追加 | `DrowZzzClockConstants` 維持 |
| ---- | ---- | ---- |
| Clock 仕様との結合 | 21 は時計 21:00 → 07:00(24 時間制 mod 24)に構造的に紐づく | **構造的整合(ADR-0008 §1)** |
| ゲームバランス調整可能性 | 21 を変えると Clock の Round/Hour 変換式が破綻 | **調整不可(意味的に固定値)** |
| 重複の回避 | IGameConfig と Constants で 21 を二重定義 → 不整合リスク | **単一情報源(SSOT)** |
| ScriptableObject 化(M4)の利点 | 21 を Inspector で変えても Clock が壊れるだけ | **Designer が触る価値なし** |

つまり「21」はゲームバランス調整可能値(L3、CLAUDE.md §9)ではなく **数学的・構造的不変量(L2)** に分類すべきで、`IGameConfig` ではなく `DrowZzzClockConstants` の `const int MaxRoundNumber = 21` で十分。

#### `IGameConfig.cs` のコメント訂正(本 ADR 同 PR 同梱)

```csharp
// 後続追加予定:
//   int MaxRoundNumber { get; }   // M3 着手 PR (ゲーム終了判定)
```

を以下に訂正:

```csharp
// 後続追加予定(M2 以降のカード効果実装で必要になり次第追加):
//   その他バランス調整値(現状 FdpPool / DdpPool のみ)
// 注: MaxRoundNumber は IGameConfig に追加しない判断(ADR-0010 §8、Clock 構造に紐づく
// L2 数学的不変量として DrowZzzClockConstants が単一情報源)
```

### 9. `EarlyWinScoreThreshold` の所在:新規 `DrowZzzVictoryConstants`

#### Decision

早期勝利の閾値 `100` および将来の勝利条件関連定数を集約する **新規ファイル `DrowZzzVictoryConstants`** を `Drowsy.Application.Games.DrowZzz` 名前空間に新設:

```csharp
namespace Drowsy.Application.Games.DrowZzz;

public static class DrowZzzVictoryConstants
{
    /// <summary>
    /// 早期勝利成立に必要な持ち点の閾値(夜の間にこの値以上で就寝カードをプレイすると早期勝利)。
    /// プロジェクトオーナー JIT 確定(ADR-0009 §「コンセプト」)、本 ADR で定数集約。
    /// </summary>
    public const int EarlyWinScoreThreshold = 100;
}
```

#### 配置の根拠

| 配置案 | 採用判断 |
| ---- | ---- |
| **`DrowZzzVictoryConstants`(新規)** | **採用**。「勝利条件関連」という意味的グループ(将来 tiebreaker 関連定数 / 各種閾値が増える前提) |
| `DrowZzzClockConstants` に同居 | 不採用。Clock は時刻・ラウンド変換の物理的不変量、Victory は勝利条件のゲームルール定数で意味グループが異なる |
| `EarlyWinTriggerEffect` 内 `const` | 不採用。閾値は全就寝カード共通でカード固有ではない、effect record 内固有 const は適切でない |
| `IGameConfig.EarlyWinScoreThreshold` 追加 | 不採用。100 はゲーム設計の核心値で「調整」する対象ではない(変えるとゲームバランスが根本的に変わる)、L2 不変量として constants 化が筋 |
| `DrowZzzGameSession` 内 `const` | 不採用。Session は状態保持で定数の置き場ではない |

`DrowZzzClockConstants` / `DdpPoolConstants` と同パターン(ADR-0008 / ADR-0009、L2 const 集約)で、constants は意味グループ単位でファイル分離する方針を継続。

## Consequences

### Positive

- M3 の完成定義(ゲーム終了 + 勝者決定)が API レベルで確定し、M3 着手 PR(M3-PR1)が即座に始められる
- `IGameRule` への M3 拡張(IsTerminated / GetWinner)が generic interface に閉じて他ゲームでも素直に流用可能
- `GameOutcome`(WinnerOutcome / DrawOutcome)を Domain 層に置くことで、UI / Presenter から終了状態を直接読める(M5 で活用)
- 引き分け仕様の確定により M3 着手中に再 JIT が発生しない(ADR-0009 §6 で保留した最後の項目を本 ADR で解消)
- `MaxRoundNumber` / `EarlyWinScoreThreshold` の所在を constants で確定し、定数の重複や IGameConfig 肥大化を回避
- Round 22 への過渡的範囲(ADR-0008 §5)を本 ADR で明示的ガードに昇格、Clock の no-op 挙動への依存をなくす

### Negative

- `DrowZzzGameSession` の 7 引数 → 8 引数 ctor 化(2 度目の breaking change、M2-PR3 で 3→4 / M2-PR4 で 4→6 / M2-PR5 で 6→7 に続く 4 度目の拡張)
  - **緩和**: 既存 11 テストファイルに `Outcome` パラメータを追加する機械的修正で完結(M2-PR5 で `Inf()` ヘルパー伝播パターン確立済、同パターンで `Outcome: null` を渡す)
- `IGameRule<TAction, TSession>` の API が 2→4 メソッドに拡張(他ゲーム実装時の負担が増える)
  - **緩和**: 拡張先は generic interface のため、`IGameRule` を実装する具象クラス(現状は `DrowZzzRule` のみ)が増えるたびに 2 メソッド追加するだけ
- `Outcome` プロパティを Session に持つことで「ゲーム終了の事実」が状態として永続化される(計算で得られない)
  - **緩和**: 早期勝利は **発火イベント**(特定の play action で確定)なので状態保持が必須、計算で再現できない(ADR の §4(a)経路参照)。終了時勝利は理論上計算可能だが、両経路を統一表現する方が API が単純
- 引き分けで `GetWinner` が `null` を返す仕様により、呼び出し側が「null = 引き分け、例外 = 未終了」を区別する必要(契約上明示)
  - **緩和**: `IsTerminated` を先に呼ぶ契約を XML doc で明示、過去の `TryGet` パターン採用も検討したが「失敗が想定内」用途と整合しないため例外採用

### Neutral

- 本 ADR は ADR-0009 §5 / §6 / §「コンセプト」で予約された M3 範囲を実装可能形に落とすもので、ADR-0009 の Decision を覆さない(M3 着手 ADR、ADR-0009 §6 の予告通り)
- `EarlyWinTriggerEffect` の最初の利用カード(就寝カード No.X)は本 ADR では確定しない:カード仕様は M2-PR6+ で JIT 共有される運用(ADR-0006 / ADR-0007 と同パターン)
- 「就寝カード」というカテゴリ概念は `EarlyWinTriggerEffect` を効果列に持つカード、と暗黙定義される(ADR-0007 §1 「カード効果は IEffect で表現」と整合)
- ScriptableObject 化(M4)は本 ADR の範囲外、`EarlyWinScoreThreshold` 等の定数も M4 で SO 移行する場合は別 ADR で再評価

## Alternatives Considered

| 案 | 不採用理由 |
| ---- | ---- |
| `IGameRule.IsTerminated` / `GetWinner` を `IGameRule` でなく別 interface(`IGameTermination<TSession>`)に分離 | 役割が `IGameRule` と密結合(同じ Rule オブジェクトが Apply + 終了判定を担う)、interface 分離はテスト容易性の向上に貢献しない |
| `Session.IsTerminated` / `Session.Winner` を Session の computed プロパティで完結、Rule に終了判定を持たせない | 早期勝利は「特定の play action でトリガー」される **発火イベント** で、Session 状態だけからは「就寝カードがプレイされた瞬間か」を判定不能。発火経路を Rule が持つ方が筋(ADR-0006 §1.2 純関数 Rule 原則と整合)|
| `Outcome` を Domain `GameState` に持たせる | ADR-0002 Domain ゲーム非依存原則と矛盾(`GameOutcome` は generic だが、DrowZzz 終了タイミング判定は固有)、Application 層 `DrowZzzGameSession` 配置で Domain 純粋性を維持 |
| 引き分けを `WinnerOutcome` 派生型で表現(`DrawOutcome` を作らず `WinnerOutcome(null)` で表現)| `WinnerOutcome.Winner` の null 防御を破棄することになり、record 不変条件が壊れる(null 防御の二重ガードパターン M1 確立、ADR-0006)|
| 早期勝利 / 終了時勝利の判定タイミングを 1 箇所(`EndTurnAction.Apply`)に集約 | 早期勝利は **play 時点で確定** すべき(プレイヤーが即時勝利を体感する)、EndTurn まで遅延すると同ターン内の後続効果が誤って評価されるリスク |
| Round 21 完了処理を `IsTerminated` の computed として実装(EndTurn での Outcome 設定を行わない)| 早期勝利との二経路の統一表現が崩れる、`Outcome` プロパティが「早期勝利時のみ設定」となり API の対称性が悪化 |
| `MaxRoundNumber` を `IGameConfig` に追加(ADR-0009 §5 の予告通り) | Clock 構造との結合上「調整不可」(§8 表参照)、L2 不変量として constants 単一情報源が筋 |
| `EarlyWinScoreThreshold` を `DrowZzzClockConstants` に同居 | 意味グループが異なる(時計 vs 勝利条件)、ADR-0009 で `DdpPoolConstants` を別ファイルにした分離方針と整合 |
| tiebreaker(SDP 比較 / FDP 比較 / 行動順)を導入 | 戦略次元の追加よりシンプル性を優先(§7 参照)、引き分けは「両者が同点で起きていた」意味のある結末 |
| `IsTerminated == false` 時の `GetWinner` で `null` を返す(例外を投げない) | 引き分けと未終了が `null` で区別不能、契約曖昧 |

## Implementation Notes

### M3-PR1 着手 PR の構成(仮、進行中に調整)

1. **Domain 層**:
   - `Drowsy.Domain.Game.GameOutcome.cs`(新規): abstract record + `WinnerOutcome` / `DrawOutcome`(本 ADR §2)
   - `Drowsy.Domain.Game.GameOutcomeTests.cs`(新規): null 防御 / 値同値性 / `DrawOutcome` 同値判定
   - `Drowsy.Domain.Configuration.IGameConfig.cs`(変更): コメント訂正のみ(MaxRoundNumber 不採用判断、本 ADR §8)

2. **Application 層**:
   - `Drowsy.Application.IGameRule.cs`(変更): `IsTerminated` / `GetWinner` メソッド追加、XML doc に契約明示
   - `Drowsy.Application.Games.DrowZzz.DrowZzzVictoryConstants.cs`(新規): `EarlyWinScoreThreshold = 100`(本 ADR §9)
   - `Drowsy.Application.Games.DrowZzz.DrowZzzGameSession.cs`(変更): `Outcome: GameOutcome?` プロパティ追加、8 引数 ctor 化、`IsTerminated` computed、Equals / GetHashCode 拡張
   - `Drowsy.Application.Games.DrowZzz.DrowZzzRule.cs`(変更): `IsTerminated` / `GetWinner` 実装 + `IsLegalMove` で `Outcome != null` ガード + `ApplyEndTurn` で Round 21 完了検出 + `Outcome` 設定
   - `Drowsy.Application.Games.DrowZzz.Effects.EarlyWinTriggerEffect.cs`(新規): `IEffect` 派生、フィールドなしマーカー record(本 ADR §5)
   - `Drowsy.Application.Games.DrowZzz.Effects.EffectInterpreter.cs`(変更): `EarlyWinTriggerEffect` の case 追加、評価ロジック(`Clock.IsNight && TotalPoints >= EarlyWinScoreThreshold` で `Outcome` 設定)
   - `Drowsy.Application.Games.DrowZzz.StartGameUseCase.cs`(変更): `Outcome: null` で session を初期化

3. **テスト(Application.Tests)**:
   - `DrowZzzRuleTests` に `IsTerminated` / `GetWinner` / `IsLegalMove` の終了後ガード / `ApplyEndTurn` の Round 21 完了検出テスト追加
   - `DrowZzzGameSessionTests` に `Outcome` 値保持 / null 防御 / Equals 寄与テスト追加
   - `EarlyWinTriggerEffectTests`(新規): 夜 + 持ち点 100 で `Outcome` 設定 / 朝で no-op / 持ち点 99 で no-op / 既に終了済 session で no-op
   - 既存 11 テストファイル(M2-PR5 までで `Inf()` ヘルパー伝播済)に `Outcome: null` 引数を追加(機械的修正)

4. **EARS / Gherkin**:
   - `docs/specs/application/game-rule.md`(変更): APP- 採番で `IsTerminated` / `GetWinner` 契約を追加
   - `docs/specs/games/drowzzz/victory-conditions.md`(新規): DZ- 採番で本 ADR §1〜§7 を要件化
   - `docs/specs/games/drowzzz/effects/early-win-trigger.md`(新規): `EarlyWinTriggerEffect` 仕様
   - 既存 `docs/specs/games/drowzzz/dp-mechanism.md` / `end-turn.md` の関連箇所更新

5. **ドキュメント**:
   - `docs/adr/README.md`: 本 ADR を追加
   - `CLAUDE.md` §11「確立済み ADR 一覧」: 本 ADR を追加(M2-PR5 完成記録 PR でスリム化済の構造に追記、1 行)
   - `docs/adr/0009-m2-m3-dp-and-victory-conditions.md`: §「M3 範囲」の M3 候補から本 ADR への引用追加

### M3 完成基準(Definition of Done)

| 観点 | 完了基準 |
| ---- | ---- |
| API | `IGameRule.IsTerminated` / `GetWinner` 実装、契約に従う |
| 早期勝利 | 夜 + 持ち点 100 + 就寝カードプレイで `Outcome = WinnerOutcome(currentPlayer)` |
| 終了時勝利 | Round 21 完了で `Outcome = WinnerOutcome(lowerScorePlayer)` または `DrawOutcome` |
| 終了後ガード | `Outcome != null` の session に対する Action はすべて illegal |
| 引き分け | TotalPoints 等値時 `DrawOutcome`、tiebreaker なし |
| カバレッジ | Application C0 100% 維持(`EarlyWinTriggerEffect` / `DrowZzzRule.IsTerminated` / `GetWinner` の全分岐) |
| 既存テスト | `Outcome: null` 引数追加で全件緑維持 |

### 要件 ID prefix(M3 範囲)

| Prefix | 範囲 | 配置 |
| ---- | ---- | ---- |
| `APP-` | 汎用 Application 層 interface(`IGameRule.IsTerminated` / `GetWinner` 契約)| `docs/specs/application/game-rule.md` |
| `DZ-` | DrowZzz 固有(勝利条件 / EarlyWinTriggerEffect / 引き分け)| `docs/specs/games/drowzzz/victory-conditions.md` 等 |
| `CFG-` | `IGameConfig` 関連(本 ADR では追加なし、コメント訂正のみ)| 該当なし |

M3-PR1 着手時に最新採番状況を `grep -hroE "\b(APP|DZ|CFG)-[0-9]+\b" docs/specs/ | sort -u | tail` で再確認、連番継続。

### M3-PR1 完成記録(2026-05-12、ゲーム終了判定 + 勝者決定 + 早期勝利 + 引き分け仕様)

**完成 PR**: PR #42 `feat(app): IGameRule.IsTerminated/GetWinner + 早期勝利 + 引き分け仕様を実装 (M3-PR1)`(merged `a1bf50c`)+ 関連 hot-fix PR #43 `fix(test): Round 21 完了テストの FDP デフォルト依存を解消`(merged `d744bd6`)。

#### Definition of Done 達成項目(本 ADR §1〜§9 で確定した仕様の最初の実装)

| スコープ項目 | 達成状況 | 備考 |
| ---- | ---- | ---- |
| `IGameRule<TAction, TSession>` に `IsTerminated(TSession) : bool` / `GetWinner(TSession) : PlayerId` を追加 | ✓ | 既存最小 API(2 メソッド)→ 4 メソッドに拡張(本 ADR §1)、`GetWinner` 未終了時は `InvalidOperationException` / 引き分け時は `null` |
| `GameOutcome` 階層を Domain 層に新設(`abstract record` + `WinnerOutcome(PlayerId Winner)` + `DrawOutcome`)| ✓ | Domain 配置(`Drowsy.Domain.Game.GameOutcome.cs`、本 ADR §2)、`WinnerOutcome` は null 防御の二重ガード、`DrawOutcome` はマーカー的派生型 |
| `DrowZzzGameSession.Outcome` プロパティ追加(8 引数 ctor 化)| ✓ | `GameOutcome?`(null = 未終了)、`IsTerminated` computed プロパティ(本 ADR §3) |
| 終了トリガー 2 経路の実装 | ✓ | (a) `EarlyWinTriggerEffect` 評価時に `Clock.IsNight && TotalPoints[currentPlayer] >= 100` で `WinnerOutcome` 設定 / (b) `DrowZzzRule.ApplyEndTurn` 内 Round 21 完了検出で `TotalPoints` 比較 → 低い方が勝者 / 等値なら `DrawOutcome`(本 ADR §4) |
| `EarlyWinTriggerEffect` 効果 record 新設(マーカー記録)| ✓ | フィールドなし `sealed record`、閾値は `DrowZzzVictoryConstants.EarlyWinScoreThreshold = 100` 集約(本 ADR §5 / §9) |
| Round 22 への遷移ブロック | ✓ | `DrowZzzRule.IsLegalMove` で `session.Outcome != null` は全 Action illegal、`Apply` 側にも防御的 `InvalidOperationException`(本 ADR §6) |
| 引き分け仕様の確定実装 | ✓ | TotalPoints 等値で `DrawOutcome`、tiebreaker なし(本 ADR §7、ADR-0009 §6 保留分の JIT 確定) |
| `MaxRoundNumber` の `DrowZzzClockConstants` 維持判断 | ✓ | `IGameConfig.cs` コメント訂正同梱、L2 不変量として constants 単一情報源(本 ADR §8) |
| `EarlyWinScoreThreshold` を新規 `DrowZzzVictoryConstants` に集約 | ✓ | 勝利条件関連定数の意味グループ分離(本 ADR §9) |

#### 仕様 ID / NUnit 増加

- 仕様 ID 新規採番: GS-101〜GS-105(GameOutcome、Domain 層)/ APP-041〜APP-043(IGameRule M3 拡張)/ DZ-183〜DZ-192(EarlyWinTriggerEffect / DrowZzzRule M3 拡張 / DrowZzzGameSession.Outcome)
- NUnit Property: **+38 件 →累計 305 件**(M3-PR1 マージ直後)、PR #43 hot-fix で論理修正のみ
  - 新規 2 ファイル: `GameOutcomeTests`(6 件)/ `EarlyWinTriggerEffectTests`(5 件)
  - 既存拡張: `IGameRuleTests`(+5)/ `DrowZzzRuleTests`(+10)/ `DrowZzzGameSessionTests`(+5)
- 既存 11 テストファイルに `outcome: null` を Python 機械挿入(52 箇所、M2-PR5 `Inf()` ヘルパー伝播パターン継続)

#### code-reviewer subagent 反映

- W-1: `DrowZzzGameSessionTests` に DZ-179 専用 5 テスト追加
- W-2: `ChoiceEffectTests.BuildMinimalSession` の FQN を using で整理
- W-3: `ApplyInfluenceEffectTests` クラスサマリに DZ-161 追記
- P-1: `GameOutcome.cs` xmldoc から Application 層 cref を削除
- P-2: `DrowZzzRule.Apply` に終了済 session への防御的 `InvalidOperationException` 追加
- P-4: `ApplyEndTurn` コメントで `TurnNumber` / `RoundNumber` を区別明示
- W-3: discard 変数 `DrawOutcome _` を `DrawOutcome` パターンマッチに簡素化(警告 W-3 別件)

#### hot-fix PR #43(merged `d744bd6`)の経緯

M3-PR1 マージ後の Unity Test Runner で `Given_Round21最終フェーズで両者同点_When_EndTurnでRound22到達_Then_OutcomeはDraw` が失敗:

- 原因: `DrowZzzRuleTests.NewSession` のデフォルト FDP が `{ p1: 0, p2: 10 }` で非ゼロ差分を含み、SDP=10/10 でも TotalPoints が 10 vs 20 で不一致
- 修正: DZ-190 関連 2 テストで FDP も 0/0 に明示上書き、TotalPoints の差分を SDP のみに依存させる

学び:**ヘルパーのデフォルト値が「非対称な初期状態」を表現している場合、新規テストが「対称性」を前提とするなら明示的に上書きする必要がある**。今後のテスト追加時に意識すべきパターン。

### M2-M3 完成記録の追記タイミング

本 ADR の M3-PR1 完成記録は §直上に追記済。M3-PR2 以降の機構実装(ベッド破損 / 放棄 / 連想 / キーワード能力 / 夢カード)は **ADR-0011** §M3-PR-N 完成記録に追記される運用(ADR-0011 §「M3 完成記録の追記タイミング」と整合)。M3 全体の完成時に §M3 完成記録(全体)を別途追加、Definition of Done 達成方法を集約する。

### Phase 2 の進捗バナー更新(本 ADR 起票 PR では行わない)

ADR 起票 PR では CLAUDE.md §11 のバナー更新は最小限(本 ADR を「確立済み ADR 一覧」表に 1 行追加するのみ)に留める。M3 着手中 / 完成時のバナー更新は M3-PR1 着手 PR / 完成記録 PR で実施。

### 引き分け仕様の JIT 確定経緯

ADR-0009 §6 で「引き分けの判定仕様 = M3 着手 ADR で JIT」と保留されていたが、本 ADR §7 で **「TotalPoints 等値で引き分け、tiebreaker なし」** に確定(プロジェクトオーナー JIT 確認、2026-05-12)。決定根拠:

- ゲームコンセプト「朝まで起きていられた方が勝ち」の文脈で、「両者同点で朝を迎えた」は意味のある結末
- tiebreaker 導入は戦略次元を増やすが、引き分けの意味的清潔さを失う
- 隠し情報(FDP / DDP)による tiebreaker は「プレイヤー操作不能な要素で勝敗が決まる」ゲーム性として不健全
- 公開情報(SDP)単独 tiebreaker は「SDP 駆け引きを過度に重視させる」meta を生む

## Related

- 前提: [ADR-0001 ADR Operations](0001-adr-operations.md)
- 前提: [ADR-0002 Phase 1 Domain 拡張の集約境界と概念モデル](0002-phase1-domain-boundaries.md) — Domain ゲーム非依存原則 / Immutability ポリシー(`GameOutcome` の Domain 配置と整合)
- 前提: [ADR-0004 IsExternalInit polyfill](0004-init-setter-polyfill.md) — `GameOutcome` 派生 record の `with` 利用前提
- 前提: [ADR-0005 Phase 2 Roadmap](0005-phase2-roadmap-drowzzz.md) — §M3 Definition of Done(本 ADR §「M3 完成基準」が具体化)
- 前提: [ADR-0006 M1 詳細](0006-m1-detail-application-interfaces.md) — `IGameRule<TAction, TSession>` 最小 API、本 ADR で M3 拡張(IsTerminated / GetWinner)
- 前提: [ADR-0007 M2 詳細(カード効果)](0007-m2-detail-card-effects.md) — `IEffect` + `EffectInterpreter` パターン、本 ADR の `EarlyWinTriggerEffect` 追加先
- 前提: [ADR-0008 M2 Clock + 夜・朝フェーズ](0008-m2-drowzzz-clock-and-night-morning.md) — Clock 21 ラウンド構造、§5「`RoundNumber > 21` への振る舞い」を本 ADR で明示的ガードに昇格
- 前提: [ADR-0009 M2-M3 DP 機構 + 勝利条件](0009-m2-m3-dp-and-victory-conditions.md) — §5 / §6 / §「コンセプト」で予約された M3 範囲、本 ADR が具体化
- 関連規約: [`CLAUDE.md`](../../CLAUDE.md) §5 アーキテクチャ依存ルール / §6 テスト方針 / §9 定数管理方針(L2 不変量集約) / §11 ADR 運用
- 関連: [`docs/specs/games/drowzzz/`](../specs/games/drowzzz/) — M3-PR1 で `victory-conditions.md` / `effects/early-win-trigger.md` 等を追加
- 関連: [`docs/specs/application/game-rule.md`](../specs/application/game-rule.md) — M3-PR1 で `IsTerminated` / `GetWinner` 契約を APP- 採番で追加
- 後続: M3-PR1〜PR-N(進行中)
- 後続: ADR-0011 候補(M4 永続化 / SO 化、本 ADR で `EarlyWinScoreThreshold` の SO 移行を留保した分の再評価)
