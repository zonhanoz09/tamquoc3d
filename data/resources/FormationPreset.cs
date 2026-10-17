using System;
using Godot;

namespace TamQuoc3D.Data.Resources;

/// <summary>
/// FormationEntry - Dai dien cho mot vi tri tuong trong doi hinh 3x3.
/// </summary>
[GlobalClass]
public partial class FormationEntry : Resource
{
    [Export] public string HeroId { get; set; } = string.Empty;
    [Export] public int Row { get; set; } = 0;
    [Export] public int Col { get; set; } = 0;

    public FormationEntry() { }

    public FormationEntry(string heroId, int row, int col)
    {
        HeroId = heroId;
        Row = row;
        Col = col;
    }
}

/// <summary>
/// FormationPreset - Dinh nghia mot preset doi hinh da luu gom ten, danh sach vi tri va moc thoi gian.
/// </summary>
[GlobalClass]
public partial class FormationPreset : Resource
{
    [Export] public string PresetName { get; set; } = "Đội hình 1";
    [Export] public Godot.Collections.Array<FormationEntry> Entries { get; set; } = [];
    [Export] public long SavedAtUnix { get; set; } = 0;

    public DateTime SavedAt
    {
        get => SavedAtUnix > 0 ? DateTimeOffset.FromUnixTimeSeconds(SavedAtUnix).LocalDateTime : DateTime.Now;
        set => SavedAtUnix = new DateTimeOffset(value).ToUnixTimeSeconds();
    }
}
