using System;
using System.Collections.Generic;
using Godot;
using TamQuoc3D.Core.Autoload;
using TamQuoc3D.Core.Constants;
using TamQuoc3D.Core.Navigation;
using TamQuoc3D.Core.Network;
using TamQuoc3D.Data.Database;
using TamQuoc3D.Data.Resources;
using TamQuoc3D.Features.DeckBuilder;

namespace TamQuoc3D.Features.HeroCollection;

/// <summary>
/// HeroCollectionScene (Phase 3) - Tuong Phu day du:
/// luoi tuong so huu + loc theo hang, panel chi tiet (chi so goc -> sau scale
/// level/sao, tien trinh EXP, ky nang) va nut len sao (server authoritative,
/// offline thi tru vi local). Level tu EXP: thang tran chien dich cong EXP
/// cho tuong trong doi hinh (server), 300 EXP = 1 level.
/// </summary>
[GlobalClass]
public partial class HeroCollectionScene : Control
{
    private readonly List<HeroData> _heroes = [];
    private BattlePosition? _filter = null;
    private HeroData _selected;

    private GridContainer _grid;
    private Label _walletLabel;
    private Label _statusLabel;

    private TextureRect _detailIcon;
    private Label _detailTitle;
    private Label _detailMeta;
    private Label _detailStars;
    private Label _detailExp;
    private ProgressBar _expBar;
    private Label _detailStats;
    private Label _detailSkills;
    private Button _starUpBtn;

    public override void _Ready()
    {
        BuildUi();
        LoadHeroes();
        RefreshGrid();
        RefreshWallet();
    }

    // ==================== Data ====================

    private void LoadHeroes()
    {
        _heroes.Clear();
        var manager = GameManager.Instance;
        if (manager != null && manager.OwnedHeroes.Count > 0)
        {
            foreach (var hero in manager.OwnedHeroes)
            {
                if (hero != null) _heroes.Add(hero);
            }
        }
        if (_heroes.Count == 0 && HeroDatabase.Instance != null)
        {
            _heroes.AddRange(HeroDatabase.Instance.GetAllHeroes());
        }
        // Sap xep: do hiem giam dan, sau do luc chien giam dan
        _heroes.Sort((a, b) =>
        {
            int byRarity = b.Rarity.CompareTo(a.Rarity);
            return byRarity != 0
                ? byRarity
                : HeroCard.CalculateCombatPower(b).CompareTo(HeroCard.CalculateCombatPower(a));
        });
    }

    private GameManager.HeroProgress ProgressOf(HeroData hero) =>
        GameManager.Instance?.GetHeroProgress(hero.HeroId) ?? new GameManager.HeroProgress();

    // ==================== UI ====================

    private void BuildUi()
    {
        var bg = new ColorRect { Color = new Color(0.07f, 0.08f, 0.12f) };
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

        // Header
        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 12);
        root.AddChild(header);

        var backBtn = new Button { Text = "⬅ Quay lại", CustomMinimumSize = new Vector2(110, 40) };
        backBtn.Pressed += () => SceneNavigator.GoBack();
        header.AddChild(backBtn);

        var title = new Label { Text = "🎖️ TƯỚNG PHỦ" };
        title.AddThemeFontSizeOverride("font_size", 26);
        title.AddThemeColorOverride("font_color", new Color(1.0f, 0.85f, 0.35f));
        header.AddChild(title);

        header.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.Expand });

        _walletLabel = new Label();
        _walletLabel.AddThemeFontSizeOverride("font_size", 16);
        header.AddChild(_walletLabel);

        // Filter bar + status
        var filterBar = new HBoxContainer();
        filterBar.AddThemeConstantOverride("separation", 8);
        root.AddChild(filterBar);
        AddFilterButton(filterBar, "Tất cả", null);
        AddFilterButton(filterBar, "Tiền quân", BattlePosition.Front);
        AddFilterButton(filterBar, "Trung quân", BattlePosition.Mid);
        AddFilterButton(filterBar, "Hậu quân", BattlePosition.Back);

        filterBar.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.Expand });
        _statusLabel = new Label { Text = "" };
        _statusLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.9f, 1.0f));
        filterBar.AddChild(_statusLabel);

        // Main split: grid (left) + detail (right)
        var main = new HBoxContainer();
        main.AddThemeConstantOverride("separation", 12);
        main.SizeFlagsVertical = SizeFlags.Expand;
        root.AddChild(main);

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.Expand,
            SizeFlagsVertical = SizeFlags.Expand
        };
        main.AddChild(scroll);

        _grid = new GridContainer { Columns = 4 };
        _grid.AddThemeConstantOverride("h_separation", 10);
        _grid.AddThemeConstantOverride("v_separation", 10);
        scroll.AddChild(_grid);

        BuildDetailPanel(main);
    }

    private void AddFilterButton(HBoxContainer bar, string text, BattlePosition? filter)
    {
        var btn = new Button { Text = text, CustomMinimumSize = new Vector2(110, 34) };
        btn.Pressed += () =>
        {
            _filter = filter;
            RefreshGrid();
        };
        bar.AddChild(btn);
    }

    private void BuildDetailPanel(HBoxContainer main)
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(400, 0) };
        var sb = new StyleBoxFlat
        {
            BgColor = new Color(0.11f, 0.12f, 0.18f, 0.97f),
            BorderColor = new Color(0.85f, 0.7f, 0.25f)
        };
        sb.SetBorderWidthAll(2);
        sb.SetCornerRadiusAll(10);
        sb.ContentMarginLeft = 16;
        sb.ContentMarginRight = 16;
        sb.ContentMarginTop = 14;
        sb.ContentMarginBottom = 14;
        panel.AddThemeStyleboxOverride("panel", sb);
        main.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);
        panel.AddChild(vbox);

        _detailIcon = new TextureRect
        {
            CustomMinimumSize = new Vector2(128, 128),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter
        };
        vbox.AddChild(_detailIcon);

        _detailTitle = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _detailTitle.AddThemeFontSizeOverride("font_size", 22);
        vbox.AddChild(_detailTitle);

        _detailMeta = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _detailMeta.AddThemeColorOverride("font_color", new Color(0.75f, 0.8f, 0.9f));
        vbox.AddChild(_detailMeta);

        _detailStars = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _detailStars.AddThemeFontSizeOverride("font_size", 20);
        _detailStars.AddThemeColorOverride("font_color", new Color(1.0f, 0.8f, 0.2f));
        vbox.AddChild(_detailStars);

        _detailExp = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        vbox.AddChild(_detailExp);

        _expBar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = GameManager.ExpPerLevel,
            CustomMinimumSize = new Vector2(0, 14),
            ShowPercentage = false
        };
        vbox.AddChild(_expBar);

        _detailStats = new Label();
        _detailStats.AddThemeFontSizeOverride("font_size", 15);
        vbox.AddChild(_detailStats);

        _detailSkills = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _detailSkills.AddThemeFontSizeOverride("font_size", 14);
        _detailSkills.AddThemeColorOverride("font_color", new Color(0.85f, 0.88f, 0.95f));
        vbox.AddChild(_detailSkills);

        vbox.AddChild(new Control { SizeFlagsVertical = SizeFlags.Expand });

        _starUpBtn = new Button { CustomMinimumSize = new Vector2(0, 46) };
        _starUpBtn.Pressed += async () => await OnStarUpPressed();
        vbox.AddChild(_starUpBtn);
    }

    // ==================== Grid + detail refresh ====================

    private void RefreshGrid()
    {
        foreach (var child in _grid.GetChildren())
        {
            _grid.RemoveChild(child);
            child.QueueFree();
        }

        foreach (var hero in _heroes)
        {
            if (_filter.HasValue && hero.Position != _filter.Value) continue;
            _grid.AddChild(BuildHeroCard(hero));
        }

        if (_selected != null)
        {
            RefreshDetail();
        }
        else if (_heroes.Count > 0)
        {
            // Tu hub bam vao mot tuong tren san -> mo thang chi tiet tuong do
            var focusId = GameManager.Instance?.FocusHeroId;
            var focused = !string.IsNullOrEmpty(focusId)
                ? _heroes.Find(h => h.HeroId == focusId)
                : null;
            if (GameManager.Instance != null) GameManager.Instance.FocusHeroId = null;
            SelectHero(focused ?? _heroes[0]);
        }
    }

    private Control BuildHeroCard(HeroData hero)
    {
        var progress = ProgressOf(hero);
        var btn = new Button
        {
            CustomMinimumSize = new Vector2(132, 158),
            Text = ""
        };

        var vbox = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        vbox.SetAnchorsPreset(LayoutPreset.FullRect);
        vbox.AddThemeConstantOverride("separation", 2);
        btn.AddChild(vbox);

        var icon = new TextureRect
        {
            CustomMinimumSize = new Vector2(72, 72),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter
        };
        var tex = GD.Load<Texture2D>($"res://assets/heroes/{hero.HeroId}/icon.png");
        if (tex != null) icon.Texture = tex;
        vbox.AddChild(icon);

        var nameLabel = new Label
        {
            Text = hero.HeroName,
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        nameLabel.AddThemeColorOverride("font_color", RarityColor(hero.Rarity));
        vbox.AddChild(nameLabel);

        var infoLabel = new Label
        {
            Text = $"Lv.{progress.Level}  {StarsText(progress.Star)}",
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        infoLabel.AddThemeFontSizeOverride("font_size", 12);
        vbox.AddChild(infoLabel);

        btn.Pressed += () => SelectHero(hero);
        return btn;
    }

    private void SelectHero(HeroData hero)
    {
        _selected = hero;
        RefreshDetail();
    }

    private void RefreshDetail()
    {
        if (_selected == null) return;
        var hero = _selected;
        var progress = ProgressOf(hero);
        float scale = GameManager.HeroStatScale(progress);
        var s = hero.BaseStats;

        var tex = GD.Load<Texture2D>($"res://assets/heroes/{hero.HeroId}/icon.png");
        if (tex != null) _detailIcon.Texture = tex;

        _detailTitle.Text = hero.HeroName;
        _detailTitle.AddThemeColorOverride("font_color", RarityColor(hero.Rarity));
        _detailMeta.Text = $"{hero.Title} • {hero.Rarity} • Hệ {hero.Element} • {PositionText(hero.Position)}";
        _detailStars.Text = StarsText(progress.Star);

        if (progress.Level >= 60)
        {
            _detailExp.Text = $"Cấp {progress.Level} (TỐI ĐA) • EXP {progress.Exp}";
            _expBar.Value = GameManager.ExpPerLevel;
        }
        else
        {
            long intoLevel = progress.Exp % GameManager.ExpPerLevel;
            _detailExp.Text = $"Cấp {progress.Level} • EXP {intoLevel}/{GameManager.ExpPerLevel} (tổng {progress.Exp})";
            _expBar.Value = intoLevel;
        }

        _detailStats.Text =
            $"❤️ HP: {Scaled(s.HpMax, scale)}\n" +
            $"⚔️ Công vật lý: {Scaled(s.AtkPhys, scale)}\n" +
            $"🔮 Công phép: {Scaled(s.AtkMag, scale)}\n" +
            $"🛡️ Thủ vật lý: {Scaled(s.DefPhys, scale)}\n" +
            $"✨ Thủ phép: {Scaled(s.DefMag, scale)}\n" +
            $"💨 Tốc độ: {s.Speed}   🎯 Bạo kích: {s.CritRate * 100:0}%\n" +
            $"⚡ Lực chiến: {HeroCard.CalculateCombatPower(hero)} (hệ số cấp/sao x{scale:0.00})";

        var skillLines = new List<string>();
        foreach (var skill in hero.Skills)
        {
            if (skill == null) continue;
            string kind = skill.IsRageSkill ? "🔥 Nộ chiêu" : "⚔️ Chiêu thường";
            skillLines.Add($"{kind}: {skill.SkillName} (x{skill.DamageMultiplier:0.0} ST, {skill.Type})");
        }
        _detailSkills.Text = skillLines.Count > 0
            ? string.Join("\n", skillLines)
            : "Chưa có kỹ năng.";

        if (progress.Star >= 6)
        {
            _starUpBtn.Text = "⭐ ĐÃ ĐẠT SAO TỐI ĐA (6)";
            _starUpBtn.Disabled = true;
        }
        else
        {
            _starUpBtn.Text = $"⬆️ LÊN SAO {progress.Star + 1} — {50 * progress.Star} 💎 Kim Cương";
            _starUpBtn.Disabled = false;
        }
    }

    private void RefreshWallet()
    {
        var manager = GameManager.Instance;
        if (manager == null) return;
        _walletLabel.Text = $"🪙 {manager.Gold:N0} Bạc    💎 {manager.Gems:N0} Kim Cương";
    }

    // ==================== Star up ====================

    private async System.Threading.Tasks.Task OnStarUpPressed()
    {
        if (_selected == null) return;
        var hero = _selected;
        var progress = ProgressOf(hero);
        if (progress.Star >= 6) return;
        int cost = 50 * progress.Star;

        _starUpBtn.Disabled = true;
        var network = NetworkManager.Instance;
        if (network != null && network.IsServerOnline && network.IsAuthenticated)
        {
            var result = await network.StarUpHeroAsync(hero.HeroId);
            if (result != null)
            {
                _statusLabel.Text = $"🎉 {hero.HeroName} lên {result.Star} sao! (-{result.GoldSpent} 💎)";
            }
            else
            {
                _statusLabel.Text = "❌ Lên sao thất bại (không đủ Kim Cương hoặc lỗi server).";
            }
        }
        else
        {
            // Che do offline: tru vi local va luu tien trinh vao GameManager
            var manager = GameManager.Instance;
            if (manager == null || !manager.SpendGems(cost))
            {
                _statusLabel.Text = "❌ Không đủ Kim Cương để lên sao!";
                RefreshDetail();
                return;
            }
            if (!manager.HeroProgressById.TryGetValue(hero.HeroId, out var entry))
            {
                entry = new GameManager.HeroProgress();
                manager.HeroProgressById[hero.HeroId] = entry;
            }
            entry.Star += 1;
            _statusLabel.Text = $"🎉 {hero.HeroName} lên {entry.Star} sao! (-{cost} 💎, offline)";
        }

        RefreshWallet();
        RefreshGrid();
        RefreshDetail();
    }

    // ==================== Helpers ====================

    private static string StarsText(int star) =>
        new string('★', Math.Clamp(star, 0, 6)) + new string('☆', 6 - Math.Clamp(star, 0, 6));

    private static string Scaled(int baseValue, float scale)
    {
        if (scale <= 1.001f) return baseValue.ToString("N0");
        int scaled = (int)(baseValue * scale);
        return $"{scaled:N0} (gốc {baseValue:N0})";
    }

    private static Color RarityColor(HeroData.RarityType rarity) => rarity switch
    {
        HeroData.RarityType.Ur => new Color(1.0f, 0.35f, 0.45f),
        HeroData.RarityType.Ssr => new Color(1.0f, 0.75f, 0.25f),
        HeroData.RarityType.Sr => new Color(0.7f, 0.5f, 1.0f),
        _ => new Color(0.6f, 0.85f, 1.0f)
    };

    private static string PositionText(BattlePosition position) => position switch
    {
        BattlePosition.Front => "Tiền quân",
        BattlePosition.Mid => "Trung quân",
        BattlePosition.Back => "Hậu quân",
        _ => position.ToString()
    };
}
