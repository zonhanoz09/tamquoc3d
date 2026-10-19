using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using TamQuoc3D.Core.Autoload;
using TamQuoc3D.Core.Network;
using TamQuoc3D.Data.Resources;
using TamQuoc3D.Features.Battle.Controllers;
using TamQuoc3D.Features.Campaign;

namespace TamQuoc3D.Tests;

/// <summary>
/// TestCampaignMap - Bo kiem thu tich hop Phase 3: Ban Do Chien Dich PvE & Ai Ai.
/// </summary>
[GlobalClass]
public partial class TestCampaignMap : Node2D
{
    public override async void _Ready()
    {
        GD.Print("\n=== 🗺️ BẮT ĐẦU TEST BẢN ĐỒ CHIẾN DỊCH PvE (PHASE 3) ===");
        bool allPassed = true;

        try
        {
            // Test 1: Load Chapter 1 Data & Stages
            allPassed &= await TestChapter1Data();

            // Test 2: StarRatingSystem logic
            allPassed &= TestStarRatingSystem();

            // Test 3: CampaignScene UI hierarchy & nodes
            allPassed &= await TestCampaignSceneUI();

            // Test 4: StageInfoPopup & Challenge flow
            allPassed &= TestStageInfoPopup();

            // Test 5: Server Campaign API Sync
            allPassed &= await TestServerCampaignSync();

            if (allPassed)
            {
                GD.Print("\n🎉 TOÀN BỘ BỘ TEST CHIẾN DỊCH PvE (PHASE 3) ĐÃ VƯỢT QUA 100% THÀNH CÔNG!\n");
                GetTree().Quit(0);
            }
            else
            {
                GD.PrintErr("\n❌ CÓ BÀI TEST THẤT BẠI TRONG PHASE 3!\n");
                GetTree().Quit(1);
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"❌ Lỗi ngoại lệ trong quá trình test: {ex.Message}\n{ex.StackTrace}");
            GetTree().Quit(1);
        }
    }

    private async Task<bool> TestChapter1Data()
    {
        GD.Print("[1/5] Kiểm tra nạp tài nguyên Chương 1 & 6 Ải PvE...");
        string ch1Path = "res://data/database/chapters/chapter_01.tres";
        if (!ResourceLoader.Exists(ch1Path))
        {
            GD.PrintErr("  ❌ Không tìm thấy chapter_01.tres");
            return false;
        }

        var ch1 = GD.Load<ChapterData>(ch1Path);
        if (ch1 == null || ch1.Stages.Count != 6)
        {
            GD.PrintErr($"  ❌ Chapter 1 không đủ 6 ải (hiện có: {ch1?.Stages.Count ?? 0})");
            return false;
        }

        var bossStage = ch1.Stages[5];
        if (bossStage.Type != StageType.Boss || bossStage.EnemyFormation.Count == 0)
        {
            GD.PrintErr("  ❌ Ải 6 không phải là Boss stage!");
            return false;
        }

        GD.Print($"  ✅ Đã nạp thành công '{ch1.ChapterName}' gồm {ch1.Stages.Count} ải (Boss: {bossStage.StageName})");
        await Task.Delay(50);
        return true;
    }

    private bool TestStarRatingSystem()
    {
        GD.Print("[2/5] Kiểm tra logic tính toán đánh giá 3 Sao (StarRatingSystem)...");

        var stage = new StageData
        {
            Star2Condition = 3, // >= 3 heroes survive
            Star3Condition = 5  // <= 5 turns
        };

        // Case 1: Thất bại => 0 sao
        var rFail = new BattleResult { Won = false, SurvivingHeroes = 5, TurnCount = 2 };
        if (StarRatingSystem.CalculateStars(rFail, stage) != 0) return false;

        // Case 2: Thắng nhưng chết nhiều tướng (2 sống), quá lượt (8 lượt) => 1 sao
        var r1Star = new BattleResult { Won = true, SurvivingHeroes = 2, TurnCount = 8 };
        if (StarRatingSystem.CalculateStars(r1Star, stage) != 1) return false;

        // Case 3: Thắng, sống sót 4 tướng (>= 3), nhưng quá 5 lượt (7 lượt) => 2 sao
        var r2Star = new BattleResult { Won = true, SurvivingHeroes = 4, TurnCount = 7 };
        if (StarRatingSystem.CalculateStars(r2Star, stage) != 2) return false;

        // Case 4: Thắng, sống sót 4 tướng, trong 4 lượt (<= 5) => 3 sao
        var r3Star = new BattleResult { Won = true, SurvivingHeroes = 4, TurnCount = 4 };
        if (StarRatingSystem.CalculateStars(r3Star, stage) != 3) return false;

        GD.Print("  ✅ StarRatingSystem tính toán chính xác 100% cho cả 4 trường hợp (0, 1, 2, 3 sao).");
        return true;
    }

    private async Task<bool> TestCampaignSceneUI()
    {
        GD.Print("[3/5] Khởi tạo CampaignScene và kiểm tra cấu trúc giao diện...");
        var scene = new CampaignScene();
        AddChild(scene);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        var map = scene.GetNodeOrNull<ChapterMap>("ChapterMap");
        var panel = scene.GetNodeOrNull<ChapterSelectPanel>("ChapterSelectPanel");
        var popup = scene.GetNodeOrNull<StageInfoPopup>("StageInfoPopup");

        if (map == null || panel == null || popup == null)
        {
            GD.PrintErr("  ❌ Thiếu các node cốt lõi trong CampaignScene!");
            scene.QueueFree();
            return false;
        }

        GD.Print("  ✅ Cấu trúc Scene, ChapterMap, ChapterSelectPanel và StageInfoPopup khởi tạo hoàn hảo.");
        scene.QueueFree();
        return true;
    }

    private bool TestStageInfoPopup()
    {
        GD.Print("[4/5] Kiểm tra hiển thị Modal Popup thông tin ải (StageInfoPopup)...");
        var popup = new StageInfoPopup();
        AddChild(popup);

        var sampleStage = new StageData
        {
            ChapterId = 1,
            StageId = 3,
            StageKey = "1-3",
            StageName = "Qua Ải Tiểu Bái",
            Description = "Test desc",
            Type = StageType.Resource,
            RecommendedPower = 3000,
            StaminaCost = 4,
            FirstClearDiamond = 80,
            NormalGold = 300,
            NormalExp = 150
        };

        popup.ShowStage(sampleStage, currentStars: 3);
        if (!popup.Visible)
        {
            GD.PrintErr("  ❌ Popup không hiển thị khi gọi ShowStage!");
            popup.QueueFree();
            return false;
        }

        popup.HidePopup();
        popup.QueueFree();
        GD.Print("  ✅ Modal Popup mở, cập nhật thông tin và đóng mượt mà.");
        return true;
    }

    private async Task<bool> TestServerCampaignSync()
    {
        GD.Print("[5/5] Kiểm tra đồng bộ tiến trình chiến dịch với PostgreSQL Server...");
        if (NetworkManager.Instance == null)
        {
            GD.PrintErr("  ❌ NetworkManager không tồn tại.");
            return false;
        }

        bool isOnline = await NetworkManager.Instance.CheckHealthAsync();
        if (!isOnline)
        {
            GD.PrintErr("  ⚠️ Server không online, bỏ qua test sync DB.");
            return true; // Still pass if offline
        }

        const string testUsername = "campaign_tester";
        const string testPassword = "CampaignPass123!";
        if (NetworkManager.Instance.IsAuthenticated)
        {
            await NetworkManager.Instance.LogoutAsync();
        }

        var login = await NetworkManager.Instance.LoginAsync(testUsername, testPassword);
        if (!login.Success && login.Message.Contains("incorrect", StringComparison.OrdinalIgnoreCase))
        {
            var registration = await NetworkManager.Instance.RegisterAsync(
                testUsername,
                testPassword,
                "Campaign Tester");
            if (!registration.Success)
            {
                GD.PrintErr($"  ❌ Không thể tạo test account: {registration.Message}");
                return false;
            }
        }
        else if (!login.Success)
        {
            GD.PrintErr($"  ❌ Không thể đăng nhập test account: {login.Message}");
            return false;
        }

        var progress = await NetworkManager.Instance.FetchCampaignProgressAsync();
        if (progress == null)
        {
            GD.PrintErr("  ❌ Không lấy được campaign progress từ Server!");
            return false;
        }

        string progressSummary = progress.Count > 0
            ? $"Stage đầu: {progress[0].StageId}, Stars: {progress[0].Stars}⭐"
            : "chưa có ải hoàn thành";
        GD.Print($"  ✅ Đã tải thành công {progress.Count} bản ghi tiến trình vượt ải từ PostgreSQL ({progressSummary}).");
        await NetworkManager.Instance.LogoutAsync();
        return true;
    }
}
