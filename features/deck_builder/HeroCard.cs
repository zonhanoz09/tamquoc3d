using Godot;
using TamQuoc3D.Data.Resources;

namespace TamQuoc3D.Features.DeckBuilder;

/// <summary>
/// HeroCard - Widget the tuong trong danh sach chon doi hinh.
/// Hien thi anh chan dung, ten, he nguyen to, so sao, luc chien va trang thai chon / da ra tran.
/// </summary>
[GlobalClass]
public partial class HeroCard : Control
{
    [Signal]
    public delegate void CardClickedEventHandler(string heroId);

    public string HeroId { get; private set; } = string.Empty;
    public HeroData Data { get; private set; }
    public bool IsSelected { get; private set; } = false;
    public bool IsInFormation { get; private set; } = false;

    private TextureRect _bgRect;
    private TextureRect _portraitRect;
    private Label _nameLabel;
    private TextureRect _elementIcon;
    private HBoxContainer _starsContainer;
    private Label _powerLabel;
    private TextureRect _selectionOverlay;
    private TextureRect _inFormationOverlay;
    private ColorRect _dimOverlay;

    private Tween _scaleTween;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(80, 120);
        PivotOffset = new Vector2(40, 60);
        MouseFilter = MouseFilterEnum.Stop;

        SetupVisualTree();

        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
    }

    private void SetupVisualTree()
    {
        // 1. Background
        _bgRect = new TextureRect
        {
            CustomMinimumSize = new Vector2(80, 120),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(_bgRect);

        // 2. Element Icon (Top-Left)
        _elementIcon = new TextureRect
        {
            Position = new Vector2(4, 4),
            Size = new Vector2(16, 16),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(_elementIcon);

        // 3. Stars Container (Top-Right)
        _starsContainer = new HBoxContainer
        {
            Position = new Vector2(24, 5),
            Size = new Vector2(52, 14),
            Alignment = BoxContainer.AlignmentMode.End,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _starsContainer.AddThemeConstantOverride("separation", 1);
        AddChild(_starsContainer);

        // 4. Portrait (Center)
        _portraitRect = new TextureRect
        {
            Position = new Vector2(12, 26),
            Size = new Vector2(56, 56),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(_portraitRect);

        // 5. Name Label
        _nameLabel = new Label
        {
            Position = new Vector2(2, 82),
            Size = new Vector2(76, 16),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _nameLabel.AddThemeFontSizeOverride("font_size", 9);
        _nameLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.95f, 0.85f));
        AddChild(_nameLabel);

        // 6. Combat Power Label (Bottom)
        _powerLabel = new Label
        {
            Position = new Vector2(2, 100),
            Size = new Vector2(76, 16),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _powerLabel.AddThemeFontSizeOverride("font_size", 8);
        _powerLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.84f, 0.3f));
        AddChild(_powerLabel);

        // 7. Dim Overlay (when in formation)
        _dimOverlay = new ColorRect
        {
            CustomMinimumSize = new Vector2(80, 120),
            Color = new Color(0, 0, 0, 0.55f),
            Visible = false,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(_dimOverlay);

        // 8. In-formation Frame
        _inFormationOverlay = new TextureRect
        {
            CustomMinimumSize = new Vector2(80, 120),
            Texture = GD.Load<Texture2D>("res://assets/ui/formation/hero_card_frame_in_formation.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            Visible = false,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(_inFormationOverlay);

        // 9. Selection Overlay (Golden Border)
        _selectionOverlay = new TextureRect
        {
            CustomMinimumSize = new Vector2(80, 120),
            Texture = GD.Load<Texture2D>("res://assets/ui/formation/hero_card_frame_selected.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            Visible = false,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(_selectionOverlay);
    }

    public void SetData(HeroData data)
    {
        Data = data;
        if (data == null) return;

        HeroId = data.HeroId;
        _nameLabel.Text = data.HeroName;

        // Background by Element
        string elemKey = data.Element switch
        {
            HeroData.ElementType.Fire => "fire",
            HeroData.ElementType.Water => "water",
            HeroData.ElementType.Wind => "wind",
            HeroData.ElementType.Light => "light",
            HeroData.ElementType.Dark => "dark",
            _ => "default"
        };
        var bgTex = GD.Load<Texture2D>($"res://assets/ui/formation/hero_card_bg_{elemKey}.png");
        if (bgTex != null) _bgRect.Texture = bgTex;

        // Element Icon
        var elemTex = GD.Load<Texture2D>($"res://assets/common/icons/elem_{elemKey}.png");
        if (elemTex != null) _elementIcon.Texture = elemTex;

        // Portrait
        var portTex = GD.Load<Texture2D>($"res://assets/heroes/{data.HeroId}/portrait.png")
                   ?? GD.Load<Texture2D>($"res://assets/heroes/{data.HeroId}/icon.png");
        if (portTex != null) _portraitRect.Texture = portTex;

        // Stars (R=3, SR=4, SSR=5, UR=5)
        int stars = data.Rarity switch
        {
            HeroData.RarityType.Ur => 5,
            HeroData.RarityType.Ssr => 5,
            HeroData.RarityType.Sr => 4,
            _ => 3
        };

        foreach (var child in _starsContainer.GetChildren())
        {
            child.QueueFree();
        }

        var starFilled = GD.Load<Texture2D>("res://assets/ui/formation/icon_star_filled.png");
        var starEmpty = GD.Load<Texture2D>("res://assets/ui/formation/icon_star_empty.png");

        for (int i = 0; i < 5; i++)
        {
            var sRect = new TextureRect
            {
                CustomMinimumSize = new Vector2(8, 8),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                Texture = i < stars ? starFilled : starEmpty,
                MouseFilter = MouseFilterEnum.Ignore
            };
            _starsContainer.AddChild(sRect);
        }

        // Combat Power
        int power = CalculateCombatPower(data);
        _powerLabel.Text = $"⚔ {power:N0}";
    }

    public static int CalculateCombatPower(HeroData hero)
    {
        if (hero?.BaseStats == null) return 0;
        var s = hero.BaseStats;
        int basePower = (s.HpMax / 5) + (s.AtkPhys + s.AtkMag) * 2 + (s.DefPhys + s.DefMag) * 2 + s.Speed * 3;
        int bonusPower = (int)((s.CritRate + s.DodgeRate + s.BlockRate + s.CounterRate) * 1000);
        int rarityMult = hero.Rarity switch
        {
            HeroData.RarityType.Ur => 130,
            HeroData.RarityType.Ssr => 115,
            HeroData.RarityType.Sr => 100,
            _ => 90
        };
        return (basePower + bonusPower) * rarityMult / 100;
    }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        _selectionOverlay.Visible = selected;
    }

    public void SetInFormation(bool inFormation)
    {
        IsInFormation = inFormation;
        _dimOverlay.Visible = inFormation;
        _inFormationOverlay.Visible = inFormation;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && mb.Pressed)
        {
            EmitSignal(SignalName.CardClicked, HeroId);
            AcceptEvent();
        }
    }

    private void OnMouseEntered()
    {
        if (_scaleTween != null && _scaleTween.IsValid()) _scaleTween.Kill();
        _scaleTween = CreateTween();
        _scaleTween.TweenProperty(this, "scale", new Vector2(1.06f, 1.06f), 0.12f)
                   .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        ZIndex = 10;
    }

    private void OnMouseExited()
    {
        if (_scaleTween != null && _scaleTween.IsValid()) _scaleTween.Kill();
        _scaleTween = CreateTween();
        _scaleTween.TweenProperty(this, "scale", Vector2.One, 0.12f)
                   .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        ZIndex = 0;
    }
}
