using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Panel "Nhiệm Vụ Chính" bên phải hub (§6.3d): CHỈ hiển thị tiến trình chính đã có
/// sẵn (ải chiến dịch kế tiếp, tầng tháp kế tiếp) — KHÔNG phải nhiệm vụ hằng ngày
/// (chức năng đó làm sau theo chốt của Huynh, không có nút nhận thưởng, không đếm ngày).
/// Chọn một mục → panel chi tiết bên dưới hiện mô tả; nút "Đi" điều hướng tới đích thật.
/// </summary>
[GlobalClass]
public partial class QuestTrackerPanel : PanelContainer
{
    [Signal] public delegate void QuestGoRequestedEventHandler(string actionId);

    public class QuestEntry
    {
        public string Id;
        public string Title;
        public string Sub;
        public string Detail;
        public string ActionId;
    }

    private const int PanelWidth = 272;

    private VBoxContainer _entriesContainer;
    private Label _detailLabel;
    private Button _goButton;
    private List<QuestEntry> _entries = new();
    private QuestEntry _selected;

    public int EntryCount => _entries.Count;
    public string SelectedActionId => _selected?.ActionId ?? "";

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(PanelWidth, 0);
        Size = new Vector2(PanelWidth, 0);
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
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        AddChild(margin);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 6);
        margin.AddChild(root);

        var title = new Label();
        title.Text = "NHIỆM VỤ CHÍNH";
        title.AddThemeFontSizeOverride("font_size", 14);
        title.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.4f));
        root.AddChild(title);

        _entriesContainer = new VBoxContainer();
        _entriesContainer.AddThemeConstantOverride("separation", 4);
        root.AddChild(_entriesContainer);

        var divider = new HSeparator();
        root.AddChild(divider);

        _detailLabel = new Label();
        _detailLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _detailLabel.AddThemeFontSizeOverride("font_size", 12);
        _detailLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.9f));
        root.AddChild(_detailLabel);

        _goButton = new Button();
        _goButton.Text = "Đi";
        _goButton.Visible = false;
        _goButton.AddThemeFontSizeOverride("font_size", 13);
        _goButton.Pressed += () =>
        {
            if (_selected != null)
            {
                EmitSignal(SignalName.QuestGoRequested, _selected.ActionId);
            }
        };
        root.AddChild(_goButton);
    }

    public void SetEntries(List<QuestEntry> entries)
    {
        _entries = entries ?? new List<QuestEntry>();
        foreach (var child in _entriesContainer.GetChildren())
        {
            child.QueueFree();
        }
        _selected = null;
        _detailLabel.Text = _entries.Count == 0 ? "Chưa có tiến trình chính." : "";
        _goButton.Visible = false;

        foreach (var entry in _entries)
        {
            var captured = entry;
            var button = new Button();
            button.Text = $"{entry.Title}\n{entry.Sub}";
            button.Alignment = HorizontalAlignment.Left;
            button.AddThemeFontSizeOverride("font_size", 12);
            button.AddThemeColorOverride("font_color", Colors.White);
            button.Pressed += () => SelectEntry(captured);
            _entriesContainer.AddChild(button);
        }

        if (_entries.Count > 0)
        {
            SelectEntry(_entries[0]);
        }
    }

    private void SelectEntry(QuestEntry entry)
    {
        _selected = entry;
        _detailLabel.Text = entry.Detail;
        _goButton.Visible = true;
    }
}
