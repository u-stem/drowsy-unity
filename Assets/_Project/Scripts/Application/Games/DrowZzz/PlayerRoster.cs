using System;
using System.Collections.Generic;
using Drowsy.Domain.Players;

namespace Drowsy.Application.Games.DrowZzz
{
    /// <summary>
    /// DrowZzz の対戦に参加するプレイヤー Id 列を表す不変 wrapper(VContainer collection rule 回避用)。
    /// </summary>
    /// <remarks>
    /// VContainer 1.17.0 の <c>CollectionInstanceProvider.Match</c> は
    /// <c>IEnumerable&lt;&gt;</c> / <c>IReadOnlyList&lt;&gt;</c> を予約型として扱い、
    /// <c>RegisterInstance&lt;IReadOnlyList&lt;PlayerId&gt;&gt;(...)</c> による明示登録を実質的に上書きして
    /// 要素型(<see cref="PlayerId"/>)の registration を集めた空配列を返す。
    /// 本 wrapper は予約型を避けて DI に安全に乗せるための型であり、Application 層に配置することで
    /// Bootstrap / Presenter 境界で共通利用する。
    /// <para>
    /// <b>等値性</b>:<c>sealed record</c> の参照同値で十分(Bootstrap で 1 つ作って共有する利用想定、
    /// <see cref="IReadOnlyList{PlayerId}"/> プロパティの内容比較は不要)。<c>Pile</c> / <c>DdpPool</c> の
    /// 順序付きシーケンス同値とは設計目的が異なる(あちらは Domain 不変値の等値比較が必要)。
    /// </para>
    /// <para>
    /// <b>不変条件</b>:ctor で <c>null</c> と空配列を弾く(Parse-don't-validate)。
    /// <see cref="StartGameUseCase.Execute"/> 側の <c>players</c> 検証(空 / 重複 / null 要素)は引き続き有効で、
    /// 本 wrapper はそのうち最低限の null + 空のみを早期検出する責務に留める(二重検証は許容)。
    /// </para>
    /// </remarks>
    public sealed record PlayerRoster
    {
        /// <summary>プレイヤー Id 列(順序保持、空ではない)。</summary>
        /// <remarks>
        /// <c>init</c> setter は採用しない。`record` の <c>with</c> 式で
        /// <c>new PlayerRoster(...) with { Players = Array.Empty&lt;PlayerId&gt;() }</c> を許すと ctor 検証
        /// (null / empty)をバイパスできるため安全側に倒す。
        /// </remarks>
        public IReadOnlyList<PlayerId> Players { get; }

        /// <summary>
        /// プレイヤー Id 列から roster を生成する。
        /// </summary>
        /// <param name="players">プレイヤー Id 列(順序保持、1 人以上)</param>
        /// <exception cref="ArgumentNullException"><paramref name="players"/> が null</exception>
        /// <exception cref="ArgumentException"><paramref name="players"/> が空</exception>
        public PlayerRoster(IReadOnlyList<PlayerId> players)
        {
            Players = players ?? throw new ArgumentNullException(nameof(players));
            if (Players.Count == 0)
            {
                throw new ArgumentException("players は 1 人以上必要です", nameof(players));
            }
        }
    }
}
