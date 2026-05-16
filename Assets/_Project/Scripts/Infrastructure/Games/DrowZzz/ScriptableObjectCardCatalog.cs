using System;
using System.Collections.Generic;
using Drowsy.Application;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Domain.Cards;
using Drowsy.Infrastructure.Games.DrowZzz.Effects;
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
    /// <b>引数型(ADR-0018)</b>:本 catalog の lookup key は <see cref="CardTypeId"/> である。<see cref="CardEntryAsset.CardIdValue"/>
    /// が SerializeField の string を保持し、本 catalog の <see cref="RebuildCache"/> で <see cref="CardTypeId.Of"/> を
    /// 通して構築する。<see cref="CardEntryAsset.CardIdValue"/> プロパティ名は SerializeField の互換性維持のため
    /// 旧名を残しているが、保持される文字列の意味は「種別 ID(<see cref="CardTypeId.Value"/>)」である。
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
        // 共通 EmptyEffects(graceful 空列、INF-010 / INF-017 / 未登録 id の戻り値)。
        private static readonly IReadOnlyList<IEffect> EmptyEffects = Array.Empty<IEffect>();

        [SerializeField] private CardEntryAsset[] _entries;

        // 構築済キャッシュ(同じ typeId を複数回 Get したときに毎回 ToCardData / ToDomain を呼ばないため)
        // ADR-0018 で lookup key を CardId → CardTypeId に変更(catalog の責務「種別 → CardData」を型で明示)。
        private Dictionary<CardTypeId, CardData> _cache;
        // M4-PR2 で追加:CardTypeId → IEffect[] の効果列キャッシュ(INF-017)。EffectAsset[].ToDomain() を
        // RebuildCache 時に集計、GetEffects は本キャッシュから取得して返す。
        private Dictionary<CardTypeId, IReadOnlyList<IEffect>> _effectsCache;

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
        // M4-PR2: EffectAsset[].ToDomain() で _effectsCache も同時構築(INF-017〜019)。
        private void RebuildCache()
        {
            _cache = new Dictionary<CardTypeId, CardData>();
            _effectsCache = new Dictionary<CardTypeId, IReadOnlyList<IEffect>>();
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
                CardTypeId typeId;
                CardData data;
                try
                {
                    typeId = CardTypeId.Of(entry.CardIdValue);
                    data = entry.ToCardData();
                }
                catch (ArgumentException ex)
                {
                    // CardTypeId.Of / CardData ctor / ToCardData が投げる ArgumentException(派生 ArgumentNullException 含む)を catch。
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
                _cache[typeId] = data;  // 後勝ち(重複は OnValidate 側で別途警告)
                _effectsCache[typeId] = BuildEffectsFromAssets(entry.Effects, i);
            }
        }

        // EffectAsset[] → IEffect[] への変換(M4-PR2、INF-017 / INF-018 / INF-019)。
        // null 要素 / ToDomain 失敗(ArgumentException 系)は skip + Debug.LogError、他要素は影響なく処理を続ける。
        // 全要素 skip / null array の場合は EmptyEffects を返す。
        private IReadOnlyList<IEffect> BuildEffectsFromAssets(IReadOnlyList<EffectAsset> assets, int entryIndex)
        {
            if (assets is null || assets.Count == 0)
            {
                return EmptyEffects;
            }
            var list = new List<IEffect>(assets.Count);
            for (int j = 0; j < assets.Count; j++)
            {
                var asset = assets[j];
                if (asset is null)
                {
                    // SerializeReference の missing reference 復元 / 配列要素手動 null 挿入の防御(INF-018)。
                    // 意図的発火経路として EditMode テスト(INF-018 ScriptableObjectCardCatalogTests)が
                    // LogAssert.Expect で消費する(M4-PR1 学び 2「意図的発火コメント多層化」継承)。
                    Debug.LogError(
                        $"ScriptableObjectCardCatalog: entry[{entryIndex}].Effects[{j}] が null です。skip します(SerializeReference の missing reference 等、INF-018)。",
                        this);
                    continue;
                }
                try
                {
                    list.Add(asset.ToDomain());
                }
                catch (ArgumentException ex)
                {
                    // EffectAsset 派生型の ToDomain() が IEffect record ctor 防御で投げる ArgumentException
                    // (派生 ArgumentNullException / ArgumentOutOfRangeException 含む)を catch、skip + LogError(INF-019)。
                    // 例えば M4-PR3 で KeywordedEffect の Inner が null の場合に発火する可能性。
                    Debug.LogError(
                        $"ScriptableObjectCardCatalog: entry[{entryIndex}].Effects[{j}] ({asset.GetType().Name}) の ToDomain() に失敗しました。skip します: {ex.Message}",
                        this);
                }
            }
            return list.Count == 0 ? EmptyEffects : list;
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
        // M4-PR2 で _effectsCache 追加。`RebuildCache` は両 cache を必ず同時に初期化するため、どちらか一方が null なら
        // 再構築すれば十分(両者は常に同期、M4-PR2 code-reviewer W-2 反映 2026-05-13:コメントと実装の方向を整合)。
        private void EnsureCacheBuilt()
        {
            if (_cache is null || _effectsCache is null)
            {
                RebuildCache();
            }
        }

        /// <summary>
        /// 登録済(キャッシュ構築済)の全 <see cref="CardTypeId"/> を返す(ADR-0018 で <c>CardId</c> → <c>CardTypeId</c> に変更)。
        /// </summary>
        /// <remarks>
        /// M5-PR4 で追加(ADR-0016 §3.2 / §11 M5-PR4)。Bootstrap(<c>ProjectLifetimeScope</c>)が
        /// 新規対戦の <c>initialDeck</c> を組み立てるために本 catalog の登録カード種別を列挙する用途で利用する。
        /// <see cref="ICardCatalog{TEffect}"/> interface(ADR-0007 で確定)には追加せず、 SO 具象クラス専用の
        /// 拡張 API とする(interface の汎用性を保ち、列挙が必要な consumer は具象型に依存する設計)。
        /// 戻り値は <see cref="RebuildCache"/> の重複「後勝ち」/ 不正 entry skip ロジック適用後のキー集合。
        /// <para>
        /// <c>_cache.Keys</c>(<c>Dictionary.KeyCollection</c>)を直接返すと <see cref="RebuildCache"/> による
        /// <c>_cache</c> 差し替え(<c>OnEnable</c> / Asset reload)後に呼び出し側が古いライブビューを保持し続ける
        /// 危険があるため、呼び出し時点の snapshot(配列)を返す(code-reviewer W-1 反映)。
        /// </para>
        /// </remarks>
        public IReadOnlyCollection<CardTypeId> RegisteredCardTypeIds
        {
            get
            {
                EnsureCacheBuilt();
                var snapshot = new CardTypeId[_cache.Count];
                _cache.Keys.CopyTo(snapshot, 0);
                return snapshot;
            }
        }

        /// <summary>
        /// 登録済 <paramref name="typeId"/> に対応する <see cref="CardData"/> を返す。
        /// 未登録の場合は <see cref="KeyNotFoundException"/>(<see cref="ICardCatalog{TEffect}.Get"/> 契約、INF-005)。
        /// </summary>
        /// <exception cref="KeyNotFoundException"><paramref name="typeId"/> が未登録</exception>
        public CardData Get(CardTypeId typeId)
        {
            EnsureCacheBuilt();
            if (_cache.TryGetValue(typeId, out var data))
            {
                return data;
            }
            throw new KeyNotFoundException(
                $"ScriptableObjectCardCatalog に登録されていない CardTypeId: {typeId?.Value ?? "<null>"}");
        }

        /// <summary>
        /// 登録済 <paramref name="typeId"/> なら <c>true</c> を返し <paramref name="data"/> に <see cref="CardData"/> を設定する。
        /// 未登録なら <c>false</c> を返し <paramref name="data"/> には <c>null</c> を設定する(INF-007)。
        /// </summary>
        public bool TryGet(CardTypeId typeId, out CardData data)
        {
            EnsureCacheBuilt();
            return _cache.TryGetValue(typeId, out data);
        }

        /// <summary>
        /// 登録済 <paramref name="typeId"/> に対応する効果列を返す(INF-017)。M4-PR2 で本格化:
        /// <see cref="CardEntryAsset.Effects"/> に格納された <see cref="EffectAsset"/> を
        /// <see cref="EffectAsset.ToDomain"/> 経由で <see cref="IEffect"/> に変換した結果を
        /// <see cref="_effectsCache"/> から返す(<see cref="RebuildCache"/> 時に集計済)。
        /// 未登録 typeId / null typeId / 効果なしカードは空配列を返す(graceful)。
        /// </summary>
        /// <remarks>
        /// M4-PR1 では <c>InMemoryCardCatalog.GetEffects</c> と同様、空配列固定の骨格実装だった(INF-008 Optional)。
        /// M4-PR2 で <see cref="EffectAsset"/> 基底 + <see cref="AdjustSdpEffectAsset"/>(最初の派生型)を
        /// 導入し、本メソッドが SO ベースで効果列を返却する経路に拡張(ADR-0012 §3 案 (a))。
        /// 残り 11 派生型は M4-PR3 で順次対応。
        /// </remarks>
        public IReadOnlyList<IEffect> GetEffects(CardTypeId typeId)
        {
            EnsureCacheBuilt();
            if (typeId is null)
            {
                return EmptyEffects;
            }
            return _effectsCache.TryGetValue(typeId, out var effects) ? effects : EmptyEffects;
        }

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
