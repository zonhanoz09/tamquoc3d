using Godot;
using TamQuoc3D.Core.Autoload;
using TamQuoc3D.Core.Navigation;
using TamQuoc3D.Features.Hub.Ui;

namespace TamQuoc3D.Features.Hub;

/// <summary>
/// HubController - Điều khiển màn hình chính (Hub Screen) của Project Pixel Tactics.
/// Kết nối TopBar, ShortcutBar, QuestPanel, ChatBox, BottomNavBar, HubBackground và EventNotification.
/// </summary>
[GlobalClass]
public partial class HubController : Control
{
    private HubBackground _background;
    private TopBar _topBar;
    private AvatarWidget _avatarWidget;
    private ShortcutBar _shortcutBar;
    private QuestPanel _questPanel;
    private ChatBox _chatBox;
    private BottomNavBar _bottomNavBar;
    private EventNotification _eventNotification;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(1280, 720);
        BuildHubLayout();
        //ConnectEvents();
        //ShowWelcomeExperience();
    }

    private void BuildHubLayout()
    {
        // 1. Hub Background (Courtyard, Animated Characters, Particles)
        _background = new HubBackground();
        AddChild(_background);

        // 2. Player avatar panel below the TopBar
        _avatarWidget = new AvatarWidget
        {
            Name = "AvatarWidget",
            Position = new Vector2(8, 60),
            ZIndex = 10
        };
        AddChild(_avatarWidget);

        // 3. ShortcutBar (10 circular buttons below TopBar)
        // _shortcutBar = new ShortcutBar
        // {
        //     Position = new Vector2(0, 68),
        //     Size = new Vector2(1280, 76),
        //     ZIndex = 10
        // };
        // AddChild(_shortcutBar);

        // 5. QuestPanel (Right side, collapsible)
        // _questPanel = new QuestPanel
        // {
        //     Position = new Vector2(1015, 150),
        //     ZIndex = 10
        // };
        // AddChild(_questPanel);

        // // 6. ChatBox (Bottom Left above BottomNav)
        // _chatBox = new ChatBox
        // {
        //     Position = new Vector2(16, 508),
        //     ZIndex = 10
        // };
        // AddChild(_chatBox);

        // 7. BottomNavBar (Anchor: Bottom, Height 68px)
        _bottomNavBar = new BottomNavBar
        {
            Position = new Vector2(0, 652),
            ZIndex = 10
        };
        AddChild(_bottomNavBar);

        // 8. Event Notification (CanvasLayer for toasts and slide-in banners)
        // _eventNotification = new EventNotification();
        // AddChild(_eventNotification);
    }

    private void ConnectEvents()
    {
        if (_topBar != null)
        {
            _topBar.AvatarClicked += OnAvatarClicked;
            _topBar.SettingsClicked += OnSettingsClicked;
            _topBar.MailClicked += OnMailClicked;
        }

        if (_shortcutBar != null)
        {
            _shortcutBar.ShortcutClicked += OnShortcutClicked;
        }

        if (_bottomNavBar != null)
        {
            _bottomNavBar.TabSelected += OnBottomTabSelected;
        }
    }

    private void ShowWelcomeExperience()
    {
        var timer = GetTree().CreateTimer(1.2f);
        timer.Timeout += () =>
        {
            _eventNotification?.ShowToast("Đại Bản Doanh", "Chào mừng Chúa Công trở lại! Hãy kiểm tra nhiệm vụ hàng ngày.", 3.5f);

            var bannerTimer = GetTree().CreateTimer(2.0f);
            bannerTimer.Timeout += () =>
            {
                _eventNotification?.QueueBanner(new EventNotification.BannerData
                {
                    EventId = "quan_do_battle",
                    Title = "⚔️ ĐẠI CHIẾN QUAN ĐỘ",
                    Description = "Vượt ải Quan Độ nhận ngay Trang Bị Thần Binh Hoàng Kim!",
                    ActionText = "Tham Gia Ngay"
                });
            };
        };
    }

    private void OnAvatarClicked()
    {
        var profile = GameManager.Instance?.Profile;
        string name = profile?.PlayerName ?? "Chúa Công";
        int lvl = profile?.Level ?? 80;
        int vip = profile?.VipLevel ?? 11;
        _eventNotification?.ShowToast($"Hồ Sơ: {name}", $"Cấp độ: {lvl} | VIP {vip} | Quân hàm: Đại Tướng Quân", 3.0f);
    }

    private void OnSettingsClicked()
    {
        _eventNotification?.ShowToast("Cài Đặt Hệ Thống", "Âm lượng: 100% | Đồ họa: HD-2D Pixel Art | Phiên bản 1.0.0", 3.0f);
    }

    private void OnMailClicked()
    {
        _eventNotification?.ShowToast("Hòm Thư", "Bạn có 2 thư chưa đọc từ Quân Sư Gia Cát Lượng!", 3.0f);
    }

    private void OnShortcutClicked(string shortcutId)
    {
        switch (shortcutId)
        {
            case "hoat_dong":
                _eventNotification?.ShowToast("Hoạt Động", "Hoạt động Đua Top Lực Chiến đang diễn ra!", 2.5f);
                break;
            case "su_kien":
                _eventNotification?.QueueBanner(new EventNotification.BannerData
                {
                    EventId = "su_kien_trieu_hoi",
                    Title = "🔥 TRIỆU HỒI TƯỚNG ĐỎ",
                    Description = "Tỷ lệ xuất hiện Lữ Bố, Triệu Vân tăng 300% hôm nay!",
                    ActionText = "Chiêu Mộ"
                });
                break;
            case "khanh_dien":
                _eventNotification?.ShowToast("Khánh Điển", "Đăng nhập 7 ngày nhận Tướng Cam Triệu Vân!", 2.5f);
                break;
            case "dau_gia":
                _eventNotification?.ShowToast("Đấu Giá", "Phiên đấu giá Thần Binh mở lúc 20:00 hàng ngày.", 2.5f);
                break;
            case "quoc_gia":
                _eventNotification?.ShowToast("Quốc Gia", "Quốc chiến Thục - Ngụy - Ngô sắp khởi tranh!", 2.5f);
                break;
            case "tuyet_lo":
                _eventNotification?.ShowToast("Tuyệt Lộ", "Khiêu chiến Tuyệt Lộ nhận Đá Cường Hóa cao cấp.", 2.5f);
                break;
            case "long_dien":
                _eventNotification?.ShowToast("Long Điện", "Thu thập Long Hồn để mở khóa võ tướng truyền thuyết.", 2.5f);
                break;
            case "di_canh":
                _eventNotification?.ShowToast("Di Cảnh", "Thám hiểm Di Cảnh cổ đại tìm kiếm bí tịch.", 2.5f);
                break;
            case "tu_luyen":
                _eventNotification?.ShowToast("Tu Luyện", "Tướng lĩnh đang bế quan tu luyện võ học (+20% EXP).", 2.5f);
                break;
            case "donate":
                _eventNotification?.ShowToast("Quà Nạp", "Nạp lần đầu bất kỳ nhận ngay Thần Tướng Quan Vũ!", 2.5f);
                break;
            default:
                _eventNotification?.ShowToast("Thông Báo", $"Đã chọn lối tắt: {shortcutId}", 2.0f);
                break;
        }
    }

    private void OnBottomTabSelected(string tabId)
    {
        GD.Print($"[HubController] Tab selected: {tabId}");
        // BottomNavBar handles direct navigation for formation, campaign, arena, hero, guild
    }
}
