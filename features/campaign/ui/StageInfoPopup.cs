using Godot;
using TamQuoc3D.Core.Autoload;
using TamQuoc3D.Data.Database;
using TamQuoc3D.Data.Resources;

namespace TamQuoc3D.Features.Campaign;

/// <summary>
/// StageInfoPopup - Modal popup hien thi thong tin chi tiet ai: ke dich, dieu kien sao, phan thuong, nut Xuat Tran va Quet nhanh.
/// </summary>
[GlobalClass]
public partial class StageInfoPopup : Control
{
    [Signal]
    public delegate void ChallengeRequestedEventHandler(StageData stageData);

    [Signal]
    public delegate void SweepRequestedEventHandler(StageData stageData);

    [Signal]
    public delegate void ClosedEventHandler();

    private StageData _currentStage;
    private int _currentStars = 0;

    private ColorRect _backdrop;
    private PanelContainer _modalPanel;
    private Label _titleLabel;
    private Label _typeBadge;
    private Label _descLabel;
    private Label _powerLabel;
    private Label _staminaLabel;
    private HBoxContainer _enemyList;
    private Label _star1Label;
    private Label _star2Label;
    private Label _star3Label;
    private Label _rewardLabel;
    private Button _challengeBtn;
    private Button _sweepBtn;
    private Button _closeBtn;

    private static readonly Texture2D FrameTex = GD.Load<Texture2D>("res://assets/ui/activities/campaign/stage_popup_frame.png");
    private static readonly Texture2D StarFilledTex = GD.Load<Texture2D>("res://assets/ui/activities/campaign/star_filled.png");

    public override void _Ready()
    {
        Visible = false;
        CustomMinimumSize = new Vector2(1280, 720);
        BuildUI();
    }

    private void BuildUI()
    {
        // Dark translucent backdrop
        _backdrop = new ColorRect
        {
            CustomMinimumSize = new Vector2(1280, 720),
            Color = new Color(0, 0, 0, 0.65f),
            MouseFilter = MouseFilterEnum.Stop
        };
        _backdrop.GuiInput += (ev) =>
        {
            if (ev is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
            {
                HidePopup();
            }
        };
        AddChild(_backdrop);

        // Center Modal Container
        var center = new CenterContainer
        {
            CustomMinimumSize = new Vector2(1280, 720),
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(center);

        // Modal Panel using NinePatch frame
        _modalPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(640, 520),
            MouseFilter = MouseFilterEnum.Stop
        };
        
        var styleBox = new StyleBoxTexture
        {
            Texture = FrameTex,
            TextureMarginLeft = 8,
            TextureMarginTop = 8,
            TextureMarginRight = 8,
            TextureMarginBottom = 8,
            AxisStretchHorizontal = StyleBoxTexture.AxisStretchMode.Stretch,
            AxisStretchVertical = StyleBoxTexture.AxisStretchMode.Stretch
        };
        _modalPanel.AddThemeStyleboxOverride("panel", styleBox);
        center.AddChild(_modalPanel);

        // Inner Margin
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 24);
        margin.AddThemeConstantOverride("margin_top", 18);
        margin.AddThemeConstantOverride("margin_right", 24);
        margin.AddThemeConstantOverride("margin_bottom", 20);
        _modalPanel.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 10);
        margin.AddChild(vbox);

        // --- 1. Header (Title + Badge + Close) ---
        var header = new HBoxContainer();
        vbox.AddChild(header);

        _titleLabel = new Label
        {
            Text = "Ải 1-1: Tuần Tra Biên Giới",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Modulate = new Color(1f, 0.88f, 0.35f)
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 20);
        header.AddChild(_titleLabel);

        _typeBadge = new Label
        {
            Text = "[ẢI THƯỜNG]",
            Modulate = new Color(0.4f, 0.85f, 1f)
        };
        _typeBadge.AddThemeFontSizeOverride("font_size", 13);
        header.AddChild(_typeBadge);

        _closeBtn = new Button
        {
            Text = " ✕ ",
            CustomMinimumSize = new Vector2(34, 34)
        };
        _closeBtn.Pressed += HidePopup;
        header.AddChild(_closeBtn);

        // --- 2. Description & Stats ---
        _descLabel = new Label
        {
            Text = "Mô tả chi tiết của ải ở đây...",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Modulate = new Color(0.85f, 0.85f, 0.85f)
        };
        _descLabel.AddThemeFontSizeOverride("font_size", 13);
        vbox.AddChild(_descLabel);

        var statsRow = new HBoxContainer();
        statsRow.AddThemeConstantOverride("separation", 30);
        vbox.AddChild(statsRow);

        _powerLabel = new Label
        {
            Text = "⚔️ Lực chiến gợi ý: 1,500",
            Modulate = new Color(1f, 0.6f, 0.3f)
        };
        _powerLabel.AddThemeFontSizeOverride("font_size", 14);
        statsRow.AddChild(_powerLabel);

        _staminaLabel = new Label
        {
            Text = "🍗 Thể lực tiêu hao: 3",
            Modulate = new Color(0.4f, 1f, 0.5f)
        };
        _staminaLabel.AddThemeFontSizeOverride("font_size", 14);
        statsRow.AddChild(_staminaLabel);

        // --- 3. Enemy lineup preview ---
        var enemyHeader = new Label
        {
            Text = "🛡️ ĐỘI HÌNH ĐỊCH QUÂN:",
            Modulate = new Color(0.95f, 0.8f, 0.4f)
        };
        enemyHeader.AddThemeFontSizeOverride("font_size", 14);
        vbox.AddChild(enemyHeader);

        _enemyList = new HBoxContainer();
        _enemyList.AddThemeConstantOverride("separation", 10);
        vbox.AddChild(_enemyList);

        // --- 4. 3-Star Objectives ---
        var starBox = new VBoxContainer();
        starBox.AddThemeConstantOverride("separation", 4);
        vbox.AddChild(starBox);

        var starTitle = new Label
        {
            Text = "⭐ ĐIỀU KIỆN ĐÁNH GIÁ SAO:",
            Modulate = new Color(1f, 0.85f, 0.2f)
        };
        starTitle.AddThemeFontSizeOverride("font_size", 13);
        starBox.AddChild(starTitle);

        _star1Label = new Label { Text = "  ⭐ Chiến thắng trận đấu" };
        _star1Label.AddThemeFontSizeOverride("font_size", 12);
        starBox.AddChild(_star1Label);

        _star2Label = new Label { Text = "  ⭐⭐ Số tướng sống sót ≥ 3" };
        _star2Label.AddThemeFontSizeOverride("font_size", 12);
        starBox.AddChild(_star2Label);

        _star3Label = new Label { Text = "  ⭐⭐⭐ Hoàn thành trong ≤ 5 lượt" };
        _star3Label.AddThemeFontSizeOverride("font_size", 12);
        starBox.AddChild(_star3Label);

        // --- 5. Rewards ---
        _rewardLabel = new Label
        {
            Text = "🎁 Phần thưởng: 500 Bạc | 50 KNB (Lần đầu)",
            Modulate = new Color(1f, 0.95f, 0.6f)
        };
        _rewardLabel.AddThemeFontSizeOverride("font_size", 13);
        vbox.AddChild(_rewardLabel);

        // Spacer
        var spacer = new Control { SizeFlagsVertical = SizeFlags.ExpandFill };
        vbox.AddChild(spacer);

        // --- 6. Action Buttons ---
        var btnRow = new HBoxContainer();
        btnRow.AddThemeConstantOverride("separation", 20);
        btnRow.Alignment = BoxContainer.AlignmentMode.Center;
        vbox.AddChild(btnRow);

        _sweepBtn = new Button
        {
            Text = "⚡ Quét Nhanh",
            CustomMinimumSize = new Vector2(150, 44)
        };
        _sweepBtn.Pressed += OnSweepPressed;
        btnRow.AddChild(_sweepBtn);

        _challengeBtn = new Button
        {
            Text = "⚔️ Xuất Trận",
            CustomMinimumSize = new Vector2(170, 44)
        };
        _challengeBtn.Pressed += OnChallengePressed;
        btnRow.AddChild(_challengeBtn);
    }

    public void ShowStage(StageData stage, int currentStars)
    {
        _currentStage = stage;
        _currentStars = currentStars;

        if (stage == null) return;

        _titleLabel.Text = $"Ải {stage.GetStageDisplayNumber()}: {stage.StageName}";
        _descLabel.Text = stage.Description;
        _powerLabel.Text = $"⚔️ Lực chiến gợi ý: {stage.RecommendedPower:N0}";
        _staminaLabel.Text = $"🍗 Tiêu hao: {stage.StaminaCost} Thể lực";

        switch (stage.Type)
        {
            case StageType.Boss:
                _typeBadge.Text = "[BOSS ĐẠI TƯỚNG]";
                _typeBadge.Modulate = new Color(1f, 0.3f, 0.3f);
                break;
            case StageType.Resource:
                _typeBadge.Text = "[PHÓ BẢN TÀI NGUYÊN]";
                _typeBadge.Modulate = new Color(1f, 0.85f, 0.3f);
                break;
            case StageType.Elite:
                _typeBadge.Text = "[TINH ANH TIỀN TIÊU]";
                _typeBadge.Modulate = new Color(0.9f, 0.5f, 1f);
                break;
            default:
                _typeBadge.Text = "[ẢI THƯỜNG]";
                _typeBadge.Modulate = new Color(0.4f, 0.85f, 1f);
                break;
        }

        // Enemy Preview List
        foreach (Node child in _enemyList.GetChildren())
        {
            child.QueueFree();
        }

        if (stage.EnemyFormation != null)
        {
            foreach (var enemy in stage.EnemyFormation)
            {
                var panel = new PanelContainer
                {
                    CustomMinimumSize = new Vector2(90, 36)
                };
                var lbl = new Label
                {
                    Text = FormatEnemyName(enemy.HeroId),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                lbl.AddThemeFontSizeOverride("font_size", 11);
                panel.AddChild(lbl);
                _enemyList.AddChild(panel);
            }
        }

        // 3-Star objectives text & colors
        _star1Label.Modulate = currentStars >= 1 ? new Color(1f, 0.9f, 0.2f) : new Color(0.7f, 0.7f, 0.7f);
        _star1Label.Text = $"  {(currentStars >= 1 ? "✅" : "⭐")} Chiến thắng trận đấu";

        _star2Label.Modulate = currentStars >= 2 ? new Color(1f, 0.9f, 0.2f) : new Color(0.7f, 0.7f, 0.7f);
        _star2Label.Text = $"  {(currentStars >= 2 ? "✅" : "⭐⭐")} Số tướng sống sót ≥ {stage.Star2Condition}";

        _star3Label.Modulate = currentStars >= 3 ? new Color(1f, 0.9f, 0.2f) : new Color(0.7f, 0.7f, 0.7f);
        _star3Label.Text = $"  {(currentStars >= 3 ? "✅" : "⭐⭐⭐")} Hoàn thành trong ≤ {stage.Star3Condition} lượt";

        // Rewards
        string firstReward = currentStars == 0 ? $" | 🎁 Thưởng đầu: +{stage.FirstClearDiamond} KNB" : "";
        _rewardLabel.Text = $"🎁 Thưởng vượt ải: {stage.NormalGold} Bạc, {stage.NormalExp} EXP{firstReward}";

        // Sweep button only enabled when 3 stars achieved
        _sweepBtn.Disabled = currentStars < 3;
        _sweepBtn.TooltipText = currentStars < 3 ? "Cần đạt 3 sao để mở khóa tính năng Quét nhanh" : "Quét nhanh ải nhận thưởng ngay!";

        Visible = true;
        _modalPanel.Scale = new Vector2(0.9f, 0.9f);
        var tween = CreateTween();
        tween?.TweenProperty(_modalPanel, "scale", Vector2.One, 0.15f)
             .SetTrans(Tween.TransitionType.Back)
             .SetEase(Tween.EaseType.Out);
    }

    public void HidePopup()
    {
        var tween = CreateTween();
        if (tween != null)
        {
            tween.TweenProperty(_modalPanel, "scale", new Vector2(0.9f, 0.9f), 0.1f);
            tween.TweenCallback(Callable.From(() =>
            {
                Visible = false;
                EmitSignal(SignalName.Closed);
            }));
        }
        else
        {
            Visible = false;
            EmitSignal(SignalName.Closed);
        }
    }

    private void OnChallengePressed()
    {
        if (_currentStage == null) return;
        EmitSignal(SignalName.ChallengeRequested, _currentStage);
    }

    private void OnSweepPressed()
    {
        if (_currentStage == null || _currentStars < 3) return;
        EmitSignal(SignalName.SweepRequested, _currentStage);
    }

    private static string FormatEnemyName(string heroId)
    {
        return heroId switch
        {
            "dien_vi" => "Điển Vi",
            "tao_nhan" => "Tào Nhân",
            "hoang_cai" => "Hoàng Cái",
            "hoang_trung" => "Hoàng Trung",
            "tieu_kieu" => "Tiểu Kiều",
            "bang_thong" => "Bàng Thống",
            "chu_du" => "Chu Du",
            "lu_bo" => "Lữ Bố (Boss)",
            "quan_vu" => "Quan Vũ",
            "gia_cat_luong" => "Gia Cát Lượng",
            "sai_van_co" => "Sài Văn Cơ",
            "trieu_van" => "Triệu Vân",
            "ma_sieu" => "Mã Siêu",
            "dieu_thuyen" => "Điêu Thuyền",
            _ => heroId
        };
    }
}
