using Godot;

namespace TamQuoc3D.Visual;

/// <summary>
/// FloatingText - Hien thi so sat thuong va chu hieu ung nay tren chien truong.
/// </summary>
[GlobalClass]
public partial class FloatingText : Node2D
{
    public string DisplayText { get; set; } = string.Empty;
    public Color TextColor { get; set; } = Colors.White;
    public bool IsCrit { get; set; } = false;
    public int FontSize { get; set; } = 16;

    public override void _Ready()
    {
        ZIndex = 50;

        var label = new Label
        {
            Text = DisplayText,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Position = new Vector2(-70, -15),
            Size = new Vector2(140, 30)
        };

        label.AddThemeColorOverride("font_color", TextColor);
        label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.9f));
        label.AddThemeConstantOverride("shadow_offset_x", 1);
        label.AddThemeConstantOverride("shadow_offset_y", 2);
        label.AddThemeConstantOverride("shadow_outline_size", 2);

        if (IsCrit)
        {
            label.AddThemeFontSizeOverride("font_size", 22);
            Scale = new Vector2(1.5f, 1.5f);
        }
        else
        {
            label.AddThemeFontSizeOverride("font_size", 16);
        }

        AddChild(label);

        // Animation
        var tw = CreateTween().SetParallel(true);
        float targetY = Position.Y - (IsCrit ? 60f : 45f);
        float randX = (float)GD.RandRange(-15.0, 15.0);

        tw.TweenProperty(this, "position:y", targetY, 0.7)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        tw.TweenProperty(this, "position:x", Position.X + randX, 0.7);

        if (IsCrit)
        {
            var scaleTw = CreateTween();
            scaleTw.TweenProperty(this, "scale", new Vector2(1.8f, 1.8f), 0.15);
            scaleTw.TweenProperty(this, "scale", new Vector2(1.1f, 1.1f), 0.2);
        }

        var fadeTw = CreateTween();
        fadeTw.TweenInterval(0.35);
        fadeTw.TweenProperty(this, "modulate:a", 0.0f, 0.35);
        fadeTw.TweenCallback(Callable.From(QueueFree));
    }
}
