# ADR-0027: CLAUDE.md の役割再定義 — 規約 SSoT に絞り変動状態を docs へ分離

- Status: Accepted
- Date: 2026-05-29
- Decider: -

---

## Context

CLAUDE.md は毎セッション context にロードされる「常駐メモリ」である。肥大化が AI(本セッションの Claude)の判断品質を実際に低下させていた。

### 肥大化の弊害

- **context 消費**: 再構成前で約 34KB(推定 8〜9 千トークン)。毎ターン読まれ、実作業に使える窓を圧迫する。
- **指示の埋没(lost in the middle)**: 重要な禁止事項が大量の状態記述に埋もれ、拾われにくくなる。CLAUDE.md 自身が「重要な指示ほど先に置く」と書いていたのは、後半が薄れることの裏返し。
- **古い情報による誤判断**: 整備時点で README / CLAUDE.md の ADR 数・Phase 進捗が古く、誤った前提で動きかけた。変動情報を常駐メモリに置くと、更新漏れがそのまま誤動作になる。

### 構造的な二重管理

- §5(アーキテクチャ)/ §6(テスト)/ §7(機械検知)/ §8(ワークフロー)/ §9(定数)は、いずれも `docs/` に詳細版があるのに CLAUDE.md がミラーしていた。
- §7 機械検知(約 75 行)と §6 テスト(約 55 行)が肥大の主因。
- 「2026-05-13 に追加」等の時系列注記が散在していた(履歴は git が持つべき)。

本 ADR は ADR-0026(ドキュメント・コメント簡潔性ポリシー)を CLAUDE.md の構造そのものに適用するものである。

## Decision

### 1. CLAUDE.md は「不変の規約 + docs への地図」に絞る

- CLAUDE.md に**変動する状態を置かない**(Phase 進捗・バージョン番号・件数・時系列注記)。
- `docs/` に詳細がある規約は、要点数行 + リンクに圧縮する。
- 規約の Single Source of Truth は、詳細を持つ `docs/` 側に置く。

### 2. 新しい章立て(9 章)

1. 言語規約
2. アーキテクチャ規約(→ `docs/architecture/dependency-rules.md`)
3. テスト規約(→ `docs/testing-strategy.md`)
4. 機械検知(→ `docs/machine-detection.md` + `lefthook.yml`)
5. ワークフロー(→ `docs/workflow.md`)
6. 定数管理(→ `docs/architecture/constants-management.md`)
7. ADR / TODO 運用(→ `docs/adr/README.md` / `docs/todo.md`)
8. Claude の振る舞い規範
9. グローバル規約の継承

### 3. 状態情報の移設先

| 移設対象(旧 CLAUDE.md) | 移設先 |
| ---- | ---- |
| §11 Phase 進捗 | `docs/roadmap.md`(現在地のみ、詳細は ADR-0005) |
| §7 機械検知の詳細(検知レイヤ表 / 検知対象表 / Roslyn 構成 / lefthook 構成 / CLI スクリプト) | `docs/machine-detection.md` |
| §4 バージョン番号(Unity / URP / VContainer 等) | README(既出に集約) |
| §7 等の時系列注記(「2026-05-13 に追加」等) | 削除(履歴は git が保持) |

## Consequences

### Positive

- context 消費が削減され(34KB → 8〜10KB 目標)、毎ターンの常駐コストが下がる。
- 重要な規約・禁止事項が埋もれず拾われやすくなる。
- 二重管理が解消され、更新漏れ起因の誤判断が起きなくなる。
- 状態は変動を許容する `docs/` で管理され、CLAUDE.md は安定する。

### Negative

- 規約の詳細を見るには `docs/` を開く一手間が増える。
- 再構成の一時コストが発生する。

### Neutral

- 規約の内容自体は不変。置き場所のみを変える。
- ADR 本体の書式(ADR-0001)・コミット規約は不変。

## Related

- ADR-0026(ドキュメント・コメント簡潔性ポリシー)の延長。
- ADR-0001(ADR 運用)。
- 移設先: `docs/machine-detection.md` / `docs/roadmap.md` / README。
