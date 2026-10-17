using System.Collections.Generic;
using Godot;

namespace TamQuoc3D.Data.Resources;

public enum StageType
{
    Normal,
    Boss,
    Resource,
    Elite
}

/// <summary>
/// StageData - Dinh nghia thong tin tai nguyen cua mot ai PvE (.tres).
/// </summary>
[GlobalClass]
public partial class StageData : Resource
{
    [Export] public int ChapterId { get; set; } = 1;
    [Export] public int StageId { get; set; } = 1;
    [Export] public string StageKey { get; set; } = "1-1";
    [Export] public string StageName { get; set; } = "Tuần Tra Biên Giới";
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = string.Empty;
    [Export] public StageType Type { get; set; } = StageType.Normal;
    [Export] public int RecommendedPower { get; set; } = 1200;
    [Export] public int StaminaCost { get; set; } = 3;

    [Export] public Godot.Collections.Array<EnemyEntry> EnemyFormation { get; set; } = [];

    // Phan thuong lan dau
    [Export] public int FirstClearGold { get; set; } = 500;
    [Export] public int FirstClearDiamond { get; set; } = 50;
    [Export] public int FirstClearExp { get; set; } = 200;
    [Export] public Godot.Collections.Array<string> FirstClearItems { get; set; } = [];

    // Phan thuong binh thuong (moi lan vuot ai)
    [Export] public int NormalGold { get; set; } = 200;
    [Export] public int NormalExp { get; set; } = 100;

    // Dieu kien danh gia 3 sao
    // Sao 1: Chien thang
    // Sao 2: So tuong song sot >= Star2Condition
    [Export] public int Star2Condition { get; set; } = 3;
    // Sao 3: So luot hoan thanh <= Star3Condition
    [Export] public int Star3Condition { get; set; } = 5;

    public string GetStageDisplayNumber() => $"{ChapterId}-{StageId}";
}
