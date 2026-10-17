using System.Collections.Generic;
using Godot;
using TamQuoc3D.Data.Resources;

namespace TamQuoc3D.Features.Campaign;

/// <summary>
/// ChapterSelectPanel - Thanh lua chon cac chuong chien dich (Chapters Drawer) voi tong so sao dat duoc va trang thai khoa/mo.
/// </summary>
[GlobalClass]
public partial class ChapterSelectPanel : PanelContainer
{
    [Signal]
    public delegate void ChapterSelectedEventHandler(int chapterId);

    private VBoxContainer _chapterList;
    private Button _toggleBtn;
    private bool _isExpanded = false;
    private Tween _animTween;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(300, 720);
        BuildUI();
    }

    private void BuildUI()
    {
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.12f, 0.16f, 0.95f),
            BorderColor = new Color(0.3f, 0.35f, 0.45f),
            BorderWidthRight = 2
        };
        AddThemeStyleboxOverride("panel", style);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_top", 16);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_bottom", 16);
        AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 14);
        margin.AddChild(vbox);

        // Header
        var header = new HBoxContainer();
        vbox.AddChild(header);

        var title = new Label
        {
            Text = "📜 CHƯƠNG CHIẾN DỊCH",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Modulate = new Color(1f, 0.85f, 0.3f)
        };
        title.AddThemeFontSizeOverride("font_size", 16);
        header.AddChild(title);

        _toggleBtn = new Button
        {
            Text = "◀",
            CustomMinimumSize = new Vector2(30, 30)
        };
        _toggleBtn.Pressed += TogglePanel;
        header.AddChild(_toggleBtn);

        // Scrollable list of chapters
        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        vbox.AddChild(scroll);

        _chapterList = new VBoxContainer();
        _chapterList.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(_chapterList);
    }

    public void PopulateChapters(List<ChapterData> chapters, int currentChapterId, Dictionary<int, int> chapterStars)
    {
        foreach (Node child in _chapterList.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var ch in chapters)
        {
            int chId = ch.ChapterId;
            int stars = chapterStars != null && chapterStars.TryGetValue(chId, out int s) ? s : 0;
            bool isUnlocked = ch.UnlockRequirement <= (chapterStars != null && chapterStars.TryGetValue(chId - 1, out int prevS) ? prevS : (chId == 1 ? 0 : -1));
            bool isCurrent = chId == currentChapterId;

            var btn = new Button
            {
                CustomMinimumSize = new Vector2(260, 64),
                Alignment = HorizontalAlignment.Left
            };

            var btnBox = new VBoxContainer { Position = new Vector2(10, 6) };
            
            var nameLbl = new Label
            {
                Text = ch.ChapterName,
                Modulate = isCurrent ? new Color(1f, 0.85f, 0.2f) : (isUnlocked ? new Color(0.95f, 0.95f, 0.95f) : new Color(0.5f, 0.5f, 0.5f))
            };
            nameLbl.AddThemeFontSizeOverride("font_size", 13);
            btnBox.AddChild(nameLbl);

            string starText = isUnlocked ? $"⭐ {stars}/{ch.TotalStars} Sao" : $"🔒 Cần {ch.UnlockRequirement}⭐ Chương trước";
            var starLbl = new Label
            {
                Text = starText,
                Modulate = isUnlocked ? new Color(1f, 0.75f, 0.2f) : new Color(0.6f, 0.4f, 0.4f)
            };
            starLbl.AddThemeFontSizeOverride("font_size", 11);
            btnBox.AddChild(starLbl);

            btn.AddChild(btnBox);
            btn.Disabled = !isUnlocked;
            btn.Pressed += () =>
            {
                EmitSignal(SignalName.ChapterSelected, chId);
                ClosePanel();
            };

            _chapterList.AddChild(btn);
        }
    }

    public void TogglePanel()
    {
        if (_isExpanded)
        {
            ClosePanel();
        }
        else
        {
            OpenPanel();
        }
    }

    public void OpenPanel()
    {
        _isExpanded = true;
        _toggleBtn.Text = "◀";
        AnimatePosition(0);
    }

    public void ClosePanel()
    {
        _isExpanded = false;
        _toggleBtn.Text = "▶";
        AnimatePosition(-260);
    }

    private void AnimatePosition(float targetX)
    {
        if (_animTween != null && _animTween.IsValid())
        {
            _animTween.Kill();
        }
        _animTween = CreateTween();
        _animTween?.TweenProperty(this, "position:x", targetX, 0.2f)
                   .SetTrans(Tween.TransitionType.Cubic)
                   .SetEase(Tween.EaseType.Out);
    }
}
