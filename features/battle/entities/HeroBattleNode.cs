using System;
using Godot;
using TamQuoc3D.Core.Audio;
using TamQuoc3D.Core.Constants;
using TamQuoc3D.Data.Resources;

namespace TamQuoc3D.Features.Battle.Entities;

/// <summary>
/// HeroBattleNode - Node do hoa & hoat hoa chien dau 2D tren san dau.
/// </summary>
[GlobalClass]
public partial class HeroBattleNode : Node2D
{
    [Signal]
    public delegate void SkillBannerFinishedEventHandler();

    public BattleHero BattleHero { get; set; }
    public bool IsEnemy { get; set; } = false;
    public Vector2 HomePosition { get; set; } = Vector2.Zero;

    // Visual nodes
    public Sprite2D Pedestal { get; private set; }
    public Sprite2D SkillReadyAura { get; private set; }
    public Sprite2D ActivePedestal { get; private set; }
    public Sprite2D Sprite { get; private set; }
    public HealthBar HealthBarNode { get; private set; }

    // Tweens
    private Tween _idleTween;
    private Tween _activeTween;
    private Tween _skillReadyTween;
    private bool _isDead = false;

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

        // 2. Skill Ready Blazing Aura
        SkillReadyAura = new Sprite2D();
        var srTex = GD.Load<Texture2D>("res://assets/ui/shared/pedestal_skill_ready.png");
        if (srTex != null)
        {
            SkillReadyAura.Texture = srTex;
        }
        SkillReadyAura.Position = Vector2.Zero;
        SkillReadyAura.Scale = new Vector2(1.15f, 1.15f);
        var srMat = new CanvasItemMaterial
        {
            BlendMode = CanvasItemMaterial.BlendModeEnum.Add
        };
        SkillReadyAura.Material = srMat;
        SkillReadyAura.Visible = false;
        SkillReadyAura.ZIndex = LayerConstants.SkillReadyAuraZ;
        AddChild(SkillReadyAura);

        // 3. Active Turn Glow
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

        // 4. Hero Spritesheet (64x64 HD Spritesheet)
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
        AddChild(Sprite);

        // 5. Overhead HealthBar Component
        HealthBarNode = new HealthBar();
        AddChild(HealthBarNode);
        HealthBarNode.Setup(BattleHero.Data);
    }

    public void ShowSkillBanner(string skillName, float speedMult = 1.0f)
    {
        var banner = new PanelContainer
        {
            Position = new Vector2(-80, -152),
            Size = new Vector2(160, 26),
            ZIndex = LayerConstants.SkillBannerZ,
            PivotOffset = new Vector2(80, 13)
        };

        var sb = new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.06f, 0.1f, 0.95f)
        };
        sb.SetBorderWidthAll(2);

        Color borderCol = BattleHero.Data.Element switch
        {
            HeroData.ElementType.Fire => new Color(1.0f, 0.35f, 0.2f),
            HeroData.ElementType.Water => new Color(0.25f, 0.8f, 1.0f),
            HeroData.ElementType.Wind => new Color(0.3f, 0.95f, 0.6f),
            HeroData.ElementType.Dark => new Color(0.85f, 0.45f, 1.0f),
            HeroData.ElementType.Light => new Color(1.0f, 0.95f, 0.4f),
            _ => new Color(1.0f, 0.85f, 0.3f)
        };
        string iconSym = BattleHero.Data.Element switch
        {
            HeroData.ElementType.Fire => "🔥",
            HeroData.ElementType.Water => "💧",
            HeroData.ElementType.Wind => "🌪️",
            HeroData.ElementType.Dark => "🌘",
            HeroData.ElementType.Light => "⚡",
            _ => "⚔️"
        };

        sb.BorderColor = borderCol;
        sb.SetCornerRadiusAll(13);
        sb.ContentMarginLeft = 12;
        sb.ContentMarginRight = 12;
        sb.ContentMarginTop = 4;
        sb.ContentMarginBottom = 4;
        banner.AddThemeStyleboxOverride("panel", sb);

        var lbl = new Label
        {
            Text = $"{iconSym} {skillName}",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        lbl.AddThemeFontSizeOverride("font_size", 12);
        lbl.AddThemeColorOverride("font_color", new Color(1.0f, 0.95f, 0.85f));
        lbl.AddThemeColorOverride("font_shadow_color", Colors.Black);
        lbl.AddThemeConstantOverride("shadow_offset_x", 1);
        lbl.AddThemeConstantOverride("shadow_offset_y", 1);
        lbl.AddThemeConstantOverride("shadow_outline_size", 2);
        banner.AddChild(lbl);

        AddChild(banner);

        banner.Scale = new Vector2(0.3f, 0.3f);
        banner.Modulate = new Color(banner.Modulate.R, banner.Modulate.G, banner.Modulate.B, 0.0f);

        float durIn = GameConstants.SkillBannerInDuration / speedMult;
        float durStay = GameConstants.SkillBannerStayDuration / speedMult;
        float durOut = GameConstants.SkillBannerOutDuration / speedMult;

        var tw = banner.CreateTween();
        tw.TweenProperty(banner, "scale", new Vector2(1.15f, 1.15f), durIn * 0.7f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        tw.Parallel().TweenProperty(banner, "modulate:a", 1.0f, durIn * 0.5f);
        tw.TweenProperty(banner, "scale", new Vector2(1.0f, 1.0f), durIn * 0.3f);
        tw.TweenInterval(durStay);
        tw.TweenProperty(banner, "position:y", -172f, durOut).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        tw.Parallel().TweenProperty(banner, "modulate:a", 0.0f, durOut);
        tw.TweenCallback(Callable.From(() =>
        {
            banner.QueueFree();
            EmitSignal(SignalName.SkillBannerFinished);
        }));
    }

    public void SetActiveTurn(bool active)
    {
        ActivePedestal.Visible = active;
        if (active)
        {
            if (_activeTween != null && _activeTween.IsValid())
            {
                _activeTween.Kill();
            }
            _activeTween = CreateTween().SetLoops();
            _activeTween.TweenProperty(ActivePedestal, "modulate:a", 1.0f, 0.3f);
            _activeTween.TweenProperty(ActivePedestal, "modulate:a", 0.4f, 0.3f);
        }
        else
        {
            if (_activeTween != null && _activeTween.IsValid())
            {
                _activeTween.Kill();
            }
            ActivePedestal.Modulate = new Color(ActivePedestal.Modulate.R, ActivePedestal.Modulate.G, ActivePedestal.Modulate.B, 1.0f);
        }
    }

    public void SetSkillReady(bool ready)
    {
        if (_isDead || !GodotObject.IsInstanceValid(SkillReadyAura))
        {
            return;
        }

        if (ready)
        {
            if (!SkillReadyAura.Visible || _skillReadyTween == null)
            {
                SkillReadyAura.Visible = true;
                if (_skillReadyTween != null && _skillReadyTween.IsValid())
                {
                    _skillReadyTween.Kill();
                }
                _skillReadyTween = CreateTween().SetLoops();
                _skillReadyTween.TweenProperty(SkillReadyAura, "scale", new Vector2(1.22f, 1.22f), 0.45f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
                _skillReadyTween.Parallel().TweenProperty(SkillReadyAura, "modulate", new Color(1.2f, 1.15f, 0.9f, 1.0f), 0.45f);
                _skillReadyTween.TweenProperty(SkillReadyAura, "scale", new Vector2(1.08f, 1.08f), 0.45f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
                _skillReadyTween.Parallel().TweenProperty(SkillReadyAura, "modulate", new Color(0.95f, 0.8f, 0.45f, 0.75f), 0.45f);
            }
        }
        else
        {
            SkillReadyAura.Visible = false;
            if (_skillReadyTween != null && _skillReadyTween.IsValid())
            {
                _skillReadyTween.Kill();
                _skillReadyTween = null;
            }
        }
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
        if (HealthBarNode?.StatusRow == null) return;

        foreach (var child in HealthBarNode.StatusRow.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var st in BattleHero.StatusEffects)
        {
            if (st.TryGetValue("type", out var typeVal))
            {
                var t = (SkillData.EffectType)(int)typeVal;
                string sName = t switch
                {
                    SkillData.EffectType.Stun => "stun",
                    SkillData.EffectType.Silence => "silence",
                    SkillData.EffectType.BuffAtk => "buff_atk",
                    SkillData.EffectType.BuffDef => "buff_def",
                    SkillData.EffectType.BuffSpeed => "buff_spd",
                    SkillData.EffectType.DebuffAtk => "debuff_atk",
                    SkillData.EffectType.DebuffDef => "debuff_def",
                    _ => string.Empty
                };

                if (!string.IsNullOrEmpty(sName))
                {
                    var texRect = new TextureRect
                    {
                        Texture = GD.Load<Texture2D>($"res://assets/common/icons/status_{sName}.png"),
                        CustomMinimumSize = new Vector2(14, 14),
                        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize
                    };
                    HealthBarNode.StatusRow.AddChild(texRect);
                }
            }
        }
    }

    public void PlayIdle()
    {
        if (_isDead) return;
        if (_idleTween != null && _idleTween.IsValid()) _idleTween.Kill();

        _idleTween = CreateTween().SetLoops();
        _idleTween.TweenCallback(Callable.From(() => Sprite.Frame = 0));
        _idleTween.TweenInterval(0.4f);
        _idleTween.TweenCallback(Callable.From(() => Sprite.Frame = 1));
        _idleTween.TweenInterval(0.4f);
    }

    public void AnimateAttack(Vector2 targetPos, Action onImpact, Action onComplete, float speedMult = 1.0f)
    {
        if (_idleTween != null && _idleTween.IsValid()) _idleTween.Kill();

        var tw = CreateTween();
        var attackDest = targetPos + (IsEnemy ? new Vector2(55, 0) : new Vector2(-55, 0));
        float durDash = GameConstants.AttackDashDuration / speedMult;
        float durHold = GameConstants.AttackHoldDuration / speedMult;
        float durBack = GameConstants.AttackBackDuration / speedMult;

        tw.TweenProperty(this, "position", attackDest, durDash).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        tw.TweenCallback(Callable.From(() =>
        {
            Sprite.Frame = 2;
            SoundManager.Instance?.PlaySlash();
            onImpact?.Invoke();
        }));

        tw.TweenInterval(durHold);

        tw.TweenCallback(Callable.From(() => Sprite.Frame = 0));
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

        var tw = CreateTween();
        float durForward = 0.22f / speedMult;
        float durHold = 0.42f / speedMult;
        float durBack = 0.22f / speedMult;

        var stepForward = HomePosition + (IsEnemy ? new Vector2(-18, 0) : new Vector2(18, 0));
        tw.TweenProperty(this, "position", stepForward, durForward);
        tw.TweenCallback(Callable.From(() =>
        {
            Sprite.Frame = 2;
            SoundManager.Instance?.PlayMagic();
            onImpact?.Invoke();
        }));
        tw.TweenInterval(durHold);
        tw.TweenProperty(this, "position", HomePosition, durBack);
        tw.TweenCallback(Callable.From(() =>
        {
            Sprite.Frame = 0;
            PlayIdle();
            onComplete?.Invoke();
        }));
    }

    public void AnimateHurt(bool isDodge, bool isBlock, float speedMult = 1.0f)
    {
        if (_isDead) return;
        if (_idleTween != null && _idleTween.IsValid()) _idleTween.Kill();

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

        Sprite.Frame = 3;
        var knockback = IsEnemy ? new Vector2(18, 0) : new Vector2(-18, 0);
        tw.TweenProperty(this, "position", HomePosition + knockback, dur * 0.4f);
        tw.Parallel().TweenProperty(Sprite, "modulate", new Color(2.5f, 2.5f, 2.5f), dur * 0.4f);

        tw.TweenProperty(this, "position", HomePosition, dur * 0.6f);
        tw.Parallel().TweenProperty(Sprite, "modulate", Colors.White, dur * 0.6f);
        tw.TweenCallback(Callable.From(() =>
        {
            Sprite.Frame = 0;
            PlayIdle();
        }));
    }

    public void AnimateDeath(float speedMult = 1.0f)
    {
        _isDead = true;
        SetSkillReady(false);
        SetActiveTurn(false);
        if (_idleTween != null && _idleTween.IsValid()) _idleTween.Kill();

        Sprite.Frame = 3;
        var tw = CreateTween();
        float dur = 0.65f / speedMult;
        tw.TweenProperty(this, "position:y", Position.Y + 12f, dur);
        tw.Parallel().TweenProperty(this, "modulate", new Color(0.2f, 0.2f, 0.2f, 0.0f), dur);
        tw.TweenCallback(Callable.From(() => Visible = false));
    }

    public void ShowFloatingText(string txt, Color color, bool isCrit = false)
    {
        var ft = new FloatingText
        {
            DisplayText = txt,
            TextColor = color,
            IsCrit = isCrit,
            Position = Position + new Vector2(0, -75)
        };
        GetParent().AddChild(ft);
    }
}

/// <summary>
/// Alias HeroVisual giup tuong thich toan bo code cu ma khong bi gay lien ket.
/// </summary>
[GlobalClass]
public partial class HeroVisual : HeroBattleNode {}
