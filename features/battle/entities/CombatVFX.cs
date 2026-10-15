using System.Collections.Generic;
using Godot;
using TamQuoc3D.Core.Constants;

namespace TamQuoc3D.Features.Battle.Entities;

/// <summary>
/// CombatVFX - Quan ly sinh hieu ung Pixel VFX chuyen biet theo tung tuong va ky nang.
/// </summary>
[GlobalClass]
public partial class CombatVFX : Node2D
{
    private static readonly Dictionary<string, string> HeroVfxHeroMap = new()
    {
        { "vfx_spear_blue", "trieu_van" },
        { "vfx_dragon_white", "trieu_van" },
        { "vfx_cross_axes", "dien_vi" },
        { "vfx_magma_eruption", "dien_vi" },
        { "vfx_serpent_spear", "truong_phi" },
        { "vfx_sonic_roar", "truong_phi" },
        { "vfx_shield_strike", "tao_nhan" },
        { "vfx_fortress_shield", "tao_nhan" },
        { "vfx_water_smash", "hoang_cai" },
        { "vfx_fire_ship", "hoang_cai" },
        { "vfx_crescent_blade_green", "quan_vu" },
        { "vfx_dragon_blade_ultimate", "quan_vu" },
        { "vfx_halberd_crimson", "lu_bo" },
        { "vfx_demonic_meteor", "lu_bo" },
        { "vfx_fire_sword", "chu_du" },
        { "vfx_inferno_storm", "chu_du" },
        { "vfx_arrow_gold", "hoang_trung" },
        { "vfx_sniper_beam", "hoang_trung" },
        { "vfx_cavalry_blade", "ma_sieu" },
        { "vfx_cavalry_charge", "ma_sieu" },
        { "vfx_silk_ribbon", "dieu_thuyen" },
        { "vfx_crescent_moon", "dieu_thuyen" },
        { "vfx_bagua_circle", "gia_cat_luong" },
        { "vfx_divine_storm", "gia_cat_luong" },
        { "vfx_music_water", "tieu_kieu" },
        { "vfx_lotus_healing", "tieu_kieu" },
        { "vfx_fire_talisman", "bang_thong" },
        { "vfx_phoenix_chains", "bang_thong" },
        { "vfx_guqin_waves", "sai_van_co" },
        { "vfx_celestial_zither", "sai_van_co" }
    };

    public static void SpawnHeroAttackVfx(Node parent, string heroId, Vector2 targetPos, Vector2 attackerPos, bool isEnemy, bool isCrit = false)
    {
        var hitPos = targetPos + new Vector2(0, -30);

        if (isCrit)
        {
            SpawnImpact(parent, hitPos, true);
        }

        switch (heroId)
        {
            case "trieu_van":
                SpawnLinearPierce(parent, hitPos, "vfx_spear_blue", isEnemy, new Vector2(1.8f, 1.8f), 0.22f);
                break;
            case "dien_vi":
                SpawnBurstVfx(parent, hitPos, "vfx_cross_axes", new Vector2(1.8f, 1.8f), 0.28f, true);
                break;
            case "truong_phi":
                SpawnSlashVfx(parent, hitPos, "vfx_serpent_spear", isEnemy, new Vector2(1.8f, 1.8f), 0.25f);
                break;
            case "tao_nhan":
                SpawnBurstVfx(parent, hitPos, "vfx_shield_strike", new Vector2(1.6f, 1.6f), 0.24f, false);
                break;
            case "hoang_cai":
                SpawnBurstVfx(parent, hitPos, "vfx_water_smash", new Vector2(1.7f, 1.7f), 0.3f, false);
                break;
            case "quan_vu":
                SpawnSlashVfx(parent, hitPos, "vfx_crescent_blade_green", isEnemy, new Vector2(1.9f, 1.9f), 0.26f);
                break;
            case "lu_bo":
                SpawnSlashVfx(parent, hitPos, "vfx_halberd_crimson", isEnemy, new Vector2(2.0f, 2.0f), 0.28f);
                break;
            case "chu_du":
                SpawnBurstVfx(parent, hitPos, "vfx_fire_sword", new Vector2(1.8f, 1.8f), 0.25f, false);
                break;
            case "hoang_trung":
                SpawnProjectile(parent, attackerPos + new Vector2(0, -35), hitPos, "vfx_arrow_gold", isEnemy, 0.2f);
                break;
            case "ma_sieu":
                SpawnSlashVfx(parent, hitPos, "vfx_cavalry_blade", isEnemy, new Vector2(1.8f, 1.8f), 0.22f);
                break;
            case "dieu_thuyen":
                SpawnBurstVfx(parent, hitPos, "vfx_silk_ribbon", new Vector2(1.8f, 1.8f), 0.32f, true);
                break;
            case "gia_cat_luong":
                SpawnBurstVfx(parent, targetPos + new Vector2(0, -10), "vfx_bagua_circle", new Vector2(1.6f, 1.6f), 0.35f, true);
                break;
            case "tieu_kieu":
                SpawnBurstVfx(parent, hitPos, "vfx_music_water", new Vector2(1.6f, 1.6f), 0.32f, false);
                break;
            case "bang_thong":
                SpawnBurstVfx(parent, hitPos, "vfx_fire_talisman", new Vector2(1.7f, 1.7f), 0.3f, false);
                break;
            case "sai_van_co":
                SpawnBurstVfx(parent, hitPos, "vfx_guqin_waves", new Vector2(1.6f, 1.6f), 0.3f, false);
                break;
            default:
                SpawnSlash(parent, hitPos, isEnemy);
                break;
        }
    }

    public static void SpawnHeroSkillVfx(Node parent, string heroId, Vector2 targetPos, Vector2 attackerPos, IReadOnlyList<Vector2> allTargetPositions, bool isEnemy)
    {
        var hitPos = targetPos + new Vector2(0, -30);

        switch (heroId)
        {
            case "trieu_van":
                SpawnDragonRush(parent, attackerPos + new Vector2(0, -35), targetPos + new Vector2(0, -35), "vfx_dragon_white", isEnemy);
                break;
            case "dien_vi":
                SpawnMagmaQuake(parent, allTargetPositions);
                break;
            case "truong_phi":
                SpawnSonicWaveRow(parent, targetPos + new Vector2(0, -25));
                break;
            case "tao_nhan":
                SpawnFortressBarrier(parent, allTargetPositions);
                break;
            case "hoang_cai":
                SpawnFireShipCharge(parent, targetPos + new Vector2(0, -20), isEnemy);
                break;
            case "quan_vu":
                SpawnDivineDragonBlade(parent, targetPos + new Vector2(0, -20), allTargetPositions, isEnemy);
                break;
            case "lu_bo":
                SpawnDemonicMeteorStrike(parent, targetPos + new Vector2(0, -25), allTargetPositions);
                break;
            case "chu_du":
                SpawnRedCliffInferno(parent, allTargetPositions);
                break;
            case "hoang_trung":
                SpawnSniperHyperbeam(parent, attackerPos + new Vector2(0, -35), targetPos + new Vector2(0, -35), isEnemy);
                break;
            case "ma_sieu":
                SpawnPhantomCavalry(parent, attackerPos, targetPos, isEnemy);
                break;
            case "dieu_thuyen":
                SpawnMoonlightDescent(parent, targetPos + new Vector2(0, -35));
                break;
            case "gia_cat_luong":
                SpawnDivineStormFormation(parent, allTargetPositions);
                break;
            case "tieu_kieu":
                SpawnSacredLotusHeal(parent, allTargetPositions);
                break;
            case "bang_thong":
                SpawnPhoenixChainExplosion(parent, allTargetPositions);
                break;
            case "sai_van_co":
                SpawnCelestialZitherRings(parent, allTargetPositions);
                break;
            default:
                SpawnImpact(parent, hitPos, true);
                break;
        }
    }

    private static Sprite2D CreateVfxSprite(string texName)
    {
        var spr = new Sprite2D();
        Texture2D tex = null;

        if (HeroVfxHeroMap.TryGetValue(texName, out var heroId))
        {
            tex = GD.Load<Texture2D>($"res://assets/heroes/{heroId}/vfx/{texName}.png");
        }

        if (tex == null)
        {
            tex = GD.Load<Texture2D>($"res://assets/vfx/physical/{texName}.png")
               ?? GD.Load<Texture2D>($"res://assets/vfx/hits/{texName}.png")
               ?? GD.Load<Texture2D>($"res://assets/vfx/fire/{texName}.png")
               ?? GD.Load<Texture2D>($"res://assets/vfx/water/{texName}.png")
               ?? GD.Load<Texture2D>($"res://assets/vfx/lightning/{texName}.png");
        }

        if (tex != null)
        {
            spr.Texture = tex;
        }

        var mat = new CanvasItemMaterial
        {
            BlendMode = CanvasItemMaterial.BlendModeEnum.Add
        };
        spr.Material = mat;
        spr.ZIndex = LayerConstants.HeroSkillVfxZ;
        return spr;
    }

    private static void SpawnLinearPierce(Node parent, Vector2 pos, string tex, bool isEnemy, Vector2 scaleVal, float dur)
    {
        var spr = CreateVfxSprite(tex);
        spr.Position = pos;
        spr.Scale = new Vector2(0.3f, scaleVal.Y);
        spr.FlipH = isEnemy;
        parent.AddChild(spr);

        var tw = spr.CreateTween();
        tw.TweenProperty(spr, "scale:x", scaleVal.X, dur * 0.4f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        tw.Parallel().TweenProperty(spr, "position:x", pos.X + (isEnemy ? -20f : 20f), dur * 0.4f);
        tw.TweenProperty(spr, "modulate:a", 0.0f, dur * 0.6f);
        tw.TweenCallback(Callable.From(spr.QueueFree));
    }

    private static void SpawnBurstVfx(Node parent, Vector2 pos, string tex, Vector2 scaleVal, float dur, bool rotateEff = false)
    {
        var spr = CreateVfxSprite(tex);
        spr.Position = pos;
        spr.Scale = scaleVal * 0.6f;
        parent.AddChild(spr);

        var tw = spr.CreateTween();
        tw.TweenProperty(spr, "scale", scaleVal * 1.25f, dur * 0.5f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        if (rotateEff)
        {
            tw.Parallel().TweenProperty(spr, "rotation", Mathf.DegToRad(45.0f), dur);
        }
        tw.TweenProperty(spr, "scale", scaleVal * 1.4f, dur * 0.5f);
        tw.Parallel().TweenProperty(spr, "modulate:a", 0.0f, dur * 0.5f);
        tw.TweenCallback(Callable.From(spr.QueueFree));
    }

    private static void SpawnSlashVfx(Node parent, Vector2 pos, string tex, bool isEnemy, Vector2 scaleVal, float dur)
    {
        var spr = CreateVfxSprite(tex);
        spr.Position = pos;
        spr.Scale = scaleVal * 0.7f;
        spr.FlipH = isEnemy;
        parent.AddChild(spr);

        var tw = spr.CreateTween();
        tw.TweenProperty(spr, "scale", scaleVal * 1.3f, dur * 0.45f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        tw.TweenProperty(spr, "modulate:a", 0.0f, dur * 0.55f);
        tw.TweenCallback(Callable.From(spr.QueueFree));
    }

    private static void SpawnProjectile(Node parent, Vector2 fromPos, Vector2 toPos, string tex, bool isEnemy, float dur)
    {
        var spr = CreateVfxSprite(tex);
        spr.Position = fromPos;
        spr.Scale = new Vector2(1.5f, 1.5f);
        spr.FlipH = isEnemy;
        parent.AddChild(spr);

        var tw = spr.CreateTween();
        tw.TweenProperty(spr, "position", toPos, dur).SetTrans(Tween.TransitionType.Linear);
        tw.TweenCallback(Callable.From(() =>
        {
            SpawnImpact(parent, toPos, false);
            spr.QueueFree();
        }));
    }

    private static void SpawnDragonRush(Node parent, Vector2 fromPos, Vector2 toPos, string tex, bool isEnemy)
    {
        var spr = CreateVfxSprite(tex);
        spr.Position = fromPos;
        spr.Scale = new Vector2(1.8f, 1.8f);
        spr.FlipH = isEnemy;
        parent.AddChild(spr);

        var tw = spr.CreateTween();
        tw.TweenProperty(spr, "position", toPos, 0.35f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        tw.Parallel().TweenProperty(spr, "scale", new Vector2(2.5f, 2.5f), 0.35f);
        tw.TweenProperty(spr, "modulate:a", 0.0f, 0.25f);
        tw.TweenCallback(Callable.From(spr.QueueFree));
    }

    private static void SpawnMagmaQuake(Node parent, IReadOnlyList<Vector2> targets)
    {
        foreach (var tPos in targets)
        {
            var spr = CreateVfxSprite("vfx_magma_eruption");
            spr.Position = tPos + new Vector2(0, -20);
            spr.Scale = new Vector2(0.5f, 0.5f);
            parent.AddChild(spr);

            var tw = spr.CreateTween();
            tw.TweenProperty(spr, "scale", new Vector2(2.0f, 2.0f), 0.3f).SetTrans(Tween.TransitionType.Bounce).SetEase(Tween.EaseType.Out);
            tw.TweenProperty(spr, "modulate:a", 0.0f, 0.4f);
            tw.TweenCallback(Callable.From(spr.QueueFree));
        }
    }

    private static void SpawnSonicWaveRow(Node parent, Vector2 centerPos)
    {
        var spr = CreateVfxSprite("vfx_sonic_roar");
        spr.Position = centerPos;
        spr.Scale = new Vector2(1.0f, 1.0f);
        parent.AddChild(spr);

        var tw = spr.CreateTween();
        tw.TweenProperty(spr, "scale", new Vector2(3.5f, 3.5f), 0.45f).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
        tw.Parallel().TweenProperty(spr, "modulate:a", 0.0f, 0.5f);
        tw.TweenCallback(Callable.From(spr.QueueFree));
    }

    private static void SpawnFortressBarrier(Node parent, IReadOnlyList<Vector2> allies)
    {
        foreach (var aPos in allies)
        {
            var spr = CreateVfxSprite("vfx_fortress_shield");
            spr.Position = aPos + new Vector2(0, -35);
            spr.Scale = new Vector2(0.5f, 0.5f);
            parent.AddChild(spr);

            var tw = spr.CreateTween();
            tw.TweenProperty(spr, "scale", new Vector2(1.6f, 1.6f), 0.3f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            tw.TweenInterval(0.4f);
            tw.TweenProperty(spr, "modulate:a", 0.0f, 0.3f);
            tw.TweenCallback(Callable.From(spr.QueueFree));
        }
    }

    private static void SpawnFireShipCharge(Node parent, Vector2 targetPos, bool isEnemy)
    {
        var spr = CreateVfxSprite("vfx_fire_ship");
        spr.Position = targetPos + (isEnemy ? new Vector2(80, -20) : new Vector2(-80, -20));
        spr.Scale = new Vector2(1.8f, 1.8f);
        spr.FlipH = isEnemy;
        parent.AddChild(spr);

        var tw = spr.CreateTween();
        tw.TweenProperty(spr, "position", targetPos + new Vector2(0, -20), 0.35f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        tw.TweenCallback(Callable.From(() =>
        {
            SpawnFire(parent, targetPos + new Vector2(0, -20));
            SpawnImpact(parent, targetPos + new Vector2(0, -20), true);
        }));
        tw.TweenProperty(spr, "scale", new Vector2(2.5f, 2.5f), 0.25f);
        tw.Parallel().TweenProperty(spr, "modulate:a", 0.0f, 0.25f);
        tw.TweenCallback(Callable.From(spr.QueueFree));
    }

    private static void SpawnDivineDragonBlade(Node parent, Vector2 targetPos, IReadOnlyList<Vector2> targets, bool isEnemy)
    {
        var spr = CreateVfxSprite("vfx_dragon_blade_ultimate");
        spr.Position = targetPos + new Vector2(0, -70);
        spr.Scale = new Vector2(1.2f, 1.2f);
        spr.FlipH = isEnemy;
        parent.AddChild(spr);

        var tw = spr.CreateTween();
        tw.TweenProperty(spr, "position:y", targetPos.Y - 15f, 0.3f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
        tw.Parallel().TweenProperty(spr, "scale", new Vector2(2.6f, 2.6f), 0.3f);
        tw.TweenCallback(Callable.From(() =>
        {
            foreach (var t in targets)
            {
                SpawnImpact(parent, t + new Vector2(0, -25), true);
            }
        }));
        tw.TweenProperty(spr, "modulate:a", 0.0f, 0.35f);
        tw.TweenCallback(Callable.From(spr.QueueFree));
    }

    private static void SpawnDemonicMeteorStrike(Node parent, Vector2 targetPos, IReadOnlyList<Vector2> targets)
    {
        var spr = CreateVfxSprite("vfx_demonic_meteor");
        spr.Position = targetPos + new Vector2(0, -120);
        spr.Scale = new Vector2(1.0f, 1.0f);
        parent.AddChild(spr);

        var tw = spr.CreateTween();
        tw.TweenProperty(spr, "position", targetPos + new Vector2(0, -20), 0.35f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        tw.TweenCallback(Callable.From(() =>
        {
            foreach (var t in targets)
            {
                SpawnFire(parent, t + new Vector2(0, -20));
                SpawnImpact(parent, t + new Vector2(0, -20), true);
            }
        }));
        tw.TweenProperty(spr, "scale", new Vector2(3.0f, 3.0f), 0.4f).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
        tw.Parallel().TweenProperty(spr, "modulate:a", 0.0f, 0.4f);
        tw.TweenCallback(Callable.From(spr.QueueFree));
    }

    private static void SpawnRedCliffInferno(Node parent, IReadOnlyList<Vector2> targets)
    {
        foreach (var t in targets)
        {
            var spr = CreateVfxSprite("vfx_inferno_storm");
            spr.Position = t + new Vector2(0, -25);
            spr.Scale = new Vector2(0.8f, 0.8f);
            parent.AddChild(spr);

            var tw = spr.CreateTween();
            tw.TweenProperty(spr, "scale", new Vector2(2.2f, 2.2f), 0.4f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            tw.Parallel().TweenProperty(spr, "position:y", t.Y - 45f, 0.5f);
            tw.TweenProperty(spr, "modulate:a", 0.0f, 0.3f);
            tw.TweenCallback(Callable.From(spr.QueueFree));
        }
    }

    private static void SpawnSniperHyperbeam(Node parent, Vector2 fromPos, Vector2 toPos, bool isEnemy)
    {
        var spr = CreateVfxSprite("vfx_sniper_beam");
        spr.Position = (fromPos + toPos) / 2.0f;
        spr.Scale = new Vector2(2.4f, 2.0f);
        spr.FlipH = isEnemy;
        parent.AddChild(spr);

        var tw = spr.CreateTween();
        tw.TweenProperty(spr, "scale:y", 3.0f, 0.25f).SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
        tw.TweenProperty(spr, "modulate:a", 0.0f, 0.3f);
        tw.TweenCallback(Callable.From(spr.QueueFree));
    }

    private static void SpawnPhantomCavalry(Node parent, Vector2 fromPos, Vector2 toPos, bool isEnemy)
    {
        var spr = CreateVfxSprite("vfx_cavalry_charge");
        spr.Position = fromPos;
        spr.Scale = new Vector2(1.8f, 1.8f);
        spr.FlipH = isEnemy;
        parent.AddChild(spr);

        var tw = spr.CreateTween();
        tw.TweenProperty(spr, "position", toPos, 0.35f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        tw.Parallel().TweenProperty(spr, "scale", new Vector2(2.4f, 2.4f), 0.35f);
        tw.TweenProperty(spr, "modulate:a", 0.0f, 0.25f);
        tw.TweenCallback(Callable.From(spr.QueueFree));
    }

    private static void SpawnMoonlightDescent(Node parent, Vector2 targetPos)
    {
        var spr = CreateVfxSprite("vfx_crescent_moon");
        spr.Position = targetPos + new Vector2(0, -60);
        spr.Scale = new Vector2(0.8f, 0.8f);
        parent.AddChild(spr);

        var tw = spr.CreateTween();
        tw.TweenProperty(spr, "position:y", targetPos.Y - 20f, 0.35f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        tw.Parallel().TweenProperty(spr, "scale", new Vector2(2.0f, 2.0f), 0.35f);
        tw.TweenInterval(0.2f);
        tw.TweenProperty(spr, "modulate:a", 0.0f, 0.35f);
        tw.TweenCallback(Callable.From(spr.QueueFree));
    }

    private static void SpawnDivineStormFormation(Node parent, IReadOnlyList<Vector2> targets)
    {
        foreach (var t in targets)
        {
            var spr = CreateVfxSprite("vfx_divine_storm");
            spr.Position = t + new Vector2(0, -30);
            spr.Scale = new Vector2(0.6f, 0.6f);
            parent.AddChild(spr);

            var tw = spr.CreateTween();
            tw.TweenProperty(spr, "scale", new Vector2(2.2f, 2.2f), 0.45f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            tw.Parallel().TweenProperty(spr, "rotation", Mathf.DegToRad(90f), 0.5f);
            tw.TweenProperty(spr, "modulate:a", 0.0f, 0.35f);
            tw.TweenCallback(Callable.From(spr.QueueFree));
        }
    }

    private static void SpawnSacredLotusHeal(Node parent, IReadOnlyList<Vector2> allies)
    {
        foreach (var a in allies)
        {
            var spr = CreateVfxSprite("vfx_lotus_healing");
            spr.Position = a + new Vector2(0, -25);
            spr.Scale = new Vector2(0.5f, 0.5f);
            parent.AddChild(spr);

            var tw = spr.CreateTween();
            tw.TweenProperty(spr, "scale", new Vector2(1.8f, 1.8f), 0.4f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            tw.Parallel().TweenProperty(spr, "position:y", a.Y - 45f, 0.5f);
            tw.TweenProperty(spr, "modulate:a", 0.0f, 0.3f);
            tw.TweenCallback(Callable.From(spr.QueueFree));
        }
    }

    private static void SpawnPhoenixChainExplosion(Node parent, IReadOnlyList<Vector2> targets)
    {
        foreach (var t in targets)
        {
            var spr = CreateVfxSprite("vfx_phoenix_chains");
            spr.Position = t + new Vector2(0, -25);
            spr.Scale = new Vector2(1.0f, 1.0f);
            parent.AddChild(spr);

            var tw = spr.CreateTween();
            tw.TweenProperty(spr, "scale", new Vector2(2.0f, 2.0f), 0.3f).SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
            tw.TweenCallback(Callable.From(() =>
            {
                SpawnFire(parent, t + new Vector2(0, -20));
            }));
            tw.TweenProperty(spr, "modulate:a", 0.0f, 0.3f);
            tw.TweenCallback(Callable.From(spr.QueueFree));
        }
    }

    private static void SpawnCelestialZitherRings(Node parent, IReadOnlyList<Vector2> allies)
    {
        foreach (var a in allies)
        {
            var spr = CreateVfxSprite("vfx_celestial_zither");
            spr.Position = a + new Vector2(0, -25);
            spr.Scale = new Vector2(0.8f, 0.8f);
            parent.AddChild(spr);

            var tw = spr.CreateTween();
            tw.TweenProperty(spr, "scale", new Vector2(2.2f, 2.2f), 0.45f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
            tw.TweenProperty(spr, "modulate:a", 0.0f, 0.35f);
            tw.TweenCallback(Callable.From(spr.QueueFree));
        }
    }

    public static void SpawnSlash(Node parent, Vector2 pos, bool flipH = false)
    {
        var spr = new Sprite2D();
        var tex = GD.Load<Texture2D>("res://assets/vfx/physical/slash.png");
        if (tex != null)
        {
            spr.Texture = tex;
        }
        spr.Position = pos;
        spr.Scale = new Vector2(1.8f, 1.8f);
        spr.FlipH = flipH;
        spr.ZIndex = LayerConstants.SlashVfxZ;
        parent.AddChild(spr);

        var tw = spr.CreateTween();
        tw.TweenProperty(spr, "scale", new Vector2(2.4f, 2.4f), 0.22f);
        tw.Parallel().TweenProperty(spr, "modulate:a", 0.0f, 0.25f);
        tw.TweenCallback(Callable.From(spr.QueueFree));
    }

    public static void SpawnImpact(Node parent, Vector2 pos, bool isCrit = false)
    {
        var spr = new Sprite2D();
        var tex = GD.Load<Texture2D>("res://assets/vfx/hits/impact.png");
        if (tex != null)
        {
            spr.Texture = tex;
        }
        spr.Position = pos;
        spr.Scale = isCrit ? new Vector2(2.6f, 2.6f) : new Vector2(1.6f, 1.6f);
        spr.ZIndex = LayerConstants.ImpactVfxZ;
        parent.AddChild(spr);

        var tw = spr.CreateTween();
        tw.TweenProperty(spr, "rotation", Mathf.DegToRad(isCrit ? 180.0f : 90.0f), 0.3f);
        tw.Parallel().TweenProperty(spr, "scale", spr.Scale * 1.3f, 0.25f);
        tw.Parallel().TweenProperty(spr, "modulate:a", 0.0f, 0.3f);
        tw.TweenCallback(Callable.From(spr.QueueFree));
    }

    public static void SpawnFire(Node parent, Vector2 pos)
    {
        var spr = new Sprite2D();
        var tex = GD.Load<Texture2D>("res://assets/vfx/fire/fire.png");
        if (tex != null)
        {
            spr.Texture = tex;
        }
        spr.Position = pos;
        spr.Scale = new Vector2(2.2f, 2.2f);
        spr.ZIndex = LayerConstants.MagicVfxZ;
        parent.AddChild(spr);

        var tw = spr.CreateTween();
        tw.TweenProperty(spr, "scale", new Vector2(3.0f, 3.0f), 0.35f);
        tw.Parallel().TweenProperty(spr, "modulate:a", 0.0f, 0.4f);
        tw.TweenCallback(Callable.From(spr.QueueFree));
    }

    public static void SpawnWind(Node parent, Vector2 pos)
    {
        var spr = new Sprite2D();
        var tex = GD.Load<Texture2D>("res://assets/vfx/lightning/wind.png");
        if (tex != null)
        {
            spr.Texture = tex;
        }
        spr.Position = pos;
        spr.Scale = new Vector2(2.4f, 2.4f);
        spr.ZIndex = LayerConstants.MagicVfxZ;
        parent.AddChild(spr);

        var tw = spr.CreateTween();
        tw.TweenProperty(spr, "position:y", pos.Y - 30f, 0.35f);
        tw.Parallel().TweenProperty(spr, "rotation", Mathf.DegToRad(120f), 0.35f);
        tw.Parallel().TweenProperty(spr, "modulate:a", 0.0f, 0.4f);
        tw.TweenCallback(Callable.From(spr.QueueFree));
    }

    public static void SpawnWater(Node parent, Vector2 pos)
    {
        var spr = new Sprite2D();
        var tex = GD.Load<Texture2D>("res://assets/vfx/water/water.png");
        if (tex != null)
        {
            spr.Texture = tex;
        }
        spr.Position = pos;
        spr.Scale = new Vector2(2.0f, 2.0f);
        spr.ZIndex = LayerConstants.MagicVfxZ;
        parent.AddChild(spr);

        var tw = spr.CreateTween();
        tw.TweenProperty(spr, "position:y", pos.Y - 35f, 0.45f);
        tw.Parallel().TweenProperty(spr, "scale", new Vector2(2.6f, 2.6f), 0.45f);
        tw.Parallel().TweenProperty(spr, "modulate:a", 0.0f, 0.45f);
        tw.TweenCallback(Callable.From(spr.QueueFree));
    }

    public static Node SpawnRageAura(Node parent, Vector2 pos)
    {
        var spr = new Sprite2D();
        var tex = GD.Load<Texture2D>("res://assets/vfx/fire/rage_aura.png");
        if (tex != null)
        {
            spr.Texture = tex;
        }
        spr.Position = pos + new Vector2(0, 10);
        spr.Scale = new Vector2(1.3f, 0.8f);
        spr.ZIndex = LayerConstants.MagicVfxZ;
        parent.AddChild(spr);

        var tw = spr.CreateTween().SetLoops(3);
        tw.TweenProperty(spr, "scale", new Vector2(1.6f, 1.0f), 0.3f);
        tw.TweenProperty(spr, "scale", new Vector2(1.2f, 0.7f), 0.3f);
        return spr;
    }
}
