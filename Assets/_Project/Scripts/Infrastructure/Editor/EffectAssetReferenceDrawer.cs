using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;
using Drowsy.Infrastructure.Games.DrowZzz.Effects;

namespace Drowsy.Infrastructure.Editor
{
    /// <summary>
    /// <see cref="EffectAsset"/> 派生型を Inspector の <c>[SerializeReference]</c> 配列要素として
    /// polymorphic に編集するための <see cref="PropertyDrawer"/>(M4-PR7 第 4 弾、Editor only)。
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unity 6 標準の <c>[SerializeReference]</c> Inspector UI は、配列要素の型選択ドロップダウンを
    /// 安定して表示せず Designer が型を指定できないことが M4-PR7 第 3 弾で実機確認された
    /// (実機スクリーンショットは `docs/screenshots/` 配下に保管、`.gitignore` 経由でリポジトリ非含有)。
    /// 本 Drawer を導入することで、配列要素の見出し行に「型」ドロップダウンを表示し、12 派生型 +
    /// wrapper / 中間型から選択できるようにする。
    /// </para>
    /// <para>
    /// <b>対象型</b>:<c>typeof(EffectAsset), useForChildren: true</c> により <see cref="EffectAsset"/> および
    /// その派生(<see cref="AdjustSdpEffectAsset"/> / <see cref="TimeOfDayBranchEffectAsset"/> /
    /// <see cref="ChoiceEffectAsset"/> / <see cref="KeywordedEffectAsset"/> 他)を 1 つの Drawer で処理。
    /// </para>
    /// <para>
    /// <b>インスタンス生成方式</b>:<see cref="FormatterServices.GetUninitializedObject"/> で
    /// ctor を呼ばずに空 instance を生成する(全派生型は <c>internal</c> ctor のため
    /// <see cref="Activator.CreateInstance"/> は引数不一致で失敗するが、Unity の SerializeField デフォルト初期化に乗る)。
    /// 本 API は .NET Standard 2.1(Unity 6 / Mono ターゲット)で利用可能な唯一の手段で、
    /// <c>RuntimeHelpers.GetUninitializedObject</c>(.NET 5+)は本環境では使えない(M4-PR7 第 4 弾 code-reviewer P-2)。
    /// </para>
    /// <para>
    /// <b>Drawer 描画範囲</b>:本 Drawer は「型ドロップダウン 1 行」+「派生型の SerializeField 子要素群」を縦に並べる。
    /// 子要素群は <see cref="EditorGUI.PropertyField"/> + <c>includeChildren: true</c> で Unity 標準の再帰描画に委譲し、
    /// 内部の wrapper effect(<c>nightEffects: EffectAsset[]</c> 等)も再帰的に本 Drawer が適用される。
    /// </para>
    /// <para>
    /// テストは Editor GUI のため自動化困難、Designer の Unity Editor 目視確認に委ねる
    /// (M4-PR7 第 4 弾完了時に `docs/screenshots/` 経由で実証)。
    /// </para>
    /// </remarks>
    [CustomPropertyDrawer(typeof(EffectAsset), useForChildren: true)]
    public sealed class EffectAssetReferenceDrawer : PropertyDrawer
    {
        // 候補型キャッシュ(初回 OnGUI で 1 回だけ AppDomain を walk)。
        // index 0 = (None / null)、index 1〜 = 各派生型
        // Domain Reload 時(コード再コンパイル後)に最新派生型を反映するため、
        // AssemblyReloadEvents.afterAssemblyReload でキャッシュを明示的にクリアする
        // (M4-PR7 第 4 弾 code-reviewer W-4 反映)
        private static Type[] _candidateTypes;
        private static GUIContent[] _displayNames;

        [InitializeOnLoadMethod]
        private static void RegisterAssemblyReloadHook()
        {
            AssemblyReloadEvents.afterAssemblyReload += InvalidateCache;
        }

        private static void InvalidateCache()
        {
            _candidateTypes = null;
            _displayNames = null;
        }

        private static void EnsureTypeCache()
        {
            if (_candidateTypes is not null)
            {
                return;
            }

            var baseType = typeof(EffectAsset);
            var types = new List<Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] assemblyTypes;
                try
                {
                    assemblyTypes = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // 一部 assembly でロード失敗した場合、ロード成功した型のみ採用
                    assemblyTypes = ex.Types.Where(t => t is not null).ToArray();
                }
                foreach (var t in assemblyTypes)
                {
                    if (!t.IsAbstract && !t.IsInterface && baseType.IsAssignableFrom(t))
                    {
                        types.Add(t);
                    }
                }
            }
            _candidateTypes = types.OrderBy(t => t.Name).ToArray();
            _displayNames = new GUIContent[_candidateTypes.Length + 1];
            _displayNames[0] = new GUIContent("(None)");
            for (int i = 0; i < _candidateTypes.Length; i++)
            {
                _displayNames[i + 1] = new GUIContent(_candidateTypes[i].Name);
            }
        }

        // GetPropertyHeight では `_candidateTypes` を参照しないため EnsureTypeCache 呼び出しは不要
        // (将来本メソッドで型キャッシュを参照するよう変更したら EnsureTypeCache を追加すること、
        //  M4-PR7 第 4 弾 code-reviewer P-3 反映)
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // 型ドロップダウン行 + (型が選択されていれば)子要素群の合計高さ
            var typeRowHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            if (string.IsNullOrEmpty(property.managedReferenceFullTypename))
            {
                return typeRowHeight;
            }
            // 子要素を手動 walk して高さを積算する(`EditorGUI.GetPropertyHeight(property, includeChildren: true)` を
            // ここで呼ぶと、property 自身が再度本 Drawer の GetPropertyHeight にディスパッチされ
            // 無限再帰のリスクがあるため、SerializedProperty.NextVisible で子要素のみ walk する、
            // M4-PR7 第 4 弾 code-reviewer W-1 反映)
            var child = property.Copy();
            var end = property.GetEndProperty();
            float childrenHeight = 0f;
            bool enterChildren = true;
            while (child.NextVisible(enterChildren) && !SerializedProperty.EqualContents(child, end))
            {
                childrenHeight += EditorGUI.GetPropertyHeight(child, includeChildren: true)
                    + EditorGUIUtility.standardVerticalSpacing;
                enterChildren = false;
            }
            return typeRowHeight + childrenHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EnsureTypeCache();

            // 1 行目:型選択ドロップダウン
            var typeRowRect = new Rect(
                position.x,
                position.y,
                position.width,
                EditorGUIUtility.singleLineHeight);

            int currentIndex = FindCurrentIndex(property);
            int newIndex = EditorGUI.Popup(typeRowRect, label, currentIndex, _displayNames);
            if (newIndex != currentIndex)
            {
                // Undo 記録 + managedReferenceValue 変更を行い、ApplyModifiedProperties は呼ばない
                // (OnGUI 内で ApplyModifiedProperties を呼ぶと Inspector の即時 Repaint が現フレームの
                //  GUI レイアウト計算と競合し InvalidOperationException / GetLastRect エラーの既知罠あり、
                //  Unity の自動 dirty 検出に委ねる、M4-PR7 第 4 弾 code-reviewer W-3 反映)
                Undo.RecordObject(property.serializedObject.targetObject, "Change EffectAsset Type");
                AssignNewInstance(property, newIndex);
                return;
            }

            // 2 行目以降:現在の派生型の子要素を手動 walk で描画
            // (GetPropertyHeight と同じ理由で property 自身を再帰描画させない、
            //  M4-PR7 第 4 弾 code-reviewer W-1 反映)
            if (!string.IsNullOrEmpty(property.managedReferenceFullTypename))
            {
                float yCursor = position.y
                    + EditorGUIUtility.singleLineHeight
                    + EditorGUIUtility.standardVerticalSpacing;
                var child = property.Copy();
                var end = property.GetEndProperty();
                bool enterChildren = true;
                while (child.NextVisible(enterChildren) && !SerializedProperty.EqualContents(child, end))
                {
                    var childHeight = EditorGUI.GetPropertyHeight(child, includeChildren: true);
                    var childRect = new Rect(position.x, yCursor, position.width, childHeight);
                    EditorGUI.PropertyField(childRect, child, includeChildren: true);
                    yCursor += childHeight + EditorGUIUtility.standardVerticalSpacing;
                    enterChildren = false;
                }
            }
        }

        private static int FindCurrentIndex(SerializedProperty property)
        {
            // managedReferenceFullTypename は "<AssemblyName> <FullTypeName>" の半角空白区切り
            var fullName = property.managedReferenceFullTypename;
            if (string.IsNullOrEmpty(fullName))
            {
                return 0;
            }
            var parts = fullName.Split(' ');
            if (parts.Length < 2)
            {
                return 0;
            }
            var typeFullName = parts[1];
            for (int i = 0; i < _candidateTypes.Length; i++)
            {
                if (_candidateTypes[i].FullName == typeFullName)
                {
                    return i + 1;
                }
            }
            return 0;
        }

        private static void AssignNewInstance(SerializedProperty property, int newIndex)
        {
            if (newIndex == 0)
            {
                property.managedReferenceValue = null;
                return;
            }
            var type = _candidateTypes[newIndex - 1];
            try
            {
                // 全派生型は internal ctor のみ + positional record 風な必須引数を持つため、
                // FormatterServices.GetUninitializedObject で ctor を呼ばずに空インスタンスを生成。
                // SerializeField デフォルト値で初期化される(Unity の SerializeReference 標準動作)
                var instance = FormatterServices.GetUninitializedObject(type);
                property.managedReferenceValue = instance;
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"{nameof(EffectAssetReferenceDrawer)}: 型 {type.FullName} のインスタンス生成に失敗しました。" +
                    $"内部例外: {ex.Message}");
            }
        }
    }
}
