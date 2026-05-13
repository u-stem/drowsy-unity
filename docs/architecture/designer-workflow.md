# Designer ワークフロー(M4-PR7 で確立)

Unity Editor 上で **DrowZzz の SO Asset(`DrowZzzGameConfig.asset` / `DrowZzzCardCatalog.asset`)を Inspector で編集する手順** を確立する。本ドキュメントは M4 完成時点での Designer 体験のリファレンスで、M5 Bootstrap 統合(ADR-0016)で本 .asset を Inspector 注入する経路の前提になる。

## 配置先(M4-PR7 で確定)

| Asset | 配置先 | 個数 |
| ---- | ---- | ---- |
| `DrowZzzGameConfig.asset`(`DrowZzzGameConfigAsset` SO) | `Assets/_Project/Data/Configuration/` | 1 個固定 |
| `DrowZzzCardCatalog.asset`(`ScriptableObjectCardCatalog` SO) | `Assets/_Project/Data/Catalogs/` | 1 個固定 |

**重要(M4-PR7 で訂正、ADR-0016 §7.1 の初期記述との差分)**:`EffectAsset` 派生型(12 種)と `CardEntryAsset` は `[Serializable] POCO`(ScriptableObject 非継承)で、独立 `.asset` ファイルにはならない。すべて **`DrowZzzCardCatalog.asset` 1 個の中に `[SerializeReference]` で polymorphic にインライン編集** される。Designer は 1 つの Catalog Asset を開いて全カード + 全効果をネスト編集する。

```
Assets/_Project/Data/
├── Configuration/
│   ├── DrowZzzGameConfig.asset           ← DrowZzzGameConfigAsset SO(L3 値)
│   └── DrowZzzGameConfig.asset.meta
└── Catalogs/
    ├── DrowZzzCardCatalog.asset          ← ScriptableObjectCardCatalog SO
    │                                       └─ Entries: CardEntryAsset[]
    │                                          └─ Effects: EffectAsset[](SerializeReference)
    └── DrowZzzCardCatalog.asset.meta
```

## ワークフロー 1: `DrowZzzGameConfig.asset` の作成と編集

### 1-1. 新規作成

1. Project ペインで `Assets/_Project/Data/Configuration/` フォルダを作成(右クリック → `Create > Folder`)
2. 同フォルダ内で右クリック → `Create > Drowsy > DrowZzz > Game Config`
3. 自動命名された `DrowZzzGameConfig.asset` を選択
4. Inspector で `FdpPool` / `DdpPool` を確認(`Reset()` で自動投入された本物デフォルト値)

期待される初期値:
- `FdpPool`(10 要素):`[0, 10, 20, 30, 35, 40, 45, 50, 55, 60]`(ADR-0006 §M1)
- `DdpPool`(39 要素 = 13 種 × 3 枚):`-30, -30, -30, -25, -25, -25, ..., 30, 30, 30`(`DdpPoolConstants.BuildDefaultPool()`、5 刻み × 3 枚)

### 1-2. 値の編集

- Inspector の配列 size を変更すると要素が追加 / 削除される
- 空 / null にすると Console に `Debug.LogError` が表示される(`OnValidate` 経路、INF-078 / INF-079)
- 本物に戻したい場合は Inspector 右上の `⋮ > Reset` メニュー

## ワークフロー 2: `DrowZzzCardCatalog.asset` の作成と編集

### 2-1. 新規作成

1. `Assets/_Project/Data/Catalogs/` フォルダを作成
2. 右クリック → `Create > Drowsy > DrowZzz > Card Catalog`
3. `DrowZzzCardCatalog.asset` を選択

### 2-2. 3 カード(No.00「夢」/ No.01「コップ一杯の脅威」/ No.02「緑の侵攻」)を Entries に追加

Inspector で `Entries` の size を `3` に設定し、各 Entry を以下の構造で入力する。構造のリファレンスは `Drowsy.Infrastructure.Tests.Games.DrowZzz.Cards` 配下の各 fixture(`CupOfThreatCardCatalogTests` / `GreenInvasionCardCatalogTests` / `DreamCardCatalogTests`)を参照。

#### Entry 1: No.01「コップ一杯の脅威」(M2-PR3 確立、ADR-0009)

| フィールド | 値 |
| ---- | ---- |
| `CardIdValue` | `01` |
| `Name` | `コップ一杯の脅威` |
| `Attributes` | (空) |
| `Effects` | 1 要素:`TimeOfDayBranchEffectAsset` |
| └ Effects[0].nightEffects | 3 要素 |
| 　└ `AdjustSdpEffectAsset(SdpTarget.Self, -4)` | |
| 　└ `DrawCardEffectAsset(SdpTarget.Self, 1)` | |
| 　└ `AdjustSdpEffectAsset(SdpTarget.Opponent, -10)` | |
| └ Effects[0].morningEffects | 2 要素 |
| 　└ `AdjustSdpEffectAsset(SdpTarget.Self, -4)` | |
| 　└ `AdjustSdpEffectAsset(SdpTarget.Opponent, 10)` | |

#### Entry 2: No.02「緑の侵攻」(M2-PR5 確立、ADR-0007 §1.5)

| フィールド | 値 |
| ---- | ---- |
| `CardIdValue` | `02` |
| `Name` | `緑の侵攻` |
| `Effects` | 1 要素:`ChoiceEffectAsset`(2 分岐) |
| └ Branch[0](攻撃的) | 3 要素:`AdjustSdpEffectAsset(Self, -6)` / `RemoveInfluenceEffectAsset(Opponent)` / `ApplyInfluenceEffectAsset(Opponent, PlayerInfluenceAsset(OwnPhaseStart, AdjustSdpEffectAsset(Self, -5), 3))` |
| └ Branch[1](防御的) | 3 要素:`AdjustSdpEffectAsset(Opponent, 6)` / `RemoveInfluenceEffectAsset(Self)` / `ApplyInfluenceEffectAsset(Self, PlayerInfluenceAsset(...))` |

#### Entry 3: No.00「夢」(M3-PR6 確立、ADR-0011 §6 / §7)

| フィールド | 値 |
| ---- | ---- |
| `CardIdValue` | `00` |
| `Name` | `夢` |
| `Effects` | 4 要素(M3 全機構統合) |
| └ Effects[0] | `AssociatableMarkerEffectAsset()` |
| └ Effects[1] | `RequiresMinimumTotalPointsMarkerEffectAsset(100)`(`DrowZzzVictoryConstants.EarlyWinScoreThreshold`)|
| └ Effects[2] | `UsageRestrictionMarkerEffectAsset()` |
| └ Effects[3] | `TimeOfDayBranchEffectAsset` |
| 　└ nightEffects | 1 要素:`KeywordedEffectAsset([Frenzy, Instinct], EarlyWinTriggerEffectAsset())` |
| 　└ morningEffects | 1 要素:`AdjustSdpEffectAsset(SdpTarget.Self, -80)` |

### 2-3. polymorphic 編集(`[SerializeReference]` + `EffectAssetReferenceDrawer`)

`Effects` フィールドは `EffectAsset` 基底の派生型を `[SerializeReference]` で持つ。Unity 6 標準 UI では型選択ドロップダウンが安定して表示されない(M4-PR7 第 3 弾で実機確認)ため、M4-PR7 第 4 弾で **Custom PropertyDrawer**(`Assets/_Project/Scripts/Infrastructure/Editor/EffectAssetReferenceDrawer.cs`)を導入した。

操作手順:

1. `Effects` 配列の `+` ボタンで要素を追加(初期状態は `(None)`)
2. 追加された **要素 0 / Element 0** の行に **型ドロップダウン**(`(None) / AdjustSdpEffectAsset / ApplyInfluenceEffectAsset / AssociatableMarkerEffectAsset / ChoiceEffectAsset / DamageBedEffectAsset / DrawCardEffectAsset / EarlyWinTriggerEffectAsset / KeywordedEffectAsset / RemoveInfluenceEffectAsset / RequiresMinimumTotalPointsMarkerEffectAsset / TimeOfDayBranchEffectAsset / UsageRestrictionMarkerEffectAsset`)が表示される
3. 派生型を選ぶと該当型の SerializeField 子要素(`Target` / `Delta` / `NightEffects` 等)が下に展開される
4. wrapper effect(`TimeOfDayBranchEffectAsset` の `nightEffects` / `morningEffects`、`ChoiceEffectAsset` の `branches[i].effects`、`KeywordedEffectAsset` の `inner`)の内部 `EffectAsset[]` でも本 Drawer が再帰的に適用されるため、同じドロップダウン UI で型を選べる

### 2-4. ネスト構造の例(No.01「コップ一杯の脅威」)

```
Effects (size = 1)
└─ Element 0: TimeOfDayBranchEffectAsset (ドロップダウンで選択)
   ├─ Night Effects (size = 3)
   │  ├─ Element 0: AdjustSdpEffectAsset      (Target: Self,     Delta: -4)
   │  ├─ Element 1: DrawCardEffectAsset       (Target: Self,     Count: 1)
   │  └─ Element 2: AdjustSdpEffectAsset      (Target: Opponent, Delta: -10)
   └─ Morning Effects (size = 2)
      ├─ Element 0: AdjustSdpEffectAsset      (Target: Self,     Delta: -4)
      └─ Element 1: AdjustSdpEffectAsset      (Target: Opponent, Delta: 10)
```

### 2-5. 重複 ID の検出

Designer が誤って同じ `CardIdValue` を持つ Entry を 2 つ作ると、`ScriptableObjectCardCatalog.OnValidate` が `Debug.LogError(this)` を Console に出力する(Asset リンク付き、Build は妨げない)。

## ワークフロー 3: Application 層での読み取り確認

M4-PR7 範囲では Bootstrap が未実装のため、**Application 層と同等の動作確認は Infrastructure.Tests の 6 件**(`CupOfThreatCardCatalogTests.Given_No01_SO_When_GetName_Then_InMemoryと一致` 等)で **自動検証** される。Designer が手動で作った `.asset` を Application 層で読む確認は M5-PR1 着手時に Bootstrap 経由で行う。

**M5-PR1 着手時の TODO**(M4-PR7 code-reviewer P-5 反映):本ドキュメントの「ワークフロー 3」を Bootstrap 統合経路を反映した内容に更新する(Project ペインに置いた `DrowZzzCardCatalog.asset` を `ProjectLifetimeScope` の `[SerializeField]` に割り当てる手順 + Play モードで Application 層が同 .asset を読んでゲームが起動する確認)。M5-PR1 PR description に本更新 TODO を明示。

## チェックリスト(M4-PR7 完成時に Designer 体験を実証する手順)

- [ ] `Assets/_Project/Data/Configuration/DrowZzzGameConfig.asset` を Inspector から作成(`Create > Drowsy > DrowZzz > Game Config`)
- [ ] FdpPool が `[0, 10, ..., 60]` で 10 要素自動投入されている
- [ ] DdpPool が 39 要素自動投入されている(13 種 × 3 枚、`-30, -30, -30, ..., 30, 30, 30`)
- [ ] FdpPool を空にすると Console に `Debug.LogError` が表示される
- [ ] Reset メニューで本物のデフォルト値に戻せる
- [ ] `Assets/_Project/Data/Catalogs/DrowZzzCardCatalog.asset` を Inspector から作成(`Create > Drowsy > DrowZzz > Card Catalog`)
- [ ] No.01「コップ一杯の脅威」を 1 Entry として追加し、TimeOfDayBranchEffect + 内側 5 effect を入力
- [ ] No.02「緑の侵攻」を 1 Entry として追加し、ChoiceEffect 2 分岐 + 内側 6 effect を入力
- [ ] No.00「夢」を 1 Entry として追加し、4 effect(マーカー 3 + TimeOfDayBranch wrapper)を入力
- [ ] 同じ `CardIdValue` を持つ Entry を 2 つ作ると Console に `Debug.LogError` が表示される
- [ ] スクリーンショット 2 枚を取得(`DrowZzzGameConfig.asset` の Inspector + `DrowZzzCardCatalog.asset` の Inspector の Entries 展開)、PR description に添付

## 関連

- ADR: [ADR-0012 §3](../adr/0012-m4-scriptableobject-and-persistence.md) — SO 表現案 (a)
- ADR: [ADR-0016 §7.1 / §7.2](../adr/0016-m5-bootstrap-presentation.md) — Bootstrap での Inspector 注入経路
- Spec: [`docs/specs/infrastructure/card-catalog.md`](../specs/infrastructure/card-catalog.md) — `ScriptableObjectCardCatalog`
- Spec: [`docs/specs/infrastructure/effect-assets.md`](../specs/infrastructure/effect-assets.md) — `EffectAsset` 12 派生型 + 中間型 2 件
- Spec: [`docs/specs/infrastructure/game-config-asset.md`](../specs/infrastructure/game-config-asset.md) — `DrowZzzGameConfigAsset`
- Fixture: `Drowsy.Infrastructure.Tests.Games.DrowZzz.Cards.{CupOfThreat,GreenInvasion,Dream}CardCatalogTests` — 3 カードの構造リファレンス
