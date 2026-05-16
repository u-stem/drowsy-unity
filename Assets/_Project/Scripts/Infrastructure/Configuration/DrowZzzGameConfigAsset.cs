using System;
using System.Collections.Generic;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Domain.Configuration;
using UnityEngine;

namespace Drowsy.Infrastructure.Configuration
{
    /// <summary>
    /// <see cref="IGameConfig"/> の <see cref="ScriptableObject"/> 実装(M4-PR7 で導入、ADR-0012 §3)。
    /// Designer が Unity Editor 上で <see cref="_fdpPool"/> / <see cref="_ddpPool"/> を編集することで
    /// DrowZzz のゲームバランス調整可能値を管理する。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 本 SO は CLAUDE.md §9 階層モデル L3(ゲームバランス調整可能値)に該当する 2 プロパティ
    /// <see cref="FdpPool"/> / <see cref="DdpPool"/> を <see cref="ScriptableObject"/> として
    /// Inspector 編集可能にする。L1 / L2(<see cref="DdpPoolConstants"/> / <see cref="DrowZzzClockConstants"/>
    /// / <see cref="DrowZzzVictoryConstants"/>)は本 SO の編集対象外で、各 const クラスが単一情報源
    /// (ADR-0010 §8 / §9)。
    /// </para>
    /// <para>
    /// Asset 配置:<c>Assets/_Project/Data/Configuration/DrowZzzGameConfig.asset</c> 1 個固定
    /// (ADR-0016 §7.1、M4-PR7 で実 .asset を配置)。新規 .asset 作成時は Unity が <see cref="Reset"/>
    /// を呼んで本 SO のデフォルト値(ADR-0006 §M1 の FdpPool + <see cref="DdpPoolConstants.BuildDefaultPool"/>
    /// の DdpPool)を自動投入する。
    /// </para>
    /// <para>
    /// テストは <see cref="ScriptableObject.CreateInstance{T}"/> + <c>internal SetPoolsForTest</c> で
    /// Inspector 経路を経由せず構築する(M4-PR1 で確立した <see cref="ScriptableObjectCardCatalog"/> パターン継承、
    /// <c>InternalsVisibleTo("Drowsy.Infrastructure.Tests")</c>)。
    /// </para>
    /// <para>
    /// <see cref="StubGameConfig"/>(Application.Tests 内 Stub)との関係(ADR-0012 §5):
    /// Application.Tests は Pure C# 維持のため <c>StubGameConfig</c> を継続利用、本番経路と Infrastructure.Tests は
    /// 本 SO を使用する Ports &amp; Adapters パターン。
    /// </para>
    /// <para>
    /// <b>OnEnable 不要の理由</b>(M4-PR7 code-reviewer W-4 反映):本 SO はキャッシュを持たず
    /// <see cref="_fdpPool"/> / <see cref="_ddpPool"/> の <c>SerializeField</c> がそのまま <see cref="FdpPool"/> /
    /// <see cref="DdpPool"/> プロパティに渡る構造のため、<see cref="ScriptableObjectCardCatalog"/> のような
    /// <c>OnEnable</c> による <c>RebuildCache</c> は不要。Asset ロード直後にプロパティ呼び出しすれば
    /// Serialize された値がそのまま読み取れる。
    /// </para>
    /// <para>
    /// <b>ADR-0012 §4 検証の縮退</b>(M4-PR7 code-reviewer W-3 反映):ADR-0012 §4「Designer 検証(OnValidate、初期推奨)」
    /// が挙げる 3 件のうち、本 M4-PR7 第 1 弾では「null / 空」検出(INF-078 / INF-079)のみを実装。
    /// 以下は <c>docs/todo.md</c>(M5 以降)で追跡:
    /// <list type="bullet">
    /// <item><c>_fdpPool.Length &gt;= プレイヤー数 N</c>(現状 N=2 想定)</item>
    /// <item><c>_fdpPool</c> 重複なし(<see cref="Drowsy.Application.Games.DrowZzz.StartGameUseCase"/> が重複なし抽選を要求)</item>
    /// <item><c>_ddpPool</c> の合計値が 0(対称構造の意図的崩しは LogWarning のみ)</item>
    /// </list>
    /// </para>
    /// </remarks>
    [CreateAssetMenu(menuName = "Drowsy/DrowZzz/Game Config", fileName = "DrowZzzGameConfig")]
    public sealed class DrowZzzGameConfigAsset : ScriptableObject, IGameConfig
    {
        // ADR-0006 §M1 で確定した本物 FdpPool:N=2 では 2 / 10 = 20% の組み合わせをカバーする 10 要素の不均等間隔プール
        private static readonly int[] DefaultFdpPool =
            new[] { 0, 10, 20, 30, 35, 40, 45, 50, 55, 60 };

        [SerializeField] private int[] _fdpPool;
        [SerializeField] private int[] _ddpPool;

        /// <summary>
        /// First Drowsy Point 抽選プール(ADR-0006 §1.4 / ADR-0009)。<c>null</c> や空配列なら
        /// <see cref="Array.Empty{T}"/> を返す(<see cref="OnValidate"/> で Designer に <see cref="Debug.LogError"/>
        /// 通知、本番経路 <see cref="Drowsy.Application.Games.DrowZzz.StartGameUseCase"/> は空プールを `ArgumentException` で弾く)。
        /// </summary>
        public IReadOnlyList<int> FdpPool => _fdpPool ?? Array.Empty<int>();

        /// <summary>
        /// Draw Drowsy Point 共有プール(ADR-0009 §「DDP プールの構造」)。<c>null</c> や空配列なら
        /// <see cref="Array.Empty{T}"/> を返す(同上 graceful 動作)。
        /// </summary>
        public IReadOnlyList<int> DdpPool => _ddpPool ?? Array.Empty<int>();

        /// <summary>
        /// Unity Editor で「Reset」メニュー実行時 / 新規 .asset 作成時に呼ばれる
        /// (Editor only、Build には含まれない)。本物のデフォルト値をフィールドに投入する。
        /// </summary>
        /// <remarks>
        /// Designer が手動で空にして本物に戻したい場合に Inspector の「⋮ > Reset」で復元可能。
        /// </remarks>
        private void Reset()
        {
            _fdpPool = (int[])DefaultFdpPool.Clone();
            // DdpPool は DdpPoolConstants.BuildDefaultPool() で 36 枚を整序生成
            var defaultDdp = DdpPoolConstants.BuildDefaultPool();
            _ddpPool = new int[defaultDdp.Count];
            for (int i = 0; i < defaultDdp.Count; i++)
            {
                _ddpPool[i] = defaultDdp[i];
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Inspector 編集時 / Asset ロード時に呼ばれる(Editor only)。Designer に空 / null を
        /// <see cref="Debug.LogError"/> で通知する(Build は妨げない、Editor 編集中の即時フィードバック、
        /// <see cref="ScriptableObjectCardCatalog.OnValidate"/> と同パターン)。
        /// </summary>
        /// <remarks>
        /// Infra W-3 post-Phase2 レビュー反映:OnValidate は Editor 専用ライフサイクルだが、メソッド本体は
        /// `#if UNITY_EDITOR` ガードなしでは IL2CPP / WebGL バイナリに残るため物理排除する。
        /// </remarks>
        private void OnValidate()
        {
            if (_fdpPool is null || _fdpPool.Length == 0)
            {
                Debug.LogError(
                    $"{nameof(DrowZzzGameConfigAsset)}: FdpPool が空 / null です。" +
                    "Inspector の Reset メニューでデフォルト値を復元できます。", this);
            }
            if (_ddpPool is null || _ddpPool.Length == 0)
            {
                Debug.LogError(
                    $"{nameof(DrowZzzGameConfigAsset)}: DdpPool が空 / null です。" +
                    "Inspector の Reset メニューでデフォルト値を復元できます。", this);
            }
        }
#endif

        /// <summary>
        /// テスト用に直接プールを設定する(本 ctor は <c>internal</c>、
        /// <c>InternalsVisibleTo("Drowsy.Infrastructure.Tests")</c> 経由でテストから呼び出し可能)。
        /// 本番経路では <see cref="Reset"/> + Inspector 編集を使う。
        /// </summary>
        internal void SetPoolsForTest(int[] fdpPool, int[] ddpPool)
        {
            _fdpPool = fdpPool;
            _ddpPool = ddpPool;
        }
    }
}
