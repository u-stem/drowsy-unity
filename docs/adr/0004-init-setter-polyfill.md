# ADR-0004: C# 9 `init` setter / record `with` 式のための IsExternalInit polyfill 採用

| 項目 | 値 |
| ---- | ---- |
| Status | Accepted |
| Date | 2026-05-10 |
| Decider | プロジェクトオーナー |

## Context

PR-3 (PlayerState) で `record class PlayerState` に `init` setter + バッキングフィールドのパターンを導入し、コンストラクタ経由と `with` 式経由の両方で null 検証を効かせる設計を採用した。これにより PLAYER-014(`with` 式の正常動作)と PLAYER-017 / PLAYER-018(`with { Id = null }` / `with { Hand = null }` の防御)が同じ `init` setter で実現される。

しかし Unity Editor でビルドしたところ、以下のコンパイルエラーが発生した:

```
Assets/_Project/Scripts/Domain/Players/PlayerState.cs(27,13): error CS0518:
  Predefined type 'System.Runtime.CompilerServices.IsExternalInit' is not defined or imported

Assets/_Project/Scripts/Domain/Players/PlayerState.cs(34,13): error CS0518:
  Predefined type 'System.Runtime.CompilerServices.IsExternalInit' is not defined or imported
```

C# 9 で導入された `init` accessor および `record` の `with` 式は、コンパイラが `System.Runtime.CompilerServices.IsExternalInit` という marker 型の存在を要求する。.NET 5 以降の BCL には標準で含まれているが、Unity 6 (`6000.4.6f1`) が同梱する Mono / .NET Standard 2.1 相当の SDK には含まれないため、未定義のまま使用するとビルドが失敗する。

これは Unity プロジェクトで C# 9 機能を使う際に広く知られた制約で、Unity / Mono / .NET Standard 2.1 環境向けの標準的なワークアラウンドとして「`internal static class IsExternalInit { }` を assembly 内に定義する shim パターン」が確立されている。

本プロジェクトの Domain では PlayerState 以降、`record + init + with` 式を以下のような場面で使うことを想定:

- PR-4 (GameState): 5 フィールド(Players / Deck / Discard / Field / Turn)を持つルート集約。`gameState with { Deck = newDeck }` のような単一フィールド更新が頻発する
- PR-5 (TurnState): ターン番号 / 現在プレイヤーの状態更新
- 後続 Phase の Domain 値オブジェクトでも `record + with` 式は強力な手段

そのため、PlayerState 単体のビルドエラー回避にとどまらず「Domain 全体で `record + init` を使えるようにする」という Phase 全体に波及する設計判断が必要となる。

## Decision

**Domain assembly に `Assets/_Project/Scripts/Domain/Compat/IsExternalInit.cs` を polyfill として追加する。**

| 項目 | 値 |
| ---- | ---- |
| ファイル | `Assets/_Project/Scripts/Domain/Compat/IsExternalInit.cs` |
| 内容 | `namespace System.Runtime.CompilerServices { internal static class IsExternalInit { } }` |
| アクセシビリティ | `internal`(assembly 境界を越えない、将来 BCL に追加されても衝突しない) |
| 配置ディレクトリ | `Domain/Compat/`(将来の互換 polyfill を集約する場所として確保) |

### 他 assembly での扱い

`internal` は assembly 境界を越えないため、Application / Infrastructure / Presentation の各 assembly で `record + init` を使う場合は、それぞれの assembly 内に同じ shim をコピーする必要がある。各 assembly の `Compat/IsExternalInit.cs` として配置する規約とする(将来 record + init を最初に必要とする PR で同時に追加し、本 ADR を Related として参照する)。

### 撤去条件

将来 Unity / Mono の BCL に `IsExternalInit` が組み込まれた場合(または Unity が .NET 6+ runtime を採用した場合)、`internal` は重複定義が許容されるため即座のエラーにはならないが、不要になった polyfill は本 ADR の `Status` を `Deprecated` に更新したうえで削除する。撤去判断のトリガは「`IsExternalInit` を消してビルドが通るか」を検証して判断する。

## Consequences

### Positive

- `record + init + with` 式を Domain 全般で使える。PR-4 (GameState) で 5 フィールドを `with { ... }` で更新する設計が選択肢になる
- PlayerState の現設計(`init` setter + バッキングフィールド + `value ?? throw` での null 防御)が維持され、PLAYER-014 / 017 / 018 の要件をシンプルに実装できる
- `record` の auto-equals が標準的に使えるため、内部に値同値が壊れる構造(辞書など)を持たない値オブジェクトは boilerplate 削減
- polyfill ファイルは 1 ファイル + 数行で、メンテコストは極小

### Negative

- 各 assembly に同じ shim をコピーする必要がある(`internal` 制約)。Phase 1 では Domain だけで済むが、Application 以降で record + init を使う際は重複コードが増える
- 将来 Unity SDK に `IsExternalInit` が組み込まれた場合、polyfill が冗長化する(`internal` のため衝突エラーは出ないが、コードノイズになる)
- 「Unity の SDK 制約に手を入れる」ことの心理的コスト(初見の開発者が「これは何?」と一度調べるコストが発生する)。本 ADR と shim 内コメントで根拠を明示することで緩和

### Neutral

- 本 polyfill 採用により「Domain は `record + init` を積極的に使ってよい」という方針がプロジェクト規約として確立される
- ADR-0002 では「`Pile` / `Hand` / `CardData` は内部辞書/配列のため sealed class + IEquatable」と決めたが、`PlayerState` のような単純な値オブジェクトは record で書ける、という基準が明確になる

## Alternatives Considered

| 案 | 不採用理由 |
| ---- | ---- |
| `PlayerState` を `sealed class + IEquatable<PlayerState>` に書き換え、`with` 式の代わりに `WithHand(Hand)` メソッドを定義 | 機能的には等価だが、後続 PR (GameState など)で `record + with` を使う道が閉ざされる。GameState は 5 フィールドあり `WithDeck(Pile)` / `WithDiscard(Pile)` / `WithField(Pile)` / `WithPlayers(IReadOnlyList<PlayerState>)` / `WithTurn(TurnState)` の 5 メソッドを書く必要があり、record + with の方が圧倒的に簡潔 |
| `init` を諦めて getter only に戻す(record だが `with` 式は使えない) | record の最大の利点である `with` 式が失われる。状態更新が「`new GameState(prev.Players, newDeck, prev.Discard, prev.Field, prev.Turn)` のように全フィールド渡し」になり、フィールド数が増えると保守困難 |
| Domain で `record` 自体を禁止する | CardId / PlayerId は record で十分整合的に書けており禁止すると逆に冗長になる。ADR-0002 で「内部辞書/配列がある型は sealed class」「単純値オブジェクトは record」という基準を採っているのと矛盾する |
| C# のバージョンを下げる(`<LangVersion>` 設定) | Unity の C# バージョンは Editor バージョンに紐付き、勝手に下げられない。また `record` は C# 9 機能なので record ごと使えなくなる |
| Unity の SDK / Mono runtime を上げる | プロジェクト都合で Unity 全体の SDK バージョンを変更するのは過剰、互換性リスクも高い |

## Implementation Notes

### 配置ディレクトリ `Compat/` の意図

`Domain/Compat/` を「Unity / Mono の BCL 不足を補う互換 polyfill の集約場所」として確保する。本 PR で `IsExternalInit.cs` 1 ファイルを置く。将来似た shim が必要になった場合(例: `System.Index` / `System.Range` polyfill など)も同ディレクトリに集約する。

### 機械検証の振る舞い(本 PR で対応)

- `Compat/IsExternalInit.cs` は仕様(EARS)を持たない polyfill ファイル
- lefthook の `check-spec-files.sh` は「Domain 配下の新規 .cs に対応する EARS 仕様 (.md) が必須」とするため、`Compat/` 配下のみ例外扱いに変更した(本 PR で `scripts/check-spec-files.sh` を修正、Compat ディレクトリは仕様駆動対象外と明記)
- 将来 `Compat/` に別の polyfill(例: `System.Index` / `System.Range` shim 等)を追加する際も同例外設定が継続適用される

### 各 assembly に追加する場合のコピー手順

`Application/Compat/IsExternalInit.cs` 等として同内容をコピーする。本 ADR のコメントブロックに「採用根拠: docs/adr/0004-init-setter-polyfill.md」と書いてあるためコピー時もそれを保つ。

## Related

- 前提: [ADR-0002 Phase 1 Domain 拡張の集約境界と概念モデル](0002-phase1-domain-boundaries.md) — `record + init` を使う設計判断の出発点
- 前提: [ADR-0001 ADR Operations](0001-adr-operations.md) — 機械的ガードレール / 互換 shim の判断は ADR で残す方針
- 関連: [`docs/specs/domain/players/player-state.md`](../specs/domain/players/player-state.md) — 本 polyfill が要請される最初のユースケース(`init` setter + `with` 式 + null 防御)
- 関連: PR-4 (GameState、後続) — record + with 式が大きく効果を発揮する想定
- 関連: [`Assets/_Project/Scripts/Domain/Compat/IsExternalInit.cs`](../../Assets/_Project/Scripts/Domain/Compat/IsExternalInit.cs) — 実体
