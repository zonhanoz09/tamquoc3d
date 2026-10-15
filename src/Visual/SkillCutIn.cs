using Godot;
using TamQuoc3D.Audio;
using TamQuoc3D.Hero;

namespace TamQuoc3D.Visual;

/// <summary>
/// SkillCutIn - Banner tuyet ky no chieu dien anh phong cach Tam Quoc.
/// </summary>
[GlobalClass]
public partial class SkillCutIn : CanvasLayer
{
    [Signal]
    public delegate void FinishedEventHandler();

    public void ShowCutIn(HeroData heroData, string skillName, float speedMult = 1.0f)
    {
        Layer = 100;

        // 1. Dark overlay
        var overlay = new ColorRect
        {
            Size = new Vector2(1280, 720),
            Color = new Color(0, 0, 0, 0.65f)
        };
        AddChild(overlay);

        // 2. Horizontal Banner
        var banner = new ColorRect
        {
            Size = new Vector2(1280, 140),
            Position = new Vector2(0, 290)
        };

        Color elemColor;
        switch (heroData.Element)
        {
            case HeroData.ElementType.Water:
                elemColor = new Color(0.15f, 0.45f, 0.85f, 0.85f);
                break;
            case HeroData.ElementType.Wind:
                elemColor = new Color(0.2f, 0.7f, 0.4f, 0.85f);
                break;
            case HeroData.ElementType.Light:
                elemColor = new Color(0.9f, 0.75f, 0.2f, 0.85f);
                break;
            case HeroData.ElementType.Dark:
                elemColor = new Color(0.5f, 0.2f, 0.7f, 0.85f);
                break;
            default:
                elemColor = new Color(0.7f, 0.3f, 0.15f, 0.85f);
                break;
        }

        banner.Color = elemColor;
        AddChild(banner);

        // Gold border lines
        var topLine = new ColorRect
        {
            Size = new Vector2(1280, 3),
            Position = new Vector2(0, 290),
            Color = new Color(1.0f, 0.85f, 0.3f)
        };
        AddChild(topLine);

        var botLine = new ColorRect
        {
            Size = new Vector2(1280, 3),
            Position = new Vector2(0, 427),
            Color = new Color(1.0f, 0.85f, 0.3f)
        };
        AddChild(botLine);

        // 3. Hero Portrait (Left)
        var portrait = new TextureRect();
        var pPath = $"res://assets/sprites/portraits/{heroData.HeroId}.png";
        var tex = GD.Load<Texture2D>(pPath);
        if (tex != null)
        {
            portrait.Texture = tex;
        }
        portrait.Size = new Vector2(110, 110);
        portrait.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        portrait.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        portrait.Position = new Vector2(160, 305);
        AddChild(portrait);

        // 4. Text Container (Hero Name + Skill Name)
        var heroLbl = new Label
        {
            Text = $"⚡ TUYỆT KỸ: {heroData.HeroName.ToUpper()} [{heroData.Title}]",
            Position = new Vector2(300, 310),
            Size = new Vector2(800, 30)
        };
        heroLbl.AddThemeColorOverride("font_color", new Color(1.0f, 0.9f, 0.5f));
        heroLbl.AddThemeFontSizeOverride("font_size", 18);
        AddChild(heroLbl);

        var skillLbl = new Label
        {
            Text = skillName.ToUpper(),
            Position = new Vector2(300, 340),
            Size = new Vector2(800, 60)
        };
        skillLbl.AddThemeColorOverride("font_color", Colors.White);
        skillLbl.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.9f));
        skillLbl.AddThemeConstantOverride("shadow_offset_x", 2);
        skillLbl.AddThemeConstantOverride("shadow_offset_y", 3);
        skillLbl.AddThemeFontSizeOverride("font_size", 38);
        AddChild(skillLbl);

        // Sound
        SoundManager.Instance?.PlayRage();

        // Animation timing
        float dur = 0.55f / speedMult;
        var tw = CreateTween();

        banner.Position = new Vector2(-1280, banner.Position.Y);
        topLine.Position = new Vector2(-1280, topLine.Position.Y);
        botLine.Position = new Vector2(1280, botLine.Position.Y);
        portrait.Position = new Vector2(-200, portrait.Position.Y);
        skillLbl.Scale = new Vector2(1.5f, 1.5f);

        tw.SetParallel(true);
        tw.TweenProperty(banner, "position:x", 0.0f, 0.15f / speedMult).SetTrans(Tween.TransitionType.Quad);
        tw.TweenProperty(topLine, "position:x", 0.0f, 0.15f / speedMult);
        tw.TweenProperty(botLine, "position:x", 0.0f, 0.15f / speedMult);
        tw.TweenProperty(portrait, "position:x", 160.0f, 0.15f / speedMult);
        tw.TweenProperty(skillLbl, "scale", new Vector2(1.0f, 1.0f), 0.15f / speedMult);

        // Hold then slide up
        tw.Chain().TweenInterval(dur * 0.6f);
        tw.Chain().TweenProperty(this, "offset:y", -720.0f, 0.18f / speedMult).SetTrans(Tween.TransitionType.Quad);
        tw.TweenCallback(Callable.From(() =>
        {
            EmitSignal(SignalName.Finished);
            QueueFree();
        }));
    }
}
