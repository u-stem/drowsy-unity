# テスト戦略

drowsy-unity のテスト方針詳細。CLAUDE.md §6 / §7 の補足ドキュメント。

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

このルールは Phase 0 では lefthook の grep ベース簡易検知、Phase 1 以降でカスタム Roslyn Analyzer に格上げする。

## 3. カバレッジ目標

### 3.1 層別目標

| レイヤ | C0 (Statement) | C1 (Branch) | MC/DC 相当のケース設計 |
| ---- | ---- | ---- | ---- |
| Domain | **95%+** | **100%** | 重要なルール判定(合法手・終了判定)で必須。`docs/specs/.../<feature>.md` にケース表を併記 |
| Application | 80% | 80% | 主要 UseCase の正常 / 異常分岐のみ |
| Infrastructure | 60% | 50% | I/O 系はモックで主要経路のみ |
| Presentation | **計測対象外** | — | MonoBehaviour 中心。手動 QA / E2E でカバー |

### 3.2 計測ツール

`com.unity.testtools.codecoverage` (Unity 公式)を使用。

- C0 / C1 計測対応(MCC / MC/DC は計測不可、ケース設計で意識)
- HTML レポート + Cobertura XML 出力(SonarQube 互換)
- asmdef 単位でフィルタリング可能

### 3.3 計測コマンド例(将来 GitHub Actions で自動実行)

```yaml
- uses: game-ci/unity-test-runner@v4
  with:
    testMode: editmode
    coverageOptions: 'generateAdditionalMetrics;generateHtmlReport;assemblyFilters:+Drowsy.Domain,+Drowsy.Application'
```

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

## 4. TDD ループ

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

## 5. テストファイル配置

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

## 6. 機械検知

仕様 / テスト規約の機械検知は CLAUDE.md §7 を参照。本ドキュメントの規約はすべて以下のいずれかで強制される:

- lefthook (pre-commit / commit-msg)
- Roslyn Analyzer(`Microsoft.CodeAnalysis.NetAnalyzers`, `Roslynator.Analyzers`)
- カスタムスクリプト(`scripts/check-*.sh`)
- GitHub Actions / GameCI(将来)
- Unity Code Coverage パッケージ

## 7. 参考

- [EARS: A Short History](https://alistairmavin.com/ears/)
- [Gherkin Reference](https://cucumber.io/docs/gherkin/reference/)
- [Google Testing Blog: Test Sizes](https://testing.googleblog.com/2010/12/test-sizes.html)
- [DO-178B/C MC/DC Tutorial (FAA)](https://www.faa.gov/sites/faa.gov/files/aircraft/air_cert/design_approvals/air_software/CAST/cast-6.pdf)
