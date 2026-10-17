using System.Collections.Generic;
using Godot;
using TamQuoc3D.Core.Navigation;

namespace TamQuoc3D.Features.Hub.Ui;

/// <summary>
/// BottomNavBar - Thanh điều hướng dưới đáy màn hình gồm 11 danh mục chính phong cách MMO chiến thuật.
/// </summary>
[GlobalClass]
public partial class BottomNavBar : PanelContainer
{
    [Signal]
    public delegate void TabSelectedEventHandler(string tabId);

    public class TabConfig
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public string IconPath { get; set; }
        public int BadgeCount { get; set; }
    }

    private readonly Dictionary<string, (Button btn, TextureRect activeBar, Label badgeLabel, Control badgeContainer)> _tabs = new();
    private string _currentTabId = string.Empty;

    public override void _Ready()
    {
        BuildLayout();
    }

    private void BuildLayout()
    {
        CustomMinimumSize = new Vector2(1280, 68);
        Size = new Vector2(1280, 68);

        var styleBox = new StyleBoxTexture
        {
            Texture = GD.Load<Texture2D>("res://assets/ui/hub/bottom_nav_bg.png")
        };
        AddThemeStyleboxOverride("panel", styleBox);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_top", 4);
        margin.AddThemeConstantOverride("margin_bottom", 4);
        AddChild(margin);

        var hBox = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        hBox.AddThemeConstantOverride("separation", 10);
        margin.AddChild(hBox);

        var tabConfigs = new List<TabConfig>
        {
            new() { Id = "hero", Label = "Võ Tướng", IconPath = "res://assets/ui/hub/icon_hero.png", BadgeCount = 0 },
            new() { Id = "item", Label = "Bảo Vật", IconPath = "res://assets/ui/hub/icon_item.png", BadgeCount = 0 },
            new() { Id = "arena", Label = "Thí Đấu", IconPath = "res://assets/ui/hub/icon_arena.png", BadgeCount = 3 },
            new() { Id = "campaign", Label = "Địa Chỉ", IconPath = "res://assets/ui/hub/icon_map.png", BadgeCount = 0 },
            new() { Id = "bag", Label = "Túi", IconPath = "res://assets/ui/hub/icon_bag.png", BadgeCount = 0 },
            new() { Id = "guild", Label = "Bang Hội", IconPath = "res://assets/ui/hub/icon_guild.png", BadgeCount = 1 },
            new() { Id = "pvp", Label = "Võ Thành", IconPath = "res://assets/ui/hub/icon_pvp.png", BadgeCount = 0 },
            new() { Id = "pet", Label = "Linh Thú", IconPath = "res://assets/ui/hub/icon_pet.png", BadgeCount = 0 },
            new() { Id = "upgrade", Label = "Cường Hóa", IconPath = "res://assets/ui/hub/icon_upgrade.png", BadgeCount = 2 },
            new() { Id = "formation", Label = "Quân Đoàn", IconPath = "res://assets/ui/hub/icon_formation.png", BadgeCount = 0 },
            new() { Id = "mount", Label = "Thú Cưỡi", IconPath = "res://assets/ui/hub/icon_wedding.png", BadgeCount = 0 }
        };

        foreach (var cfg in tabConfigs)
        {
            CreateTabNode(hBox, cfg);
        }
    }

    private void CreateTabNode(HBoxContainer parent, TabConfig cfg)
    {
        var tabContainer = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(98, 60),
            Alignment = BoxContainer.AlignmentMode.Center
        };
        tabContainer.AddThemeConstantOverride("separation", 2);

        // Active indicator on top of tab
        var activeBar = new TextureRect
        {
            CustomMinimumSize = new Vector2(80, 4),
            Texture = GD.Load<Texture2D>("res://assets/ui/hub/bottom_nav_tab_active.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Modulate = new Color(1, 1, 1, 0) // Hidden by default
        };
        tabContainer.AddChild(activeBar);

        // Button wrapper with Icon & Label
        var btn = new Button
        {
            CustomMinimumSize = new Vector2(94, 50),
            Flat = true
        };
        var normalStyle = new StyleBoxEmpty();
        btn.AddThemeStyleboxOverride("normal", normalStyle);
        btn.AddThemeStyleboxOverride("focus", normalStyle);

        var innerVBox = new VBoxContainer
        {
            Position = new Vector2(0, 0),
            Size = new Vector2(94, 50),
            Alignment = BoxContainer.AlignmentMode.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        innerVBox.AddThemeConstantOverride("separation", 2);
        btn.AddChild(innerVBox);

        // Icon container with badge overlay
        var iconWrapper = new Control
        {
            CustomMinimumSize = new Vector2(32, 28),
            MouseFilter = MouseFilterEnum.Ignore
        };
        innerVBox.AddChild(iconWrapper);

        var iconRect = new TextureRect
        {
            Position = new Vector2(31, 0),
            Size = new Vector2(32, 28),
            Texture = GD.Load<Texture2D>(cfg.IconPath),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore
        };
        iconWrapper.AddChild(iconRect);

        // Badge container (number in red badge)
        var badgeWrapper = new Control
        {
            Position = new Vector2(52, -2),
            Size = new Vector2(18, 18),
            Visible = cfg.BadgeCount > 0,
            MouseFilter = MouseFilterEnum.Ignore
        };
        iconWrapper.AddChild(badgeWrapper);

        var badgeBg = new TextureRect
        {
            Size = new Vector2(18, 18),
            Texture = GD.Load<Texture2D>("res://assets/ui/hub/notification_badge_bg.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            MouseFilter = MouseFilterEnum.Ignore
        };
        badgeWrapper.AddChild(badgeBg);

        var badgeLabel = new Label
        {
            Position = new Vector2(0, 0),
            Size = new Vector2(18, 18),
            Text = cfg.BadgeCount.ToString(),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        badgeLabel.AddThemeFontSizeOverride("font_size", 10);
        badgeWrapper.AddChild(badgeLabel);

        // Label
        var nameLabel = new Label
        {
            Text = cfg.Label,
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = new Color(0.9f, 0.85f, 0.75f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        nameLabel.AddThemeFontSizeOverride("font_size", 12);
        innerVBox.AddChild(nameLabel);

        // Click handler
        btn.Pressed += () =>
        {
            OnTabClicked(cfg.Id);
        };

        btn.MouseEntered += () =>
        {
            nameLabel.Modulate = new Color(1f, 0.95f, 0.4f);
        };
        btn.MouseExited += () =>
        {
            if (_currentTabId != cfg.Id)
            {
                nameLabel.Modulate = new Color(0.9f, 0.85f, 0.75f);
            }
        };

        tabContainer.AddChild(btn);
        parent.AddChild(tabContainer);

        _tabs[cfg.Id] = (btn, activeBar, badgeLabel, badgeWrapper);
    }

    private void OnTabClicked(string tabId)
    {
        SetActiveTab(tabId);
        EmitSignal(SignalName.TabSelected, tabId);

        // Route directly via SceneNavigator for known destinations
        switch (tabId)
        {
            case "formation":
                SceneNavigator.GoToFormationSelector("hub");
                break;
            case "campaign":
                SceneNavigator.GoToCampaign();
                break;
            case "arena":
                SceneNavigator.GoToArena();
                break;
            case "hero":
                SceneNavigator.GoToHeroCollection();
                break;
            case "guild":
                SceneNavigator.GoToGuild();
                break;
        }
    }

    public void SetActiveTab(string tabId)
    {
        _currentTabId = tabId;
        foreach (var (key, val) in _tabs)
        {
            bool isActive = key == tabId;
            var tween = CreateTween();
            tween.TweenProperty(val.activeBar, "modulate:a", isActive ? 1.0f : 0.0f, 0.15f);
        }
    }

    public void SetBadge(string tabId, int count)
    {
        if (_tabs.TryGetValue(tabId, out var item))
        {
            item.badgeContainer.Visible = count > 0;
            item.badgeLabel.Text = count > 99 ? "99+" : count.ToString();
        }
    }
}
