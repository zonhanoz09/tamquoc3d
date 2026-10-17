using Godot;
using TamQuoc3D.Data.Resources;

namespace TamQuoc3D.Features.DeckBuilder;

/// <summary>
/// FormationSlot - Dai dien cho 1 o trong luoi tran hinh 3x3.
/// Hien thi trang thai trong, da xep tuong, hoac bi khoa (inactive), cung nut go tuong (X).
/// </summary>
[GlobalClass]
public partial class FormationSlot : Control
{
    [Signal]
    public delegate void SlotClickedEventHandler(int row, int col);

    [Signal]
    public delegate void RemoveClickedEventHandler(int row, int col);

    public int Row { get; set; } = 0;
    public int Col { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public bool IsOccupied => !string.IsNullOrEmpty(OccupiedHeroId);
    public string OccupiedHeroId { get; private set; } = string.Empty;
    public HeroData CurrentHero { get; private set; }

    private TextureRect _bgRect;
    private TextureRect _heroPortrait;
    private Label _heroNameLabel;
    private Button _removeButton;
    private TextureRect _highlightOverlay;
    private Label _laneHintLabel;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(96, 96);
        MouseFilter = IsActive ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;

        SetupVisualTree();
        UpdateVisuals();
    }

    private void SetupVisualTree()
    {
        // 1. Background (96x96)
        _bgRect = new TextureRect
        {
            CustomMinimumSize = new Vector2(96, 96),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(_bgRect);

        // 2. Hero Portrait (Centered)
        _heroPortrait = new TextureRect
        {
            Position = new Vector2(18, 10),
            Size = new Vector2(60, 60),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Visible = false,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(_heroPortrait);

        // 3. Hero Name Label (Bottom)
        _heroNameLabel = new Label
        {
            Position = new Vector2(4, 72),
            Size = new Vector2(88, 18),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Visible = false,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _heroNameLabel.AddThemeFontSizeOverride("font_size", 10);
        _heroNameLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.9f, 0.6f));
        AddChild(_heroNameLabel);

        // 4. Lane / Role Hint Label (When empty)
        _laneHintLabel = new Label
        {
            Position = new Vector2(4, 38),
            Size = new Vector2(88, 20),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _laneHintLabel.AddThemeFontSizeOverride("font_size", 9);
        _laneHintLabel.AddThemeColorOverride("font_color", new Color(0.65f, 0.60f, 0.50f, 0.75f));
        AddChild(_laneHintLabel);

        // 5. Highlight Overlay
        _highlightOverlay = new TextureRect
        {
            CustomMinimumSize = new Vector2(96, 96),
            Texture = GD.Load<Texture2D>("res://assets/ui/formation/hero_card_frame_selected.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            Visible = false,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(_highlightOverlay);

        // 6. Remove Button '✕' (Top-Right)
        _removeButton = new Button
        {
            Text = "✕",
            Position = new Vector2(72, 2),
            Size = new Vector2(22, 22),
            Visible = false
        };
        _removeButton.AddThemeFontSizeOverride("font_size", 10);
        _removeButton.AddThemeColorOverride("font_color", new Color(1.0f, 0.35f, 0.35f));
        var sbRemove = new StyleBoxFlat
        {
            BgColor = new Color(0.2f, 0.05f, 0.05f, 0.85f),
            BorderColor = new Color(0.9f, 0.2f, 0.2f)
        };
        sbRemove.SetBorderWidthAll(1);
        sbRemove.SetCornerRadiusAll(11);
        _removeButton.AddThemeStyleboxOverride("normal", sbRemove);
        _removeButton.Pressed += OnRemovePressed;
        AddChild(_removeButton);
    }

    public void SetupSlot(int row, int col, bool isActive, string hintText = "")
    {
        Row = row;
        Col = col;
        IsActive = isActive;
        MouseFilter = isActive ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
        if (!string.IsNullOrEmpty(hintText) && _laneHintLabel != null)
        {
            _laneHintLabel.Text = hintText;
        }
        UpdateVisuals();
    }

    public void SetHero(HeroData hero)
    {
        CurrentHero = hero;
        OccupiedHeroId = hero?.HeroId ?? string.Empty;
        UpdateVisuals();
    }

    public void ClearHero()
    {
        CurrentHero = null;
        OccupiedHeroId = string.Empty;
        UpdateVisuals();
    }

    public void SetHighlight(bool on)
    {
        if (_highlightOverlay != null)
        {
            _highlightOverlay.Visible = on && IsActive;
        }
    }

    private void UpdateVisuals()
    {
        if (_bgRect == null) return;

        if (!IsActive)
        {
            _bgRect.Texture = GD.Load<Texture2D>("res://assets/ui/formation/slot_inactive.png");
            _heroPortrait.Visible = false;
            _heroNameLabel.Visible = false;
            _removeButton.Visible = false;
            if (_laneHintLabel != null) _laneHintLabel.Visible = false;
            return;
        }

        if (IsOccupied && CurrentHero != null)
        {
            _bgRect.Texture = GD.Load<Texture2D>("res://assets/ui/formation/slot_filled.png");
            var tex = GD.Load<Texture2D>($"res://assets/heroes/{CurrentHero.HeroId}/portrait.png")
                   ?? GD.Load<Texture2D>($"res://assets/heroes/{CurrentHero.HeroId}/icon.png");
            _heroPortrait.Texture = tex;
            _heroPortrait.Visible = true;
            _heroNameLabel.Text = CurrentHero.HeroName;
            _heroNameLabel.Visible = true;
            _removeButton.Visible = true;
            if (_laneHintLabel != null) _laneHintLabel.Visible = false;
        }
        else
        {
            _bgRect.Texture = GD.Load<Texture2D>("res://assets/ui/formation/slot_empty.png");
            _heroPortrait.Visible = false;
            _heroNameLabel.Visible = false;
            _removeButton.Visible = false;
            if (_laneHintLabel != null) _laneHintLabel.Visible = true;
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (!IsActive) return;

        if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && mb.Pressed)
        {
            EmitSignal(SignalName.SlotClicked, Row, Col);
            AcceptEvent();
        }
    }

    private void OnRemovePressed()
    {
        EmitSignal(SignalName.RemoveClicked, Row, Col);
    }
}
