using System;
using System.Collections.Generic;
using Godot;
using TamQuoc3D.Audio;
using TamQuoc3D.Battle;
using TamQuoc3D.Data;
using TamQuoc3D.Hero;
using TamQuoc3D.Visual;

namespace TamQuoc3D.Scenes;

/// <summary>
/// BattleScene - Man hinh chien dau Tam Quoc chuan Formation HD-2D (.NET 8).
/// </summary>
[GlobalClass]
public partial class BattleScene : Node2D
{
    private BattleManager _battleManager;
    private SoundManager _soundManager;

    // Camera & Shake
    private Camera2D _camera;
    private float _shakeAmount = 0.0f;

    // Hero Visuals Storage
    private readonly List<HeroVisual> _playerVisuals = new();
    private readonly List<HeroVisual> _enemyVisuals = new();

    // UI Nodes
    private Label _roundLabel;
    private Label _logLabel;
    private Button _speedBtn;
    private Button _pauseBtn;
    private Button _restartBtn;
    private PanelContainer _logPanel;
    private HBoxContainer _timelineContainer;
    private Control _resultBanner;

    private readonly List<string> _logLines = new();
    private const int MaxLogLines = 12;

    private HeroData GetHero(string id)
    {
        if (HeroDatabase.Instance != null)
        {
            var h = HeroDatabase.Instance.GetHero(id);
            if (h != null) return h;
        }

        var dbNode = GetNodeOrNull<HeroDatabase>("/root/HeroDatabase");
        if (dbNode != null)
        {
            var h = dbNode.GetHero(id);
            if (h != null) return h;
        }

        // Fallback: create database instance if running standalone scene
        var fallbackDb = new HeroDatabase();
        fallbackDb.RegisterStarterHeroes();
        return fallbackDb.GetHero(id);
    }

    public override void _Ready()
    {
        SetupCamera();
        SetupSound();
        SetupBackground();
        SetupUi();
        SetupBattle();
    }

    public override void _Process(double delta)
    {
        if (_shakeAmount > 0)
        {
            _shakeAmount = Mathf.Max(0.0f, _shakeAmount - (float)delta * 25.0f);
            _camera.Offset = new Vector2(
                (float)GD.RandRange(-_shakeAmount, _shakeAmount),
                (float)GD.RandRange(-_shakeAmount, _shakeAmount)
            );
        }
    }

    public void TriggerScreenShake(float amount = 8.0f)
    {
        _shakeAmount = amount;
    }

    private void SetupCamera()
    {
        _camera = new Camera2D
        {
            Position = new Vector2(640, 360)
        };
        AddChild(_camera);
    }

    private void SetupSound()
    {
        _soundManager = new SoundManager();
        AddChild(_soundManager);
    }

    private void SetupBackground()
    {
        var bgSprite = new Sprite2D();
        var bgTex = GD.Load<Texture2D>("res://assets/environments/battlefields/xich_bich/battlefield.png");
        if (bgTex != null)
        {
            bgSprite.Texture = bgTex;
        }
        bgSprite.Position = new Vector2(640, 360);
        bgSprite.ZIndex = 0;
        AddChild(bgSprite);
    }

    private void SetupUi()
    {
        var uiLayer = new CanvasLayer { Layer = 10 };
        AddChild(uiLayer);

        // 1. Top HUD Header
        var topBar = new PanelContainer
        {
            Position = new Vector2(240, 10),
            Size = new Vector2(800, 50)
        };
        var sbTop = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.08f, 0.15f, 0.85f),
            BorderColor = new Color(0.85f, 0.7f, 0.25f)
        };
        sbTop.SetBorderWidthAll(2);
        sbTop.SetCornerRadiusAll(6);
        topBar.AddThemeStyleboxOverride("panel", sbTop);
        uiLayer.AddChild(topBar);

        var topHbox = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(780, 44),
            Alignment = BoxContainer.AlignmentMode.Center
        };
        topBar.AddChild(topHbox);

        _roundLabel = new Label
        {
            Text = "⚔️ HIỆP 1"
        };
        _roundLabel.AddThemeFontSizeOverride("font_size", 18);
        _roundLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.85f, 0.3f));
        topHbox.AddChild(_roundLabel);

        var sep = new VSeparator
        {
            CustomMinimumSize = new Vector2(20, 30)
        };
        topHbox.AddChild(sep);

        var tlLbl = new Label
        {
            Text = "THỨ TỰ HÀNH ĐỘNG:"
        };
        tlLbl.AddThemeFontSizeOverride("font_size", 13);
        tlLbl.AddThemeColorOverride("font_color", new Color(0.7f, 0.8f, 0.9f));
        topHbox.AddChild(tlLbl);

        _timelineContainer = new HBoxContainer();
        _timelineContainer.AddThemeConstantOverride("separation", 6);
        topHbox.AddChild(_timelineContainer);

        // 2. Control Buttons
        var ctrlBox = new HBoxContainer
        {
            Position = new Vector2(1060, 16),
            Size = new Vector2(200, 36)
        };
        ctrlBox.AddThemeConstantOverride("separation", 8);
        uiLayer.AddChild(ctrlBox);

        _speedBtn = new Button
        {
            Text = "Tốc độ: x1",
            CustomMinimumSize = new Vector2(90, 32)
        };
        _speedBtn.Pressed += OnSpeedToggle;
        ctrlBox.AddChild(_speedBtn);

        _pauseBtn = new Button
        {
            Text = "Tạm dừng",
            CustomMinimumSize = new Vector2(85, 32)
        };
        _pauseBtn.Pressed += OnPauseToggle;
        ctrlBox.AddChild(_pauseBtn);

        // 3. Combat Log
        _logPanel = new PanelContainer
        {
            Position = new Vector2(16, 580),
            Size = new Vector2(240, 125)
        };
        var sbLog = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.05f, 0.08f, 0.65f),
            BorderColor = new Color(0.4f, 0.4f, 0.5f, 0.5f)
        };
        sbLog.SetBorderWidthAll(1);
        sbLog.SetCornerRadiusAll(4);
        _logPanel.AddThemeStyleboxOverride("panel", sbLog);
        uiLayer.AddChild(_logPanel);

        var logVbox = new VBoxContainer();
        _logPanel.AddChild(logVbox);

        var logTitle = new Label
        {
            Text = "📜 NHẬT KÝ CHIẾN TRẬN"
        };
        logTitle.AddThemeFontSizeOverride("font_size", 11);
        logTitle.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.8f));
        logVbox.AddChild(logTitle);

        _logLabel = new Label
        {
            Text = "Khởi tạo trận đánh...",
            AutowrapMode = TextServer.AutowrapMode.Word
        };
        _logLabel.AddThemeFontSizeOverride("font_size", 10);
        _logLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.95f));
        logVbox.AddChild(_logLabel);

        // 4. Result Banner
        _resultBanner = new Control
        {
            Position = Vector2.Zero,
            Size = new Vector2(1280, 720),
            Visible = false
        };
        uiLayer.AddChild(_resultBanner);

        var resBg = new ColorRect
        {
            Size = new Vector2(1280, 720),
            Color = new Color(0, 0, 0, 0.65f)
        };
        _resultBanner.AddChild(resBg);

        var resLbl = new Label
        {
            Name = "ResultText",
            Position = new Vector2(0, 260),
            Size = new Vector2(1280, 80),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        resLbl.AddThemeFontSizeOverride("font_size", 48);
        _resultBanner.AddChild(resLbl);

        _restartBtn = new Button
        {
            Text = "🔄 ĐẤU LẠI",
            Position = new Vector2(560, 380),
            Size = new Vector2(160, 48)
        };
        _restartBtn.Pressed += OnRestartPressed;
        _resultBanner.AddChild(_restartBtn);
    }

    public static Vector2 GetSlotPosition(int team, int row, int col)
    {
        if (team == 0)
        {
            // Player
            return row switch
            {
                0 => new Vector2(520, 520),
                1 => col switch
                {
                    0 => new Vector2(350, 410),
                    1 => new Vector2(350, 520),
                    _ => new Vector2(350, 630)
                },
                _ => col switch
                {
                    0 => new Vector2(180, 410),
                    1 => new Vector2(180, 520),
                    _ => new Vector2(180, 630)
                }
            };
        }
        else
        {
            // Enemy
            return row switch
            {
                0 => new Vector2(760, 520),
                1 => col switch
                {
                    0 => new Vector2(930, 410),
                    1 => new Vector2(930, 520),
                    _ => new Vector2(930, 630)
                },
                _ => col switch
                {
                    0 => new Vector2(1100, 410),
                    1 => new Vector2(1100, 520),
                    _ => new Vector2(1100, 630)
                }
            };
        }
    }

    private void SetupBattle()
    {
        _battleManager = new BattleManager();
        AddChild(_battleManager);

        _battleManager.LogMessage += OnLog;
        _battleManager.BattleEnded += OnBattleEnded;
        _battleManager.RoundStarted += OnRoundStarted;
        _battleManager.HeroTurnStarted += OnHeroTurnStarted;
        _battleManager.HeroTurnEnded += OnHeroTurnEnded;
        _battleManager.HeroActed += (hero, data) => _ = OnHeroActedAsync(hero, data);
        _battleManager.HeroDied += OnHeroDied;

        StartBattleWithRoster();
    }

    private void StartBattleWithRoster()
    {
        foreach (var v in _playerVisuals)
        {
            if (GodotObject.IsInstanceValid(v)) v.QueueFree();
        }
        foreach (var v in _enemyVisuals)
        {
            if (GodotObject.IsInstanceValid(v)) v.QueueFree();
        }
        _playerVisuals.Clear();
        _enemyVisuals.Clear();

        var playerParty = new List<HeroData>
        {
            GetHero("tao_nhan"),
            GetHero("quan_vu"),
            GetHero("trieu_van"),
            GetHero("gia_cat_luong"),
            GetHero("tieu_kieu")
        };

        var enemyParty = new List<HeroData>
        {
            GetHero("dien_vi"),
            GetHero("lu_bo"),
            GetHero("hoang_trung"),
            GetHero("bang_thong"),
            GetHero("sai_van_co")
        };

        _battleManager.SetupBattle(playerParty, enemyParty);

        // Spawn Player Visuals
        foreach (var bh in _battleManager.Team0)
        {
            var vis = new HeroVisual
            {
                Position = GetSlotPosition(0, bh.SlotRow, bh.SlotCol)
            };
            vis.HomePosition = vis.Position;
            vis.ZIndex = (int)(vis.Position.Y / 10.0f);
            AddChild(vis);
            vis.Setup(bh, false);
            _playerVisuals.Add(vis);
        }

        // Spawn Enemy Visuals
        foreach (var bh in _battleManager.Team1)
        {
            var vis = new HeroVisual
            {
                Position = GetSlotPosition(1, bh.SlotRow, bh.SlotCol)
            };
            vis.HomePosition = vis.Position;
            vis.ZIndex = (int)(vis.Position.Y / 10.0f);
            AddChild(vis);
            vis.Setup(bh, true);
            _enemyVisuals.Add(vis);
        }

        UpdateTimeline();
        _battleManager.CallDeferred(BattleManager.MethodName.StartBattle);
    }

    private void UpdateTimeline()
    {
        foreach (var c in _timelineContainer.GetChildren())
        {
            c.QueueFree();
        }

        if (_battleManager.TurnManager == null) return;

        var queue = _battleManager.TurnManager.TurnQueue;
        foreach (var bh in queue)
        {
            if (!bh.Stats.IsAlive()) continue;

            var avatarRect = new TextureRect();
            var pTex = GD.Load<Texture2D>($"res://assets/heroes/{bh.Data.HeroId}/icon.png");
            if (pTex != null)
            {
                avatarRect.Texture = pTex;
            }
            avatarRect.CustomMinimumSize = new Vector2(32, 32);
            avatarRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            avatarRect.Modulate = bh.Team == 0 ? new Color(0.2f, 0.8f, 1.0f) : new Color(1.0f, 0.3f, 0.3f);
            _timelineContainer.AddChild(avatarRect);
        }
    }

    private void OnRoundStarted(int rNum)
    {
        _roundLabel.Text = $"⚔️ HIỆP {rNum}";
        UpdateTimeline();
    }

    private void OnHeroTurnStarted(BattleHero hero)
    {
        if (hero.Node is HeroVisual vis)
        {
            vis.SetActiveTurn(true);
        }
    }

    private void OnHeroTurnEnded(BattleHero hero)
    {
        if (hero.Node is HeroVisual vis)
        {
            vis.SetActiveTurn(false);
        }
    }

    private async System.Threading.Tasks.Task OnHeroActedAsync(BattleHero hero, Godot.Collections.Dictionary actionData)
    {
        bool isRage = actionData.TryGetValue("is_rage", out var rVal) && (bool)rVal;
        var skill = (SkillData)actionData["skill"];
        var targets = (Godot.Collections.Array)actionData["targets"];
        var results = (Godot.Collections.Array)actionData["results"];

        if (hero.Node is not HeroVisual attackerNode) return;

        // 1. Skill Banner on rage
        if (isRage)
        {
            _soundManager.PlayRage();
            TriggerScreenShake(6.0f);
            attackerNode.ShowSkillBanner(skill.SkillName, _battleManager.BattleSpeed);
            await ToSignal(attackerNode, HeroVisual.SignalName.SkillBannerFinished);
        }

        if (targets.Count == 0) return;

        var primaryTarget = (BattleHero)targets[0];
        var primaryNode = primaryTarget.Node as HeroVisual;
        var targetPos = primaryNode != null ? primaryNode.Position : attackerNode.Position;

        void OnImpact()
        {
            ApplyActionEffects(hero, skill, isRage, results);
        }

        void OnComplete()
        {
            foreach (Godot.Collections.Dictionary res in results)
            {
                var tHero = (BattleHero)res["target"];
                if (tHero.Node is HeroVisual tVis)
                {
                    tVis.UpdateBars(true);
                    tVis.UpdateStatusIcons();
                }
            }
            attackerNode.UpdateBars(true);
            UpdateTimeline();
        }

        if (skill.Type is SkillData.SkillType.Melee or SkillData.SkillType.Flank)
        {
            attackerNode.AnimateAttack(targetPos, OnImpact, OnComplete, _battleManager.BattleSpeed);
        }
        else
        {
            attackerNode.AnimateRangedAttack(targetPos, OnImpact, OnComplete, _battleManager.BattleSpeed);
        }
    }

    private void ApplyActionEffects(BattleHero attacker, SkillData skill, bool isRage, Godot.Collections.Array results)
    {
        var attackerPos = attacker.Node?.Position ?? Vector2.Zero;
        var allTargetPositions = new List<Vector2>();

        foreach (Godot.Collections.Dictionary res in results)
        {
            var tHero = (BattleHero)res["target"];
            if (tHero.Node != null)
            {
                allTargetPositions.Add(tHero.Node.Position);
            }
        }

        // 1. Skill VFX on rage
        if (isRage)
        {
            var priPos = allTargetPositions.Count > 0 ? allTargetPositions[0] : attackerPos;
            CombatVFX.SpawnHeroSkillVfx(this, attacker.Data.HeroId, priPos, attackerPos, allTargetPositions, attacker.Team == 1);
        }

        // 2. Apply effects to each target
        foreach (Godot.Collections.Dictionary res in results)
        {
            var tHero = (BattleHero)res["target"];
            if (tHero.Node is not HeroVisual tNode) continue;

            var pos = tNode.Position;

            // Support / Heal
            if (res.TryGetValue("is_support", out var supVal) && (bool)supVal)
            {
                if (!isRage)
                {
                    CombatVFX.SpawnWater(this, pos + new Vector2(0, -30));
                }
                _soundManager.PlayHeal();
                int healAmount = (int)res["heal_amount"];
                tNode.ShowFloatingText($"+{healAmount} HP", new Color(0.2f, 0.9f, 0.3f));
                continue;
            }

            // Dodge
            if (res.TryGetValue("is_dodge", out var dodgeVal) && (bool)dodgeVal)
            {
                _soundManager.PlayDodge();
                tNode.AnimateHurt(true, false, _battleManager.BattleSpeed);
                tNode.ShowFloatingText("NÉ TRÁNH!", new Color(0.8f, 0.8f, 0.85f));
                continue;
            }

            // Block
            if (res.TryGetValue("is_block", out var blockVal) && (bool)blockVal)
            {
                _soundManager.PlayBlock();
                tNode.AnimateHurt(false, true, _battleManager.BattleSpeed);
                int dmg = (int)res["damage"];
                tNode.ShowFloatingText($"ĐỠ ĐÒN! -{dmg}", new Color(0.4f, 0.8f, 1.0f));
                CombatVFX.SpawnImpact(this, pos + new Vector2(0, -30), false);
                continue;
            }

            // Hit / Crit
            bool isCrit = res.TryGetValue("is_crit", out var critVal) && (bool)critVal;
            int damage = (int)res["damage"];

            if (isCrit)
            {
                _soundManager.PlayCrit();
                TriggerScreenShake(8.0f);
                tNode.ShowFloatingText($"BẠO KÍCH! -{damage}", new Color(1.0f, 0.25f, 0.25f), true);
            }
            else
            {
                _soundManager.PlayHit();
                tNode.ShowFloatingText($"-{damage}", new Color(1.0f, 0.95f, 0.8f));
            }

            if (!isRage)
            {
                CombatVFX.SpawnHeroAttackVfx(this, attacker.Data.HeroId, pos, attackerPos, attacker.Team == 1, isCrit);
            }

            tNode.AnimateHurt(false, false, _battleManager.BattleSpeed);
        }
    }

    private void OnHeroDied(BattleHero hero)
    {
        if (hero.Node is HeroVisual vis)
        {
            vis.AnimateDeath(_battleManager.BattleSpeed);
        }
        UpdateTimeline();
    }

    private void OnLog(string text)
    {
        _logLines.Add(text);
        if (_logLines.Count > MaxLogLines)
        {
            _logLines.RemoveAt(0);
        }
        if (_logLabel != null)
        {
            _logLabel.Text = string.Join("\n", _logLines);
        }
    }

    private void OnBattleEnded(int winnerTeam)
    {
        bool isPlayerWin = winnerTeam == 0;
        string title = isPlayerWin ? "🏆 ĐẠI THẮNG QUANG VINH!" : "💀 QUÂN TA BẠI TRẬN!";
        var col = isPlayerWin ? new Color(1.0f, 0.85f, 0.2f) : new Color(0.9f, 0.3f, 0.3f);

        _soundManager?.PlayVictory();

        _resultBanner.Visible = true;
        var lbl = _resultBanner.GetNode<Label>("ResultText");
        lbl.Text = title;
        lbl.AddThemeColorOverride("font_color", col);
    }

    private void OnSpeedToggle()
    {
        if (Mathf.IsEqualApprox(_battleManager.BattleSpeed, 1.0f))
        {
            _battleManager.BattleSpeed = 1.5f;
            _speedBtn.Text = "Tốc độ: x1.5";
        }
        else if (Mathf.IsEqualApprox(_battleManager.BattleSpeed, 1.5f))
        {
            _battleManager.BattleSpeed = 2.0f;
            _speedBtn.Text = "Tốc độ: x2";
        }
        else
        {
            _battleManager.BattleSpeed = 1.0f;
            _speedBtn.Text = "Tốc độ: x1";
        }
    }

    private void OnPauseToggle()
    {
        _battleManager.IsPaused = !_battleManager.IsPaused;
        _pauseBtn.Text = _battleManager.IsPaused ? "Tiếp tục" : "Tạm dừng";
    }

    private void OnRestartPressed()
    {
        _resultBanner.Visible = false;
        _battleManager.IsBattleActive = false;
        StartBattleWithRoster();
    }
}
