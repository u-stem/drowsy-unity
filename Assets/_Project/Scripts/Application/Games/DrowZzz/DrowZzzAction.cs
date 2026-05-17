using System;
using Drowsy.Domain.Cards;

namespace Drowsy.Application.Games.DrowZzz
{
    /// <summary>
    /// DrowZzz のアクション階層の基底。
    /// <see cref="IGameAction"/> を実装するマーカー record で、具体アクションは sealed record で派生する。
    /// 詳細は ADR-0006 §2.1 を参照。
    /// </summary>
    public abstract record DrowZzzAction : IGameAction;

    /// <summary>
    /// ゲーム開始アクション。セッション未生成状態で <c>StartGameUseCase</c> から扱う(M1-PR3 で実装)。
    /// プレイヤー Id は payload に持たず、<c>StartGameUseCase</c> の入力から決定する。
    /// </summary>
    public sealed record StartGameAction : DrowZzzAction;

    /// <summary>
    /// 山札から現プレイヤーの手札に 1 枚移動するアクション。
    /// 現プレイヤー Id は <c>session.GameState.Turn.CurrentPlayerIndex</c> から暗黙取得する。
    /// </summary>
    public sealed record DrawCardAction : DrowZzzAction;

    /// <summary>
    /// 現プレイヤーの手札から指定 <paramref name="Card"/> を場 (<c>Field</c>) に移動するアクション。
    /// <see cref="Card"/> は null 不可(生成時 / <c>with</c> 式 経由の両方で <see cref="ArgumentNullException"/> で防御)。
    /// </summary>
    /// <param name="Card">場に出すカードの識別子(現プレイヤーの手札に存在することが合法条件)</param>
    /// <param name="Choice">
    /// 選択式カード(<see cref="Drowsy.Application.Games.DrowZzz.Effects.ChoiceEffect"/> を含むカード)の選択肢 index
    /// (0-based)。非選択式カードでは無視される。M2-PR5 で導入(ADR-0007 §1.5、カード No.02「緑の侵攻」)。
    /// </param>
    /// <param name="InfluenceRemovalIndex">
    /// <see cref="Drowsy.Application.Games.DrowZzz.Effects.RemoveInfluenceEffect"/> を含む効果列の評価時に、
    /// 削除対象の影響 index(0-based)として参照される。範囲外なら no-op(graceful)。M2-PR5 で導入。
    /// </param>
    /// <remarks>
    /// M2-PR5 で <paramref name="Choice"/> / <paramref name="InfluenceRemovalIndex"/> を追加(両者 default 0 で後方互換維持、
    /// M1-PR5〜M2-PR4 の単一引数 ctor 呼び出しは全て継続動作)。
    /// </remarks>
    public sealed record PlayCardAction(CardId Card, int Choice = 0, int InfluenceRemovalIndex = 0) : DrowZzzAction
    {
        // null 防御の二重ガード:
        //  - バッキングフィールドの初期化式で positional ctor 経由の null を弾く (Card パラメータを使用、
        //    CS8907 回避: 「Parameter 'Card' is unread」警告を出さないため Card を初期化式で参照)
        //  - init setter 本体で `with { Card = null }` 経由の null を弾く (Phase 1 GameState パターン)
        // 両者とも `_card` を ArgumentNullException で防御し、生成時 / with 式の両経路をカバーする。
        private readonly CardId _card = Card ?? throw new ArgumentNullException(nameof(Card));

        /// <summary>場に出すカードの識別子。</summary>
        public CardId Card
        {
            get => _card;
            init => _card = value ?? throw new ArgumentNullException(nameof(Card));
        }
    }

    /// <summary>
    /// ターン終了アクション。<c>GameState.Turn</c> を <c>Next(playerCount)</c> で次フェーズへ進める。
    /// </summary>
    public sealed record EndTurnAction : DrowZzzAction;

    /// <summary>
    /// 放棄(代替ターン行動)アクション。<see cref="PlayCardAction"/> の代わりに `WaitingForPlay` フェーズで選択する。
    /// 手札から指定 index のカードを 1 枚捨て、<see cref="AbandonChoice"/> に応じて SDP +5 or ベッド -20% 修繕を行う。
    /// </summary>
    /// <param name="Choice">放棄選択肢(<see cref="AbandonChoice.GainSdp"/> / <see cref="AbandonChoice.RepairBed"/>)</param>
    /// <param name="CardIndex">手札から捨てる対象カードの index(0-based、default 0)。範囲外は illegal-move</param>
    /// <remarks>
    /// ADR-0011 §2 で確定した「放棄」機構の Action 派生型として M3-PR3 で導入。`PlayCardAction` の代替として
    /// `WaitingForPlay` フェーズで選択可能で、Apply 後は `WaitingForEndTurn` に遷移する(`PlayCardAction` と同じフェーズ遷移)。
    /// <para>
    /// `IsLegalMove` の合法条件(M3-PR3 で確定、ADR-0011 §2):
    /// <list type="bullet">
    /// <item><c>PhaseState == WaitingForPlay</c></item>
    /// <item>手札に 1 枚以上のカード(`Hand.Count > 0`)+ `CardIndex` が `[0, Hand.Count)` 範囲内</item>
    /// <item><see cref="AbandonChoice.RepairBed"/> 選択時:現プレイヤーの <c>BedDamages > 0%</c>(0% では修繕不可、JIT 確定 2026-05-13)</item>
    /// </list>
    /// </para>
    /// <para>
    /// 「本能(Instinct)」キーワード対応は M3-PR5 で追加予定(ADR-0011 §4):本能を持つ効果列のカードは
    /// `CardIndex` で選択できない(`IsLegalMove` で除外)。本 PR(M3-PR3)範囲では制限なし。
    /// </para>
    /// </remarks>
    public sealed record AbandonAction(AbandonChoice Choice, int CardIndex = 0) : DrowZzzAction;

    /// <summary>
    /// 連想(特殊ドロー)アクション。現プレイヤーが宣言し、指定 <paramref name="Card"/> を catalog 経由で
    /// 直接手札に追加する。通常の山札 Draw とは別の機構(ADR-0011 §1、M3-PR4 で導入)。
    /// </summary>
    /// <param name="Card">連想で手札に加える <see cref="CardId"/>(連想可能カード:効果列に
    /// <see cref="Drowsy.Application.Games.DrowZzz.Effects.AssociatableMarkerEffect"/> を含むカードのみ)</param>
    /// <remarks>
    /// ADR-0011 §1 で確定した「連想」機構の Action 派生型として M3-PR4 で導入。本 PR で JIT 確定した 3 項目を反映する
    /// (2026-05-13、ADR-0011 §1 「JIT 確認待ち」項目):
    /// <list type="bullet">
    /// <item><b>連想対象の領域</b>: (c)/(d) <c>ICardCatalog</c> から直接生成 / 別 Pile。連想可能カードは初期山札に含まれず、
    /// catalog 経由のみで手札に追加される(山札 / 捨て札 / 場 / 影響 / DP / Outcome / ベッド破損は全て不変)。</item>
    /// <item><b>FDS 境界</b>: 80 以上(80+ で発動可、<see cref="DrowZzzAssociationConstants.AssociationThreshold"/> = 80)。
    /// 「FDS」は <see cref="DrowZzzGameSession.TotalPoints"/>(= FDP + DDP + SDP)の用語規約(ADR-0011 §1 / §6)。</item>
    /// <item><b>タイミング</b>: 自ターン中のみ(<see cref="DrowZzzPhaseState.WaitingForDraw"/> /
    /// <see cref="DrowZzzPhaseState.WaitingForPlay"/> / <see cref="DrowZzzPhaseState.WaitingForEndTurn"/> のいずれかで合法)。
    /// 相手ターン中は不可(ADR-0006「自分のターン中のみカードプレイ可能」原則を破壊しない、
    /// 反撃キーワード経路は ADR-0011 §4.3 で M3-PR5 / PR6 の独立スコープ)。</item>
    /// </list>
    /// <para>
    /// 判別方式は ADR-0011 §1 起票時の「初期推奨案 (i) マーカー effect 方式」を採用:
    /// <see cref="Drowsy.Application.Games.DrowZzz.Effects.AssociatableMarkerEffect"/> を効果列に持つカードのみが
    /// <see cref="AssociateAction"/> の対象になる(<see cref="DrowZzzRule.IsLegalMove"/> でチェック)。
    /// 「就寝カード = <see cref="Drowsy.Application.Games.DrowZzz.Effects.EarlyWinTriggerEffect"/> を持つ」
    /// (ADR-0010 §5)と同パターン。
    /// </para>
    /// <para>
    /// <see cref="DrowZzzRule.Apply"/> 後は <see cref="DrowZzzPhaseState"/> は変更しない(連想は割り込み式、
    /// 現フェーズ維持)。手札にカードを 1 枚追加する以外の状態変化はない。
    /// </para>
    /// <para>
    /// 「連想で引いたカードは次の自分のターン以降使用可能」(ADR-0011 §6)という使用制限は M3-PR4 範囲では実装しない:
    /// 本 PR は「カード追加」に範囲を限定し、使用制限機構は M3-PR6(夢カード統合)で別途設計する。
    /// </para>
    /// </remarks>
    public sealed record AssociateAction(CardId Card) : DrowZzzAction
    {
        // null 防御の二重ガード:PlayCardAction.Card と同パターン(positional ctor / `with` 式 両経路カバー)
        private readonly CardId _card = Card ?? throw new ArgumentNullException(nameof(Card));

        /// <summary>連想で手札に加える <see cref="CardId"/>。</summary>
        public CardId Card
        {
            get => _card;
            init => _card = value ?? throw new ArgumentNullException(nameof(Card));
        }
    }

    /// <summary>
    /// 反撃(Counter)アクション。M3-PR5b で導入(ADR-0011 §4.3)。
    /// 相手プレイヤーが直前にプレイした <see cref="Target"/> カードに対し、本プレイヤーが <see cref="Counter"/> カード
    /// (効果列に <see cref="Drowsy.Application.Games.DrowZzz.Effects.Keyword.Counter"/> を含む)を使って反撃する。
    /// </summary>
    /// <param name="Counter">反撃のためにプレイする現プレイヤーの手札カード(Counter キーワード持ち効果列を含む)</param>
    /// <param name="Target">無効化対象の相手プレイヤーがプレイした Field 上のカード</param>
    /// <remarks>
    /// JIT 確定 2026-05-13(本 PR で確定):
    /// <list type="bullet">
    /// <item><b>効果無効化セマンティクス</b>: (C) target カードを捨て札(Discard)へ移動。プレイ済だが効果は走らず、
    /// カードは捨て札に行く。</item>
    /// <item><b>Frenzy vs Counter</b>: target カードの効果列に <see cref="Drowsy.Application.Games.DrowZzz.Effects.Keyword.Frenzy"/> を
    /// 含む KeywordedEffect がある場合、<see cref="DrowZzzRule.IsLegalMove"/> で false(illegal-move で不可)。</item>
    /// </list>
    /// <para>
    /// 合法フェーズは <see cref="DrowZzzPhaseState.WaitingForCounterResponse"/> のみ
    /// (ADR-0011 §4.3.3 で確定した「(i) WaitingForCounterResponse PhaseState 追加」採用、JIT 確定 2026-05-13)。
    /// </para>
    /// <para>
    /// 「反撃の反撃」+ 元カード遡及発動(ADR-0011 §4.4)は本 PR(M3-PR5b)範囲外、**M3-PR5c で別途実装**。
    /// 「Counter キーワード持ち効果の PlayCardAction 経路 skip」(ADR-0011 §4.3.1 自ターン通常プレイで Counter 非付与の
    /// 効果のみ発動)も本 PR 範囲外、別 PR で対応。
    /// </para>
    /// </remarks>
    public sealed record CounterAction(CardId Counter, CardId Target) : DrowZzzAction
    {
        // null 防御の二重ガード(PlayCardAction.Card / AssociateAction.Card と同パターン)
        private readonly CardId _counter = Counter ?? throw new ArgumentNullException(nameof(Counter));
        private readonly CardId _target = Target ?? throw new ArgumentNullException(nameof(Target));

        /// <summary>反撃のためにプレイする現プレイヤーの手札カード。</summary>
        public CardId Counter
        {
            get => _counter;
            init => _counter = value ?? throw new ArgumentNullException(nameof(Counter));
        }

        /// <summary>無効化対象の相手プレイヤーがプレイした Field 上のカード。</summary>
        public CardId Target
        {
            get => _target;
            init => _target = value ?? throw new ArgumentNullException(nameof(Target));
        }
    }

    /// <summary>
    /// 反撃応答スキップ(Pass)アクション。M3-PR5b で導入(ADR-0011 §4.3.3)。
    /// <see cref="DrowZzzPhaseState.WaitingForCounterResponse"/> で「反撃しない」を明示的に宣言し、
    /// <see cref="DrowZzzPhaseState.WaitingForEndTurn"/> に遷移する(相手のターン進行を継続)。
    /// </summary>
    /// <remarks>
    /// `WaitingForCounterResponse` フェーズで唯一合法な「進行 action」(<see cref="CounterAction"/> 以外)。
    /// 本 action 自体は session 状態を PhaseState 遷移以外変更しない(手札 / Field / DP / 影響 / Outcome すべて不変)。
    /// </remarks>
    public sealed record PassCounterAction : DrowZzzAction;
}
