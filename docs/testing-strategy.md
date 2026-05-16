# テスト戦略

drowsy-unity のテスト方針詳細。CLAUDE.md「6. テスト方針」「7. 機械検知方針」の補足ドキュメント。

---

## 1. 仕様駆動開発(SBE: Specification by Example)

### 1.1 EARS

機能要件は `docs/specs/<layer>/<module>/<feature>.md` に EARS 構文で記述する。

#### EARS の 5 パターン

| パターン | 構文 | 用途 |
| ---- | ---- | ---- |
| Ubiquitous (普遍) | `<system> shall <response>` | 常に成立する性質 |
| Event-driven (事象駆動) | `When <trigger>, <system> shall <response>` | 操作に対する応答 |
| State-driven (状態駆動) | `While <state>, <system> shall <response>` | 特定状態下での挙動 |
| Unwanted (異常) | `If <unwanted>, then <system> shall <response>` | 例外的入力への対応 |
| Optional (任意) | `Where <feature>, <system> shall <response>` | 設定により変わる挙動 |

実例は `docs/specs/.template.md` を参照。

### 1.2 Gherkin

受け入れシナリオを `docs/specs/.../<feature>.feature` に Gherkin 構文で記述する。

- `# language: ja` で日本語キーワード使用可(機能 / シナリオ / 前提 / もし / ならば)
- SpecFlow 等の自動実行ツールは Unity Test Runner と統合困難なため使用しない
- `.feature` ファイルは「人間可読な仕様書」として運用し、対応する NUnit テストを別途実装する

実例は `docs/specs/.template.feature` を参照。

## 2. テスト分類

### 2.1 Size 分類(Google 流)

| 分類 | 定義 | 想定対象 |
| ---- | ---- | ---- |
| **Small** | 単一プロセス、in-memory のみ、< 1 秒 | Domain 純粋ロジック、値オブジェクト、Pure 関数 |
| **Medium** | ローカルファイル / 軽い I/O、< 5 秒 | ScriptableObject 経由のデータロード、Repository 実装 |
| **Large** | 外部リソース / Unity Editor 起動 / ビルド統合 | PlayMode テスト、シーンロード、ビルドパイプライン |

### 2.2 Type 分類

| 分類 | 定義 | テスト例 |
| ---- | ---- | ---- |
| **Normal (正常系)** | 期待される入力に対する期待される結果 | 通常の Draw |
| **SemiNormal (準正常系)** | 仕様内のエッジ(空、最小要素 1、ゼロ等) | 1 枚しかない山札からの Draw |
| **Abnormal (異常系)** | 仕様外の入力 → 例外 / Result.Error | 空山札からの Draw、null 引数 |
| **SuperNormal (超正常系)** | 仕様限界 / オーバーフロー / 上限超過 | int.MaxValue 個の要素、深い再帰 |

### 2.3 NUnit 実装ルール

```csharp
using NUnit.Framework;

[TestFixture]
public class PileTests
{
    [Test, Category("Small"), Category("Normal")]
    public void Given_空でない山札_When_Drawを呼ぶ_Then_先頭カードと残り山札を返す()
    {
        // Given
        var pile = new Pile(new[] { CardId.Of("A"), CardId.Of("B") });
        // When
        var (drawn, remaining) = pile.Draw();
        // Then
        Assert.That(drawn, Is.EqualTo(CardId.Of("A")));
    }
}
```

- 全テストに **Size カテゴリ × 1 + Type カテゴリ × 1 = 計 2 つ以上の `[Category]` 属性必須**
- メソッド名は `Given_X_When_Y_Then_Z` 形式(日本語可)
- AAA (Arrange-Act-Assert) パターンを徹底し、コメント `// Given / When / Then` で区切る
- 1 テスト 1 アサーション原則(複合検証は SubTest 化)

このルールは Phase 0〜1 では lefthook の grep ベース簡易検知、Phase 2 以降でカスタム Roslyn Analyzer に格上げする。

## 3. カバレッジ目標

### 3.1 層別目標

| レイヤ | C0 (Statement) | 重要分岐の網羅 |
| ---- | ---- | ---- |
| Domain | **95%+** | **MC/DC 相当のケース設計を必須**。重要なルール判定(合法手・終了判定)では `docs/specs/.../<feature>.md` にケース表を併記 |
| Application | 80% | 主要 UseCase の正常 / 異常分岐 |
| Infrastructure | 60% | I/O 系はモックで主要経路のみ |
| Presentation(Pure C#) | **80%** | **Presenter / Binder 等の Pure C# クラスのみ計測対象**(`DrowZzzGamePresenter` / `UserSettingsBinder` 等、M5-PR2〜PR7 で多数テスト追加済)。`assemblyFilters` で `Drowsy.Presentation` を含めつつ MonoBehaviour(`DrowZzzGameView` 等)は実質除外(MonoBehaviour は EditMode テストで instantiate しない設計のため自動的に未計測になる) |
| Presentation(MonoBehaviour) | **計測対象外** | MonoBehaviour は手動 QA / E2E でカバー(PlayMode テスト対象、PRES-033 のような `[Optional]` 統合経路) |

### 3.2 計測ツール

`com.unity.testtools.codecoverage` v1.3.0 (Unity 公式)を使用。

- **C0 (Statement Coverage) のみサポート**
- **C1 (Branch Coverage) は v1.3.0 時点で未実装**(常に 0、[公式ドキュメント明記](https://docs.unity3d.com/Packages/com.unity.testtools.codecoverage@1.2/manual/TechnicalDetails.html))
  - 代替として MC/DC ケース表をテスト設計時に併記し、Self-Review で担保する
  - 将来 dotCover 等の外部ツールを併用する選択肢はあるが Phase 0 では不採用
- HTML レポート + Cobertura XML 出力(SonarQube 互換)
- asmdef 単位でフィルタリング可能

### 3.3 計測コマンド例(将来 GitHub Actions で自動実行)

```yaml
- uses: game-ci/unity-test-runner@v4
  with:
    testMode: editmode
    coverageOptions: 'generateAdditionalMetrics;generateHtmlReport;assemblyFilters:+Drowsy.Domain,+Drowsy.Application,+Drowsy.Infrastructure,+Drowsy.Presentation'
```

`Drowsy.Presentation` の MonoBehaviour(`DrowZzzGameView` 等)は EditMode テストで instantiate されないため、結果として Pure C#(Presenter / Binder)のみ計測対象になる(明示的な `-Drowsy.Presentation.Games.DrowZzz.DrowZzzGameView` 除外は不要、2026-05-16 B-2 で確認)。

### 3.4 MC/DC 相当のケース設計

複合条件のあるルール判定では、各条件が独立に結果を決定することを示すケース表を `docs/specs/<layer>/<module>/<feature>.md` 末尾に併記する。

例: `IsLegalMove(card, player, state)` で `card != null && player.HasCard(card) && state.IsCurrentPlayerTurn(player)` の場合、4 ケースで MC/DC を達成。

| # | card | HasCard | IsCurrentTurn | 期待結果 | 備考 |
| ---- | ---- | ---- | ---- | ---- | ---- |
| 1 | not null | true | true | true | 全真 |
| 2 | null | * | * | false | card が独立に結果決定 |
| 3 | not null | false | * | false | HasCard が独立に結果決定 |
| 4 | not null | true | false | false | IsCurrentTurn が独立に結果決定 |

`*` は短絡評価により評価されない条件。

## 4. 要件トレーサビリティ

### 4.1 ID 体系

各 EARS 要件には `[<MODULE>-<NUMBER>]` 形式の一意 ID を付与する。

- **MODULE**: モジュール短縮名(`CARD`, `PILE`, `RND`, `PLY`, `ST` 等、3〜5 文字大文字)
- **NUMBER**: 単純連番(`001` から開始、3 桁ゼロ埋め)
  - 空き番号は許容(削除した ID の番号は再利用しない)
  - カテゴリ(普遍/事象駆動/異常等)で番号を分けない(柔軟性確保)

### 4.2 マーカー

要件種別と直交する「テスト適用可否」のマーカー:

| マーカー | 意味 | 例 |
| ---- | ---- | ---- |
| `[Ubiquitous]` | 構造的性質(immutable / read-only / sealed 等)で、テスト直接検証を免除 | `[CARD-001] [Ubiquitous] The CardId shall be immutable.` |
| `[Optional]` | 設定によって有効化される機能で、テスト対応は任意 | `[RND-005] [Optional] Where seed == 0, ...` |

これらのマーカーが付いた要件はトレーサビリティ機械検証から除外される(警告レベルで通知のみ)。

### 4.3 各レイヤへの埋め込み

| レイヤ | 記法 | 例 |
| ---- | ---- | ---- |
| EARS Markdown | `[<ID>]` を要件文の先頭に | `- [PILE-005] When AddTop(card)...` |
| Gherkin .feature | `@<ID>` をシナリオの直前に(複数可) | `@PILE-005`<br>`シナリオ: AddTop で先頭...` |
| NUnit テスト | `[Property("Requirement", "<ID>")]` 属性(複数可) | `[Test, Category("Small"), Category("Normal"), Property("Requirement", "PILE-005")]` |

複数 ID をまとめてカバーするテストには複数 Property を付与可能:

```csharp
[Property("Requirement", "RND-002"), Property("Requirement", "RND-003")]
public void Given_同じseed_When_NextInt_Then_同じ系列を生成() { ... }
```

### 4.4 機械検証

`scripts/check-traceability.sh` が lefthook の pre-commit で実行され、以下を双方向検証:

| 検出 | レベル | 対処 |
| ---- | ---- | ---- |
| テスト Property に存在するが EARS にない ID(typo) | ERROR | EARS への追加か typo 修正 |
| EARS の必須要件(マーカーなし)でテスト未対応 | ERROR | 対応テストを追加 |
| EARS の `[Ubiquitous]` / `[Optional]` 要件でテスト未対応 | INFO(無視可) | 構造的性質として許容、必要なら reflection テスト追加 |

### 4.5 ID 体系図(Phase 0 時点)

| Module | Prefix | 範囲(Phase 0) |
| ---- | ---- | ---- |
| Cards.CardId | CARD | CARD-001〜008 |
| Cards.Pile | PILE | PILE-001〜013 |
| Random | RND | RND-001〜005 |
| Configuration (IGameConfig) | CFG | CFG-001〜003 |
| (将来) Players | PLY | — |
| (将来) State | ST | — |
| (将来) Action | ACT | — |
| (将来) Rule | RULE | — |
| (将来) Token | TKN | — |
| Infrastructure (`ScriptableObjectCardCatalog` / `DrowZzzGameConfigAsset` / Persistence 等)| INF | M4 で採番開始(ADR-0012)|
| UserSettings(`IUserSettings` / `PlayerPrefsUserSettings`)| USR | M4-PR6 で採番開始(ADR-0012)|

新規モジュール追加時は本表を更新する。`INF-` / `USR-` の有効化は ADR-0012 起票 PR で実施(M3-PR6 起票 PR code-reviewer W-5 反映 2026-05-14)。

## 5. TDD ループ

1. **Red**: 失敗するテストを書く(EARS / Gherkin の 1 シナリオに対応)
2. **Green**: 最小実装でテストを通す(over-engineering 禁止)
3. **Refactor**: テストを通したまま品質を上げる
4. バグ修正は再現テストから書き始める

新機能追加の流れ:
1. EARS Markdown を書く(要件定義)
2. Gherkin .feature を書く(受け入れシナリオ)
3. NUnit テストを書く(失敗確認)
4. 実装(成功確認)
5. リファクタ + カバレッジ確認

## 6. テストファイル配置

```
Assets/_Project/Scripts/Tests/
  Domain.Tests/
    Cards/
      PileTests.cs           # Pile に対するテスト
      CardIdTests.cs
    Players/
      PlayerIdTests.cs
    Random/
      XorShiftRandomTests.cs
  Application.Tests/         # 必要になったら追加
  Infrastructure.Tests/      # 必要になったら追加
```

ディレクトリ構造は `Assets/_Project/Scripts/<Layer>/<Module>/` と対応させる。

## 7. 機械検知

仕様 / テスト規約の機械検知は CLAUDE.md「7. 機械検知方針」を参照。本ドキュメントの規約はすべて以下のいずれかで強制される:

- lefthook (pre-commit / commit-msg)
- Roslyn Analyzer(`Microsoft.CodeAnalysis.NetAnalyzers`, `Microsoft.Unity.Analyzers`, `Roslynator.Analyzers`)
- カスタムスクリプト(`scripts/check-*.sh`)
- GitHub Actions / GameCI(将来)
- Unity Code Coverage パッケージ

## 8. 参考

- [EARS: A Short History](https://alistairmavin.com/ears/)
- [Gherkin Reference](https://cucumber.io/docs/gherkin/reference/)
- [Google Testing Blog: Test Sizes](https://testing.googleblog.com/2010/12/test-sizes.html)
- [DO-178B/C MC/DC Tutorial (FAA)](https://www.faa.gov/sites/faa.gov/files/aircraft/air_cert/design_approvals/air_software/CAST/cast-6.pdf)
