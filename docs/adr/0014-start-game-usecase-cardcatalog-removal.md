# ADR-0014: `StartGameUseCase` から `ICardCatalog<IEffect>` 依存を削除する

| 項目 | 値 |
| ---- | ---- |
| Status | Accepted |
| Date | 2026-05-13 |
| Decider | プロジェクトオーナー |

## Context

[ADR-0006 §3](0006-m1-detail-application-interfaces.md) で `StartGameUseCase` の constructor injection を `(IRandomSource, ICardCatalog, IGameConfig)` と確定し、M1-PR3 で実装した。しかし M1 範囲では `ICardCatalog` は内部で一切参照されておらず、コードコメントにも「本 PR (M1-PR3) では参照しない」と明記されていた。

[ADR-0007 §3](0007-m2-detail-card-effects.md) で `ICardCatalog` → `ICardCatalog<TEffect>` にジェネリック化(M2-PR1)した時点で、`StartGameUseCase` が `IEffect` を内部利用しないにもかかわらず constructor シグネチャに `ICardCatalog<IEffect>` 型を持つ「設計上の割り切り」が発生した。ADR-0007 §3 では以下 3 つの選択肢を比較:

| 案 | 評価 |
| ---- | ---- |
| X(`StartGameUseCase` から `ICardCatalog` 引数削除) | ADR-0006 §3 を覆す変更で ADR-0007 スコープ外、**将来 SO 化(M4)時に再評価する旨を `docs/todo.md` に登録**(本 ADR がその再評価) |
| Y(非ジェネリック `ICardCatalog` を `ICardCatalog<TEffect>` の基底に分離) | 型階層が複雑化、現状 1 ゲーム実装のみの規模では過剰 |
| 採用案(型引数結合を割り切り受容) | 型引数 `IEffect` が constructor + フィールド型のみで完結、内部実装には染み出さない |

ADR-0007 §3 採用案は M4 完了までの暫定判断として明記されていた。M4-PR1〜PR6 完了済(`Roslynator.Analyzers` 4.15.0 導入 ADR-0013 完成済)の時点でコード実装を再確認した結果:

- `StartGameUseCase._catalog` は constructor で代入されるのみで、`Execute()` 本文では一切参照されない(`_catalog.*` 呼び出し 0 件、`StartGameUseCase.cs:44` の readonly field 宣言と constructor 代入のみで dead)
- ADR-0007 §3 が予告した「`StartGameUseCase` がカード情報を本当に必要としないことが確定」という再評価条件に到達

[`docs/todo.md`](../todo.md) TODO「`StartGameUseCase` から未使用の `ICardCatalog` 依存削除を検討する」(priority: low、ADR-0007 起票時に登録)で選択 A(削除)/ 選択 B(現状維持)の JIT 判断が継続保留されていた。本 ADR は M4 完了時の JIT で **選択 A(削除)** を確定する。

## Decision

**`StartGameUseCase` から `ICardCatalog<IEffect>` 依存を削除する**。constructor シグネチャを `(IRandomSource rng, IGameConfig config)` の 2 引数に変更し、`_catalog` フィールドとその null チェックを除去する。

### 変更詳細

| 項目 | 変更前 | 変更後 |
| ---- | ---- | ---- |
| constructor 引数 | `(IRandomSource rng, ICardCatalog<IEffect> catalog, IGameConfig config)` | `(IRandomSource rng, IGameConfig config)` |
| フィールド | `_rng` / `_catalog` / `_config` | `_rng` / `_config` |
| null チェック | rng / catalog / config の 3 件 | rng / config の 2 件 |
| using ディレクティブ | `Drowsy.Application.Games.DrowZzz.Effects`(`IEffect` 用)を含む | `IEffect` 不要のため除去 |

### 呼び出し側修正

- `Tests/Application.Tests/Games/DrowZzz/StartGameUseCaseTests.cs`:`NewUseCase` ヘルパーから `catalog` 引数 + `InMemoryCardCatalog` 生成を除去
- `Tests/Application.Tests/Integration/M1IntegrationTests.cs`:`NewUseCases` ヘルパー内の `new StartGameUseCase(rng, catalog, config)` を `new StartGameUseCase(rng, config)` に変更(`catalog` 変数自体は `DrowZzzRule` で引き続き必要なので残す)

### ADR-0006 §3 との関係

ADR-0006 §3「UseCase 構成(ハイブリッド)」のうち、以下は **本 ADR で覆さない**(維持):

- `StartGameUseCase` を「セッション未生成からの開始」用の特殊 UseCase として分離する構成
- `Drowsy.Application.Games.DrowZzz` namespace 配置
- `ApplyActionUseCase` の `IsLegalMove` 違反時の `InvalidOperationException` 方針
- ハイブリッド UseCase 構成全般

**本 ADR で更新**するのは ADR-0006 §3 に含まれる constructor 引数 `(IRandomSource, ICardCatalog, IGameConfig)` の 3 番目の `ICardCatalog` 引数のみ。ADR-0006 §3 全体の `Status` は `Accepted` のまま維持し、本 ADR で部分的に更新する。

### ADR-0007 §3 との関係

ADR-0007 §3「`StartGameUseCase` の型引数結合(設計上の割り切り)」は、本 ADR で **解消**(該当する dead 依存が削除されるため)。ADR-0007 §3 が予告した「将来 SO 化(M4)時に再評価」のとおり、M4 完了後の本 ADR で型引数結合を削除した。ADR-0007 §3 が記録する暫定判断の経緯は履歴として有用なので、`Status` は `Accepted` のまま維持し、本 ADR を Related に追記する形で参照経路を残す。

## Consequences

### Positive

- **Dead 依存の解消**:`_catalog` の constructor 注入 + フィールド宣言が `Execute()` 本文と整合し、コード理解の負荷が下がる
- **機械検知レイヤとの整合性向上**:Roslyn の `IDE0052`(unread private members)や Roslynator の類似ルールを `warning` 化した際に false positive を起こさない
- **呼び出し側の簡略化**:`StartGameUseCaseTests` / `M1IntegrationTests` の test fixture 構築コードから `ICardCatalog` 生成を除去でき、テスト fixture の意図(`StartGameUseCase` のテスト)とコード(`InMemoryCardCatalog` の構築)の乖離が解消
- **型引数結合の削除**:ADR-0007 §3 の「設計上の割り切り」が記述していた `IEffect` 型引数の不必要な伝播が完全に消滅
- **ADR-0006 §3 / ADR-0007 §3 の継続的整理**:ADR-0007 §3 が明示予告した「M4 完了時の再評価」を本 ADR で達成、暫定判断 → 確定判断への移行を文書化

### Negative

- **ADR-0006 §3 を部分的に覆す breaking change**:`StartGameUseCase` の constructor シグネチャが変わるため、外部から呼び出すコード(本プロジェクト内では `StartGameUseCaseTests` / `M1IntegrationTests` の 2 箇所)を全て更新する必要があった(本 ADR の同時 PR で完了済)
- **将来 `StartGameUseCase` がカード情報を必要とする変更が発生した場合のリスク**:M5 以降の Bootstrap / Presentation 統合で、もし `StartGameUseCase` 内でカードカタログを参照する必要が出た場合、本 ADR の判断を再度覆して constructor injection を復活させる必要がある(その時点で別 ADR で記録)。現状の M1〜M4 範囲では発生しないと判断

### Neutral

- 本 ADR の影響範囲は Application 層 + Tests のみ。Domain 層 / Infrastructure 層 / Presentation 層への影響なし
- VContainer DI 設定(M5 で導入予定)への影響なし(Pure C# constructor injection の引数を 1 つ削るだけで、 ADR-0005 「M1〜M4 は VContainer 不使用」の方針とは独立)

## Alternatives Considered

| 案 | 不採用理由 |
| ---- | ---- |
| **選択 B**(現状維持 + TODO 完了済み移動)| ADR-0007 §3 の「設計上の割り切り」は M4 完了までの暫定判断と明記されており、M4 完了後も維持する積極的理由がない。dead 依存を残すと Roslyn / Roslynator の機械検知レイヤ(IDE0052 / RCS1213 等の "unread / unused" 系)を warning 化する際に常に false positive となり、検知レイヤの実効性を阻害する |
| `_catalog` を残しつつ `[UsedImplicitly]` 属性で警告抑制 | ライブラリ依存(`JetBrains.Annotations`)が必要、本プロジェクトでは現状未導入。dead 依存を解消する方が筋 |
| 非ジェネリック `ICardCatalog` 基底分離(ADR-0007 §3 案 Y)| ADR-0007 §3 で「現状 1 ゲーム実装のみの規模では過剰」と却下済、本 ADR でも採らない |

## Implementation Notes

### ADR-0006 §3 / ADR-0007 §3 の Status 維持判断

ADR-0001 で確立した Status 語彙(`Proposed` / `Accepted` / `Rejected` / `Withdrawn` / `Deprecated` / `Superseded by NNNN`)のうち、ADR-0006 / ADR-0007 を `Superseded by 0014` にする選択肢もあった。しかし両 ADR は M1 / M2 範囲の判断全体を記録するもので、本 ADR で覆すのは ADR-0006 §3 の constructor 引数 1 つ + ADR-0007 §3 の「設計上の割り切り」セクションのみ。ADR 全体を `Superseded by` にすると「ADR-0006 / ADR-0007 全体が無効化された」と読み手が誤解するため、両 ADR の `Status` は `Accepted` のまま維持し、本 ADR の Related で部分更新の経路を示す。

### 呼び出し側の修正範囲

本プロジェクト内の `new StartGameUseCase(...)` 呼び出しは 2 箇所のみ(`StartGameUseCaseTests` / `M1IntegrationTests`)で、いずれもテストファイル。プロダクションコードの呼び出しは現状無い(M5 で Bootstrap が導入された時に最初の呼び出しが発生する想定、その時点で本 ADR の 2 引数 constructor を直接利用する)。

### 検証

`dotnet build drowsy-unity.slnx --nologo --verbosity quiet`:0 警告 / 0 エラー / 5.34 秒(本 ADR 同時 PR で確認済、Roslynator 4.15.0 RCS-prefix 4 ルール warning 化下)。Unity Test Runner 実機緑確認はプロジェクトオーナー側。

## Related

- 前提: [ADR-0006 §3 UseCase 構成(ハイブリッド)](0006-m1-detail-application-interfaces.md) — 本 ADR で constructor 引数 `ICardCatalog` を削除、§3 のそれ以外の判断(ハイブリッド構成 / namespace / IsLegalMove 違反方針)は維持
- 前提: [ADR-0007 §3 `PlayCardAction.Apply` の責務拡張 / 「`StartGameUseCase` の型引数結合(設計上の割り切り)」](0007-m2-detail-card-effects.md) — 本 ADR で「設計上の割り切り」を解消、§3 全体の Status は維持
- 起点: [`docs/todo.md`](../todo.md)「`StartGameUseCase` から未使用の `ICardCatalog` 依存削除を検討する」(本 ADR で完了処理)
- 関連: [`Assets/_Project/Scripts/Application/Games/DrowZzz/StartGameUseCase.cs`](../../Assets/_Project/Scripts/Application/Games/DrowZzz/StartGameUseCase.cs) — 実装本体
- 関連: [ADR-0005 Phase 2 Roadmap](0005-phase2-roadmap-drowzzz.md) — M5 Bootstrap で DI 統合時に本 ADR の 2 引数 constructor を直接利用
- 関連: [ADR-0024](./0024-associate-to-first-player-on-game-start.md) — 本 ADR を **部分的に覆す**(ゲーム開始時自動連想 marker `AssociateToFirstPlayerOnGameStartEffect` 導入により `ICardCatalog<IEffect>` 依存を再追加、2026-05-18)。本 ADR §「将来 `StartGameUseCase` がカード情報を必要とする変更が発生した場合のリスク」で予告した再評価条件に該当。本 ADR の Status は **Accepted のまま維持**(精神=dead 依存の排除 は維持される、ADR-0024 で再追加された catalog は live 依存になるため)
