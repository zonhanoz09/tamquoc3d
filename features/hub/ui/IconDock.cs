using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Cụm icon phải của hub (§6): dồn phải, neo mép phải màn hình, mở rộng được.
/// Cấu trúc từ trên xuống: các hàng icon (trong khung cuộn) → hàng tiện ích.
/// - Hàng khai báo bằng dữ liệu (DockRowGroup): thêm event/tính năng mới chỉ cần
///   thêm icon vào nhóm, không sửa layout. Thứ tự nhóm: Sự kiện → Tính năng → Khóa.
/// - Mỗi hàng tối đa IconsPerRow icon; icon thừa tự tràn xuống hàng mới cùng nhóm.
/// - Mỗi hàng dồn phải: icon xếp sát mép phải, phần trống nằm bên trái (nhãn nhóm).
/// - Mặc định chỉ hiện 2 hàng đầu; nút Mở Rộng hiện toàn bộ hàng trong khung cuộn
///   cao tối đa ExpandedMaxHeight — dù sau này có bao nhiêu hàng cũng không tràn
///   xuống che sân hay đẩy panel nhiệm vụ mất hút.
/// </summary>
[GlobalClass]
public partial class IconDock : Control
{
    [Signal] public delegate void IconClickedEventHandler(string iconId);
    [Signal] public delegate void MapClickedEventHandler();
    [Signal] public delegate void SettingsClickedEventHandler();
    [Signal] public delegate void ExpandToggledEventHandler(bool expanded);
    [Signal] public delegate void HeightChangedEventHandler(float height);

    public const int DockWidth = 700;
    public const int IconsPerRow = 10;
    public const int CollapsedVisibleRows = 2;
    public const int ExpandedMaxHeight = 240;
    public const int UtilityRowHeight = 34;

    public enum RowGroup { Event, Feature, Locked }

    public class DockIcon
    {
        public string Id;
        public string Name;
        public string IconPath;
        public bool Locked;
    }

    public class DockRowGroupConfig
    {
        public RowGroup Group;
        public string GroupLabel;
        public int IconSize;
        public List<DockIcon> Icons = new();
    }

    private class IconItem
    {
        public Control Container;
        public TextureRect Dot;
        public Label SubLabel;
    }

    private readonly Dictionary<string, IconItem> _itemsById = new();
    private ScrollContainer _scroll;
    private VBoxContainer _rowsContainer;
    private Button _expandButton;
    private bool _expanded;
    private int _rowCount;

    /// <summary>Tổng số icon trong dock (không tính nút tiện ích).</summary>
    public int ButtonCount => _itemsById.Count;
    public int RowCount => _rowCount;
    public bool Expanded => _expanded;

    /// <summary>Chiều cao hiện tại của dock (hàng icon + hàng tiện ích).</summary>
    public float DockHeight => _scroll == null ? 0 : _scroll.CustomMinimumSize.Y + UtilityRowHeight;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(DockWidth, 0);
        Size = new Vector2(DockWidth, 0);
        MouseFilter = MouseFilterEnum.Stop;

        var root = new VBoxContainer();
        root.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        root.AddThemeConstantOverride("separation", 4);
        AddChild(root);

        _scroll = new ScrollContainer();
        _scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        root.AddChild(_scroll);

        _rowsContainer = new VBoxContainer();
        _rowsContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _rowsContainer.AddThemeConstantOverride("separation", 4);
        _scroll.AddChild(_rowsContainer);

        BuildRows(DefaultGroups());
        ApplyExpanded(false);

        // Hàng tiện ích: địa điểm + Bản Đồ + Cài Đặt + Thu Gọn/Mở Rộng.
        var utility = new HBoxContainer();
        utility.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        utility.Alignment = BoxContainer.AlignmentMode.End;
        utility.AddThemeConstantOverride("separation", 6);
        root.AddChild(utility);

        var locationLabel = new Label();
        locationLabel.Text = "Đại Bản Doanh";
        locationLabel.AddThemeFontSizeOverride("font_size", 13);
        locationLabel.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.4f));
        utility.AddChild(locationLabel);

        var mapButton = BuildUtilityButton("Bản Đồ");
        mapButton.Pressed += () => EmitSignal(SignalName.MapClicked);
        utility.AddChild(mapButton);

        var settingsButton = BuildUtilityButton("⚙");
        settingsButton.Pressed += () => EmitSignal(SignalName.SettingsClicked);
        utility.AddChild(settingsButton);

        _expandButton = BuildUtilityButton("Mở Rộng");
        _expandButton.Pressed += () => SetExpanded(!_expanded);
        utility.AddChild(_expandButton);
    }

    /// <summary>Các nhóm hàng mặc định của hub (§6.3c).</summary>
    public static List<DockRowGroupConfig> DefaultGroups() => new()
    {
        new DockRowGroupConfig
        {
            Group = RowGroup.Event, GroupLabel = "Sự kiện", IconSize = 40,
            Icons = new List<DockIcon>
            {
                new() { Id = "su_kien", Name = "Sự Kiện", IconPath = "res://assets/ui/hub/icon_su_kien.png" },
                new() { Id = "qua", Name = "Quà", IconPath = "res://assets/ui/hub/icon_qua.png" },
                new() { Id = "hom_thu", Name = "Hòm Thư", IconPath = "res://assets/ui/hub/icon_hom_thu.png" },
                new() { Id = "cai_dat", Name = "Cài Đặt", IconPath = "res://assets/ui/hub/icon_cai_dat.png" },
            }
        },
        new DockRowGroupConfig
        {
            Group = RowGroup.Feature, GroupLabel = "Tính năng", IconSize = 56,
            Icons = new List<DockIcon>
            {
                new() { Id = "chieu_mo", Name = "Chiêu Mộ", IconPath = "res://assets/ui/hub/icon_chieu_mo.png" },
                new() { Id = "cua_hang", Name = "Cửa Hàng", IconPath = "res://assets/ui/hub/icon_cua_hang.png" },
                new() { Id = "thap", Name = "Tháp", IconPath = "res://assets/ui/hub/icon_thap.png" },
                new() { Id = "arena", Name = "Đấu Trường", IconPath = "res://assets/ui/hub/icon_arena.png" },
                new() { Id = "formation", Name = "Đội Hình", IconPath = "res://assets/ui/hub/icon_formation.png" },
                new() { Id = "hero", Name = "Tướng", IconPath = "res://assets/ui/hub/icon_hero.png" },
            }
        },
        new DockRowGroupConfig
        {
            Group = RowGroup.Locked, GroupLabel = "Sắp mở", IconSize = 56,
            Icons = new List<DockIcon>
            {
                new() { Id = "bang_hoi", Name = "Bang Hội", IconPath = "res://assets/ui/hub/icon_bang_hoi.png", Locked = true },
                new() { Id = "me_cung", Name = "Mê Cung", IconPath = "res://assets/ui/hub/icon_me_cung.png", Locked = true },
            }
        },
    };

    /// <summary>Dựng lại toàn bộ hàng từ danh sách nhóm (dùng khi thêm hàng mới).</summary>
    public void RebuildRows(List<DockRowGroupConfig> groups)
    {
        foreach (var child in _rowsContainer.GetChildren())
        {
            child.QueueFree();
        }
        _itemsById.Clear();
        _rowCount = 0;
        BuildRows(groups);
        ApplyExpanded(_expanded);
    }

    private void BuildRows(List<DockRowGroupConfig> groups)
    {
        foreach (var group in groups)
        {
            for (var start = 0; start < group.Icons.Count; start += IconsPerRow)
            {
                var count = Math.Min(IconsPerRow, group.Icons.Count - start);
                var row = new HBoxContainer();
                row.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                row.Alignment = BoxContainer.AlignmentMode.End;
                row.AddThemeConstantOverride("separation", 6);
                _rowsContainer.AddChild(row);

                // Nhãn nhóm chiếm phần trống bên trái, icon dồn sát mép phải.
                var groupLabel = new Label();
                groupLabel.Text = group.GroupLabel;
                groupLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                groupLabel.HorizontalAlignment = HorizontalAlignment.Left;
                groupLabel.VerticalAlignment = VerticalAlignment.Center;
                groupLabel.AddThemeFontSizeOverride("font_size", 11);
                groupLabel.AddThemeColorOverride("font_color", new Color(0.75f, 0.75f, 0.8f));
                row.AddChild(groupLabel);

                for (var i = 0; i < count; i++)
                {
                    var icon = group.Icons[start + i];
                    row.AddChild(BuildIconItem(icon, group.IconSize));
                }
                _rowCount++;
            }
        }
    }

    private Control BuildIconItem(DockIcon icon, int iconSize)
    {
        // Control thuần (không phải VBox) để chấm đỏ neo đúng góc phải trên của nút.
        var itemHeight = iconSize + 13 + 11 + 2;
        var item = new Control();
        item.CustomMinimumSize = new Vector2(iconSize, itemHeight);
        item.Size = new Vector2(iconSize, itemHeight);

        var button = new TextureButton();
        button.TextureNormal = GD.Load<Texture2D>(icon.IconPath);
        button.CustomMinimumSize = new Vector2(iconSize, iconSize);
        button.Size = new Vector2(iconSize, iconSize);
        button.Position = Vector2.Zero;
        button.IgnoreTextureSize = true;
        button.StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered;
        if (icon.Locked)
        {
            button.Modulate = new Color(0.55f, 0.55f, 0.6f);
        }
        var capturedId = icon.Id;
        button.Pressed += () => EmitSignal(SignalName.IconClicked, capturedId);
        button.MouseEntered += () => button.Modulate = icon.Locked
            ? new Color(0.7f, 0.7f, 0.75f)
            : new Color(1.15f, 1.15f, 1.15f);
        button.MouseExited += () => button.Modulate = icon.Locked
            ? new Color(0.55f, 0.55f, 0.6f)
            : Colors.White;
        item.AddChild(button);

        var label = new Label();
        label.Text = icon.Locked ? $"🔒 {icon.Name}" : icon.Name;
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.Position = new Vector2(-10, iconSize + 1);
        label.Size = new Vector2(iconSize + 20, 13);
        label.AddThemeFontSizeOverride("font_size", 11);
        label.AddThemeColorOverride("font_color", icon.Locked
            ? new Color(0.6f, 0.6f, 0.65f)
            : Colors.White);
        item.AddChild(label);

        var subLabel = new Label();
        subLabel.Text = "";
        subLabel.HorizontalAlignment = HorizontalAlignment.Center;
        subLabel.Position = new Vector2(-10, iconSize + 14);
        subLabel.Size = new Vector2(iconSize + 20, 11);
        subLabel.AddThemeFontSizeOverride("font_size", 9);
        subLabel.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.4f));
        item.AddChild(subLabel);

        var dot = new TextureRect();
        dot.Texture = GD.Load<Texture2D>("res://assets/ui/hub/shortcut_notification_dot.png");
        dot.CustomMinimumSize = new Vector2(14, 14);
        dot.Size = new Vector2(14, 14);
        dot.Position = new Vector2(iconSize - 14, 0);
        dot.Visible = false;
        item.AddChild(dot);

        _itemsById[icon.Id] = new IconItem { Container = item, Dot = dot, SubLabel = subLabel };
        return item;
    }

    private static Button BuildUtilityButton(string text)
    {
        var button = new Button();
        button.Text = text;
        button.AddThemeFontSizeOverride("font_size", 13);
        button.AddThemeColorOverride("font_color", Colors.White);
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.1f, 0.2f, 0.8f),
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        button.AddThemeStyleboxOverride("normal", style);
        button.AddThemeStyleboxOverride("hover", style);
        button.AddThemeStyleboxOverride("pressed", style);
        return button;
    }

    public void SetExpanded(bool expanded)
    {
        if (_expanded == expanded && _scroll != null) return;
        ApplyExpanded(expanded);
        EmitSignal(SignalName.ExpandToggled, _expanded);
    }

    private void ApplyExpanded(bool expanded)
    {
        _expanded = expanded;
        if (_scroll == null) return;
        var rowHeight = _rowCount == 0 ? 0 : EstimateRowHeight();
        var collapsedHeight = Math.Min(CollapsedVisibleRows, _rowCount) * rowHeight;
        var fullHeight = _rowCount * rowHeight;
        var height = expanded ? Math.Min(fullHeight, ExpandedMaxHeight) : collapsedHeight;
        _scroll.CustomMinimumSize = new Vector2(DockWidth, height);
        _scroll.Size = new Vector2(DockWidth, height);
        if (_expandButton != null)
        {
            _expandButton.Text = expanded ? "Thu Gọn" : "Mở Rộng";
        }
        EmitSignal(SignalName.HeightChanged, DockHeight);
    }

    /// <summary>Chiều cao ước lượng của một hàng (icon lớn nhất + nhãn + sublabel).</summary>
    private int EstimateRowHeight() => 56 + 13 + 11 + 4;

    public void SetNotification(string iconId, bool show)
    {
        if (_itemsById.TryGetValue(iconId, out var item))
        {
            item.Dot.Visible = show;
        }
    }

    public void SetSubLabel(string iconId, string text)
    {
        if (_itemsById.TryGetValue(iconId, out var item))
        {
            item.SubLabel.Text = text;
        }
    }
}
