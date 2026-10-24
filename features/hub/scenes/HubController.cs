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
    private ShortcutBar _shortcutBar;
    private ChatBox _chatBox;
    private BottomNavBar _bottomNavBar;
    private EventNotification _eventNotification;
    private Control _modalRoot;

    private readonly System.Collections.Generic.Dictionary<HubCharacter, NetworkManager.ArenaOpponentDto> _guestData = new();
    private bool _guestsSpawned;
    private EventBus.SystemNotificationEventHandler _onSystemNotification;

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

        // 3. Cot icon chuc nang ben phai (tinh nang that + khoa tinh nang chua co)
        _shortcutBar = new ShortcutBar
        {
            Name = "ShortcutBar",
            Position = new Vector2(1146, 72),
            ZIndex = 15
        };
        AddChild(_shortcutBar);

        // 4. ChatBox kenh he thong (goc trai duoi, tren BottomNav)
        _chatBox = new ChatBox
        {
            Name = "ChatBox",
            Position = new Vector2(16, 508),
            ZIndex = 12
        };
        AddChild(_chatBox);

        // 5. BottomNavBar (dieu huong chinh, tu xu ly chuyen scene)
        _bottomNavBar = new BottomNavBar
        {
            Name = "BottomNavBar",
            Position = new Vector2(0, 652),
            ZIndex = 10
        };
        AddChild(_bottomNavBar);

        // 6. Thong bao toast + banner su kien (nhan EventBus.SystemNotification)
        _eventNotification = new EventNotification();
        AddChild(_eventNotification);
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance != null && _onSystemNotification != null)
        {
            EventBus.Instance.SystemNotification -= _onSystemNotification;
        }
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

        if (_shortcutBar != null)
        {
            _shortcutBar.ShortcutClicked += OnShortcutClicked;
        }

        if (_bottomNavBar != null)
        {
            _bottomNavBar.TabSelected += OnBottomTabSelected;
        }

        // Moi thong bao he thong cung duoc ghi vao kenh He Thong cua ChatBox
        if (EventBus.Instance != null)
        {
            _onSystemNotification = (title, body, type) =>
            {
                if (IsInstanceValid(this) && _chatBox != null)
                {
                    _chatBox.AddMessage(title, body, "sys");
                }
            };
            EventBus.Instance.SystemNotification += _onSystemNotification;
        }
    }

    private void OnShortcutClicked(string shortcutId)
    {
        switch (shortcutId)
        {
            case "chieu_mo":
                SceneNavigator.GoToGacha();
                break;
            case "cua_hang":
                SceneNavigator.GoToShop();
                break;
            case "thap":
                SceneNavigator.GoToTower();
                break;
            case "hom_thu":
                _ = OpenMailPanelAsync();
                break;
            case "su_kien":
                // Su kien dang chay chinh la banner Chieu Mo gioi han
                SceneNavigator.GoToGacha();
                break;
            case "cai_dat":
                OpenSettingsPanel();
                break;
            case "me_cung":
                _eventNotification?.ShowToast("Mê Cung", "Tính năng sắp mở.", 2.0f);
                break;
            case "bang_hoi":
                _eventNotification?.ShowToast("Bang Hội", "Tính năng sắp mở.", 2.0f);
                break;
            default:
                GD.Print($"[HubController] Shortcut chua duoc gan: {shortcutId}");
                break;
        }
    }

    /// <summary>
    /// Khach online: 2 nguoi choi that tu danh sach doi thu arena, dung tuong
    /// dai dien di dao tren san. Du lieu dang snapshot (khong phai realtime);
    /// offline/khong co doi thu thi khong hien gi ca.
    /// </summary>
    private async System.Threading.Tasks.Task SpawnGuestsAsync()
    {
        if (_guestsSpawned || _background == null) return;
        var network = NetworkManager.Instance;
        if (network == null || string.IsNullOrEmpty(network.CurrentUserId)) return;

        var opponents = await network.FetchArenaOpponentsAsync();
        var spots = new[] { new Vector2(215, 470), new Vector2(1015, 465) };
        int spawned = 0;
        foreach (var opp in opponents)
        {
            if (spawned >= spots.Length) break;
            if (opp.HeroIds == null || opp.HeroIds.Count == 0) continue;

            var guest = _background.CreateCharacter(
                opp.HeroIds[0],
                opp.DisplayName,
                $"LC {opp.CombatPower:N0}",
                isMain: false, isGuest: true,
                position: spots[spawned],
                wander: true);
            _guestData[guest] = opp;
            guest.CharacterClicked += OnGuestClicked;
            spawned++;
        }
        _guestsSpawned = spawned > 0;
    }

    private void OnGuestClicked(HubCharacter guest)
    {
        if (!_guestData.TryGetValue(guest, out var opp)) return;

        var content = OpenModal($"⚔ {opp.DisplayName}");
        content.AddChild(new Label { Text = $"Hạng MMR: {opp.Mmr}" });
        content.AddChild(new Label { Text = $"Thắng / Thua: {opp.WinCount} / {opp.LossCount}" });
        content.AddChild(new Label { Text = $"Lực chiến đội hình: {opp.CombatPower:N0}" });

        var challengeBtn = new Button
        {
            Text = "Tới Đấu Trường Thách Đấu",
            CustomMinimumSize = new Vector2(220, 34)
        };
        challengeBtn.Pressed += () =>
        {
            CloseModal();
            SceneNavigator.GoToArena();
        };
        content.AddChild(challengeBtn);
    }

    /// <summary>Badge cho cot icon: dot Chieu Mo khi du KC, tang thap ke tiep, dot thu chua doc.</summary>
    private async System.Threading.Tasks.Task RefreshShortcutBadgesAsync()
    {
        if (_shortcutBar == null) return;
        _shortcutBar.SetNotification("chieu_mo", (GameManager.Instance?.Gems ?? 0) >= 100);

        var network = NetworkManager.Instance;
        if (network == null || string.IsNullOrEmpty(network.CurrentUserId)) return;
        var tower = await network.FetchTowerProgressAsync();
        if (tower != null)
        {
            GameManager.Instance.TowerHighestFloor = tower.HighestFloor;
            _shortcutBar.SetSubLabel("thap", tower.HighestFloor >= 50 ? "MAX" : $"Tầng {tower.HighestFloor + 1}");
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
            await RefreshShortcutBadgesAsync();
            await SpawnGuestsAsync();
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
        int unread = mails.Count(m => !m.IsRead);
        _topBar.SetMailBadge(unread);
        _shortcutBar?.SetNotification("hom_thu", unread > 0);
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
        OpenSettingsPanel();
    }

    private void OnMailClicked()
    {
        _ = OpenMailPanelAsync();
    }

    // ---------- Modal panels (mail / settings) ----------

    private VBoxContainer OpenModal(string title)
    {
        CloseModal();
        _modalRoot = new Control
        {
            Name = "HubModal",
            Position = Vector2.Zero,
            Size = new Vector2(1280, 720),
            ZIndex = 40
        };
        AddChild(_modalRoot);

        var dim = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.55f),
            Size = new Vector2(1280, 720)
        };
        _modalRoot.AddChild(dim);

        var panel = new PanelContainer
        {
            Position = new Vector2(390, 130),
            Size = new Vector2(500, 460),
            CustomMinimumSize = new Vector2(500, 460)
        };
        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.13f, 0.1f, 0.17f, 0.98f),
            BorderColor = new Color(0.85f, 0.7f, 0.3f),
            BorderWidthBottom = 2,
            BorderWidthTop = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8
        };
        panel.AddThemeStyleboxOverride("panel", panelStyle);
        _modalRoot.AddChild(panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        panel.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);
        margin.AddChild(vbox);

        var header = new HBoxContainer();
        vbox.AddChild(header);
        var titleLabel = new Label
        {
            Text = title,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Modulate = new Color(1f, 0.85f, 0.3f)
        };
        titleLabel.AddThemeFontSizeOverride("font_size", 16);
        header.AddChild(titleLabel);
        var closeBtn = new Button { Text = "✕", CustomMinimumSize = new Vector2(30, 26) };
        closeBtn.Pressed += CloseModal;
        header.AddChild(closeBtn);

        return vbox;
    }

    private void CloseModal()
    {
        if (_modalRoot != null && IsInstanceValid(_modalRoot))
        {
            _modalRoot.QueueFree();
        }
        _modalRoot = null;
    }

    private async System.Threading.Tasks.Task OpenMailPanelAsync()
    {
        var network = NetworkManager.Instance;
        if (network == null || string.IsNullOrEmpty(network.CurrentUserId))
        {
            _eventNotification?.ShowToast("Hòm Thư", "Đăng nhập để xem thư.", 2.5f);
            return;
        }

        var content = OpenModal("✉ HÒM THƯ");
        var loading = new Label { Text = "Đang tải thư..." };
        content.AddChild(loading);

        var mails = await network.FetchMailAsync();
        if (_modalRoot == null || !IsInstanceValid(_modalRoot)) return;
        loading.QueueFree();

        int unread = mails.Count(m => !m.IsRead);
        _topBar?.SetMailBadge(unread);
        _shortcutBar?.SetNotification("hom_thu", unread > 0);

        if (mails.Count == 0)
        {
            content.AddChild(new Label { Text = "Chưa có thư nào." });
            return;
        }

        foreach (var mail in mails.Take(10))
        {
            var row = new Label
            {
                Text = $"{(mail.IsRead ? "·" : "●")} {mail.Title}",
                Modulate = mail.IsRead ? new Color(0.8f, 0.8f, 0.8f) : new Color(1f, 0.95f, 0.8f),
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            };
            row.AddThemeFontSizeOverride("font_size", 13);
            content.AddChild(row);
            if (!string.IsNullOrWhiteSpace(mail.Body))
            {
                var body = new Label
                {
                    Text = mail.Body,
                    Modulate = new Color(0.75f, 0.75f, 0.75f),
                    AutowrapMode = TextServer.AutowrapMode.WordSmart
                };
                body.AddThemeFontSizeOverride("font_size", 11);
                content.AddChild(body);
            }
        }
    }

    private void OpenSettingsPanel()
    {
        var content = OpenModal("⚙ CÀI ĐẶT");

        var volumeRow = new HBoxContainer();
        volumeRow.AddThemeConstantOverride("separation", 10);
        content.AddChild(volumeRow);
        volumeRow.AddChild(new Label { Text = "Âm lượng tổng" });
        int masterBus = AudioServer.GetBusIndex("Master");
        var slider = new HSlider
        {
            MinValue = 0,
            MaxValue = 1,
            Step = 0.05,
            Value = Mathf.DbToLinear(AudioServer.GetBusVolumeDb(masterBus)),
            CustomMinimumSize = new Vector2(220, 24),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        slider.ValueChanged += (value) =>
        {
            AudioServer.SetBusVolumeDb(masterBus, Mathf.LinearToDb((float)value));
            AudioServer.SetBusMute(masterBus, value <= 0.01);
        };
        volumeRow.AddChild(slider);

        bool online = NetworkManager.Instance?.IsDatabaseConnected ?? false;
        var status = new Label
        {
            Text = $"Máy chủ: {(online ? "Đã kết nối" : "Offline")} | Phiên bản 1.0.0"
        };
        status.AddThemeFontSizeOverride("font_size", 12);
        content.AddChild(status);

        var logoutBtn = new Button
        {
            Text = "Đăng Xuất",
            CustomMinimumSize = new Vector2(140, 32)
        };
        logoutBtn.Pressed += async () =>
        {
            CloseModal();
            await NetworkManager.Instance.LogoutAsync();
            GetTree().ChangeSceneToFile(SceneNavigator.LoginScenePath);
        };
        content.AddChild(logoutBtn);
    }

    private void OnBottomTabSelected(string tabId)
    {
        GD.Print($"[HubController] Tab selected: {tabId}");
        if (tabId == "guild")
        {
            _eventNotification?.ShowToast("Bang Hội", "Tính năng sắp mở.", 2.0f);
        }
        // BottomNavBar handles direct navigation for formation, campaign, arena, hero
    }
}
