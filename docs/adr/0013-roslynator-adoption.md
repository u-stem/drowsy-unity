# ADR-0013: Roslynator.Analyzers の導入(機械検知レイヤ拡張)

| 項目 | 値 |
| ---- | ---- |
| Status | Accepted |
| Date | 2026-05-13 |
| Decider | プロジェクトオーナー |

## Context

CLAUDE.md §7「Roslyn Analyzer 構成」は Phase 0 から `Roslynator.Analyzers` を導入予定として記述してきたが、実態は以下の 2 つのみで `Roslynator.Analyzers` は未配置のまま継続してきた。

- `Microsoft.CodeAnalysis.NetAnalyzers` 10.0.203(CA-prefix)
- `Microsoft.Unity.Analyzers` 1.26.0(UNT-prefix)

加えて CLAUDE.md §7 の Analyzer 一覧から `Microsoft.Unity.Analyzers` 自体も欠落しており(実態は導入済、`.editorconfig` に UNT-prefix の severity 設定も完備)、ドキュメント記述と実態が双方向で乖離した状態にあった。

過去の ADR でもこの不整合は複数回参照されている。

- [ADR-0007 §Negative](0007-m2-detail-card-effects.md):「Roslynator 整合(継続保留)」
- [ADR-0011 §後追い対応](0011-m3-dream-card-and-game-mechanics-expansion.md):「Roslynator.Analyzers の導入 or CLAUDE.md §7 訂正(Phase 整備)」

[`docs/todo.md`](../todo.md) には「Roslynator.Analyzers の導入 or CLAUDE.md §7 訂正」が選択 A(導入)/ 選択 B(訂正)の二者択一として登録されており、Phase 1 完結後も継続保留されていた。本 ADR は M4 期の chore として JIT で選択 A(導入)を確定し、ドキュメントと実態の双方向の乖離を解消する。

## Decision

**選択 A を採用:`Roslynator.Analyzers` 4.15.0 を NuGetForUnity 経由で導入し、`.editorconfig` に severity 設定を追加する。同時に CLAUDE.md §7 / docs/testing-strategy.md / README.md の Analyzer 一覧を実態整合化する(`Microsoft.Unity.Analyzers` の欠落も訂正)。**

### パッケージ仕様

| 項目 | 値 |
| ---- | ---- |
| パッケージ ID | `Roslynator.Analyzers` |
| バージョン | 4.15.0(2025-12-14 リリース、最新安定版) |
| 提供ルール数 | 200+(主にコードシンプリフィケーション / リファクタリング系) |
| 依存関係 | なし(self-contained) |
| 必要 Roslyn | 3.8.0 以上(Unity 6000.4.6f1 同梱の Roslyn 4.x 系で満たす想定、実機検証で確認) |
| ライセンス | Apache 2.0 |

### 導入方法

1. `Assets/packages.config` に `<package id="Roslynator.Analyzers" version="4.15.0" manuallyInstalled="true" />` を追記
2. Unity Editor で NuGetForUnity の `Restore Packages` を実行(または `Manage NuGet Packages` で個別 Install)
3. NuGetForUnity が `Assets/Packages/Roslynator.Analyzers.4.15.0/` 配下に nupkg を展開、`analyzers/dotnet/cs/*.dll.meta` に `RoslynAnalyzer` ラベルを自動付与
4. Unity AssetDatabase が `RoslynAnalyzer` ラベル付き DLL を検出して csproj の `<Analyzer Include="..." />` 要素を追加(既存 `Microsoft.CodeAnalysis.NetAnalyzers` / `Microsoft.Unity.Analyzers` と同じ経路)
5. `dotnet build drowsy-unity.slnx` で `.editorconfig` の severity 設定を適用

### severity baseline

`.editorconfig` の C# セクションに以下を追加する。

```ini
# ─── Roslynator.Analyzers (RCS-prefix) ───
# ADR-0013 で導入。Roslynator は 200+ ルールを提供するが、既存コードへの影響を制御するため
# baseline はカテゴリ全体を silent にし、後続 PR で個別ルールを段階的に warning/error へ昇格する。
# (段階的有効化は docs/todo.md「Roslynator RCS ルールの段階的有効化」で追跡)
dotnet_analyzer_diagnostic.category-roslynator.severity = silent
```

`silent` を baseline に置く理由:

- 既存コードベース(Domain 9 クラス / Application UseCase 群 / Infrastructure M4 範囲)が一切の RCS 警告を吐かない状態で導入したいため
- 200+ ルールを一度に有効化すると修正コストが過大で、本 PR スコープ(機械検知レイヤ拡張の chore)を超える
- 個別ルールの段階的 warning 化は本 ADR スコープ外、`docs/todo.md`「Roslynator RCS ルールの段階的有効化」で追跡

### 同時訂正範囲(ドキュメント整合)

| ファイル | 修正内容 |
| ---- | ---- |
| `CLAUDE.md` §7 | Analyzer 一覧に `Microsoft.Unity.Analyzers` を追加(従来欠落)、`Roslynator.Analyzers` を「導入予定」から「導入済」に書き換え、baseline silent + 段階的 warning 化方針を明記 |
| `CLAUDE.md` §11 | 確立済 ADR 一覧に本 ADR-0013 を追加 |
| `docs/testing-strategy.md` §7「機械検知」 | Roslyn Analyzer 列に `Microsoft.Unity.Analyzers` を追加(欠落訂正)|
| `README.md` 使用ライブラリ表 | `Roslynator.Analyzers` 4.15.0 行を追加 |
| `README.md` セットアップ手順 5 | NuGetForUnity Restore に Roslynator 4.15.0 確認を追加 |
| `Assets/packages.config` | `Roslynator.Analyzers` 4.15.0 行を追加 |
| `.editorconfig` | Roslynator severity 制御コメント追記 + `category-roslynator.severity = silent` 追加 |
| `docs/todo.md` | 「Roslynator.Analyzers の導入 or CLAUDE.md §7 訂正」を完了済みへ移動、「Roslynator RCS ルールの段階的有効化」を新規未着手 TODO として起票 |

## Consequences

### Positive

- 機械検知レイヤの拡張:Roslynator の 200+ ルール(主にコードシンプリフィケーション / リファクタリング系)が利用可能になり、Phase 2 以降のコード品質向上の余地が拡張される
- 段階的有効化(baseline silent → 個別ルール warning 化)で既存コードへの影響を制御できる
- CLAUDE.md §7 と実態の長期乖離(ADR-0007 / ADR-0011 で複数回先送り)が解消され、新規参加者(将来の自分含む)の混乱が消える
- `Microsoft.Unity.Analyzers` の §7 欠落も同時に解消され、Phase 0 で確立した Analyzer 構成が文書面でも完全に表明される
- ADR-0003 で確立した TODO 運用の累積負債(Roslynator 不整合)を 1 件解消

### Negative

- `.editorconfig` 管理コストの増加:Roslynator 4.15.0 の 200+ ルールを段階的に評価して個別 severity を設定する継続作業が `docs/todo.md` で発生する
- リポジトリサイズの増加:`Assets/Packages/Roslynator.Analyzers.4.15.0/` 配下が約 4.2MB(DLL + meta、既存 `Microsoft.CodeAnalysis.NetAnalyzers` / `Microsoft.Unity.Analyzers` と同等オーダー、Unity プロジェクトの肥大化への寄与は限定的)
- 既存 2 Analyzer と同様に DLL + `.meta` を git 管理下に置くため `git clone` 直後から `dotnet build` で Analyzer が動作する設計だが、後続で NuGetForUnity の `Restore Packages` をうっかり走らせると Roslynator パッケージのバージョンが意図せず更新される可能性がある(`Assets/packages.config` で 4.15.0 を固定しているため通常は発生しないが、 NuGetForUnity の UI 操作経由で latest に上げると差分が出る)

### Neutral

- baseline `silent` 採用により、本 ADR 単体では既存コードへの可視な変化は発生しない(警告 0 件維持)
- 「導入したが活用していない」状態を一時的に許容するが、後続 TODO で段階的に活用していく前提

## Alternatives Considered

| 案 | 不採用理由 |
| ---- | ---- |
| **選択 B: CLAUDE.md §7 から `Roslynator.Analyzers` 記述を削除して実態と揃える** | 機械検知レイヤの拡張機会を放棄することになる。Roslynator は依存なし(self-contained)で導入コストが小さく、baseline silent で開始すれば既存コードへの影響もない。CLAUDE.md §7 の「Roslyn Analyzer 構成」の充実度として、Microsoft 系 Analyzer + コミュニティ Analyzer の併用は Unity プロジェクトで一般的な構成であり、削減方向は積極的選択にならない |
| baseline `default`(各ルールのデフォルト severity を採用)で一括有効化 | Roslynator のデフォルト severity は info / hidden 中心とはいえ、200+ ルールの初回全有効化で「警告は出ないが提案表示が大量発生」する可能性があり、IDE 体験が悪化する。段階的有効化の方が制御可能 |
| baseline `warning` / `error` で一括有効化 | 既存コードベースが大量の警告 / エラーで fail し、本 PR スコープ(機械検知レイヤ拡張の chore)が膨大な修正 PR に化ける |
| 個別 RCS ルールを本 PR 内で 5〜10 件選んで warning 化 | 「どのルールを選ぶか」の議論で本 PR が長引く。1 PR = 1 論理的変更の原則に従い、導入と個別有効化を分離する方が筋 |
| Roslynator の旧バージョン(4.x 系の古いリリース)を採用 | 4.15.0(2025-12-14)は最新安定版、依存なし、Roslyn 3.8.0+ という要件は Unity 6 で満たすため、最新を選ぶデメリットが見当たらない |

## Implementation Notes

### 段階的有効化のプロセス

本 ADR 完成 PR 後の運用:

1. `docs/todo.md`「Roslynator RCS ルールの段階的有効化」エントリで個別ルールの warning 化を追跡
2. 1 PR あたり 3〜5 ルール程度を warning 化、影響箇所を修正
3. 影響大なルール(例: 多数の修正が必要なシンプリフィケーション系)は別途検討、必要なら個別の判断記録を Notes に残す
4. Phase 整備段階で「Roslynator ルールセットの確定版」を ADR / `.editorconfig` で表明(段階的有効化が一段落した時点で本 ADR を `Superseded by` で更新するか、別 ADR で確定版を表明)

### 検証手順(本 ADR 完成 PR マージ後)

ローカル(プロジェクトオーナー):

1. main を pull、`bun lefthook install`(unchanged)
2. Unity Editor を起動 → 起動時の auto-package-restore で `Assets/Packages/Roslynator.Analyzers.4.15.0/` が展開されることを確認
3. `dotnet build drowsy-unity.slnx` で警告 0 / エラー 0 を確認(baseline silent のため新規警告は出ないはず)
4. IDE(VS Code 等)で `.cs` ファイルを開き、RCS-prefix の info / hint が表示されることを確認(severity silent のため警告 / エラーは表示されない、ただし IDE の「すべての診断」表示で可視化される)

CI(Phase 6 で GameCI 整備時):

- `Assets/Packages/Roslynator.Analyzers.4.15.0/` の存在を前提に `dotnet build` 実行
- baseline silent のため lefthook pre-commit の `dotnet build` も警告 0 維持

### `IsExternalInit` polyfill との関係

ADR-0004 で導入した `Compat/IsExternalInit.cs` polyfill は Roslynator が追加で何かを要求するものではない(Roslynator は Roslyn API のみに依存)。本 ADR との衝突は発生しない。

### NuGetForUnity の `manuallyInstalled="true"` フラグ

既存 2 パッケージと同じく `manuallyInstalled="true"` を付与する。これは NuGetForUnity が「依存関係の自動展開対象外」とするフラグであり、本パッケージが依存なし(self-contained)であることと整合する。

### Roslyn バージョン別フォルダ構造と `RoslynAnalyzer` ラベル

Roslynator.Analyzers 4.15.0 の nupkg は `analyzers/dotnet/roslyn3.8/cs/` と `analyzers/dotnet/roslyn4.7/cs/` の 2 つの Roslyn バージョン別フォルダを持つマルチバージョン構造。NuGetForUnity 4.5.0 は `analyzers/dotnet/**/*.dll` 全てに `RoslynAnalyzer` ラベルを自動付与する仕様のため、本 PR コミット時点で roslyn3.8 + roslyn4.7 両方の DLL がラベル付与済(計 16 ファイル)。

通常の MSBuild / NuGet 環境では `$(RoslynVersion)` プロパティで自動選択されるが、Unity の csproj 生成ではこの分岐が効かない可能性があり、両 Roslyn バージョンの DLL が `<Analyzer Include="..." />` で同時参照されると衝突 / Loader Error の懸念がある。本 ADR では以下の方針を採る:

- 本 PR コミット時点では NuGetForUnity の自動ラベル付与状態(両バージョン有効)を維持
- プロジェクトオーナーが Unity Editor + `dotnet build drowsy-unity.slnx` で動作確認(Loader Error / 重複診断の有無を検証)
- 衝突が観測された場合、`analyzers/dotnet/roslyn3.8/cs/*.dll.meta` から `RoslynAnalyzer` ラベルを除去する追加 PR を起票(Unity 6000.4.6f1 が Roslyn 4.x 系を使う前提、roslyn4.7 系のみ有効化)
- 衝突が観測されない場合は両バージョン有効化を維持(Roslyn 内部で version-pick されている、または無害)

検証結果は本 ADR の `Implementation Notes` に追記する(`Status` を `Accepted` のまま維持、技術詳細の補足のみ)。

### 検証結果(2026-05-13、本 PR 同コミット内)

プロジェクトオーナー操作で `Edit > Preferences > External Tools > Regenerate project files` を実行 → 全 6 csproj が 16:06:22 に再生成 → `dotnet build drowsy-unity.slnx --nologo --verbosity quiet` を実行した結果:

| 観測項目 | 結果 |
| ---- | ---- |
| csproj への Analyzer 参照追加 | 全 6 csproj(Domain / Application / Infrastructure + 各 Tests)に `<Analyzer Include="...Roslynator...">` が **16 件ずつ追加**(roslyn3.8 系 8 DLL + roslyn4.7 系 8 DLL 両方) |
| `dotnet build` 結果 | **0 警告 / 0 エラー / 1.32 秒**(2 回目以降の incremental build) |
| Loader Error | **観測されず**(両 Roslyn バージョン DLL を同時参照しても衝突なし) |
| 重複診断 | **観測されず**(baseline `category-roslynator.severity = silent` が両バージョンに一括適用、Roslyn 内部で version-pick または無害な重複ロード) |
| 既存 Analyzer 動作への影響 | NetAnalyzers / Unity.Analyzers / Unity.SourceGenerators の動作は不変、`dotnet build` も従来通り 1〜5 秒で完了 |

**結論**: roslyn3.8 + roslyn4.7 両バージョン有効化を維持する。`roslyn3.8` 系から `RoslynAnalyzer` ラベルを除去する追加 PR は **不要**。後続の段階的有効化 PR(`docs/todo.md`「Roslynator RCS ルールの段階的有効化」)で個別 RCS ルールを `warning` 化する際にも、現状の baseline 構成のまま進められる。

## Related

- 前提: [CLAUDE.md §7 機械検知方針](../../CLAUDE.md)(本 ADR で §7 を訂正)
- 前提: [ADR-0001 ADR Operations](0001-adr-operations.md)
- 前提: [ADR-0003 TODO 運用](0003-todo-operations.md)
- 前提: [`docs/todo.md`](../todo.md)「Roslynator.Analyzers の導入 or CLAUDE.md §7 訂正」(本 ADR で完了処理)
- 関連: [ADR-0007 §Negative](0007-m2-detail-card-effects.md)「Roslynator 整合(継続保留)」(本 ADR で解消)
- 関連: [ADR-0011 §後追い対応](0011-m3-dream-card-and-game-mechanics-expansion.md)「Roslynator.Analyzers の導入 or CLAUDE.md §7 訂正(Phase 整備)」(本 ADR で解消)
- 関連: [`Assets/packages.config`](../../Assets/packages.config)(本 ADR で更新)
- 関連: [`.editorconfig`](../../.editorconfig)(本 ADR で `category-roslynator.severity = silent` 追加)
- 関連: [`README.md`](../../README.md)(本 ADR で使用ライブラリ / セットアップ手順を更新)
- 関連: [`docs/testing-strategy.md`](../testing-strategy.md)(本 ADR で §7 訂正)
- 後続: `docs/todo.md`「Roslynator RCS ルールの段階的有効化(baseline silent → 個別 warning 化)」(本 ADR で起票、Phase 整備の継続作業として追跡)
