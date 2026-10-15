using Godot;
using TamQuoc3D.Core.Constants;
using TamQuoc3D.Data.Resources;

namespace TamQuoc3D.Features.Battle.Entities;

/// <summary>
/// HealthBar - Thanh mau thanh manh 4px, bo goc 2px, kem vach tut mau ghost-bar va hang icon trang thai.
/// </summary>
[GlobalClass]
public partial class HealthBar : Node2D
{
    public const float Width = GameConstants.HealthBarWidth;
    public const float Height = GameConstants.HealthBarHeight;

    public Panel HpBg { get; private set; }
    public Panel HpGhostBar { get; private set; }
    public Panel HpBar { get; private set; }
    public Label NameLabel { get; private set; }
    public Sprite2D ElemIcon { get; private set; }
    public HBoxContainer StatusRow { get; private set; }

    private Tween _hpTween;
    private Tween _ghostTween;

    public void Setup(HeroData data)
    {
        Position = new Vector2(0, -112);
        ZIndex = LayerConstants.OverheadUiZ;

        // 1. Element Icon
        ElemIcon = new Sprite2D();
        string elemName = data.Element switch
        {
            HeroData.ElementType.Fire => "fire",
            HeroData.ElementType.Water => "water",
            HeroData.ElementType.Wind => "wind",
            HeroData.ElementType.Light => "light",
            HeroData.ElementType.Dark => "dark",
            _ => "neutral"
        };
        var eTex = GD.Load<Texture2D>($"res://assets/common/icons/elem_{elemName}.png");
        if (eTex != null)
        {
            ElemIcon.Texture = eTex;
        }
        ElemIcon.Scale = new Vector2(0.65f, 0.65f);
        ElemIcon.Position = new Vector2(-52, -9);
        AddChild(ElemIcon);

        // 2. Name Label
        NameLabel = new Label
        {
            Text = data.HeroName,
            HorizontalAlignment = HorizontalAlignment.Center,
            Position = new Vector2(-Width / 2.0f, -18),
            Size = new Vector2(Width, 16)
        };
        NameLabel.AddThemeFontSizeOverride("font_size", 11);
        NameLabel.AddThemeColorOverride("font_shadow_color", Colors.Black);
        NameLabel.AddThemeConstantOverride("shadow_offset_x", 1);
        NameLabel.AddThemeConstantOverride("shadow_offset_y", 1);
        NameLabel.AddThemeConstantOverride("shadow_outline_size", 1);

        Color rarityColor = data.Rarity switch
        {
            HeroData.RarityType.Ur => new Color(1.0f, 0.35f, 0.35f),
            HeroData.RarityType.Ssr => new Color(1.0f, 0.88f, 0.25f),
            HeroData.RarityType.Sr => new Color(0.75f, 0.5f, 0.95f),
            _ => new Color(0.85f, 0.85f, 0.9f)
        };
        NameLabel.AddThemeColorOverride("font_color", rarityColor);
        AddChild(NameLabel);

        // 3. HP Background Track
        HpBg = new Panel
        {
            Position = new Vector2(-Width / 2.0f, 0),
            Size = new Vector2(Width, Height)
        };
        var sbBg = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.08f, 0.1f, 0.95f)
        };
        sbBg.SetCornerRadiusAll(GameConstants.HealthBarCornerRadius);
        HpBg.AddThemeStyleboxOverride("panel", sbBg);
        AddChild(HpBg);

        // 4. HP Ghost Bar
        HpGhostBar = new Panel
        {
            Position = new Vector2(-Width / 2.0f, 0),
            Size = new Vector2(Width, Height)
        };
        var sbGhost = new StyleBoxFlat
        {
            BgColor = new Color(0.98f, 0.88f, 0.38f, 0.9f)
        };
        sbGhost.SetCornerRadiusAll(GameConstants.HealthBarCornerRadius);
        HpGhostBar.AddThemeStyleboxOverride("panel", sbGhost);
        AddChild(HpGhostBar);

        // 5. HP Main Bar
        HpBar = new Panel
        {
            Position = new Vector2(-Width / 2.0f, 0),
            Size = new Vector2(Width, Height)
        };
        var sbFill = new StyleBoxFlat
        {
            BgColor = new Color(0.22f, 0.88f, 0.42f)
        };
        sbFill.SetCornerRadiusAll(GameConstants.HealthBarCornerRadius);
        HpBar.AddThemeStyleboxOverride("panel", sbFill);
        AddChild(HpBar);

        // 6. Status Icons Row
        StatusRow = new HBoxContainer
        {
            Position = new Vector2(-Width / 2.0f, -32),
            Size = new Vector2(Width, 12),
            Alignment = BoxContainer.AlignmentMode.Center
        };
        AddChild(StatusRow);
    }

    public void UpdateHp(int currentHp, int maxHp, bool animate = true)
    {
        float ratio = Mathf.Clamp((float)currentHp / (float)maxHp, 0.0f, 1.0f);
        float targetW = Mathf.Clamp(Width * ratio, 0.0f, Width);

        if (HpBar.GetThemeStylebox("panel") is StyleBoxFlat fillStyle)
        {
            if (ratio > 0.5f)
            {
                fillStyle.BgColor = new Color(0.22f, 0.88f, 0.42f);
            }
            else if (ratio > 0.25f)
            {
                fillStyle.BgColor = new Color(0.96f, 0.76f, 0.16f);
            }
            else
            {
                fillStyle.BgColor = new Color(0.96f, 0.24f, 0.24f);
            }
        }

        HpBar.Visible = targetW > 0.0f;
        HpGhostBar.Visible = targetW > 0.0f;

        if (animate)
        {
            if (_hpTween != null && _hpTween.IsValid()) _hpTween.Kill();
            _hpTween = CreateTween();
            _hpTween.TweenProperty(HpBar, "size:x", targetW, 0.22f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);

            if (_ghostTween != null && _ghostTween.IsValid()) _ghostTween.Kill();
            _ghostTween = CreateTween();
            _ghostTween.TweenInterval(0.25f);
            _ghostTween.TweenProperty(HpGhostBar, "size:x", targetW, 0.5f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        }
        else
        {
            HpBar.Size = new Vector2(targetW, HpBar.Size.Y);
            HpGhostBar.Size = new Vector2(targetW, HpGhostBar.Size.Y);
        }
    }
}
