namespace TamQuoc3D.Core.Constants;

/// <summary>
/// Vị trí hàng đứng trong trận đấu 3x3 (Tiền quân, Trung quân, Hậu quân).
/// </summary>
public enum BattlePosition
{
    Front = 0, // Tiền quân (Hàng trước - Chịu đòn, khống chế, phòng thủ)
    Mid = 1,   // Trung quân (Hàng giữa - Sát thương chủ lực, bạo kích)
    Back = 2   // Hậu quân (Hàng sau - Hỗ trợ, trị liệu, phép thuật)
}

/// <summary>
/// Vị trí cột/đường trong ma trận 3x3 (Trên, Giữa, Dưới).
/// </summary>
public enum BattleLane
{
    Top = 0,    // Đường trên (Row top)
    Center = 1, // Đường giữa (Row center)
    Bottom = 2  // Đường dưới (Row bottom)
}

/// <summary>
/// Định danh 9 vị trí ô cờ 3x3 cụ thể trên sàn đấu.
/// </summary>
public enum GridPosition
{
    FrontTop = 0,
    FrontCenter = 1,
    FrontBottom = 2,
    MidTop = 3,
    MidCenter = 4,
    MidBottom = 5,
    BackTop = 6,
    BackCenter = 7,
    BackBottom = 8
}
