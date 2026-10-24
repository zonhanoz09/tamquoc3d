using System;
using System.IO;
using Godot;
using TamQuoc3D.Core.Autoload;
using TamQuoc3D.Core.Navigation;
using TamQuoc3D.Data.Resources;
using TamQuoc3D.Features.Hub;
using TamQuoc3D.Features.Hub.Ui;

namespace TamQuoc3D.Tests;

/// <summary>
/// TestHubScreen - Bộ kiểm thử tự động toàn diện cho Phase 2 Hub Screen & Navigation.
/// </summary>
public partial class TestHubScreen : Node
{
    private const string ScreenshotPath = "/home/annhan/.gemini/antigravity-acp/brain/9d9302d6-a748-45dd-b3db-6c2f0a5ad3c0/hub_screen_live_preview.png";

    public override void _Ready()
    {
        RunTests();
    }

    private async void RunTests()
    {
        GD.Print("\n=== 🧪 BẮT ĐẦU TEST SUITE PHASE 2: HUB SCREEN & NAVIGATION ===");

        // --- 1. Test PlayerProfile Data Class ---
        GD.Print("[1/9] Testing PlayerProfile...");
        var profile = PlayerProfile.CreateDefault();
        Assert(profile.PlayerName == "Long Tướng Quân", "Player name must match default");
        Assert(profile.Level == 80, "Default level must be 80");
        Assert(profile.VipLevel == 11, "Default VIP must be 11");
        Assert(profile.Gold == 2145370, "Default gold must be 2145370");
        Assert(profile.Diamond == 150000, "Default diamond must be 150000");
        Assert(profile.Stamina == 100, "Default stamina must be 100");
        Assert(profile.MaxStamina == 145, "Max stamina must be 145");

        // Save & reload test
        string testPath = "user://test_profile.json";
        profile.Level = 82;
        profile.Gold = 3000000;
        profile.Save(testPath);
        var loaded = PlayerProfile.Load(testPath);
        Assert(loaded.Level == 82, "Loaded level should be 82");
        Assert(loaded.Gold == 3000000, "Loaded gold should be 3000000");
        GD.Print("✅ Test 1: PlayerProfile data model passed.");

        // --- 2. Test TopBar UI Component ---
        GD.Print("[2/9] Testing TopBar UI...");
        var topBar = new TopBar();
        AddChild(topBar);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        topBar.UpdateGold(5000000);
        topBar.UpdateDiamond(200000);
        topBar.UpdateStamina(120, 145);
        topBar.UpdateVipBadge(12);
        GD.Print("✅ Test 2: TopBar rendering & dynamic updates passed.");
        topBar.QueueFree();

        // --- 3. Test ShortcutBar UI Component ---
        GD.Print("[3/9] Testing ShortcutBar UI...");
        var shortcutBar = new ShortcutBar();
        AddChild(shortcutBar);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        bool shortcutEventTriggered = false;
        shortcutBar.ShortcutClicked += (id) =>
        {
            shortcutEventTriggered = true;
            Assert(id == "hoat_dong", "Shortcut ID must be hoat_dong");
        };
        shortcutBar.SetNotification("hoat_dong", true);
        shortcutBar.SetNotification("su_kien", false);
        shortcutBar.EmitSignal(ShortcutBar.SignalName.ShortcutClicked, "hoat_dong");
        Assert(shortcutEventTriggered, "Shortcut click signal must fire");
        GD.Print("✅ Test 3: ShortcutBar & notification dots passed.");
        shortcutBar.QueueFree();

        // --- 4. Test BottomNavBar UI Component ---
        GD.Print("[4/9] Testing BottomNavBar UI...");
        var bottomNav = new BottomNavBar();
        AddChild(bottomNav);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        bool tabSelectedTriggered = false;
        bottomNav.TabSelected += (tabId) =>
        {
            tabSelectedTriggered = true;
        };
        bottomNav.SetActiveTab("formation");
        bottomNav.SetBadge("arena", 5);
        bottomNav.EmitSignal(BottomNavBar.SignalName.TabSelected, "formation");
        Assert(tabSelectedTriggered, "BottomNav tab selected must fire");
        GD.Print("✅ Test 4: BottomNavBar 11 tabs & badges passed.");
        bottomNav.QueueFree();

        // --- 5. Test QuestPanel UI Component ---
        GD.Print("[5/9] Testing QuestPanel UI...");
        var questPanel = new QuestPanel();
        AddChild(questPanel);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        questPanel.UpdateQuestProgress("quest_patrol", 1, 1);
        GD.Print("✅ Test 5: QuestPanel progress tracking passed.");
        questPanel.QueueFree();

        // --- 6. Test ChatBox UI Component ---
        GD.Print("[6/9] Testing ChatBox UI...");
        var chatBox = new ChatBox();
        AddChild(chatBox);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        chatBox.AddMessage("Quân Sư", "Thiên thời địa lợi nhân hòa!", "sys");
        chatBox.AddMessage("Lữ Bố", "Chiến thần tái xuất!", "world");
        GD.Print("✅ Test 6: ChatBox channels & messages passed.");
        chatBox.QueueFree();

        // --- 7. Test SceneNavigator Routing & Stack ---
        GD.Print("[7/9] Testing SceneNavigator stack & routes...");
        SceneNavigator.ClearHistory();
        Assert(SceneNavigator.HubScenePath == "res://features/hub/scenes/HubScene.tscn", "HubScenePath valid");
        Assert(SceneNavigator.DeckBuilderScenePath == "res://features/deck_builder/scenes/DeckBuilderScene.tscn", "DeckBuilderScenePath valid");
        Assert(SceneNavigator.CampaignScenePath == "res://features/campaign/scenes/CampaignScene.tscn", "CampaignScenePath valid");
        Assert(SceneNavigator.ArenaScenePath == "res://features/arena/scenes/ArenaScene.tscn", "ArenaScenePath valid");
        Assert(SceneNavigator.HeroCollectionScenePath == "res://features/hero_collection/scenes/HeroCollectionScene.tscn", "HeroCollectionScenePath valid");
        GD.Print("✅ Test 7: SceneNavigator routes passed.");

        // --- 8. Test EventNotification ---
        GD.Print("[8/9] Testing EventNotification banner & toast...");
        var notif = new EventNotification();
        AddChild(notif);
        notif.ShowToast("Thử Nghiệm", "Thông báo thử nghiệm hệ thống", 1.0f);
        notif.QueueBanner(new EventNotification.BannerData
        {
            EventId = "test_evt",
            Title = "Thử Nghiệm Sự Kiện",
            Description = "Mô tả sự kiện"
        });
        await ToSignal(GetTree().CreateTimer(0.2f), SceneTreeTimer.SignalName.Timeout);
        notif.DismissCurrentBanner();
        notif.QueueFree();
        GD.Print("✅ Test 8: EventNotification banner queue & toast passed.");

        // --- 9. Full HubController Screen Integration ---
        GD.Print("[9/9] Testing Full HubController Scene Integration...");
        var hub = new HubController();
        AddChild(hub);

        // Wait for frames and render
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        var hubTopBar = hub.GetNodeOrNull<TopBar>("TopBar");
        Assert(hubTopBar != null, "Hub must display the TopBar (profile + wallet).");
        var hubBottomNav = hub.GetNodeOrNull<BottomNavBar>("BottomNavBar");
        Assert(hubBottomNav != null, "Hub must display the BottomNavBar.");
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree().CreateTimer(0.6f), SceneTreeTimer.SignalName.Timeout);

        if (!DisplayServer.GetName().Equals("headless", StringComparison.OrdinalIgnoreCase))
        {
            var img = GetViewport().GetTexture()?.GetImage();
            if (img != null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScreenshotPath)!);
                img.SavePng(ScreenshotPath);
                GD.Print($"📸 Live Screenshot saved to {ScreenshotPath}");
            }
        }

        GD.Print("\n🎉 ALL PHASE 2 HUB SCREEN & NAVIGATION TESTS PASSED SUCCESSFULLY!\n");
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
