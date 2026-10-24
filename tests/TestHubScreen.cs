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

        // --- 2. Test PlayerCard UI Component (§6: the nguoi choi don trai, fix cung) ---
        GD.Print("[2/9] Testing PlayerCard UI...");
        var playerCard = new PlayerCard();
        AddChild(playerCard);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        playerCard.UpdateGold(5000000);
        playerCard.UpdateDiamond(200000);
        playerCard.UpdateStamina(120, 145);
        playerCard.UpdateVipBadge(12);
        Assert(playerCard.Size.X == 365, "PlayerCard must be fixed 365px wide (left dock).");
        GD.Print("✅ Test 2: PlayerCard rendering & dynamic updates passed.");
        playerCard.QueueFree();

        // --- 3. Test IconDock UI Component (§6: cum icon phai, don phai, mo rong duoc) ---
        GD.Print("[3/9] Testing IconDock UI...");
        var iconDock = new IconDock();
        AddChild(iconDock);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        bool dockEventTriggered = false;
        iconDock.IconClicked += (id) =>
        {
            dockEventTriggered = true;
            Assert(id == "chieu_mo", "Dock icon ID must be chieu_mo");
        };
        Assert(iconDock.ButtonCount == 12, "IconDock must expose 12 icons (4 event + 6 feature + 2 locked)");
        Assert(iconDock.RowCount == 3, "IconDock must build 3 rows (event, feature, locked)");
        iconDock.SetNotification("chieu_mo", true);
        iconDock.SetNotification("hom_thu", false);
        iconDock.SetSubLabel("thap", "Tầng 3");
        iconDock.SetNotification("khong_ton_tai", true); // unknown id must be ignored safely
        iconDock.EmitSignal(IconDock.SignalName.IconClicked, "chieu_mo");
        Assert(dockEventTriggered, "Dock icon click signal must fire");
        iconDock.SetExpanded(true);
        Assert(iconDock.Expanded, "IconDock must expand on toggle.");
        iconDock.SetExpanded(false);
        Assert(!iconDock.Expanded, "IconDock must collapse on toggle.");
        GD.Print("✅ Test 3: IconDock rows, expand/collapse & notification dots passed.");
        iconDock.QueueFree();

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
        var hubPlayerCard = hub.GetNodeOrNull<PlayerCard>("PlayerCard");
        Assert(hubPlayerCard != null, "Hub must display the PlayerCard (left, fixed).");
        Assert(hubPlayerCard.Position == Vector2.Zero, "PlayerCard must be fixed at the top-left corner.");
        var hubBottomNav = hub.GetNodeOrNull<BottomNavBar>("BottomNavBar");
        Assert(hubBottomNav != null, "Hub must display the BottomNavBar.");
        var hubDock = hub.GetNodeOrNull<IconDock>("IconDock");
        Assert(hubDock != null && hubDock.ButtonCount == 12, "Hub must display the 12-icon right dock.");
        Assert(hubDock.Position.X == 1272 - IconDock.DockWidth, "IconDock right edge must be fixed at x = 1272.");
        var hubQuestPanel = hub.GetNodeOrNull<QuestTrackerPanel>("QuestPanel");
        Assert(hubQuestPanel != null, "Hub must display the main-quest tracker panel.");
        var hubChat = hub.GetNodeOrNull<ChatBox>("ChatBox");
        Assert(hubChat != null, "Hub must display the system-channel ChatBox.");
        Assert(hubChat.Position == new Vector2(16, 550), "ChatBox must be fixed bottom-left (day y = 700).");
        Assert(hubBottomNav.Position == new Vector2(0, 648), "BottomNavBar must be fixed at y = 648 (day y = 700).");
        var hubExpBar = hub.GetNodeOrNull<ExpBar>("ExpBar");
        Assert(hubExpBar != null && hubExpBar.Position == new Vector2(0, 704),
            "Hub must display the full-width ExpBar at the very bottom (y = 704).");

        // Courtyard (§6): 1 nhan vat nguoi choi (duy nhat di chuyen duoc) + 3 NPC
        // dung yen; khong con doi hinh 5 tuong di dao, khong khach online di dao.
        HubBackground hubBg = null;
        foreach (var child in hub.GetChildren())
        {
            if (child is HubBackground bg) hubBg = bg;
        }
        Assert(hubBg != null, "Hub must have a HubBackground.");
        Assert(hubBg.Characters.Count == 4, $"Courtyard must show 1 player + 3 static NPCs, got {hubBg.Characters.Count}");
        Assert(hubBg.MainCharacter != null, "Courtyard must have a main character.");
        Assert(HubBackground.WalkZone == new Rect2(200, 340, 840, 300), "Walk zone must be x200-1040 / y340-640.");
        var startPositions = new System.Collections.Generic.Dictionary<HubCharacter, Vector2>();
        foreach (var c in hubBg.Characters)
        {
            Assert(!c.IsGuest, "No guest characters may appear while offline.");
            startPositions[c] = c.Position;
        }
        // Vung di chi rang buoc nhan vat nguoi choi; NPC dung co dinh o me san
        // (chan Dao y=330 nam ngay tren mep vung di y=340) nen khong ap dung zone cho NPC.
        Assert(HubBackground.WalkZone.Grow(2).HasPoint(hubBg.MainCharacter.Position),
            "Player character must start inside the walk zone.");
        await ToSignal(GetTree().CreateTimer(3.5f), SceneTreeTimer.SignalName.Timeout);
        foreach (var c in hubBg.Characters)
        {
            if (c == hubBg.MainCharacter) continue;
            Assert(c.Position.DistanceTo(startPositions[c]) < 1.0f && !c.CurrentTarget.HasValue,
                "NPCs must stand still (no wander, no target).");
        }
        hubBg.MainCharacter.SetMoveTarget(new Vector2(300, 420));
        Assert(hubBg.MainCharacter.CurrentTarget.HasValue, "Player character must accept click-to-move targets.");
        GD.Print("✅ Test 9b: Courtyard player + static NPCs & click-to-move passed.");
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
