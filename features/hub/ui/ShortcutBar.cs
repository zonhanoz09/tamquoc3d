using System.Collections.Generic;
using Godot;

namespace TamQuoc3D.Features.Hub.Ui;

/// <summary>
/// ShortcutBar - Cot icon chuc nang ben phai hub (2 cot x 4 hang).
/// Moi icon mo dung mot tinh nang that cua game; tinh nang chua lam
/// (Me Cung, Bang Hoi) hien dang khoa "Sap mo" bang icon xam.
/// </summary>
[GlobalClass]
public partial class ShortcutBar : GridContainer
{
    [Signal]
    public delegate void ShortcutClickedEventHandler(string shortcutId);

    public class ShortcutConfig
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string IconPath { get; set; }
        public bool Locked { get; set; }
    }

    private readonly Dictionary<string, (TextureButton btn, TextureRect dot, Label subLabel)> _buttons = new();

    public override void _Ready()
    {
        Columns = 2;
        AddThemeConstantOverride("h_separation", 10);
        AddThemeConstantOverride("v_separation", 6);

        var configs = new List<ShortcutConfig>
        {
            new() { Id = "chieu_mo", Name = "Chiêu Mộ", IconPath = "res://assets/ui/hub/icon_chieu_mo.png" },
            new() { Id = "cua_hang", Name = "Cửa Hàng", IconPath = "res://assets/ui/hub/icon_cua_hang.png" },
            new() { Id = "thap", Name = "Tháp", IconPath = "res://assets/ui/hub/icon_thap.png" },
            new() { Id = "hom_thu", Name = "Hòm Thư", IconPath = "res://assets/ui/hub/icon_hom_thu.png" },
            new() { Id = "su_kien", Name = "Sự Kiện", IconPath = "res://assets/ui/hub/icon_su_kien.png" },
            new() { Id = "cai_dat", Name = "Cài Đặt", IconPath = "res://assets/ui/hub/icon_cai_dat.png" },
            new() { Id = "me_cung", Name = "Mê Cung", IconPath = "res://assets/ui/hub/icon_me_cung_locked.png", Locked = true },
            new() { Id = "bang_hoi", Name = "Bang Hội", IconPath = "res://assets/ui/hub/icon_bang_hoi_locked.png", Locked = true },
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
            Name = $"Shortcut_{cfg.Id}",
            Alignment = BoxContainer.AlignmentMode.Center
        };
        itemContainer.AddThemeConstantOverride("separation", 1);

        var btnContainer = new Control
        {
            CustomMinimumSize = new Vector2(56, 56)
        };
        itemContainer.AddChild(btnContainer);

        var button = new TextureButton
        {
            Name = "IconButton",
            Position = new Vector2(0, 0),
            Size = new Vector2(56, 56),
            TextureNormal = GD.Load<Texture2D>(cfg.IconPath),
            StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
            IgnoreTextureSize = false,
            PivotOffset = new Vector2(28, 28),
            Modulate = cfg.Locked ? new Color(0.75f, 0.75f, 0.75f, 0.9f) : Colors.White,
            TooltipText = cfg.Locked ? $"{cfg.Name} - Sắp mở" : cfg.Name
        };

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
            Visible = false,
            MouseFilter = MouseFilterEnum.Ignore
        };
        btnContainer.AddChild(dot);

        // Name Label below
        var nameLabel = new Label
        {
            Text = cfg.Locked ? $"🔒 {cfg.Name}" : cfg.Name,
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = new Color(0.95f, 0.9f, 0.8f)
        };
        nameLabel.AddThemeFontSizeOverride("font_size", 11);
        itemContainer.AddChild(nameLabel);

        // Sub label (e.g. tower next floor) - empty by default
        var subLabel = new Label
        {
            Text = string.Empty,
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = new Color(0.6f, 0.9f, 1f)
        };
        subLabel.AddThemeFontSizeOverride("font_size", 9);
        itemContainer.AddChild(subLabel);

        AddChild(itemContainer);
        _buttons[cfg.Id] = (button, dot, subLabel);
    }

    public void SetNotification(string shortcutId, bool hasNew)
    {
        if (_buttons.TryGetValue(shortcutId, out var item))
        {
            item.dot.Visible = hasNew;
        }
    }

    public void SetSubLabel(string shortcutId, string text)
    {
        if (_buttons.TryGetValue(shortcutId, out var item))
        {
            item.subLabel.Text = text ?? string.Empty;
        }
    }

    public int ButtonCount => _buttons.Count;
}
