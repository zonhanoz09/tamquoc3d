using System;
using Godot;
using TamQuoc3D.Core.Autoload;
using TamQuoc3D.Data.Database;
using TamQuoc3D.Data.Resources;

namespace TamQuoc3D.Features.Hub.Ui;

/// <summary>
/// TopBar - Thanh thông tin đỉnh màn hình hiển thị Avatar, Cấp độ, VIP, Danh hiệu, Tiền tệ (Vàng, KNB, Thể lực) và đếm ngược hồi thể lực.
/// </summary>
[GlobalClass]
public partial class TopBar : PanelContainer
{
    [Signal] public delegate void AvatarClickedEventHandler();
    [Signal] public delegate void SettingsClickedEventHandler();
    [Signal] public delegate void MailClickedEventHandler();

    private TextureButton _avatarBtn;
    private TextureRect _avatarRect;
    private TextureRect _vipBadgeRect;
    private Label _playerNameLabel;
    private Label _levelLabel;
    private Label _titleLabel;

    private Label _goldLabel;
    private Label _diamondLabel;
    private Label _staminaLabel;
    private ProgressBar _staminaBar;
    private Label _staminaTimerLabel;

    private int _staminaSecondsRemaining = 1483;
    private double _timerAccumulator = 0.0;

    public override void _Ready()
    {
        BuildLayout();
        ConnectSignals();
        RefreshFromGameManager();
    }

    public override void _Process(double delta)
    {
        _timerAccumulator += delta;
        if (_timerAccumulator >= 1.0)
        {
            _timerAccumulator -= 1.0;
            if (_staminaSecondsRemaining > 0)
            {
                _staminaSecondsRemaining--;
                UpdateStaminaTimerDisplay();
                EventBus.Instance?.EmitSignal(EventBus.SignalName.StaminaTimerTick, _staminaSecondsRemaining);
            }
        }
    }

    private void BuildLayout()
    {
        CustomMinimumSize = new Vector2(1280, 60);
        Size = new Vector2(1280, 60);

        // TopBar background texture
        var styleBox = new StyleBoxTexture
        {
            Texture = GD.Load<Texture2D>("res://assets/ui/hub/topbar_bg.png"),
            ExpandMarginTop = 0,
            ExpandMarginBottom = 0
        };
        AddThemeStyleboxOverride("panel", styleBox);

        var marginContainer = new MarginContainer();
        marginContainer.AddThemeConstantOverride("margin_left", 14);
        marginContainer.AddThemeConstantOverride("margin_right", 14);
        marginContainer.AddThemeConstantOverride("margin_top", 4);
        marginContainer.AddThemeConstantOverride("margin_bottom", 4);
        AddChild(marginContainer);

        var hBox = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Begin
        };
        hBox.AddThemeConstantOverride("separation", 16);
        marginContainer.AddChild(hBox);

        // --- 1. LEFT SECTION: AVATAR & PLAYER INFO ---
        var leftBox = new HBoxContainer();
        leftBox.AddThemeConstantOverride("separation", 10);
        hBox.AddChild(leftBox);

        // Avatar container with frame
        var avatarContainer = new Control
        {
            CustomMinimumSize = new Vector2(52, 52)
        };
        leftBox.AddChild(avatarContainer);

        _avatarRect = new TextureRect
        {
            Position = new Vector2(4, 4),
            Size = new Vector2(44, 44),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Texture = LoadHeroAvatar("lu_bo")
        };
        avatarContainer.AddChild(_avatarRect);

        var avatarFrame = new TextureRect
        {
            Position = new Vector2(0, 0),
            Size = new Vector2(52, 52),
            Texture = GD.Load<Texture2D>("res://assets/ui/hub/topbar_avatar_frame.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            MouseFilter = MouseFilterEnum.Ignore
        };
        avatarContainer.AddChild(avatarFrame);

        _avatarBtn = new TextureButton
        {
            Position = new Vector2(0, 0),
            Size = new Vector2(52, 52)
        };
        _avatarBtn.Pressed += () => EmitSignal(SignalName.AvatarClicked);
        avatarContainer.AddChild(_avatarBtn);

        // Info VBox (Player name, VIP, Level, Title)
        var infoVBox = new VBoxContainer();
        infoVBox.AddThemeConstantOverride("separation", 2);
        infoVBox.Alignment = BoxContainer.AlignmentMode.Center;
        leftBox.AddChild(infoVBox);

        var row1 = new HBoxContainer();
        row1.AddThemeConstantOverride("separation", 6);
        infoVBox.AddChild(row1);

        _playerNameLabel = new Label
        {
            Text = "Chúa Công",
            Modulate = new Color(1f, 0.95f, 0.8f)
        };
        _playerNameLabel.AddThemeFontSizeOverride("font_size", 14);
        row1.AddChild(_playerNameLabel);

        _vipBadgeRect = new TextureRect
        {
            CustomMinimumSize = new Vector2(40, 20),
            Size = new Vector2(40, 20),
            Texture = GD.Load<Texture2D>("res://assets/ui/hub/vip_badge_11.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
        };
        row1.AddChild(_vipBadgeRect);

        var row2 = new HBoxContainer();
        row2.AddThemeConstantOverride("separation", 8);
        infoVBox.AddChild(row2);

        _levelLabel = new Label
        {
            Text = "Lv.80",
            Modulate = new Color(0.4f, 0.9f, 1f)
        };
        _levelLabel.AddThemeFontSizeOverride("font_size", 12);
        row2.AddChild(_levelLabel);

        _titleLabel = new Label
        {
            Text = "[Địa Chủ]",
            Modulate = new Color(1f, 0.85f, 0.2f)
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 12);
        row2.AddChild(_titleLabel);

        // Spacer to middle
        var spacer1 = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        hBox.AddChild(spacer1);

        // --- 2. MIDDLE SECTION: CURRENCY BAR ---
        var currencyBox = new HBoxContainer();
        currencyBox.AddThemeConstantOverride("separation", 20);
        currencyBox.Alignment = BoxContainer.AlignmentMode.Center;
        hBox.AddChild(currencyBox);

        // Gold Container
        var goldBox = CreateCurrencyItem("res://assets/ui/hub/currency_icon_gold.png", out _goldLabel);
        currencyBox.AddChild(goldBox);

        // Diamond Container
        var diamondBox = CreateCurrencyItem("res://assets/ui/hub/currency_icon_diamond.png", out _diamondLabel);
        currencyBox.AddChild(diamondBox);

        // Stamina Container (Icon + Progress + Label + Countdown)
        var staminaBox = new HBoxContainer();
        staminaBox.AddThemeConstantOverride("separation", 6);
        staminaBox.Alignment = BoxContainer.AlignmentMode.Center;
        currencyBox.AddChild(staminaBox);

        var staminaIcon = new TextureRect
        {
            CustomMinimumSize = new Vector2(24, 24),
            Texture = GD.Load<Texture2D>("res://assets/ui/hub/currency_icon_stamina.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
        };
        staminaBox.AddChild(staminaIcon);

        var staminaBarContainer = new VBoxContainer();
        staminaBarContainer.AddThemeConstantOverride("separation", 1);
        staminaBarContainer.Alignment = BoxContainer.AlignmentMode.Center;
        staminaBox.AddChild(staminaBarContainer);

        var staminaHeader = new HBoxContainer();
        staminaHeader.AddThemeConstantOverride("separation", 6);
        staminaBarContainer.AddChild(staminaHeader);

        _staminaLabel = new Label
        {
            Text = "100/145",
            Modulate = new Color(0.95f, 0.95f, 0.95f)
        };
        _staminaLabel.AddThemeFontSizeOverride("font_size", 12);
        staminaHeader.AddChild(_staminaLabel);

        _staminaTimerLabel = new Label
        {
            Text = "24:43",
            Modulate = new Color(0.7f, 0.7f, 0.75f)
        };
        _staminaTimerLabel.AddThemeFontSizeOverride("font_size", 10);
        staminaHeader.AddChild(_staminaTimerLabel);

        _staminaBar = new ProgressBar
        {
            CustomMinimumSize = new Vector2(90, 6),
            MinValue = 0,
            MaxValue = 145,
            Value = 100,
            ShowPercentage = false
        };
        var bgStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.08f, 0.15f),
            CornerRadiusBottomLeft = 3,
            CornerRadiusBottomRight = 3,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3
        };
        var fillStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.85f, 0.25f, 0.28f),
            CornerRadiusBottomLeft = 3,
            CornerRadiusBottomRight = 3,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3
        };
        _staminaBar.AddThemeStyleboxOverride("background", bgStyle);
        _staminaBar.AddThemeStyleboxOverride("fill", fillStyle);
        staminaBarContainer.AddChild(_staminaBar);

        // Spacer to right
        var spacer2 = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        hBox.AddChild(spacer2);

        // --- 3. RIGHT SECTION: QUICK SHORTCUTS ---
        var rightBox = new HBoxContainer();
        rightBox.AddThemeConstantOverride("separation", 10);
        rightBox.Alignment = BoxContainer.AlignmentMode.End;
        hBox.AddChild(rightBox);

        var mailBtn = CreateIconButton("✉", "Hòm Thư");
        mailBtn.Pressed += () => EmitSignal(SignalName.MailClicked);
        rightBox.AddChild(mailBtn);

        var settingsBtn = CreateIconButton("⚙", "Cài Đặt");
        settingsBtn.Pressed += () => EmitSignal(SignalName.SettingsClicked);
        rightBox.AddChild(settingsBtn);
    }

    private Control CreateCurrencyItem(string iconPath, out Label labelRef)
    {
        var box = new HBoxContainer();
        box.AddThemeConstantOverride("separation", 6);
        box.Alignment = BoxContainer.AlignmentMode.Center;

        var icon = new TextureRect
        {
            CustomMinimumSize = new Vector2(24, 24),
            Texture = GD.Load<Texture2D>(iconPath),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
        };
        box.AddChild(icon);

        var label = new Label
        {
            Text = "0",
            Modulate = new Color(1f, 0.95f, 0.8f)
        };
        label.AddThemeFontSizeOverride("font_size", 13);
        box.AddChild(label);
        labelRef = label;

        return box;
    }

    private Button CreateIconButton(string text, string tooltip)
    {
        var btn = new Button
        {
            Text = text,
            TooltipText = tooltip,
            CustomMinimumSize = new Vector2(34, 34)
        };
        var normalBox = new StyleBoxFlat
        {
            BgColor = new Color(0.2f, 0.16f, 0.24f, 0.9f),
            BorderColor = new Color(0.7f, 0.55f, 0.25f),
            BorderWidthBottom = 1,
            BorderWidthTop = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4
        };
        btn.AddThemeStyleboxOverride("normal", normalBox);
        return btn;
    }

    private void ConnectSignals()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.GoldChanged += (delta, total) => UpdateGold(total);
            EventBus.Instance.DiamondChanged += (delta, total) => UpdateDiamond(total);
            EventBus.Instance.StaminaChanged += (cur, max) => UpdateStamina(cur, max);
            EventBus.Instance.VipLevelChanged += UpdateVipBadge;
            EventBus.Instance.LevelUp += (lvl) => _levelLabel.Text = $"Lv.{lvl}";
            EventBus.Instance.TitleChanged += (title) => _titleLabel.Text = title;
        }
    }

    public void RefreshFromGameManager()
    {
        if (GameManager.Instance == null) return;

        var profile = GameManager.Instance.Profile ?? PlayerProfile.CreateDefault();
        UpdateProfile(profile);
    }

    public void UpdateProfile(PlayerProfile profile)
    {
        if (profile == null) return;

        _playerNameLabel.Text = profile.PlayerName;
        _levelLabel.Text = $"Lv.{profile.Level}";
        _titleLabel.Text = profile.CurrentTitle;
        UpdateVipBadge(profile.VipLevel);
        UpdateCurrency(profile.Gold, profile.Diamond, profile.Stamina, profile.MaxStamina);

        if (!string.IsNullOrEmpty(profile.AvatarHeroId))
        {
            _avatarRect.Texture = LoadHeroAvatar(profile.AvatarHeroId);
        }

        var remaining = (int)(profile.StaminaRefillTime - DateTime.Now).TotalSeconds;
        StartStaminaTimer(Math.Max(0, remaining));
    }

    public void UpdateCurrency(long gold, int diamond, int stamina, int maxStamina)
    {
        UpdateGold(gold);
        UpdateDiamond(diamond);
        UpdateStamina(stamina, maxStamina);
    }

    public void UpdateGold(long gold)
    {
        if (_goldLabel == null) return;
        _goldLabel.Text = FormatNumber(gold);
    }

    public void UpdateDiamond(int diamond)
    {
        if (_diamondLabel == null) return;
        _diamondLabel.Text = FormatNumber(diamond);
    }

    public void UpdateStamina(int stamina, int maxStamina)
    {
        if (_staminaLabel == null || _staminaBar == null) return;
        _staminaLabel.Text = $"{stamina}/{maxStamina}";
        _staminaBar.MaxValue = maxStamina;
        _staminaBar.Value = stamina;
    }

    public void UpdateVipBadge(int vipLevel)
    {
        int clamped = Mathf.Clamp(vipLevel, 1, 15);
        string path = $"res://assets/ui/hub/vip_badge_{clamped}.png";
        if (ResourceLoader.Exists(path))
        {
            _vipBadgeRect.Texture = GD.Load<Texture2D>(path);
        }
    }

    public void StartStaminaTimer(int secondsRemaining)
    {
        _staminaSecondsRemaining = secondsRemaining;
        UpdateStaminaTimerDisplay();
    }

    private void UpdateStaminaTimerDisplay()
    {
        if (_staminaTimerLabel == null) return;
        int m = _staminaSecondsRemaining / 60;
        int s = _staminaSecondsRemaining % 60;
        _staminaTimerLabel.Text = $"{m:D2}:{s:D2}";
    }

    private string FormatNumber(long val)
    {
        if (val >= 10000)
        {
            double v = val / 10000.0;
            return $"{v:0.#} vạn";
        }
        return $"{val:N0}";
    }

    private Texture2D LoadHeroAvatar(string heroId)
    {
        string path = $"res://assets/heroes/{heroId}/portrait.png";
        if (ResourceLoader.Exists(path))
        {
            return GD.Load<Texture2D>(path);
        }
        // Fallback to icon
        string iconPath = $"res://assets/heroes/{heroId}/icon.png";
        if (ResourceLoader.Exists(iconPath))
        {
            return GD.Load<Texture2D>(iconPath);
        }
        return GD.Load<Texture2D>("res://assets/ui/hub/icon_hero.png");
    }
}
