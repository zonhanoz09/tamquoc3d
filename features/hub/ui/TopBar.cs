using System;
using Godot;
using TamQuoc3D.Core.Autoload;
using TamQuoc3D.Core.Network;
using TamQuoc3D.Data.Database;
using TamQuoc3D.Data.Resources;

namespace TamQuoc3D.Features.Hub.Ui;

/// <summary>
/// TopBar - Thanh thông tin đỉnh màn hình hiển thị Avatar, Cấp độ, VIP, Danh hiệu, Tiền tệ (Vàng, KNB, Thể lực), đếm ngược hồi thể lực và trạng thái máy chủ.
/// </summary>
[GlobalClass]
public partial class TopBar : PanelContainer
{
    [Signal] public delegate void AvatarClickedEventHandler();
    [Signal] public delegate void SettingsClickedEventHandler();
    [Signal] public delegate void MailClickedEventHandler();
    [Signal] public delegate void DiamondPlusClickedEventHandler();

    private Label _mailBadgeLabel;

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
    private Label _serverBadge;

    private int _staminaSecondsRemaining = 1483;
    private double _timerAccumulator = 0.0;

    // Delegates for safe detachment
    private EventBus.GoldChangedEventHandler _onGoldChanged;
    private EventBus.DiamondChangedEventHandler _onDiamondChanged;
    private EventBus.StaminaChangedEventHandler _onStaminaChanged;
    private EventBus.VipLevelChangedEventHandler _onVipChanged;
    private EventBus.LevelUpEventHandler _onLevelUp;
    private EventBus.TitleChangedEventHandler _onTitleChanged;
    private NetworkManager.DatabaseConnectionChangedEventHandler _onDatabaseConnectionChanged;
    private NetworkManager.DataSyncedEventHandler _onDataSynced;

    public override void _Ready()
    {
        BuildLayout();
        ConnectSignals();
        RefreshFromGameManager();
    }

    public override void _ExitTree()
    {
        DisconnectSignals();
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
            Texture = LoadHeroAvatar("default_hero")
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
            Text = "user name",
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
            Text = "Lv.1",
            Modulate = new Color(0.4f, 0.9f, 1f)
        };
        _levelLabel.AddThemeFontSizeOverride("font_size", 12);
        row2.AddChild(_levelLabel);

        _titleLabel = new Label
        {
            Text = string.Empty,
            Modulate = new Color(1f, 0.8f, 0.35f)
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 12);
        row2.AddChild(_titleLabel);

        // Spacer to right
        var spacer2 = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        hBox.AddChild(spacer2);

        // --- 2. CURRENCY SECTION: stamina / silver (gold) / diamond ---
        var currencyBox = new HBoxContainer();
        currencyBox.AddThemeConstantOverride("separation", 14);
        currencyBox.Alignment = BoxContainer.AlignmentMode.Center;
        hBox.AddChild(currencyBox);

        // Stamina: icon + bar + value + refill timer
        var staminaBox = new HBoxContainer();
        staminaBox.AddThemeConstantOverride("separation", 4);
        currencyBox.AddChild(staminaBox);
        staminaBox.AddChild(CreateCurrencyIcon("res://assets/ui/hub/currency_icon_stamina.png"));
        var staminaVBox = new VBoxContainer();
        staminaVBox.AddThemeConstantOverride("separation", 0);
        staminaBox.AddChild(staminaVBox);
        var staminaRow = new HBoxContainer();
        staminaRow.AddThemeConstantOverride("separation", 6);
        staminaVBox.AddChild(staminaRow);
        _staminaLabel = new Label { Text = "0/0" };
        _staminaLabel.AddThemeFontSizeOverride("font_size", 12);
        staminaRow.AddChild(_staminaLabel);
        _staminaTimerLabel = new Label
        {
            Text = string.Empty,
            Modulate = new Color(0.75f, 0.75f, 0.75f)
        };
        _staminaTimerLabel.AddThemeFontSizeOverride("font_size", 10);
        staminaRow.AddChild(_staminaTimerLabel);
        _staminaBar = new ProgressBar
        {
            CustomMinimumSize = new Vector2(90, 6),
            Size = new Vector2(90, 6),
            ShowPercentage = false,
            MaxValue = 100,
            Value = 0
        };
        staminaVBox.AddChild(_staminaBar);

        // Silver (Gold)
        var goldBox = new HBoxContainer();
        goldBox.AddThemeConstantOverride("separation", 4);
        currencyBox.AddChild(goldBox);
        goldBox.AddChild(CreateCurrencyIcon("res://assets/ui/hub/currency_icon_gold.png"));
        _goldLabel = new Label { Text = "0" };
        _goldLabel.AddThemeFontSizeOverride("font_size", 13);
        goldBox.AddChild(_goldLabel);

        // Diamond (Gems) + plus button -> shop
        var diamondBox = new HBoxContainer();
        diamondBox.AddThemeConstantOverride("separation", 4);
        currencyBox.AddChild(diamondBox);
        diamondBox.AddChild(CreateCurrencyIcon("res://assets/ui/hub/currency_icon_diamond.png"));
        _diamondLabel = new Label { Text = "0" };
        _diamondLabel.AddThemeFontSizeOverride("font_size", 13);
        diamondBox.AddChild(_diamondLabel);
        var plusBtn = new Button
        {
            Text = "+",
            TooltipText = "Tới Cửa Hàng",
            CustomMinimumSize = new Vector2(20, 20)
        };
        plusBtn.Pressed += () => EmitSignal(SignalName.DiamondPlusClicked);
        diamondBox.AddChild(plusBtn);

        // --- 3. RIGHT SECTION: MAIL, SETTINGS & SERVER BADGE ---
        var rightBox = new HBoxContainer();
        rightBox.AddThemeConstantOverride("separation", 10);
        rightBox.Alignment = BoxContainer.AlignmentMode.End;
        hBox.AddChild(rightBox);

        // Mail button with unread badge
        var mailContainer = new Control { CustomMinimumSize = new Vector2(34, 34) };
        var mailBtn = CreateIconButton("✉", "Hòm Thư");
        mailBtn.Pressed += () => EmitSignal(SignalName.MailClicked);
        mailContainer.AddChild(mailBtn);
        _mailBadgeLabel = new Label
        {
            Text = string.Empty,
            Position = new Vector2(20, -4),
            Modulate = new Color(1f, 0.35f, 0.3f),
            Visible = false
        };
        _mailBadgeLabel.AddThemeFontSizeOverride("font_size", 11);
        mailContainer.AddChild(_mailBadgeLabel);
        rightBox.AddChild(mailContainer);

        var settingsBtn = CreateIconButton("⚙", "Cài Đặt");
        settingsBtn.Pressed += () => EmitSignal(SignalName.SettingsClicked);
        rightBox.AddChild(settingsBtn);

        _serverBadge = new Label
        {
            Text = string.Empty,
            Modulate = new Color(0.6f, 0.6f, 0.6f)
        };
        _serverBadge.AddThemeFontSizeOverride("font_size", 10);
        rightBox.AddChild(_serverBadge);
    }

    private static TextureRect CreateCurrencyIcon(string path)
    {
        return new TextureRect
        {
            Texture = GD.Load<Texture2D>(path),
            CustomMinimumSize = new Vector2(20, 20),
            Size = new Vector2(20, 20),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
        };
    }

    /// <summary>So thu chua doc hien tren nut Hom Thu (0 = an badge).</summary>
    public void SetMailBadge(int unreadCount)
    {
        if (_mailBadgeLabel == null) return;
        _mailBadgeLabel.Visible = unreadCount > 0;
        _mailBadgeLabel.Text = unreadCount > 9 ? "9+" : unreadCount.ToString();
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
            _onGoldChanged = (delta, total) => { if (IsInstanceValid(this) && _goldLabel != null) UpdateGold(total); };
            _onDiamondChanged = (delta, total) => { if (IsInstanceValid(this) && _diamondLabel != null) UpdateDiamond(total); };
            _onStaminaChanged = (cur, max) => { if (IsInstanceValid(this) && _staminaLabel != null) UpdateStamina(cur, max); };
            _onVipChanged = (vip) => { if (IsInstanceValid(this) && _vipBadgeRect != null) UpdateVipBadge(vip); };
            _onLevelUp = (lvl) => { if (IsInstanceValid(this) && _levelLabel != null) _levelLabel.Text = $"Lv.{lvl}"; };
            _onTitleChanged = (title) => { if (IsInstanceValid(this) && _titleLabel != null) _titleLabel.Text = title; };

            EventBus.Instance.GoldChanged += _onGoldChanged;
            EventBus.Instance.DiamondChanged += _onDiamondChanged;
            EventBus.Instance.StaminaChanged += _onStaminaChanged;
            EventBus.Instance.VipLevelChanged += _onVipChanged;
            EventBus.Instance.LevelUp += _onLevelUp;
            EventBus.Instance.TitleChanged += _onTitleChanged;
        }

        if (NetworkManager.Instance != null)
        {
            _onDatabaseConnectionChanged = (isConnected) => { if (IsInstanceValid(this) && _serverBadge != null) UpdateDatabaseBadge(isConnected); };
            _onDataSynced = () => { if (IsInstanceValid(this)) RefreshFromGameManager(); };

            UpdateDatabaseBadge(NetworkManager.Instance.IsDatabaseConnected);
            NetworkManager.Instance.DatabaseConnectionChanged += _onDatabaseConnectionChanged;
            NetworkManager.Instance.DataSynced += _onDataSynced;
        }
    }

    private void DisconnectSignals()
    {
        if (EventBus.Instance != null)
        {
            if (_onGoldChanged != null) EventBus.Instance.GoldChanged -= _onGoldChanged;
            if (_onDiamondChanged != null) EventBus.Instance.DiamondChanged -= _onDiamondChanged;
            if (_onStaminaChanged != null) EventBus.Instance.StaminaChanged -= _onStaminaChanged;
            if (_onVipChanged != null) EventBus.Instance.VipLevelChanged -= _onVipChanged;
            if (_onLevelUp != null) EventBus.Instance.LevelUp -= _onLevelUp;
            if (_onTitleChanged != null) EventBus.Instance.TitleChanged -= _onTitleChanged;
        }

        if (NetworkManager.Instance != null)
        {
            if (_onDatabaseConnectionChanged != null) NetworkManager.Instance.DatabaseConnectionChanged -= _onDatabaseConnectionChanged;
            if (_onDataSynced != null) NetworkManager.Instance.DataSynced -= _onDataSynced;
        }
    }

    private void UpdateDatabaseBadge(bool isConnected)
    {
        if (_serverBadge == null) return;
        if (isConnected)
        {
            _serverBadge.Text = "● DB: Connected";
            _serverBadge.TooltipText = "Game Server đã kết nối PostgreSQL";
            _serverBadge.Modulate = new Color(0.2f, 0.9f, 0.4f);
        }
        else
        {
            _serverBadge.Text = "● DB: Offline";
            _serverBadge.TooltipText = "Không kết nối được PostgreSQL qua Game Server";
            _serverBadge.Modulate = new Color(0.6f, 0.6f, 0.6f);
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
