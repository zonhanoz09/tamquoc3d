using System;
using System.Collections.Generic;
using Godot;
using TamQuoc3D.Core.Constants;
using TamQuoc3D.Core.Audio;
using TamQuoc3D.Core.Autoload;
using TamQuoc3D.Core.Navigation;
using TamQuoc3D.Core.Network;
using TamQuoc3D.Data.Database;
using TamQuoc3D.Data.Resources;
using TamQuoc3D.Features.Battle.Entities;
using TamQuoc3D.Features.Battle.UI;
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
    private CanvasLayer _uiLayer;
    private Label _roundLabel;
    private RichTextLabel _logRichText;
    private Button _toggleLogBtn;
    private bool _isLogMinimized = false;
    private Button _speedBtn;
    private Button _pauseBtn;
    private Button _restartBtn;
    private PanelContainer _logPanel;
    private TurnTimeline _timelineContainer;
    private Control _resultBanner;
    private Label _resultDetailLabel;
    private Button _campaignBtn;
    private bool _campaignResultRecorded = false;

    // Manual mode (Phase 2): panel chon chieu + trang thai cho click muc tieu
    private Button _modeBtn;
    private PanelContainer _manualPanel;
    private Label _manualPrompt;
    private Button _skillBtn;
    private Button _rageBtn;
    private Button _autoTurnBtn;
    private System.Threading.Tasks.TaskCompletionSource<SkillData> _skillTcs;
    private System.Threading.Tasks.TaskCompletionSource<BattleHero> _targetTcs;
    private bool _targetingAllies = false;
    private SkillData _manualHeroSkillNormal;
    private SkillData _manualHeroSkillRage;
    private readonly System.Collections.Generic.List<(HeroBattleNode Node, Color Modulate)> _highlighted = [];

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
    /// Camera Zoom c\u1eadn c\u1ea3nh & Slow-motion nh\u1eb9 0.1s khi B\u1ea1o K\u00edch ho\u1eb7c K\u1ebft Li\u1ec5u (Arcade Impact Feel).
    /// </summary>
    public void TriggerCritSlowMoAndZoom(Vector2 targetPos)
    {
        if (_camera == null) return;

        // 1. Zoom in c\u1eadn c\u1ea3nh h\u01b0\u1edbng v\u00e0o v\u1ecb tr\u00ed va ch\u1ea1m
        if (_zoomTween != null && _zoomTween.IsValid()) _zoomTween.Kill();
        _zoomTween = CreateTween();

        Vector2 baseCamPos = new Vector2(GameConstants.ViewportWidth / 2.0f, GameConstants.ViewportHeight / 2.0f);
        Vector2 focusCamPos = baseCamPos.Lerp(targetPos, 0.15f);

        _zoomTween.TweenProperty(_camera, "zoom", new Vector2(1.08f, 1.08f), 0.08f)
                  .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        _zoomTween.Parallel().TweenProperty(_camera, "position", focusCamPos, 0.08f);

        _zoomTween.TweenInterval(0.10f); // Gi\u1eef c\u1eadn c\u1ea3nh 0.1s

        _zoomTween.TweenProperty(_camera, "zoom", Vector2.One, 0.20f)
                  .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.InOut);
        _zoomTween.Parallel().TweenProperty(_camera, "position", baseCamPos, 0.20f);

        // 2. Slow-motion nh\u1eb9 0.1s th\u1eddi gian th\u1ef1c
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
        _uiLayer = new CanvasLayer { Layer = LayerConstants.HudCanvasLayer };
        AddChild(_uiLayer);

        // 1. Top HUD Header
        var topBar = new PanelContainer
        {
            Position = new Vector2(180, 10),
            Size = new Vector2(920, 50)
        };
        var sbTop = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.08f, 0.15f, 0.85f),
            BorderColor = new Color(0.85f, 0.7f, 0.25f)
        };
        sbTop.SetBorderWidthAll(2);
        sbTop.SetCornerRadiusAll(6);
        topBar.AddThemeStyleboxOverride("panel", sbTop);
        _uiLayer.AddChild(topBar);

        var topHbox = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        topHbox.AddThemeConstantOverride("separation", 16);
        topBar.AddChild(topHbox);

        var backToFormationBtn = new Button
        {
            Text = "⬅ Bố Trận",
            CustomMinimumSize = new Vector2(85, 32)
        };
        backToFormationBtn.Pressed += () => SceneNavigator.GoToFormationSelector();
        topHbox.AddChild(backToFormationBtn);

        _roundLabel = new Label
        {
            Text = "⚔️ HIỆP 1",
            VerticalAlignment = VerticalAlignment.Center
        };
        _roundLabel.AddThemeFontSizeOverride("font_size", 16);
        _roundLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.85f, 0.3f));
        topHbox.AddChild(_roundLabel);

        // Timeline Action Order (component TurnTimeline - Phase 4 HUD)
        _timelineContainer = new TurnTimeline();
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

        _modeBtn = new Button
        {
            Text = "Chế độ: Auto",
            CustomMinimumSize = new Vector2(105, 32)
        };
        _modeBtn.Pressed += OnModeToggle;
        topHbox.AddChild(_modeBtn);

        // 1b. Manual Mode Panel (hien khi toi luot nguoi choi o che do Thu cong)
        _manualPanel = new PanelContainer
        {
            Position = new Vector2(340, 596),
            Size = new Vector2(600, 104),
            Visible = false
        };
        var sbManual = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.1f, 0.16f, 0.95f),
            BorderColor = new Color(0.85f, 0.7f, 0.25f)
        };
        sbManual.SetBorderWidthAll(2);
        sbManual.SetCornerRadiusAll(8);
        sbManual.ContentMarginLeft = 12;
        sbManual.ContentMarginRight = 12;
        sbManual.ContentMarginTop = 8;
        sbManual.ContentMarginBottom = 8;
        _manualPanel.AddThemeStyleboxOverride("panel", sbManual);
        _uiLayer.AddChild(_manualPanel);

        var manualVBox = new VBoxContainer();
        manualVBox.AddThemeConstantOverride("separation", 8);
        _manualPanel.AddChild(manualVBox);

        _manualPrompt = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Text = ""
        };
        _manualPrompt.AddThemeFontSizeOverride("font_size", 15);
        _manualPrompt.AddThemeColorOverride("font_color", new Color(1.0f, 0.9f, 0.6f));
        manualVBox.AddChild(_manualPrompt);

        var manualHBox = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        manualHBox.AddThemeConstantOverride("separation", 10);
        manualVBox.AddChild(manualHBox);

        _skillBtn = new Button { CustomMinimumSize = new Vector2(180, 40) };
        _skillBtn.Pressed += () => _skillTcs?.TrySetResult(_manualHeroSkillNormal);
        manualHBox.AddChild(_skillBtn);

        _rageBtn = new Button { CustomMinimumSize = new Vector2(180, 40) };
        _rageBtn.Pressed += () => _skillTcs?.TrySetResult(_manualHeroSkillRage);
        manualHBox.AddChild(_rageBtn);

        _autoTurnBtn = new Button
        {
            Text = "🤖 Tự đánh lượt này",
            CustomMinimumSize = new Vector2(160, 40)
        };
        _autoTurnBtn.Pressed += () => _skillTcs?.TrySetResult(null);
        manualHBox.AddChild(_autoTurnBtn);

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
        _uiLayer.AddChild(_logPanel);

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
        _uiLayer.AddChild(_resultBanner);

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

        _resultDetailLabel = new Label
        {
            Name = "ResultDetail",
            Position = new Vector2(0, 338),
            Size = new Vector2(GameConstants.ViewportWidth, 36),
            HorizontalAlignment = HorizontalAlignment.Center,
            Text = ""
        };
        _resultDetailLabel.AddThemeFontSizeOverride("font_size", 20);
        _resultDetailLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.9f, 0.7f));
        _resultBanner.AddChild(_resultDetailLabel);

        _campaignBtn = new Button
        {
            Text = "🏰 CHIẾN DỊCH",
            Position = new Vector2(260, 380),
            Size = new Vector2(160, 48),
            Visible = false
        };
        _campaignBtn.Pressed += () => SceneNavigator.GoToCampaign();
        _resultBanner.AddChild(_campaignBtn);

        _restartBtn = new Button
        {
            Text = "🔄 ĐẤU LẠI",
            Position = new Vector2(460, 380),
            Size = new Vector2(160, 48)
        };
        _restartBtn.Pressed += OnRestartPressed;
        _resultBanner.AddChild(_restartBtn);

        var formationBtn = new Button
        {
            Text = "📋 BỐ TRẬN",
            Position = new Vector2(660, 380),
            Size = new Vector2(160, 48)
        };
        formationBtn.Pressed += () => SceneNavigator.GoToFormationSelector();
        _resultBanner.AddChild(formationBtn);
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
        _battleManager.PlayerActionProvider = ProvidePlayerActionAsync;
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

        var gm = GameManager.Instance;
        var stage = gm?.CurrentStage;
        bool isCampaignStage = stage != null && gm.BattleContext == "campaign" && stage.EnemyFormation.Count > 0;

        if (isCampaignStage)
        {
            // Tran chien dich: dich lay dung EnemyFormation cua ai dang thach dau,
            // khong con dung doi dich hardcode cho moi ai.
            var formation = gm.CurrentFormation ?? new Godot.Collections.Array<FormationEntry>();
            _battleManager.SetupBattleWithFormations(formation, stage.EnemyFormation);
        }
        else if (gm != null && gm.CurrentFormation.Count > 0)
        {
            _battleManager.SetupBattleWithFormation(gm.CurrentFormation, enemyParty);
        }
        else
        {
            _battleManager.SetupBattle(playerParty, enemyParty);
        }

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
        if (_timelineContainer == null || _battleManager?.TurnManager == null) return;
        _timelineContainer.Refresh(
            _battleManager.TurnManager.TurnQueue,
            _battleManager.TurnManager.CurrentTurnIndex);
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

        // 1. Super Move Cut-In & Skill Banner on rage
        if (isRage)
        {
            _soundManager.PlayRage();
            TriggerScreenShake(6.0f);

            var bannerTcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
            void OnBannerFinished() => bannerTcs.TrySetResult(true);
            attackerNode.SkillBannerFinished += OnBannerFinished;

            attackerNode.ShowSkillBanner(skill.SkillName, _battleManager.BattleSpeed);
            var cutInTask = HeroCutInCinematic.PlayAsync(_uiLayer ?? (Node)this, hero.Data, skill, hero.Team == 1, _battleManager.BattleSpeed);

            await System.Threading.Tasks.Task.WhenAll(bannerTcs.Task, cutInTask);
            attackerNode.SkillBannerFinished -= OnBannerFinished;
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

        // 1. Skill VFX & Elemental Audio on rage
        if (isRage)
        {
            var priPos = allTargetPositions.Count > 0 ? allTargetPositions[0] : attackerPos;
            CombatVFX.SpawnHeroSkillVfx(this, attacker.Data.HeroId, priPos, attackerPos, allTargetPositions, attacker.Team == 1);
            _soundManager.PlayElementSfx(attacker.Data.Element);
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

        // Dong vong lap Campaign: thang tran phai ghi sao + thuong + mo ai tiep,
        // khong chi dung lai o banner (truoc day khong noi nao emit StageCompleted).
        var gm = GameManager.Instance;
        bool isCampaignStage = gm?.CurrentStage != null && gm.BattleContext == "campaign";
        if (isCampaignStage)
        {
            _campaignBtn.Visible = true;
            if (winner == 0)
            {
                _ = RecordCampaignResultAsync();
            }
            else
            {
                _resultDetailLabel.Text = "Thất bại — không tốn Thể Lực. Bố trí lại đội hình rồi thử lại!";
            }
        }
    }

    /// <summary>
    /// Tinh sao bang StarRatingSystem, trao thuong va dong bo tien trinh vuot ai.
    /// Online: server tu tinh thuong theo stage_rewards va la nguon chuan cho vi tien.
    /// Offline: ap dung dung gia tri trong StageData (.tres) cho phien choi local.
    /// </summary>
    private async System.Threading.Tasks.Task RecordCampaignResultAsync()
    {
        if (_campaignResultRecorded) return;
        _campaignResultRecorded = true;

        try
        {
            var gm = GameManager.Instance;
            var stage = gm?.CurrentStage;
            if (gm == null || stage == null) return;

            int surviving = 0;
            foreach (var h in _battleManager.Team0)
            {
                if (h.IsAlive) surviving++;
            }

            var result = new BattleResult
            {
                Won = true,
                SurvivingHeroes = surviving,
                TurnCount = _battleManager.TurnManager.CurrentRound
            };
            int stars = StarRatingSystem.CalculateStars(result, stage);
            bool firstClear = gm.RecordStageResult(stage.StageId, stars);

            int silver;
            int gold;
            int exp;
            NetworkManager.CampaignClearResultDto dto = null;
            var network = NetworkManager.Instance;
            if (network != null && network.IsServerOnline && network.IsAuthenticated)
            {
                dto = await network.ClearStageAsync(stage.StageKey, stars, result.TurnCount);
            }

            if (dto != null)
            {
                silver = dto.RewardSilver;
                gold = dto.RewardGold;
                exp = dto.FirstClear ? stage.FirstClearExp : stage.NormalExp;
                gm.ConsumeStamina(dto.StaminaCost);
            }
            else
            {
                silver = firstClear ? stage.FirstClearGold : stage.NormalGold;
                gold = firstClear ? stage.FirstClearDiamond : 0;
                exp = firstClear ? stage.FirstClearExp : stage.NormalExp;
                gm.ConsumeStamina(stage.StaminaCost);
                if (silver > 0) gm.AddGold(silver);
                if (gold > 0) gm.AddGems(gold);
            }

            EventBus.Instance?.EmitSignal(EventBus.SignalName.StageCompleted, stage.ChapterId, stage.StageId, stars);
            if (firstClear)
            {
                EventBus.Instance?.EmitSignal(EventBus.SignalName.FirstClearReward, stage.StageId, silver, gold);
            }

            string starText = new string('★', stars) + new string('☆', 3 - stars);
            _resultDetailLabel.Text = $"{starText}   •   +{silver} Bạc   •   +{gold} Kim Cương   •   +{exp} EXP";
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[BattleScene] RecordCampaignResult failed: {ex.Message}");
        }
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
        else if (Mathf.IsEqualApprox(_battleManager.BattleSpeed, 2.0f))
        {
            _battleManager.BattleSpeed = 3.0f;
            _speedBtn.Text = "Tốc độ: x3";
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
        _campaignResultRecorded = false;
        if (_resultDetailLabel != null) _resultDetailLabel.Text = "";
        if (_campaignBtn != null) _campaignBtn.Visible = false;
        _battleManager.IsBattleActive = false;
        StartBattleWithRoster();
    }

    // ===================== Manual Mode (Phase 2) =====================

    private void OnModeToggle()
    {
        _battleManager.ManualMode = !_battleManager.ManualMode;
        _modeBtn.Text = _battleManager.ManualMode ? "Chế độ: Thủ công" : "Chế độ: Auto";
        OnLog(_battleManager.ManualMode
            ? "[color=#fde68a]🎯 Chế độ THỦ CÔNG: bạn sẽ chọn chiêu + mục tiêu cho từng lượt tướng phe mình.[/color]"
            : "[color=#94a3b8]🤖 Chế độ AUTO: AI tự đánh toàn bộ.[/color]");
    }

    /// <summary>
    /// BattleManager goi ham nay khi toi luot tuong phe nguoi choi o che do Manual:
    /// hien panel chon chieu, sau do cho click chon muc tieu tren san dau.
    /// Tra ve null neu nguoi choi bam "Tu danh luot nay".
    /// </summary>
    private async System.Threading.Tasks.Task<BattleManager.PlayerAction> ProvidePlayerActionAsync(BattleHero hero)
    {
        _manualHeroSkillNormal = hero.Data.GetNormalAttack();
        _manualHeroSkillRage = hero.Data.GetRageSkill();

        _manualPrompt.Text = $"🎯 Lượt của {hero.Data.HeroName} (Nộ: {hero.Stats.MoraleCurrent}/100) — chọn chiêu:";
        _skillBtn.Text = $"⚔️ {_manualHeroSkillNormal?.SkillName ?? "Đánh thường"}";
        _skillBtn.Disabled = false;
        _rageBtn.Text = $"🔥 {_manualHeroSkillRage?.SkillName ?? "Nộ chiêu"}";
        _rageBtn.Disabled = _manualHeroSkillRage == null || !hero.Stats.CanCastRage();
        _autoTurnBtn.Disabled = false;
        _manualPanel.Visible = true;

        _skillTcs = new System.Threading.Tasks.TaskCompletionSource<SkillData>();
        var skill = await _skillTcs.Task;
        if (skill == null)
        {
            _manualPanel.Visible = false;
            return null; // AI tu danh luot nay
        }

        if (!NeedsManualTarget(skill))
        {
            _manualPanel.Visible = false;
            return new BattleManager.PlayerAction { Skill = skill, PrimaryTarget = null };
        }

        _targetingAllies = skill.Type is SkillData.SkillType.Heal or SkillData.SkillType.Buff;
        _manualPrompt.Text = _targetingAllies
            ? $"💚 Chọn đồng minh để dùng «{skill.SkillName}»:"
            : $"🎯 Chọn mục tiêu cho «{skill.SkillName}»:";
        _skillBtn.Disabled = true;
        _rageBtn.Disabled = true;
        _autoTurnBtn.Disabled = true;
        HighlightValidTargets(_targetingAllies);

        _targetTcs = new System.Threading.Tasks.TaskCompletionSource<BattleHero>();
        var target = await _targetTcs.Task;

        ClearTargetHighlights();
        _manualPanel.Visible = false;
        return new BattleManager.PlayerAction { Skill = skill, PrimaryTarget = target };
    }

    private static bool NeedsManualTarget(SkillData skill)
    {
        return skill.Type is SkillData.SkillType.Melee
            or SkillData.SkillType.Ranged
            or SkillData.SkillType.Flank
            or SkillData.SkillType.Heal
            or SkillData.SkillType.Buff;
    }

    private void HighlightValidTargets(bool allies)
    {
        ClearTargetHighlights();
        var visuals = allies ? _playerVisuals : _enemyVisuals;
        foreach (var node in visuals)
        {
            if (node?.BattleHero == null || !node.BattleHero.IsAlive) continue;
            _highlighted.Add((node, node.Modulate));
            node.Modulate = new Color(1.35f, 1.25f, 0.55f);
        }
    }

    private void ClearTargetHighlights()
    {
        foreach (var (node, modulate) in _highlighted)
        {
            if (GodotObject.IsInstanceValid(node)) node.Modulate = modulate;
        }
        _highlighted.Clear();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_targetTcs == null || _targetTcs.Task.IsCompleted) return;
        if (@event is not InputEventMouseButton mb || !mb.Pressed || mb.ButtonIndex != MouseButton.Left) return;

        var clickPos = GetGlobalMousePosition();
        var visuals = _targetingAllies ? _playerVisuals : _enemyVisuals;
        HeroBattleNode best = null;
        float bestDist = 64.0f;
        foreach (var node in visuals)
        {
            if (node?.BattleHero == null || !node.BattleHero.IsAlive) continue;
            float d = clickPos.DistanceTo(node.Position + new Vector2(0, -32));
            if (d < bestDist)
            {
                bestDist = d;
                best = node;
            }
        }

        if (best != null)
        {
            _targetTcs.TrySetResult(best.BattleHero);
            GetViewport().SetInputAsHandled();
        }
    }
}
