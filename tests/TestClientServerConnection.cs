using System;
using System.Collections.Generic;
using Godot;
using TamQuoc3D.Core.Autoload;
using TamQuoc3D.Core.Network;
using TamQuoc3D.Data.Database;
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
        Assert(net != null, "NetworkManager autoload must exist");

        // 1. Test server/database health and account registration.
        GD.Print("[1/7] Kiểm tra máy chủ Game Server & PostgreSQL /health...");
        bool isHealthy = await net.CheckHealthAsync();
        Assert(isHealthy, "Máy chủ Game Server phải phản hồi trạng thái healthy!");
        Assert(net.IsDatabaseConnected, "PostgreSQL phải được xác nhận kết nối qua /health");
        GD.Print("✅ Test 1: Máy chủ Game Server và PostgreSQL đang hoạt động bình thường.");

        string testUsername = $"integration_{Guid.NewGuid():N}"[..19];
        const string testPassword = "IntegrationPass123!";
        const string testDisplayName = "Integration Tester";
        GD.Print("[2/7] Tạo tài khoản mới qua API...");
        var registration = await net.RegisterAsync(testUsername, testPassword, testDisplayName);
        if (!registration.Success)
        {
            Assert(registration.Message.Contains("already registered", StringComparison.OrdinalIgnoreCase),
                $"Registration failed unexpectedly: {registration.Message}");
            await net.LogoutAsync();
            registration = await net.LoginAsync(testUsername, testPassword);
        }
        Assert(registration.Success, $"Registration/login failed: {registration.Message}");

        // 3. Test authenticated profile and wallet reads.
        GD.Print("[3/7] Tải hồ sơ và ví tài khoản đã xác thực từ PostgreSQL...");
        var profile = await net.FetchProfileAsync();
        Assert(profile != null, "Dữ liệu hồ sơ người chơi không được null");
        Assert(profile.DisplayName == testDisplayName, "Tên hiển thị phải khớp tài khoản vừa đăng ký");
        Assert(profile.Level == 1, "Tài khoản mới phải bắt đầu ở cấp 1");
        Assert(profile.AvatarId == "default_hero", "Tướng mặc định phải được dùng làm avatar ban đầu");
        GD.Print($"✅ Test 3: Đọc profile của {profile.DisplayName} qua session token.");

        var wallet = await net.FetchWalletAsync();
        Assert(wallet != null, "Dữ liệu ví không được null");
        Assert(wallet.Silver == 5000 && wallet.Gold == 1000, "Tài khoản mới phải nhận đúng ví khởi đầu");
        GD.Print($"✅ Test ví: Bạc {wallet.Silver:N0}, KNB {wallet.Gold:N0}, thể lực {wallet.Stamina}/{wallet.MaxStamina}.");
        var heroes = await net.FetchHeroesAsync();
        Assert(heroes != null && heroes.Count == 1, "Tài khoản mới chỉ được sở hữu một tướng khởi đầu");
        Assert(heroes[0].HeroId == "default_hero" && heroes[0].IsCommander,
            "Tướng mặc định phải là tướng duy nhất và được đánh dấu chủ tướng");
        Assert(heroes[0].HeroName == testDisplayName,
            "Tên tướng mặc định phải khớp tên hiển thị của tài khoản");
        Assert(heroes[0].Skills != null && heroes[0].Skills.Count == 0,
            "Tướng mặc định phải khởi tạo không có skill");
        var defaultHero = HeroDatabase.Instance?.GetHero("default_hero");
        Assert(defaultHero != null && defaultHero.Skills.Count == 0 &&
               defaultHero.BaseStats.HpMax == 1000 && defaultHero.BaseStats.AtkPhys == 100,
            "Resource tướng mặc định phải có chỉ số cơ bản và không khai báo skill");
        Assert(GD.Load<Texture2D>("res://assets/heroes/default_hero/portrait.png") != null &&
               GD.Load<Texture2D>("res://assets/heroes/default_hero/icon.png") != null &&
               GD.Load<SpriteFrames>("res://assets/heroes/default_hero/sprite_frames.tres") != null,
            "Asset portrait, icon và animation của tướng mặc định phải được nạp");

        // 4. Verify authenticated database data is applied to the game state.
        GD.Print("[4/7] Đồng bộ dữ liệu tài khoản đã đăng nhập vào GameManager...");
        bool syncOk = await net.SyncPlayerDataAsync();
        Assert(syncOk, "Đồng bộ profile, wallet và formations phải thành công");
        Assert(GameManager.Instance.Profile.PlayerId == profile.Id, "Player ID trong game phải lấy từ PostgreSQL");
        Assert(GameManager.Instance.Gold == wallet.Silver, "Vàng hiển thị phải ánh xạ từ cột silver");
        Assert(GameManager.Instance.Gems == wallet.Gold, "KNB hiển thị phải ánh xạ từ cột gold");
        Assert(GameManager.Instance.Stamina == wallet.Stamina, "Thể lực trong game phải lấy từ PostgreSQL");
        Assert(GameManager.Instance.OwnedHeroes.Count == heroes.Count, "Danh sách tướng trong game phải khớp PostgreSQL");
        Assert(GameManager.Instance.Profile.OwnedHeroIds.Count == 1 &&
               GameManager.Instance.Profile.OwnedHeroIds[0] == "default_hero",
            "Hồ sơ client phải đồng bộ đúng tướng mặc định duy nhất");
        Assert(GameManager.Instance.OwnedHeroes.Count == 1 &&
               GameManager.Instance.OwnedHeroes[0].HeroId == "default_hero" &&
               GameManager.Instance.OwnedHeroes[0].HeroName == testDisplayName &&
               GameManager.Instance.OwnedHeroes[0].Skills.Count == 0,
            "GameManager phải nạp tướng không skill, mang tên hiển thị tài khoản");
        GD.Print("✅ Test 4: GameManager sử dụng đúng dữ liệu của tài khoản mới.");

        // 5. Test authenticated formation persistence.
        GD.Print("[5/7] Kiểm tra đọc & ghi trận hình tài khoản...");
        var formations = await net.FetchFormationsAsync();
        Assert(formations != null && formations.Count > 0, "Danh sách trận hình từ DB phải có dữ liệu");

        var testEntries = new List<FormationEntry>
        {
            new("default_hero", 1, 1)
        };
        bool saveOk = await net.SaveFormationFromEntriesAsync("PVE_DEFAULT", testEntries);
        Assert(saveOk, "Gửi và lưu trận hình lên PostgreSQL phải thành công!");
        GD.Print("✅ Test 5: Đồng bộ trận hình 3x3 hai chiều Client <-> PostgreSQL thành công.");

        // 6. Logout invalidates the token; 7. logging in again issues a new token.
        GD.Print("[6/7] Kiểm tra logout và thu hồi session...");
        await net.LogoutAsync();
        Assert(!net.IsAuthenticated, "Client phải xóa session sau logout");
        GD.Print("✅ Test 6: Logout đã xóa session phía client.");

        GD.Print("[7/7] Đăng nhập lại bằng tài khoản đã đăng ký...");
        var invalidLogin = await net.LoginAsync(testUsername, "IncorrectPass123!");
        Assert(!invalidLogin.Success, "Đăng nhập với mật khẩu sai phải bị từ chối");
        var login = await net.LoginAsync(testUsername, testPassword);
        Assert(login.Success, $"Login failed: {login.Message}");
        Assert(net.IsAuthenticated, "Đăng nhập phải thiết lập session token");
        var restored = await net.TryRestoreSessionAsync();
        Assert(restored.Success, "Saved bearer token phải được kiểm tra và phục hồi thành công");
        var loggedInProfile = await net.FetchProfileAsync();
        Assert(loggedInProfile?.DisplayName == testDisplayName, "Session đăng nhập phải trỏ đúng tài khoản");
        await net.LogoutAsync();
        GD.Print("✅ Test 7: Login cấp session hợp lệ và chỉ đọc đúng tài khoản.");

        GD.Print("\n🎉 TOÀN BỘ 7 KIỂM THỬ KẾT NỐI CLIENT - SERVER ĐÃ VƯỢT QUA!\n");
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
