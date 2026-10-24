using System.Collections.Generic;
using System.Linq;
using Godot;
using TamQuoc3D.Core.Autoload;
using TamQuoc3D.Core.Navigation;
using TamQuoc3D.Data.Database;

namespace TamQuoc3D.Features.Hub.Ui;

/// <summary>
/// HubBackground - San dien hoang cung voi parallax, hat lua ambient va
/// cac nhan vat di lai tren san: doi hinh that cua nguoi choi (toi da 5 tuong,
/// nameplate ten + cap + sao) cong khach online (nguoi choi khac) o H4.
/// Tuong chinh nhan lenh click-to-move khi bam xuong nen san.
/// </summary>
[GlobalClass]
public partial class HubBackground : Node2D
{
    /// <summary>Vung di lai tren nen san (tranh TopBar, cot icon phai, BottomNav).</summary>
    public static readonly Rect2 WalkZone = new(90, 350, 970, 285);

    private Sprite2D _bgSprite;
    private Node2D _characterContainer;
    private CpuParticles2D _ambientParticles;
    private Vector2 _targetParallax = Vector2.Zero;
    private Vector2 _currentParallax = Vector2.Zero;

    private readonly List<HubCharacter> _characters = new();
    public IReadOnlyList<HubCharacter> Characters => _characters;
    public HubCharacter MainCharacter { get; private set; }

    public override void _Ready()
    {
        BuildCourtyardVisuals();
        SpawnTeamCharacters();
        SetupAmbientParticles();
    }

    public override void _Process(double delta)
    {
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
            float nx = (mm.Position.X - 640f) / 640f;
            float ny = (mm.Position.Y - 360f) / 360f;
            _targetParallax = new Vector2(-nx * 14f, -ny * 8f);
        }
        else if (@event is InputEventMouseButton mb && mb.Pressed
                 && mb.ButtonIndex == MouseButton.Left
                 && WalkZone.HasPoint(mb.Position))
        {
            // Click xuong nen san -> tuong chinh di toi (click trung tuong da bi
            // TextureButton cua tuong do xu ly truoc, khong xuong toi day)
            MainCharacter?.SetMoveTarget(mb.Position);
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

        _characterContainer = new Node2D
        {
            ZIndex = 5,
            YSortEnabled = true
        };
        AddChild(_characterContainer);
    }

    /// <summary>Doi hinh that tu CurrentFormation; chua co thi dung doi khoi dau mau.</summary>
    private List<string> ResolveTeamHeroIds()
    {
        var gm = GameManager.Instance;
        var ids = new List<string>();
        if (gm?.CurrentFormation != null)
        {
            foreach (var entry in gm.CurrentFormation)
            {
                if (!string.IsNullOrEmpty(entry.HeroId) && !ids.Contains(entry.HeroId))
                {
                    ids.Add(entry.HeroId);
                }
            }
        }
        if (ids.Count == 0)
        {
            ids.AddRange(new[] { "quan_vu", "trieu_van", "ma_sieu", "gia_cat_luong", "tieu_kieu" });
        }
        return ids.Take(5).ToList();
    }

    private void SpawnTeamCharacters()
    {
        var gm = GameManager.Instance;
        var heroIds = ResolveTeamHeroIds();
        string mainId = gm?.Profile?.AvatarHeroId;
        if (string.IsNullOrEmpty(mainId) || !heroIds.Contains(mainId))
        {
            mainId = heroIds[0];
        }

        var spots = new List<Vector2>
        {
            new(640, 575), new(430, 520), new(850, 520), new(310, 585), new(970, 585)
        };

        // Tuong chinh dung giua, cac tuong con lai xep hai ben
        var ordered = new List<string> { mainId };
        ordered.AddRange(heroIds.Where(id => id != mainId));

        for (int i = 0; i < ordered.Count; i++)
        {
            string heroId = ordered[i];
            bool isMain = heroId == mainId;
            var heroData = HeroDatabase.Instance?.GetHero(heroId);
            string displayName = heroData?.HeroName ?? heroId;
            var progress = gm?.GetHeroProgress(heroId);
            string subText = progress != null ? $"Lv.{progress.Level}  ★{progress.Star}" : string.Empty;

            var character = CreateCharacter(
                heroId, displayName, subText,
                isMain: isMain, isGuest: false,
                position: spots[i % spots.Count],
                wander: !isMain);
            if (isMain) MainCharacter = character;
            character.CharacterClicked += OnTeamCharacterClicked;
        }
    }

    private void OnTeamCharacterClicked(HubCharacter character)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.FocusHeroId = character.HeroId;
        }
        SceneNavigator.GoToHeroCollection();
    }

    /// <summary>Tao mot nhan vat tren san (dung cho ca doi hinh lan khach online).</summary>
    public HubCharacter CreateCharacter(
        string heroId, string displayName, string subText,
        bool isMain, bool isGuest, Vector2 position, bool wander)
    {
        var character = new HubCharacter
        {
            Name = $"HubChar_{heroId}",
            Position = position
        };
        _characterContainer.AddChild(character);
        character.Setup(heroId, displayName, subText, isMain, isGuest, WalkZone, wander);
        character.CanStandAt = (point) => IsSpotFree(character, point);
        _characters.Add(character);
        return character;
    }

    private bool IsSpotFree(HubCharacter self, Vector2 point)
    {
        foreach (var other in _characters)
        {
            if (other == self) continue;
            if (other.Position.DistanceTo(point) < 70f) return false;
            if (other.CurrentTarget.HasValue && other.CurrentTarget.Value.DistanceTo(point) < 70f) return false;
        }
        return true;
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
