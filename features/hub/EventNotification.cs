using System.Collections.Generic;
using Godot;
using TamQuoc3D.Core.Autoload;

namespace TamQuoc3D.Features.Hub;

/// <summary>
/// EventNotification - Quản lý hiển thị Banner sự kiện (Slide-in) và Toast thông báo hệ thống góc trên bên phải.
/// </summary>
[GlobalClass]
public partial class EventNotification : CanvasLayer
{
    public class BannerData
    {
        public string EventId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ActionText { get; set; } = "Tham Gia";
    }

    private Control _bannerContainer;
    private VBoxContainer _toastContainer;
    private readonly Queue<BannerData> _bannerQueue = new();
    private bool _isBannerShowing = false;

    public override void _Ready()
    {
        Layer = 25;
        BuildUi();
        ConnectSignals();
    }

    private void BuildUi()
    {
        // 1. Toast Container (Top Right: X: 940, Y: 70, width: 320)
        _toastContainer = new VBoxContainer
        {
            Position = new Vector2(930, 70),
            CustomMinimumSize = new Vector2(330, 200),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _toastContainer.AddThemeConstantOverride("separation", 6);
        AddChild(_toastContainer);

        // 2. Event Banner Container (Middle Right, Slide-in)
        _bannerContainer = new Control
        {
            Position = new Vector2(1300, 220), // Offscreen to right
            CustomMinimumSize = new Vector2(380, 140),
            Size = new Vector2(380, 140)
        };
        AddChild(_bannerContainer);

        var bannerPanel = new PanelContainer
        {
            Size = new Vector2(380, 140)
        };
        var bannerStyle = new StyleBoxTexture
        {
            Texture = GD.Load<Texture2D>("res://assets/ui/hub/event_banner_frame.png")
        };
        bannerPanel.AddThemeStyleboxOverride("panel", bannerStyle);
        _bannerContainer.AddChild(bannerPanel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 14);
        margin.AddThemeConstantOverride("margin_right", 14);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        bannerPanel.AddChild(margin);

        var vBox = new VBoxContainer();
        vBox.AddThemeConstantOverride("separation", 6);
        margin.AddChild(vBox);

        var titleRow = new HBoxContainer();
        vBox.AddChild(titleRow);

        var titleLabel = new Label
        {
            Name = "TitleLabel",
            Text = "🎉 SỰ KIỆN HẠN GIỜ",
            Modulate = new Color(1f, 0.9f, 0.3f),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        titleLabel.AddThemeFontSizeOverride("font_size", 14);
        titleRow.AddChild(titleLabel);

        var closeBtn = new Button
        {
            Text = "✕",
            Flat = true,
            CustomMinimumSize = new Vector2(24, 24)
        };
        closeBtn.Pressed += DismissCurrentBanner;
        titleRow.AddChild(closeBtn);

        var descLabel = new Label
        {
            Name = "DescLabel",
            Text = "Quần Hùng Trục Lộc - Tăng x2 tỷ lệ rớt bảo vật hiếm trong tuần này!",
            Modulate = new Color(0.95f, 0.95f, 0.95f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        descLabel.AddThemeFontSizeOverride("font_size", 11);
        vBox.AddChild(descLabel);

        var actBtn = new Button
        {
            Name = "ActionBtn",
            Text = "Tham Gia Ngay",
            CustomMinimumSize = new Vector2(110, 26),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd
        };
        actBtn.AddThemeFontSizeOverride("font_size", 11);
        var actBtnStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.75f, 0.2f, 0.25f),
            BorderColor = new Color(1f, 0.85f, 0.3f),
            BorderWidthBottom = 1,
            BorderWidthTop = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4
        };
        actBtn.AddThemeStyleboxOverride("normal", actBtnStyle);
        actBtn.Pressed += DismissCurrentBanner;
        vBox.AddChild(actBtn);
    }

    private void ConnectSignals()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.SystemNotification += (title, body, type) => ShowToast(title, body);
            EventBus.Instance.EventBannerShow += (evtId) =>
            {
                QueueBanner(new BannerData
                {
                    EventId = evtId,
                    Title = "⚔️ ĐẠI CHIẾN QUAN ĐỘ",
                    Description = "Vượt ải Quan Độ nhận ngay Trang Bị Thần Binh Hoàng Kim!",
                    ActionText = "Tham Gia"
                });
            };
        }
    }

    public void ShowToast(string title, string message, float duration = 3.0f)
    {
        var toast = new PanelContainer
        {
            CustomMinimumSize = new Vector2(320, 50),
            Modulate = new Color(1, 1, 1, 0)
        };
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.08f, 0.16f, 0.92f),
            BorderColor = new Color(0.85f, 0.7f, 0.3f),
            BorderWidthBottom = 1,
            BorderWidthTop = 1,
            BorderWidthLeft = 2,
            BorderWidthRight = 1,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4
        };
        toast.AddThemeStyleboxOverride("panel", style);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_top", 4);
        margin.AddThemeConstantOverride("margin_bottom", 4);
        toast.AddChild(margin);

        var vBox = new VBoxContainer();
        vBox.AddThemeConstantOverride("separation", 2);
        margin.AddChild(vBox);

        var titleLbl = new Label
        {
            Text = title,
            Modulate = new Color(1f, 0.85f, 0.3f)
        };
        titleLbl.AddThemeFontSizeOverride("font_size", 12);
        vBox.AddChild(titleLbl);

        var msgLbl = new Label
        {
            Text = message,
            Modulate = new Color(0.95f, 0.95f, 0.95f)
        };
        msgLbl.AddThemeFontSizeOverride("font_size", 10);
        vBox.AddChild(msgLbl);

        _toastContainer.AddChild(toast);

        var tween = CreateTween();
        tween.TweenProperty(toast, "modulate:a", 1.0f, 0.2f);
        tween.TweenInterval(duration);
        tween.TweenProperty(toast, "modulate:a", 0.0f, 0.3f);
        tween.TweenCallback(Callable.From(() => toast.QueueFree()));
    }

    public void QueueBanner(BannerData data)
    {
        if (_bannerQueue.Count < 3)
        {
            _bannerQueue.Enqueue(data);
        }
        if (!_isBannerShowing)
        {
            ShowNextBanner();
        }
    }

    private void ShowNextBanner()
    {
        if (_bannerQueue.Count == 0)
        {
            _isBannerShowing = false;
            return;
        }

        _isBannerShowing = true;
        var data = _bannerQueue.Dequeue();

        var titleLabel = _bannerContainer.FindChild("TitleLabel", true, false) as Label;
        var descLabel = _bannerContainer.FindChild("DescLabel", true, false) as Label;
        var actBtn = _bannerContainer.FindChild("ActionBtn", true, false) as Button;

        if (titleLabel != null) titleLabel.Text = data.Title;
        if (descLabel != null) descLabel.Text = data.Description;
        if (actBtn != null) actBtn.Text = data.ActionText;

        // Slide-in from right (X: 1300 -> 880)
        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(_bannerContainer, "position:x", 880f, 0.4f);
    }

    public void DismissCurrentBanner()
    {
        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.In);
        tween.TweenProperty(_bannerContainer, "position:x", 1300f, 0.3f);
        tween.TweenCallback(Callable.From(() =>
        {
            _isBannerShowing = false;
            ShowNextBanner();
        }));
    }
}
