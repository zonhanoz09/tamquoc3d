using System.Collections.Generic;
using System.Linq;
using Godot;
using TamQuoc3D.Data.Database;
using TamQuoc3D.Data.Resources;

namespace TamQuoc3D.Features.DeckBuilder;

/// <summary>
/// FormationGrid - Quan ly ma tran bo tran 3x3 (7 o active: 1 Tien quan, 3 Trung quan, 3 Hau quan).
/// Ho tro dat tuong, go tuong, hoan doi (swap) 2 o, va xuat du lieu doi hinh.
/// </summary>
[GlobalClass]
public partial class FormationGrid : Control
{
    [Signal]
    public delegate void SlotClickedEventHandler(int row, int col);

    [Signal]
    public delegate void SlotRemoveClickedEventHandler(int row, int col);

    [Signal]
    public delegate void GridChangedEventHandler();

    public const int GridRows = 3;
    public const int GridCols = 3;

    private readonly FormationSlot[,] _slots = new FormationSlot[GridRows, GridCols];
    private readonly Dictionary<(int row, int col), HeroData> _assignedHeroes = new();

    private TextureRect _bgRect;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(640, 480);
        SetupVisuals();
    }

    private void SetupVisuals()
    {
        // 1. Board Background
        _bgRect = new TextureRect
        {
            CustomMinimumSize = new Vector2(640, 480),
            Texture = GD.Load<Texture2D>("res://assets/ui/formation/formation_grid_bg.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(_bgRect);

        // 2. Row Labels
        string[] rowTitles =
        [
            "⚔️ TIỀN QUÂN (Hàng Đầu - 1 Vị Trí)",
            "🛡️ TRUNG QUÂN (Chủ Lực - 3 Vị Trí)",
            "🏹 HẬU QUÂN (Hỗ Trợ & Trị Liệu - 3 Vị Trí)"
        ];

        int[] rowY = [60, 190, 320];
        int[] colX = [100, 272, 444];

        for (int r = 0; r < GridRows; r++)
        {
            var rowHeader = new Label
            {
                Position = new Vector2(100, rowY[r] - 22),
                Size = new Vector2(440, 20),
                Text = rowTitles[r],
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore
            };
            rowHeader.AddThemeFontSizeOverride("font_size", 11);
            rowHeader.AddThemeColorOverride("font_color", new Color(0.95f, 0.85f, 0.5f));
            AddChild(rowHeader);
        }

        // 3. 3x3 Slots (Row 0: center only; Row 1 & 2: top, center, bottom)
        for (int r = 0; r < GridRows; r++)
        {
            for (int c = 0; c < GridCols; c++)
            {
                bool isActive = (r == 0 && c == 1) || (r == 1) || (r == 2);
                string hint = "";
                if (isActive)
                {
                    if (r == 0) hint = "+ Tiền quân";
                    else if (r == 1) hint = c switch
                    {
                        0 => "+ Trung (Trên)",
                        1 => "+ Trung (Giữa)",
                        _ => "+ Trung (Dưới)"
                    };
                    else if (r == 2) hint = c switch
                    {
                        0 => "+ Hậu (Trên)",
                        1 => "+ Hậu (Giữa)",
                        _ => "+ Hậu (Dưới)"
                    };
                }

                var slot = new FormationSlot
                {
                    Position = new Vector2(colX[c], rowY[r])
                };
                AddChild(slot);
                slot.SetupSlot(r, c, isActive, hint);

                int curR = r;
                int curC = c;
                slot.SlotClicked += (row, col) => EmitSignal(SignalName.SlotClicked, row, col);
                slot.RemoveClicked += (row, col) => RemoveHero(row, col);

                _slots[r, c] = slot;
            }
        }
    }

    public bool IsSlotActive(int row, int col)
    {
        if (row < 0 || row >= GridRows || col < 0 || col >= GridCols) return false;
        return _slots[row, col]?.IsActive ?? false;
    }

    public bool IsSlotOccupied(int row, int col)
    {
        if (!IsSlotActive(row, col)) return false;
        return _slots[row, col].IsOccupied;
    }

    public HeroData GetHeroAt(int row, int col)
    {
        return _assignedHeroes.GetValueOrDefault((row, col));
    }

    public FormationSlot GetSlot(int row, int col)
    {
        if (row < 0 || row >= GridRows || col < 0 || col >= GridCols) return null;
        return _slots[row, col];
    }

    public bool AssignHero(HeroData hero, int row, int col)
    {
        if (hero == null || !IsSlotActive(row, col)) return false;

        // Neu tuong da o o khac trong doi hinh, go khoi o cu
        foreach (var kvp in new Dictionary<(int row, int col), HeroData>(_assignedHeroes))
        {
            if (kvp.Value.HeroId == hero.HeroId)
            {
                RemoveHero(kvp.Key.row, kvp.Key.col, false);
            }
        }

        // Toi da 5 tuong xuat tran trong doi hinh
        if (_assignedHeroes.Count >= 5 && !_assignedHeroes.ContainsKey((row, col)))
        {
            return false;
        }

        _assignedHeroes[(row, col)] = hero;
        _slots[row, col].SetHero(hero);
        EmitSignal(SignalName.GridChanged);
        return true;
    }

    public void RemoveHero(int row, int col, bool emitSignal = true)
    {
        if (!IsSlotActive(row, col)) return;

        if (_assignedHeroes.Remove((row, col)))
        {
            _slots[row, col].ClearHero();
            if (emitSignal)
            {
                EmitSignal(SignalName.SlotRemoveClicked, row, col);
                EmitSignal(SignalName.GridChanged);
            }
        }
    }

    public void SwapHeroes(int r1, int c1, int r2, int c2)
    {
        if (!IsSlotActive(r1, c1) || !IsSlotActive(r2, c2)) return;

        var hero1 = GetHeroAt(r1, c1);
        var hero2 = GetHeroAt(r2, c2);

        if (hero1 != null) _slots[r2, c2].SetHero(hero1);
        else _slots[r2, c2].ClearHero();

        if (hero2 != null) _slots[r1, c1].SetHero(hero2);
        else _slots[r1, c1].ClearHero();

        _assignedHeroes.Remove((r1, c1));
        _assignedHeroes.Remove((r2, c2));

        if (hero1 != null) _assignedHeroes[(r2, c2)] = hero1;
        if (hero2 != null) _assignedHeroes[(r1, c1)] = hero2;

        EmitSignal(SignalName.GridChanged);
    }

    public void ClearAll()
    {
        _assignedHeroes.Clear();
        for (int r = 0; r < GridRows; r++)
        {
            for (int c = 0; c < GridCols; c++)
            {
                if (_slots[r, c] != null && _slots[r, c].IsActive)
                {
                    _slots[r, c].ClearHero();
                    _slots[r, c].SetHighlight(false);
                }
            }
        }
        EmitSignal(SignalName.GridChanged);
    }

    public void SetHighlightSlot(int row, int col, bool on)
    {
        var slot = GetSlot(row, col);
        slot?.SetHighlight(on);
    }

    public void ClearAllHighlights()
    {
        for (int r = 0; r < GridRows; r++)
        {
            for (int c = 0; c < GridCols; c++)
            {
                _slots[r, c]?.SetHighlight(false);
            }
        }
    }

    public List<FormationEntry> GetFormation()
    {
        var list = new List<FormationEntry>();
        foreach (var kvp in _assignedHeroes)
        {
            list.Add(new FormationEntry(kvp.Value.HeroId, kvp.Key.row, kvp.Key.col));
        }
        return list;
    }

    public int GetAssignedCount() => _assignedHeroes.Count;

    public Dictionary<(int row, int col), HeroData> GetAllAssigned()
    {
        return new Dictionary<(int row, int col), HeroData>(_assignedHeroes);
    }
}
