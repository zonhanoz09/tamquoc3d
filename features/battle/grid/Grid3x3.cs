using System.Collections.Generic;
using Godot;
using TamQuoc3D.Core.Constants;

namespace TamQuoc3D.Features.Battle.Grid;

/// <summary>
/// Grid3x3 - Quan ly ma tran vi tri 3x3 chuan doi xung chien tran Tam Quoc.
/// Quy tac bat bien: Tien quan chi co DUY NHAT 1 vi tri o chinh giua (Row 0, Col 1).
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
            // 1. Tien quan (Front): Luat tran hinh chi co duy nhat 1 vi tri o chinh giua (Col 1)
            var frontPos = CalculateSlotPosition(team, BattlePosition.Front, BattleLane.Center);
            _slots[(team, (int)BattlePosition.Front, (int)BattleLane.Center)] =
                new PedestalSlot(team, (int)BattlePosition.Front, (int)BattleLane.Center, frontPos);

            // 2. Trung quan (Row 1) & Hau quan (Row 2): 3 duong Top, Center, Bottom
            for (int col = 0; col < 3; col++)
            {
                var midPos = CalculateSlotPosition(team, BattlePosition.Mid, (BattleLane)col);
                _slots[(team, (int)BattlePosition.Mid, col)] =
                    new PedestalSlot(team, (int)BattlePosition.Mid, col, midPos);

                var backPos = CalculateSlotPosition(team, BattlePosition.Back, (BattleLane)col);
                _slots[(team, (int)BattlePosition.Back, col)] =
                    new PedestalSlot(team, (int)BattlePosition.Back, col, backPos);
            }
        }
    }

    public static Vector2 CalculateSlotPosition(int team, int row, int col)
    {
        return CalculateSlotPosition(team, (BattlePosition)row, (BattleLane)col);
    }

    public static Vector2 CalculateSlotPosition(int team, BattlePosition pos, BattleLane lane)
    {
        // 1. Tien quan (Front): CHI CO 1 VI TRI O CHINH GIUA
        if (pos == BattlePosition.Front)
        {
            float frontX = team == 0 ? 490.0f : 790.0f;
            return new Vector2(frontX, 505.0f);
        }

        // 2. Trung quan (Mid) & Hau quan (Back): phan bo theo cac duong
        float y = lane switch
        {
            BattleLane.Top => 425.0f,
            BattleLane.Bottom => 585.0f,
            _ => 505.0f // Center
        };

        float x = team == 0
            ? (pos == BattlePosition.Mid ? 350.0f : 210.0f)
            : (pos == BattlePosition.Mid ? 930.0f : 1070.0f);

        return new Vector2(x, y);
    }

    public PedestalSlot GetSlot(int team, int row, int col)
    {
        // Neu truy van Tien quan bat ke cot nao deu quy ve vi tri chinh giua (Col 1)
        if (row == (int)BattlePosition.Front)
        {
            col = (int)BattleLane.Center;
        }
        return _slots.GetValueOrDefault((team, row, col));
    }

    public PedestalSlot GetSlot(int team, BattlePosition pos, BattleLane lane)
    {
        return GetSlot(team, (int)pos, (int)lane);
    }
}
