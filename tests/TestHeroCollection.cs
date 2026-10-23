using System;
using Godot;
using TamQuoc3D.Features.HeroCollection;

namespace TamQuoc3D.Tests;

/// <summary>
/// TestHeroCollection (Phase 3) - HeroCollectionScene phai dung luoi tuong day du
/// va panel chi tiet co nut len sao.
/// </summary>
public partial class TestHeroCollection : Node
{
    public override void _Ready()
    {
        RunTest();
    }

    private async void RunTest()
    {
        GD.Print("\n=== 🎖️ STARTING HERO COLLECTION TEST ===");
        try
        {
            var scene = new HeroCollectionScene();
            AddChild(scene);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            GridContainer grid = FindGrid(scene);
            Assert(grid != null, "GridContainer must exist");
            Assert(grid.GetChildCount() >= 15,
                $"Grid must list owned heroes, got {grid.GetChildCount()}");

            Button starBtn = FindButtonByText(scene, "LÊN SAO");
            Assert(starBtn != null, "Detail panel must have a star-up button");
            Assert(!starBtn.Disabled, "Star-up button must be enabled for a 1-star hero");

            Button filterBtn = FindButtonByText(scene, "Tiền quân");
            Assert(filterBtn != null, "Filter buttons must exist");
            filterBtn.EmitSignal(BaseButton.SignalName.Pressed);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Assert(grid.GetChildCount() >= 1 && grid.GetChildCount() < 15,
                $"Front filter must narrow the grid, got {grid.GetChildCount()}");

            GD.Print($"🎉 HERO COLLECTION TEST PASSED (cards after filter: {grid.GetChildCount()})");
            GetTree().Quit(0);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"❌ HERO COLLECTION TEST FAILED: {ex.Message}");
            GetTree().Quit(1);
        }
    }

    private static GridContainer FindGrid(Node node)
    {
        if (node is GridContainer grid) return grid;
        foreach (var child in node.GetChildren())
        {
            var found = FindGrid(child);
            if (found != null) return found;
        }
        return null;
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

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("ASSERT: " + message);
    }
}
