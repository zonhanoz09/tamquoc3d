using System.Collections.Generic;
using Godot;
using TamQuoc3D.Core.Constants;

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
                    var pos = CalculateSlotPosition(team, (BattlePosition)row, (BattleLane)col);
                    _slots[(team, row, col)] = new PedestalSlot(team, row, col, pos);
                }
            }
        }
    }

    public static Vector2 CalculateSlotPosition(int team, int row, int col)
    {
        return CalculateSlotPosition(team, (BattlePosition)row, (BattleLane)col);
    }

    public static Vector2 CalculateSlotPosition(int team, BattlePosition pos, BattleLane lane)
    {
        // Cac duong ngang (Lanes) can bang voi khoang cach 80px:
        // Top = 425, Center = 505, Bottom = 585
        float y = lane switch
        {
            BattleLane.Top => 425.0f,
            BattleLane.Center => 505.0f,
            _ => 585.0f
        };

        // Cac hang doc (Rows) doi xung hoan hao qua tam san dau X = 640:
        // - Tien quan (Front): cach tam 150px (vung giao chien o giua = 300px)
        // - Trung quan (Mid): cach tam 290px (lui sau Tien quan 140px)
        // - Hau quan (Back): cach tam 430px (lui sau Trung quan 140px, cach mep man hinh 210px)
        float x = team == 0
            ? pos switch
            {
                BattlePosition.Front => 490.0f,
                BattlePosition.Mid => 350.0f,
                _ => 210.0f
            }
            : pos switch
            {
                BattlePosition.Front => 790.0f,
                BattlePosition.Mid => 930.0f,
                _ => 1070.0f
            };

        return new Vector2(x, y);
    }

    public PedestalSlot GetSlot(int team, int row, int col)
    {
        return _slots.GetValueOrDefault((team, row, col));
    }

    public PedestalSlot GetSlot(int team, BattlePosition pos, BattleLane lane)
    {
        return GetSlot(team, (int)pos, (int)lane);
    }
}
