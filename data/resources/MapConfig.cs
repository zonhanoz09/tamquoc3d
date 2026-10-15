using Godot;

namespace TamQuoc3D.Data.Resources;

/// <summary>
/// MapConfig - Dinh nghia cau hinh san dau / ban do chien tran (.tres).
/// </summary>
[GlobalClass]
public partial class MapConfig : Resource
{
    [Export] public string MapId { get; set; } = string.Empty;
    [Export] public string MapName { get; set; } = string.Empty;
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = string.Empty;

    [Export] public string BackgroundPath { get; set; } = "res://assets/environments/battlefields/xich_bich/battlefield.png";
    [Export] public string BgmPath { get; set; } = string.Empty;

    [Export] public int GridRows { get; set; } = 3;
    [Export] public int GridCols { get; set; } = 3;
}
