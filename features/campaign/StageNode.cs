using Godot;
using TamQuoc3D.Data.Resources;

namespace TamQuoc3D.Features.Campaign;

/// <summary>
/// StageNode - Hien thi bieu tuong mot ai tren ban do chien dich voi so sao, ten ai, trang thai khoa/mo khoa.
/// </summary>
[GlobalClass]
public partial class StageNode : Control
{
    [Signal]
    public delegate void StageClickedEventHandler(int chapterId, int stageId);

    public int ChapterId { get; private set; } = 1;
    public int StageId { get; private set; } = 1;
    public StageData Data { get; private set; }
    public int BestStars { get; private set; } = 0;
    public bool IsUnlocked { get; private set; } = false;
    public bool IsCurrentTarget { get; private set; } = false;

    private TextureButton _nodeBtn;
    private Label _numberLabel;
    private Label _nameLabel;
    private HBoxContainer _starsContainer;
    private TextureRect[] _starIcons = new TextureRect[3];
    private TextureRect _lockIcon;
    private Tween _pulseTween;

    private static readonly Texture2D NormalTex = GD.Load<Texture2D>("res://assets/ui/activities/campaign/stage_node_normal.png");
    private static readonly Texture2D BossTex = GD.Load<Texture2D>("res://assets/ui/activities/campaign/stage_node_boss.png");
    private static readonly Texture2D ResourceTex = GD.Load<Texture2D>("res://assets/ui/activities/campaign/stage_node_resource.png");
    private static readonly Texture2D LockedTex = GD.Load<Texture2D>("res://assets/ui/activities/campaign/stage_node_locked.png");
    private static readonly Texture2D StarFilledTex = GD.Load<Texture2D>("res://assets/ui/activities/campaign/star_filled.png");
    private static readonly Texture2D StarEmptyTex = GD.Load<Texture2D>("res://assets/ui/activities/campaign/star_empty.png");

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(100, 110);
        BuildUI();
    }

    private void BuildUI()
    {
        // Button container
        _nodeBtn = new TextureButton
        {
            CustomMinimumSize = new Vector2(80, 80),
            Position = new Vector2(10, 0),
            IgnoreTextureSize = true,
            StretchMode = TextureButton.StretchModeEnum.Scale,
            TextureFilter = TextureFilterEnum.Nearest
        };
        _nodeBtn.Pressed += OnNodePressed;
        _nodeBtn.MouseEntered += OnHoverEnter;
        _nodeBtn.MouseExited += OnHoverExit;
        AddChild(_nodeBtn);

        // Stage Number Label inside icon
        _numberLabel = new Label
        {
            Position = new Vector2(10, 52),
            Size = new Vector2(80, 20),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Text = "1-1",
            Modulate = new Color(1f, 0.95f, 0.8f)
        };
        _numberLabel.AddThemeFontSizeOverride("font_size", 12);
        AddChild(_numberLabel);

        // Lock icon overlay
        _lockIcon = new TextureRect
        {
            Position = new Vector2(36, 26),
            Size = new Vector2(28, 28),
            Texture = LockedTex,
            Visible = false,
            TextureFilter = TextureFilterEnum.Nearest
        };
        AddChild(_lockIcon);

        // 3 Stars container below node
        _starsContainer = new HBoxContainer
        {
            Position = new Vector2(18, 82),
            Size = new Vector2(64, 20),
            Alignment = BoxContainer.AlignmentMode.Center
        };
        _starsContainer.AddThemeConstantOverride("separation", 2);

        for (int i = 0; i < 3; i++)
        {
            _starIcons[i] = new TextureRect
            {
                CustomMinimumSize = new Vector2(16, 16),
                Texture = StarEmptyTex,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TextureFilter = TextureFilterEnum.Nearest
            };
            _starsContainer.AddChild(_starIcons[i]);
        }
        AddChild(_starsContainer);

        // Stage Name Label
        _nameLabel = new Label
        {
            Position = new Vector2(-15, 102),
            Size = new Vector2(130, 20),
            HorizontalAlignment = HorizontalAlignment.Center,
            Text = "Ải 1",
            Modulate = new Color(0.9f, 0.9f, 0.9f)
        };
        _nameLabel.AddThemeFontSizeOverride("font_size", 11);
        AddChild(_nameLabel);

        if (Data != null)
        {
            UpdateVisuals();
        }
    }

    public void SetData(StageData data, int stars, bool isUnlocked, bool isCurrentTarget = false)
    {
        Data = data;
        if (data != null)
        {
            ChapterId = data.ChapterId;
            StageId = data.StageId;
        }
        BestStars = Mathf.Clamp(stars, 0, 3);
        IsUnlocked = isUnlocked;
        IsCurrentTarget = isCurrentTarget;

        if (_nodeBtn != null)
        {
            UpdateVisuals();
        }
    }

    private void UpdateVisuals()
    {
        if (Data == null) return;

        _numberLabel.Text = Data.GetStageDisplayNumber();
        _nameLabel.Text = Data.StageName;

        // Texture selection based on Type and Lock status
        if (!IsUnlocked)
        {
            _nodeBtn.TextureNormal = LockedTex;
            _nodeBtn.Disabled = true;
            _lockIcon.Visible = true;
            _starsContainer.Visible = false;
            _nameLabel.Modulate = new Color(0.5f, 0.5f, 0.5f);
            StopPulse();
        }
        else
        {
            _nodeBtn.Disabled = false;
            _lockIcon.Visible = false;
            _starsContainer.Visible = true;
            _nameLabel.Modulate = new Color(0.95f, 0.95f, 0.95f);

            switch (Data.Type)
            {
                case StageType.Boss:
                    _nodeBtn.TextureNormal = BossTex;
                    _nameLabel.Modulate = new Color(1f, 0.4f, 0.4f);
                    break;
                case StageType.Resource:
                    _nodeBtn.TextureNormal = ResourceTex;
                    _nameLabel.Modulate = new Color(1f, 0.85f, 0.3f);
                    break;
                default:
                    _nodeBtn.TextureNormal = NormalTex;
                    break;
            }

            // Update stars
            for (int i = 0; i < 3; i++)
            {
                _starIcons[i].Texture = (i < BestStars) ? StarFilledTex : StarEmptyTex;
            }

            if (IsCurrentTarget)
            {
                StartPulse();
            }
            else
            {
                StopPulse();
            }
        }
    }

    private void StartPulse()
    {
        StopPulse();
        _pulseTween = CreateTween();
        if (_pulseTween == null) return;
        _pulseTween.SetLoops();
        _pulseTween.TweenProperty(_nodeBtn, "scale", new Vector2(1.1f, 1.1f), 0.5f)
                   .SetTrans(Tween.TransitionType.Sine);
        _pulseTween.TweenProperty(_nodeBtn, "scale", new Vector2(1.0f, 1.0f), 0.5f)
                   .SetTrans(Tween.TransitionType.Sine);
    }

    private void StopPulse()
    {
        if (_pulseTween != null && _pulseTween.IsValid())
        {
            _pulseTween.Kill();
            _pulseTween = null;
        }
        if (_nodeBtn != null)
        {
            _nodeBtn.Scale = Vector2.One;
        }
    }

    private void OnHoverEnter()
    {
        if (!IsUnlocked) return;
        var t = CreateTween();
        t?.TweenProperty(_nodeBtn, "scale", new Vector2(1.12f, 1.12f), 0.15f);
    }

    private void OnHoverExit()
    {
        if (!IsUnlocked) return;
        var t = CreateTween();
        t?.TweenProperty(_nodeBtn, "scale", IsCurrentTarget ? new Vector2(1.05f, 1.05f) : Vector2.One, 0.15f);
    }

    private void OnNodePressed()
    {
        if (!IsUnlocked) return;
        EmitSignal(SignalName.StageClicked, ChapterId, StageId);
    }

    public override void _ExitTree()
    {
        StopPulse();
    }
}
