#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DungeonBuilder.M0.Tests
{
    public abstract partial class PhaseSevenA4ProductionSceneTests
    {
        [UnityTest] public IEnumerator CombatC2ReserveAffordabilityVisibleWithoutHover()
        {
            var root = GameRoot.Instance;
            var ui = controller.GetComponent<UIDocument>().rootVisualElement;
            var room = root.Save.validatedCanonicalSpatialState.Floors[0].Layout.Rooms[0];
            // Remove the setup defender into custody: its identity remains owned, reserve becomes zero.
            controller.EnterEdit(); controller.TapWorld(ContentCell(room, 0, 0));
            Assert.That(controller.UnassignSelectedContent(), Is.True);
            yield return null; yield return null;
            Assert.That(controller.Commit(), Is.True);
            root.Save.structureRuntime.ManaReserve = 24;
            controller.EnterEdit(); controller.TapWorld(ContentCell(room, 2, 2));
            Assert.That(controller.OpenContentCategory("placement.category.monster"), Is.True);
            Assert.That(controller.SelectNewContent("placement.option.monster.goblin"), Is.True);
            Assert.That(ui.Q<Label>("contentPlacementInfo").text, Does.Not.Contain("Enough mana"));
            controller.TapWorld(ContentCell(room, 2, 2));
            Assert.That(controller.ContentPreview.IsValid, Is.True);
            Assert.That(ui.Q<Label>("contentPlacementInfo").text, Does.Contain("Recovery reserves need funding"));
            Assert.That(ui.Q<Label>("contentPlacementInfo").text, Does.Contain("Required recovery reserve: 6 mana"));
            foreach (var size in new[] { new Vector2Int(1920,1080), new Vector2Int(720,1280) })
            {
                DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(size.x,size.y);
                yield return SettleContentPanel();
                AssertReachable(ui.Q<Button>("confirmContent")); AssertReachable(ui.Q<Button>("cancelContent"));
                yield return RoomContentCapture("combat-c2-reserve-ui-placement-"+size.x+".png");
            }
            root.Save.structureRuntime.ManaReserve = 26; yield return null; yield return null;
            Assert.That(ui.Q<Label>("contentPlacementInfo").text, Does.Contain("Enough mana"));
            root.SetOnline(false); controller.TapWorld(ContentCell(room, 2, 2));
            Assert.That(ui.Q<Label>("contentPlacementInfo").text, Does.Contain("Enough mana"));
            Assert.That(root.DungeonDraftPurchaseEligibility().Allowed, Is.False);
            root.SetOnline(true);
            Assert.That(controller.ConfirmContentPlacement(), Is.True); yield return null; yield return null;
            Assert.That(controller.Commit(), Is.True);
            Assert.That(root.Save.structureRuntime.ManaReserve, Is.EqualTo(6));
            root.Save.structureRuntime.ManaReserve = 2; yield return null; yield return null;
            foreach (var size in new[] { new Vector2Int(1920,1080), new Vector2Int(720,1280) })
            foreach (var text in new[] { DungeonTextSize.Default, DungeonTextSize.Large })
            {
                DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(size.x,size.y);
                controller.SetTextSize(text); yield return SettleContentPanel();
                var reserve = ui.Q<Label>("manaReserve");
                Assert.That(reserve.text, Does.Contain("Required recovery reserve: 6 mana").And.Contain("Recovery reserves need funding"));
                Assert.That(reserve.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
                Assert.That(reserve.worldBound.height, Is.GreaterThan(0));
                Assert.That(ui.Q("topChrome").worldBound.Contains(reserve.worldBound.center), Is.True);
                Assert.That(reserve.worldBound.xMin, Is.GreaterThanOrEqualTo(controller.SafeRoot.worldBound.xMin));
                Assert.That(reserve.worldBound.xMax, Is.LessThanOrEqualTo(controller.SafeRoot.worldBound.xMax));
                yield return RoomContentCapture("combat-c2-reserve-ui-"+size.x+"-"+text+".png");
            }
            controller.SetTextSize(DungeonTextSize.Default);
        }
    }
}
#endif
