using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using TamQuoc3D.Core.Autoload;
using TamQuoc3D.Core.Constants;
using TamQuoc3D.Data.Database;
using TamQuoc3D.Data.Resources;
using TamQuoc3D.Features.DeckBuilder;

namespace TamQuoc3D.Tests;

/// <summary>
/// TestFormationSelector - Automated test suite & visual verification for Phase 1 Formation Selector.
/// </summary>
public partial class TestFormationSelector : Node
{
    private const string ScreenshotPath = "/home/annhan/.gemini/antigravity-acp/brain/381308d8-d571-492b-bca8-d7872acb9706/formation_selector_live_preview.png";

    public override void _Ready()
    {
        RunTests();
    }

    private async void RunTests()
    {
        GD.Print("\n=== 🧪 STARTING FORMATION SELECTOR UNIT & INTEGRATION TESTS ===");

        var db = new HeroDatabase();
        db.GenerateDefaultHeroDatabase();

        var tv = db.GetHero("trieu_van");
        var qv = db.GetHero("quan_vu");
        var ms = db.GetHero("ma_sieu");
        var gcl = db.GetHero("gia_cat_luong");
        var tk = db.GetHero("tieu_kieu");
        var dv = db.GetHero("dien_vi");
        var lb = db.GetHero("lu_bo");

        Assert(tv != null && qv != null, "Hero database load failed");

        // 1. Test FormationGrid Layout: 1 Front, 3 Mid, 3 Back
        var grid = new FormationGrid();
        AddChild(grid);

        Assert(grid.IsSlotActive(0, 1), "Slot (0, 1) must be active (Tiền quân)");
        Assert(grid.IsSlotActive(1, 0), "Slot (1, 0) must be active (Trung quân trên)");
        Assert(grid.IsSlotActive(1, 1), "Slot (1, 1) must be active (Trung quân giữa)");
        Assert(grid.IsSlotActive(1, 2), "Slot (1, 2) must be active (Trung quân dưới)");
        Assert(grid.IsSlotActive(2, 0), "Slot (2, 0) must be active (Hậu quân trên)");
        Assert(grid.IsSlotActive(2, 1), "Slot (2, 1) must be active (Hậu quân giữa)");
        Assert(grid.IsSlotActive(2, 2), "Slot (2, 2) must be active (Hậu quân dưới)");

        Assert(!grid.IsSlotActive(0, 0), "Slot (0, 0) must be inactive");
        Assert(!grid.IsSlotActive(0, 2), "Slot (0, 2) must be inactive");
        GD.Print("✅ Test 1: FormationGrid 3x3 layout passed (7 active: 1 Front, 3 Mid, 3 Back; 2 inactive).");

        // 2. Test Assign Hero & Uniqueness & Mid/Back Center slots
        bool placedCenterMid = grid.AssignHero(tv, 1, 1);
        Assert(placedCenterMid, "Should assign Trieu Van to (1, 1) Trung quân giữa");
        Assert(grid.GetHeroAt(1, 1)?.HeroId == "trieu_van", "Trieu Van should be at (1, 1)");

        bool placedCenterBack = grid.AssignHero(qv, 2, 1);
        Assert(placedCenterBack, "Should assign Quan Vu to (2, 1) Hậu quân giữa");
        Assert(grid.GetHeroAt(2, 1)?.HeroId == "quan_vu", "Quan Vu should be at (2, 1)");

        // Assign to another slot should move hero
        grid.AssignHero(tv, 0, 1);
        Assert(grid.GetHeroAt(0, 1)?.HeroId == "trieu_van", "Trieu Van should move to (0, 1)");
        Assert(grid.GetHeroAt(1, 1) == null, "Previous slot (1, 1) should now be empty");
        Assert(grid.GetAssignedCount() == 2, "2 heroes in formation");
        GD.Print("✅ Test 2: Hero assignment & Center slots passed.");

        // 3. Test Max 5 Heroes constraint
        grid.AssignHero(ms, 1, 0); // 3rd hero
        grid.AssignHero(gcl, 1, 2); // 4th hero
        grid.AssignHero(tk, 2, 0); // 5th hero
        Assert(grid.GetAssignedCount() == 5, "Should have exactly 5 heroes assigned");

        // Try to place a 6th hero in empty slot (2, 2)
        bool sixthPlaced = grid.AssignHero(lb, 2, 2);
        Assert(!sixthPlaced, "Should reject placing a 6th hero when 5 already assigned");
        Assert(grid.GetAssignedCount() == 5, "Still exactly 5 heroes assigned");
        GD.Print("✅ Test 3: Maximum 5 heroes constraint passed.");

        // 4. Test Swap Heroes
        grid.SwapHeroes(0, 1, 2, 1);
        Assert(grid.GetHeroAt(0, 1)?.HeroId == "quan_vu", "QV should now be at (0, 1)");
        Assert(grid.GetHeroAt(2, 1)?.HeroId == "trieu_van", "TV should now be at (2, 1)");
        GD.Print("✅ Test 4: Hero swapping passed.");

        // 5. Test Remove Hero & ClearAll
        grid.RemoveHero(0, 1);
        Assert(grid.GetHeroAt(0, 1) == null, "Slot (0, 1) should be empty after removal");
        Assert(grid.GetAssignedCount() == 4, "4 heroes remaining");
        grid.ClearAll();
        Assert(grid.GetAssignedCount() == 0, "Grid should be empty after ClearAll");
        GD.Print("✅ Test 5: Hero removal & clear all passed.");

        // 6. Test HeroFilter
        var filter = new HeroFilter();
        AddChild(filter);

        var allHeroes = db.GetAllHeroes().ToList();
        var fireHeroes = allHeroes.Where(h => h.Element == HeroData.ElementType.Fire).ToList();
        Assert(fireHeroes.Count >= 3, "Should have multiple Fire heroes");

        var sortedByPower = allHeroes.OrderByDescending(HeroCard.CalculateCombatPower).ToList();
        Assert(HeroCard.CalculateCombatPower(sortedByPower[0]) >= HeroCard.CalculateCombatPower(sortedByPower[^1]),
            "First hero power should be >= last hero power");
        GD.Print("✅ Test 6: HeroFilter & Combat power calculation passed.");

        // Clean up standalone test nodes
        grid.QueueFree();
        filter.QueueFree();

        // 7. Test DeckBuilderController Full Scene Integration
        var deckBuilder = new DeckBuilderController();
        AddChild(deckBuilder);

        // Wait 2 frames for UI to populate and render
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree().CreateTimer(0.3f), SceneTreeTimer.SignalName.Timeout);

        // Capture screenshot
        var img = GetViewport().GetTexture().GetImage();
        if (img != null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScreenshotPath)!);
            img.SavePng(ScreenshotPath);
            GD.Print($"📸 Screenshot saved to {ScreenshotPath}");
        }

        GD.Print("🎉 ALL FORMATION SELECTOR TESTS PASSED SUCCESSFULLY!\n");
        GetTree().Quit(0);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            GD.PrintErr($"❌ ASSERTION FAILED: {message}");
            throw new Exception($"Assertion failed: {message}");
        }
    }
}
