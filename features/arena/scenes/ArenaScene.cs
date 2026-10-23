using System.Collections.Generic;
using Godot;
using TamQuoc3D.Core.Autoload;
using TamQuoc3D.Core.Navigation;
using TamQuoc3D.Core.Network;

namespace TamQuoc3D.Features.Arena;

/// <summary>
/// ArenaScene (Phase 4) - Dau truong async PvP:
/// Server mo phong tran bang DOI THAT cua 2 nguoi choi (doi hinh PvE da luu,
/// chi so da scale theo level/sao) - khong con so lieu gia co dinh.
/// Thang +25 MMR / thua -15, thuong 300 Bac + 3 Arena Token khi thang.
/// Khong co doi thu that thi danh voi NPC suc manh tuong duong.
/// </summary>
[GlobalClass]
public partial class ArenaScene : Control
{
    private Label _profileLabel;
    private Label _statusLabel;
    private VBoxContainer _opponentList;
    private RichTextLabel _logText;
    private Button _npcBtn;
    private Button _refreshBtn;

    public override void _Ready()
    {
        BuildUi();
        _ = LoadArenaAsync();
    }

    private bool Online => NetworkManager.Instance != null
        && NetworkManager.Instance.IsServerOnline
        && NetworkManager.Instance.IsAuthenticated;

    private void BuildUi()
    {
        var bg = new ColorRect { Color = new Color(0.1f, 0.05f, 0.08f) };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        AddChild(margin);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 10);
        margin.AddChild(root);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 12);
        root.AddChild(header);

        var backBtn = new Button { Text = "⬅ Quay lại", CustomMinimumSize = new Vector2(110, 40) };
        backBtn.Pressed += () => SceneNavigator.GoBack();
        header.AddChild(backBtn);

        var title = new Label { Text = "🏟️ ĐẤU TRƯỜNG LÔI ĐÀI" };
        title.AddThemeFontSizeOverride("font_size", 26);
        title.AddThemeColorOverride("font_color", new Color(1.0f, 0.55f, 0.6f));
        header.AddChild(title);

        header.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.Expand });

        _refreshBtn = new Button { Text = "🔄 Làm mới đối thủ", CustomMinimumSize = new Vector2(170, 40) };
        _refreshBtn.Pressed += () => _ = LoadArenaAsync();
        header.AddChild(_refreshBtn);

        _profileLabel = new Label();
        _profileLabel.AddThemeFontSizeOverride("font_size", 17);
        _profileLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.9f, 0.7f));
        root.AddChild(_profileLabel);

        _statusLabel = new Label { Text = "" };
        _statusLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.9f, 1.0f));
        root.AddChild(_statusLabel);

        var main = new HBoxContainer();
        main.AddThemeConstantOverride("separation", 12);
        main.SizeFlagsVertical = SizeFlags.Expand;
        root.AddChild(main);

        var leftPanel = new VBoxContainer { CustomMinimumSize = new Vector2(520, 0) };
        leftPanel.AddThemeConstantOverride("separation", 8);
        main.AddChild(leftPanel);

        var oppTitle = new Label { Text = "⚔️ ĐỐI THỦ (MMR gần bạn)" };
        oppTitle.AddThemeFontSizeOverride("font_size", 18);
        leftPanel.AddChild(oppTitle);

        _npcBtn = new Button
        {
            Text = "🤖 Đánh với NPC Thủ Vệ (luyện tập có thưởng)",
            CustomMinimumSize = new Vector2(0, 44)
        };
        _npcBtn.Pressed += async () => await OnChallenge(null);
        leftPanel.AddChild(_npcBtn);

        var oppScroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.Expand,
            SizeFlagsVertical = SizeFlags.Expand
        };
        leftPanel.AddChild(oppScroll);
        _opponentList = new VBoxContainer();
        _opponentList.AddThemeConstantOverride("separation", 8);
        oppScroll.AddChild(_opponentList);

        var logPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.Expand };
        var sb = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.05f, 0.09f, 0.97f),
            BorderColor = new Color(0.6f, 0.35f, 0.4f)
        };
        sb.SetBorderWidthAll(2);
        sb.SetCornerRadiusAll(10);
        sb.ContentMarginLeft = 12;
        sb.ContentMarginRight = 12;
        sb.ContentMarginTop = 10;
        sb.ContentMarginBottom = 10;
        logPanel.AddThemeStyleboxOverride("panel", sb);
        main.AddChild(logPanel);

        _logText = new RichTextLabel
        {
            BbcodeEnabled = true,
            ScrollFollowing = true,
            Text = "Nhật ký trận đấu sẽ hiện ở đây. Chọn một đối thủ để thách đấu!"
        };
        logPanel.AddChild(_logText);
    }

    private async System.Threading.Tasks.Task LoadArenaAsync()
    {
        if (!Online)
        {
            _profileLabel.Text = "⚠️ Đấu trường cần kết nối server và đăng nhập.";
            _npcBtn.Disabled = true;
            return;
        }
        _npcBtn.Disabled = false;

        var network = NetworkManager.Instance;
        var profile = await network.FetchArenaProfileAsync();
        if (profile != null)
        {
            _profileLabel.Text =
                $"🏅 {profile.DisplayName} • Hạng {profile.RankTier} • MMR {profile.Mmr} • " +
                $"Thắng {profile.WinCount} / Thua {profile.LossCount} • Chuỗi thắng {profile.CurrentWinningStreak} • " +
                $"⚡ Lực chiến đội: {profile.CombatPower:N0}";
        }

        foreach (var child in _opponentList.GetChildren())
        {
            _opponentList.RemoveChild(child);
            child.QueueFree();
        }

        var opponents = await network.FetchArenaOpponentsAsync();
        if (opponents.Count == 0)
        {
            _opponentList.AddChild(new Label
            {
                Text = "Chưa có người chơi khác trên đấu trường. Hãy đánh với NPC Thủ Vệ!",
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            });
            return;
        }

        foreach (var opponent in opponents)
        {
            _opponentList.AddChild(BuildOpponentRow(opponent));
        }
    }

    private Control BuildOpponentRow(NetworkManager.ArenaOpponentDto opponent)
    {
        var row = new PanelContainer { CustomMinimumSize = new Vector2(0, 64) };
        var sb = new StyleBoxFlat
        {
            BgColor = new Color(0.14f, 0.09f, 0.13f, 0.98f),
            BorderColor = new Color(0.55f, 0.4f, 0.45f)
        };
        sb.SetBorderWidthAll(1);
        sb.SetCornerRadiusAll(8);
        sb.ContentMarginLeft = 12;
        sb.ContentMarginRight = 12;
        sb.ContentMarginTop = 8;
        sb.ContentMarginBottom = 8;
        row.AddThemeStyleboxOverride("panel", sb);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 10);
        row.AddChild(hbox);

        var info = new Label
        {
            Text = $"👤 {opponent.DisplayName}\nMMR {opponent.Mmr} • Thắng {opponent.WinCount}/Thua {opponent.LossCount} • ⚡ {opponent.CombatPower:N0}",
            SizeFlagsHorizontal = SizeFlags.Expand
        };
        hbox.AddChild(info);

        var btn = new Button
        {
            Text = "⚔️ Thách đấu",
            CustomMinimumSize = new Vector2(120, 40)
        };
        string defenderId = opponent.UserId;
        btn.Pressed += async () => await OnChallenge(defenderId);
        hbox.AddChild(btn);
        return row;
    }

    private async System.Threading.Tasks.Task OnChallenge(string defenderId)
    {
        if (!Online)
        {
            _statusLabel.Text = "⚠️ Cần kết nối server để thi đấu.";
            return;
        }

        _statusLabel.Text = "Đang thi đấu...";
        _logText.Text = "";

        var result = await NetworkManager.Instance.SimulateBattleAsync("ASYNC_ARENA", defenderId);
        if (result == null)
        {
            _statusLabel.Text = "❌ Thi đấu thất bại (lỗi server).";
            return;
        }

        var lines = new List<string>
        {
            $"[b]Bạn (⚡{result.AttackerPower:N0}) vs {result.DefenderName} (⚡{result.DefenderPower:N0})[/b]"
        };
        if (result.Log != null) lines.AddRange(result.Log);
        _logText.Text = string.Join("\n", lines);

        string rewardText = result.Rewards != null
            ? $" • Thưởng: +{result.Rewards.Silver} Bạc, +{result.Rewards.ArenaTokens} Arena Token"
            : "";
        string mmrText = result.MmrAfter.HasValue ? $" • MMR mới: {result.MmrAfter.Value}" : "";
        _statusLabel.Text = result.AttackerWin
            ? $"🏆 CHIẾN THẮNG sau {result.Rounds} hiệp!{rewardText}{mmrText}"
            : $"💀 Thất bại sau {result.Rounds} hiệp.{rewardText}{mmrText}";

        _ = NetworkManager.Instance.FetchWalletAsync();
        _ = LoadArenaAsync();
    }
}
