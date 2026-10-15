using System.Collections.Generic;
using Godot;
using TamQuoc3D.Data.Resources;

namespace TamQuoc3D.Features.DeckBuilder;

/// <summary>
/// DeckBuilderController - Quan ly giao dien sap xep doi hinh vi tri 3x3 va quan ly tuong du bi.
/// </summary>
[GlobalClass]
public partial class DeckBuilderController : Node
{
    [Signal]
    public delegate void FormationChangedEventHandler();

    // Luu tru vi tri 5 tuong xuat tran (Row 0..2, Col 0..2)
    public Dictionary<(int row, int col), HeroData> FormationGrid { get; } = new();
    public List<HeroData> ReserveHeroes { get; } = [];

    public override void _Ready()
    {
        GD.Print("🎴 DeckBuilderController khoi tao thanh cong.");
    }

    public bool PlaceHero(HeroData hero, int row, int col)
    {
        if (FormationGrid.Count >= 5 && !FormationGrid.ContainsKey((row, col)))
        {
            return false; // Toi da 5 tuong xuat tran
        }

        FormationGrid[(row, col)] = hero;
        EmitSignal(SignalName.FormationChanged);
        return true;
    }

    public void RemoveHero(int row, int col)
    {
        if (FormationGrid.Remove((row, col)))
        {
            EmitSignal(SignalName.FormationChanged);
        }
    }
}
