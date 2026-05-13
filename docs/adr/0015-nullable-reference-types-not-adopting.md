# ADR-0015: Nullable Reference Types (NRT) を採らない

| 項目 | 値 |
| ---- | ---- |
| Status | Accepted |
| Date | 2026-05-13 |
| Decider | プロジェクトオーナー |

## Context

C# 8 で導入された Nullable Reference Types (NRT、`#nullable enable` / `<Nullable>enable</Nullable>` で有効化される静的解析機能)は、参照型に対する null 許容性を型レベルで宣言できる仕組み。Phase 1 開始時点(PR #12 CardData レビュー)では NRT 有効化を試行したが、Unity 6 のデフォルト構成では NRT が無効で `CardData?` / `object?` のアノテーション 7 箇所に対し `CS8632`(`The annotation for nullable reference types should only be used in code within a '#nullable' annotations context`)警告が発生し、既存パターン(NRT 無効)に揃えて `?` を削除した経緯がある。

[`docs/todo.md`](../todo.md) には「NRT (Nullable Reference Types) 有効化を検討する」(`priority: low`)が登録され、Done when として以下が明記されていた:

- NRT 有効化のコスト・利益を評価
- 採用する場合:`csc.rsp` または `.editorconfig` で NRT 有効化、Domain 全体に nullable annotation を導入、既存 NUnit テスト全 Green を維持、Domain C0 95%+ を維持、ADR で判断記録
- 不採用の場合:ADR で「NRT を採らない理由」を記録(同じ問題を繰り返さないため)
- Notes:「必要性が高まった時点で再検討。Phase 2 以降で外部 API クライアント等の null 多発コードが入る前に判断したい」

M4 完了時点(M1〜M4 で約 87 のプロダクションコード `.cs` ファイル + 約 60 のテストファイルが確立済)で再評価する JIT 判断を実施する。

### 現状の null 戦略

本プロジェクトでは NRT 無効下でも以下の実用的な null 安全性パターンが確立されている:

| パターン | 適用箇所 | 採用 ADR |
| ---- | ---- | ---- |
| `?? throw new ArgumentNullException(nameof(arg))` | constructor / public method 引数の null 防御 | ADR-0002(Domain)、 ADR-0006(Application) |
| `record + init + value ?? throw` | 値オブジェクトの `with { Field = null }` 防御 | ADR-0004(`IsExternalInit` polyfill) |
| 明示的 `if (x is null) { throw ... }` | constructor 内で複数引数を順次検証 | ADR-0002 / ADR-0006 |
| `[Test, Category("Abnormal")]` での null 防御テスト | 各防御パスの NUnit テスト網羅 | ADR-0002(Domain C0 95%+ / MC/DC 相当)|

実装規模:`ArgumentNullException` を参照するソースは合計 **57 ファイル**(プロダクション 30 ファイル / 全 87 の 34%、テスト 27 ファイル)。「参照」は実際の `throw` 文と docstring の `<exception cref>` 言及の両方を含む(例:`IUserSettings.cs` は docstring のみで `throw` していないが、 interface 契約として null 防御を表明している)。プロダクションコード側の null 防御は網羅的に書かれている。

### NRT 有効化の影響範囲

仮に NRT を有効化する場合の影響:

- 全 87 プロダクションファイル + 約 60 テストファイルに `?` / 非 nullable annotation を付与
- 既存 `?? throw` パターンは温存できるが、 nullable 表記の選択(`string?` か `string` か)を全フィールド / 引数で判断する作業
- public API シグネチャ変更による外部呼び出し側の修正(本プロジェクト内のみで完結するが、 PR 規模は巨大)
- Unity 6 × NRT の Editor 統合検証(Code Coverage / AssetDatabase / Roslyn Analyzer + Roslynator との衝突有無)
- M5 で Bootstrap / Presentation 統合する際に既存 NRT annotation の整合性確認が必要

## Decision

**Nullable Reference Types を採らない**。現状の null 戦略(`ArgumentNullException` + `record + init + value ?? throw` + 明示的 null チェック + Abnormal カテゴリのテスト網羅)を継続維持する。

### 不採用の判断ロジック

| 軸 | 評価 |
| ---- | ---- |
| 既存 null 戦略の実用性 | **十分**(プロダクション 87 中 30 ファイル(34%)で `ArgumentNullException` を参照、Domain `record + init` で `with { Field = null }` 防御済、Abnormal テストで網羅)|
| NRT 有効化の追加価値 | **限定的**(コンパイル時 null 検証は追加されるが、既存パターンで実行時防御は完備、機械検知レイヤとしての差は marginal)|
| 修正コスト | **過大**(87 プロダクション + 60 テストファイル、 巨大 PR or 多数の段階 PR が必要、 Phase 整備として規模過大)|
| Unity 6 互換性 | **未検証**(NRT 自体は C# 8 機能で Unity 6 / Mono 対応だが、 Editor 統合 / AssetDatabase / Code Coverage / Roslynator との衝突確認が別途必要)|
| 再評価条件 | **明確**(M5 Bootstrap / Presentation 統合で外部 API クライアント / Web 通信 / シリアライザ等の null 多発コードが入る時点が再評価点として筋、ADR-0005 Phase 2 Roadmap で M5 着手予定)|

### 再評価条件

以下のいずれかが発生した時点で本 ADR を再評価し、本 ADR の Status を `Superseded by NNNN` に更新する別 ADR で覆す:

1. **M5 Bootstrap / Presentation 統合で null 多発コードが入る**:外部 API クライアント(HTTP / WebSocket)、シリアライザ(Newtonsoft.Json 既導入、ADR-0012)、Unity の `GameObject.GetComponent<T>` のような null 返り値 API 等で、 NRT 静的検証の価値が顕著になる場面
2. **他ゲームの Application 追加**:ADR-0007 §3 で言及した「将来 `Drowsy.Application.Games.OtherGame` 追加」時に、 NRT で型レベルの null 契約を表明する方が筋になる場面
3. **Unity / Roslyn 側の進化**:Unity Editor が NRT を完全統合し、 Code Coverage / AssetDatabase / Analyzer が NRT 前提で動作するようになった場合
4. **既存 null 戦略の限界が観測される**:`ArgumentNullException` の網羅漏れで実際に null 起因のバグが発生した場合

これらの条件が発生していない現時点では、 NRT 採用のコストが利益を上回ると判断する。

## Consequences

### Positive

- **既存 87 プロダクションファイル + 60 テストファイルへの annotation 付与作業を回避**:M4 完了時の Phase 整備として、 巨大リファクタを避けてフェーズ進捗(M5 着手)に集中できる
- **既存 null 戦略の継続適用**:ADR-0002 / ADR-0004 / ADR-0006 で確立した防御パターンを覆さず、 後続 PR でも同じパターンを継続適用可能(認知負荷を増やさない)
- **Unity 6 × NRT 互換性検証作業の回避**:Editor 統合 / Code Coverage / Asset Database / Roslynator との衝突確認といった副次作業を発生させない
- **判断記録による将来同問題の回避**:同じ「NRT 有効化を検討する」議論が後続の Phase で再発した時に、 本 ADR を参照して経緯と判断軸を即時共有可能(ADR-0003 の TODO 完了処理意図と整合)

### Negative

- **コンパイル時 null 検証の機会損失**:NRT による静的検証(IDE 内で `x.Foo()` が null 警告される機能)が得られない。実行時には `ArgumentNullException` で防御されるが、 IDE 編集中の即時フィードバックは弱い
- **将来の外部 API クライアント追加時のリスク**:M5 以降で null 多発コードが入った場合、 NRT 無効環境では null 起因バグの混入リスクが相対的に高くなる(再評価条件 1 で対処予定)
- **他ゲームへの転用余地と NRT の関係**:ADR-0007 §3 で言及した「将来 `OtherGame` 追加」時に、 NRT で型契約を表明する方が筋という設計圧が後発的に発生する可能性(再評価条件 2 で対処予定)

### Neutral

- **本 ADR は将来覆されうる**:Status `Accepted` で記録するが、 再評価条件発生時に Superseded by NNNN で別 ADR が覆す。 ADR は「現時点での判断」を記録するもので、 永続的な禁則ではない
- **影響範囲**:`.editorconfig` / `csc.rsp` / 全 `.cs` ファイルに変更なし(本 ADR は判断記録のみ)、 M5 着手時に再評価する旨を `docs/todo.md` のセクションで明示はしない(本 ADR が再評価条件として機能するため)

## Alternatives Considered

| 案 | 不採用理由 |
| ---- | ---- |
| **A. 全 assembly 一括 NRT 有効化** | 87 プロダクション + 60 テストファイルへの annotation 付与は単一 PR では巨大で、レビュー粒度が低下する。複数 PR への分割も整合性確保のため設計コストが高い。 Phase 整備として現時点(M4 完了)の優先度に合わない |
| **B. Domain assembly のみ先行で有効化、段階的に拡張** | Domain 9 ファイルへの annotation は実現可能だが、 Application が NRT 無効のままだと境界(`ICardCatalog<IEffect>` 等の Application 経由 API)で nullable contract が断絶する。半端な状態が続くと逆に認知負荷を増やす。完全有効化(案 A)か完全不採用(本決定)の二択が筋 |
| **C(本決定): 不採用 + ADR 起票** | 既存 null 戦略で実用的な null 安全性は確立済、修正コストが過大、 Unity 6 互換性が未検証、再評価条件が明確 — の 4 軸で総合判断 |
| **D. ADR 起票せず TODO のみで管理継続** | 同じ議論が再発した時に、 過去の判断軸が `docs/todo.md` のエントリのみだと粒度が荒く、 ADR レベルの記録(Context / Decision / Consequences / Alternatives)が不足する。 ADR-0003 の TODO 完了処理意図とも整合しない |

## Implementation Notes

### 本 ADR の影響範囲

本 ADR は判断記録のみで、リポジトリ内のコード / 設定ファイルへの変更は無い。以下のみ更新:

- `docs/adr/0015-nullable-reference-types-not-adopting.md`(本 ADR、新規)
- `docs/adr/README.md`(インデックスに追加)
- `CLAUDE.md` §11(ADR 一覧に追加)
- `docs/todo.md`(TODO「NRT 有効化を検討する」を「未着手 → 完了済み」へ移動、 ADR-0003 運用通り削除せず Related に ADR-0015 を追記)

### `.editorconfig` / `csc.rsp` の現状維持

本 ADR では `.editorconfig` への nullable diagnostic 設定(`dotnet_diagnostic.CS8602.severity` 等)は **変更しない**。既存の `.editorconfig` 設定はすでに以下を含む:

- `dotnet_diagnostic.CS8600.severity = error`(null リテラルを非 nullable 型に代入)
- `dotnet_diagnostic.CS8602.severity = error`(null 参照の可能性がある参照の逆参照)
- `dotnet_diagnostic.CS8603.severity = error`(null 参照の可能性がある戻り値)
- `dotnet_diagnostic.CS8618.severity = error`(コンストラクターで非 nullable フィールド未初期化)

これらは NRT が有効化された場合に効くが、 NRT 無効下では発火しないため害は無い。 NRT 不採用の現状でも将来切り替え時の severity 設定を残しておく形で整合する。

### 再評価のトリガと作業範囲

再評価条件(本 ADR §Decision「再評価条件」参照)が発生した時点で、 別 ADR を起票し本 ADR を `Superseded by NNNN` に更新する。 別 ADR では以下を扱う:

- 採用範囲(全 assembly / Domain のみ / 個別 module / 個別 file 単位)
- 切り替え方法(`csc.rsp` / `.editorconfig` / `#nullable` ディレクティブ / asmdef per-project 設定)
- 既存 87 ファイルへの annotation 付与計画(段階的 PR 分割か単一 PR か)
- Unity 6 互換性検証手順(Editor / Code Coverage / Asset Database)

## Related

- 起点: PR #12 (CardData) — NRT 有効化試行で CS8632 警告発生、 `?` を削除した経緯
- 起点 TODO: [`docs/todo.md`](../todo.md)「NRT (Nullable Reference Types) 有効化を検討する」(本 ADR で完了処理)
- 関連 ADR: [ADR-0002 Phase 1 Domain 拡張の集約境界と概念モデル](0002-phase1-domain-boundaries.md) — `ArgumentNullException` + 値同値性方針
- 関連 ADR: [ADR-0004 IsExternalInit polyfill](0004-init-setter-polyfill.md) — `record + init + value ?? throw` パターン
- 関連 ADR: [ADR-0006 §3 UseCase 構成](0006-m1-detail-application-interfaces.md) — Application 層の null 防御
- 関連 ADR: [ADR-0007 §3 「`StartGameUseCase` の型引数結合」](0007-m2-detail-card-effects.md) — 他ゲーム転用時の NRT 圧について言及
- 関連 ADR: [ADR-0013 Roslynator.Analyzers の導入](0013-roslynator-adoption.md) — 機械検知レイヤの拡張、 本 ADR と一緒に Phase 整備フェーズの判断を構成
- 関連: [`CLAUDE.md`](../../CLAUDE.md) §7 機械検知方針(本 ADR で NRT を採らない判断は §7 の方針と整合)
- 後続: M5 Bootstrap / Presentation 統合時に外部 API クライアント / シリアライザ等の null 多発コードが入った場合に再評価
