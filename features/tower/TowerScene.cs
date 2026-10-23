using Godot;
using TamQuoc3D.Core.Autoload;
using TamQuoc3D.Core.Navigation;
using TamQuoc3D.Core.Network;

namespace TamQuoc3D.Features.Tower;

/// <summary>
/// TowerScene (Phase 4) - Thap Thi Luyen 50 tang:
/// Moi tang la mot tran PvE that trong BattleScene (dich sinh theo tang,
/// chi so +12%/tang). Thang tang thi nhan thuong tu server (100 Bac/tang,
/// +20 KC moi 5 tang) va server ghi highest_floor - khong nhay tang duoc.
/// Offline: tien trinh + thuong tinh local trong GameManager.
/// </summary>
[GlobalClass]
public partial class TowerScene : Control
{
    public const int MaxFloor = 50;

    private GridContainer _floorGrid;
    private Label _progressLabel;
    private Label _walletLabel;
    private int _highestFloor;

    public override void _Ready()
    {
        BuildUi();
        _ = LoadProgressAsync();
    }

    private async System.Threading.Tasks.Task LoadProgressAsync()
    {
        var manager = GameManager.Instance;
        _highestFloor = manager?.TowerHighestFloor ?? 0;

        var network = NetworkManager.Instance;
        if (network != null && network.IsServerOnline && network.IsAuthenticated)
        {
            var progress = await network.FetchTowerProgressAsync();
            if (progress != null)
            {
                _highestFloor = progress.HighestFloor;
                if (manager != null) manager.TowerHighestFloor = _highestFloor;
            }
        }
        RefreshFloors();
    }

    private void BuildUi()
    {
        var bg = new ColorRect { Color = new Color(0.07f, 0.07f, 0.11f) };
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

        var title = new Label { Text = "🗼 THÁP THÍ LUYỆN" };
        title.AddThemeFontSizeOverride("font_size", 26);
        title.AddThemeColorOverride("font_color", new Color(0.75f, 0.8f, 1.0f));
        header.AddChild(title);

        header.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.Expand });
        _walletLabel = new Label();
        _walletLabel.AddThemeFontSizeOverride("font_size", 16);
        header.AddChild(_walletLabel);
        RefreshWallet();

        _progressLabel = new Label
        {
            Text = "Thưởng: 100 Bạc × tầng (+20 Kim Cương mỗi 5 tầng) • Địch mạnh dần +12%/tầng"
        };
        _progressLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.88f, 0.95f));
        root.AddChild(_progressLabel);

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.Expand,
            SizeFlagsVertical = SizeFlags.Expand
        };
        root.AddChild(scroll);

        _floorGrid = new GridContainer { Columns = 5 };
        _floorGrid.AddThemeConstantOverride("h_separation", 10);
        _floorGrid.AddThemeConstantOverride("v_separation", 10);
        scroll.AddChild(_floorGrid);
    }

    private void RefreshFloors()
    {
        _progressLabel.Text = $"Đã qua: {_highestFloor}/{MaxFloor} tầng • Thưởng 100 Bạc × tầng (+20 KC mỗi 5 tầng) • Địch mạnh dần +12%/tầng";

        foreach (var child in _floorGrid.GetChildren())
        {
            _floorGrid.RemoveChild(child);
            child.QueueFree();
        }

        // Hien tu tang cao xuong tang 1 (tang dang thach dau o dau danh sach hien thi nguoc)
        for (int floor = MaxFloor; floor >= 1; floor--)
        {
            _floorGrid.AddChild(BuildFloorButton(floor));
        }
    }

    private Control BuildFloorButton(int floor)
    {
        bool cleared = floor <= _highestFloor;
        bool current = floor == _highestFloor + 1;
        int enemyCount = System.Math.Min(5, 3 + floor / 8);

        var btn = new Button
        {
            CustomMinimumSize = new Vector2(200, 74),
            Text = cleared
                ? $"✅ Tầng {floor} — Đã qua"
                : current
                    ? $"⚔️ Tầng {floor} — THÁCH ĐẤU\n{enemyCount} địch • x{1.0f + 0.12f * (floor - 1):0.00} sức mạnh"
                    : $"🔒 Tầng {floor}",
            Disabled = !current && !cleared
        };
        if (current)
        {
            btn.AddThemeColorOverride("font_color", new Color(1.0f, 0.9f, 0.5f));
        }
        int captured = floor;
        btn.Pressed += () => OnChallengeFloor(captured);
        return btn;
    }

    private void OnChallengeFloor(int floor)
    {
        var manager = GameManager.Instance;
        if (manager == null) return;
        manager.BattleContext = "tower";
        manager.CurrentTowerFloor = floor;
        manager.CurrentStage = null;
        SceneNavigator.GoToBattle();
    }

    private void RefreshWallet()
    {
        var manager = GameManager.Instance;
        if (manager == null || _walletLabel == null) return;
        _walletLabel.Text = $"🪙 {manager.Gold:N0} Bạc    💎 {manager.Gems:N0} Kim Cương";
    }
}
