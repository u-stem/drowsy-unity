# ADR-0001: ADR 運用の開始

| 項目 | 値 |
| ---- | ---- |
| Status | Accepted |
| Date | 2026-05-09 |
| Decider | プロジェクトオーナー |

## Context

Phase 0 では環境セットアップ(lefthook / gitleaks / Roslyn Analyzer / asmdef 4 層構成)と最初の Domain 骨格(`CardId` / `Pile` / `IRandomSource` / `XorShiftRandom`)を整え、要件トレーサビリティ ID 体系と定数管理 L1〜L5 を確立した。これらの判断は CLAUDE.md 各章および `docs/architecture/*.md` に断片的に記録されているが、

- **判断の根拠と代替案の検討経緯が残っていない**(結論のみ記録)
- **後から覆る可能性のある判断と、不変原則とが同じ場所に混在している**
- **Phase 1 で Hand / Deck / Player / GameState を追加するにあたり、集約境界・Immutability・Card 抽象などの設計判断をまとめて確定する必要が出た**

ユーザーグローバル `~/.claude/CLAUDE.md` および `~/ws/claude-system/principles/02-decision-recording.md` で「3 年後の自分に聞かれて答えに窮する判断は ADR に残す」方針が確立されているが、これまで本プロジェクトには ADR の運用が無かった。Phase 1 着手のタイミングで運用を立ち上げる。

## Decision

本プロジェクトでは ADR を以下の規約で運用する。詳細は [`docs/adr/README.md`](README.md) に集約する。

| 項目 | 値 |
| ---- | ---- |
| 配置 | `docs/adr/` |
| ファイル名 | `NNNN-kebab-case-title.md`(4 桁ゼロ埋め連番) |
| 欠番 | 禁止(撤回しても番号は残す) |
| 必須セクション | Context / Decision / Consequences / Related |
| Status 語彙 | Proposed / Accepted / Rejected / Withdrawn / Deprecated / Superseded by NNNN |
| インデックス | `docs/adr/README.md` の表で一元管理 |
| コミット規約 | `docs(adr): <日本語説明>`(Conventional Commits、type は英語) |
| 識別子規約 | Decider 欄に本名・新規連絡先・自分の handle literal を書かない |
| Public/Private 境界 | Public 文書から Private リソースへのリンクを書かない |

ADR が対象とする判断の範囲・起票しない判断・起票手順は `docs/adr/README.md` を参照する。

CLAUDE.md にも「11. 意思決定記録 (ADR)」セクションを追加し、配置と参照先を明記する。

## Consequences

### Positive

- アーキテクチャ判断の経緯を後から辿れる(「なぜこうしたのか」「何を採らなかったのか」が残る)
- 設計判断と不変原則が分離される(ADR は時間軸を持つ判断、principles は時間軸を持たない原則)
- 個人開発でも、休止期間後の自分が文脈を復元できる
- ユーザーグローバル / claude-system 側の ADR 運用と語彙が揃い、別プロジェクト間で書式の認知負荷が無い

### Negative

- ADR を書く手間が増える(1 件あたり 30 分程度)
- 起票判断のオーバーヘッド(「これは ADR レベルか?」を毎回考える)
- 過剰起票のリスク(軽微な判断まで ADR にすると価値が薄まる)

### Neutral

- 今後の主要判断は CLAUDE.md と ADR の両方を確認する必要がある(CLAUDE.md は規約、ADR は判断の根拠)

## Alternatives Considered

- **CLAUDE.md と docs/architecture/ のみで運用を続ける**: 既存方式。判断の根拠と代替案検討が残らないため不採用
- **GitHub Issue で意思決定を記録する**: Issue は流れて消える(close 後の検索性が低い)、リポジトリと履歴が分離される、オフラインで参照しにくいため不採用
- **Notion / 外部ツールで管理**: コードと連動した版管理ができない、Public/Private 境界が曖昧になるため不採用

## Related

- 後続: [ADR-0002 Phase 1 Domain 拡張の集約境界と概念モデル](0002-phase1-domain-boundaries.md)
- 関連: [`docs/adr/README.md`](README.md) — 運用規約とインデックス
- 関連: [`CLAUDE.md`](../../CLAUDE.md) §11 — プロジェクト規約への組み込み
- 上位原則: ユーザーグローバル `~/.claude/CLAUDE.md` および `~/ws/claude-system/principles/02-decision-recording.md`(Private リソースで参照不能)
