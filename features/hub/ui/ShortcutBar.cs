using System.Collections.Generic;
using Godot;

namespace TamQuoc3D.Features.Hub.Ui;

/// <summary>
/// ShortcutBar - Dãy 10 icon tròn lối tắt hoạt động/sự kiện phong cách Long Tướng MMO cổ phong.
/// </summary>
[GlobalClass]
public partial class ShortcutBar : HBoxContainer
{
    [Signal]
    public delegate void ShortcutClickedEventHandler(string shortcutId);

    public class ShortcutConfig
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string IconPath { get; set; }
        public bool HasNotification { get; set; }
    }

    private readonly Dictionary<string, (TextureButton btn, TextureRect dot)> _buttons = new();

    public override void _Ready()
    {
        Alignment = AlignmentMode.Center;
        AddThemeConstantOverride("separation", 14);

        var configs = new List<ShortcutConfig>
        {
            new() { Id = "hoat_dong", Name = "Hoạt Động", IconPath = "res://assets/ui/hub/shortcut_hoat_dong.png", HasNotification = true },
            new() { Id = "su_kien", Name = "Sự Kiện", IconPath = "res://assets/ui/hub/shortcut_su_kien.png", HasNotification = true },
            new() { Id = "khanh_dien", Name = "Khánh Điển", IconPath = "res://assets/ui/hub/shortcut_khanh_dien.png", HasNotification = false },
            new() { Id = "dau_gia", Name = "Đấu Giá", IconPath = "res://assets/ui/hub/shortcut_dau_gia.png", HasNotification = false },
            new() { Id = "quoc_gia", Name = "Quốc Gia", IconPath = "res://assets/ui/hub/shortcut_quoc_gia.png", HasNotification = false },
            new() { Id = "tuyet_lo", Name = "Tuyệt Lộ", IconPath = "res://assets/ui/hub/shortcut_tuyet_lo.png", HasNotification = true },
            new() { Id = "long_dien", Name = "Long Điện", IconPath = "res://assets/ui/hub/shortcut_long_dien.png", HasNotification = false },
            new() { Id = "di_canh", Name = "Di Cảnh", IconPath = "res://assets/ui/hub/shortcut_di_canh.png", HasNotification = false },
            new() { Id = "tu_luyen", Name = "Tu Luyện", IconPath = "res://assets/ui/hub/shortcut_tu_luyen.png", HasNotification = false },
            new() { Id = "donate", Name = "Donate & Quà", IconPath = "res://assets/ui/hub/shortcut_donate.png", HasNotification = true },
        };

        foreach (var cfg in configs)
        {
            CreateShortcutNode(cfg);
        }
    }

    private void CreateShortcutNode(ShortcutConfig cfg)
    {
        var itemContainer = new VBoxContainer
        {
            Alignment = AlignmentMode.Center
        };
        itemContainer.AddThemeConstantOverride("separation", 2);

        var btnContainer = new Control
        {
            CustomMinimumSize = new Vector2(56, 56)
        };
        itemContainer.AddChild(btnContainer);

        var button = new TextureButton
        {
            Position = new Vector2(0, 0),
            Size = new Vector2(56, 56),
            TextureNormal = GD.Load<Texture2D>(cfg.IconPath),
            StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
            IgnoreTextureSize = false,
            PivotOffset = new Vector2(28, 28)
        };

        // Hover animation
        button.MouseEntered += () =>
        {
            var tween = CreateTween();
            tween.TweenProperty(button, "scale", new Vector2(1.12f, 1.12f), 0.12f);
        };
        button.MouseExited += () =>
        {
            var tween = CreateTween();
            tween.TweenProperty(button, "scale", Vector2.One, 0.12f);
        };
        button.Pressed += () =>
        {
            var tween = CreateTween();
            tween.TweenProperty(button, "scale", new Vector2(0.92f, 0.92f), 0.06f);
            tween.TweenProperty(button, "scale", Vector2.One, 0.08f);
            EmitSignal(SignalName.ShortcutClicked, cfg.Id);
        };
        btnContainer.AddChild(button);

        // Frame
        var frame = new TextureRect
        {
            Position = new Vector2(0, 0),
            Size = new Vector2(56, 56),
            Texture = GD.Load<Texture2D>("res://assets/ui/hub/shortcut_circle_frame.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            MouseFilter = MouseFilterEnum.Ignore
        };
        btnContainer.AddChild(frame);

        // Notification dot (top right)
        var dot = new TextureRect
        {
            Position = new Vector2(40, 0),
            Size = new Vector2(14, 14),
            Texture = GD.Load<Texture2D>("res://assets/ui/hub/shortcut_notification_dot.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            Visible = cfg.HasNotification,
            MouseFilter = MouseFilterEnum.Ignore
        };
        btnContainer.AddChild(dot);

        // Name Label below
        var nameLabel = new Label
        {
            Text = cfg.Name,
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = new Color(0.95f, 0.9f, 0.8f)
        };
        nameLabel.AddThemeFontSizeOverride("font_size", 11);
        itemContainer.AddChild(nameLabel);

        AddChild(itemContainer);
        _buttons[cfg.Id] = (button, dot);
    }

    public void SetNotification(string shortcutId, bool hasNew)
    {
        if (_buttons.TryGetValue(shortcutId, out var item))
        {
            item.dot.Visible = hasNew;
        }
    }
}
