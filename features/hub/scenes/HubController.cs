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
/// Ket noi HubBackground (san dien + nhan vat), PlayerCard (the nguoi choi trai
/// tren, fix cung), IconDock (cum icon phai, don phai, mo rong duoc), panel
/// Nhiem Vu Chinh, BottomNavBar va EventNotification (toast/banner).
/// Du lieu hien thi luon lay that tu GameManager/NetworkManager - khong so lieu gia.
/// </summary>
[GlobalClass]
public partial class HubController : Control
{
    private HubBackground _background;
    private PlayerCard _playerCard;
    private IconDock _iconDock;
    private QuestTrackerPanel _questPanel;
    private ChatBox _chatBox;
    private BottomNavBar _bottomNavBar;
    private ExpBar _expBar;
    private EventNotification _eventNotification;
    private Control _modalRoot;

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

        // 2. PlayerCard: the nguoi choi don trai, fix cung (0, 0, 365, 135)
        _playerCard = new PlayerCard
        {
            Name = "PlayerCard",
            Position = new Vector2(0, 0),
            ZIndex = 20
        };
        AddChild(_playerCard);

        // 3. IconDock: cum icon phai don phai, mep phai neo o x = 1272.
        // Ben duoi dock la panel Nhiem Vu Chinh (vi tri Y chay theo chieu cao dock).
        _iconDock = new IconDock
        {
            Name = "IconDock",
            Position = new Vector2(1272 - IconDock.DockWidth, 0),
            ZIndex = 15
        };
        AddChild(_iconDock);

        _questPanel = new QuestTrackerPanel
        {
            Name = "QuestPanel",
            ZIndex = 15
        };
        AddChild(_questPanel);
        PositionQuestPanel();

        // 4. ChatBox: don trai, fix cung, day khung y = 700 (ngang hang nav)
        _chatBox = new ChatBox
        {
            Name = "ChatBox",
            Position = new Vector2(16, 550),
            ZIndex = 12
        };
        AddChild(_chatBox);

        // 5. BottomNavBar: don phai, fix cung (0, 648), day y = 700
        _bottomNavBar = new BottomNavBar
        {
            Name = "BottomNavBar",
            Position = new Vector2(0, 648),
            ZIndex = 10
        };
        AddChild(_bottomNavBar);

        // 6. Thanh kinh nghiem: full-width, sat day man hinh (y 704-720)
        _expBar = new ExpBar
        {
            Name = "ExpBar",
            Position = new Vector2(0, 704),
            ZIndex = 10
        };
        AddChild(_expBar);

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

    private void PositionQuestPanel()
    {
        if (_questPanel == null || _iconDock == null) return;
        _questPanel.Position = new Vector2(1272 - 272, _iconDock.Position.Y + _iconDock.DockHeight + 8);
    }

    private void ConnectEvents()
    {
        if (_playerCard != null)
        {
            _playerCard.AvatarClicked += OnAvatarClicked;
            _playerCard.DiamondPlusClicked += () => SceneNavigator.GoToShop();
        }

        if (_iconDock != null)
        {
            _iconDock.IconClicked += OnDockIconClicked;
            _iconDock.MapClicked += () => SceneNavigator.GoToCampaign();
            _iconDock.SettingsClicked += OnSettingsClicked;
            _iconDock.HeightChanged += _ => PositionQuestPanel();
        }

        if (_questPanel != null)
        {
            _questPanel.QuestGoRequested += actionId =>
            {
                switch (actionId)
                {
                    case "campaign":
                        SceneNavigator.GoToCampaign();
                        break;
                    case "tower":
                        SceneNavigator.GoToTower();
                        break;
                }
            };
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

    private void OnDockIconClicked(string iconId)
    {
        switch (iconId)
        {
            case "chieu_mo":
            case "su_kien":
                // Su kien dang chay chinh la banner Chieu Mo gioi han
                SceneNavigator.GoToGacha();
                break;
            case "cua_hang":
            case "qua":
                SceneNavigator.GoToShop();
                break;
            case "thap":
                SceneNavigator.GoToTower();
                break;
            case "arena":
                SceneNavigator.GoToArena();
                break;
            case "formation":
                SceneNavigator.GoToFormationSelector();
                break;
            case "hero":
                SceneNavigator.GoToHeroCollection();
                break;
            case "hom_thu":
                _ = OpenMailPanelAsync();
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
                GD.Print($"[HubController] Icon chua duoc gan: {iconId}");
                break;
        }
    }

    /// <summary>Badge cho dock icon: dot Chieu Mo khi du KC, tang thap ke tiep, dot thu chua doc.</summary>
    private async System.Threading.Tasks.Task RefreshShortcutBadgesAsync()
    {
        if (_iconDock == null) return;
        _iconDock.SetNotification("chieu_mo", (GameManager.Instance?.Gems ?? 0) >= 100);

        var network = NetworkManager.Instance;
        if (network == null || string.IsNullOrEmpty(network.CurrentUserId)) return;
        var tower = await network.FetchTowerProgressAsync();
        if (tower != null)
        {
            GameManager.Instance.TowerHighestFloor = tower.HighestFloor;
            _iconDock.SetSubLabel("thap", tower.HighestFloor >= 50 ? "MAX" : $"Tầng {tower.HighestFloor + 1}");
        }
    }

    /// <summary>
    /// Panel Nhiem Vu Chinh: chi tien trinh chinh tu du lieu that — ai chien dich
    /// ke tiep (theo thu tu 1-1..3-6, lay tu campaign progress) + tang thap ke tiep.
    /// Khong phai nhiem vu hang ngay (lam sau).
    /// </summary>
    private async System.Threading.Tasks.Task BuildQuestEntriesAsync()
    {
        if (_questPanel == null) return;
        var entries = new System.Collections.Generic.List<QuestTrackerPanel.QuestEntry>();
        var network = NetworkManager.Instance;

        if (network != null && !string.IsNullOrEmpty(network.CurrentUserId))
        {
            var progress = await network.FetchCampaignProgressAsync();
            var cleared = new System.Collections.Generic.HashSet<string>();
            if (progress != null)
            {
                foreach (var row in progress)
                {
                    if (row.ClearCount > 0) cleared.Add(row.StageId);
                }
            }
            var orderedKeys = new[]
            {
                "1-1", "1-2", "1-3", "1-4", "1-5", "1-6",
                "2-1", "2-2", "2-3", "2-4", "2-5", "2-6",
                "3-1", "3-2", "3-3", "3-4", "3-5", "3-6"
            };
            var next = System.Array.Find(orderedKeys, k => !cleared.Contains(k));
            entries.Add(new QuestTrackerPanel.QuestEntry
            {
                Id = "campaign_next",
                Title = next == null ? "Đã qua hết ải chiến dịch" : $"Vượt ải {next}",
                Sub = $"Chiến dịch · {cleared.Count}/18 ải đã qua",
                Detail = next == null
                    ? "Chúa công đã chinh phục toàn bộ 3 chương chiến dịch."
                    : $"Ải chiến dịch tiếp theo chưa qua là ải {next}. Vào Chiến Dịch để khiêu chiến.",
                ActionId = "campaign"
            });

            var tower = await network.FetchTowerProgressAsync();
            var floor = tower?.HighestFloor ?? GameManager.Instance.TowerHighestFloor;
            entries.Add(new QuestTrackerPanel.QuestEntry
            {
                Id = "tower_next",
                Title = floor >= 50 ? "Tháp đã chinh phục tối đa" : $"Lên tầng tháp {floor + 1}",
                Sub = $"Tháp Thí Luyện · tầng cao nhất {floor}",
                Detail = floor >= 50
                    ? "Chúa công đã leo tới đỉnh Tháp Thí Luyện."
                    : $"Tầng tháp tiếp theo là tầng {floor + 1}. Vào Tháp để khiêu chiến.",
                ActionId = "tower"
            });
        }

        _questPanel.SetEntries(entries);
    }

    /// <summary>
    /// Vao hub thi dong bo lai du lieu tu server (vi, ho so, tuong) -
    /// PlayerCard tu cap nhat qua signal DataSynced, them badge thu chua doc.
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
            await BuildQuestEntriesAsync();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[HubController] Sync on enter failed: {ex.Message}");
        }
    }

    public async System.Threading.Tasks.Task RefreshMailBadgeAsync()
    {
        var network = NetworkManager.Instance;
        if (network == null || string.IsNullOrEmpty(network.CurrentUserId)) return;

        var mails = await network.FetchMailAsync();
        int unread = mails.Count(m => !m.IsRead);
        _iconDock?.SetNotification("hom_thu", unread > 0);
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
        _iconDock?.SetNotification("hom_thu", unread > 0);

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
