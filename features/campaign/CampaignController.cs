using System.Collections.Generic;
using Godot;
using TamQuoc3D.Data.Resources;

namespace TamQuoc3D.Features.Campaign;

/// <summary>
/// CampaignController - Quan ly ban do chien dich, danh sach ai ai (Stages) va phan thuong.
/// </summary>
[GlobalClass]
public partial class CampaignController : Node
{
    [Signal]
    public delegate void StageSelectedEventHandler(string stageId);

    public int CurrentChapter { get; set; } = 1;
    public int CurrentStage { get; set; } = 1;
    public List<MapConfig> AvailableMaps { get; } = [];

    public override void _Ready()
    {
        GD.Print("🗺️ CampaignController khoi tao thanh cong.");
    }

    public void SelectStage(string stageId)
    {
        EmitSignal(SignalName.StageSelected, stageId);
    }
}
