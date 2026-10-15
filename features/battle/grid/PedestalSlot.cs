using Godot;

namespace TamQuoc3D.Features.Battle.Grid;

/// <summary>
/// PedestalSlot - Mo ta mot vi tri o tren luoi doi hinh chien thuat 3x3.
/// </summary>
public class PedestalSlot
{
    public int Team { get; set; }       // 0 = Player, 1 = Enemy
    public int Row { get; set; }        // 0 = Tien quan, 1 = Trung quan, 2 = Hau quan
    public int Col { get; set; }        // 0 = Tren, 1 = Giua, 2 = Duoi
    public Vector2 Position { get; set; }
    public GodotObject Occupant { get; set; }

    public bool IsOccupied => Occupant != null;

    public PedestalSlot(int team, int row, int col, Vector2 pos)
    {
        Team = team;
        Row = row;
        Col = col;
        Position = pos;
    }
}
