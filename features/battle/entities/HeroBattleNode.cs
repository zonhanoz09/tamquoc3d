using System;
using Godot;
using TamQuoc3D.Core.Constants;
using TamQuoc3D.Core.Audio;
using TamQuoc3D.Data.Resources;

namespace TamQuoc3D.Features.Battle.Entities;

/// <summary>
/// HeroBattleNode - Node do hoa & hoat hoa chien dau 2D tren san dau (.NET 8).
/// Ho tro ca AnimatedSprite2D HD-2D nhieu frame lan Sprite2D co dien.
/// </summary>
[GlobalClass]
public partial class HeroBattleNode : Node2D
{
    [Signal]
    public delegate void SkillBannerFinishedEventHandler();

    public BattleHero BattleHero { get; private set; }
    public bool IsEnemy { get; set; } = false;
    public Vector2 HomePosition { get; set; } = Vector2.Zero;

    // Visual nodes
    public Sprite2D Pedestal { get; private set; }
    public Sprite2D FootShadow { get; private set; }
    public Sprite2D SkillReadyAura { get; private set; }
    public Sprite2D ActivePedestal { get; private set; }
    public Sprite2D Sprite { get; private set; }
    public AnimatedSprite2D AnimatedSprite { get; private set; }
    public HealthBar HealthBarNode { get; private set; }

    // Tweens & State
    private Tween _idleTween;
    private Tween _activeTween;
    private Tween _skillReadyTween;
    private bool _isDead = false;

    public bool IsDead => _isDead;

    public void Setup(BattleHero bh, bool enemy)
    {
        BattleHero = bh;
        IsEnemy = enemy;
        BattleHero.Node = this;
        BuildVisuals();
        UpdateBars(false);
        PlayIdle();
    }

    private void BuildVisuals()
    {
        // 1. Tactical Pedestal
        Pedestal = new Sprite2D();
        string pName = IsEnemy ? "pedestal_enemy" : "pedestal_player";
        var pTex = GD.Load<Texture2D>($"res://assets/ui/shared/{pName}.png");
        if (pTex != null)
        {
            Pedestal.Texture = pTex;
        }
        Pedestal.Position = Vector2.Zero;
        Pedestal.Scale = new Vector2(1.15f, 1.15f);
        Pedestal.ZIndex = LayerConstants.PedestalZ;
        AddChild(Pedestal);

        // 2. Dynamic Foot Shadow Ellipse (Co dãn theo độ cao chân & chuyển động)
        FootShadow = new Sprite2D();
        var shadowImg = Image.CreateEmpty(32, 16, false, Image.Format.Rgba8);
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float nx = (x - 15.5f) / 15.5f;
                float ny = (y - 7.5f) / 7.5f;
                float distSq = nx * nx + ny * ny;
                if (distSq <= 1.0f)
                {
                    float a = Mathf.Clamp(1.0f - distSq, 0f, 1f) * 0.42f;
                    shadowImg.SetPixel(x, y, new Color(0f, 0f, 0f, a));
                }
            }
        }
        FootShadow.Texture = ImageTexture.CreateFromImage(shadowImg);
        FootShadow.Position = new Vector2(0, -6);
        FootShadow.Scale = new Vector2(1.3f, 0.9f);
        FootShadow.ZIndex = LayerConstants.PedestalZ + 1;
        AddChild(FootShadow);

        // 3. Skill Ready Blazing Aura (Scaled neatly to hug the pedestal)
        SkillReadyAura = new Sprite2D();
        var srTex = GD.Load<Texture2D>("res://assets/ui/shared/pedestal_skill_ready.png");
        if (srTex != null)
        {
            SkillReadyAura.Texture = srTex;
        }
        SkillReadyAura.Position = Vector2.Zero;
        SkillReadyAura.Scale = new Vector2(0.65f, 0.65f);
        var srMat = new CanvasItemMaterial
        {
            BlendMode = CanvasItemMaterial.BlendModeEnum.Add
        };
        SkillReadyAura.Material = srMat;
        SkillReadyAura.Visible = false;
        SkillReadyAura.ZIndex = LayerConstants.SkillReadyAuraZ;
        AddChild(SkillReadyAura);

        // 4. Active Turn Glow
        ActivePedestal = new Sprite2D();
        var aTex = GD.Load<Texture2D>("res://assets/ui/shared/pedestal_active.png");
        if (aTex != null)
        {
            ActivePedestal.Texture = aTex;
        }
        ActivePedestal.Position = Vector2.Zero;
        ActivePedestal.Scale = new Vector2(1.15f, 1.15f);
        ActivePedestal.Visible = false;
        ActivePedestal.ZIndex = LayerConstants.ActivePedestalZ;
        AddChild(ActivePedestal);

        // 5. Hero Sprite: Kiểm tra xem có SpriteFrames HD-2D (.tres) hay không
        var sfPath = $"res://assets/heroes/{BattleHero.Data.HeroId}/sprite_frames.tres";
        if (ResourceLoader.Exists(sfPath))
        {
            var frames = GD.Load<SpriteFrames>(sfPath);
            if (frames != null)
            {
                AnimatedSprite = new AnimatedSprite2D
                {
                    SpriteFrames = frames,
                    Position = new Vector2(0, -48),
                    Scale = new Vector2(2.0f, 2.0f),
                    FlipH = IsEnemy,
                    ZIndex = LayerConstants.HeroSpriteZ
                };
                AddChild(AnimatedSprite);
            }
        }

        // Fallback: Sprite2D cổ điển nếu chưa có SpriteFrames
        Sprite = new Sprite2D();
        var sprPath = $"res://assets/heroes/{BattleHero.Data.HeroId}/spritesheet.png";
        var tex = GD.Load<Texture2D>(sprPath);
        if (tex != null)
        {
            Sprite.Texture = tex;
            Sprite.Hframes = 4;
            Sprite.Frame = 0;
        }
        Sprite.Position = new Vector2(0, -48);
        Sprite.Scale = new Vector2(2.0f, 2.0f);
        Sprite.FlipH = IsEnemy;
        Sprite.ZIndex = LayerConstants.HeroSpriteZ;
        if (AnimatedSprite != null)
        {
            Sprite.Visible = false;
        }
        AddChild(Sprite);

        // 6. Overhead HealthBar Component
        HealthBarNode = new HealthBar();
        AddChild(HealthBarNode);
        HealthBarNode.Setup(BattleHero.Data);
    }

    public void ShowSkillBanner(string skillName, float speedMult = 1.0f)
    {
        var banner = new Control();
        banner.Position = new Vector2(0, -95);
        banner.ZIndex = LayerConstants.SkillBannerZ;
        AddChild(banner);

        var bg = new ColorRect
        {
            Position = new Vector2(-110, -14),
            Size = new Vector2(220, 28),
            Color = new Color(0.08f, 0.05f, 0.15f, 0.90f)
        };
        banner.AddChild(bg);

        var goldBorder = new ReferenceRect
        {
            Position = new Vector2(-110, -14),
            Size = new Vector2(220, 28),
            BorderColor = new Color(1.0f, 0.85f, 0.25f, 1.0f),
            BorderWidth = 2.0f,
            EditorOnly = false
        };
        banner.AddChild(goldBorder);

        var lbl = new Label
        {
            Text = $"⚡ {skillName} ⚡",
            Position = new Vector2(-110, -14),
            Size = new Vector2(220, 28),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        lbl.AddThemeFontSizeOverride("font_size", 13);
        lbl.AddThemeColorOverride("font_color", new Color(1.0f, 0.95f, 0.5f));
        banner.AddChild(lbl);

        banner.Scale = new Vector2(0.2f, 0.2f);
        banner.Modulate = new Color(1, 1, 1, 0);

        var tw = CreateTween();
        float durIn = GameConstants.SkillBannerInDuration / speedMult;
        float durStay = GameConstants.SkillBannerStayDuration / speedMult;
        float durOut = GameConstants.SkillBannerOutDuration / speedMult;

        tw.TweenProperty(banner, "scale", new Vector2(1.15f, 1.15f), durIn * 0.7f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        tw.Parallel().TweenProperty(banner, "modulate:a", 1.0f, durIn * 0.7f);
        tw.TweenProperty(banner, "scale", Vector2.One, durIn * 0.3f);
        tw.TweenInterval(durStay);
        tw.TweenProperty(banner, "scale", new Vector2(1.2f, 0.1f), durOut);
        tw.Parallel().TweenProperty(banner, "modulate:a", 0.0f, durOut);
        tw.TweenCallback(Callable.From(() =>
        {
            banner.QueueFree();
            EmitSignal(SignalName.SkillBannerFinished);
        }));
    }

    public void UpdateBars(bool animate = true)
    {
        if (_isDead) return;

        int currentHp = BattleHero.Stats.HpCurrent;
        int maxHp = BattleHero.Stats.HpMax;
        HealthBarNode?.UpdateHp(currentHp, maxHp, animate);

        bool isMaxRage = BattleHero.Stats.MoraleCurrent >= GameConstants.MaxMorale;
        SetSkillReady(isMaxRage);
    }

    public void UpdateStatusIcons()
    {
        // Status row visual updates if needed
    }

    public void SetActiveTurn(bool active)
    {
        ActivePedestal.Visible = active;
        if (active)
        {
            if (_activeTween != null && _activeTween.IsValid()) _activeTween.Kill();
            _activeTween = CreateTween().SetLoops();
            _activeTween.TweenProperty(ActivePedestal, "modulate:a", 0.5f, 0.45f);
            _activeTween.TweenProperty(ActivePedestal, "modulate:a", 1.0f, 0.45f);
        }
        else
        {
            if (_activeTween != null && _activeTween.IsValid()) _activeTween.Kill();
            ActivePedestal.Modulate = Colors.White;
        }
    }

    public void SetSkillReady(bool ready)
    {
        SkillReadyAura.Visible = ready;
        if (ready)
        {
            if (_skillReadyTween != null && _skillReadyTween.IsValid()) _skillReadyTween.Kill();
            _skillReadyTween = CreateTween().SetLoops();
            _skillReadyTween.TweenProperty(SkillReadyAura, "scale", new Vector2(0.72f, 0.72f), 0.4f).SetTrans(Tween.TransitionType.Sine);
            _skillReadyTween.Parallel().TweenProperty(SkillReadyAura, "modulate:a", 0.6f, 0.4f);
            _skillReadyTween.TweenProperty(SkillReadyAura, "scale", new Vector2(0.62f, 0.62f), 0.4f).SetTrans(Tween.TransitionType.Sine);
            _skillReadyTween.Parallel().TweenProperty(SkillReadyAura, "modulate:a", 1.0f, 0.4f);
        }
        else
        {
            if (_skillReadyTween != null && _skillReadyTween.IsValid()) _skillReadyTween.Kill();
            SkillReadyAura.Scale = new Vector2(0.65f, 0.65f);
            SkillReadyAura.Modulate = Colors.White;
        }
    }

    public void ShowFloatingText(string text, Color color, bool isCrit = false)
    {
        var lbl = new Label
        {
            Text = text,
            Position = new Vector2(-60, -75),
            Size = new Vector2(120, 24),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        lbl.AddThemeColorOverride("font_color", color);
        lbl.AddThemeFontSizeOverride("font_size", isCrit ? 16 : 13);
        lbl.ZIndex = LayerConstants.FloatingTextZ;
        AddChild(lbl);

        var tw = CreateTween();
        if (isCrit)
        {
            lbl.Scale = new Vector2(0.5f, 0.5f);
            tw.TweenProperty(lbl, "scale", new Vector2(1.3f, 1.3f), 0.12f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            tw.TweenProperty(lbl, "scale", Vector2.One, 0.1f);
        }

        tw.TweenProperty(lbl, "position:y", lbl.Position.Y - 32f, 0.65f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        tw.Parallel().TweenProperty(lbl, "modulate:a", 0.0f, 0.65f);
        tw.TweenCallback(Callable.From(lbl.QueueFree));
    }

    public void PlayIdle()
    {
        if (_isDead) return;
        if (_idleTween != null && _idleTween.IsValid()) _idleTween.Kill();

        if (AnimatedSprite != null)
        {
            AnimatedSprite.Play("idle");
            return;
        }

        _idleTween = CreateTween().SetLoops();
        _idleTween.TweenCallback(Callable.From(() => Sprite.Frame = 0));
        _idleTween.TweenInterval(0.4f);
        _idleTween.TweenCallback(Callable.From(() => Sprite.Frame = 1));
        _idleTween.TweenInterval(0.4f);
    }

    public void AnimateAttack(Vector2 targetPos, Action onImpact, Action onComplete, float speedMult = 1.0f)
    {
        if (_idleTween != null && _idleTween.IsValid()) _idleTween.Kill();

        if (AnimatedSprite != null)
        {
            AnimatedSprite.Play("attack");
        }

        var tw = CreateTween();
        var attackDest = targetPos + (IsEnemy ? new Vector2(55, 0) : new Vector2(-55, 0));
        float durDash = GameConstants.AttackDashDuration / speedMult;
        float durHold = GameConstants.AttackHoldDuration / speedMult;
        float durBack = GameConstants.AttackBackDuration / speedMult;

        // Juice: nhẹ nhàng kéo giãn (Squash & Stretch) tăng uy lực
        tw.TweenProperty(this, "position", attackDest, durDash).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        tw.Parallel().TweenProperty(this, "scale", new Vector2(1.08f, 0.94f), durDash * 0.35f);
        tw.TweenProperty(this, "scale", Vector2.One, durDash * 0.65f);

        // Dynamic shadow squash & stretch
        if (FootShadow != null)
        {
            tw.Parallel().TweenProperty(FootShadow, "scale", new Vector2(1.6f, 0.7f), durDash * 0.35f);
            tw.TweenProperty(FootShadow, "scale", new Vector2(1.3f, 0.9f), durDash * 0.65f);
        }

        tw.TweenCallback(Callable.From(() =>
        {
            if (AnimatedSprite == null) Sprite.Frame = 2;
            SoundManager.Instance?.PlaySlash();
            onImpact?.Invoke();
        }));

        tw.TweenInterval(durHold);

        tw.TweenCallback(Callable.From(() =>
        {
            if (AnimatedSprite == null) Sprite.Frame = 0;
        }));
        tw.TweenProperty(this, "position", HomePosition, durBack).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        tw.TweenCallback(Callable.From(() =>
        {
            PlayIdle();
            onComplete?.Invoke();
        }));
    }

    public void AnimateRangedAttack(Vector2 targetPos, Action onImpact, Action onComplete, float speedMult = 1.0f)
    {
        if (_idleTween != null && _idleTween.IsValid()) _idleTween.Kill();

        if (AnimatedSprite != null)
        {
            AnimatedSprite.Play("skill");
        }

        var tw = CreateTween();
        float durForward = 0.22f / speedMult;
        float durHold = 0.42f / speedMult;
        float durBack = 0.22f / speedMult;

        var stepForward = HomePosition + (IsEnemy ? new Vector2(-18, 0) : new Vector2(18, 0));
        tw.TweenProperty(this, "position", stepForward, durForward);

        if (FootShadow != null)
        {
            tw.Parallel().TweenProperty(FootShadow, "scale", new Vector2(1.0f, 0.7f), durForward);
            tw.Parallel().TweenProperty(FootShadow, "modulate:a", 0.5f, durForward);
        }

        tw.TweenCallback(Callable.From(() =>
        {
            if (AnimatedSprite == null) Sprite.Frame = 2;
            SoundManager.Instance?.PlayMagic();
            onImpact?.Invoke();
        }));
        tw.TweenInterval(durHold);
        tw.TweenProperty(this, "position", HomePosition, durBack);

        if (FootShadow != null)
        {
            tw.Parallel().TweenProperty(FootShadow, "scale", new Vector2(1.3f, 0.9f), durBack);
            tw.Parallel().TweenProperty(FootShadow, "modulate:a", 1.0f, durBack);
        }

        tw.TweenCallback(Callable.From(() =>
        {
            if (AnimatedSprite == null) Sprite.Frame = 0;
            PlayIdle();
            onComplete?.Invoke();
        }));
    }

    public void AnimateHurt(bool isDodge, bool isBlock, float speedMult = 1.0f)
    {
        if (_isDead) return;
        if (_idleTween != null && _idleTween.IsValid()) _idleTween.Kill();

        if (AnimatedSprite != null && !isDodge)
        {
            AnimatedSprite.Play("hurt");
        }

        var tw = CreateTween();
        float dur = 0.28f / speedMult;

        if (isDodge)
        {
            var dodgeOff = IsEnemy ? new Vector2(28, 0) : new Vector2(-28, 0);
            tw.TweenProperty(this, "position", HomePosition + dodgeOff, dur * 0.5f);
            tw.TweenProperty(this, "modulate:a", 0.5f, dur * 0.5f);
            tw.TweenProperty(this, "position", HomePosition, dur * 0.5f);
            tw.Parallel().TweenProperty(this, "modulate:a", 1.0f, dur * 0.5f);
            tw.TweenCallback(Callable.From(PlayIdle));
            return;
        }

        if (AnimatedSprite == null) Sprite.Frame = 3;
        var knockback = IsEnemy ? new Vector2(18, 0) : new Vector2(-18, 0);
        tw.TweenProperty(this, "position", HomePosition + knockback, dur * 0.4f);

        CanvasItem activeSpr = AnimatedSprite != null ? AnimatedSprite : Sprite;
        tw.Parallel().TweenProperty(activeSpr, "modulate", new Color(2.5f, 2.5f, 2.5f), dur * 0.4f);

        tw.TweenProperty(this, "position", HomePosition, dur * 0.6f);
        tw.Parallel().TweenProperty(activeSpr, "modulate", Colors.White, dur * 0.6f);
        tw.TweenCallback(Callable.From(() =>
        {
            if (AnimatedSprite == null) Sprite.Frame = 0;
            PlayIdle();
        }));
    }

    public void AnimateDeath(float speedMult = 1.0f)
    {
        if (_isDead) return;
        _isDead = true;
        SetSkillReady(false);
        SetActiveTurn(false);
        if (_idleTween != null && _idleTween.IsValid()) _idleTween.Kill();

        if (AnimatedSprite != null)
        {
            AnimatedSprite.Play("death");
        }
        else
        {
            Sprite.Frame = 3;
        }

        var tw = CreateTween();
        float dur = 0.65f / speedMult;
        tw.TweenProperty(this, "position:y", Position.Y + 12f, dur);
        tw.Parallel().TweenProperty(this, "modulate:a", 0.0f, dur);
        if (FootShadow != null)
        {
            tw.Parallel().TweenProperty(FootShadow, "modulate:a", 0.0f, dur);
        }
        tw.TweenCallback(Callable.From(() =>
        {
            Visible = false;
        }));
    }
}
