using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TamQuoc3D.Core.Autoload;
using TamQuoc3D.Core.Navigation;
using TamQuoc3D.Core.Network;
using TamQuoc3D.Data.Resources;

namespace TamQuoc3D.Features.Campaign;

/// <summary>
/// CampaignController - Bo dieu khien trung tam cua Ban Do Chien Dich PvE (Phase 3).
/// Dieu phoi ChapterMap, StageInfoPopup, ChapterSelectPanel va dong bo tien trinh vuot ai voi PostgreSQL.
/// </summary>
[GlobalClass]
public partial class CampaignController : Control
{
    [Signal] public delegate void StageSelectedEventHandler(int chapterId, int stageId);
    [Signal] public delegate void ChapterLoadedEventHandler(int chapterId);

    public int CurrentChapterId { get; private set; } = 1;
    public int HighestUnlockedStageId { get; private set; } = 1;

    private readonly List<ChapterData> _chapters = [];
    private readonly Dictionary<int, int> _stageStars = new();
    private readonly Dictionary<int, int> _chapterStars = new();

    private ChapterMap _chapterMap;
    private ChapterSelectPanel _chapterPanel;
    private StageInfoPopup _stagePopup;
    private Label _titleLabel;
    private Label _subtitleLabel;
    private Label _totalStarsLabel;
    private Label _staminaLabel;
    private Label _goldLabel;
    private Label _gemsLabel;
    private Button _chapterDrawerBtn;
    private Button _backBtn;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(1280, 720);
        LoadAllChaptersFromDatabase();
        BuildUI();
        ConnectSignals();
        _ = SyncProgressAndRefreshAsync();
    }

    private void LoadAllChaptersFromDatabase()
    {
        _chapters.Clear();
        string[] paths =
        [
            "res://data/database/chapters/chapter_01.tres",
            "res://data/database/chapters/chapter_02.tres"
        ];

        foreach (var p in paths)
        {
            if (ResourceLoader.Exists(p))
            {
                var ch = GD.Load<ChapterData>(p);
                if (ch != null)
                {
                    _chapters.Add(ch);
                }
            }
        }

        // Initialize default local progress if empty (1-1 to 1-4 cleared by default in starter seed)
        _stageStars[1] = 3;
        _stageStars[2] = 3;
        _stageStars[3] = 3;
        _stageStars[4] = 3;
        HighestUnlockedStageId = 5; // Stage 1-5 unlocked
        RecalculateChapterStars();
    }

    private void BuildUI()
    {
        // 1. Chapter Map (Background + Paths + Stage Nodes)
        _chapterMap = new ChapterMap
        {
            Name = "ChapterMap",
            CustomMinimumSize = new Vector2(1280, 720)
        };
        _chapterMap.StageSelected += OnStageSelected;
        AddChild(_chapterMap);

        // 2. Top Header Bar
        var topBar = new PanelContainer
        {
            Name = "CampaignTopBar",
            Position = new Vector2(0, 0),
            CustomMinimumSize = new Vector2(1280, 64)
        };
        var topStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.1f, 0.14f, 0.9f),
            BorderColor = new Color(0.25f, 0.3f, 0.4f),
            BorderWidthBottom = 2
        };
        topBar.AddThemeStyleboxOverride("panel", topStyle);
        AddChild(topBar);

        var topMargin = new MarginContainer();
        topMargin.AddThemeConstantOverride("margin_left", 20);
        topMargin.AddThemeConstantOverride("margin_right", 20);
        topBar.AddChild(topMargin);

        var topHBox = new HBoxContainer();
        topHBox.AddThemeConstantOverride("separation", 18);
        topMargin.AddChild(topHBox);

        // Back button
        _backBtn = new Button
        {
            Text = " ⬅ Hub ",
            CustomMinimumSize = new Vector2(90, 40)
        };
        _backBtn.Pressed += OnBackPressed;
        topHBox.AddChild(_backBtn);

        // Chapter Drawer toggle
        _chapterDrawerBtn = new Button
        {
            Text = " 📜 Chương ▾ ",
            CustomMinimumSize = new Vector2(110, 40)
        };
        _chapterDrawerBtn.Pressed += () => _chapterPanel?.TogglePanel();
        topHBox.AddChild(_chapterDrawerBtn);

        // Title & Subtitle VBox
        var titleBox = new VBoxContainer();
        titleBox.AddThemeConstantOverride("separation", 2);
        titleBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        topHBox.AddChild(titleBox);

        _titleLabel = new Label
        {
            Text = "CHƯƠNG 1: KHỞI BINH ĐẠI HÁN",
            Modulate = new Color(1f, 0.85f, 0.3f)
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 16);
        titleBox.AddChild(_titleLabel);

        _subtitleLabel = new Label
        {
            Text = "Dẹp loạn Hoàng Cân, khởi binh định thiên hạ",
            Modulate = new Color(0.8f, 0.85f, 0.9f)
        };
        _subtitleLabel.AddThemeFontSizeOverride("font_size", 11);
        titleBox.AddChild(_subtitleLabel);

        // Currencies on right
        var rightHBox = new HBoxContainer();
        rightHBox.AddThemeConstantOverride("separation", 16);
        topHBox.AddChild(rightHBox);

        _totalStarsLabel = new Label
        {
            Text = "⭐ 12/18",
            Modulate = new Color(1f, 0.85f, 0.2f)
        };
        _totalStarsLabel.AddThemeFontSizeOverride("font_size", 14);
        rightHBox.AddChild(_totalStarsLabel);

        _staminaLabel = new Label
        {
            Text = $"🍗 {GameManager.Instance?.Stamina ?? 100}/{GameManager.Instance?.MaxStamina ?? 145}",
            Modulate = new Color(0.4f, 1f, 0.5f)
        };
        _staminaLabel.AddThemeFontSizeOverride("font_size", 13);
        rightHBox.AddChild(_staminaLabel);

        _goldLabel = new Label
        {
            Text = $"🪙 {GameManager.Instance?.Gold:N0}",
            Modulate = new Color(1f, 0.9f, 0.5f)
        };
        _goldLabel.AddThemeFontSizeOverride("font_size", 13);
        rightHBox.AddChild(_goldLabel);

        _gemsLabel = new Label
        {
            Text = $"💎 {GameManager.Instance?.Gems:N0}",
            Modulate = new Color(0.4f, 0.8f, 1f)
        };
        _gemsLabel.AddThemeFontSizeOverride("font_size", 13);
        rightHBox.AddChild(_gemsLabel);

        // 3. Chapter Select Panel (Drawer on left)
        _chapterPanel = new ChapterSelectPanel
        {
            Name = "ChapterSelectPanel",
            Position = new Vector2(-260, 64)
        };
        _chapterPanel.ChapterSelected += LoadChapter;
        AddChild(_chapterPanel);

        // 4. Stage Info Modal Popup
        _stagePopup = new StageInfoPopup
        {
            Name = "StageInfoPopup"
        };
        _stagePopup.ChallengeRequested += OnChallengeStage;
        _stagePopup.SweepRequested += OnSweepStage;
        AddChild(_stagePopup);
    }

    private void ConnectSignals()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.GoldChanged += OnGoldChanged;
            EventBus.Instance.DiamondChanged += OnDiamondChanged;
            EventBus.Instance.StaminaChanged += OnStaminaChanged;
            EventBus.Instance.StageCompleted += OnStageCompleted;
        }
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.GoldChanged -= OnGoldChanged;
            EventBus.Instance.DiamondChanged -= OnDiamondChanged;
            EventBus.Instance.StaminaChanged -= OnStaminaChanged;
            EventBus.Instance.StageCompleted -= OnStageCompleted;
        }
    }

    public async System.Threading.Tasks.Task SyncProgressAndRefreshAsync()
    {
        if (NetworkManager.Instance != null && NetworkManager.Instance.IsServerOnline)
        {
            var serverProgress = await NetworkManager.Instance.FetchCampaignProgressAsync();
            if (serverProgress != null && serverProgress.Count > 0)
            {
                int maxCleared = 0;
                foreach (var p in serverProgress)
                {
                    // Parse "stage_1_1" or "1-1"
                    int sId = ParseStageId(p.StageId);
                    if (sId > 0)
                    {
                        _stageStars[sId] = p.Stars;
                        maxCleared = Mathf.Max(maxCleared, sId);
                    }
                }
                HighestUnlockedStageId = Mathf.Max(1, maxCleared + 1);
                RecalculateChapterStars();
            }
        }

        LoadChapter(CurrentChapterId);
    }

    public void LoadChapter(int chapterId)
    {
        CurrentChapterId = chapterId;
        var ch = _chapters.Find(c => c.ChapterId == chapterId) ?? (_chapters.Count > 0 ? _chapters[0] : null);
        if (ch == null) return;

        _titleLabel.Text = ch.ChapterName.ToUpper();
        _subtitleLabel.Text = ch.Subtitle;

        int earned = _chapterStars.TryGetValue(chapterId, out int s) ? s : 0;
        _totalStarsLabel.Text = $"⭐ {earned}/{ch.TotalStars}";

        _chapterMap.LoadChapter(ch, _stageStars, HighestUnlockedStageId);
        _chapterPanel.PopulateChapters(_chapters, CurrentChapterId, _chapterStars);

        EmitSignal(SignalName.ChapterLoaded, chapterId);
    }

    private void OnStageSelected(StageData stageData)
    {
        if (stageData == null) return;
        int stars = _stageStars.TryGetValue(stageData.StageId, out int s) ? s : 0;
        _stagePopup.ShowStage(stageData, stars);
        EmitSignal(SignalName.StageSelected, stageData.ChapterId, stageData.StageId);
    }

    private void OnChallengeStage(StageData stage)
    {
        if (stage == null) return;

        if (GameManager.Instance != null && GameManager.Instance.Stamina < stage.StaminaCost)
        {
            EventBus.Instance?.EmitSignal(EventBus.SignalName.SystemNotification, "Thể Lực", "Không đủ Thể Lực để xuất trận!", "warning");
            return;
        }

        _stagePopup.HidePopup();

        // Configure battle context and navigate to Formation Selector
        if (GameManager.Instance != null)
        {
            GameManager.Instance.BattleContext = "campaign";
        }
        SceneNavigator.GoToFormationSelector("campaign");
    }

    private async void OnSweepStage(StageData stage)
    {
        if (stage == null) return;

        if (GameManager.Instance != null && !GameManager.Instance.ConsumeStamina(stage.StaminaCost))
        {
            EventBus.Instance?.EmitSignal(EventBus.SignalName.SystemNotification, "Thể Lực", "Không đủ Thể Lực để quét ải!", "warning");
            return;
        }

        // Grant rewards
        GameManager.Instance?.AddGold(stage.NormalGold);
        EventBus.Instance?.EmitSignal(EventBus.SignalName.SystemNotification, "Quét Nhanh", $"Vượt nhanh thành công! Nhận {stage.NormalGold} Bạc.", "reward");

        // Sync to server
        if (NetworkManager.Instance != null && NetworkManager.Instance.IsServerOnline)
        {
            await NetworkManager.Instance.ClearStageAsync(
                stage.StageKey,
                3,
                1,
                stage.NormalGold,
                0,
                stage.StaminaCost
            );
        }

        _stagePopup.HidePopup();
        RefreshWalletDisplay();
    }

    private void OnStageCompleted(int chapterId, int stageId, int stars)
    {
        int prevStars = _stageStars.TryGetValue(stageId, out int s) ? s : 0;
        _stageStars[stageId] = Mathf.Max(prevStars, stars);
        HighestUnlockedStageId = Mathf.Max(HighestUnlockedStageId, stageId + 1);

        RecalculateChapterStars();
        LoadChapter(CurrentChapterId);
    }

    private void RecalculateChapterStars()
    {
        int ch1Total = 0;
        for (int i = 1; i <= 6; i++)
        {
            if (_stageStars.TryGetValue(i, out int st))
            {
                ch1Total += st;
            }
        }
        _chapterStars[1] = ch1Total;
        _chapterStars[2] = 0;
    }

    private void RefreshWalletDisplay()
    {
        if (!IsInstanceValid(this)) return;
        if (_staminaLabel != null && GameManager.Instance != null)
        {
            _staminaLabel.Text = $"🍗 {GameManager.Instance.Stamina}/{GameManager.Instance.MaxStamina}";
        }
        if (_goldLabel != null && GameManager.Instance != null)
        {
            _goldLabel.Text = $"🪙 {GameManager.Instance.Gold:N0}";
        }
        if (_gemsLabel != null && GameManager.Instance != null)
        {
            _gemsLabel.Text = $"💎 {GameManager.Instance.Gems:N0}";
        }
    }

    private void OnGoldChanged(int delta, int total) => RefreshWalletDisplay();
    private void OnDiamondChanged(int delta, int total) => RefreshWalletDisplay();
    private void OnStaminaChanged(int current, int max) => RefreshWalletDisplay();

    private void OnBackPressed()
    {
        SceneNavigator.GoToHub();
    }

    private static int ParseStageId(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return 0;
        string[] parts = raw.Split(new[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 0 && int.TryParse(parts[^1], out int id))
        {
            return id;
        }
        return 0;
    }
}
