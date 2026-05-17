# カード No.11「機械仕掛けの冬将軍」 (Phase 2 完結後)

DrowZzz Phase 2 完結後の **第 9 新規カード追加**(2026-05-17 オーナー JIT 確定)。**狂乱(Frenzy)持ち** + 即時 SDP 変動(自分 -4 / 相手 -8)+ 乙に「自フェーズ開始時に SDP-n(n = 乙の Hand.Count)」の **動的計算永続 Influence** を付与する戦術カード。「手札が多い相手ほど刻まれる」退却型デバフで、乙のリソース管理戦略を圧迫する。

## 概要

| 観点 | 値 |
| ---- | ---- |
| カード番号 | No.11 |
| 名前 | 機械仕掛けの冬将軍 |
| CardTypeId | `"11"` |
| 初期山札枚数 | 1(オーナー JIT 確定 2026-05-17、No.06「牙の届かぬ領域」と同じ Frenzy + 高威力レア枠)|
| 効果構造 | 最上位 3 件:`AdjustSdpEffect(Self, -4)` + `AdjustSdpEffect(Opponent, -8)` + `KeywordedEffect([Frenzy], ApplyInfluenceEffect(Opponent, WinterGeneralInfluence))` |
| 新規導入概念 | `AdjustSdpByHandCountEffect`(影響保有者の Hand.Count を Tick 時に動的計算して SDP-n、動的計算 TickEffect の初導入)|

## 効果

時間帯非依存(夜・朝両方で同じ効果、`ChoiceEffect` / `TimeOfDayBranchEffect` なし):

- 自分の SDP が 4 減る
- 相手の SDP が 8 減る
- 自分(actor)はカード全体に **Frenzy(狂乱)キーワード** を付与(`HasKeywordInEffects` で OR 検出 → 相手の Counter Action illegal 化、ADR-0011 §4.5、No.06「牙の届かぬ領域」と同パターン)
- 相手にこのカード固有の **影響(永続、`InfluenceConstants.Perpetual`)** を付与

戦略的解釈:
- 自分 SDP -4 のコストを払い、相手 SDP -8 + 自フェーズ開始時に **動的 SDP-n**(n = 相手の Hand.Count)の継続デバフを永続付与
- 相手は「次のターンに引かずに手札を減らしてからフェーズを迎えれば被害が少ない」「手札を増やせばダメージが膨れる」というリソース管理の判断を強いられる
- Frenzy 持ちのため相手から反撃を受けない(Counter キーワードカードでの打ち消し不可)
- 永続 Influence のため、相手が「廻るための知恵(No.08)」のような SDP 符号反転 marker を持つと **保有者の SDP がプラス化**(回復方向)になる可能性あり(永続マーカー同士の組み合わせ戦術)
- 「機械仕掛け」というフレーバーは「規則的・無慈悲に毎ターン襲ってくる」という Tick 機構の擬人化、「冬将軍」は寒波擬人化(凍えて手が動かなくなる = 手札が逆効果になる)を表現

## カード固有「影響」

| 観点 | 値 |
| ---- | ---- |
| トリガー | `InfluenceTrigger.OwnPhaseStart`(影響保有者の自フェーズ開始時)|
| Tick 効果 | `AdjustSdpByHandCountEffect`(動的計算:`SDP[保有者] -= 保有者.Hand.Count`)|
| 残発動回数 | **`InfluenceConstants.Perpetual` = int.MaxValue**(永続、テキスト「カウント」記述なし = 永続解釈、No.08「廻るための知恵」と同じ慣例)|
| Semantics | **存在時:影響保有者の自フェーズ開始時 Tick で `SDP -= Hand.Count`**。Hand.Count == 0 なら no-op(SDP -0 = 不変、graceful)|

### 動的計算の実装メカニズム

`AdjustSdpByHandCountEffect` は **EffectInterpreter 内で session 状態を見て動的に計算** する初の effect:

- 従来の `AdjustSdpEffect(Self, -5)`(No.02「緑の侵攻」)は **固定 delta** で、record フィールドに `Delta` を持つ
- 本 effect は **record フィールドなし**、Tick 時に `session.GameState.Turn.CurrentPlayerIndex` が指すプレイヤー(= 影響保有者、`TickInfluencesForCurrentPlayer` は新 current player に対して Tick)の Hand.Count を取得
- ADR-0007 §1.3「EffectInterpreter」の `Apply(session, effect, context)` 3 引数 overload を活用(context は EffectContext.Default で OK、per-play 文脈不要)

### 「機械仕掛け」= Perpetual の意図

「機械」のように **規則的・無慈悲に毎ターン襲ってくる** ことを `Perpetual`(int.MaxValue)で表現。「廻るための知恵(No.08)」の永続 Influence と同パターンで、ゲーム終了まで保有し続ける(ADR-0020 後の Decrement で毎フェーズ -1 されるが、`int.MaxValue - 42`(N=2 × 21 ラウンド)≒ MaxValue なので実質除去されない、`InfluenceConstants.Perpetual` xmldoc 参照)。

### Hand.Count == 0 の挙動

Tick 時に保有者の Hand.Count == 0 の場合、`SDP -= 0` は no-op(session 不変返却)。これは graceful な仕様で、保有者が手札を全部使い切ったフェーズでは「機械仕掛けが止まる」という直感的な挙動になる。

### No.08「廻るための知恵」との組み合わせ(オーナー JIT 共有 2026-05-17 想定)

両者を保有する稀ケース:
- No.08 InvertBedDamage は `ApplyBedDamageToCurrentPlayer` 内の **ベッド破損 SDP マイナス計算** の符号を反転する marker
- 本 effect の `SDP -= Hand.Count` は **`ApplyBedDamageToCurrentPlayer` の経路外**(EffectInterpreter 経由)で実行される
- そのため両者は **独立して計算**(No.08 はベッド破損のみ反転、本 effect は Hand.Count 由来の SDP マイナスはそのまま実行)

## カードデータ表現(InMemoryCardCatalog 登録形)

```csharp
// entries 側
new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("11"), new CardData("機械仕掛けの冬将軍", new Dictionary<string, int>()))

// 影響定義(永続、Tick で動的計算)
var WinterGeneralInfluence = new PlayerInfluence(
    InfluenceTrigger.OwnPhaseStart,
    new AdjustSdpByHandCountEffect(),
    InfluenceConstants.Perpetual);

// effects 側(最上位 3 件、3 件目を Frenzy 包みでカード全体に Frenzy 性質付与、No.06 と同パターン)
new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(CardTypeId.Of("11"), new IEffect[]
{
    new AdjustSdpEffect(SdpTarget.Self, -4),
    new AdjustSdpEffect(SdpTarget.Opponent, -8),
    new KeywordedEffect(new[] { Keyword.Frenzy },
        new ApplyInfluenceEffect(SdpTarget.Opponent, WinterGeneralInfluence)),
})
```

## 普遍要件 (Ubiquitous)

- [DZ-323] [Ubiquitous] Card `"11"` shall be registered in the initial `InMemoryCardCatalog` (and `ScriptableObjectCardCatalog` for production) with name `"機械仕掛けの冬将軍"` and the **3 top-level effects** specified above. The third effect shall be a `KeywordedEffect([Frenzy], ApplyInfluenceEffect(...))` so that `HasKeywordInEffects(effects, Frenzy)` returns `true`(カード全体に Frenzy 性質)。

## 事象駆動要件 (Event-driven)

- [DZ-324] When Card `"11"` is played by player A on player B, the resulting session shall reflect `SDP[A] -= 4`(時間帯非依存)。
- [DZ-325] When Card `"11"` is played by player A on player B, the resulting session shall reflect `SDP[B] -= 8`(時間帯非依存)。
- [DZ-326] When Card `"11"` is played by player A on player B, B's influence list shall gain one new `PlayerInfluence(OwnPhaseStart, AdjustSdpByHandCountEffect, InfluenceConstants.Perpetual)` entry.

## 合法性判定(`IsLegalMove` 拡張、既存パターン)

- [DZ-327] When Card `"11"` is targeted by `CounterAction`(対戦相手による反撃)during `WaitingForCounterResponse`, `IsLegalMove` shall return `false`(Frenzy 持ちカードは反撃を受けない、ADR-0011 §4.5、No.06「牙の届かぬ領域」/ No.07「知恵の及ばぬ領域」と同パターン)。

## Tick 評価のシナリオ(動的 SDP-n)

- [DZ-328] When B holds `PlayerInfluence(OwnPhaseStart, AdjustSdpByHandCountEffect, Perpetual)` and a phase rotation makes B the new current player while B's Hand has 3 cards, the resulting session shall reflect `SDP[B] -= 3`(動的計算:Hand.Count=3 → SDP-3)。
- [DZ-329] When B holds the influence with Hand.Count == 0 at the moment of phase rotation, the influence Tick shall be a graceful no-op(`SDP[B]` は不変、Hand.Count=0 → SDP-0)。
- [DZ-330] When B holds the influence and a phase rotation Tick fires, the influence shall remain with `RemainingCount = Perpetual`(ADR-0020:Tick は count 不変、B 自身の EndTurn で count -1 されるが Perpetual は実質除去されない、`InfluenceConstants.Perpetual` xmldoc 参照)。
- [DZ-331] When B holds the influence with Hand.Count == 1 at the moment of phase rotation, the resulting session shall reflect `SDP[B] -= 1`(最小非ゼロ境界、code-reviewer P-1 反映)。
- [DZ-332] When B holds the influence with Hand.Count == 3 at the moment of phase rotation, A's `SDP` shall remain unchanged(他プレイヤー SDP 保護、`kv.Key.Equals(currentPlayer.Id)` ガード回帰防御、code-reviewer W-2 反映)。
- [DZ-333] When B holds the influence with Hand.Count == 5 at the moment of phase rotation, the resulting session shall reflect `SDP[B] -= 5`(大き目境界、foreach 累積バグ防御、code-reviewer P-1 反映)。

## 定数依存

- **L3(カード設計値、本カード固有)**:
  - 自分 SDP 即時変動: -4
  - 相手 SDP 即時変動: -8
  - 影響 RemainingCount: `InfluenceConstants.Perpetual`(int.MaxValue、永続)
- **L2 / L1**: 該当なし(本カード独自の計算経路はなく、既存 `AdjustSdpEffect` / `ApplyInfluenceEffect` / `KeywordedEffect` + 新規 `AdjustSdpByHandCountEffect`(本 PR で導入)の組み合わせ)

## 関連

- ADR:
  - [`docs/adr/0007-m2-detail-card-effects.md`](../../../../adr/0007-m2-detail-card-effects.md) §1.3「EffectInterpreter」(動的計算 effect の初導入は ADR-0007 の switch パターンに従う)
  - [`docs/adr/0011-m3-dream-card-and-game-mechanics-expansion.md`](../../../../adr/0011-m3-dream-card-and-game-mechanics-expansion.md) §4.5「Frenzy = 反撃を受けない」
  - [`docs/adr/0020-influence-count-decrement-timing.md`](../../../../adr/0020-influence-count-decrement-timing.md)(Influence count -1 タイミング、Perpetual も毎フェーズ -1 だが実質除去されない)
- 前提効果: [`../effects/adjust-sdp.md`](../effects/adjust-sdp.md) / [`../effects/apply-influence.md`](../effects/apply-influence.md) / [`../effects/keyworded-effect.md`](../effects/keyworded-effect.md)
- 前提効果(本 PR で実装、spec md 未作成):`AdjustSdpByHandCountEffect`(`Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/AdjustSdpByHandCountEffect.cs` 参照、xmldoc に Design 記述あり)
- 既存類似カード:
  - [`./untouchable-realm.md`](./untouchable-realm.md)(No.06、Frenzy 持ち + Influence 付与、構造同型)
  - [`./circulating-wisdom.md`](./circulating-wisdom.md)(No.08、Perpetual Influence 採用パターン)
  - [`./green-invasion.md`](./green-invasion.md)(No.02、TickEffect 内で SDP 変動を行う既存パターン、ただし固定 delta)
- 実装(本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/AdjustSdpByHandCountEffect.cs`(新規)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/EffectInterpreter.cs`(case + ApplyAdjustSdpByHandCount ヘルパー)
  - `Assets/_Project/Scripts/Infrastructure/Games/DrowZzz/Effects/AdjustSdpByHandCountEffectAsset.cs`(新規)
  - `Assets/_Project/Scripts/Infrastructure/Persistence/Converters/EffectJsonConverter.cs`(1 dispatch case)
  - `Assets/_Project/Data/Catalogs/DrowZzzCardCatalog.asset`(No.11 entry + rid 990〜994 追加)
  - `Assets/_Project/Scripts/Bootstrap/ProjectLifetimeScope.cs`(カード種数コメント「11 → 12 種」)
- テスト(本 PR):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/MechanicalWinterGeneralCardTests.cs`(新規、DZ-324〜330)
  - `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Games/DrowZzz/Cards/MechanicalWinterGeneralCardCatalogTests.cs`(新規、SO ↔ InMemory 同値性、INF-151)
  - `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Persistence/EffectJsonConverterTests.cs`(INF-152、Round-Trip)
- シナリオ: `clockwork-winter-general.feature`

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-323 | (テスト免除: Ubiquitous) | catalog 登録 + Frenzy 性質は `MechanicalWinterGeneralCardTests` のヘルパー + `MechanicalWinterGeneralCardCatalogTests` で構造的に保証 |
| DZ-324 | `Given_任意フェーズ_When_Card11をプレイ_Then_自分のSDPがマイナス4` | 統合テスト |
| DZ-325 | `Given_任意フェーズ_When_Card11をプレイ_Then_相手のSDPがマイナス8` | 統合テスト |
| DZ-326 | `Given_任意フェーズ_When_Card11をプレイ_Then_相手のInfluencesにWinterGeneralが付与される` | 統合テスト |
| DZ-327 | `Given_p2手札にCard11_When_p1がCounterActionで対象_Then_IsLegalMoveがfalse` | Frenzy = 反撃不可 |
| DZ-328 | `Given_p2が本Influence保有_HandCount3_p1current_When_p1EndTurnでp2フェーズへ_Then_p2のSDPがマイナス3` | 動的計算 |
| DZ-329 | `Given_p2が本Influence保有_HandCount0_p1current_When_p1EndTurnでp2フェーズへ_Then_p2のSDPは不変` | graceful no-op |
| DZ-330 | `Given_p2が本Influence保有_p1current_When_p1EndTurnでp2フェーズへ_Then_p2のInfluenceRemainingCountは不変Perpetual` | ADR-0020 Tick で count 不変 |
| DZ-331 | `Given_p2が本Influence保有_HandCount1_p1current_When_p1EndTurnでp2フェーズへ_Then_p2のSDPがマイナス1` | 最小非ゼロ境界 |
| DZ-332 | `Given_p2が本Influence保有_HandCount3_p1current_When_p1EndTurnでp2フェーズへ_Then_p1のSDPは不変` | 他プレイヤー SDP 保護 |
| DZ-333 | `Given_p2が本Influence保有_HandCount5_p1current_When_p1EndTurnでp2フェーズへ_Then_p2のSDPがマイナス5` | 大き目境界、累積バグ防御 |
