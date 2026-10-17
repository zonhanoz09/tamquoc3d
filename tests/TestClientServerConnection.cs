using System;
using System.Collections.Generic;
using Godot;
using TamQuoc3D.Core.Autoload;
using TamQuoc3D.Core.Network;
using TamQuoc3D.Data.Resources;

namespace TamQuoc3D.Tests;

/// <summary>
/// TestClientServerConnection - Bộ kiểm thử tự động xác thực kết nối giữa Client Game Godot và Backend Game Server (Docker Compose).
/// </summary>
public partial class TestClientServerConnection : Node
{
    public override void _Ready()
    {
        RunTests();
    }

    private async void RunTests()
    {
        GD.Print("\n=== 🌐 BẮT ĐẦU TEST KẾT NỐI CLIENT GAME VỚI DOCKER GAME SERVER ===");

        var net = NetworkManager.Instance;
        if (net == null)
        {
            net = new NetworkManager();
            AddChild(net);
        }

        // 1. Test Healthcheck
        GD.Print("[1/6] Kiểm tra máy chủ Game Server & PostgreSQL /health...");
        bool isHealthy = await net.CheckHealthAsync();
        Assert(isHealthy, "Máy chủ Game Server phải phản hồi trạng thái healthy!");
        GD.Print("✅ Test 1: Máy chủ Game Server và PostgreSQL đang hoạt động bình thường.");

        // 2. Test Fetch Profile from PostgreSQL
        GD.Print("[2/6] Tải hồ sơ người chơi từ PostgreSQL (GET /api/profile)...");
        var profile = await net.FetchProfileAsync();
        Assert(profile != null, "Dữ liệu hồ sơ người chơi không được null");
        Assert(profile.DisplayName == "Long Tướng Quân", "Tên hiển thị phải khớp dữ liệu seed");
        Assert(profile.VipLevel == 11, "VIP level phải là 11");
        GD.Print($"✅ Test 2: Hồ sơ người chơi từ DB -> Tên: {profile.DisplayName}, Level: {profile.Level}, VIP: {profile.VipLevel}.");

        // 3. Test Fetch Wallet from PostgreSQL
        GD.Print("[3/6] Tải thông tin ví tiền tệ từ PostgreSQL (GET /api/wallet)...");
        var wallet = await net.FetchWalletAsync();
        Assert(wallet != null, "Dữ liệu ví không được null");
        Assert(wallet.Silver >= 2000000, "Bạc phải >= 2,000,000");
        Assert(wallet.Gold > 0, "KNB phải > 0");
        GD.Print($"✅ Test 3: Ví tiền tệ từ DB -> Bạc: {wallet.Silver:N0}, KNB: {wallet.Gold:N0}, Thể Lực: {wallet.Stamina}/{wallet.MaxStamina}.");

        // 4. Test Fetch & Save Formation
        GD.Print("[4/6] Kiểm tra đọc & ghi trận hình 3x3 lên PostgreSQL...");
        var formations = await net.FetchFormationsAsync();
        Assert(formations != null && formations.Count > 0, "Danh sách trận hình từ DB phải có dữ liệu");

        var testEntries = new List<FormationEntry>
        {
            new("lu_bo", 0, 1),
            new("quan_vu", 1, 0),
            new("trieu_van", 1, 2),
            new("gia_cat_luong", 2, 0),
            new("chu_du", 2, 2)
        };
        bool saveOk = await net.SaveFormationFromEntriesAsync("PVE_DEFAULT", testEntries);
        Assert(saveOk, "Gửi và lưu trận hình lên PostgreSQL phải thành công!");
        GD.Print("✅ Test 4: Đồng bộ trận hình 3x3 hai chiều Client <-> PostgreSQL thành công.");

        // 5. Test Gacha Pull through Server Engine
        GD.Print("[5/6] Thử nghiệm tính năng Triệu Hồi Gacha qua Server (POST /api/gacha/pull)...");
        var gachaRes = await net.PullGachaAsync("banner_lu_bo_limited", 2);
        Assert(gachaRes != null, "Kết quả triệu hồi không được null");
        Assert(gachaRes.Results != null && gachaRes.Results.Count == 2, "Phải nhận đủ 2 kết quả tướng");
        GD.Print($"✅ Test 5: Triệu hồi thành công {gachaRes.Results.Count} tướng: {gachaRes.Results[0].HeroId} [{gachaRes.Results[0].Rarity}], {gachaRes.Results[1].HeroId} [{gachaRes.Results[1].Rarity}]. Pity UR: {gachaRes.PityUr}/80.");

        // 6. Test Authoritative Combat Simulation
        GD.Print("[6/6] Thử nghiệm máy chủ tính toán trận đấu (POST /api/battle/simulate)...");
        var battleRes = await net.SimulateBattleAsync("ASYNC_ARENA");
        Assert(battleRes != null, "Kết quả chiến đấu không được null");
        Assert(!string.IsNullOrEmpty(battleRes.ReplayId), "Phải có mã Replay ID được lưu trong DB");
        Assert(battleRes.Seed > 0, "Phải có Seed RNG ngẫu nhiên của máy chủ");
        GD.Print($"✅ Test 6: Máy chủ mô phỏng trận đấu thành công (Replay ID: {battleRes.ReplayId}, Seed: {battleRes.Seed}, Số hiệp: {battleRes.Rounds}, Thắng: {battleRes.AttackerWin}).");

        GD.Print("\n🎉 TOÀN BỘ BỘ KIỂM THỬ KẾT NỐI CLIENT - SERVER ĐÃ VƯỢT QUA 100% THÀNH CÔNG!\n");
        GetTree().Quit(0);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            GD.PrintErr($"❌ LỖI KIỂM THỬ: {message}");
            throw new Exception($"Assertion failed: {message}");
        }
    }
}
