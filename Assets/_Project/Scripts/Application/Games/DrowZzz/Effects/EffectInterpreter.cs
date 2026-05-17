using System;
using System.Collections.Generic;
using Drowsy.Application.Games.DrowZzz.Influences;
using Drowsy.Domain.Game;
using Drowsy.Domain.Players;

namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// DrowZzz のカード効果(<see cref="IEffect"/> 派生型)を <see cref="DrowZzzGameSession"/> に適用する純関数を提供する。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 設計詳細は ADR-0007 §1.3 を参照。<c>switch</c> で <see cref="IEffect"/> 派生型をマッチングし、
    /// 各 case で session 遷移を返す。M2-PR1 段階では具体的な派生型は未追加(<see cref="IEffect"/> は空 marker)であり、
    /// 全ての <see cref="Apply"/> 呼び出しは <c>_</c> ケースを経由して <see cref="NotImplementedException"/> を投げる
    /// (具体 effect record の case 追加は M2-PR2 以降で 1 PR = 1 effect 種別、ADR-0007 §6)。
    /// </para>
    /// <para>
    /// 例外型の選択根拠(ADR-0007 §1.3):
    /// <list type="bullet">
    /// <item><b>実装漏れ(library 作者の switch case 追加忘れ)</b> = <see cref="NotImplementedException"/></item>
    /// <item><b>利用側の不正(state を確認せずに違法操作を要求)</b> = <see cref="InvalidOperationException"/></item>
    /// </list>
    /// 本クラスの <c>_</c> ケースは前者(library 内の case 追加漏れ)に該当するため <see cref="NotImplementedException"/>。
    /// <see cref="DrowZzzRule"/> の <c>_</c> ケースと整合させている。
    /// </para>
    /// <para>
    /// M2-PR5 で <see cref="EffectContext"/> を引数に取る 3 引数 overload を導入(ADR-0007 §1.5「継続影響」)。
    /// 既存 2 引数 overload は <see cref="EffectContext.Default"/> を渡す後方互換ラッパー。
    /// </para>
    /// </remarks>
    public sealed class EffectInterpreter
    {
        /// <summary>
        /// <see cref="Apply(DrowZzzGameSession, IEffect, EffectContext)"/> を <see cref="EffectContext.Default"/> で呼ぶ後方互換 overload。
        /// </summary>
        public DrowZzzGameSession Apply(DrowZzzGameSession session, IEffect effect)
            => Apply(session, effect, EffectContext.Default);

        /// <summary>
        /// 与えられた <paramref name="session"/> 状態に <paramref name="effect"/> を <paramref name="context"/> 下で適用した次セッションを返す。
        /// 副作用なしの純関数。
        /// </summary>
        /// <param name="session">適用前のセッション(完全状態 / オラクルビュー)</param>
        /// <param name="effect">適用する効果</param>
        /// <param name="context">プレイ時の文脈(action 由来の選択 index 等)</param>
        /// <returns>適用後のセッション</returns>
        /// <exception cref="ArgumentNullException">
        /// いずれかの引数が null
        /// </exception>
        /// <exception cref="NotImplementedException">
        /// <paramref name="effect"/> の派生型に対応する <c>switch</c> case が未実装(将来効果追加用の防御)、
        /// または <see cref="ChoiceEffect"/> 等の rule 経由でのみ unwrap される effect が直接渡された場合。
        /// </exception>
        public DrowZzzGameSession Apply(DrowZzzGameSession session, IEffect effect, EffectContext context)
        {
            if (session is null)
            {
                throw new ArgumentNullException(nameof(session));
            }
            if (effect is null)
            {
                throw new ArgumentNullException(nameof(effect));
            }
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            return effect switch
            {
                // M2-PR3: SDP 操作系効果(ADR-0007 §1.4 actor 拡張、ADR-0009 §「DP 種別」)
                AdjustSdpEffect adjust => ApplyAdjustSdp(session, adjust),
                // M2-PR3: 山札→手札ドロー効果(「コップ一杯の脅威」夜効果用)
                DrawCardEffect draw => ApplyDrawCard(session, draw),
                // M2-PR3: 夜・朝で効果列を切り替えるラッパー(ADR-0008 §8 JIT 確定)
                TimeOfDayBranchEffect branch => ApplyTimeOfDayBranch(session, branch, context),
                // M2-PR5: 継続影響(ADR-0007 §1.5)の付与 / 除去
                ApplyInfluenceEffect apply => ApplyApplyInfluence(session, apply),
                RemoveInfluenceEffect remove => ApplyRemoveInfluence(session, remove, context),
                // M3-PR1: 早期勝利トリガー(ADR-0010 §5、夜 + 持ち点 ≥ 100 で WinnerOutcome 設定)
                EarlyWinTriggerEffect _ => ApplyEarlyWinTrigger(session),
                // M3-PR2: ベッド破損率増加(ADR-0011 §3、Target プレイヤーの BedDamages を Percent 増加、上限 100% クランプ)
                DamageBedEffect damage => ApplyDamageBed(session, damage),
                // M3-PR4: 連想可能カードを示すマーカー effect(ADR-0011 §1)。判別用に効果列に置かれるだけで、
                // 評価時は session 不変返却(no-op)。連想で手札に追加する動作は AssociateAction の Apply 経路で行う。
                AssociatableMarkerEffect _ => session,
                // M3-PR6: 使用条件マーカー(ADR-0011 §6、「夢」FDS ≥ 100)。判別用に効果列に置かれるだけで、
                // 評価時は session 不変返却(no-op)。閾値チェックは DrowZzzRule.IsLegalPlayCard の最上位 scan で行う。
                RequiresMinimumTotalPointsMarkerEffect _ => session,
                // M3-PR6: 連想後使用制限マーカー(ADR-0011 §6、「夢」の使用制限機構)。2 役兼用:
                //  (1) カードの効果列内 → AssociateAction.Apply で検出 → 自プレイヤーに Influence 付与の trigger
                //  (2) PlayerInfluence.TickEffect として保有 → IsLegalPlayCard で illegal フラグ + Tick 時 no-op
                // いずれも本 case 自体は session 不変返却(no-op)。RemainingCount 減算と除去は DrowZzzRule.TickInfluences の責務。
                UsageRestrictionMarkerEffect _ => session,
                // ADR-0019 PR ②(No.04「静寂を纏う」):特定カード使用禁止マーカー。PlayerInfluence.TickEffect として保有され、
                // IsLegalPlayCard で TargetCardTypeId 一致のカードプレイを illegal 化する判別用 effect。
                // 本 case 自体は session 不変返却(no-op、UsageRestrictionMarkerEffect と完全対称)。
                RestrictSpecificCardInfluenceEffect _ => session,
                // ADR-0019 PR ②(No.04「静寂を纏う」):動的影響付与効果。context.TargetCardId.TypeId を使って
                // PlayerInfluence(OwnPhaseStart, RestrictSpecificCardInfluenceEffect(typeId), RemainingCount) を Target に付与。
                ApplyTargetedRestrictionEffect targetedRestriction => ApplyApplyTargetedRestriction(session, targetedRestriction, context),
                // 2026-05-17 No.05「喧騒を纏う」:Source プレイヤーの手札から context.TargetCardId のカードを除去し
                // 共通山札(Deck)の top に置く。次のターンの相手のドローを操作する戦術カード。
                StackHandCardOnDeckTopEffect stack => ApplyStackHandCardOnDeckTop(session, stack, context),
                // 2026-05-17 No.06「牙の届かぬ領域」:ベッド破損 SDP 変動 2 倍化 marker(PlayerInfluence.TickEffect 用)。
                // 判別用に効果列 / Tick 時に置かれるだけで session 不変返却(no-op、`UsageRestrictionMarkerEffect` と完全対称)。
                // 実 2 倍化計算は `DrowZzzRule.ApplyBedDamageToCurrentPlayer` 内で本 marker を Influence walk で検出して行う。
                DoubleBedDamageSdpInfluenceMarkerEffect _ => session,
                // 2026-05-17 No.08「廻るための知恵」:ベッド破損 SDP 符号反転 marker(PlayerInfluence.TickEffect 用)。
                // session 不変返却(no-op)。実反転計算は `DrowZzzRule.ApplyBedDamageToCurrentPlayer` 内で本 marker を
                // Influence walk で **保有数カウント** し、奇偶判定で符号反転する(オーナー JIT 確定 2026-05-17、
                // No.06 2 倍化と組み合わせ時は「反転 → 2 倍化」の順、SDP -8 → +8 → +16)。
                InvertBedDamageSdpInfluenceMarkerEffect _ => session,
                // 2026-05-17 No.07「知恵の及ばぬ領域」:Target プレイヤーの Influences から
                // InvertBedDamageSdpInfluenceMarkerEffect を TickEffect として持つ Influence を **1 件だけ** 除去。
                // 該当なしなら graceful no-op(RemoveInfluenceEffect の範囲外 no-op と同パターン)。
                RemoveInvertBedDamageInfluenceEffect remove => ApplyRemoveInvertBedDamage(session, remove),
                // M3-PR5a: キーワード能力を inner effect に付与するラッパー(ADR-0011 §4)。Keywords 自体は判別用属性で
                // 副作用を持たず、Inner effect を context 込みで再帰的に Apply するだけ。
                // Instinct は AbandonAction.IsLegalMove で利用、Frenzy / Counter は M3-PR5b 以降で機構化。
                // 注:本実装は M3-PR5a 範囲で「Counter キーワードを持つ効果が PlayCardAction 経路で評価された場合に
                // Inner を実行する」暫定挙動を取る。M3-PR5b で PlayCardAction.Apply 側で Counter 付き効果を skip する機構を
                // 追加した時点で、本 case はそのまま継続(skip 判定は呼び出し側の責務、interpreter は意味論上 Inner を Apply)。
                KeywordedEffect kw => Apply(session, kw.Inner, context),

                _ => throw new NotImplementedException(
                    $"EffectInterpreter.Apply ({effect.GetType().Name}) は本実装範囲では到達不可。" +
                    "ChoiceEffect は DrowZzzRule.ApplyPlayCard で unwrap されるため interpreter には届かない設計。" +
                    "将来 IEffect 派生型を追加する PR で対応する"),
            };
        }

        // TimeOfDayBranchEffect の評価: Clock.IsNight / IsMorning でどちらの効果列を Aggregate するかを決定。
        // 両者 false (Round 22+ 過渡的範囲、DZ-122 / ADR-0008 §5) の場合は no-op (session 不変返却)。
        // M2-PR5: context を inner effect 評価に thread する(RemoveInfluenceEffect が context 必須のため)。
        private DrowZzzGameSession ApplyTimeOfDayBranch(DrowZzzGameSession session, TimeOfDayBranchEffect effect, EffectContext context)
        {
            var clock = session.Clock;
            IReadOnlyList<IEffect> chosen;
            if (clock.IsNight)
            {
                chosen = effect.NightEffects;
            }
            else if (clock.IsMorning)
            {
                chosen = effect.MorningEffects;
            }
            else
            {
                // 夜でも朝でもない時刻(RoundNumber > 21、ADR-0008 §5 過渡的範囲)は no-op
                return session;
            }
            var current = session;
            foreach (var inner in chosen)
            {
                current = Apply(current, inner, context);
            }
            return current;
        }

        // DrawCardEffect の評価: Target=Self の現プレイヤーが山札から Count 枚を手札に引く。
        // 山札が Count より少ない場合は引ける分だけ引いて停止(DZ-117 graceful degradation、Pile.Draw() の空例外を避けるため
        // IsEmpty チェックで防御)。Target=Opponent は M2-PR3 範囲では未実装(DZ-118 で明示的に防御)。
        private static DrowZzzGameSession ApplyDrawCard(DrowZzzGameSession session, DrawCardEffect effect)
        {
            if (effect.Target == SdpTarget.Opponent)
            {
                // ADR-0007 §1.4 / DZ-118: M2-PR3 では Opponent ドローカードは未登場。将来追加時に case 拡張。
                throw new NotImplementedException(
                    "DrawCardEffect(Opponent, _) は M2-PR3 範囲では未実装(ADR-0009 仕様カードで使用されていないため、DZ-118)");
            }
            var targetId = ResolveTargetPlayerId(session, effect.Target);
            var gameState = session.GameState;
            int targetIndex = -1;
            for (int i = 0; i < gameState.Players.Count; i++)
            {
                if (gameState.Players[i].Id.Equals(targetId))
                {
                    targetIndex = i;
                    break;
                }
            }
            // ResolveTargetPlayerId が成功する以上 targetIndex は必ず見つかる(GameState の cross-field 検証で保証)。
            // 構造的に到達不可だが、将来 cross-field 検証が緩んだ場合の保険として明示防御(code-reviewer P-3 反映)。
            if (targetIndex < 0)
            {
                throw new InvalidOperationException(
                    $"内部不変条件違反: 解決された Target PlayerId {targetId?.Value} が GameState.Players に見つかりません(cross-field 検証漏れ)");
            }
            var targetPlayer = gameState.Players[targetIndex];
            var deck = gameState.Deck;
            var hand = targetPlayer.Hand;
            for (int i = 0; i < effect.Count; i++)
            {
                if (deck.IsEmpty)
                {
                    break;  // 山札空時は graceful degradation
                }
                var (drawn, remaining) = deck.Draw();
                hand = hand.Add(drawn);
                deck = remaining;
            }
            // PlayerState 配列を新しい配列に置換(現プレイヤーのみ差し替え)
            var newPlayers = new Drowsy.Domain.Players.PlayerState[gameState.Players.Count];
            for (int i = 0; i < newPlayers.Length; i++)
            {
                newPlayers[i] = i == targetIndex
                    ? targetPlayer with { Hand = hand }
                    : gameState.Players[i];
            }
            var newGameState = gameState with
            {
                Players = newPlayers,
                Deck = deck,
            };
            return session with { GameState = newGameState };
        }

        // AdjustSdpEffect の評価: Target が指す playerId の SDP を Delta だけ加減する(0 floor なし、DZ-109)
        private static DrowZzzGameSession ApplyAdjustSdp(DrowZzzGameSession session, AdjustSdpEffect effect)
        {
            var targetId = ResolveTargetPlayerId(session, effect.Target);
            var newSdp = new Dictionary<PlayerId, int>(session.SecondDrowsyPoints.Count);
            foreach (var kv in session.SecondDrowsyPoints)
            {
                newSdp[kv.Key] = kv.Key.Equals(targetId)
                    ? kv.Value + effect.Delta
                    : kv.Value;
            }
            return session with { SecondDrowsyPoints = newSdp };
        }

        // RemoveInvertBedDamageInfluenceEffect の評価: Target プレイヤーの Influences から
        // InvertBedDamageSdpInfluenceMarkerEffect を TickEffect として持つ Influence を先頭から 1 件除去。
        // 該当なしなら session 不変返却(graceful no-op、`RemoveInfluenceEffect` 範囲外 no-op と同方針)。
        // 2026-05-17 No.07「知恵の及ばぬ領域」、ADR-0019 連想由来除外パターンとは独立(本効果は Influence 削除)。
        private static DrowZzzGameSession ApplyRemoveInvertBedDamage(
            DrowZzzGameSession session,
            RemoveInvertBedDamageInfluenceEffect effect)
        {
            var targetId = ResolveTargetPlayerId(session, effect.Target);
            var newInfluences = new Dictionary<PlayerId, IReadOnlyList<Drowsy.Application.Games.DrowZzz.Influences.PlayerInfluence>>(session.Influences.Count);
            bool mutated = false;
            foreach (var kv in session.Influences)
            {
                if (!kv.Key.Equals(targetId))
                {
                    newInfluences[kv.Key] = kv.Value;
                    continue;
                }
                // Target の影響 list から InvertBedDamageSdpInfluenceMarkerEffect を TickEffect に持つ先頭 1 件を除去
                int removalIndex = -1;
                for (int i = 0; i < kv.Value.Count; i++)
                {
                    if (kv.Value[i].TickEffect is InvertBedDamageSdpInfluenceMarkerEffect)
                    {
                        removalIndex = i;
                        break;
                    }
                }
                if (removalIndex < 0)
                {
                    // 該当 marker なし、graceful no-op
                    newInfluences[kv.Key] = kv.Value;
                    continue;
                }
                // 該当 index を除去、後続要素は前にシフト
                var newList = new Drowsy.Application.Games.DrowZzz.Influences.PlayerInfluence[kv.Value.Count - 1];
                int dst = 0;
                for (int i = 0; i < kv.Value.Count; i++)
                {
                    if (i == removalIndex)
                    {
                        continue;
                    }
                    newList[dst++] = kv.Value[i];
                }
                newInfluences[kv.Key] = newList;
                mutated = true;
            }
            return mutated ? session with { Influences = newInfluences } : session;
        }

        // ApplyTargetedRestrictionEffect の評価: context.TargetCardId.TypeId を読んで Target プレイヤーに
        // PlayerInfluence(OwnPhaseStart, RestrictSpecificCardInfluenceEffect(typeId), effect.RemainingCount) を末尾追加。
        // ADR-0019 PR ②(No.04「静寂を纏う」)。context.TargetCardId が null の場合は IsLegalPlayCard で
        // 事前防御済(`ApplyTargetedRestrictionEffect` 持ちカードプレイ時に null は illegal)だが、interpreter 内でも
        // 防御的に InvalidOperationException でフェイルファスト。
        private static DrowZzzGameSession ApplyApplyTargetedRestriction(
            DrowZzzGameSession session,
            ApplyTargetedRestrictionEffect effect,
            EffectContext context)
        {
            if (context.TargetCardId is null)
            {
                throw new InvalidOperationException(
                    "ApplyTargetedRestrictionEffect 評価時に EffectContext.TargetCardId が null です。" +
                    "本 effect を含むカードプレイ時の `PlayCardAction.TargetCardId` 指定漏れ " +
                    "(IsLegalPlayCard で防御されているはずの経路)。");
            }
            var targetId = ResolveTargetPlayerId(session, effect.Target);
            var restrictionInfluence = new Drowsy.Application.Games.DrowZzz.Influences.PlayerInfluence(
                Drowsy.Application.Games.DrowZzz.Influences.InfluenceTrigger.OwnPhaseStart,
                new RestrictSpecificCardInfluenceEffect(context.TargetCardId.TypeId),
                effect.RemainingCount);
            var newInfluences = new Dictionary<PlayerId, IReadOnlyList<Drowsy.Application.Games.DrowZzz.Influences.PlayerInfluence>>(session.Influences.Count);
            foreach (var kv in session.Influences)
            {
                if (kv.Key.Equals(targetId))
                {
                    // 末尾に追加(FIFO Tick 規約、ApplyInfluenceEffect と同パターン)
                    var newList = new Drowsy.Application.Games.DrowZzz.Influences.PlayerInfluence[kv.Value.Count + 1];
                    for (int i = 0; i < kv.Value.Count; i++)
                    {
                        newList[i] = kv.Value[i];
                    }
                    newList[kv.Value.Count] = restrictionInfluence;
                    newInfluences[kv.Key] = newList;
                }
                else
                {
                    newInfluences[kv.Key] = kv.Value;
                }
            }
            return session with { Influences = newInfluences };
        }

        // StackHandCardOnDeckTopEffect の評価: context.TargetCardId を Source プレイヤーの手札から除去し、共通山札の top に置く。
        // 2026-05-17 No.05「喧騒を纏う」。context.TargetCardId が null / Source 手札に含まれない場合は IsLegalPlayCard で
        // 事前防御済(ApplyTargetedRestrictionEffect と同パターン)だが、interpreter 内でも InvalidOperationException でフェイルファスト。
        private static DrowZzzGameSession ApplyStackHandCardOnDeckTop(
            DrowZzzGameSession session,
            StackHandCardOnDeckTopEffect effect,
            EffectContext context)
        {
            if (context.TargetCardId is null)
            {
                throw new InvalidOperationException(
                    "StackHandCardOnDeckTopEffect 評価時に EffectContext.TargetCardId が null です。" +
                    "本 effect を含むカードプレイ時の `PlayCardAction.TargetCardId` 指定漏れ " +
                    "(IsLegalPlayCard で防御されているはずの経路)。");
            }
            var sourceId = ResolveTargetPlayerId(session, effect.Source);
            var gameState = session.GameState;
            int sourceIndex = -1;
            for (int i = 0; i < gameState.Players.Count; i++)
            {
                if (gameState.Players[i].Id.Equals(sourceId))
                {
                    sourceIndex = i;
                    break;
                }
            }
            if (sourceIndex < 0)
            {
                throw new InvalidOperationException(
                    $"内部不変条件違反: Source PlayerId {sourceId?.Value} が GameState.Players に見つかりません(cross-field 検証漏れ)");
            }
            var sourcePlayer = gameState.Players[sourceIndex];
            if (!sourcePlayer.Hand.Contains(context.TargetCardId))
            {
                throw new InvalidOperationException(
                    $"StackHandCardOnDeckTopEffect: Source プレイヤー {sourceId.Value} の手札に " +
                    $"TargetCardId ({context.TargetCardId.Value}) が含まれません " +
                    "(IsLegalPlayCard で防御されているはずの経路)。");
            }
            // (1) Source の Hand から TargetCardId を除去
            var updatedSource = sourcePlayer with { Hand = sourcePlayer.Hand.Remove(context.TargetCardId) };
            // (2) 共通山札の top に AddTop
            var newDeck = gameState.Deck.AddTop(context.TargetCardId);
            // Players 配列を新しい配列に置換(source プレイヤーのみ差し替え)
            var newPlayers = new PlayerState[gameState.Players.Count];
            for (int i = 0; i < newPlayers.Length; i++)
            {
                newPlayers[i] = i == sourceIndex ? updatedSource : gameState.Players[i];
            }
            var newGameState = gameState with
            {
                Players = newPlayers,
                Deck = newDeck,
            };
            return session with { GameState = newGameState };
        }

        // ApplyInfluenceEffect の評価: Target が指す playerId の影響 list 末尾に effect.Influence を追加する。
        // M2-PR5、ADR-0007 §1.5「継続影響」JIT 確定。重複付与許容(同じ影響を 2 回付与すると 2 件の独立インスタンスになる)。
        private static DrowZzzGameSession ApplyApplyInfluence(DrowZzzGameSession session, ApplyInfluenceEffect effect)
        {
            var targetId = ResolveTargetPlayerId(session, effect.Target);
            var newInfluences = new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>(session.Influences.Count);
            foreach (var kv in session.Influences)
            {
                if (kv.Key.Equals(targetId))
                {
                    // 末尾に追加(FIFO Tick 規約上、新規付与は最後尾)
                    var newList = new PlayerInfluence[kv.Value.Count + 1];
                    for (int i = 0; i < kv.Value.Count; i++)
                    {
                        newList[i] = kv.Value[i];
                    }
                    newList[kv.Value.Count] = effect.Influence;
                    newInfluences[kv.Key] = newList;
                }
                else
                {
                    newInfluences[kv.Key] = kv.Value;
                }
            }
            return session with { Influences = newInfluences };
        }

        // RemoveInfluenceEffect の評価: Target が指す playerId の影響 list から context.InfluenceRemovalIndex の影響を 1 件除去。
        // 範囲外(負値 or 件数以上)/ 空 list は no-op(graceful、RemoveInfluenceEffect.cs の xmldoc 参照)。
        private static DrowZzzGameSession ApplyRemoveInfluence(DrowZzzGameSession session, RemoveInfluenceEffect effect, EffectContext context)
        {
            var targetId = ResolveTargetPlayerId(session, effect.Target);
            int removalIndex = context.InfluenceRemovalIndex;
            var newInfluences = new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>(session.Influences.Count);
            bool mutated = false;
            foreach (var kv in session.Influences)
            {
                if (kv.Key.Equals(targetId)
                    && removalIndex >= 0
                    && removalIndex < kv.Value.Count)
                {
                    // 該当 index を除去、後続要素は前にシフト
                    var newList = new PlayerInfluence[kv.Value.Count - 1];
                    int dst = 0;
                    for (int i = 0; i < kv.Value.Count; i++)
                    {
                        if (i == removalIndex)
                        {
                            continue;
                        }
                        newList[dst++] = kv.Value[i];
                    }
                    newInfluences[kv.Key] = newList;
                    mutated = true;
                }
                else
                {
                    newInfluences[kv.Key] = kv.Value;
                }
            }
            // 変化が無い場合は session 不変返却(allocation 抑制、graceful no-op)
            return mutated ? session with { Influences = newInfluences } : session;
        }

        // EarlyWinTriggerEffect の評価: 夜の間(Clock.IsNight)+ 現プレイヤーの TotalPoints が
        // EarlyWinScoreThreshold(= 100)以上の場合、session.Outcome に WinnerOutcome(現プレイヤー)を設定。
        // いずれかの条件が満たされない場合は no-op(session 不変返却、カードプレイ自体は完了する)。
        // ADR-0010 §5、M3-PR1 で追加。
        private static DrowZzzGameSession ApplyEarlyWinTrigger(DrowZzzGameSession session)
        {
            if (!session.Clock.IsNight)
            {
                // 朝以降(Round 17〜21)/ 時計仕様外(Round 22+)では早期勝利不可、no-op
                return session;
            }
            var currentId = session.GameState.Players[session.GameState.Turn.CurrentPlayerIndex].Id;
            if (session.TotalPoints(currentId) < DrowZzzVictoryConstants.EarlyWinScoreThreshold)
            {
                // 持ち点が閾値未満、no-op
                return session;
            }
            // 条件成立: 現プレイヤーを勝者として確定
            return session with { Outcome = new WinnerOutcome(currentId) };
        }

        // DamageBedEffect の評価: Target が指す playerId の BedDamages を Percent だけ増加。
        // 上限は DrowZzzBedConstants.MaxBedDamagePercent(100%)で Math.Min クランプ。
        // ADR-0011 §3、M3-PR2 で追加。Percent は 5 の倍数 + 正値が record 側で検証済。
        private static DrowZzzGameSession ApplyDamageBed(DrowZzzGameSession session, DamageBedEffect effect)
        {
            var targetId = ResolveTargetPlayerId(session, effect.Target);
            var newBedDamages = new Dictionary<PlayerId, int>(session.BedDamages.Count);
            foreach (var kv in session.BedDamages)
            {
                newBedDamages[kv.Key] = kv.Key.Equals(targetId)
                    ? System.Math.Min(DrowZzzBedConstants.MaxBedDamagePercent, kv.Value + effect.Percent)
                    : kv.Value;
            }
            return session with { BedDamages = newBedDamages };
        }

        // SdpTarget を実際の PlayerId に解決する(N=2 想定、現プレイヤー以外を「Opponent」として一意決定)。
        // N>2 拡張時は本メソッドの返り値型を <see cref="IReadOnlyList{T}"/> に変える等の再設計が必要(Phase 3 候補)。
        private static PlayerId ResolveTargetPlayerId(DrowZzzGameSession session, SdpTarget target)
        {
            var players = session.GameState.Players;
            var currentIndex = session.GameState.Turn.CurrentPlayerIndex;
            var currentId = players[currentIndex].Id;
            if (target == SdpTarget.Self)
            {
                return currentId;
            }
            // SdpTarget.Opponent: N=2 想定で「現プレイヤー以外の唯一のプレイヤー」
            foreach (var p in players)
            {
                if (!p.Id.Equals(currentId))
                {
                    return p.Id;
                }
            }
            throw new InvalidOperationException(
                $"SdpTarget.Opponent を解決できません(N=1 と想定外、Players.Count={players.Count})");
        }
    }
}
