using Godot;

namespace TamQuoc3D.Features.Battle.Grid;

/// <summary>
/// CellNode - Node hien thi o luoi 3x3 tren san dau.
/// </summary>
[GlobalClass]
public partial class CellNode : Node2D
{
    public int Team { get; set; }
    public int Row { get; set; }
    public int Col { get; set; }

    public Sprite2D PedestalSprite { get; private set; }

    public void Setup(int team, int row, int col, Vector2 pos)
    {
        Team = team;
        Row = row;
        Col = col;
        Position = pos;

        PedestalSprite = new Sprite2D();
        string pName = team == 0 ? "pedestal_player" : "pedestal_enemy";
        var tex = GD.Load<Texture2D>($"res://assets/ui/shared/{pName}.png");
        if (tex != null)
        {
            PedestalSprite.Texture = tex;
        }
        PedestalSprite.Scale = new Vector2(1.15f, 1.15f);
        PedestalSprite.ZIndex = 0;
        AddChild(PedestalSprite);
    }
}
