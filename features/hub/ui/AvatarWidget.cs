using Godot;

namespace TamQuoc3D.Features.Hub.Ui;

[GlobalClass]
public partial class AvatarWidget : PanelContainer
{
    private const string AvatarTexturePath = "res://assets/temp/avatar.png";
    private const float DisplayWidth = 528f;

    public override void _Ready()
    {
        var texture = ResourceLoader.Load<Texture2D>(AvatarTexturePath);
        if (texture == null)
        {
            GD.PushError($"Avatar texture could not be loaded: {AvatarTexturePath}");
            return;
        }

        var displaySize = new Vector2(
            DisplayWidth,
            DisplayWidth * texture.GetHeight() / texture.GetWidth());
        CustomMinimumSize = displaySize;
        Size = displaySize;
        MouseFilter = MouseFilterEnum.Ignore;
        AddThemeStyleboxOverride("panel", new StyleBoxEmpty());

        var avatarImage = new TextureRect
        {
            Texture = texture,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            MouseFilter = MouseFilterEnum.Ignore
        };
        avatarImage.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(avatarImage);
    }
}