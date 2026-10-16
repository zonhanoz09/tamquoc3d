using System;
using System.Collections.Generic;
using Godot;
using TamQuoc3D.Core.Constants;
using TamQuoc3D.Core.Audio;
using TamQuoc3D.Data.Database;
using TamQuoc3D.Data.Resources;
using TamQuoc3D.Features.Battle.Entities;
using TamQuoc3D.Features.Battle.Grid;

namespace TamQuoc3D.Features.Battle.Controllers;

/// <summary>
/// BattleScene - Controller man hinh chien tran (.NET 8).
/// Ho tro AnimatedSprite2D HD-2D, Camera Zoom can canh, va Slow-motion 0.1s khi bao kich.
/// </summary>
[GlobalClass]
public partial class BattleScene : Node2D
{
    private BattleManager _battleManager;
    private SoundManager _soundManager;

    // Camera & Shake & Dynamic Zoom
    private Camera2D _camera;
    private float _shakeAmount = 0.0f;
    private Tween _zoomTween;

    // Hero Battle Nodes Storage
    private readonly List<HeroBattleNode> _playerVisuals = [];
    private readonly List<HeroBattleNode> _enemyVisuals = [];

    // UI Nodes
    private Label _roundLabel;
    private RichTextLabel _logRichText;
    private Button _toggleLogBtn;
    private bool _isLogMinimized = false;
    private Button _speedBtn;
    private Button _pauseBtn;
    private Button _restartBtn;
    private PanelContainer _logPanel;
    private HBoxContainer _timelineContainer;
    private Control _resultBanner;

    private readonly List<string> _logLines = [];
    private const int MaxLogLines = 100;

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

        var fallbackDb = new HeroDatabase();
        fallbackDb.GenerateDefaultHeroDatabase();
        return fallbackDb.GetHero(id);
    }

    public override void _Ready()
    {
        Engine.TimeScale = 1.0f;
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

    /// <summary>
    /// Camera Zoom cận cảnh & Slow-motion nhẹ 0.1s khi Bạo Kích hoặc Kết Liễu (Arcade Impact Feel).
    /// </summary>
    public void TriggerCritSlowMoAndZoom(Vector2 targetPos)
    {
        if (_camera == null) return;

        // 1. Zoom in cận cảnh hướng vào vị trí va chạm
        if (_zoomTween != null && _zoomTween.IsValid()) _zoomTween.Kill();
        _zoomTween = CreateTween();

        Vector2 baseCamPos = new Vector2(GameConstants.ViewportWidth / 2.0f, GameConstants.ViewportHeight / 2.0f);
        Vector2 focusCamPos = baseCamPos.Lerp(targetPos, 0.15f);

        _zoomTween.TweenProperty(_camera, "zoom", new Vector2(1.08f, 1.08f), 0.08f)
                  .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        _zoomTween.Parallel().TweenProperty(_camera, "position", focusCamPos, 0.08f);

        _zoomTween.TweenInterval(0.10f); // Giữ cận cảnh 0.1s

        _zoomTween.TweenProperty(_camera, "zoom", Vector2.One, 0.20f)
                  .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.InOut);
        _zoomTween.Parallel().TweenProperty(_camera, "position", baseCamPos, 0.20f);

        // 2. Slow-motion nhẹ 0.1s thời gian thực
        Engine.TimeScale = 0.35f;
        GetTree().CreateTimer(0.12, false, false, true).Timeout += () =>
        {
            Engine.TimeScale = 1.0f;
        };
    }

    private void SetupCamera()
    {
        _camera = new Camera2D
        {
            Position = new Vector2(GameConstants.ViewportWidth / 2.0f, GameConstants.ViewportHeight / 2.0f)
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
        var bgTex = GD.Load<Texture2D>("res://assets/environments/battlefields/dau_truong/battlefield.png")
            ?? GD.Load<Texture2D>("res://assets/environments/battlefields/xich_bich/battlefield.png");
        if (bgTex != null)
        {
            bgSprite.Texture = bgTex;
        }
        bgSprite.Position = new Vector2(640, 360);
        bgSprite.ZIndex = LayerConstants.BackgroundZ;
        AddChild(bgSprite);
    }

    private void SetupUi()
    {
        var uiLayer = new CanvasLayer { Layer = LayerConstants.HudCanvasLayer };
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
            Alignment = BoxContainer.AlignmentMode.Center
        };
        topHbox.AddThemeConstantOverride("separation", 20);
        topBar.AddChild(topHbox);

        _roundLabel = new Label
        {
            Text = "⚔️ HIỆP 1",
            VerticalAlignment = VerticalAlignment.Center
        };
        _roundLabel.AddThemeFontSizeOverride("font_size", 16);
        _roundLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.85f, 0.3f));
        topHbox.AddChild(_roundLabel);

        // Timeline Action Order
        _timelineContainer = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        _timelineContainer.AddThemeConstantOverride("separation", 8);
        topHbox.AddChild(_timelineContainer);

        // Control Buttons
        _speedBtn = new Button
        {
            Text = "Tốc độ: x1",
            CustomMinimumSize = new Vector2(90, 32)
        };
        _speedBtn.Pressed += OnSpeedToggle;
        topHbox.AddChild(_speedBtn);

        _pauseBtn = new Button
        {
            Text = "Tạm dừng",
            CustomMinimumSize = new Vector2(85, 32)
        };
        _pauseBtn.Pressed += OnPauseToggle;
        topHbox.AddChild(_pauseBtn);

        // 2. Retro Combat Battle Log Box (Góc trên bên trái, xếp gọn gàng không che khuất tướng)
        _logPanel = new PanelContainer
        {
            Position = new Vector2(16, 70),
            Size = new Vector2(250, 150)
        };
        var sbLog = new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.05f, 0.09f, 0.90f),
            BorderColor = new Color(0.85f, 0.72f, 0.32f, 1.0f)
        };
        sbLog.SetBorderWidthAll(2);
        sbLog.SetCornerRadiusAll(4);
        sbLog.ShadowColor = new Color(0f, 0f, 0f, 0.70f);
        sbLog.ShadowSize = 6;
        sbLog.ShadowOffset = new Vector2(2, 2);
        sbLog.ContentMarginLeft = 8;
        sbLog.ContentMarginRight = 8;
        sbLog.ContentMarginTop = 6;
        sbLog.ContentMarginBottom = 6;
        _logPanel.AddThemeStyleboxOverride("panel", sbLog);
        uiLayer.AddChild(_logPanel);

        var logVbox = new VBoxContainer();
        logVbox.AddThemeConstantOverride("separation", 4);
        _logPanel.AddChild(logVbox);

        // Header
        var headerBox = new HBoxContainer();
        headerBox.AddThemeConstantOverride("separation", 6);
        logVbox.AddChild(headerBox);

        var logTitle = new Label
        {
            Text = "📜 CHIẾN TRẬN KÝ"
        };
        logTitle.AddThemeFontSizeOverride("font_size", 11);
        logTitle.AddThemeColorOverride("font_color", new Color(1.0f, 0.85f, 0.3f));
        headerBox.AddChild(logTitle);

        var liveLbl = new Label
        {
            Text = "● LIVE"
        };
        liveLbl.AddThemeFontSizeOverride("font_size", 9);
        liveLbl.AddThemeColorOverride("font_color", new Color(0.35f, 0.9f, 0.45f));
        headerBox.AddChild(liveLbl);

        var spacer = new Control
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        headerBox.AddChild(spacer);

        var clearBtn = new Button
        {
            Text = "⟲",
            CustomMinimumSize = new Vector2(22, 20),
            TooltipText = "Xóa nhật ký"
        };
        clearBtn.AddThemeFontSizeOverride("font_size", 10);
        clearBtn.Pressed += OnClearLog;
        headerBox.AddChild(clearBtn);

        _toggleLogBtn = new Button
        {
            Text = "▾",
            CustomMinimumSize = new Vector2(22, 20),
            TooltipText = "Thu nhỏ nhật ký"
        };
        _toggleLogBtn.AddThemeFontSizeOverride("font_size", 10);
        _toggleLogBtn.Pressed += OnToggleLog;
        headerBox.AddChild(_toggleLogBtn);

        // RichTextLabel for colored BBCode battle logs
        _logRichText = new RichTextLabel
        {
            BbcodeEnabled = true,
            ScrollFollowing = true,
            CustomMinimumSize = new Vector2(230, 110),
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _logRichText.AddThemeFontSizeOverride("normal_font_size", 10);
        logVbox.AddChild(_logRichText);

        // 3. Result Banner
        _resultBanner = new Control
        {
            Visible = false
        };
        uiLayer.AddChild(_resultBanner);

        var resBg = new ColorRect
        {
            Size = new Vector2(GameConstants.ViewportWidth, GameConstants.ViewportHeight),
            Color = new Color(0, 0, 0, 0.65f)
        };
        _resultBanner.AddChild(resBg);

        var resLbl = new Label
        {
            Name = "ResultText",
            Position = new Vector2(0, 260),
            Size = new Vector2(GameConstants.ViewportWidth, 80),
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

    private void SetupBattle()
    {
        _battleManager = new BattleManager();
        AddChild(_battleManager);

        _battleManager.LogMessage += OnLog;
        _battleManager.BattleEnded += OnBattleEnded;
        _battleManager.RoundStarted += OnRoundStarted;
        _battleManager.HeroTurnStarted += OnHeroTurnStarted;
        _battleManager.HeroTurnEnded += OnHeroTurnEnded;
        _battleManager.ActionAwaiter = OnHeroActedAsync;
        _battleManager.HeroDied += OnHeroDied;

        StartBattleWithRoster();
    }

    private void StartBattleWithRoster()
    {
        Engine.TimeScale = 1.0f;
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
            GetHero("trieu_van"),     // 1 Tien quan chinh giua
            GetHero("quan_vu"),       // Trung quan Top
            GetHero("ma_sieu"),        // Trung quan Bottom
            GetHero("gia_cat_luong"), // Hau quan Top
            GetHero("tieu_kieu")      // Hau quan Bottom
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
            var node = new HeroBattleNode
            {
                Position = Grid3x3.CalculateSlotPosition(0, bh.SlotRow, bh.SlotCol)
            };
            node.HomePosition = node.Position;
            node.ZIndex = (int)(node.Position.Y / 10.0f);
            AddChild(node);
            node.Setup(bh, false);
            _playerVisuals.Add(node);
        }

        // Spawn Enemy Visuals
        foreach (var bh in _battleManager.Team1)
        {
            var node = new HeroBattleNode
            {
                Position = Grid3x3.CalculateSlotPosition(1, bh.SlotRow, bh.SlotCol)
            };
            node.HomePosition = node.Position;
            node.ZIndex = (int)(node.Position.Y / 10.0f);
            AddChild(node);
            node.Setup(bh, true);
            _enemyVisuals.Add(node);
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

        if (_battleManager?.TurnManager == null) return;

        var queue = _battleManager.TurnManager.TurnQueue;
        int curIdx = Mathf.Clamp(_battleManager.TurnManager.CurrentTurnIndex, 0, queue.Count);
        for (int i = curIdx; i < queue.Count; i++)
        {
            var bh = queue[i];
            if (!bh.IsAlive || !bh.Stats.IsAlive()) continue;

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
        if (hero.Node is HeroBattleNode node)
        {
            node.SetActiveTurn(true);
        }
        UpdateTimeline();
    }

    private void OnHeroTurnEnded(BattleHero hero)
    {
        if (hero.Node is HeroBattleNode node)
        {
            node.SetActiveTurn(false);
        }
    }

    private async System.Threading.Tasks.Task OnHeroActedAsync(BattleHero hero, Godot.Collections.Dictionary actionData)
    {
        bool isRage = actionData.TryGetValue("is_rage", out var rVal) && (bool)rVal;
        var skill = (SkillData)actionData["skill"];
        var targets = (Godot.Collections.Array)actionData["targets"];
        var results = (Godot.Collections.Array)actionData["results"];

        if (hero.Node is not HeroBattleNode attackerNode) return;

        // 1. Skill Banner on rage
        if (isRage)
        {
            _soundManager.PlayRage();
            TriggerScreenShake(6.0f);
            attackerNode.ShowSkillBanner(skill.SkillName, _battleManager.BattleSpeed);
            await ToSignal(attackerNode, HeroBattleNode.SignalName.SkillBannerFinished);
        }

        if (targets.Count == 0) return;

        var primaryTarget = (BattleHero)targets[0];
        var primaryNode = primaryTarget.Node as HeroBattleNode;
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
                if (tHero.Node is HeroBattleNode tNode)
                {
                    tNode.UpdateBars(true);
                    tNode.UpdateStatusIcons();
                }
            }
            attackerNode.UpdateBars(true);
            UpdateTimeline();
        }

        var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();

        void OnCompleteWrapped()
        {
            OnComplete();
            tcs.TrySetResult(true);
        }

        if (skill.Type is SkillData.SkillType.Melee or SkillData.SkillType.Flank)
        {
            attackerNode.AnimateAttack(targetPos, OnImpact, OnCompleteWrapped, _battleManager.BattleSpeed);
        }
        else
        {
            attackerNode.AnimateRangedAttack(targetPos, OnImpact, OnCompleteWrapped, _battleManager.BattleSpeed);
        }

        await tcs.Task;
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
            if (tHero.Node is not HeroBattleNode tNode) continue;

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
                tNode.UpdateBars(true);
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
                int dmg = (int)res["damage"];
                tNode.ShowFloatingText($"ĐỠ ĐÒN! -{dmg}", new Color(0.4f, 0.8f, 1.0f));
                CombatVFX.SpawnImpact(this, pos + new Vector2(0, -30), false);
                tNode.UpdateBars(true);
                if (!tHero.IsAlive || tHero.Stats.HpCurrent <= 0)
                {
                    TriggerCritSlowMoAndZoom(pos);
                    tNode.AnimateDeath(_battleManager.BattleSpeed);
                }
                else
                {
                    tNode.AnimateHurt(false, true, _battleManager.BattleSpeed);
                }
                continue;
            }

            // Hit / Crit
            bool isCrit = res.TryGetValue("is_crit", out var critVal) && (bool)critVal;
            int damage = (int)res["damage"];

            if (isCrit)
            {
                _soundManager.PlayCrit();
                TriggerScreenShake(8.0f);
                TriggerCritSlowMoAndZoom(pos);
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

            tNode.UpdateBars(true);
            if (!tHero.IsAlive || tHero.Stats.HpCurrent <= 0)
            {
                TriggerCritSlowMoAndZoom(pos);
                tNode.AnimateDeath(_battleManager.BattleSpeed);
            }
            else
            {
                tNode.AnimateHurt(false, false, _battleManager.BattleSpeed);
            }
        }
    }

    private void OnHeroDied(BattleHero hero)
    {
        if (hero.Node is HeroBattleNode node && !node.IsDead)
        {
            node.AnimateDeath(_battleManager.BattleSpeed);
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
        if (_logRichText != null)
        {
            _logRichText.Text = string.Join("\n", _logLines);
        }
    }

    private void OnClearLog()
    {
        _logLines.Clear();
        if (_logRichText != null)
        {
            _logRichText.Text = "[color=#94a3b8]-- Nhật ký đã được xóa --[/color]";
        }
    }

    private void OnToggleLog()
    {
        _isLogMinimized = !_isLogMinimized;
        if (_isLogMinimized)
        {
            _logPanel.Position = new Vector2(16, 70);
            _logPanel.Size = new Vector2(250, 32);
            _logRichText.Visible = false;
            _toggleLogBtn.Text = "▾";
            _toggleLogBtn.TooltipText = "Mở rộng nhật ký";
        }
        else
        {
            _logPanel.Position = new Vector2(16, 70);
            _logPanel.Size = new Vector2(250, 150);
            _logRichText.Visible = true;
            _toggleLogBtn.Text = "▴";
            _toggleLogBtn.TooltipText = "Thu nhỏ nhật ký";
        }
    }

    private void OnBattleEnded(int winner)
    {
        Engine.TimeScale = 1.0f;
        _resultBanner.Visible = true;
        string title = winner == 0 ? "🏆 CHIẾN THẮNG!" : "💀 THẤT BẠI!";
        Color col = winner == 0 ? new Color(1.0f, 0.85f, 0.2f) : new Color(0.9f, 0.2f, 0.2f);
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
        Engine.TimeScale = 1.0f;
        _resultBanner.Visible = false;
        _battleManager.IsBattleActive = false;
        StartBattleWithRoster();
    }
}
