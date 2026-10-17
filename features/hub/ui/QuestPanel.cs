using System.Collections.Generic;
using Godot;
using TamQuoc3D.Core.Autoload;
using TamQuoc3D.Core.Navigation;

namespace TamQuoc3D.Features.Hub.Ui;

/// <summary>
/// QuestPanel - Bảng theo dõi nhiệm vụ hàng ngày (phong cách MMO) bên cạnh phải màn hình.
/// Có thể thu gọn/mở rộng, theo dõi tiến độ và nhận thưởng trực tiếp.
/// </summary>
[GlobalClass]
public partial class QuestPanel : PanelContainer
{
    public class QuestItemData
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int CurrentProgress { get; set; }
        public int TargetProgress { get; set; }
        public string RewardText { get; set; }
        public int RewardGold { get; set; }
        public int RewardGems { get; set; }
        public bool IsClaimed { get; set; }
    }

    private VBoxContainer _questListContainer;
    private Button _toggleBtn;
    private bool _isCollapsed = false;
    private readonly Dictionary<string, (ProgressBar bar, Label progressLabel, Button claimBtn, QuestItemData data)> _questNodes = new();

    public override void _Ready()
    {
        BuildLayout();
        ConnectSignals();
        LoadDefaultQuests();
    }

    private void BuildLayout()
    {
        CustomMinimumSize = new Vector2(250, 360);
        Size = new Vector2(250, 360);

        var styleBox = new StyleBoxTexture
        {
            Texture = GD.Load<Texture2D>("res://assets/ui/hub/quest_panel_bg.png")
        };
        AddThemeStyleboxOverride("panel", styleBox);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        AddChild(margin);

        var mainVBox = new VBoxContainer();
        mainVBox.AddThemeConstantOverride("separation", 6);
        margin.AddChild(mainVBox);

        // Header with fold/unfold button
        var headerHBox = new HBoxContainer();
        headerHBox.Alignment = BoxContainer.AlignmentMode.Begin;
        mainVBox.AddChild(headerHBox);

        _toggleBtn = new Button
        {
            Text = "📜 NHIỆM VỤ ▼",
            Flat = true,
            CustomMinimumSize = new Vector2(230, 28)
        };
        _toggleBtn.AddThemeFontSizeOverride("font_size", 13);
        _toggleBtn.AddThemeColorOverride("font_color", new Color(1f, 0.88f, 0.4f));
        _toggleBtn.Pressed += ToggleFold;
        headerHBox.AddChild(_toggleBtn);

        // Scrollable or direct list
        _questListContainer = new VBoxContainer();
        _questListContainer.AddThemeConstantOverride("separation", 8);
        mainVBox.AddChild(_questListContainer);
    }

    private void ToggleFold()
    {
        _isCollapsed = !_isCollapsed;
        _questListContainer.Visible = !_isCollapsed;
        _toggleBtn.Text = _isCollapsed ? "📜 NHIỆM VỤ ▶" : "📜 NHIỆM VỤ ▼";
        CustomMinimumSize = _isCollapsed ? new Vector2(250, 44) : new Vector2(250, 360);
        Size = CustomMinimumSize;
    }

    public void LoadDefaultQuests()
    {
        var defaultQuests = new List<QuestItemData>
        {
            new()
            {
                Id = "quest_patrol",
                Title = "[Ải] Tuần Tra Kinh Đô",
                Description = "Vượt ải Tuần Tra 1 lần",
                CurrentProgress = 0,
                TargetProgress = 1,
                RewardText = "+30 vạn Bạc, 330 EXP",
                RewardGold = 300000,
                RewardGems = 50,
                IsClaimed = false
            },
            new()
            {
                Id = "quest_formation",
                Title = "[Quân] Điểm Binh Xuất Trận",
                Description = "Bố trí 5 võ tướng xuất trận",
                CurrentProgress = 5,
                TargetProgress = 5,
                RewardText = "+50 KNB, 500 EXP",
                RewardGold = 100000,
                RewardGems = 50,
                IsClaimed = false
            },
            new()
            {
                Id = "quest_arena",
                Title = "[Đấu] Thí Đấu Lôi Đài",
                Description = "Khiêu chiến Lôi Đài 1 lần",
                CurrentProgress = 0,
                TargetProgress = 1,
                RewardText = "+20 Lệnh, 400 EXP",
                RewardGold = 150000,
                RewardGems = 20,
                IsClaimed = false
            }
        };

        RenderQuests(defaultQuests);
    }

    public void RenderQuests(List<QuestItemData> quests)
    {
        foreach (Node child in _questListContainer.GetChildren())
        {
            child.QueueFree();
        }
        _questNodes.Clear();

        foreach (var q in quests)
        {
            CreateQuestItem(q);
        }
    }

    private void CreateQuestItem(QuestItemData q)
    {
        var itemContainer = new PanelContainer
        {
            CustomMinimumSize = new Vector2(234, 94)
        };
        var itemStyle = new StyleBoxTexture
        {
            Texture = GD.Load<Texture2D>("res://assets/ui/hub/quest_item_bg.png")
        };
        itemContainer.AddThemeStyleboxOverride("panel", itemStyle);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_top", 6);
        margin.AddThemeConstantOverride("margin_bottom", 6);
        itemContainer.AddChild(margin);

        var vBox = new VBoxContainer();
        vBox.AddThemeConstantOverride("separation", 3);
        margin.AddChild(vBox);

        // Title & Status
        var titleRow = new HBoxContainer();
        vBox.AddChild(titleRow);

        var titleLabel = new Label
        {
            Text = q.Title,
            Modulate = new Color(1f, 0.95f, 0.7f),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        titleLabel.AddThemeFontSizeOverride("font_size", 12);
        titleRow.AddChild(titleLabel);

        // Description
        var descLabel = new Label
        {
            Text = q.Description,
            Modulate = new Color(0.8f, 0.8f, 0.85f)
        };
        descLabel.AddThemeFontSizeOverride("font_size", 10);
        vBox.AddChild(descLabel);

        // Progress bar + Label
        var progressRow = new HBoxContainer();
        progressRow.AddThemeConstantOverride("separation", 6);
        vBox.AddChild(progressRow);

        var pBar = new ProgressBar
        {
            CustomMinimumSize = new Vector2(150, 8),
            MinValue = 0,
            MaxValue = q.TargetProgress,
            Value = q.CurrentProgress,
            ShowPercentage = false,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        var pBarBg = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.1f, 0.16f),
            CornerRadiusBottomLeft = 3,
            CornerRadiusBottomRight = 3,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3
        };
        var pBarFill = new StyleBoxFlat
        {
            BgColor = new Color(0.3f, 0.8f, 0.4f),
            CornerRadiusBottomLeft = 3,
            CornerRadiusBottomRight = 3,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3
        };
        pBar.AddThemeStyleboxOverride("background", pBarBg);
        pBar.AddThemeStyleboxOverride("fill", pBarFill);
        progressRow.AddChild(pBar);

        var pLabel = new Label
        {
            Text = $"{q.CurrentProgress}/{q.TargetProgress}",
            Modulate = new Color(0.9f, 0.9f, 0.9f)
        };
        pLabel.AddThemeFontSizeOverride("font_size", 10);
        progressRow.AddChild(pLabel);

        // Bottom row: Reward text + Claim Button
        var bottomRow = new HBoxContainer();
        bottomRow.AddThemeConstantOverride("separation", 6);
        vBox.AddChild(bottomRow);

        var rewardLabel = new Label
        {
            Text = q.RewardText,
            Modulate = new Color(1f, 0.8f, 0.3f),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        rewardLabel.AddThemeFontSizeOverride("font_size", 10);
        bottomRow.AddChild(rewardLabel);

        var claimBtn = new Button
        {
            CustomMinimumSize = new Vector2(52, 22),
            Text = "Nhận"
        };
        claimBtn.AddThemeFontSizeOverride("font_size", 11);

        bool canClaim = q.CurrentProgress >= q.TargetProgress && !q.IsClaimed;
        ApplyClaimButtonStyle(claimBtn, canClaim, q.IsClaimed);

        claimBtn.Pressed += () => OnClaimPressed(q.Id);

        bottomRow.AddChild(claimBtn);
        _questListContainer.AddChild(itemContainer);

        _questNodes[q.Id] = (pBar, pLabel, claimBtn, q);
    }

    private void ApplyClaimButtonStyle(Button btn, bool canClaim, bool isClaimed)
    {
        var style = new StyleBoxFlat
        {
            CornerRadiusBottomLeft = 3,
            CornerRadiusBottomRight = 3,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3
        };

        if (isClaimed)
        {
            btn.Text = "Đã Nhận";
            btn.Disabled = true;
            style.BgColor = new Color(0.2f, 0.2f, 0.25f, 0.6f);
            btn.Modulate = new Color(0.6f, 0.6f, 0.6f);
        }
        else if (canClaim)
        {
            btn.Text = "Nhận";
            btn.Disabled = false;
            style.BgColor = new Color(0.75f, 0.2f, 0.25f);
            style.BorderColor = new Color(1f, 0.85f, 0.3f);
            style.BorderWidthBottom = 1;
            style.BorderWidthTop = 1;
            style.BorderWidthLeft = 1;
            style.BorderWidthRight = 1;
            btn.Modulate = Colors.White;
        }
        else
        {
            btn.Text = "Đi";
            btn.Disabled = false;
            style.BgColor = new Color(0.18f, 0.28f, 0.42f);
            btn.Modulate = new Color(0.85f, 0.85f, 0.85f);
        }

        btn.AddThemeStyleboxOverride("normal", style);
    }

    private void OnClaimPressed(string questId)
    {
        if (!_questNodes.TryGetValue(questId, out var item)) return;

        if (item.data.CurrentProgress >= item.data.TargetProgress && !item.data.IsClaimed)
        {
            item.data.IsClaimed = true;
            ApplyClaimButtonStyle(item.claimBtn, false, true);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddGold(item.data.RewardGold);
                GameManager.Instance.AddGems(item.data.RewardGems);
            }

            EventBus.Instance?.EmitSignal(EventBus.SignalName.QuestCompleted, questId);
            EventBus.Instance?.EmitSignal(EventBus.SignalName.SystemNotification, "Nhiệm Vụ Hoàn Thành", $"Nhận thành công: {item.data.RewardText}", "quest");
        }
        else if (item.data.CurrentProgress < item.data.TargetProgress)
        {
            // Quick navigate to task
            if (questId == "quest_formation")
            {
                SceneNavigator.GoToFormationSelector("hub");
            }
            else if (questId == "quest_patrol")
            {
                SceneNavigator.GoToCampaign();
            }
            else if (questId == "quest_arena")
            {
                SceneNavigator.GoToArena();
            }
        }
    }

    private void ConnectSignals()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.QuestProgressUpdated += UpdateQuestProgress;
        }
    }

    public void UpdateQuestProgress(string questId, int current, int target)
    {
        if (_questNodes.TryGetValue(questId, out var item))
        {
            item.data.CurrentProgress = current;
            item.data.TargetProgress = target;
            item.bar.MaxValue = target;
            item.bar.Value = current;
            item.progressLabel.Text = $"{current}/{target}";

            bool canClaim = current >= target && !item.data.IsClaimed;
            ApplyClaimButtonStyle(item.claimBtn, canClaim, item.data.IsClaimed);
        }
    }
}
