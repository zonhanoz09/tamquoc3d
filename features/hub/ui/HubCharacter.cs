using System;
using Godot;

namespace TamQuoc3D.Features.Hub.Ui;

/// <summary>
/// HubCharacter - Mot nhan vat di lai tren san dien hub: sprite idle/walk,
/// bong duoi chan, nameplate (ten + dong phu) va vung click.
/// Hanh vi: dung idle ngau nhien 2-5s roi di dao toi diem ngau nhien trong
/// WalkZone (wander); nhan vat chinh nhan them lenh click-to-move tu hub.
/// Sprite tu dong dung anim "walk" neu SpriteFrames co, khong thi dung
/// "idle" kem nhun nhe theo buoc chan.
/// </summary>
[GlobalClass]
public partial class HubCharacter : Node2D
{
    [Signal]
    public delegate void CharacterClickedEventHandler(HubCharacter character);

    public string HeroId { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public bool IsMain { get; private set; }
    public bool IsGuest { get; private set; }
    public float Speed { get; set; } = 65f;
    public Rect2 WalkZone { get; set; }
    public Vector2? CurrentTarget => _target;

    /// <summary>Kiem tra diem dung co qua gan nhan vat khac khong (spawner cung cap).</summary>
    public Func<Vector2, bool> CanStandAt { get; set; }

    private AnimatedSprite2D _anim;
    private Sprite2D _shadow;
    private Vector2? _target;
    private double _idleTimer = 1.0;
    private double _bobPhase;
    private bool _wanderEnabled;
    private float _baseAnimY;
    private readonly Random _rng = new();

    public void Setup(
        string heroId,
        string displayName,
        string subText,
        bool isMain,
        bool isGuest,
        Rect2 walkZone,
        bool wander)
    {
        HeroId = heroId;
        DisplayName = displayName;
        IsMain = isMain;
        IsGuest = isGuest;
        WalkZone = walkZone;
        _wanderEnabled = wander;
        _idleTimer = 0.5 + _rng.NextDouble() * 2.0;
        if (isGuest) Speed = 48f;

        BuildVisuals(heroId, displayName, subText, isGuest);
    }

    private void BuildVisuals(string heroId, string displayName, string subText, bool isGuest)
    {
        // Bong duoi chan (ellipse mem ve bang code nhu ban goc)
        _shadow = new Sprite2D { Position = new Vector2(0, 2) };
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
                    shadowImg.SetPixel(x, y, new Color(0, 0, 0, Mathf.Clamp(1.0f - distSq, 0f, 1f) * 0.38f));
                }
            }
        }
        _shadow.Texture = ImageTexture.CreateFromImage(shadowImg);
        AddChild(_shadow);

        // Sprite nhan vat
        _anim = new AnimatedSprite2D();
        string sfPath = $"res://assets/heroes/{heroId}/sprite_frames.tres";
        if (ResourceLoader.Exists(sfPath))
        {
            var frames = GD.Load<SpriteFrames>(sfPath);
            if (frames != null)
            {
                _anim.SpriteFrames = frames;
                _anim.Play("idle");
            }
        }
        else
        {
            string sprPath = $"res://assets/heroes/{heroId}/spritesheet.png";
            if (ResourceLoader.Exists(sprPath))
            {
                var tex = GD.Load<Texture2D>(sprPath);
                var frames = new SpriteFrames();
                frames.AddAnimation("idle");
                for (int i = 0; i < 4; i++)
                {
                    frames.AddFrame("idle", new AtlasTexture { Atlas = tex, Region = new Rect2(i * 64, 0, 64, 64) });
                }
                frames.SetAnimationSpeed("idle", 6);
                _anim.SpriteFrames = frames;
                _anim.Play("idle");
            }
        }
        AddChild(_anim);

        // Nameplate: ten + dong phu (cap/sao hoac thong tin khach), luon nam tren dau
        var nameLabel = new Label
        {
            Text = displayName,
            Position = new Vector2(-80, -132),
            Size = new Vector2(160, 18),
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = isGuest ? new Color(0.75f, 0.9f, 1f) : new Color(1f, 0.95f, 0.8f)
        };
        nameLabel.AddThemeFontSizeOverride("font_size", 12);
        AddChild(nameLabel);

        if (!string.IsNullOrEmpty(subText))
        {
            var subLabel = new Label
            {
                Text = subText,
                Position = new Vector2(-80, -116),
                Size = new Vector2(160, 16),
                HorizontalAlignment = HorizontalAlignment.Center,
                Modulate = new Color(0.85f, 0.85f, 0.85f)
            };
            subLabel.AddThemeFontSizeOverride("font_size", 10);
            AddChild(subLabel);
        }

        // Vung click tren than nhan vat
        var clickBtn = new TextureButton
        {
            Position = new Vector2(-45, -125),
            Size = new Vector2(90, 130)
        };
        clickBtn.Pressed += () => EmitSignal(SignalName.CharacterClicked, this);
        AddChild(clickBtn);

        UpdateDepthScale();
    }

    /// <summary>Lenh di toi mot diem (click-to-move cho tuong chinh, hoac hub dieu phoi).</summary>
    public void SetMoveTarget(Vector2 point)
    {
        _target = new Vector2(
            Mathf.Clamp(point.X, WalkZone.Position.X, WalkZone.Position.X + WalkZone.Size.X),
            Mathf.Clamp(point.Y, WalkZone.Position.Y, WalkZone.Position.Y + WalkZone.Size.Y));
    }

    public override void _Process(double delta)
    {
        if (_anim == null) return;

        bool moving = false;
        if (_target.HasValue)
        {
            Vector2 toTarget = _target.Value - Position;
            float dist = toTarget.Length();
            float step = Speed * (float)delta;
            if (dist <= Mathf.Max(4f, step))
            {
                Position = _target.Value;
                _target = null;
                _idleTimer = 2.0 + _rng.NextDouble() * 3.0;
            }
            else
            {
                Vector2 dir = toTarget / dist;
                Position += dir * step;
                if (Mathf.Abs(dir.X) > 0.05f) _anim.FlipH = dir.X < 0;
                moving = true;
            }
        }
        else if (_wanderEnabled)
        {
            _idleTimer -= delta;
            if (_idleTimer <= 0)
            {
                TryPickWanderTarget();
                _idleTimer = 1.0;
            }
        }

        // Anim + nhun khi di chuyen; scale theo do sau (y cang lon cang gan camera)
        if (moving)
        {
            string animName = _anim.SpriteFrames != null && _anim.SpriteFrames.HasAnimation("walk") ? "walk" : "idle";
            if (_anim.Animation != animName) _anim.Play(animName);
            _bobPhase += delta * 9.0;
            _anim.Position = new Vector2(0, _baseAnimY - Mathf.Abs(Mathf.Sin((float)_bobPhase)) * 3f);
        }
        else
        {
            if (_anim.Animation != "idle") _anim.Play("idle");
            _anim.Position = new Vector2(0, _baseAnimY);
            _bobPhase = 0;
        }

        UpdateDepthScale();
    }

    private void TryPickWanderTarget()
    {
        for (int attempt = 0; attempt < 8; attempt++)
        {
            var candidate = new Vector2(
                WalkZone.Position.X + (float)_rng.NextDouble() * WalkZone.Size.X,
                WalkZone.Position.Y + (float)_rng.NextDouble() * WalkZone.Size.Y);
            if (CanStandAt == null || CanStandAt(candidate))
            {
                _target = candidate;
                return;
            }
        }
    }

    private void UpdateDepthScale()
    {
        if (WalkZone.Size.Y <= 0) return;
        float t = Mathf.Clamp((Position.Y - WalkZone.Position.Y) / WalkZone.Size.Y, 0f, 1f);
        float scale = 2.0f + 0.6f * t;
        if (_anim != null)
        {
            var s = _anim.Scale;
            _anim.Scale = new Vector2(scale, scale);
            _baseAnimY = -36f * (scale / 2.0f);
            if (!_target.HasValue)
            {
                _anim.Position = new Vector2(0, _baseAnimY);
            }
        }
        if (_shadow != null)
        {
            _shadow.Scale = new Vector2(scale * 0.8f, scale * 0.45f);
        }
    }
}
