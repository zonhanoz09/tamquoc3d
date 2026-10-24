using Godot;
using System;
using TamQuoc3D.Core.Autoload;
using TamQuoc3D.Core.Network;

/// <summary>
/// Thẻ người chơi góc trái trên của hub (§6): dồn trái, fix cứng, luôn hiển thị.
/// Nội dung: avatar + tên + cấp + danh hiệu + lực chiến + tiền tệ + thanh thể lực.
/// Tách từ TopBar full-width cũ: PlayerCard chỉ giữ phần thông tin người chơi;
/// nút Thư/Cài Đặt đã chuyển sang cụm icon phải (IconDock).
/// </summary>
[GlobalClass]
public partial class PlayerCard : PanelContainer
{
    [Signal] public delegate void AvatarClickedEventHandler();
    [Signal] public delegate void DiamondPlusClickedEventHandler();

    private const int CardWidth = 365;
    private const int CardHeight = 135;
    private const int AvatarSize = 76;

    private TextureRect _avatarRect;
    private Label _nameLabel;
    private TextureRect _vipBadge;
    private Label _levelLabel;
    private Label _titleLabel;
    private Label _powerLabel;
    private TextureRect _staminaIcon;
    private ProgressBar _staminaBar;
    private Label _staminaLabel;
    private Label _staminaTimerLabel;
    private TextureRect _goldIcon;
    private Label _goldLabel;
    private TextureRect _diamondIcon;
    private Label _diamondLabel;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(CardWidth, CardHeight);
        Size = new Vector2(CardWidth, CardHeight);
        MouseFilter = MouseFilterEnum.Stop;

        var bg = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.05f, 0.12f, 0.72f),
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8
        };
        AddThemeStyleboxOverride("panel", bg);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        AddChild(margin);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 8);
        margin.AddChild(hbox);

        // Avatar + khung.
        var avatarContainer = new Control();
        avatarContainer.CustomMinimumSize = new Vector2(AvatarSize, AvatarSize);
        avatarContainer.Size = new Vector2(AvatarSize, AvatarSize);
        hbox.AddChild(avatarContainer);

        _avatarRect = new TextureRect();
        _avatarRect.CustomMinimumSize = new Vector2(AvatarSize, AvatarSize);
        _avatarRect.Size = new Vector2(AvatarSize, AvatarSize);
        _avatarRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        _avatarRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;
        avatarContainer.AddChild(_avatarRect);

        var frameRect = new TextureRect();
        frameRect.Texture = GD.Load<Texture2D>("res://assets/ui/hub/topbar_avatar_frame.png");
        frameRect.CustomMinimumSize = new Vector2(AvatarSize, AvatarSize);
        frameRect.Size = new Vector2(AvatarSize, AvatarSize);
        frameRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        frameRect.StretchMode = TextureRect.StretchModeEnum.KeepAspect;
        avatarContainer.AddChild(frameRect);

        var avatarButton = new Button();
        avatarButton.CustomMinimumSize = new Vector2(AvatarSize, AvatarSize);
        avatarButton.Size = new Vector2(AvatarSize, AvatarSize);
        avatarButton.Flat = true;
        avatarButton.AddThemeStyleboxOverride("normal", new StyleBoxEmpty());
        avatarButton.AddThemeStyleboxOverride("hover", new StyleBoxEmpty());
        avatarButton.AddThemeStyleboxOverride("pressed", new StyleBoxEmpty());
        avatarButton.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
        avatarButton.Pressed += () => EmitSignal(SignalName.AvatarClicked);
        avatarContainer.AddChild(avatarButton);

        // Cột thông tin.
        var infoBox = new VBoxContainer();
        infoBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        infoBox.AddThemeConstantOverride("separation", 3);
        hbox.AddChild(infoBox);

        var nameRow = new HBoxContainer();
        nameRow.AddThemeConstantOverride("separation", 6);
        infoBox.AddChild(nameRow);
        _nameLabel = new Label();
        _nameLabel.AddThemeFontSizeOverride("font_size", 16);
        _nameLabel.AddThemeColorOverride("font_color", Colors.White);
        nameRow.AddChild(_nameLabel);
        _vipBadge = new TextureRect();
        _vipBadge.CustomMinimumSize = new Vector2(28, 14);
        _vipBadge.Size = new Vector2(28, 14);
        _vipBadge.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        _vipBadge.StretchMode = TextureRect.StretchModeEnum.KeepAspect;
        nameRow.AddChild(_vipBadge);

        var levelRow = new HBoxContainer();
        levelRow.AddThemeConstantOverride("separation", 8);
        infoBox.AddChild(levelRow);
        _levelLabel = new Label();
        _levelLabel.AddThemeFontSizeOverride("font_size", 13);
        _levelLabel.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.4f));
        levelRow.AddChild(_levelLabel);
        _titleLabel = new Label();
        _titleLabel.AddThemeFontSizeOverride("font_size", 13);
        _titleLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.9f, 1f));
        _titleLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        levelRow.AddChild(_titleLabel);
        _powerLabel = new Label();
        _powerLabel.AddThemeFontSizeOverride("font_size", 13);
        _powerLabel.AddThemeColorOverride("font_color", new Color(1f, 0.6f, 0.6f));
        levelRow.AddChild(_powerLabel);

        var staminaRow = new HBoxContainer();
        staminaRow.AddThemeConstantOverride("separation", 4);
        infoBox.AddChild(staminaRow);
        _staminaIcon = new TextureRect();
        _staminaIcon.Texture = GD.Load<Texture2D>("res://assets/ui/hub/currency_icon_stamina.png");
        _staminaIcon.CustomMinimumSize = new Vector2(16, 16);
        _staminaIcon.Size = new Vector2(16, 16);
        _staminaIcon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        _staminaIcon.StretchMode = TextureRect.StretchModeEnum.KeepAspect;
        staminaRow.AddChild(_staminaIcon);
        _staminaBar = new ProgressBar();
        _staminaBar.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _staminaBar.CustomMinimumSize = new Vector2(80, 12);
        _staminaBar.ShowPercentage = false;
        _staminaBar.AddThemeStyleboxOverride("background", BuildBarBg());
        _staminaBar.AddThemeStyleboxOverride("fill", BuildBarFill(new Color(0.2f, 0.8f, 0.4f)));
        staminaRow.AddChild(_staminaBar);
        _staminaLabel = new Label();
        _staminaLabel.AddThemeFontSizeOverride("font_size", 11);
        _staminaLabel.AddThemeColorOverride("font_color", Colors.White);
        staminaRow.AddChild(_staminaLabel);
        _staminaTimerLabel = new Label();
        _staminaTimerLabel.AddThemeFontSizeOverride("font_size", 11);
        _staminaTimerLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
        staminaRow.AddChild(_staminaTimerLabel);

        var currencyRow = new HBoxContainer();
        currencyRow.AddThemeConstantOverride("separation", 4);
        infoBox.AddChild(currencyRow);
        _goldIcon = new TextureRect();
        _goldIcon.Texture = GD.Load<Texture2D>("res://assets/ui/hub/currency_icon_gold.png");
        _goldIcon.CustomMinimumSize = new Vector2(16, 16);
        _goldIcon.Size = new Vector2(16, 16);
        _goldIcon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        _goldIcon.StretchMode = TextureRect.StretchModeEnum.KeepAspect;
        currencyRow.AddChild(_goldIcon);
        _goldLabel = new Label();
        _goldLabel.AddThemeFontSizeOverride("font_size", 12);
        _goldLabel.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.4f));
        _goldLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        currencyRow.AddChild(_goldLabel);
        _diamondIcon = new TextureRect();
        _diamondIcon.Texture = GD.Load<Texture2D>("res://assets/ui/hub/currency_icon_diamond.png");
        _diamondIcon.CustomMinimumSize = new Vector2(16, 16);
        _diamondIcon.Size = new Vector2(16, 16);
        _diamondIcon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        _diamondIcon.StretchMode = TextureRect.StretchModeEnum.KeepAspect;
        currencyRow.AddChild(_diamondIcon);
        _diamondLabel = new Label();
        _diamondLabel.AddThemeFontSizeOverride("font_size", 12);
        _diamondLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.9f, 1f));
        currencyRow.AddChild(_diamondLabel);
        var diamondPlus = new Button();
        diamondPlus.Text = "+";
        diamondPlus.CustomMinimumSize = new Vector2(20, 20);
        diamondPlus.Size = new Vector2(20, 20);
        diamondPlus.Flat = true;
        diamondPlus.AddThemeFontSizeOverride("font_size", 14);
        diamondPlus.AddThemeColorOverride("font_color", new Color(0.7f, 0.9f, 1f));
        diamondPlus.Pressed += () => EmitSignal(SignalName.DiamondPlusClicked);
        currencyRow.AddChild(diamondPlus);

        if (GameManager.Instance != null)
        {
            RefreshFromGameManager();
        }

        // Cung cach TopBar cu: EventBus cho tien te/cap/VIP/danh hieu,
        // NetworkManager.DataSynced cho dong bo tai khoan.
        if (EventBus.Instance != null)
        {
            EventBus.Instance.GoldChanged += (_, newValue) => UpdateGold(newValue);
            EventBus.Instance.DiamondChanged += (_, newValue) => UpdateDiamond(newValue);
            EventBus.Instance.StaminaChanged += (current, max) => UpdateStamina(current, max);
            EventBus.Instance.VipLevelChanged += UpdateVipBadge;
            EventBus.Instance.LevelUp += _ => RefreshFromGameManager();
            EventBus.Instance.TitleChanged += title => _titleLabel.Text = title;
        }
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.DataSynced += RefreshFromGameManager;
        }
    }

    private int _staminaSecondsRemaining = 1483;
    private double _timerAccumulator;

    public override void _Process(double delta)
    {
        _timerAccumulator += delta;
        if (_timerAccumulator >= 1.0)
        {
            _timerAccumulator -= 1.0;
            if (_staminaSecondsRemaining > 0)
            {
                _staminaSecondsRemaining--;
                var minutes = _staminaSecondsRemaining / 60;
                var seconds = _staminaSecondsRemaining % 60;
                _staminaTimerLabel.Text = $"{minutes:00}:{seconds:00}";
            }
        }
    }

    public void RefreshFromGameManager()
    {
        var manager = GameManager.Instance;
        if (manager == null || manager.Profile == null) return;
        var profile = manager.Profile;
        _nameLabel.Text = profile.PlayerName;
        _levelLabel.Text = $"Lv.{profile.Level}";
        _titleLabel.Text = profile.CurrentTitle;
        _powerLabel.Text = $"Lực chiến {FormatNumber(profile.CombatPower)}";
        UpdateGold((int)manager.Gold);
        UpdateDiamond(manager.Gems);
        UpdateStamina(manager.Stamina, manager.MaxStamina);
        UpdateVipBadge(profile.VipLevel);
        _avatarRect.Texture = LoadHeroAvatar(profile.AvatarHeroId);
    }

    public void UpdateGold(int gold) => _goldLabel.Text = FormatNumber(gold);
    public void UpdateDiamond(int gems) => _diamondLabel.Text = FormatNumber(gems);

    public void UpdateStamina(int current, int max)
    {
        _staminaBar.MaxValue = Math.Max(1, max);
        _staminaBar.Value = Math.Clamp(current, 0, max);
        _staminaLabel.Text = $"{current}/{max}";
    }

    public void UpdateVipBadge(int vipLevel)
    {
        var path = $"res://assets/ui/hub/vip_badge_{vipLevel}.png";
        _vipBadge.Texture = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
    }

    private static Texture2D LoadHeroAvatar(string heroId)
    {
        var portraitPath = $"res://assets/heroes/{heroId}/portrait.png";
        if (ResourceLoader.Exists(portraitPath))
        {
            return GD.Load<Texture2D>(portraitPath);
        }
        var iconPath = $"res://assets/heroes/{heroId}/icon.png";
        if (ResourceLoader.Exists(iconPath))
        {
            return GD.Load<Texture2D>(iconPath);
        }
        return GD.Load<Texture2D>("res://assets/ui/hub/icon_hero.png");
    }

    private static StyleBoxFlat BuildBarBg() => new()
    {
        BgColor = new Color(0.15f, 0.15f, 0.2f, 1f),
        CornerRadiusTopLeft = 3,
        CornerRadiusTopRight = 3,
        CornerRadiusBottomLeft = 3,
        CornerRadiusBottomRight = 3
    };

    private static StyleBoxFlat BuildBarFill(Color color) => new()
    {
        BgColor = color,
        CornerRadiusTopLeft = 3,
        CornerRadiusTopRight = 3,
        CornerRadiusBottomLeft = 3,
        CornerRadiusBottomRight = 3
    };

    private static string FormatNumber(long value)
    {
        if (value >= 1_000_000) return $"{value / 1_000_000.0:F1}M";
        if (value >= 1_000) return $"{value / 1_000.0:F1}K";
        return value.ToString();
    }
}
