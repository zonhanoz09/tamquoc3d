using System;
using Godot;
using TamQuoc3D.Features.Arena;
using TamQuoc3D.Features.Gacha;
using TamQuoc3D.Features.Shop;
using TamQuoc3D.Features.Tower;

namespace TamQuoc3D.Tests;

/// <summary>
/// TestPhase4Scenes (Phase 4) - Bon scene moi (Gacha/Shop/Tower/Arena) phai khoi tao
/// duoc o che do offline ma khong loi, voi day du control chinh.
/// </summary>
public partial class TestPhase4Scenes : Node
{
    public override void _Ready()
    {
        RunTest();
    }

    private async void RunTest()
    {
        GD.Print("\n=== 🧩 STARTING PHASE 4 SCENES TEST ===");
        try
        {
            var gacha = new GachaScene();
            AddChild(gacha);
            var shop = new ShopScene();
            AddChild(shop);
            var tower = new TowerScene();
            AddChild(tower);
            var arena = new ArenaScene();
            AddChild(arena);

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            Assert(FindButtonByText(gacha, "CHIÊU MỘ x1") != null, "Gacha must have a x1 pull button");
            Assert(FindButtonByText(gacha, "CHIÊU MỘ x10") != null, "Gacha must have a x10 pull button");
            Assert(FindButtonByText(shop, "Mua —") != null, "Shop must render fallback catalog cards offline");
            Assert(FindLabelByText(shop, "TÚI ĐỒ") != null, "Shop must have an inventory section");
            Assert(FindButtonByText(tower, "Tầng 1") != null, "Tower must render floor buttons");
            Assert(FindButtonByText(arena, "NPC") != null, "Arena must have an NPC challenge button");

            GD.Print("🎉 PHASE 4 SCENES TEST PASSED");
            GetTree().Quit(0);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"❌ PHASE 4 SCENES TEST FAILED: {ex.Message}");
            GetTree().Quit(1);
        }
    }

    private static Button FindButtonByText(Node node, string contains)
    {
        if (node is Button btn && btn.Text.Contains(contains)) return btn;
        foreach (var child in node.GetChildren())
        {
            var found = FindButtonByText(child, contains);
            if (found != null) return found;
        }
        return null;
    }

    private static Label FindLabelByText(Node node, string contains)
    {
        if (node is Label label && label.Text.Contains(contains)) return label;
        foreach (var child in node.GetChildren())
        {
            var found = FindLabelByText(child, contains);
            if (found != null) return found;
        }
        return null;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("ASSERT: " + message);
    }
}
