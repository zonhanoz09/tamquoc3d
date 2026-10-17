using Godot;
using TamQuoc3D.Core.Navigation;

namespace TamQuoc3D.Features.Campaign;

/// <summary>
/// CampaignScene - Màn hình Chiến Dịch (Placeholder Phase 3) cho phép chọn ải và quay lại Hub.
/// </summary>
[GlobalClass]
public partial class CampaignScene : Control
{
    public override void _Ready()
    {
        // Background
        var bg = new ColorRect
        {
            CustomMinimumSize = new Vector2(1280, 720),
            Color = new Color(0.08f, 0.1f, 0.12f)
        };
        AddChild(bg);

        var title = new Label
        {
            Position = new Vector2(40, 30),
            Text = "🗺️ CHIẾN DỊCH TAM QUỐC (CAMPAIGN)",
            Modulate = new Color(1f, 0.85f, 0.3f)
        };
        title.AddThemeFontSizeOverride("font_size", 22);
        AddChild(title);

        var subTitle = new Label
        {
            Position = new Vector2(40, 70),
            Text = "Chương 1: Hoàng Cân Khởi Nghĩa — Chọn ải để xuất quân hoặc bố trận.",
            Modulate = new Color(0.85f, 0.85f, 0.85f)
        };
        subTitle.AddThemeFontSizeOverride("font_size", 14);
        AddChild(subTitle);

        // Buttons
        var toFormationBtn = new Button
        {
            Position = new Vector2(40, 140),
            Size = new Vector2(200, 46),
            Text = "⚔️ Bố Trận Xuất Trận"
        };
        toFormationBtn.Pressed += () => SceneNavigator.GoToFormationSelector("campaign");
        AddChild(toFormationBtn);

        var backBtn = new Button
        {
            Position = new Vector2(40, 200),
            Size = new Vector2(200, 46),
            Text = "⬅️ Quay Lại Hub"
        };
        backBtn.Pressed += () => SceneNavigator.GoBack();
        AddChild(backBtn);
    }
}
