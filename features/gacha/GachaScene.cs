using System.Collections.Generic;
using Godot;
using TamQuoc3D.Core.Autoload;
using TamQuoc3D.Core.Navigation;
using TamQuoc3D.Core.Network;
using TamQuoc3D.Data.Database;

namespace TamQuoc3D.Features.Gacha;

/// <summary>
/// GachaScene (Phase 4) - Chieu mo tuong: goi server /api/gacha/pull
/// (ti le UR 2% / SSR 8% / SR 35%, pity UR 80 / SSR rieng, gia 100 KC/luot).
/// Ket qua hien dang luoi the voi mau theo do hiem + hieu ung lat the don gian.
/// Chi hoat dong khi da dang nhap (gacha la he thong server-authoritative).
/// </summary>
[GlobalClass]
public partial class GachaScene : Control
{
    private const string BannerId = "limited_lu_bo";
    private const int CostPerPull = 100;

    private Label _walletLabel;
    private Label _pityLabel;
    private Label _statusLabel;
    private GridContainer _resultsGrid;
    private Button _pull1Btn;
    private Button _pull10Btn;

    public override void _Ready()
    {
        BuildUi();
        RefreshWallet();
        UpdatePityLabel(null);
    }

    private void BuildUi()
    {
        var bg = new ColorRect { Color = new Color(0.09f, 0.05f, 0.13f) };
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

        var title = new Label { Text = "🎰 CHIÊU MỘ DANH TƯỚNG" };
        title.AddThemeFontSizeOverride("font_size", 26);
        title.AddThemeColorOverride("font_color", new Color(1.0f, 0.6f, 0.85f));
        header.AddChild(title);

        header.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.Expand });
        _walletLabel = new Label();
        _walletLabel.AddThemeFontSizeOverride("font_size", 16);
        header.AddChild(_walletLabel);

        var banner = new Label
        {
            Text = "🔥 BANNER GIỚI HẠN: LỮ BỐ — CHIẾN THẦN VÔ SONG 🔥\nTỷ lệ: UR 2% • SSR 8% • SR 35% • R 55% — Bảo hiểm UR ở lượt 80, SSR ở lượt gần nhất được server theo dõi riêng.",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        banner.AddThemeFontSizeOverride("font_size", 15);
        banner.AddThemeColorOverride("font_color", new Color(0.95f, 0.85f, 0.6f));
        root.AddChild(banner);

        _pityLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _pityLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.85f, 1.0f));
        root.AddChild(_pityLabel);

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.Expand,
            SizeFlagsVertical = SizeFlags.Expand
        };
        root.AddChild(scroll);

        _resultsGrid = new GridContainer { Columns = 5 };
        _resultsGrid.AddThemeConstantOverride("h_separation", 12);
        _resultsGrid.AddThemeConstantOverride("v_separation", 12);
        scroll.AddChild(_resultsGrid);

        _statusLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _statusLabel.AddThemeFontSizeOverride("font_size", 16);
        root.AddChild(_statusLabel);

        var actions = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        actions.AddThemeConstantOverride("separation", 16);
        root.AddChild(actions);

        _pull1Btn = new Button
        {
            Text = $"🎫 CHIÊU MỘ x1 — {CostPerPull} 💎",
            CustomMinimumSize = new Vector2(240, 52)
        };
        _pull1Btn.AddThemeFontSizeOverride("font_size", 17);
        _pull1Btn.Pressed += async () => await OnPull(1);
        actions.AddChild(_pull1Btn);

        _pull10Btn = new Button
        {
            Text = $"🎫🎫 CHIÊU MỘ x10 — {CostPerPull * 10} 💎",
            CustomMinimumSize = new Vector2(260, 52)
        };
        _pull10Btn.AddThemeFontSizeOverride("font_size", 17);
        _pull10Btn.Pressed += async () => await OnPull(10);
        actions.AddChild(_pull10Btn);
    }

    private async System.Threading.Tasks.Task OnPull(int count)
    {
        var network = NetworkManager.Instance;
        if (network == null || !network.IsServerOnline || !network.IsAuthenticated)
        {
            _statusLabel.Text = "⚠️ Chiêu mộ cần kết nối server và đăng nhập tài khoản.";
            return;
        }

        _pull1Btn.Disabled = true;
        _pull10Btn.Disabled = true;
        _statusLabel.Text = "Đang chiêu mộ...";

        var result = await network.PullGachaAsync(BannerId, count);
        if (result == null)
        {
            _statusLabel.Text = "❌ Chiêu mộ thất bại (không đủ Kim Cương hoặc lỗi server).";
        }
        else
        {
            ShowResults(result.Results ?? new List<NetworkManager.GachaItemDto>());
            UpdatePityLabel(result);
            // Tuong moi da ve server -> dong bo danh sach so huu
            _ = network.SyncPlayerDataAsync();
            _statusLabel.Text = $"🎉 Hoàn tất! Đã dùng {result.GoldSpent} 💎.";
        }

        RefreshWallet();
        _pull1Btn.Disabled = false;
        _pull10Btn.Disabled = false;
    }

    private void ShowResults(List<NetworkManager.GachaItemDto> results)
    {
        foreach (var child in _resultsGrid.GetChildren())
        {
            _resultsGrid.RemoveChild(child);
            child.QueueFree();
        }

        foreach (var item in results)
        {
            _resultsGrid.AddChild(BuildResultCard(item));
        }
    }

    private Control BuildResultCard(NetworkManager.GachaItemDto item)
    {
        var hero = HeroDatabase.Instance?.GetHero(item.HeroId);
        string heroName = hero?.HeroName ?? item.HeroId;
        string rarity = (item.Rarity ?? "R").ToUpperInvariant();

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(170, 210) };
        var sb = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.1f, 0.18f, 0.98f),
            BorderColor = RarityColor(rarity)
        };
        sb.SetBorderWidthAll(rarity is "UR" or "SSR" ? 3 : 2);
        sb.SetCornerRadiusAll(10);
        sb.ContentMarginLeft = 8;
        sb.ContentMarginRight = 8;
        sb.ContentMarginTop = 8;
        sb.ContentMarginBottom = 8;
        panel.AddThemeStyleboxOverride("panel", sb);

        var vbox = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        vbox.AddThemeConstantOverride("separation", 4);
        panel.AddChild(vbox);

        var icon = new TextureRect
        {
            CustomMinimumSize = new Vector2(96, 96),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter
        };
        var tex = GD.Load<Texture2D>($"res://assets/heroes/{item.HeroId}/icon.png");
        if (tex != null) icon.Texture = tex;
        vbox.AddChild(icon);

        var rarityLabel = new Label
        {
            Text = rarity,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        rarityLabel.AddThemeFontSizeOverride("font_size", 18);
        rarityLabel.AddThemeColorOverride("font_color", RarityColor(rarity));
        vbox.AddChild(rarityLabel);

        var nameLabel = new Label
        {
            Text = heroName,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        vbox.AddChild(nameLabel);

        // Hieu ung "lat the": the sang dan tu mo toi ro
        panel.Modulate = new Color(1, 1, 1, 0);
        var tween = CreateTween();
        tween.TweenProperty(panel, "modulate:a", 1.0f, 0.35f);
        return panel;
    }

    private void UpdatePityLabel(NetworkManager.GachaResultDto result)
    {
        _pityLabel.Text = result == null
            ? "Pity: UR tối đa 80 lượt • SSR được đảm bảo định kỳ — server theo dõi chính xác."
            : $"Pity hiện tại: còn tối đa {80 - result.PityUr} lượt tới UR • {result.PitySsr} lượt kể từ SSR gần nhất.";
    }

    private void RefreshWallet()
    {
        var manager = GameManager.Instance;
        if (manager == null) return;
        _walletLabel.Text = $"🪙 {manager.Gold:N0} Bạc    💎 {manager.Gems:N0} Kim Cương";
    }

    private static Color RarityColor(string rarity) => rarity switch
    {
        "UR" => new Color(1.0f, 0.35f, 0.45f),
        "SSR" => new Color(1.0f, 0.75f, 0.25f),
        "SR" => new Color(0.7f, 0.5f, 1.0f),
        _ => new Color(0.6f, 0.85f, 1.0f)
    };
}
