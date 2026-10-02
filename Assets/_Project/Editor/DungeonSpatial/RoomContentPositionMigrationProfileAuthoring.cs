#if UNITY_EDITOR
using System.IO;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using UnityEditor;
using UnityEngine;

namespace DungeonBuilder.M0.Editor.DungeonSpatial
{
    public static class RoomContentPositionMigrationProfileAuthoring
    {
        // Explicit authoring operation only. Validation and runtime loading never rewrite the asset.
        public static void RegenerateCanonicalProductionAsset()
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(
                RoomContentPositionMigrationProfiles.ProductionPath);
            if (asset == null)
                throw new FileNotFoundException("Room-content position migration profile asset is missing.",
                    RoomContentPositionMigrationProfiles.ProductionPath);
            RoomContentPositionMigrationProfilesData data =
                JsonUtility.FromJson<RoomContentPositionMigrationProfilesData>(asset.text);
            data = RoomContentPositionMigrationProfiles.WithComputedIntegrity(data);
            File.WriteAllBytes(RoomContentPositionMigrationProfiles.ProductionPath,
                RoomContentPositionMigrationProfiles.SerializeCanonical(data));
            AssetDatabase.ImportAsset(RoomContentPositionMigrationProfiles.ProductionPath,
                ImportAssetOptions.ForceUpdate);
        }
    }
}
#endif
