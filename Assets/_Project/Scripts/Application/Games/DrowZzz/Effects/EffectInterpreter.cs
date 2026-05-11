using System;
using System.Collections.Generic;
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
    /// </remarks>
    public sealed class EffectInterpreter
    {
        /// <summary>
        /// 与えられた <paramref name="session"/> 状態に <paramref name="effect"/> を適用した次セッションを返す。
        /// 副作用なしの純関数。
        /// </summary>
        /// <param name="session">適用前のセッション(完全状態 / オラクルビュー)</param>
        /// <param name="effect">適用する効果</param>
        /// <returns>適用後のセッション</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="session"/> または <paramref name="effect"/> が null(APP-033 / APP-034)
        /// </exception>
        /// <exception cref="NotImplementedException">
        /// <paramref name="effect"/> の派生型に対応する <c>switch</c> case が未実装(APP-035、将来効果追加用の防御)。
        /// 例外メッセージには runtime 型名が含まれる。
        /// </exception>
        public DrowZzzGameSession Apply(DrowZzzGameSession session, IEffect effect)
        {
            if (session is null)
            {
                throw new ArgumentNullException(nameof(session));
            }
            if (effect is null)
            {
                throw new ArgumentNullException(nameof(effect));
            }
            return effect switch
            {
                // M2-PR3: SDP 操作系効果(ADR-0007 §1.4 actor 拡張、ADR-0009 §「DP 種別」)
                AdjustSdpEffect adjust => ApplyAdjustSdp(session, adjust),
                // M2-PR3: 山札→手札ドロー効果(「コップ一杯の脅威」夜効果用)
                DrawCardEffect draw => ApplyDrawCard(session, draw),
                // M2-PR3: 夜・朝で効果列を切り替えるラッパー(ADR-0008 §8 JIT 確定)
                TimeOfDayBranchEffect branch => ApplyTimeOfDayBranch(session, branch),

                _ => throw new NotImplementedException(
                    $"EffectInterpreter.Apply ({effect.GetType().Name}) は本実装範囲では到達不可。将来 IEffect 派生型を追加する PR で対応する"),
            };
        }

        // TimeOfDayBranchEffect の評価: Clock.IsNight / IsMorning でどちらの効果列を Aggregate するかを決定。
        // 両者 false (Round 22+ 過渡的範囲、DZ-122 / ADR-0008 §5) の場合は no-op (session 不変返却)。
        private DrowZzzGameSession ApplyTimeOfDayBranch(DrowZzzGameSession session, TimeOfDayBranchEffect effect)
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
                current = Apply(current, inner);
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
                    $"DrawCardEffect(Opponent, _) は M2-PR3 範囲では未実装(ADR-0009 仕様カードで使用されていないため、DZ-118)");
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

        // SdpTarget を実際の PlayerId に解決する(N=2 想定、現プレイヤー以外を「Opponent」として一意決定)
        // N>2 拡張時は本メソッドの返り値型を <see cref="IReadOnlyList{T}"/> に変える等の再設計が必要(Phase 3 候補)
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
