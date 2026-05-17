# ADR-0021: EndTurnAction の全フェーズ合法化条件 — stuck 化 Marker 保有時の脱出弁

- Status: Accepted
- Date: 2026-05-17
- Decider: -

---

## Context

PR #120(ADR-0020 / No.09「強引過ぎる一手」マージ済)で「使用・放棄禁止 Marker」(`RestrictAllUsageAndAbandonInfluenceMarkerEffect`、カウント1)が追加された。これに続く PR で **No.10「安直過ぎる一手」**(乙にベッド 30% 破損 + カウント1「ドロー禁止」Marker)を導入する際、潜在的な **進行不能化(stuck)** 問題が顕在化した。

### 進行不能化の構造

`DrowZzzPhaseState` の遷移経路は以下のみ(2026-05-17 時点):

```
WaitingForDraw  --DrawCardAction-->  WaitingForPlay
WaitingForPlay  --PlayCardAction-->  WaitingForEndTurn (or WaitingForCounterResponse)
WaitingForPlay  --AbandonAction-->   WaitingForEndTurn
WaitingForEndTurn  --EndTurnAction--> 次プレイヤーの WaitingForDraw
```

`EndTurnAction` は **`WaitingForEndTurn` でのみ合法**(`DrowZzzRule.IsLegalMove` 行 107)。`AssociateAction` は割り込み式で PhaseState を変えない(自ターン中の 3 PhaseState すべてで合法だが、Hand に連想 CardId を追加するだけ)。

この設計下で以下の Marker 系 Influence が発生すると進行不能化する:

| Marker | 禁止アクション | 進行不能化するフェーズ |
| ---- | ---- | ---- |
| No.09 `RestrictAllUsageAndAbandonInfluenceMarkerEffect` | `PlayCardAction` / `CounterAction` / `AbandonAction` | `WaitingForPlay`(`PlayCardAction` / `AbandonAction` で抜けられない、`EndTurnAction` は `WaitingForEndTurn` でないため合法外)|
| No.10 `RestrictDrawCardInfluenceMarkerEffect` | `DrawCardAction` | `WaitingForDraw`(`DrawCardAction` で抜けられない)|

`AssociateAction` は許可されているが、`TotalPoints >= AssociationThreshold (= 80)` が必須かつ Hand に連想 CardId 追加するだけで PhaseState を進めないため、stuck からの脱出には使えない。

### PR #120(No.09)マージ時点の状況

PR #120 はマージ済だが、上記 stuck 化問題は code-reviewer / 実機検証では発覚しなかった(`WaitingForPlay` での EndTurn 試行テストがなかった)。本 ADR で「Marker 保有時のみ EndTurnAction を全フェーズで合法化」を確定し、No.09 / No.10 共通の脱出弁として機能させる。

### オーナー JIT 共有(2026-05-17)

> 連想はできますね確かに、なにもできないのでターンエンドボタンは必要ですね、そもそもそのボタンは常にあってよく、ただし通常はすべてのアクションが終わってから押せるようになるってすればいいですね

「常にあってよい」=「通常時は WaitingForEndTurn でのみ合法 = ボタンは UI 上で disable / 進行不能時のみ enable」。実装上は「IsLegalMove で stuck 化 Marker 保有時のみ全フェーズ合法」とすることで、UI の常時表示 + 内部の合法性ガードを両立する。

## Decision

`DrowZzzRule.IsLegalMove` の `EndTurnAction` 経路を以下に拡張する:

```csharp
EndTurnAction =>
    session.PhaseState == DrowZzzPhaseState.WaitingForEndTurn
    || HasAnyStuckCausingInfluence(session.Influences[currentPlayerId]),
```

### `HasAnyStuckCausingInfluence` ヘルパー

「stuck 化を引き起こす Marker」を 1 つでも保有していれば true を返す。新規ヘルパー定義:

```csharp
private static bool HasAnyStuckCausingInfluence(IReadOnlyList<PlayerInfluence> influences)
{
    for (int i = 0; i < influences.Count; i++)
    {
        var e = influences[i].TickEffect;
        if (e is RestrictAllUsageAndAbandonInfluenceMarkerEffect
            || e is RestrictDrawCardInfluenceMarkerEffect)
        {
            return true;
        }
    }
    return false;
}
```

### 「stuck 化を引き起こす Marker」の判定基準

PhaseState の遷移経路(`WaitingForDraw` → `WaitingForPlay` → `WaitingForEndTurn`)のいずれかを **構造的にブロックする Marker** が該当する:

| Marker | 該当 | 理由 |
| ---- | ---- | ---- |
| `RestrictAllUsageAndAbandonInfluenceMarkerEffect`(No.09) | ✓ | `WaitingForPlay` 脱出経路(PlayCardAction / AbandonAction)を両方封鎖 |
| `RestrictDrawCardInfluenceMarkerEffect`(No.10) | ✓ | `WaitingForDraw` 脱出経路(DrawCardAction)を封鎖 |
| `RestrictSpecificCardInfluenceEffect`(No.04 由来、特定 CardTypeId のみ禁止) | ✗ | 他カードで PlayCardAction 可能、stuck しない |
| `UsageRestrictionMarkerEffect`(No.00「夢」連想後制限、特定カードのみ禁止) | ✗ | 他カードで PlayCardAction 可能、stuck しない |
| `DoubleBedDamageSdpInfluenceMarkerEffect` / `InvertBedDamageSdpInfluenceMarkerEffect`(No.06 / No.08) | ✗ | 計算介入 marker、アクション禁止なし |

将来「特定 PhaseState 経路を完全にブロックする」新規 Marker が追加された場合、本 ADR §「stuck 化を引き起こす Marker」リストに追記 + `HasAnyStuckCausingInfluence` の `is` チェックを拡張する。

### `WaitingForCounterResponse` の例外扱い(code-reviewer W-3 反映 2026-05-17)

`WaitingForCounterResponse` は相手ターン中の反撃応答フェーズ(ADR-0011 §4.3.3、`PlayCardAction` 後に相手の反撃可能カード保有時に遷移)。本フェーズで `currentPlayerIndex` は **元の PlayCard 側プレイヤー**(自フェーズの主体)を指す。

stuck Marker は通常 opponent に付与されるため、`WaitingForCounterResponse` + current プレイヤーが stuck Marker 保有という状況は稀ケース(自分が No.09/No.10 を保有した状態で相手にカードを出された直後)。この場合 `IsLegalEndTurn` が `true` を返してしまうと、`ApplyEndTurn` の `PendingCounteredEffects` 強制破棄が走り、**相手の反撃機会が消失する**(ADR-0011 §4.4 で「Pending は次ターン引き継がず破棄」を保証)。

これを避けるため、`IsLegalEndTurn` は `WaitingForCounterResponse` を **明示的に除外**(`HasAnyStuckCausingInfluence` 評価前に false 返却)する。stuck 状態の解消は `WaitingForCounterResponse` 自体が解消(反撃 / PassCounter 後の `WaitingForEndTurn` 遷移)を待ってから合法化される設計。

### 通常プレイへの影響

`stuck` 化 Marker を **保有していない** 通常プレイ時の挙動は完全に不変:
- `WaitingForDraw` / `WaitingForPlay` で `EndTurnAction` は引き続き illegal(`HasAnyStuckCausingInfluence` が false のため)
- `WaitingForEndTurn` で `EndTurnAction` は引き続き legal

ゲームバランス / 既存テスト互換性は維持される。

### UI の常時表示について(オーナー JIT 共有)

「ターンエンドボタンは常にあってよく、ただし通常はすべてのアクションが終わってから押せる」というオーナー意図は、本 ADR の実装で以下のように honor される:

- **UI レイヤ**:`EndTurnButton` を常に画面に表示し、`IsLegalMove(session, new EndTurnAction())` の結果で `enable` / `disable` を切り替える
- **通常プレイ**:`WaitingForEndTurn` 到達まで disable、到達後 enable
- **stuck 化時**:`HasAnyStuckCausingInfluence` 経路で常時 enable(乙がボタン押下で脱出可能)

UI 実装は本 PR スコープ外(Presentation 層、M5 既存 UI Toolkit View の `Render` で IsLegalMove 結果を bind するパターンで自然に対応可能、別 PR で対応)。

## Consequences

### Positive

- No.09 / No.10 の stuck 化が構造的に解消(両 Marker 保有時も AssociateAction + EndTurnAction で進行可能)
- 将来同様の stuck 化 Marker(特定 PhaseState 完全ブロック型)が追加された際の標準パターンが確立
- 通常プレイ時は IsLegalMove ロジックの追加コスト 1 回(`HasAnyStuckCausingInfluence` walk、Influences が空 list なら即 false 返却で O(1))
- ADR-0020 が「count=1 Marker 機能化」で開いた新規 Marker 設計の余地を、進行不能化リスクなしで本格活用可能

### Negative

- `EndTurnAction` の合法条件が 2 値分岐(PhaseState 一致 OR Marker 保有)になり、ロジックがやや複雑化
- 新規 stuck 化 Marker 追加時に `HasAnyStuckCausingInfluence` の更新が必須(漏れ検出は code-reviewer / 統合テスト依存)
- 既存 force-play.md DZ-311(「No.09 Marker 保有時、EndTurnAction が `WaitingForEndTurn` で合法」)の意味が拡張される(「常時合法」へ)、`force-play.md` の文言更新が必要

### Neutral

- 既存テスト(N=152 件以上)の中で `EndTurnAction` を `WaitingForEndTurn` 以外で試すケースはなし(grep で確認、`WaitingForEndTurn` 以外で `EndTurnAction` を `IsLegalMove` に渡すテストは 0 件)。リグレッション影響なし
- `IGameRule<DrowZzzAction, DrowZzzGameSession>` interface 変更なし(本 ADR は IsLegalMove 内部ロジックのみ変更)
- Persistence / SO catalog / Bootstrap への影響なし

## Alternatives Considered

### 不採用案 A: 禁止アクション(DrawCardAction / PlayCardAction / AbandonAction)を **「Apply で no-op + 進行」** セマンティクスに変更

オーナー初期質問への回答案として提示した:Marker 保有時のみ IsLegalMove は true、Apply は「効果を無視して PhaseState だけ進める」no-op。

| 観点 | 評価 |
| ---- | ---- |
| カードテキスト「使用できない」「引けない」の意味 | ✗ 「できる(が効果は出ない)」になり、テキスト直訳と乖離 |
| 「カードを消費するか / 山札 1 枚減るか」の判断必要 | ✗ no-op の範囲設計が複雑化、Hand 消費 / 山札消費 / SDP 効果のどれを skip するか per-action 判断 |
| 既存 `IsLegalMove` 設計との整合 | ✗ 「IsLegalMove true 時の Apply は必ず効果を発動する」原則(ADR-0006 §3)を破る |

→ テキスト直訳優先 + 既存原則維持のため不採用。

### 不採用案 B: `SkipPhaseAction` 新規 DrowZzzAction 派生型を追加

stuck 状態でのみ合法な「進行用空アクション」を新設(`SkipDrawAction` / `SkipPlayAction` のように細分するか、汎用 `SkipPhaseAction` か)。

| 観点 | 評価 |
| ---- | ---- |
| 新規アクション数 | ✗ Action 派生型が増えると `DrowZzzAction` switch / IsLegalMove / Apply / EffectInterpreter / Persistence(将来)に波及 |
| UI レイヤとの整合 | ✗ ターンエンドボタンと別の「Skip ボタン」が必要になり UX 複雑化 |
| オーナー意図「ターンエンドボタンが必要」との整合 | ✗ オーナーは明示的に「ターンエンドボタン」を求めており、新規 Skip アクションは意図と乖離 |
| 既存 EndTurnAction との重複 | ✗ 「フェーズを終わらせる」意図は既存 EndTurnAction と同じ、概念重複 |

→ 既存 EndTurnAction の合法条件拡張で十分、Action 派生型増加コストを払う価値なし。不採用。

### 不採用案 C: 「stuck 化 Marker」検出を **ホワイトリスト** ではなく **ブラックリスト**(=「進行可能性チェック」)で行う

`IsLegalPlayCard` / `IsLegalAbandon` / `IsLegalDraw` の結果が **すべて false** なら stuck 判定、というアクション側ベースのロジック。

| 観点 | 評価 |
| ---- | ---- |
| 動的 stuck 判定の正確性 | ✓ 「実際に進行できない」状態を厳密に検出可能 |
| 計算コスト | ✗ EndTurnAction の IsLegalMove 1 回で 3 アクションの IsLegalMove を内部で評価(O(3 × Influences walk))、本 ADR の Marker walk 1 回(O(Influences walk))の 3 倍 |
| 「Hand 空 + Pile 空」のような stuck も検出してしまう | ✗ Hand が空でも EndTurnAction は通常打てる、過剰検出 |
| 設計の明示性 | ✗ 「どの Marker が stuck 化するか」が動的判定で隠れ、新規 Marker 追加時の意図確認が難しい |

→ ホワイトリスト(明示 `is` チェック)のほうが設計意図が明確で計算コストも低い、不採用。

## Related

- カード仕様: [`docs/specs/games/drowzzz/cards/easy-play.md`](../specs/games/drowzzz/cards/easy-play.md)(本 PR 同梱、No.10「安直過ぎる一手」本体実装)
- 関連カード仕様: [`docs/specs/games/drowzzz/cards/force-play.md`](../specs/games/drowzzz/cards/force-play.md)(No.09、DZ-311 の意味を本 ADR で拡張)
- ADR-0020: [`docs/adr/0020-influence-count-decrement-timing.md`](0020-influence-count-decrement-timing.md)(count=1 Marker 機能化、本 ADR の前提)
- ADR-0011 §5「順序保証」: PhaseState 遷移経路の文書化、本 ADR §1 の前提
- ADR-0006 §3「IsLegalMove 違反時の方針」: 「IsLegalMove true 時の Apply は必ず効果を発動」原則、本 ADR §不採用案 A の前提
- 実装: `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzRule.cs` `IsLegalMove`(`EndTurnAction` 経路 + `HasAnyStuckCausingInfluence` ヘルパー新設)
- EARS: 新規 No.10 EARS + force-play.md DZ-311 文言更新
