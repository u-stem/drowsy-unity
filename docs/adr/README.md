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
