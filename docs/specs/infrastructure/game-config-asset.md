# `DrowZzzGameConfigAsset`(M4-PR7、ADR-0012 §3)

ADR-0012 §3 で宣言された `IGameConfig` の `ScriptableObject` 実装。M4-PR1〜PR6 では PR 分割計画から見落とされていたが、**M4-PR7(M4 完成 PR)で本 SO を追加実装**し、Designer が Inspector で FdpPool / DdpPool を編集可能にする。

## 概要

| 観点 | 値 |
| ---- | ---- |
| 型 | `Drowsy.Infrastructure.Configuration.DrowZzzGameConfigAsset : ScriptableObject, IGameConfig` |
| Asset 配置 | `Assets/_Project/Data/Configuration/DrowZzzGameConfig.asset` 1 個固定(ADR-0016 §7.1)|
| 編集対象プロパティ | `FdpPool`(L3) / `DdpPool`(L3)。L1 / L2(`DdpPoolConstants` / `DrowZzzClockConstants` / `DrowZzzVictoryConstants`)は編集対象外(ADR-0010 §8 / §9)|
| 内部表現 | `[SerializeField] int[] _fdpPool` + `[SerializeField] int[] _ddpPool` |
| デフォルト値投入 | `Reset()`(Editor only)で `[0, 10, ..., 60]`(ADR-0006 §M1)+ `DdpPoolConstants.BuildDefaultPool()`(39 要素整序、13 種 × 3 枚)|
| null / 空配列の挙動 | `Array.Empty<int>()` を返す(graceful)+ `OnValidate` で `Debug.LogError`(JIT 確定 2026-05-13 同パターン継承)|
| 本番経路 | M5 Bootstrap の `ProjectLifetimeScope` が `[SerializeField] DrowZzzGameConfigAsset` を Inspector 注入 → `RegisterInstance<IGameConfig>(_gameConfig)`(ADR-0016 §7.2)|
| テスト経路 | `ScriptableObject.CreateInstance<T>` + `internal SetPoolsForTest`(`InternalsVisibleTo("Drowsy.Infrastructure.Tests")` 経由、M4-PR1 で確立した `ScriptableObjectCardCatalog` パターン継承)|

## 構造

```csharp
[CreateAssetMenu(menuName = "Drowsy/DrowZzz/Game Config", fileName = "DrowZzzGameConfig")]
public sealed class DrowZzzGameConfigAsset : ScriptableObject, IGameConfig
{
    [SerializeField] private int[] _fdpPool;
    [SerializeField] private int[] _ddpPool;

    public IReadOnlyList<int> FdpPool => _fdpPool ?? Array.Empty<int>();
    public IReadOnlyList<int> DdpPool => _ddpPool ?? Array.Empty<int>();

    private void Reset();       // Editor only: デフォルト値投入
    private void OnValidate();  // Editor only: 空 / null を Debug.LogError
    internal void SetPoolsForTest(int[] fdpPool, int[] ddpPool);  // test 専用
}
```

## 要件(EARS)

### Ubiquitous

- [INF-072] [Ubiquitous] `DrowZzzGameConfigAsset.FdpPool` プロパティは `[SerializeField] int[] _fdpPool` の参照を `IReadOnlyList<int>` として公開する。
- [INF-073] [Ubiquitous] `DrowZzzGameConfigAsset.DdpPool` プロパティは `[SerializeField] int[] _ddpPool` の参照を `IReadOnlyList<int>` として公開する。

### Event-driven

- [INF-074] `_fdpPool` が `null` のとき `FdpPool` プロパティは `Array.Empty<int>()` を返す。
- [INF-075] `_ddpPool` が `null` のとき `DdpPool` プロパティは `Array.Empty<int>()` を返す。
- [INF-076] Unity Editor が `Reset()` を呼ぶとき(新規 `.asset` 作成時 / Inspector の Reset メニュー実行時)、`_fdpPool` に ADR-0006 §M1 の本物プール `[0, 10, 20, 30, 35, 40, 45, 50, 55, 60]`(10 要素)が投入される。
- [INF-077] Unity Editor が `Reset()` を呼ぶとき、`_ddpPool` に `DdpPoolConstants.BuildDefaultPool()` と同値の 39 要素プール(13 種 × 3 枚、`-30, -30, -30, -25, -25, -25, ..., 30, 30, 30`)が投入される。
- [INF-078] `OnValidate()` が呼ばれたとき `_fdpPool` が `null` または長さ 0 ならば、`Debug.LogError` が「`DrowZzzGameConfigAsset: FdpPool が空 / null`」メッセージ + Asset リンクで発火する。
- [INF-079] `OnValidate()` が呼ばれたとき `_ddpPool` が `null` または長さ 0 ならば、`Debug.LogError` が「`DrowZzzGameConfigAsset: DdpPool が空 / null`」メッセージ + Asset リンクで発火する。

## Gherkin 受け入れシナリオ

`game-config-asset.feature` を参照。

## ADR-0012 §4 検証の縮退と先送り(M4-PR7)

ADR-0012 §4「Designer 検証(`OnValidate`、初期推奨)」が挙げる 3 件のうち、本 M4-PR7 では **「null / 空」検出のみを INF-078 / INF-079 として実装** し、以下 3 件は `docs/todo.md`(M5 以降)で追跡する:

| 検証項目 | 重要度 | 先送り理由 |
| ---- | ---- | ---- |
| `_fdpPool.Length >= プレイヤー数 N`(現状 N=2 想定) | 中 | プレイヤー数 N が SO 側にハードコードされると Phase 3 の N>2 拡張で再修正必要、設計判断は M5 着手時に再評価 |
| `_fdpPool` 重複なし | 中 | `StartGameUseCase` が重複なし抽選を要求(ADR-0006 §1.4 / CFG-101)、本番経路では `StartGameUseCase` 側で `ArgumentException` を投げるためサイレント通過しても実行時 fail は早期、Designer フィードバックの即時性のみが論点 |
| `_ddpPool` の合計値が 0 | 低 | デフォルト値(13 種 × 3 枚 = 39 要素、-30〜+30 対称)では 0、Designer が意図的に非対称設計したい場合は LogWarning のみで build を妨げない仕様(ADR-0012 §4) |

これら 3 件は **`[Optional]` マーカー相当の扱い** で本 PR のトレーサビリティ機械検証から免除する(`docs/todo.md` エントリで追跡)。

## 定数依存

- `DdpPoolConstants.BuildDefaultPool()`(`Drowsy.Application.Games.DrowZzz`):INF-077 の参照定数
- `DefaultFdpPool`(本 SO 内 `private static readonly`):ADR-0006 §M1 で確定した本物 FdpPool、INF-076 の参照定数

## 関連

- ADR: [ADR-0012 §3](../../adr/0012-m4-scriptableobject-and-persistence.md) — SO 実装の宣言
- ADR: [ADR-0006 §1.4](../../adr/0006-m1-detail-application-interfaces.md) — `IGameConfig` 拡張予定表
- ADR: [ADR-0010 §8 / §9](../../adr/0010-m3-game-termination-and-victory-determination.md) — `MaxRoundNumber` / `EarlyWinScoreThreshold` が `IGameConfig` 非対象
- ADR: [ADR-0016 §7.1 / §7.2](../../adr/0016-m5-bootstrap-presentation.md) — Bootstrap での `[SerializeField]` 注入
- 関連: [`docs/specs/infrastructure/card-catalog.md`](card-catalog.md) — 同 M4-PR1 で確立した SO + `[CreateAssetMenu]` + `internal SetXxxForTest` パターン
- 関連: [`docs/architecture/constants-management.md`](../../architecture/constants-management.md) §L3
