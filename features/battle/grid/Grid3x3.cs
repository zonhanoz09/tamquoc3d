using System.Collections.Generic;
using Godot;

namespace TamQuoc3D.Features.Battle.Grid;

/// <summary>
/// Grid3x3 - Quan ly ma tran vi tri 3x3 chuan doi xung chien tran Tam Quoc.
/// </summary>
public class Grid3x3
{
    private readonly Dictionary<(int team, int row, int col), PedestalSlot> _slots = new();

    public Grid3x3()
    {
        InitializeSlots();
    }

    private void InitializeSlots()
    {
        for (int team = 0; team <= 1; team++)
        {
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    var pos = CalculateSlotPosition(team, row, col);
                    _slots[(team, row, col)] = new PedestalSlot(team, row, col, pos);
                }
            }
        }
    }

    public static Vector2 CalculateSlotPosition(int team, int row, int col)
    {
        if (team == 0)
        {
            // Player
            return row switch
            {
                0 => new Vector2(520, 520),
                1 => col switch
                {
                    0 => new Vector2(350, 410),
                    1 => new Vector2(350, 520),
                    _ => new Vector2(350, 630)
                },
                _ => col switch
                {
                    0 => new Vector2(180, 410),
                    1 => new Vector2(180, 520),
                    _ => new Vector2(180, 630)
                }
            };
        }
        else
        {
            // Enemy
            return row switch
            {
                0 => new Vector2(760, 520),
                1 => col switch
                {
                    0 => new Vector2(930, 410),
                    1 => new Vector2(930, 520),
                    _ => new Vector2(930, 630)
                },
                _ => col switch
                {
                    0 => new Vector2(1100, 410),
                    1 => new Vector2(1100, 520),
                    _ => new Vector2(1100, 630)
                }
            };
        }
    }

    public PedestalSlot GetSlot(int team, int row, int col)
    {
        return _slots.GetValueOrDefault((team, row, col));
    }
}
