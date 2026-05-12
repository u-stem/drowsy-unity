# Architecture Decision Records (ADR)

本プロジェクトの主要な設計判断を、後から経緯を辿れる形で記録する。
「3 年後の自分に聞かれて答えに窮する判断」を残すことが目的。

---

## いつ ADR を起票するか

- アーキテクチャ全体に波及する設計判断(集約境界、レイヤ責務、Immutability ポリシー など)
- 機械的ガードレール(hooks / lefthook / CI / Roslyn Analyzer / asmdef 設定)を新設・撤去するとき
- 既存設計を覆す変更
- 「なぜこうしなかったのか」を将来問われたときに記憶では答えられないと予想されるとき

逆に以下では起票しない:
- 単純なバグ修正・typo 直し
- 1 ファイル内で完結するリファクタ
- パッケージのマイナー更新(破壊的変更を伴わないもの)
- 軽微な文言調整

## ファイル名規則

```
docs/adr/NNNN-kebab-case-title.md
```

- `NNNN`: 4 桁ゼロ埋めの連番(`0001`, `0002`, ...)
- 欠番禁止。撤回しても番号は残す
- 英小文字・ハイフン区切りの kebab-case

## Status 語彙

| Status | 意味 |
| ---- | ---- |
| `Proposed` | 議論中。Decision が暫定または未確定 |
| `Accepted` | 採用済み。現在有効な決定 |
| `Rejected` | 不採用。検討したが採らなかった案を記録として残す |
| `Withdrawn` | 起票者が取り下げた |
| `Deprecated` | 役割を終えた(後続 ADR で置き換えなし) |
| `Superseded by NNNN` | 後続 ADR で置き換えられた |

新規 ADR は基本 `Accepted` で起票する。議論を残したい場合のみ `Proposed` を使い、合意後に `Accepted` へ書き換える。

## 必須セクション

すべての ADR は以下 4 セクションを必ず埋める。

1. `Context` — なぜこの判断が必要になったか(背景・制約・代替案の事情)
2. `Decision` — 何を決めたか(曖昧さなく記述。条件分岐があるなら表で示す)
3. `Consequences` — 結果として生じる影響を Positive / Negative / Neutral の 3 区分で
4. `Related` — 関連 ADR 番号、関連 Phase / コミット / ファイル、関連ドキュメント

任意セクション(複雑な決定で推奨):
- `Alternatives Considered` — 検討した他案を 1〜2 行ずつ
- `Implementation Notes` — 実装時の注意事項

## 起票手順

1. 直近 ADR 番号 + 1 を採番(`ls docs/adr/[0-9]*.md | tail -1` で確認)
2. `NNNN-kebab-case-title.md` を作成、必須セクション + Status を埋める
3. 本 README のインデックス表に 1 行追加
4. 既存 ADR を覆す場合は旧 ADR の Status を `Superseded by NNNN` に更新
5. feature ブランチで commit、PR 作成(`docs(adr): ...` 形式)
6. PR description に判断の要旨と影響範囲を記載
7. Self-Review(`code-reviewer` subagent または `/ultrareview`)を実施し、結果を PR コメントに添付してマージ

## 個人情報・Public/Private 境界

- Decider 欄に本名・新規連絡先・自分の handle literal を書かない(`プロジェクトオーナー` のような抽象語を使う)
- Public 文書から Private リソース(URL / git remote)へのリンクを書かない
- 旧設計に言及するときは「Private リソースで参照不能」と明記する

## インデックス

| # | タイトル | Status | 概要 |
| ---- | ---- | ---- | ---- |
| [0001](0001-adr-operations.md) | ADR Operations | Accepted | 本プロジェクトで ADR 運用を開始する。配置・命名・書式を確立 |
| [0002](0002-phase1-domain-boundaries.md) | Phase 1 Domain 拡張の集約境界と概念モデル | Accepted | GameState 単一ルート集約、CardId+CardData(等値性 override 必須)、Domain 全体 immutable に統一、N 人プレイヤー想定 |
| [0003](0003-todo-operations.md) | TODO 運用と docs/todo.md の新設 | Accepted | リポジトリ内 `docs/todo.md` で TODO 追跡。Title/Why/Done when/Related/Notes テンプレート、未着手/進行中/完了済み 3 セクション、30 件アーカイブ |
| [0004](0004-init-setter-polyfill.md) | C# 9 init setter / record with 式のための IsExternalInit polyfill 採用 | Accepted | Unity 6 / Mono に欠ける `System.Runtime.CompilerServices.IsExternalInit` を `internal static class` として Domain assembly に追加。record + init + with 式を Domain で使えるようにする |
| [0005](0005-phase2-roadmap-drowzzz.md) | Phase 2 Roadmap — DrowZzz の段階的縦串実装 | Accepted | 本命ゲーム DrowZzz を `drowsy-unity` 同居で段階的縦串実装、M1〜M5 マイルストーン分割、ロジック先行 + ルール最適化と並行 EARS 化 |
| [0006](0006-m1-detail-application-interfaces.md) | M1 詳細 — 汎用 Application 層 interface と DrowZzz 最小実装の設計 | Accepted | `IGameAction` / `IGameRule<TAction, TSession>` / `ICardCatalog` の最小 API、DrowZzz 4 Action + GameSession + TurnPhase + Rule、ハイブリッド UseCase、Phase 1 サブターン vs DrowZzz ラウンドの用語整理 |
| [0007](0007-m2-detail-card-effects.md) | M2 詳細 — カード効果メカニズム(`IEffect` + `EffectInterpreter`)とサブセット先行スコープ | Accepted | `IEffect` マーカー + record 階層 + `EffectInterpreter`、`ICardCatalog<TEffect>` ジェネリック化、`PlayCardAction.Apply` 内同期発動、M2 サブセット 4〜8 枚先行、SO 化は M4 に送る、山札枯渇は現状想定下では発生しない |
| [0008](0008-m2-drowzzz-clock-and-night-morning.md) | M2 — DrowZzzClock 概念と「夜・朝」フェーズの導入 | Accepted | `DrowZzzClock(int RoundNumber)` 値オブジェクト(Hour / Minute / IsNight / IsMorning)、Session の computed プロパティ経由、真の単一情報源は `TurnNumber`、21:00 開始 / 21 ラウンド / 24 時間制 mod 24、夜 21:00〜04:30(Round 1〜16)と朝 05:00〜07:00(Round 17〜21)、ラウンド上限到達処理は M3。ADR-0009 起票時に Clock 21 ラウンド化を境界訂正 |
| [0009](0009-m2-m3-dp-and-victory-conditions.md) | M2-M3 — DP 機構(FDP / DDP / SDP)と持ち点・勝利条件 | Accepted | FDP / DDP / SDP の 3 種別 DP、持ち点 = 合計 (computed)、DDP プール 39 枚共有(13 種 × 3 枚、-30〜+30、起票時「36 枚」表記は計算誤記、M2-PR4 PR で訂正)、DDP 抽選 5 回(Round 5/9/13/17/21)、早期勝利(15 ターン以内に持ち点 ≥ 100 + 特定カード)+ 終了時勝利(持ち点低い方)、DP 機構は M2 / 勝敗判定本格実装は M3、ADR-0008 / ADR-0007 の境界訂正(Clock 21 ラウンド化)同梱 |
| [0010](0010-m3-game-termination-and-victory-determination.md) | M3 詳細 — ゲーム終了判定と勝者決定 | Accepted | `IGameRule.IsTerminated` / `GetWinner` を generic interface に追加、`GameOutcome`(`WinnerOutcome` / `DrawOutcome`)を Domain に新設、`DrowZzzGameSession.Outcome` 8 引数 ctor 化、`EarlyWinTriggerEffect` 効果 record + 評価ロジック(夜 + 持ち点 ≥ 100 で発火)、Round 21 完了で `EndTurnAction.Apply` 内 Outcome 設定、引き分け仕様確定(TotalPoints 等値で `DrawOutcome`、tiebreaker なし)、`MaxRoundNumber` は `DrowZzzClockConstants` 維持(`IGameConfig` 不採用)、`EarlyWinScoreThreshold = 100` を新規 `DrowZzzVictoryConstants` に集約 |
| [0011](0011-m3-dream-card-and-game-mechanics-expansion.md) | M3 詳細(拡張) — 「夢」カード + ゲームメカニクス拡張 | Accepted | 6 機構(連想 / 放棄 / ベッド破損 / キーワード能力(狂乱・本能・反撃)/ ターン構造詳細化 / カード No.00「夢」)を統合整理。新規 Action 派生型 `AssociateAction` / `AbandonAction` / `CounterAction`、`DrowZzzGameSession.BedDamages` 追加(9 引数 ctor 化、5 度目の breaking change)、`KeywordedEffect` ラッパー record で効果単位のキーワード付与、ADR-0010 §5 `EarlyWinTriggerEffect` の発動条件を本 ADR §7 で拡張(夢カード経由の多段階発動)、M3-PR2(ベッド)/ PR3(放棄)/ PR4(連想)/ PR5(キーワード能力)/ PR6(夢統合)に PR 分割、JIT 確認待ち項目を §で明示しつつ機構の枠組みを確定 |
| [0012](0012-m4-scriptableobject-and-persistence.md) | M4 詳細 — ScriptableObject 化 + 永続化 + ユーザー設定 | Accepted | メインスコープ(`ScriptableObjectCardCatalog` / `IGameConfig` SO 実装 `DrowZzzGameConfigAsset` / `IEffect` の SO 表現 / 既存 3 カード No.00 / No.01 / No.02 の SO 移行)+ サブスコープ(`DrowZzzGameSession` JSON 永続化 / `IUserSettings` PlayerPrefs 実装)、M4-PR1〜PR7 に PR 分割、ADR-0007 §5「SO 化は M4」と ADR-0011 §「ADR-0012 候補」の確約を実装計画化、`Drowsy.Infrastructure.Tests` asmdef 新設、`InMemoryCardCatalog` ↔ `ScriptableObjectCardCatalog` 併存戦略、ADR-0006 §1.3 → ADR-0007 §5 → 本 ADR §1 の 3 段階意思決定履歴を維持 |
