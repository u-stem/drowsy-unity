# ADR-0007: M2 詳細 — カード効果メカニズム(`IEffect` + `EffectInterpreter`)とサブセット先行スコープ

| 項目 | 値 |
| ---- | ---- |
| Status | Accepted |
| Date | 2026-05-11 |
| Decider | プロジェクトオーナー |

## Context

ADR-0005 で Phase 2 のロードマップを M1〜M5 に分割し、ADR-0006 で **M1(ターン進行 + カードプレイの最小骨格)** を完成させた。M1 完成時点で `CardData.Attributes`(`IReadOnlyDictionary<string, int>`)は **data のみ** の状態で、効果としての解釈手段は未確定のまま残っている(ADR-0006 §6 で「カード効果は M2」と整理済)。

本 ADR は **M2(カード効果の段階的実装)の詳細設計** を確定する。ADR-0006 が M1 で果たした役割を、本 ADR が M2 で果たす(JIT 共有方式の継続)。

ADR-0005 / ADR-0006 で既に決まっている前提:

- 縦串で本命ゲーム DrowZzz を直接実装、練習用ゲームを挟まない(ADR-0005)
- 名前空間: 汎用 interface(`Drowsy.Application` 直下)+ DrowZzz 固有(`Drowsy.Application.Games.DrowZzz`)
- DI 方針: M1〜M4 は Pure C#、M5 で VContainer 統合(ADR-0005)
- ロジック先行(Presentation は M5 まで保留、ADR-0005)
- 1 PR = 1 効果カテゴリ(ADR-0005 §M2、本 ADR で更に粒度を確定)
- M1 で確立した実装パターン: record + null 防御二重ガード / record + 内部 Dictionary の Equals override / `_` ケース UnknownXxx ダミーでカバレッジ確保(ADR-0006 §M1 進行中の学び)

本 ADR で確定する詳細:

1. カード効果の表現手段(`IEffect` マーカー + record 階層 + `EffectInterpreter`)
2. `ICardCatalog` への `GetEffects(CardId)` 追加と CardData/Effect の責務境界
3. 効果発動タイミング(`PlayCardAction.Apply` 中に同期発動、合成は左から順)
4. M2 完了スコープ(サブセット先行 4〜8 枚、本物 56 枚フル実装は M3 以降に分散)
5. `ICardCatalog` の M2 での実装方針(`InMemoryCardCatalog` 拡張、`ScriptableObjectCardCatalog` 化は M4 へ)
6. M2 の PR 分割粒度(1 PR = 1 効果 record + Interpreter 拡張 + 対応カード実例)
7. 山札枯渇 / 手札 0 枚の現状仕様(ADR-0006 §6 で「M2 以降」と保留した部分を本 ADR で確定)
8. M2 範囲外の事項(M3 以降に送る項目の明示)

### M2 で扱う DrowZzz 効果仕様の境界(プロジェクトオーナーから共有された範囲)

| 項目 | 値 |
| ---- | ---- |
| プレイヤー数 | N=2(M1 と同じ) |
| 効果の発生源 | `PlayCardAction` でプレイされたカード(M2 範囲では他の発動契機を扱わない) |
| 効果の発動タイミング | プレイ時に同期発動(`PlayCardAction.Apply` 内で `EffectInterpreter.Apply` を逐次呼び出し) |
| 効果の数 | 1 枚のカードが 0〜N 個の `IEffect` を持つ(0 個なら M1 と同じ挙動、効果なし) |
| 効果の合成順序 | `IReadOnlyList<IEffect>` を左から順に Aggregate(分岐 / 並列 / 中断は M2 範囲外) |
| 効果の対象範囲 | 自プレイヤー中心(他者影響系は M2 内に登場するなら IEffect record にフィールド追加で表現) |
| フレーバーテキスト | **現状存在しない**(M2 進行中に追加される場合、Public 公開可否は別途判断) |
| 個別カードの効果定義 | M2-PR2 以降の各 PR 着手時に JIT 共有(ADR-0006 §M1 と同じ運用) |

### 山札枯渇 / 手札 0 枚 — 現状の数値前提下では発生しない

ADR-0006 §6 で「M2 以降に判断」と保留した山札枯渇 / 手札 0 枚の挙動について、現状のルール数値前提下では発生しないことが計算で確認できる:

| 項目 | 値 |
| ---- | ---- |
| 山札サイズ | 56 枚(N=2 想定、ADR-0006 M1-PR3 で確定) |
| 初期配布 | プレイヤー 2 名 × 5 枚 = 10 枚 |
| 1 ラウンドあたりの Draw 数 | 2 名 × 1 Draw = 2 枚 |
| 最大ラウンド数(`MaxRoundNumber`) | **21**(M3 で `IGameConfig.MaxRoundNumber` として実装予定、ADR-0006 §1.4、ADR-0009 起票時に Clock 仕様 21 ラウンドへ訂正反映) |
| 総 Draw 数(ゲーム終了まで) | 2 枚 × 21 ラウンド = **42 枚** |
| 初期配布 + 総 Draw 数 | 10 + 42 = **52 枚 ≤ 56 枚(余裕 4 枚)** |

将来仕様が変わって(効果でドロー枚数が変動 / `MaxRoundNumber` 拡大 / 山札サイズ縮小 等)枯渇シナリオが発生し得る場合は、別 ADR で再シャッフル / ゲーム終了 / その他の方針を確定する。本 ADR では「現状想定下で枯渇は発生しない」「Phase 1 `Pile.Draw` の空 Pile 例外を防御として維持する」のみを確定する。

## Decision

### 1. 効果メカニズム: `IEffect` マーカー + record 階層 + `EffectInterpreter`

ADR-0006 で確立した `IGameAction` + record 階層 + `DrowZzzRule` の対称な設計を採る。`IEffect` をマーカー interface、具体形を record で表現し、`EffectInterpreter` が switch で派生型をマッチングして session 遷移を返す純関数とする。

#### 1.1 `IEffect`

namespace は `Drowsy.Application.Games.DrowZzz.Effects` に配置する。汎用化(`Drowsy.Application` 直下)はしない:現状 DrowZzz 専用の概念であり、Premature Abstraction を避ける(YAGNI、ADR-0006 §3 の `ApplyActionUseCase<TAction, TSession>` を見送ったのと同じ判断)。

```csharp
namespace Drowsy.Application.Games.DrowZzz.Effects;

public interface IEffect
{
}
```

#### 1.2 Effect record 階層(M2 進行中に拡張)

M2-PR1 では空の階層(`IEffect` のみ)を用意し、M2-PR2 以降で 1 PR = 1 record を追加する。各 record の名称・フィールド・効果意味は M2 進行中に JIT 共有される(ADR-0006 §M1-PR3〜PR5 で個別ルールが JIT 共有された運用を継続)。

設計指針:

- `record class` + `init` setter(ADR-0004 polyfill 前提)
- positional 引数を持つ record は **null 防御の二重ガードパターン** を必須(ADR-0006 §M1 進行中の学び:バッキングフィールド初期化式 + init setter 本体の両方で `value ?? throw`)
- 内部に `Dictionary<K, V>` / `List<T>` を持つ effect は `Equals` / `GetHashCode` の override が必須(ADR-0006 §M1 進行中の学び)
- 値型は `CardId` / `PlayerId` / `int` 等の小さな値オブジェクトに限定し、`GameState` / `DrowZzzGameSession` を effect 内に埋め込まない(循環依存防止)

#### 1.3 `EffectInterpreter`

namespace: `Drowsy.Application.Games.DrowZzz.Effects`

```csharp
namespace Drowsy.Application.Games.DrowZzz.Effects;

public sealed class EffectInterpreter
{
    public DrowZzzGameSession Apply(DrowZzzGameSession session, IEffect effect);
}
```

- `Apply` は純関数(副作用なし、入力から出力を返す)
- switch で `IEffect` 派生型をマッチング、各 case で session 遷移を返す
- 未知の派生型(`_` ケース)は **`NotImplementedException`** を投げる(`DrowZzzRule.Apply` / `IsLegalMove` の `_` ケースが `NotImplementedException` を採用しているため整合させる、ADR-0006 §M1 進行中の学び §「`_` ケースカバレッジ確保」)
- カバレッジ確保のため Tests assembly 内に `UnknownEffect` ダミー型を定義し、`_` ケースを到達させる(ADR-0006 §M1 進行中の学び §`_` ケースカバレッジ確保 を踏襲)

**例外型の選択根拠**: `EffectInterpreter._` ケースと `DrowZzzRule._` ケースは「ライブラリ作者が switch case 追加を忘れている」状況を表す同質の防御例外なので、両者を `NotImplementedException` で統一する。これに対し `ApplyActionUseCase` の `IsLegalMove` false 時(ADR-0006 §3)は「呼び出し側が状態を確認せずに違法な操作を要求した」状況で、根本原因は呼び出し側の不正利用にあるため `InvalidOperationException`。両例外の使い分けは「**実装漏れ = `NotImplementedException`** / **利用側の不正 = `InvalidOperationException`**」とする。

**namespace `Effects` サブ namespace に置く判断根拠**: `Drowsy.Application.Games.DrowZzz` 直下ではなく `Drowsy.Application.Games.DrowZzz.Effects` に分離する理由は、M2 進行中に Effect record が 4〜8 個追加されたとき DrowZzz namespace 直下のファイル数が肥大化するため、論理サブグループでフォルダを切る方が見通しが良い(`Actions` も同じく分離する余地があるが、`DrowZzzAction` 階層は 4 種で固定的なため現状は `DrowZzz` 直下に維持)。`EffectInterpreter` から `DrowZzzGameSession`(親 namespace)を `using` で参照する単方向参照になるが、`Effects` から見て `DrowZzz` は親 namespace で循環依存にはならない。

#### 1.4 `actor`(発動元 PlayerId)の扱い

多くの効果は「現プレイヤーが自分に対して」発動するため、`session.GameState.Turn.CurrentPlayerIndex` から暗黙取得する(`PlayCardAction` と同じ ADR-0006 §2.1 の方針)。他者影響系効果(例: 相手にドローさせる)が必要になった時点で:

- 案 A: `IEffect` record のフィールドに `TargetPlayerId` を追加(該当 record 単位で導入)
- 案 B: `EffectInterpreter.Apply` のシグネチャを `Apply(session, effect, actor)` に拡張

どちらを採るかは **M2 進行中に他者影響系の最初の record が登場した時点で JIT 判断** する。M2-PR1 では暗黙取得の最小 API のみ確定する。

**M2-PR3 JIT 確定(2026-05-11、ADR-0009「コップ一杯の脅威」JIT 共有時)**: 案 A のサブセット派生として **`enum SdpTarget { Self, Opponent }`** を効果 record の positional 引数に取る方式を採用(`AdjustSdpEffect(SdpTarget Target, int Delta)` / `DrawCardEffect(SdpTarget Target, int Count)`)。`TargetPlayerId` を直接持つより、N=2 想定下では「現プレイヤーに対する相対位置」の方が effect record の自己完結性が高く、N>2 拡張時にも `enum DpTarget { Self, AllOpponents, Specific }` 等で広げやすい。`EffectInterpreter` 内部で `ResolveTargetPlayerId(session, target)` が `SdpTarget` を実際の `PlayerId` に解決する(N=2 で Opponent は一意決定)。案 B は採用せず、`EffectInterpreter.Apply` のシグネチャは ADR-0007 §1.3 のまま `(session, effect)` を維持。詳細は `docs/specs/games/drowzzz/dp-mechanism.md` §「actor 概念」/ `Effects/SdpTarget.cs` の XML doc 参照。

### 2. `ICardCatalog.GetEffects(CardId)` の追加と責務拡張

M1 で確定した `ICardCatalog`(ADR-0006 §1.3)に effect 取得メソッドを追加する。

```csharp
namespace Drowsy.Application;

public interface ICardCatalog
{
    CardData Get(CardId id);
    bool TryGet(CardId id, out CardData? data);
    IReadOnlyList<IEffect> GetEffects(CardId id);  // M2-PR1 で追加
}
```

ただし `IEffect` は DrowZzz 固有(`Drowsy.Application.Games.DrowZzz.Effects` namespace)である一方、`ICardCatalog` は汎用 interface(`Drowsy.Application` 直下)。**汎用 interface に DrowZzz 固有型を露出するのは依存方向違反** となるため、以下の 2 案を比較:

| 案 | 採用判断 |
| ---- | ---- |
| 案 A: `ICardCatalog<TEffect>` のジェネリック化 | 採用候補(`IGameRule<TAction, TSession>` と対称) |
| 案 B: 別 interface `IEffectProvider<TEffect>` を新設し、`InMemoryCardCatalog` が 2 つ実装 | 不採用(SO 化時に CardData と Effect 定義が 2 つの asset に分散、Designer ワークフロー悪化) |
| 案 C: `ICardCatalog.GetEffects` を `IReadOnlyList<object>` で返す | 不採用(型安全性が失われる) |
| 案 D: `ICardCatalog` をそのまま DrowZzz 固有に DZ-namespace へ移す | 不採用(他ゲーム転用余地を残す ADR-0005 の Application 配置と矛盾) |

**採用: 案 A**(`ICardCatalog<TEffect> where TEffect : class`)。`IGameRule<TAction, TSession>` と同じくジェネリックで型安全を保つ。M1 で既に `ICardCatalog`(非ジェネリック)を導入しているため、**M2-PR1 で `ICardCatalog<TEffect>` に拡張する破壊的変更が入る** ことを Implementation Notes で明示する(M1-PR2 で導入された `InMemoryCardCatalog` のシグネチャも併せて更新)。

最終形:

```csharp
namespace Drowsy.Application;

public interface ICardCatalog<TEffect>
    where TEffect : class
{
    CardData Get(CardId id);
    bool TryGet(CardId id, out CardData? data);
    IReadOnlyList<TEffect> GetEffects(CardId id);
}
```

DrowZzz 側は `ICardCatalog<IEffect>` を実装する。

**「依存方向違反」ではなく「Application 内の汎用 / 固有結合問題」**: 案 B〜D の却下理由を「依存方向違反」と書きたくなるが、これは正確ではない。`ICardCatalog`(汎用)と `IEffect`(DrowZzz 固有)は両方とも `Drowsy.Application` assembly 内のため、Roslyn / asmdef レベルの依存方向検証では弾かれない(同一レイヤ内)。本来の問題は「将来 `Drowsy.Application.Games.OtherGame` を追加した際に、`OtherGame.OtherCatalog` が DrowZzz 固有の `IEffect` を強制 import / cast する設計汚染が発生する」点。よって `ICardCatalog<TEffect>` でジェネリック化し、他ゲーム実装が `ICardCatalog<IOtherEffect>` で自分の Effect 型を選べる形が筋。

### 3. `PlayCardAction.Apply` の責務拡張(効果発動タイミング)

M1 では `PlayCardAction.Apply` は「手札 → Field に AddTop で 1 枚移動 → `TurnPhase = WaitingForEndTurn`」のみ(ADR-0006 §2.4 Apply 表)。M2 では効果発動を **Field 移動の後に同期実行** する。

以下のコード片は M1-PR5 で実装された `DrowZzzRule.ApplyPlayCard`(`Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzRule.cs:143-186`)の **末尾構造をそのまま継承した具体的な書き換え** を示す(extension メソッドを新設せず、現状の明示的な配列ループ + `record with` パターンに 2 行追加する形):

```csharp
// 現状(M1-PR5 実装):Players 配列を newPlayers に置換 → GameState を with で更新 →
// session を with で更新 → return まで。
// M2-PR1 で「return 直前」に効果評価を挿入する:
var afterPlay = session with
{
    GameState = newGameState,
    TurnPhase = DrowZzzTurnPhase.WaitingForEndTurn,
};
var effects = _catalog.GetEffects(action.Card);  // M2-PR1 で ICardCatalog<IEffect>.GetEffects(CardId) を追加
return effects.Aggregate(afterPlay, (s, e) => _interpreter.Apply(s, e));
```

- 効果なし(`effects.Count == 0`)の場合は M1 と完全互換(`Aggregate` の初期値 `afterPlay` がそのまま返る)
- 効果は **左から順** に逐次評価、分岐 / 並列 / 中断は M2 範囲外
- 効果評価後に session の TurnPhase が変わる可能性はあるが、M2 範囲では「効果は GameState を変えるが TurnPhase は変えない」を原則とする(TurnPhase を変える効果は M2 範囲外)
- 上記コード片の `newGameState` / `action.Card` は現状実装と同じ識別子(extension メソッド `WithCurrentPlayer` / `WithField` / `RemoveFromHand` は存在しない)

**`DrowZzzRule` の依存追加**: M1 では `DrowZzzRule` は依存ゼロ(ADR-0006 §2.4)。M2-PR1 で constructor injection で `ICardCatalog<IEffect>` と `EffectInterpreter` を受け取る形に変更する。`ApplyActionUseCase` も constructor で `DrowZzzRule` を受け取るのは M1 のまま(ADR-0006 §3)、間接依存として `ICardCatalog<IEffect>` / `EffectInterpreter` が増える。

**`StartGameUseCase` の型引数結合(設計上の割り切り)**: `StartGameUseCase`(`Assets/_Project/Scripts/Application/Games/DrowZzz/StartGameUseCase.cs:44`)も constructor で `ICardCatalog` を受け取るが、現状 `CardId` の移動のみ扱い `CardData` / `IEffect` を一切参照しない(remarks に「本 PR (M1-PR3) では参照しない」と明記済、ADR-0006 §3 で「constructor injection は維持」と判断)。M2-PR1 で `ICardCatalog<IEffect>` へジェネリック化すると、`StartGameUseCase` は `IEffect` を内部で参照しないにもかかわらず constructor シグネチャに `ICardCatalog<IEffect>` 型を持たされる。

この型引数結合は本 ADR で **設計上の割り切り** として受容する。理由:

- M1-PR3 で「constructor injection 維持」を確定済(ADR-0006 §3)
- 案 X(`StartGameUseCase` から `ICardCatalog` 引数削除)は ADR-0006 §3 を覆す変更で、本 ADR のスコープ外。将来 SO 化(M4)時に `StartGameUseCase` がカード情報を本当に必要としないことが確定したら別 PR / 別 ADR で再評価
- 案 Y(非ジェネリック `ICardCatalog` を `ICardCatalog<TEffect>` の基底に分離)は型階層が複雑化し、現状 1 ゲーム実装のみの規模では過剰
- 型引数 `IEffect` は `StartGameUseCase` の内部実装に染み出さず、constructor シグネチャ + フィールド型のみで完結(`_catalog.Get(...)` / `_catalog.TryGet(...)` の利用面では型引数は不可視)

将来 `StartGameUseCase` から `ICardCatalog` 依存削除を検討する旨を `docs/todo.md` に登録する(本 ADR 起票 PR 同梱、§Implementation Notes §「TODO 登録」参照)。

### 4. M2 完了スコープ: サブセット先行 4〜8 枚

M2 完了 = 効果メカニズム整備 + 主要効果カテゴリの代表カード 4〜8 枚が動く + 統合テストで end-to-end 検証(M1-PR7 と同じパターン)。

| 観点 | M2 完了基準 |
| ---- | ---- |
| 効果メカニズム | `IEffect` / `EffectInterpreter` / `ICardCatalog<IEffect>` が動作、`_` ケース防御例外 + Tests カバレッジ確保 |
| 個別カード | 4〜8 枚(主要効果カテゴリの代表)、各カードに対応する EARS / .feature 整備済 |
| カテゴリ網羅 | ドロー系 / 場操作系 / 相手影響系 / FDP 操作系の代表効果が 1 つ以上動く(具体的なカテゴリ分けは M2-PR2 着手時に JIT 共有) |
| テスト | Application C0 100% 維持、Domain 影響なし、統合テスト追加 |
| ドキュメント | EARS / .feature の APP-/DZ- 連番採番、ADR-0007 完了記録の追記 |

残り 48〜52 枚は M3(勝敗判定)/ M4(永続化 + SO 化)/ M5(Bootstrap)のいずれかで段階的に追加する。本 ADR では分散先を確定しない(M3 着手 ADR で再評価)。

### 5. `ICardCatalog` の M2 実装: `InMemoryCardCatalog` 拡張

ADR-0006 §1.3 の「M2 で SO 化(`Drowsy.Infrastructure.Games.DrowZzz.ScriptableObjectCardCatalog`)」記載を **本 ADR で M4 に変更** する。

| 観点 | M2 採用 | M4 に送る理由 |
| ---- | ---- | ---- |
| データ規模 | サブセット 4〜8 枚 | InMemoryCardCatalog で十分、SO 化のメンテコスト不要 |
| Designer ワークフロー | M5 まで Presentation なし(ADR-0005 ロジック先行) | SO による Designer 編集需要は M4 / M5 で発生 |
| 永続化との同時設計 | M4 で JSON 永続化が入る | SO ベースと JSON ベースの整合性を M4 で同時設計する方が筋 |
| テスト容易性 | Application.Tests は Pure C#(ADR-0006 §4) | SO は Unity Editor / EditMode 必須、Application.Tests の独立性を保つ |

**ADR-0006 記載との不整合について**: ADR-0006 は M1 時点のスナップショットとして扱い、本文には手を入れない(ADR-0001 の運用)。新しい判断は本 ADR-0007 が優先する。

### 6. M2 の PR 分割粒度

1 PR = 1 効果 record 追加 + Interpreter 拡張 + 対応カード実例(セット出し)を基本単位とする。

| PR | 内容 |
| ---- | ---- |
| **M2-PR1** | 効果インフラ整備: `IEffect` / `EffectInterpreter` 骨格 / `ICardCatalog<TEffect>` ジェネリック化 / `InMemoryCardCatalog` 更新 / `DrowZzzRule.PlayCardAction.Apply` 拡張 / 既存テスト全緑維持 / `UnknownEffect` ダミー + `_` ケーステスト |
| **M2-PR2** 〜 **M2-PR(N-1)** | 1 PR = 1 効果 record(例: `DrawCardsEffect`)+ Interpreter case 追加 + 対応カード 1〜2 枚を `InMemoryCardCatalog` に追加 + EARS / .feature 整備 + 効果単体テスト + 統合テスト |
| **M2-PR-N**(最終) | M2 完成 PR: 統合テスト拡充(複数効果カードのプレイ、効果カテゴリ網羅)+ M2 完成 docs 整備(本 ADR の Implementation Notes 更新 / README ステータスバナー / CLAUDE.md §11) |

PR 数の目安: M2-PR1 + 効果 4〜8 種 + 完成 PR = **6〜10 PR**(M1 が 7 PR だったのと近い規模)。

ADR-0005 §M2 で「1 PR = 1 効果カテゴリ」と記載していたが、本 ADR で「1 PR = 1 効果 record(+ 対応カード実例)」に粒度を細分化する。カテゴリ単位だと 1 PR が大きくなり 1 PR = 1 論理変更との両立が難しくなるため。

### 7. 山札枯渇 / 手札 0 枚の現状仕様

Context §「山札枯渇 / 手札 0 枚」で示した通り、**現状の数値前提下では発生しない**(N=2 × 21 ラウンド × 1 Draw + 初期配布 10 = 52 ≤ 山札 56、余裕 4 枚)。本 ADR では以下を確定する:

- M2 範囲では枯渇シナリオへの予防実装を **行わない**(Phase 1 `Pile.Draw` の空 Pile 例外を防御として維持)
- 将来仕様変更で枯渇シナリオが発生し得る場合は **別 ADR**(再シャッフル / ゲーム終了 / その他)で対応
- `MaxRoundNumber` が拡大される場合や、効果でドロー枚数が増える場合(例: `DrawCardsEffect(3)`)に枯渇可能性が発生するため、効果追加時の各 PR で「ドロー総数が山札サイズを超えないか」を確認するメモを **`docs/todo.md` の TODO エントリ** として登録する(本 ADR 起票 PR 同梱)

### 8. M2 範囲外(本 ADR で明記)

| 項目 | 扱う場所 |
| ---- | ---- |
| 勝敗判定 | M3(`IGameRule.IsTerminated` / `GetWinner`) |
| 永続化 / SO 化(`ScriptableObjectCardCatalog`) | M4 |
| UI / Bootstrap / VContainer 実利用 | M5 |
| 持続効果(永続発動)/ EndTurn 発動 / 遅延発動 / 条件発動 | 必要時に別 ADR |
| 効果連鎖の条件分岐 / 並列発動 / 中断 | 同上 |
| 「省略」相当の Action 種別 | ADR-0006 と同じく将来 PR / ADR(プロジェクトオーナーが JIT 共有) |
| 1 ターン目と通常時の差 | 同上 |
| N>2 プレイヤー | Phase 3 |
| 山札枯渇シナリオの本格対応(再シャッフル / 強制終了 等) | §7 の通り別 ADR |
| 本物 56 枚フル実装 | M3 以降に分散(本 ADR では分散先を確定しない) |
| フレーバーテキスト | 現状存在せず、M2 中に追加されれば Public 公開可否を都度判断 |

## Consequences

### Positive

- M1 と同じ JIT 共有方式で M2 が段階的に進められる(`IGameAction` + `DrowZzzRule` の M1 経験を `IEffect` + `EffectInterpreter` に転用)
- `IEffect` + `Interpreter` パターンが `IGameAction` + `Rule` と対称で、コードベース全体の一貫性が高い
- PR 粒度が「1 PR = 1 効果 record」で安定し、レビュー単位が明確(M2-PR1 を除き各 PR は機械的に切れる)
- 山札枯渇仕様を本 ADR で確定したため、M2 進行中に予防実装の判断ブレが発生しない
- `ICardCatalog<TEffect>` ジェネリック化により、将来別ゲームを追加した際にも `ICardCatalog<IOtherEffect>` で型安全に再利用可能
- SO 化を M4 に送ったことで M2 が肥大化せず、サブセット先行(4〜8 枚)のシンプルさが保たれる

### Negative

- `IEffect` record が増えるごとに `EffectInterpreter` の switch が肥大化する(M3 / M4 / M5 で 56 枚相当まで)
  - **緩和**: 各 case 内のロジックを Effect record 内に wrap する Visitor / Double Dispatch パターンへの移行余地を残す(現状は YAGNI で switch を採用)
- `ICardCatalog` をジェネリック化することで M1 時点の `ICardCatalog`(非ジェネリック)とは **破壊的変更**
  - **緩和**: M1-PR2 で導入された `InMemoryCardCatalog` のシグネチャを M2-PR1 で同時に更新する(Tests 5 件程度の更新で済む見込み)、本 ADR Implementation Notes で明示
- SO 化を M4 に送ったため、ADR-0006 §1.3 の記載と齟齬が発生
  - **緩和**: ADR-0006 は M1 時点のスナップショットとして扱い変更しない、本 ADR-0007 が新しい判断として優先される(ADR-0001 の運用に従う)
- M2 完了後も本物 56 枚のうち 48〜52 枚が未実装で残る
  - **緩和**: ADR-0005 の段階的縦串哲学と整合、M3 以降で段階的に追加する分散先を当該 M 着手 ADR で確定
- 他者影響系効果が登場した時点で `IEffect` / `EffectInterpreter` のシグネチャ拡張(actor 引数追加)が必要になる可能性
  - **緩和**: M2 進行中に最初の他者影響系 record が登場した時点で JIT 判断、現状は最小 API

### Neutral

- M2 着手 PR(M2-PR1〜PR-N)で本 ADR の決定を実装に落としていく流れ
- M2 中のルール最適化 / 仕様調整に伴う EARS / .feature 更新は通常通り PR で履歴を残す(ADR-0005 §6 の運用継続)
- 個別カードの効果仕様 / Attribute キーは JIT 共有で M2-PR2 以降に体系化、本 ADR では仕様一覧を持たない
- 本 ADR では Effect record の具体名(`DrawCardsEffect` 等)を **確定しない**:M2-PR1 では `IEffect` 空 marker のみ導入し、最初の具体 record は M2-PR2 着手時に JIT 共有で命名・設計する

## Alternatives Considered

| 案 | 不採用理由 |
| ---- | ---- |
| `PlayCardAction.Apply` 内に `CardData.Attributes` の switch を直接書く | `DrowZzzRule` が肥大化、効果が増えると 1 ファイルに集約、テスト分離困難、`CardData.Attributes` の汎用辞書を効果解釈に流用するのは型安全性低下 |
| `DrowZzzAction` を effect 種別ごとに分割(`PlayDrawCardAction` / `PlayDiscardAction` 等) | Action 階層が効果種別の数だけ増える、UI から見ると同じ「Play」が複数 Action に分かれ統一性低下、`ICardCatalog` を Action 生成側で参照する必要が発生(Presentation 責務漏れ) |
| `EffectInterpreter` を Visitor / Double Dispatch パターンで実装 | 現状の effect 種別数(M2 完了時点で 4〜8 種)では過剰、record + switch で十分、将来肥大化した時点で別 ADR で移行判断 |
| 効果発動を EndTurn 時に集約 | M1 と同じ「Play では Field 移動のみ」挙動になり、M2 のリトマス試験が弱くなる、UI 表現(プレイ → 効果アニメーション → ターン終了)の自然さも損なう |
| 効果発動を `PlayEffectAction` 別 Action に分割 | `DrowZzzTurnPhase` に新フェーズ(`WaitingForEffect` 等)追加、Action 数増加、効果が同期完了する前提ではメリット薄 |
| `ICardCatalog` を変更せず `IEffectProvider<TEffect>` を別 interface 分離 | SO 化時に CardData と Effect 定義が 2 つの asset に分散、Designer ワークフロー悪化、統一 Catalog の方が筋 |
| `ICardCatalog.GetEffects` を `IReadOnlyList<object>` で返す(非ジェネリック維持) | 型安全性が失われ、各実装でキャスト必要、Pure C# の利点が減る |
| `ICardCatalog` をそのまま DrowZzz 固有 namespace へ移動 | 汎用 Application 配置の方針(ADR-0005 §3)に矛盾、他ゲーム転用余地を捨てる |
| SO 化を M2 で実施(ADR-0006 記載通り) | M2 スコープが膨らむ、サブセット 4〜8 枚なら InMemoryCardCatalog で十分、SO 化は永続化(M4)と同時設計する方がコスト効率良 |
| M2 で本物 56 枚フル実装 | PR 数 15〜20 で M2 が肥大化、ADR-0005 の段階的縦串哲学と整合しない、M3 / M4 の独自スコープが薄くなる |
| 山札枯渇でゲーム強制終了 / 再シャッフル(M2 で予防実装) | 現状想定下では発生しない、仕様未確定状態で予防実装すると後で巻き戻しコストが発生、Phase 1 防御例外を維持する方が筋 |
| `IEffect` を `Drowsy.Application` 直下に汎用化 | 現状 DrowZzz 専用、別ゲーム追加時点で汎用化判断する方が Premature Abstraction を避けられる(`ApplyActionUseCase<TAction, TSession>` 汎用化を見送った ADR-0006 §3 と同じ判断) |
| `IEffect.Apply(session)` を effect 自身に持たせる(Internal Iterator) | record としての値同値性を保ちにくくなる(振る舞いを内包すると単純な data 比較で済まない)、Interpreter 外側集約の方がテスト容易 |
| 効果順序を「ランダム / 並列」にする | 現状ルールでは順序付き(プロジェクトオーナー JIT 共有)、ランダム化は仕様未確定でリスク高 |

## Implementation Notes

### M2 着手 PR 群(仮、進行中に調整)

1. **M2-PR1**: 効果インフラ整備(本 ADR の核心)
   - `Drowsy.Application.Games.DrowZzz.Effects` namespace 新設
   - `IEffect.cs`(空 marker interface)
   - `EffectInterpreter.cs`(空の switch、`_` ケースで `NotImplementedException`、§1.3)
   - `ICardCatalog<TEffect>` ジェネリック化(`Drowsy.Application/ICardCatalog.cs` 更新)
   - `Drowsy.Application/ICardCatalog.cs` の XML doc コメント(現状「M2 以降で `ScriptableObject` ベース... を予定」)を本 ADR §5 の決定(M4 へ送る)に合わせて訂正
   - `InMemoryCardCatalog`(`Drowsy.Application/Catalog/InMemoryCardCatalog.cs`)を `ICardCatalog<IEffect>` 実装に更新、全カード Effect 空配列
   - `Drowsy.Application/Catalog/InMemoryCardCatalog.cs` の remarks(現状「M1〜M2 のテスト・skeleton 用途で利用し... M2 以降の `ScriptableObjectCardCatalog`... と並行存続する」)を本 ADR §5 の決定(SO 化は M4)に合わせて訂正
   - `DrowZzzRule.PlayCardAction.Apply` を効果逐次評価形(0 個でも動く)に拡張(§3 コード片)
   - `DrowZzzRule` constructor に `ICardCatalog<IEffect>` / `EffectInterpreter` 追加
   - `StartGameUseCase` constructor の `ICardCatalog` → `ICardCatalog<IEffect>` 型引数変更(§3「設計上の割り切り」)
   - `Drowsy.Application.Tests` の Stubs に `UnknownEffect` ダミー追加、`_` ケーステスト
   - 既存テスト全緑維持(M1 完成時点の 334 件、Application.Tests 129 件のうち `DrowZzzRule` / `ApplyActionUseCase` / `M1IntegrationTests` 周辺は依存更新で再構築)
   - EARS: `docs/specs/application/effect-interpreter.md`(APP-031〜)/ `docs/specs/games/drowzzz/effect-mechanism.md`(DZ-082〜)を新設(M2-PR1 着手時に確定、§要件 ID prefix 参照)

2. **M2-PR2** 〜 **M2-PR(N-1)**: 個別効果 record + 対応カード
   - 1 PR = 1 effect 種別 record 追加(例: `DrawCardsEffect.cs`、フィールド・意味は JIT 共有)
   - `EffectInterpreter` switch に case 追加
   - `InMemoryCardCatalog` に対応カード 1〜2 枚追加(CardData + IEffect 配列)
   - EARS / .feature 追加(APP-/DZ- 連番継続)
   - 効果単体テスト + 統合テスト(複数効果の合成、効果ありカードのプレイループ)

3. **M2-PR-N**(最終、M2 完成 PR):
   - 統合テスト拡充(複数効果カードのプレイ、効果カテゴリ網羅、N=2 数ラウンドの end-to-end)
   - README ステータスバナー更新(「M2 完成」 / 「次は M3 着手前に ADR-0008」)
   - CLAUDE.md §11 確立済 ADR 列に「ADR-0007(M2 詳細)」と「M2 完成記録」を追記
   - 本 ADR-0007 の Implementation Notes に M2 完成記録を追記(ADR-0005 / ADR-0006 と同じ運用)

### `ICardCatalog<TEffect>` への破壊的変更の影響範囲(M2-PR1 で同時更新)

| ファイル | 変更内容 |
| ---- | ---- |
| `Drowsy.Application/ICardCatalog.cs` | `ICardCatalog` → `ICardCatalog<TEffect>` |
| `Drowsy.Application/Catalog/InMemoryCardCatalog.cs` | `: ICardCatalog<IEffect>` に変更、`GetEffects(CardId)` 実装(M2-PR1 では全カード空配列) |
| `Drowsy.Application/Games/DrowZzz/DrowZzzRule.cs` | constructor に `ICardCatalog<IEffect>` / `EffectInterpreter` 追加、`PlayCardAction.Apply` 拡張 |
| `Drowsy.Application/Games/DrowZzz/StartGameUseCase.cs` | constructor の `ICardCatalog` → `ICardCatalog<IEffect>` |
| `Drowsy.Application/Games/DrowZzz/ApplyActionUseCase.cs` | 依存先 `DrowZzzRule` の更新を間接的に受ける(`ApplyActionUseCase` 自体のシグネチャは不変) |
| `Drowsy.Application.Tests/Catalog/InMemoryCardCatalogTests.cs` | `GetEffects` テスト追加 |
| `Drowsy.Application.Tests/Games/DrowZzz/*Tests.cs` | `InMemoryCardCatalog` / `DrowZzzRule` 構築箇所の引数追加(共通テストヘルパー導入機会、`docs/todo.md` の「共通テストヘルパー抽出」TODO 消化候補) |

合計 **7 行(本体ファイル 5 件 + Tests 関連 2 行)** の変更。Tests 関連 2 行(`InMemoryCardCatalogTests` 1 ファイル + `Games/DrowZzz/*Tests.cs` 複数ファイル)は実際の変更ファイル数 4〜6 件相当(`DrowZzzRuleTests` / `ApplyActionUseCaseTests` / `M1IntegrationTests` / `StartGameUseCaseTests`)になる見込み。テスト件数の純増は 5〜10 件程度(`_` ケース / `GetEffects` 単体 / Interpreter 0 個・1 個 / 統合テスト等)。M2-PR1 着手時に最終的なファイル一覧 / テスト件数を確定する。

### Effect record 設計指針(M2-PR2 以降で適用)

ADR-0006 §M1 進行中の学びを継承:

- **record positional + null 防御の二重ガード必須**: バッキングフィールド初期化式 `= Param ?? throw` + init setter 本体 `init => _field = value ?? throw` の両方
- **record + 内部 `Dictionary` / `List` を持つ型の `Equals` / `GetHashCode` 必須 override**: 順序非依存マルチセット同値 or 順序依存シーケンス同値を効果の意味に応じて選択
- **テスト用ダミー派生型による `_` ケースカバレッジ確保**: `UnknownEffect`(Tests assembly 内)
- **JIT 共有方式**: M2 中の細部(個別カードの効果定義 / Attribute キー / 数値 / 対象範囲)はプロジェクトオーナーから着手時に都度受け取る

### 山札枯渇への TODO 登録(本 ADR 起票 PR 同梱)

`docs/todo.md` に以下を追加(本 ADR 起票 PR と同コミット):

- **効果追加時のドロー総数チェック**: 各 M2-PR で効果が「ドロー枚数を増やす」変動を含む場合、`N=2 × MaxRound × 平均 Draw 数 + 初期配布 ≤ 山札サイズ` が成立するか確認するメモを当該 PR の Self-Review チェックリストに含める

### 要件 ID prefix(M2 範囲)

| Prefix | 範囲 | 配置 |
| ---- | ---- | ---- |
| `APP-` | 汎用 Application 層 interface の振る舞い(M2 で拡張: `GetEffects` 等) | `docs/specs/application/<feature>.md` |
| `DZ-` | DrowZzz 固有ルール(M2 で拡張: 効果メカニズム + 個別カード仕様) | `docs/specs/games/drowzzz/<feature>.md` |

M1 で採番済の APP-001〜N / DZ-001〜N から連続採番(M1-PR7 完了時点の数値は ADR-0006 末尾「EARS 235 件」を参照、M2-PR1 着手時に最新値を確認して連番継続)。

**M2-PR1 着手時の実値確定(2026-05-11)**: M1 完成時点で APP- は 001〜030(022 が欠番、apply-action-usecase.md の経緯)、DZ- は 001〜081 が連続採番済。M2-PR1 で以下を新規採番開始する:

| 採番開始 | 配置 | 範囲 |
| ---- | ---- | ---- |
| `APP-031〜` | `docs/specs/application/effect-interpreter.md` | `EffectInterpreter` の振る舞い(`Apply` の入出力契約 / `_` ケース防御例外 / null 防御 等) |
| `DZ-082〜` | `docs/specs/games/drowzzz/effect-mechanism.md` | DrowZzz 固有の効果発動メカニズム(`PlayCardAction.Apply` 内同期発動 / 効果 0 個での M1 互換 / 効果合成の左から順 等) |

`docs/specs/application/card-catalog.md`(APP-006〜010)の更新による追加採番(`GetEffects` 単体仕様、`ICardCatalog<TEffect>` ジェネリック化)は本 M2-PR1 の続編 commit / 別 PR で対応する場合があり、その時点で連続採番継続。

### Phase 2 の進捗バナー更新(M2-PR1 同梱で行わない)

M2-PR1 段階では「M2 着手中」表示には変えず、M2 完成 PR(M2-PR-N)で「M2 完成」に切り替える。理由: ADR 起票 PR(本 PR)と M2-PR1 の境界を明確にする(ADR は計画、PR は実装)。本 ADR 起票 PR では「次は M2 着手前に ADR-0007 起票予定」→「ADR-0007 起票済、M2-PR1 着手予定」程度の表現に留める。

### M2 進行中の TODO 追跡

ADR-0005 / ADR-0006 と同じ運用:後追い chore は `docs/todo.md` で追跡、ルール最適化に伴う EARS 更新は当該 PR で扱う。M2 着手時点で残る未着手 TODO:

- `turn-state.md` から ADR-0006 §7 への相互参照追加(`docs/todo.md` 既存エントリ)
- `IGameConfig.MaxRoundNumber` 追加(M3 着手 PR で消化、本 ADR では追加しない)
- 共通テストヘルパー抽出(M2-PR1 で `ICardCatalog<TEffect>` 破壊的変更に伴うテスト構築箇所統一の好機、消化候補)
- NRT 検討(継続保留、必要性が高まった時点で再評価)
- Roslynator 整合(継続保留)

### M2-PR1 完成記録(2026-05-11)

**完成 PR**: PR #30 `feat(app): ICardCatalog<TEffect> ジェネリック化と DrowZzzRule 効果評価拡張 (M2-PR1 完成)`(merged `f43d7c1`、既存 ADR-0006 §M1 着手 PR 群と同じ表記スタイル)

#### Definition of Done 達成項目

| スコープ項目 | 達成状況 |
| ---- | ---- |
| `IEffect.cs` 新規(空 marker interface) | ✓ |
| `EffectInterpreter.cs` 新規(空の switch、`_` ケースで `NotImplementedException`、§1.3) | ✓ |
| `ICardCatalog<TEffect>` ジェネリック化(`ICardCatalog.cs` 更新) | ✓ |
| `InMemoryCardCatalog` の `ICardCatalog<IEffect>` 実装更新(全カード Effect 空配列) | ✓ |
| `DrowZzzRule.PlayCardAction.Apply` の効果逐次評価形(0 個でも動く)拡張 | ✓ |
| `DrowZzzRule` constructor に `ICardCatalog<IEffect>` / `EffectInterpreter` 追加 | ✓ |
| `StartGameUseCase` constructor の `ICardCatalog<IEffect>` ジェネリック化 | ✓(本 ADR §3「設計上の割り切り」継続) |
| EARS / Gherkin 新規(APP-031〜038、DZ-082〜088) | ✓ |
| 既存テスト全緑維持 | ✓ |

### M2-PR3 完成記録(2026-05-11)

**完成 PR**: PR #35 `feat(app): SDP 機構 + 効果 record とカード「コップ一杯の脅威」を実装 (M2-PR3)`(merged `d03c135`、ADR-0008 §M2-PR2 完成記録と同表記スタイル、本 ADR §1.4 訂正 + ADR-0008 §8 訂正は本 PR #35 内で同梱済)

EARS / NUnit 増加:
- 仕様 ID: DZ-099〜DZ-127(29 ID、`dp-mechanism` / `effects/{adjust-sdp,draw-card-effect,time-of-day-branch}` / `cards/cup-of-threat`)+ APP-039 / APP-040(`in-memory-card-catalog` 拡張)
- NUnit Property: **+37 件**(新規 4 ファイル 25 件:`AdjustSdpEffectTests` 5 / `DrawCardEffectTests` 7 / `TimeOfDayBranchEffectTests` 7 / `CupOfThreatCardTests` 6 + 既存 3 ファイル 12 件:`DrowZzzGameSessionTests` +9 / `StartGameUseCaseTests` +2 / `InMemoryCardCatalogTests` +1)

#### Definition of Done 達成項目(本 ADR §1.4「actor 拡張」JIT 確定の完成)

| スコープ項目 | 達成状況 |
| ---- | ---- |
| 他者影響系 actor 拡張案の JIT 確定(§1.4 案 A 派生の `enum SdpTarget`)| ✓ §1.4 末尾に M2-PR3 JIT 確定文を追記 |
| `SdpTarget enum { Self, Opponent }` 実装 | ✓ `Effects/SdpTarget.cs` |
| `AdjustSdpEffect(SdpTarget Target, int Delta)` record + EffectInterpreter case | ✓(DZ-110〜DZ-113) |
| `DrawCardEffect(SdpTarget Target, int Count)` record + EffectInterpreter case + Opponent 防御 | ✓(DZ-114〜DZ-118) |
| `InMemoryCardCatalog` の 2 段 constructor(`entries, effects`) | ✓(APP-039 / APP-040) |
| `EffectInterpreter.Apply` シグネチャは `(session, effect)` 維持(§1.4 案 B 不採用) | ✓ |

#### 本 PR が確定した ADR-0007 内の JIT 判断ポイント

- **§1.4「他者影響系 actor 拡張」**: 案 A サブセット(`enum SdpTarget` 引数)を採用、案 B(`EffectInterpreter` シグネチャ拡張)は不採用と確定
- 実装名 `SdpTarget` は将来 DDP / FDP 操作系効果が増えた時点で `EffectTarget` への改名 or 別 enum 新設を再評価(`docs/todo.md` 追跡候補)

#### M2-PR3 進行中の学び

- **breaking change の波及**: `DrowZzzGameSession` の 3 引数 → 4 引数 constructor 変更は全 4 テストファイル(`DrowZzzGameSessionTests` / `ApplyActionUseCaseTests` / `DrowZzzRuleTests` / `StartGameUseCaseTests`)+ `StartGameUseCase` 実装 1 件の機械的修正で完結した。ヘルパー関数 `Sdp()`(引数なしで `[p1=0, p2=0]` を返す)を導入することで既存テストの修正は最小化
- **`ICardCatalog` 2 段 constructor**: 後方互換維持の overload 方式(`(entries)` → `(entries, null)` 内部委譲)で M1〜M2-PR2 のテストは無変更で通る。Breaking change を避けつつ機能拡張する手法として、`InMemoryCardCatalog` 拡張は他の similar API 拡張時の参考になる
- **code-reviewer 7 件指摘の反映**: 警告 3 件すべて反映(`SdpTarget` 流用意図 XML doc 明記 / `TimeOfDayBranchEffect` list 要素 null 構築時防御 / トレーサビリティ表とテスト名整合)、提案 4 件中 2 件反映、2 件 Skip(SDP 二重コピー / `GetHashCode` 空 list 初期値 0、現状性能問題なし)

### M2-PR4 完成記録(2026-05-11)

**完成 PR**: PR #37 `feat(app): DDP 機構 + DdpPool 値オブジェクト + 自動抽選機構を実装 (M2-PR4)`(merged `84966ef`、本 ADR §3 の `DrowZzzRule` constructor 2 引数維持の JIT 判断同梱、ADR-0009 §M2-PR4 完成記録と相互参照)

EARS / NUnit 増加:
- 仕様 ID: DZ-128〜DZ-154(`dp-mechanism-ddp` / 既存 `dp-mechanism` DZ-103 を 3 項合計に整合更新)+ CFG-103(`IGameConfig.DdpPool`)
- NUnit Property: **+33 件**(新規 2 ファイル 13 件:`DdpPoolTests` 9 / `StubGameConfigTests` 4 + 既存 3 ファイル 20 件:`DrowZzzGameSessionTests` +12 / `DrowZzzRuleTests` +4(DZ-141/142 は `[TestCase]` 5 ケース展開で実行 +10、Property カウントは +1 ずつ)/ `StartGameUseCaseTests` +4)

#### Definition of Done 達成項目(本 ADR §3「`DrowZzzRule` constructor」JIT 判断の完成)

| スコープ項目 | 達成状況 |
| ---- | ---- |
| 本 ADR §3 で挙げた「`DrowZzzRule` constructor に `IRandomSource` を追加するか」の JIT 判断 | ✓ M2-PR4 で「採用しない」を確定。`StartGameUseCase` で `DdpPool.Shuffle(rng)` を事前実行する設計で Rule 内 rng が不要となり、ADR-0007 §3 の 2 引数 constructor を維持 |
| `EndTurnAction.Apply` 内に DDP 自動抽選機構を追加(`PlayCardAction.Apply` 内同期発動の §3 設計と同パターンで EndTurn 内自動進行) | ✓ ターン境界(`CurrentPlayerIndex == 0`)+ 新ターン番号 ∈ DrawRounds {5,9,13,17,21} で N 枚抽選、`DrawDdpForAllPlayers` private static |
| 既存 `PlayCardAction.Apply` 内 effect 評価ロジックは無変更 | ✓ M1〜M2-PR3 と完全互換、本 PR は EndTurn 側のみ拡張 |
| ADR-0009 §3 / §4(DDP プール構造 / 抽選タイミング)の最初の実装 | ✓ DDP 機構の最小実装、ADR-0009 §M2-PR4 完成記録と相互参照 |
| §6「ScriptableObject 化 / Editor アセット」(M4 候補)| △ M4 へ送る方針継続、本 PR では `StubGameConfig` 2 段 constructor + 既存 `IGameConfig.DdpPool` 経由で IL2CPP / WebGL 安全性は維持 |

#### 本 PR が確定した ADR-0007 内の JIT 判断ポイント

- **§3「`DrowZzzRule` constructor 引数」**: ADR-0009 §4 で挙げた「rng を `DrowZzzRule` に注入する案」を**採用しない**と本 PR で再評価。`StartGameUseCase` で `DdpPool.Shuffle(rng)` を 1 回事前実行する設計のため Rule 内 rng は不要。`dp-mechanism-ddp.md` §「設計判断」§「`DrowZzzRule` constructor」に経緯記録、ADR-0007 §3 の 2 引数 constructor シグネチャは維持

#### M2-PR4 進行中の学び

##### 学び 1: ADR 内の数値計算誤記の発覚と訂正運用

- **事象**: ADR-0009 §「DDP プールの構造」起票時「13 種 × 3 枚 = 36 枚」と書かれていたが、数学的に 13 × 3 = 39 で計算誤記
- **発覚**: 実装着手後の NUnit テスト失敗(`Expected 36, but was 39`)
- **対応**: プロジェクトオーナー JIT 確認で「39 枚が正、ADR 表記を訂正」と確定。本 PR で ADR-0009 / 関連仕様 / 実装 xmldoc / テスト / TODO を一括訂正同梱(ADR-0001「Accepted 直接編集」運用)
- **再発防止**: **今後 ADR 起票時の Self-Review に「列挙値の積算と総数の整合性チェック」項目を追加すべき**(`docs/todo.md` 追跡候補)

##### 学び 2: breaking change の波及最小化

- **事象**: `DrowZzzGameSession` の 4 引数 → 6 引数 constructor 変更は全 11 テストファイルに波及
- **対応**: 各ファイルのヘルパー(`NewSession` / `Sdp()`)に `ddp` パラメータと `EmptyDdpPool` を追加することで既存テストの修正は最小限に
- **教訓**: M2-PR3 の 3→4 引数拡張で確立した「ヘルパー再利用」パターンが M2-PR4 でも有効

##### 学び 3: 専用値オブジェクト vs Domain 流用の判断軸

- **事象**: ADR-0009 §3 が「`Pile` 型を再利用」と書いていたが、`Pile` は `CardId[]` 専用で整数プールには semantic 違反
- **対応**: 専用 `DdpPool` を Application 層に新設
- **不採用案**: `Pile` ジェネリック化(`Pile<T>`)は影響範囲が広く本 PR スコープ外
- **教訓**: **型の semantic と利用先の意味的整合を ADR の「再利用」記述より優先する判断軸を確立**

##### 学び 4: Unity NUnit が `Assert.Multiple` 未対応

- **事象**: Unity Test Framework の NUnit バージョンが `Assert.Multiple` を含まない
- **対応**: 複合不変条件の検証は個別 `Assert.That` 並列に変更(最初の失敗で停止)
- **追跡**: `docs/todo.md` 追跡候補(`Assert.Multiple` サポート版への upgrade 可能性 / 代替パターンの確立)

##### 学び 5: code-reviewer 9 件指摘の反映

- **警告 4 件すべて反映**:
  - 設計判断文の反転誤記訂正
  - `IdentityRandom` コメント正確化
  - `[TestCase]` 慣例
  - Domain xmldoc から Application 名削除
- **提案 5 件中 3 件反映、2 件 Skip**: 命名議論 / `M1IntegrationTests` 命名は別 PR / ADR 候補

### M2 完成記録の追記タイミング(後続 PR で実施)

本 ADR の M2-PR1 / M2-PR3 / M2-PR4 完成記録は §直上に追記済。**M2 全体の完成(M2-PR-N 最終 PR)** 時点で、本 ADR §M2 完成記録(全体)を別途追加し、Definition of Done 達成方法 / 最終的な PR 群一覧 / 完成日を集約する(ADR-0005 §M1 完成記録 / ADR-0006 §M1 着手 PR 群 の運用と同じ)。M2-PR2(DrowZzzClock)完成記録は ADR-0008 §M2-PR2 完成記録、M2-PR5 以降(後続効果カード)は当該 PR 単位で本 ADR or 関連 ADR に追記する。

## Related

- 前提: [ADR-0001 ADR Operations](0001-adr-operations.md)
- 前提: [ADR-0002 Phase 1 Domain 拡張の集約境界と概念モデル](0002-phase1-domain-boundaries.md) — Domain ゲーム非依存原則 / `CardData.Attributes` の data 性質
- 前提: [ADR-0003 TODO 運用](0003-todo-operations.md) — 後追い chore の追跡 / 本 ADR 起票 PR 同梱の TODO 追加
- 前提: [ADR-0004 IsExternalInit polyfill](0004-init-setter-polyfill.md) — record + init + with パターンを M2 でも本格利用
- 前提: [ADR-0005 Phase 2 Roadmap](0005-phase2-roadmap-drowzzz.md) — M2 のスコープ確定、本 ADR で粒度を細分化(「1 PR = 1 効果カテゴリ」→「1 PR = 1 効果 record」)
- 前提: [ADR-0006 M1 詳細](0006-m1-detail-application-interfaces.md) — 汎用 Application 層 interface / DrowZzz 固有実装 / `IGameAction` + `DrowZzzRule` のパターンを `IEffect` + `EffectInterpreter` に転用、SO 化のタイミング判断を本 ADR で M2 → M4 に変更
- 関連: [`docs/specs/application/`](../specs/application/) — M2-PR1 以降で APP-301 等の要件追加
- 関連: [`docs/specs/games/drowzzz/`](../specs/games/drowzzz/) — M2-PR2 以降で個別効果 / 個別カードの EARS / .feature 追加
- 関連: [`docs/specs/domain/configuration/game-config.md`](../specs/domain/configuration/game-config.md) — M3 着手 PR で `MaxRoundNumber` 追加予定(ADR-0006 §1.4 / 本 ADR §「山札枯渇」計算根拠)
- 関連規約: [`CLAUDE.md`](../../CLAUDE.md) §5 アーキテクチャ依存ルール / §6 テスト方針 / §11 ADR 運用 / §12 TODO 追跡
- 後続: M2-PR1 〜 M2-PR-N(進行中)
- 後続: [ADR-0008 M2 — DrowZzzClock 概念と「夜・朝」フェーズの導入](0008-m2-drowzzz-clock-and-night-morning.md)
- 後続: [ADR-0009 M2-M3 — DP 機構と勝利条件](0009-m2-m3-dp-and-victory-conditions.md)(本 ADR §「山札枯渇」の数値訂正を §「ADR-0007 訂正項目」で同梱、Clock 仕様 21 ラウンド化に伴う再検算)
- 後続: ADR-0010 候補(M3 詳細、起票予定 — `IsTerminated` / `GetWinner` の本格実装、ラウンド上限到達時の Clock 処理)
