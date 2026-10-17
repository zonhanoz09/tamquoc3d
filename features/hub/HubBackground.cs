using Godot;
using TamQuoc3D.Core.Autoload;

namespace TamQuoc3D.Features.Hub;

/// <summary>
/// HubBackground - Hiển thị sân điện hoàng cung Tam Quốc rực rỡ, hiệu ứng tàn lửa/lá bay ambient,
/// và các nhân vật võ tướng (Chúa Công, Quan Vũ, Điêu Thuyền) đứng idle sống động trên thảm đỏ.
/// </summary>
[GlobalClass]
public partial class HubBackground : Node2D
{
    private Sprite2D _bgSprite;
    private Node2D _characterContainer;
    private CpuParticles2D _ambientParticles;
    private Vector2 _targetParallax = Vector2.Zero;
    private Vector2 _currentParallax = Vector2.Zero;

    public override void _Ready()
    {
        BuildCourtyardVisuals();
        SpawnCourtyardCharacters();
        SetupAmbientParticles();
    }

    public override void _Process(double delta)
    {
        // Smooth mouse parallax lerp
        _currentParallax = _currentParallax.Lerp(_targetParallax, (float)delta * 3.0f);
        if (_bgSprite != null)
        {
            _bgSprite.Position = new Vector2(640, 360) + _currentParallax * 0.4f;
        }
        if (_characterContainer != null)
        {
            _characterContainer.Position = _currentParallax * 1.0f;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mm)
        {
            // Normalize relative to center (1280x720)
            float nx = (mm.Position.X - 640f) / 640f;
            float ny = (mm.Position.Y - 360f) / 360f;
            _targetParallax = new Vector2(-nx * 14f, -ny * 8f);
        }
    }

    private void BuildCourtyardVisuals()
    {
        _bgSprite = new Sprite2D
        {
            Texture = GD.Load<Texture2D>("res://assets/ui/hub/hub_courtyard_bg.png"),
            Position = new Vector2(640, 360),
            ZIndex = 0
        };
        AddChild(_bgSprite);
    }

    private void SpawnCourtyardCharacters()
    {
        _characterContainer = new Node2D
        {
            ZIndex = 5
        };
        AddChild(_characterContainer);

        // 1. Companion NPC Left: Quan Vũ
        SpawnHeroAt("quan_vu", new Vector2(430, 520), scale: 2.1f, flipH: false, "Quan Vũ: Đại ca, mưu định thiên hạ!");

        // 2. Companion NPC Right: Điêu Thuyền
        SpawnHeroAt("dieu_thuyen", new Vector2(850, 520), scale: 2.1f, flipH: true, "Điêu Thuyền: Thiếp nguyện vì tướng quân đàn khúc tương phùng.");

        // 3. Main Hero Center: Lữ Bố (hoặc avatar hiện tại của player)
        string mainHeroId = GameManager.Instance?.Profile?.AvatarHeroId ?? "lu_bo";
        SpawnHeroAt(mainHeroId, new Vector2(640, 575), scale: 2.6f, flipH: false, "Chúa Công: Toàn quân nghe lệnh!");
    }

    private void SpawnHeroAt(string heroId, Vector2 pos, float scale, bool flipH, string dialogue)
    {
        var node = new Node2D
        {
            Position = pos
        };

        // Shadow
        var shadow = new Sprite2D
        {
            Position = new Vector2(0, -2),
            Scale = new Vector2(scale * 0.8f, scale * 0.45f)
        };
        var shadowImg = Image.CreateEmpty(32, 16, false, Image.Format.Rgba8);
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float nx = (x - 15.5f) / 15.5f;
                float ny = (y - 7.5f) / 7.5f;
                float distSq = nx * nx + ny * ny;
                if (distSq <= 1.0f)
                {
                    float a = Mathf.Clamp(1.0f - distSq, 0f, 1f) * 0.38f;
                    shadowImg.SetPixel(x, y, new Color(0, 0, 0, a));
                }
            }
        }
        shadow.Texture = ImageTexture.CreateFromImage(shadowImg);
        node.AddChild(shadow);

        // Pedestal
        var pedestal = new Sprite2D
        {
            Texture = GD.Load<Texture2D>("res://assets/ui/shared/pedestal_player.png"),
            Position = new Vector2(0, 4),
            Scale = new Vector2(scale * 0.45f, scale * 0.45f)
        };
        node.AddChild(pedestal);

        // Animated Sprite
        string sfPath = $"res://assets/heroes/{heroId}/sprite_frames.tres";
        if (ResourceLoader.Exists(sfPath))
        {
            var frames = GD.Load<SpriteFrames>(sfPath);
            if (frames != null)
            {
                var anim = new AnimatedSprite2D
                {
                    SpriteFrames = frames,
                    Position = new Vector2(0, -36 * (scale / 2.0f)),
                    Scale = new Vector2(scale, scale),
                    FlipH = flipH
                };
                node.AddChild(anim);
                anim.Play("idle");
            }
        }
        else
        {
            // Fallback sprite
            string sprPath = $"res://assets/heroes/{heroId}/spritesheet.png";
            if (ResourceLoader.Exists(sprPath))
            {
                var spr = new Sprite2D
                {
                    Texture = GD.Load<Texture2D>(sprPath),
                    Hframes = 4,
                    Position = new Vector2(0, -36 * (scale / 2.0f)),
                    Scale = new Vector2(scale, scale),
                    FlipH = flipH
                };
                node.AddChild(spr);
            }
        }

        // Clickable interactive area to speak dialogue
        var clickBtn = new TextureButton
        {
            Position = new Vector2(-40, -110),
            Size = new Vector2(80, 120),
            TooltipText = dialogue
        };
        clickBtn.Pressed += () =>
        {
            EventBus.Instance?.EmitSignal(EventBus.SignalName.SystemNotification, "Võ Tướng", dialogue, "hero");
        };
        node.AddChild(clickBtn);

        _characterContainer.AddChild(node);
    }

    private void SetupAmbientParticles()
    {
        _ambientParticles = new CpuParticles2D
        {
            Position = new Vector2(640, -20),
            Amount = 35,
            Lifetime = 6.0f,
            Preprocess = 4.0f,
            EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle,
            EmissionRectExtents = new Vector2(680, 20),
            Direction = new Vector2(0.4f, 1.0f),
            Spread = 25.0f,
            Gravity = new Vector2(15, 25),
            InitialVelocityMin = 20.0f,
            InitialVelocityMax = 50.0f,
            ScaleAmountMin = 2.0f,
            ScaleAmountMax = 4.5f,
            Color = new Color(1f, 0.75f, 0.35f, 0.7f),
            ZIndex = 8
        };
        AddChild(_ambientParticles);
    }
}
