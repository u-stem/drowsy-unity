using System;
using System.Collections.Generic;
using Drowsy.Application;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Domain.Cards;
using UnityEngine;

namespace Drowsy.Infrastructure.Games.DrowZzz
{
    /// <summary>
    /// DrowZzz 向け <see cref="ICardCatalog{TEffect}"/>(<c>TEffect = IEffect</c>)の
    /// <see cref="ScriptableObject"/> 実装(M4-PR1 骨格、ADR-0012 §2)。
    /// Designer が Unity Editor 上で <see cref="CardEntryAsset"/> 配列を編集することで
    /// カードデータを管理する。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 本 PR(M4-PR1)範囲は **骨格** として以下に限定:
    /// <list type="bullet">
    /// <item>カードデータ(<see cref="CardData"/>)の登録と取得(<see cref="Get"/> / <see cref="TryGet"/>)</item>
    /// <item>重複 <see cref="CardEntryAsset.CardIdValue"/> の <see cref="OnValidate"/> 検出
    /// (<see cref="Debug.LogError(object, UnityEngine.Object)"/>、JIT 確定 2026-05-14)</item>
    /// <item><see cref="GetEffects"/> は **本 PR 範囲では空配列固定**
    /// (<see cref="Array.Empty{T}"/>)。効果列対応は M4-PR2(初の SO 化対象 <c>AdjustSdpEffect</c>)/ PR3(全 11 派生型)で拡張(ADR-0012 §3 / §9、INF-008 は Optional マーカーで本 PR トレーサビリティ免除)</item>
    /// </list>
    /// </para>
    /// <para>
    /// Asset 配置: <c>Assets/_Project/Data/Cards/</c> 配下を初期推奨(Resources フォルダ非採用で Build サイズ膨張回避、
    /// JIT 確定 2026-05-14)。本番経路の <c>AssetReference</c> / <c>Addressables</c> は M5 Bootstrap で確定。
    /// </para>
    /// <para>
    /// <see cref="InMemoryCardCatalog"/> との関係(ADR-0012 §5):Application.Tests は <see cref="InMemoryCardCatalog"/>(Pure C#、
    /// テスト独立性維持)を継続利用、Infrastructure.Tests は本 SO を <see cref="ScriptableObject.CreateInstance{T}"/> 経由で
    /// テスト、本番経路は本 SO を Asset 読み込みで使用する Ports &amp; Adapters パターン。
    /// </para>
    /// </remarks>
    [CreateAssetMenu(menuName = "Drowsy/DrowZzz/Card Catalog", fileName = "DrowZzzCardCatalog")]
    public sealed class ScriptableObjectCardCatalog : ScriptableObject, ICardCatalog<IEffect>
    {
        // M4-PR1 範囲では効果列を持たない:GetEffects は本配列を経由せず常に EmptyEffects を返す。
        // M4-PR2 / PR3 で CardEntryAsset 側に効果列フィールドを追加し、本 catalog が効果列を返却する経路に拡張する。
        private static readonly IReadOnlyList<IEffect> EmptyEffects = Array.Empty<IEffect>();

        [SerializeField] private CardEntryAsset[] _entries;

        // 構築済キャッシュ(同じ id を複数回 Get したときに毎回 ToCardData を呼ばないため)
        private Dictionary<CardId, CardData> _cache;

        /// <summary>
        /// Asset がロードされた時 / <see cref="ScriptableObject.CreateInstance{T}"/> で生成された時に呼ばれる。
        /// キャッシュを再構築する(<see cref="OnValidate"/> でも呼ばれる)。
        /// </summary>
        private void OnEnable() => RebuildCache();

        /// <summary>
        /// Inspector で <see cref="_entries"/> が編集された時に呼ばれる(Editor only)。
        /// キャッシュを再構築し、重複 <see cref="CardEntryAsset.CardIdValue"/> を <see cref="Debug.LogError"/> で報告する
        /// (Build は妨げない、Editor 編集中の即時フィードバック、JIT 確定 2026-05-14)。
        /// </summary>
        private void OnValidate()
        {
            RebuildCache();
            DetectDuplicateIds();
        }

        // 内部 cache 構築:重複 ID は「後勝ち」(InMemoryCardCatalog と同パターン)。
        // 不正 entry(null / 空 CardIdValue / 構築失敗)は skip + Debug.LogError(INF-010〜012)。
        private void RebuildCache()
        {
            _cache = new Dictionary<CardId, CardData>();
            if (_entries is null)
            {
                return;
            }
            for (int i = 0; i < _entries.Length; i++)
            {
                var entry = _entries[i];
                if (entry is null || string.IsNullOrWhiteSpace(entry.CardIdValue))
                {
                    continue;
                }
                CardId id;
                CardData data;
                try
                {
                    id = CardId.Of(entry.CardIdValue);
                    data = entry.ToCardData();
                }
                catch (ArgumentException ex)
                {
                    // CardId.Of / CardData ctor / ToCardData が投げる ArgumentException(派生 ArgumentNullException 含む)を catch。
                    // それ以外の例外(OutOfMemoryException 等)は伝播させる(M4-PR1 code-reviewer P-2 反映 2026-05-14)。
                    // 注:Designer 編集中の即時通知として Debug.LogError(Asset リンク付き)を使う(M4-PR1 JIT 確定 2026-05-14)。
                    // EditMode テスト(INF-012 `Given_不正attributes_When_Get正常id_Then_他entryは影響なし`)では
                    // 本経路を意図的に発火させ LogAssert.Expect(LogType.Error, ...) で消費する。Unity Test Framework の仕様により
                    // Console には赤色エラーログが残るが、これは「テスト成功の証拠」(M4-PR1 JIT 再確定 2026-05-13:Console 赤化は意図通り)。
                    Debug.LogError(
                        $"ScriptableObjectCardCatalog: entry[{i}] (CardIdValue='{entry.CardIdValue}') の構築に失敗しました。skip します: {ex.Message}",
                        this);
                    continue;
                }
                _cache[id] = data;  // 後勝ち(重複は OnValidate 側で別途警告)
            }
        }

        // 重複 ID の検出(Editor のみ、Build / Runtime 経路では呼ばれない)。
        // Debug.LogError は Console に Asset リンク付きで表示され、Designer が即座にジャンプ可能。
        private void DetectDuplicateIds()
        {
            if (_entries is null)
            {
                return;
            }
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _entries.Length; i++)
            {
                var entry = _entries[i];
                if (entry is null || string.IsNullOrWhiteSpace(entry.CardIdValue))
                {
                    continue;
                }
                if (!seen.Add(entry.CardIdValue))
                {
                    // 注:Designer 編集中の即時通知として Debug.LogError(Asset リンク付き)を使う(M4-PR1 JIT 確定 2026-05-14)。
                    // EditMode テスト(INF-009 `Given_重複CardIdValue_When_OnValidate_Then_DebugLogError`)では
                    // 本経路を意図的に発火させ LogAssert.Expect(LogType.Error, ...) で消費する。Unity Test Framework の仕様により
                    // Console には赤色エラーログが残るが、これは「テスト成功の証拠」(M4-PR1 JIT 再確定 2026-05-13:Console 赤化は意図通り)。
                    Debug.LogError(
                        $"ScriptableObjectCardCatalog: entry[{i}] の CardIdValue '{entry.CardIdValue}' が他 entry と重複しています(後勝ち適用)。Designer は重複を解消してください(M4-PR1 JIT 確定 2026-05-14)。",
                        this);
                }
            }
        }

        // Unity 6000.x では `ScriptableObject.CreateInstance` も `OnEnable` を呼ぶため通常 `_cache` は null にならないが、
        // Unity バージョン変更 / シリアライズ再構築の過渡的状態 / サブクラス化等の将来変化に対する保険として残す
        // (M4-PR1 code-reviewer W-4 反映 2026-05-14、過剰防御コメントを実情に即して補正)。
        private void EnsureCacheBuilt()
        {
            if (_cache is null)
            {
                RebuildCache();
            }
        }

        /// <summary>
        /// 登録済 <paramref name="id"/> に対応する <see cref="CardData"/> を返す。
        /// 未登録の場合は <see cref="KeyNotFoundException"/>(<see cref="ICardCatalog{TEffect}.Get"/> 契約、INF-005)。
        /// </summary>
        /// <exception cref="KeyNotFoundException"><paramref name="id"/> が未登録</exception>
        public CardData Get(CardId id)
        {
            EnsureCacheBuilt();
            if (_cache.TryGetValue(id, out var data))
            {
                return data;
            }
            throw new KeyNotFoundException(
                $"ScriptableObjectCardCatalog に登録されていない CardId: {id?.Value ?? "<null>"}");
        }

        /// <summary>
        /// 登録済 <paramref name="id"/> なら <c>true</c> を返し <paramref name="data"/> に <see cref="CardData"/> を設定する。
        /// 未登録なら <c>false</c> を返し <paramref name="data"/> には <c>null</c> を設定する(INF-007)。
        /// </summary>
        public bool TryGet(CardId id, out CardData data)
        {
            EnsureCacheBuilt();
            return _cache.TryGetValue(id, out data);
        }

        /// <summary>
        /// 効果列を返す。**本 PR(M4-PR1)範囲では常に空配列**(INF-008、Optional)。
        /// M4-PR2 / PR3 で <see cref="CardEntryAsset"/> に効果列フィールドを追加し、本メソッドが
        /// 効果列を返却する経路に拡張する(ADR-0012 §3 / §9)。
        /// </summary>
        public IReadOnlyList<IEffect> GetEffects(CardId id) => EmptyEffects;

        /// <summary>
        /// テスト用:<see cref="_entries"/> を直接設定し、<see cref="OnValidate"/> 相当の処理
        /// (<see cref="RebuildCache"/> + <see cref="DetectDuplicateIds"/>)を実行する。
        /// 本番経路では Unity Inspector が <see cref="SerializeField"/> private field を直接初期化するため、
        /// 本メソッドは <c>internal</c> でテスト asmdef からのみアクセス可能(<see cref="AssemblyInfo"/> の
        /// <c>InternalsVisibleTo</c>、ADR-0012 §5)。
        /// </summary>
        internal void SetEntriesForTest(CardEntryAsset[] entries)
        {
            _entries = entries;
            RebuildCache();
            DetectDuplicateIds();
        }
    }
}
