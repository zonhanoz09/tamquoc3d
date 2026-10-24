using System;
using System.Linq;
using Godot;
using TamQuoc3D.Core.Autoload;
using TamQuoc3D.Core.Navigation;
using TamQuoc3D.Core.Network;
using TamQuoc3D.Features.Hub.Ui;

namespace TamQuoc3D.Features.Hub;

/// <summary>
/// HubController - Dieu khien man hinh chinh (Hub) cua Project Pixel Tactics.
/// Ket noi HubBackground (san dien + nhan vat), TopBar (ho so + vi tien),
/// ShortcutBar (icon chuc nang), BottomNavBar va EventNotification (toast/banner).
/// Du lieu hien thi luon lay that tu GameManager/NetworkManager - khong so lieu gia.
/// </summary>
[GlobalClass]
public partial class HubController : Control
{
    private HubBackground _background;
    private TopBar _topBar;
    private BottomNavBar _bottomNavBar;
    private EventNotification _eventNotification;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(1280, 720);
        BuildHubLayout();
        ConnectEvents();
        ShowWelcomeExperience();
        _ = SyncOnEnterAsync();
    }

    private void BuildHubLayout()
    {
        // 1. Nen san dien + nhan vat (HubBackground tu spawn nhan vat trong _Ready cua no)
        _background = new HubBackground();
        AddChild(_background);

        // 2. TopBar: avatar, ten/cap/VIP, the luc, Bac, Kim Cuong, thu, cai dat
        _topBar = new TopBar
        {
            Name = "TopBar",
            Position = new Vector2(0, 0),
            ZIndex = 20
        };
        AddChild(_topBar);

        // 3. BottomNavBar (dieu huong chinh, tu xu ly chuyen scene)
        _bottomNavBar = new BottomNavBar
        {
            Name = "BottomNavBar",
            Position = new Vector2(0, 652),
            ZIndex = 10
        };
        AddChild(_bottomNavBar);

        // 4. Thong bao toast + banner su kien (nhan EventBus.SystemNotification)
        _eventNotification = new EventNotification();
        AddChild(_eventNotification);
    }

    private void ConnectEvents()
    {
        if (_topBar != null)
        {
            _topBar.AvatarClicked += OnAvatarClicked;
            _topBar.SettingsClicked += OnSettingsClicked;
            _topBar.MailClicked += OnMailClicked;
            _topBar.DiamondPlusClicked += () => SceneNavigator.GoToShop();
        }

        if (_bottomNavBar != null)
        {
            _bottomNavBar.TabSelected += OnBottomTabSelected;
        }
    }

    /// <summary>
    /// Vao hub thi dong bo lai du lieu tu server (vi, ho so, tuong) -
    /// TopBar tu cap nhat qua signal DataSynced, them badge thu chua doc.
    /// </summary>
    private async System.Threading.Tasks.Task SyncOnEnterAsync()
    {
        var network = NetworkManager.Instance;
        if (network == null || string.IsNullOrEmpty(network.CurrentUserId)) return;

        try
        {
            await network.SyncPlayerDataAsync();
            await RefreshMailBadgeAsync();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[HubController] Sync on enter failed: {ex.Message}");
        }
    }

    public async System.Threading.Tasks.Task RefreshMailBadgeAsync()
    {
        var network = NetworkManager.Instance;
        if (network == null || string.IsNullOrEmpty(network.CurrentUserId) || _topBar == null) return;

        var mails = await network.FetchMailAsync();
        _topBar.SetMailBadge(mails.Count(m => !m.IsRead));
    }

    private void ShowWelcomeExperience()
    {
        var timer = GetTree().CreateTimer(1.0f);
        timer.Timeout += () =>
        {
            string name = GameManager.Instance?.Profile?.PlayerName ?? "Chúa Công";
            _eventNotification?.ShowToast("Đại Bản Doanh", $"Chào mừng {name} trở lại!", 3.5f);

            var bannerTimer = GetTree().CreateTimer(1.8f);
            bannerTimer.Timeout += () =>
            {
                _eventNotification?.QueueBanner(new EventNotification.BannerData
                {
                    EventId = "chieu_mo_lu_bo",
                    Title = "🔥 CHIÊU MỘ: LỮ BỐ",
                    Description = "Banner giới hạn đang mở - 100 Kim Cương/lượt, tích điểm pity đổi tướng UR!",
                    ActionText = "Chiêu Mộ"
                });
            };
        };
    }

    private void OnAvatarClicked()
    {
        var profile = GameManager.Instance?.Profile;
        if (profile == null) return;
        _eventNotification?.ShowToast(
            $"Hồ Sơ: {profile.PlayerName}",
            $"Cấp {profile.Level} | VIP {profile.VipLevel} | Lực chiến {profile.CombatPower:N0}",
            3.0f);
    }

    private void OnSettingsClicked()
    {
        bool online = NetworkManager.Instance?.IsDatabaseConnected ?? false;
        _eventNotification?.ShowToast(
            "Cài Đặt Hệ Thống",
            $"Máy chủ: {(online ? "Đã kết nối" : "Offline")} | Phiên bản 1.0.0",
            3.0f);
    }

    private async void OnMailClicked()
    {
        var network = NetworkManager.Instance;
        if (network == null || string.IsNullOrEmpty(network.CurrentUserId))
        {
            _eventNotification?.ShowToast("Hòm Thư", "Đăng nhập để xem thư.", 2.5f);
            return;
        }

        var mails = await network.FetchMailAsync();
        int unread = mails.Count(m => !m.IsRead);
        _topBar?.SetMailBadge(unread);
        _eventNotification?.ShowToast(
            "Hòm Thư",
            mails.Count == 0
                ? "Chưa có thư nào."
                : $"{mails.Count} thư, {unread} chưa đọc. Mới nhất: {mails[0].Title}",
            3.5f);
    }

    private void OnBottomTabSelected(string tabId)
    {
        GD.Print($"[HubController] Tab selected: {tabId}");
        // BottomNavBar handles direct navigation for formation, campaign, arena, hero, guild
    }
}
