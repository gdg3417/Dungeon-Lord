#if UNITY_EDITOR
using System;
using System.IO;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace DungeonBuilder.M0.EditorTools
{
    /// <summary>Explicit authoring entry point; never mutates assets on import or in a player.</summary>
    public static class ProductionDungeonAssetAuthoring
    {
        public const string DirectoryPath = "Assets/_Project/UI/ProductionDungeon/";
        [MenuItem("Dungeon Lord/Production Dungeon/Author scene assets")]
        public static void Author()
        {
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Bootstrap.unity");
            var policy = AssetDatabase.LoadAssetAtPath<DungeonPresentationPolicy>(DirectoryPath + "Presentation.asset");
            if (policy == null) { policy = ScriptableObject.CreateInstance<DungeonPresentationPolicy>(); AssetDatabase.CreateAsset(policy, DirectoryPath + "Presentation.asset"); }
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(DirectoryPath + "Panel.asset");
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>(); panel.scaleMode = PanelScaleMode.ConstantPixelSize;
                panel.scale = 1;
                AssetDatabase.CreateAsset(panel, DirectoryPath + "Panel.asset");
            }
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(DirectoryPath + "Dungeon.uxml");
            if (tree == null) throw new InvalidOperationException("production.dungeon.uxml_missing");
            AssetDatabase.SaveAssets();
            var existing = UnityEngine.Object.FindFirstObjectByType<ProductionDungeonController>();
            if (existing == null)
            {
                var obj = new GameObject("ProductionDungeon", typeof(UIDocument), typeof(ProductionDungeonController));
                existing = obj.GetComponent<ProductionDungeonController>();
            }
            existing.presentationPolicy = policy;
            var document = existing.GetComponent<UIDocument>(); document.visualTreeAsset = tree;
            document.sortingOrder = 1;
            var serializedDocument = new SerializedObject(document);
            serializedDocument.FindProperty("m_PanelSettings").objectReferenceValue = panel;
            serializedDocument.ApplyModifiedPropertiesWithoutUndo();
            document.panelSettings = panel;
            if (!EditorUtility.IsPersistent(panel))
                throw new InvalidOperationException("production.dungeon.panel_asset_not_persistent");
            if (document.panelSettings == null)
                throw new InvalidOperationException("production.dungeon.panel_missing");
            EditorUtility.SetDirty(document);
            EditorUtility.SetDirty(existing);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Bootstrap.unity");
            if (UnityEngine.Object.FindFirstObjectByType<ProductionDungeonController>().GetComponent<UIDocument>().panelSettings == null)
                throw new InvalidOperationException("production.dungeon.panel_not_saved");
        }
    }
}
#endif
