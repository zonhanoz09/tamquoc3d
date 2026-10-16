using System.Threading.Tasks;
using Godot;
using TamQuoc3D.Core.Constants;
using TamQuoc3D.Data.Resources;

namespace TamQuoc3D.Features.Battle.Entities;

/// <summary>
/// HeroCutInCinematic - Hieu ung trinh dien Cat-In sieu ky nang phong cach Arcade 16-bit.
/// Xuat hien khi danh tuong day No phat dong dai chieu (Octopath / Capcom CPS2 style).
/// </summary>
[GlobalClass]
public partial class HeroCutInCinematic : Control
{
    public static async Task PlayAsync(Node parent, HeroData hero, SkillData skill, bool isEnemy, float speedMult = 1.0f)
    {
        var cutIn = new HeroCutInCinematic();
        parent.AddChild(cutIn);
        await cutIn.ExecuteAnimationAsync(hero, skill, isEnemy, speedMult);
        cutIn.QueueFree();
    }

    private async Task ExecuteAnimationAsync(HeroData hero, SkillData skill, bool isEnemy, float speedMult)
    {
        ZIndex = LayerConstants.CutInCanvasLayer;
        Size = new Vector2(GameConstants.ViewportWidth, GameConstants.ViewportHeight);
        MouseFilter = MouseFilterEnum.Ignore;

        // Element theme color
        Color elemColor = hero.Element switch
        {
            HeroData.ElementType.Water => new Color(0.22f, 0.74f, 0.97f), // Blue
            HeroData.ElementType.Fire => new Color(0.94f, 0.27f, 0.27f),  // Red
            HeroData.ElementType.Wind => new Color(0.18f, 0.82f, 0.44f),  // Green
            HeroData.ElementType.Light => new Color(0.98f, 0.85f, 0.25f), // Gold
            HeroData.ElementType.Dark => new Color(0.75f, 0.35f, 0.95f),  // Purple
            _ => new Color(0.85f, 0.85f, 0.90f)
        };

        // 1. Dark Screen Tint
        var darkTint = new ColorRect
        {
            Size = new Vector2(GameConstants.ViewportWidth, GameConstants.ViewportHeight),
            Color = new Color(0, 0, 0, 0),
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(darkTint);

        // 2. Cinematic Center Strip
        float stripH = 130f;
        float stripY = (GameConstants.ViewportHeight - stripH) / 2.0f;

        var stripContainer = new Control
        {
            Position = new Vector2(isEnemy ? 1280 : -1280, stripY),
            Size = new Vector2(GameConstants.ViewportWidth, stripH),
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(stripContainer);

        var stripBg = new ColorRect
        {
            Size = new Vector2(GameConstants.ViewportWidth, stripH),
            Color = new Color(0.06f, 0.05f, 0.10f, 0.92f)
        };
        stripContainer.AddChild(stripBg);

        // Top & Bottom elemental glowing borders
        var topBorder = new ColorRect
        {
            Position = new Vector2(0, 0),
            Size = new Vector2(GameConstants.ViewportWidth, 3),
            Color = elemColor
        };
        stripContainer.AddChild(topBorder);

        var btmBorder = new ColorRect
        {
            Position = new Vector2(0, stripH - 3),
            Size = new Vector2(GameConstants.ViewportWidth, 3),
            Color = elemColor
        };
        stripContainer.AddChild(btmBorder);

        // 3. Hero Portrait (64x64 scaled 2.5x with nearest-neighbor)
        var portrait = new TextureRect
        {
            CustomMinimumSize = new Vector2(160, 160),
            Position = new Vector2(isEnemy ? 920 : 180, -15),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            TextureFilter = TextureFilterEnum.Nearest,
            FlipH = isEnemy
        };
        var pTex = GD.Load<Texture2D>($"res://assets/heroes/{hero.HeroId}/portrait.png");
        if (pTex != null)
        {
            portrait.Texture = pTex;
        }
        stripContainer.AddChild(portrait);

        // Portrait glowing aura
        var portraitGlow = new TextureRect
        {
            CustomMinimumSize = new Vector2(160, 160),
            Position = portrait.Position,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            TextureFilter = TextureFilterEnum.Nearest,
            Texture = pTex,
            Modulate = elemColor with { A = 0.45f },
            Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add },
            FlipH = isEnemy
        };
        stripContainer.AddChild(portraitGlow);

        // 4. Hero Title & Skill Labels
        float textX = isEnemy ? 220 : 380;
        var infoBox = new VBoxContainer
        {
            Position = new Vector2(textX, 22),
            Size = new Vector2(680, 85)
        };
        infoBox.AddThemeConstantOverride("separation", 4);
        stripContainer.AddChild(infoBox);

        var titleLbl = new Label
        {
            Text = $"★ {hero.Title.ToUpper()} ★"
        };
        titleLbl.AddThemeFontSizeOverride("font_size", 14);
        titleLbl.AddThemeColorOverride("font_color", new Color(1.0f, 0.88f, 0.4f));
        titleLbl.AddThemeConstantOverride("outline_size", 2);
        titleLbl.AddThemeColorOverride("font_outline_color", Colors.Black);
        infoBox.AddChild(titleLbl);

        var skillLbl = new Label
        {
            Text = $"⚡ {skill.SkillName.ToUpper()} ⚡"
        };
        skillLbl.AddThemeFontSizeOverride("font_size", 26);
        skillLbl.AddThemeColorOverride("font_color", elemColor);
        skillLbl.AddThemeConstantOverride("outline_size", 4);
        skillLbl.AddThemeColorOverride("font_outline_color", new Color(0.1f, 0.05f, 0.15f));
        infoBox.AddChild(skillLbl);

        // 5. Execution Timeline
        float durIn = 0.20f / speedMult;
        float durHold = 0.45f / speedMult;
        float durOut = 0.16f / speedMult;

        var tw = CreateTween();

        // Step A: Darken screen & Slide In with dramatic momentum
        tw.TweenProperty(darkTint, "color:a", 0.65f, durIn);
        tw.Parallel().TweenProperty(stripContainer, "position:x", 0.0f, durIn)
                     .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);

        // Glow flash on portrait
        tw.Parallel().TweenProperty(portraitGlow, "scale", new Vector2(1.15f, 1.15f), durIn);

        // Step B: Hold & Shimmer
        tw.TweenInterval(durHold);

        // Step C: Rapid Slash Out
        float exitX = isEnemy ? -1280f : 1280f;
        tw.TweenProperty(stripContainer, "position:x", exitX, durOut)
          .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        tw.Parallel().TweenProperty(darkTint, "color:a", 0.0f, durOut);

        var tcs = new TaskCompletionSource<bool>();
        tw.TweenCallback(Callable.From(() => tcs.TrySetResult(true)));

        await tcs.Task;
    }
}
