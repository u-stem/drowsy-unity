# 定数管理方針

drowsy-unity プロジェクトで「マジックナンバー禁止」を実現するための階層構造と運用規約。
CLAUDE.md「9. 定数管理方針」の補足ドキュメント。

---

## 1. 階層モデル

定数を 5 階層に分類し、階層ごとに実装場所と検証方法を変える。

| 階層 | 種類 | 実装場所 | 例 |
| ---- | ---- | ---- | ---- |
| **L1** | 数学的・物理的不変量 | Domain `<Module>Constants` (`const`) | `MathConstants.Pi`、入力検証境界 |
| **L2** | ドメイン上の真の不変量 | Domain `<Module>Constants` (`const`) | `PileConstants.MaxCards`、トランプ 52 枚 |
| **L3** | ゲームバランス調整可能値 | `IGameConfig` interface (Domain) + `GameConfigSO` (Infrastructure) | 初期手札数、最大ライフ、ターン制限 |
| **L4** | ユーザー設定 | `IUserSettings` interface + PlayerPrefs / save file | 音量、難易度 |
| **L5** | 環境固有値・ビルド設定 | Unity Editor 設定 / `csc.rsp` の define シンボル | デバッグフラグ、`UNITY_EDITOR` |

階層の判断基準:

```
質問 1: コンパイル時に決まる値か?
  YES → L1 / L2(const)
  NO  → 質問 2 へ

質問 2: ゲームデザイナーが Inspector で調整するか?
  YES → L3(IGameConfig 経由)
  NO  → 質問 3 へ

質問 3: ユーザーが設定 UI で変更するか?
  YES → L4(IUserSettings 経由)
  NO  → L5(ビルド設定)
```

---

## 2. L1 / L2: Domain Constants

### 命名規則

- ファイル: `Assets/_Project/Scripts/Domain/<Module>/<Module>Constants.cs`
- クラス: `public static class <Module>Constants`
- メンバー: `public const <Type> <PascalCaseName>`(C# の const 慣習)
- namespace: `Drowsy.Domain.<Module>`

### 例

```csharp
namespace Drowsy.Domain.Cards
{
    /// <summary>Pile に関する真の不変量(ゲームルールに依存しない物理的限界)</summary>
    public static class PileConstants
    {
        /// <summary>1 山札に置ける最大カード数(int.MaxValue / 2、安全マージン)</summary>
        public const int MaxCards = 1_000_000;

        /// <summary>Draw 操作の最小カード数(空 Pile で例外を投げる境界)</summary>
        public const int MinCardsToDraw = 1;
    }
}
```

### 使用ガイドライン

- マジックナンバー(リテラル直書きの数値)は禁止
- `if (count > 5)` のような直書きは `if (count > PileConstants.MaxHandSize)` に置換
- 0 / 1 / -1 / null / "" / 単位倍率(1000 等)など意味が自明なリテラルは例外として許容

---

## 3. L3: IGameConfig + ScriptableObject

### Domain 側(interface 宣言)

```csharp
// Assets/_Project/Scripts/Domain/Configuration/IGameConfig.cs
namespace Drowsy.Domain.Configuration
{
    public interface IGameConfig
    {
        int InitialHandSize { get; }
        int MaxLifePoints { get; }
        System.TimeSpan TurnLimit { get; }
    }
}
```

### Infrastructure 側(ScriptableObject 実装)

```csharp
// Assets/_Project/Scripts/Infrastructure/Configuration/GameConfigSO.cs
using UnityEngine;
using Drowsy.Domain.Configuration;

namespace Drowsy.Infrastructure.Configuration
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Drowsy/GameConfig")]
    public sealed class GameConfigSO : ScriptableObject, IGameConfig
    {
        [SerializeField, Min(1), Tooltip("ゲーム開始時の初期手札数")]
        private int _initialHandSize = 5;

        [SerializeField, Min(1), Tooltip("プレイヤーの最大ライフ")]
        private int _maxLifePoints = 20;

        [SerializeField, Min(1f), Tooltip("1 ターンの制限時間(秒)")]
        private float _turnLimitSeconds = 30f;

        public int InitialHandSize => _initialHandSize;
        public int MaxLifePoints => _maxLifePoints;
        public System.TimeSpan TurnLimit => System.TimeSpan.FromSeconds(_turnLimitSeconds);
    }
}
```

### Bootstrap 側(VContainer 登録)

```csharp
// Assets/_Project/Scripts/Bootstrap/AppLifetimeScope.cs
public class AppLifetimeScope : LifetimeScope
{
    [SerializeField] private GameConfigSO _gameConfig;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterInstance<IGameConfig>(_gameConfig);
    }
}
```

### 利点

- ゲームデザイナーが Inspector で値変更可能(リコンパイル不要)
- テスト時は Mock or Stub の `IGameConfig` を VContainer で差し替え
- Domain は UnityEngine 非依存のまま設定値を扱える
- `[SerializeField, Min(1)]` のような Unity 標準属性で値検証可能

---

## 4. L4: User Settings(Phase 2 以降)

```csharp
namespace Drowsy.Domain.Configuration
{
    public interface IUserSettings
    {
        float MasterVolume { get; }
        Difficulty Difficulty { get; }
    }
}
```

実装は `PlayerPrefs` または JSON save file。Phase 2 で具体化。

---

## 5. L5: 環境固有値

- `UNITY_EDITOR` / `DEVELOPMENT_BUILD` / `UNITY_WEBGL` 等の define シンボル
- `Editor` / `Player` ビルドの分岐
- `csc.rsp` の追加 define
- これらは「定数」ではなく「ビルド時条件分岐」なので、本ドキュメントのスコープ外として扱う

---

## 6. マジックナンバー禁止の機械検知

### Roslyn Analyzer

- `Microsoft.CodeAnalysis.NetAnalyzers`:
  - **CA1802**(`Use literals where appropriate`): `static readonly` のリテラル化推奨 → const 利用を強制
- `Microsoft.Unity.Analyzers`: Unity 固有の magic number 検出は限定的

`.editorconfig` で `dotnet_diagnostic.CA1802.severity = warning` に設定し、IDE / コンパイル時に警告として現れる。

### カスタム Roslyn Analyzer(Phase 2 以降)

公開 Analyzer ではマジックナンバー検出が弱いため、カスタム Analyzer の検討候補:

- `if`、`for`、`while` のリテラル直書きを警告
- `0`、`1`、`-1`、`""`、`null` 等の自明リテラルは例外
- 配列インデックス `[0]` `[1]` も例外

これは Phase 2 以降で工数次第。

---

## 7. 既存コードでの適用

### Phase 0 で既に存在する定数(Phase 1 で見直し対象)

| 場所 | リテラル | 判定 | 対処 |
| ---- | ---- | ---- | ---- |
| `XorShiftRandom.cs` | `seed == 0u ? 1u : seed` の `0u` / `1u` | XorShift32 退化点回避の数学的不変量 | `RandomConstants` に切り出し可だが、コメントで意図明記済 + 自明 |
| `XorShiftRandom.cs` | `_state ^= _state << 13` 等のシフト量 | アルゴリズム定数(XorShift32 仕様) | `RandomConstants` 候補(Phase 1) |
| `Pile.cs` | (該当なし) | — | — |
| `CardId.cs` | (該当なし) | — | — |

Phase 1 でこれらを `RandomConstants` 等に整理する選択肢あり。ただし XorShift32 シフト量はアルゴリズム不可分なのでコード内に残す方が読みやすい場合もある。

---

## 8. 仕様書での定数依存の表明

各機能の EARS Markdown(`docs/specs/<layer>/<module>/<feature>.md`)末尾に「定数依存」セクションを設け、依存する定数を列挙する。

```markdown
## 定数依存(該当する場合のみ)

| 定数 | 階層 | 由来 |
| ---- | ---- | ---- |
| MaxCards | L2 | `PileConstants.MaxCards` |
| InitialHandSize | L3 | `IGameConfig.InitialHandSize` |
```

これによりゲームバランスを調整したい時、影響範囲(=該当定数を参照する仕様)を逆引きできる。

---

## 9. ID 体系

定数管理関連の EARS 要件には `CFG-XXX`(Configuration の略)を使う。

| Module | Prefix |
| ---- | ---- |
| Configuration (IGameConfig) | CFG |
| (将来) UserSettings | USR |
| (将来) Constants 全般 | CONST |

`docs/testing-strategy.md` §4.5 の ID 体系図に追記する。

---

## 10. 参考

- [Microsoft Learn: Use constants](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/constants)
- [Unity Manual: ScriptableObject](https://docs.unity3d.com/Manual/class-ScriptableObject.html)
- [VContainer: Register and Resolve](https://vcontainer.hadashikick.jp/registering/registering-no-mono)
