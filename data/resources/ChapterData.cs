using Godot;

namespace TamQuoc3D.Data.Resources;

/// <summary>
/// ChapterData - Dinh nghia mot chuong trong chien dich ban do (.tres).
/// </summary>
[GlobalClass]
public partial class ChapterData : Resource
{
    [Export] public int ChapterId { get; set; } = 1;
    [Export] public string ChapterName { get; set; } = "Chương 1: Khởi Binh Đại Hán";
    [Export] public string Subtitle { get; set; } = "Dẹp loạn Hoàng Cân, khởi binh định thiên hạ";
    [Export] public string BackgroundImage { get; set; } = "res://assets/ui/activities/campaign/chapter_bg_ch01.png";
    [Export] public string BgmTrack { get; set; } = "res://assets/audio/bgm/campaign_ch01.ogg";
    [Export] public Godot.Collections.Array<StageData> Stages { get; set; } = [];
    [Export] public int TotalStars { get; set; } = 18;
    [Export] public int UnlockRequirement { get; set; } = 0; // Stars required from previous chapter
}
