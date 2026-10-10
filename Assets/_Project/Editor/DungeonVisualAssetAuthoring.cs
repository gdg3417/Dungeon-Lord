#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace DungeonBuilder.M0.EditorTools
{
    public static class DungeonVisualAssetAuthoring
    {
        public const string Root = "Assets/_Project/UI/ProductionDungeon/";
        [MenuItem("Dungeon Lord/Production Dungeon/Author visual assets")]
        public static void Author()
        {
            AssetDatabase.Refresh();
            foreach (var path in Directory.GetFiles(Root + "Art", "*.png"))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear; importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.isReadable = true; // Bounded qualification alpha inspection; no runtime pixel access.
                importer.maxTextureSize = 2048;
                importer.spritePixelsPerUnit = path.EndsWith("content-atlas.png") ? 512 : 128;
                importer.spriteImportMode = path.EndsWith("content-atlas.png") ? SpriteImportMode.Multiple : SpriteImportMode.Single;
                if (importer.spriteImportMode == SpriteImportMode.Multiple)
                {
                    string[] names = { "skeleton", "spike", "hoard", "entrance", "terminal", "monster-fallback" };
                    var factories=new SpriteDataProviderFactories(); factories.Init();
                    var provider=factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
                    var old=provider.GetSpriteRects();
                    var rects=names.Select((name,i)=>new SpriteRect { name=name,
                        rect=new Rect(i%3*512,i<3 ? 512 : 0,512,512),pivot=new Vector2(.5f,.5f),alignment=SpriteAlignment.Center,
                        spriteID=old.FirstOrDefault(r=>r.name==name)?.spriteID ?? GUID.Generate() }).ToArray();
                    provider.SetSpriteRects(rects);
                    provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));
                    provider.Apply();
                }
                importer.SaveAndReimport();
            }
            var catalog = AssetDatabase.LoadAssetAtPath<DungeonVisualCatalog>(Root + "Visuals.asset");
            if (catalog == null) { catalog = ScriptableObject.CreateInstance<DungeonVisualCatalog>(); AssetDatabase.CreateAsset(catalog,Root+"Visuals.asset"); }
            Sprite Surface(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Art/"+name+".png");
            var atlas = AssetDatabase.LoadAllAssetsAtPath(Root+"Art/content-atlas.png").OfType<Sprite>().ToDictionary(s=>s.name);
            catalog.RoomStone = new[] { Surface("room-stone-a"),Surface("room-stone-b"),Surface("room-stone-c") };
            catalog.Boundaries = Enumerable.Range(0,16).Select(i=>Surface("boundary-"+i)).ToArray();
            catalog.SelectedEdges = Enumerable.Range(0,16).Select(i=>Surface("selected-edge-"+i)).ToArray();
            catalog.Corridor=Surface("corridor-stone"); catalog.Surrounding=Surface("surrounding-rock");
            catalog.DoorwayThreshold=Surface("doorway-threshold"); catalog.CorridorThreshold=Surface("corridor-threshold");
            catalog.Entrance=atlas["entrance"]; catalog.Terminal=atlas["terminal"];
            catalog.Selection=Surface("selection"); catalog.Invalid=Surface("invalid"); catalog.Grid=Surface("grid"); catalog.Anchor=Surface("anchor");
            catalog.MonsterFallback=atlas["monster-fallback"]; catalog.TrapFallback=Surface("trap-fallback"); catalog.LootFallback=Surface("loot-fallback");
            catalog.Contents = new[] {
                new DungeonContentVisual { CategoryId="placement.category.monster", OptionId="placement.option.monster.skeleton",Sprite=atlas["skeleton"] },
                new DungeonContentVisual { CategoryId="placement.category.trap", OptionId="placement.option.trap.spike",Sprite=atlas["spike"] },
                new DungeonContentVisual { CategoryId="placement.category.loot_node", OptionId="placement.option.loot_node.glittering_hoard",Sprite=atlas["hoard"] } };
            if (!catalog.IsComplete || catalog.Boundaries.Any(s=>s==null)) throw new InvalidOperationException("production.dungeon.visuals_missing");
            var policy=AssetDatabase.LoadAssetAtPath<DungeonPresentationPolicy>(Root+"Presentation.asset");
            policy.Visuals=catalog; policy.EntitySize=.92f; policy.TileSize=1;
            EditorUtility.SetDirty(catalog); EditorUtility.SetDirty(policy); AssetDatabase.SaveAssets();
        }
    }
}
#endif
