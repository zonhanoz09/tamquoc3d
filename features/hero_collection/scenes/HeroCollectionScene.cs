using Godot;
using TamQuoc3D.Core.Navigation;

namespace TamQuoc3D.Features.HeroCollection;

/// <summary>
/// HeroCollectionScene - Tướng Phủ / Bộ sưu tập võ tướng.
/// </summary>
[GlobalClass]
public partial class HeroCollectionScene : Control
{
    public override void _Ready()
    {
        var bg = new ColorRect
        {
            CustomMinimumSize = new Vector2(1280, 720),
            Color = new Color(0.06f, 0.09f, 0.08f)
        };
        AddChild(bg);

        var title = new Label
        {
            Position = new Vector2(40, 30),
            Text = "🎴 TƯỚNG PHỦ — DANH TƯỚNG TAM QUỐC",
            Modulate = new Color(1f, 0.85f, 0.3f)
        };
        title.AddThemeFontSizeOverride("font_size", 22);
        AddChild(title);

        var backBtn = new Button
        {
            Position = new Vector2(40, 140),
            Size = new Vector2(200, 46),
            Text = "⬅️ Quay Lại Hub"
        };
        backBtn.Pressed += () => SceneNavigator.GoBack();
        AddChild(backBtn);
    }
}
