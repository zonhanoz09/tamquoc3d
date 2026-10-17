using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TamQuoc3D.Core.Autoload;
using TamQuoc3D.Core.Constants;
using TamQuoc3D.Core.Navigation;
using TamQuoc3D.Core.Network;
using TamQuoc3D.Data.Database;
using TamQuoc3D.Data.Resources;

namespace TamQuoc3D.Features.DeckBuilder;

/// <summary>
/// DeckBuilderController - Dieu khien man hinh Chon & Bo Tran (Phase 1).
/// Quan ly danh sach 15 tuong, luoi bo tran 3x3 (5 o active), Preset, Auto-Deploy, va chuyen vao BattleScene.
/// </summary>
[GlobalClass]
public partial class DeckBuilderController : Control
{
    [Signal]
    public delegate void FormationConfirmedEventHandler(Godot.Collections.Array formation);

    [Signal]
    public delegate void FormationChangedEventHandler();

    // UI Nodes
    private HeroFilter _filter;
    private GridContainer _heroCardGrid;
    private FormationGrid _formationGrid;
    private Label _totalPowerLabel;
    private Label _teamCountLabel;
    private Button _startBattleBtn;
    private Button _autoDeployBtn;
    private Button _clearAllBtn;
    private Button _backBtn;
    private Button _savePresetBtn;
    private Button[] _presetBtns = new Button[3];

    // State
    private readonly List<HeroData> _allHeroes = [];
    private readonly Dictionary<string, HeroCard> _heroCards = new();
    private string _selectedHeroId = string.Empty;
    private (int row, int col)? _selectedSlotForSwap = null;
    private int _activePresetIndex = 0;

    public override void _Ready()
    {
        BuildUiLayout();
        LoadHeroes();
        LoadInitialFormation();
        UpdateUiState();
    }

    private void BuildUiLayout()
    {
        // Screen background (Dark antique gradient/slate)
        var bg = new ColorRect
        {
            CustomMinimumSize = new Vector2(1280, 720),
            Color = new Color(0.07f, 0.06f, 0.09f, 1.0f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(bg);

        // Subtle textured background overlay
        var bgBoard = new TextureRect
        {
            CustomMinimumSize = new Vector2(1280, 720),
            Texture = GD.Load<Texture2D>("res://assets/environments/battlefields/dau_truong/battlefield.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            Modulate = new Color(1, 1, 1, 0.15f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(bgBoard);

        // 1. TOP BAR (Height: 56px)
        var topBar = new PanelContainer
        {
            Position = new Vector2(0, 0),
            Size = new Vector2(1280, 56)
        };
        var sbTop = new StyleBoxFlat
        {
            BgColor = new Color(0.11f, 0.09f, 0.15f, 0.95f),
            BorderColor = new Color(0.75f, 0.60f, 0.25f, 1.0f)
        };
        sbTop.BorderWidthBottom = 2;
        topBar.AddThemeStyleboxOverride("panel", sbTop);
        AddChild(topBar);

        var topHbox = new HBoxContainer
        {
            Position = new Vector2(16, 8),
            Size = new Vector2(1248, 40),
            Alignment = BoxContainer.AlignmentMode.Begin
        };
        topHbox.AddThemeConstantOverride("separation", 16);
        topBar.AddChild(topHbox);

        _backBtn = new Button
        {
            Text = "⬅ Quay Lại",
            CustomMinimumSize = new Vector2(110, 36)
        };
        _backBtn.Pressed += OnBackPressed;
        topHbox.AddChild(_backBtn);

        var titleLabel = new Label
        {
            Text = "⚔️ BỐ TRÍ TRẬN HÌNH",
            VerticalAlignment = VerticalAlignment.Center
        };
        titleLabel.AddThemeFontSizeOverride("font_size", 18);
        titleLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.88f, 0.35f));
        topHbox.AddChild(titleLabel);

        var topSpacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        topHbox.AddChild(topSpacer);

        // Preset Slots
        var presetLabel = new Label { Text = "Đội hình:", VerticalAlignment = VerticalAlignment.Center };
        presetLabel.AddThemeFontSizeOverride("font_size", 12);
        presetLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
        topHbox.AddChild(presetLabel);

        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            var btn = new Button
            {
                Text = $"#{i + 1}",
                CustomMinimumSize = new Vector2(40, 36),
                ToggleMode = true,
                ButtonPressed = i == 0
            };
            btn.Pressed += () => OnPresetSelected(idx);
            _presetBtns[i] = btn;
            topHbox.AddChild(btn);
        }

        _savePresetBtn = new Button
        {
            Text = "💾 Lưu",
            CustomMinimumSize = new Vector2(70, 36),
            TooltipText = "Lưu đội hình vào preset hiện tại"
        };
        _savePresetBtn.Pressed += OnSavePresetPressed;
        topHbox.AddChild(_savePresetBtn);

        // 2. MAIN SPLIT CONTAINER (Left: 520px, Right: 760px)
        // LEFT PANEL: Hero Roster & Filters
        var leftPanel = new PanelContainer
        {
            Position = new Vector2(16, 68),
            Size = new Vector2(540, 580)
        };
        var sbLeft = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.07f, 0.11f, 0.85f),
            BorderColor = new Color(0.35f, 0.30f, 0.45f, 0.8f)
        };
        sbLeft.SetBorderWidthAll(1);
        sbLeft.SetCornerRadiusAll(4);
        leftPanel.AddThemeStyleboxOverride("panel", sbLeft);
        AddChild(leftPanel);

        var leftVbox = new VBoxContainer
        {
            Position = new Vector2(8, 8),
            Size = new Vector2(524, 564)
        };
        leftVbox.AddThemeConstantOverride("separation", 10);
        leftPanel.AddChild(leftVbox);

        // Filter Bar
        _filter = new HeroFilter();
        _filter.FilterChanged += OnFilterChanged;
        leftVbox.AddChild(_filter);

        // Scrollable Hero Grid
        var scrollContainer = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        leftVbox.AddChild(scrollContainer);

        _heroCardGrid = new GridContainer
        {
            Columns = 5,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _heroCardGrid.AddThemeConstantOverride("h_separation", 14);
        _heroCardGrid.AddThemeConstantOverride("v_separation", 14);
        scrollContainer.AddChild(_heroCardGrid);

        // RIGHT PANEL: Formation Board & Actions
        var rightPanel = new PanelContainer
        {
            Position = new Vector2(570, 68),
            Size = new Vector2(694, 580)
        };
        var sbRight = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.07f, 0.11f, 0.85f),
            BorderColor = new Color(0.35f, 0.30f, 0.45f, 0.8f)
        };
        sbRight.SetBorderWidthAll(1);
        sbRight.SetCornerRadiusAll(4);
        rightPanel.AddThemeStyleboxOverride("panel", sbRight);
        AddChild(rightPanel);

        var rightVbox = new VBoxContainer
        {
            Position = new Vector2(16, 12),
            Size = new Vector2(662, 556)
        };
        rightVbox.AddThemeConstantOverride("separation", 12);
        rightPanel.AddChild(rightVbox);

        // Formation Actions Header
        var actHbox = new HBoxContainer();
        actHbox.AddThemeConstantOverride("separation", 16);
        rightVbox.AddChild(actHbox);

        _teamCountLabel = new Label
        {
            Text = "Quân số: 0/5 tướng",
            VerticalAlignment = VerticalAlignment.Center
        };
        _teamCountLabel.AddThemeFontSizeOverride("font_size", 13);
        _teamCountLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.6f));
        actHbox.AddChild(_teamCountLabel);

        var actSpacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        actHbox.AddChild(actSpacer);

        _autoDeployBtn = new Button
        {
            Text = "🤖 Tự Động Bố Trận",
            CustomMinimumSize = new Vector2(160, 36)
        };
        _autoDeployBtn.Pressed += OnAutoDeployPressed;
        actHbox.AddChild(_autoDeployBtn);

        _clearAllBtn = new Button
        {
            Text = "🗑️ Xóa Tất Cả",
            CustomMinimumSize = new Vector2(120, 36)
        };
        _clearAllBtn.Pressed += OnClearAllPressed;
        actHbox.AddChild(_clearAllBtn);

        // 3x3 Formation Grid Container (Centered)
        var gridCenter = new CenterContainer();
        rightVbox.AddChild(gridCenter);

        _formationGrid = new FormationGrid();
        _formationGrid.SlotClicked += OnSlotClicked;
        _formationGrid.SlotRemoveClicked += OnSlotRemoveClicked;
        _formationGrid.GridChanged += OnGridChanged;
        gridCenter.AddChild(_formationGrid);

        // 3. BOTTOM BAR (Height: 60px)
        var bottomBar = new PanelContainer
        {
            Position = new Vector2(0, 656),
            Size = new Vector2(1280, 64)
        };
        var sbBot = new StyleBoxFlat
        {
            BgColor = new Color(0.10f, 0.08f, 0.14f, 0.95f),
            BorderColor = new Color(0.75f, 0.60f, 0.25f, 1.0f)
        };
        sbBot.BorderWidthTop = 2;
        bottomBar.AddThemeStyleboxOverride("panel", sbBot);
        AddChild(bottomBar);

        var botHbox = new HBoxContainer
        {
            Position = new Vector2(24, 8),
            Size = new Vector2(1232, 48),
            Alignment = BoxContainer.AlignmentMode.End
        };
        botHbox.AddThemeConstantOverride("separation", 24);
        bottomBar.AddChild(botHbox);

        var hintLbl = new Label
        {
            Text = "💡 Chọn thẻ tướng bên trái ➜ Nhấp vào ô trống trên bảng để xếp trận. Nhấp 2 ô để hoán đổi vị trí.",
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        hintLbl.AddThemeFontSizeOverride("font_size", 11);
        hintLbl.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.75f));
        botHbox.AddChild(hintLbl);

        _totalPowerLabel = new Label
        {
            Text = "⚔️ TỔNG LỰC CHIẾN: 0",
            VerticalAlignment = VerticalAlignment.Center
        };
        _totalPowerLabel.AddThemeFontSizeOverride("font_size", 16);
        _totalPowerLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.85f, 0.25f));
        botHbox.AddChild(_totalPowerLabel);

        _startBattleBtn = new Button
        {
            Text = "⚔️ XUẤT TRẬN",
            CustomMinimumSize = new Vector2(160, 44),
            Disabled = true
        };
        _startBattleBtn.AddThemeFontSizeOverride("font_size", 14);
        var sbStart = new StyleBoxFlat
        {
            BgColor = new Color(0.7f, 0.2f, 0.15f, 1.0f),
            BorderColor = new Color(1.0f, 0.85f, 0.3f, 1.0f)
        };
        sbStart.SetBorderWidthAll(2);
        sbStart.SetCornerRadiusAll(4);
        _startBattleBtn.AddThemeStyleboxOverride("normal", sbStart);
        _startBattleBtn.Pressed += OnStartBattlePressed;
        botHbox.AddChild(_startBattleBtn);
    }

    private void LoadHeroes()
    {
        _allHeroes.Clear();

        // 1. Get heroes from GameManager or HeroDatabase
        if (GameManager.Instance != null && GameManager.Instance.OwnedHeroes.Count > 0)
        {
            _allHeroes.AddRange(GameManager.Instance.OwnedHeroes);
        }
        else if (HeroDatabase.Instance != null)
        {
            _allHeroes.AddRange(HeroDatabase.Instance.GetAllHeroes());
        }
        else
        {
            var db = new HeroDatabase();
            db.GenerateDefaultHeroDatabase();
            _allHeroes.AddRange(db.GetAllHeroes());
        }

        RenderHeroCards();
    }

    private void RenderHeroCards()
    {
        foreach (var c in _heroCardGrid.GetChildren())
        {
            c.QueueFree();
        }
        _heroCards.Clear();

        var filtered = _filter != null ? _filter.ApplyFilter(_allHeroes) : _allHeroes;

        foreach (var hero in filtered)
        {
            var card = new HeroCard();
            _heroCardGrid.AddChild(card);
            card.SetData(hero);
            card.CardClicked += OnHeroCardClicked;
            _heroCards[hero.HeroId] = card;
        }

        UpdateHeroCardAvailability();
    }

    private void LoadInitialFormation()
    {
        // 1. Check if GameManager has a current formation
        if (GameManager.Instance != null && GameManager.Instance.CurrentFormation.Count > 0)
        {
            foreach (var entry in GameManager.Instance.CurrentFormation)
            {
                var hero = _allHeroes.FirstOrDefault(h => h.HeroId == entry.HeroId);
                if (hero != null)
                {
                    _formationGrid.AssignHero(hero, entry.Row, entry.Col);
                }
            }
        }
        else
        {
            // Default formation: Top 5 heroes auto-deployed
            AutoDeploy();
        }
    }

    private void OnHeroCardClicked(string heroId)
    {
        // Cancel swap selection if active
        if (_selectedSlotForSwap.HasValue)
        {
            _formationGrid.SetHighlightSlot(_selectedSlotForSwap.Value.row, _selectedSlotForSwap.Value.col, false);
            _selectedSlotForSwap = null;
        }

        if (_selectedHeroId == heroId)
        {
            // Deselect card
            _selectedHeroId = string.Empty;
        }
        else
        {
            _selectedHeroId = heroId;
        }

        // Update card selection outlines
        foreach (var kvp in _heroCards)
        {
            kvp.Value.SetSelected(kvp.Key == _selectedHeroId);
        }
    }

    private void OnSlotClicked(int row, int col)
    {
        if (!_formationGrid.IsSlotActive(row, col)) return;

        // Case A: A hero card is currently selected -> Place into slot
        if (!string.IsNullOrEmpty(_selectedHeroId))
        {
            var hero = _allHeroes.FirstOrDefault(h => h.HeroId == _selectedHeroId);
            if (hero != null)
            {
                _formationGrid.AssignHero(hero, row, col);
                EventBus.Instance?.EmitSignal(EventBus.SignalName.HeroAssignedToSlot, hero.HeroId, row, col);
            }

            // Deselect card
            _selectedHeroId = string.Empty;
            foreach (var kvp in _heroCards) kvp.Value.SetSelected(false);

            UpdateUiState();
            return;
        }

        // Case B: No card selected -> Swap or Move logic
        if (_selectedSlotForSwap.HasValue)
        {
            var (prevR, prevC) = _selectedSlotForSwap.Value;

            if (prevR == row && prevC == col)
            {
                // Clicked same slot -> cancel selection
                _formationGrid.SetHighlightSlot(row, col, false);
                _selectedSlotForSwap = null;
            }
            else
            {
                // Clicked different active slot -> swap heroes
                _formationGrid.SwapHeroes(prevR, prevC, row, col);
                _formationGrid.SetHighlightSlot(prevR, prevC, false);
                _selectedSlotForSwap = null;
                UpdateUiState();
            }
        }
        else
        {
            // Select slot if it has a hero
            if (_formationGrid.IsSlotOccupied(row, col))
            {
                _selectedSlotForSwap = (row, col);
                _formationGrid.SetHighlightSlot(row, col, true);
            }
        }
    }

    private void OnSlotRemoveClicked(int row, int col)
    {
        if (_selectedSlotForSwap.HasValue && _selectedSlotForSwap.Value == (row, col))
        {
            _formationGrid.SetHighlightSlot(row, col, false);
            _selectedSlotForSwap = null;
        }

        EventBus.Instance?.EmitSignal(EventBus.SignalName.HeroRemovedFromSlot, row, col);
        UpdateUiState();
    }

    private void OnGridChanged()
    {
        UpdateUiState();
        EmitSignal(SignalName.FormationChanged);
    }

    private void OnFilterChanged()
    {
        RenderHeroCards();
    }

    public void AutoDeploy()
    {
        _formationGrid.ClearAll();

        // Separate heroes by position preference and power
        var available = _allHeroes.OrderByDescending(HeroCard.CalculateCombatPower).ToList();

        // 1. Tien quan (row 0, col 1): Highest ATK Front hero or highest ATK overall
        var frontHero = available.FirstOrDefault(h => h.Position == BattlePosition.Front)
                     ?? available.OrderByDescending(h => h.BaseStats?.AtkPhys ?? 0).FirstOrDefault();

        if (frontHero != null)
        {
            _formationGrid.AssignHero(frontHero, 0, 1);
            available.Remove(frontHero);
        }

        // 2. Trung quan (row 1, col 0 & 2): 2 highest DEF/HP or Mid heroes
        var midHeroes = available.Where(h => h.Position == BattlePosition.Mid)
                                 .OrderByDescending(h => (h.BaseStats?.DefPhys ?? 0) + (h.BaseStats?.HpMax ?? 0) / 10)
                                 .Take(2).ToList();

        if (midHeroes.Count < 2)
        {
            var extras = available.Except(midHeroes).Take(2 - midHeroes.Count);
            midHeroes.AddRange(extras);
        }

        if (midHeroes.Count > 0)
        {
            _formationGrid.AssignHero(midHeroes[0], 1, 0);
            available.Remove(midHeroes[0]);
        }
        if (midHeroes.Count > 1)
        {
            _formationGrid.AssignHero(midHeroes[1], 1, 2);
            available.Remove(midHeroes[1]);
        }

        // 3. Hau quan (row 2, col 0 & 2): 2 remaining support/ranged/magic or top remaining
        var backHeroes = available.Where(h => h.Position == BattlePosition.Back)
                                  .OrderByDescending(HeroCard.CalculateCombatPower)
                                  .Take(2).ToList();

        if (backHeroes.Count < 2)
        {
            var extras = available.Except(backHeroes).Take(2 - backHeroes.Count);
            backHeroes.AddRange(extras);
        }

        if (backHeroes.Count > 0)
        {
            _formationGrid.AssignHero(backHeroes[0], 2, 0);
            available.Remove(backHeroes[0]);
        }
        if (backHeroes.Count > 1)
        {
            _formationGrid.AssignHero(backHeroes[1], 2, 2);
            available.Remove(backHeroes[1]);
        }

        EventBus.Instance?.EmitSignal(EventBus.SignalName.AutoDeployRequested);
        UpdateUiState();
    }

    private void OnAutoDeployPressed()
    {
        AutoDeploy();
    }

    private void OnClearAllPressed()
    {
        _formationGrid.ClearAll();
        _selectedSlotForSwap = null;
        _selectedHeroId = string.Empty;
        UpdateUiState();
    }

    private void OnPresetSelected(int index)
    {
        _activePresetIndex = index;
        for (int i = 0; i < _presetBtns.Length; i++)
        {
            _presetBtns[i].ButtonPressed = i == index;
        }

        if (GameManager.Instance != null && index < GameManager.Instance.SavedPresets.Count)
        {
            var preset = GameManager.Instance.SavedPresets[index];
            if (preset.Entries.Count > 0)
            {
                _formationGrid.ClearAll();
                foreach (var entry in preset.Entries)
                {
                    var h = _allHeroes.FirstOrDefault(x => x.HeroId == entry.HeroId);
                    if (h != null) _formationGrid.AssignHero(h, entry.Row, entry.Col);
                }
                EventBus.Instance?.EmitSignal(EventBus.SignalName.FormationPresetLoaded, index);
                UpdateUiState();
            }
        }
    }

    private void OnSavePresetPressed()
    {
        if (GameManager.Instance == null) return;

        while (GameManager.Instance.SavedPresets.Count <= _activePresetIndex)
        {
            GameManager.Instance.SavedPresets.Add(new FormationPreset());
        }

        var preset = GameManager.Instance.SavedPresets[_activePresetIndex];
        preset.PresetName = $"Đội hình {_activePresetIndex + 1}";
        preset.Entries.Clear();

        foreach (var entry in _formationGrid.GetFormation())
        {
            preset.Entries.Add(entry);
        }
        preset.SavedAt = DateTime.Now;

        EventBus.Instance?.EmitSignal(EventBus.SignalName.FormationPresetSaved, _activePresetIndex, preset.PresetName);
        GD.Print($"💾 Đã lưu Đội hình {_activePresetIndex + 1} ({preset.Entries.Count} tướng)");
    }

    private void UpdateUiState()
    {
        UpdateHeroCardAvailability();
        UpdateTotalPower();
        UpdateStartButton();
    }

    private void UpdateHeroCardAvailability()
    {
        var assigned = _formationGrid.GetAllAssigned();
        var assignedIds = new HashSet<string>(assigned.Values.Select(h => h.HeroId));

        foreach (var kvp in _heroCards)
        {
            bool inFormation = assignedIds.Contains(kvp.Key);
            kvp.Value.SetInFormation(inFormation);
            kvp.Value.SetSelected(kvp.Key == _selectedHeroId);
        }
    }

    private void UpdateTotalPower()
    {
        var assigned = _formationGrid.GetAllAssigned();
        int totalPower = assigned.Values.Sum(HeroCard.CalculateCombatPower);
        _totalPowerLabel.Text = $"⚔️ TỔNG LỰC CHIẾN: {totalPower:N0}";
        _teamCountLabel.Text = $"Quân số: {assigned.Count}/5 tướng";
    }

    private void UpdateStartButton()
    {
        int count = _formationGrid.GetAssignedCount();
        _startBattleBtn.Disabled = count == 0;
        _startBattleBtn.Text = count >= 5 ? "⚔️ XUẤT TRẬN" : $"⚔️ XUẤT TRẬN ({count}/5)";
    }

    private void OnStartBattlePressed()
    {
        var formation = _formationGrid.GetFormation();
        if (formation.Count == 0) return;

        var array = new Godot.Collections.Array();
        foreach (var entry in formation)
        {
            array.Add(entry);
        }

        EmitSignal(SignalName.FormationConfirmed, array);
        EventBus.Instance?.EmitSignal(EventBus.SignalName.FormationConfirmed, array);

        _ = NetworkManager.Instance?.SaveFormationFromEntriesAsync("PVE_DEFAULT", formation);
        SceneNavigator.GoToBattle(formation);
    }

    private void OnBackPressed()
    {
        SceneNavigator.GoBack();
    }
}
