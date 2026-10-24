using System.Collections.Generic;
using System.Linq;
using Godot;
using TamQuoc3D.Core.Autoload;
using TamQuoc3D.Core.Navigation;
using TamQuoc3D.Data.Database;

namespace TamQuoc3D.Features.Hub.Ui;

/// <summary>
/// HubBackground - San dai ban doanh voi parallax, hat canh hoa ambient va
/// cac nhan vat tren san theo anh mau (§6): 3 NPC DUNG YEN (Dieu Thuyen canh
/// cay dao, Gia Cat Luong canh hang rao, Thuong Nhan) + DUY NHAT nhan vat
/// nguoi choi di chuyen duoc (click-to-move trong vung di).
/// Buc vang / cay lon / den da / hang rao la hinh nen ve san, khong phai node.
/// </summary>
[GlobalClass]
public partial class HubBackground : Node2D
{
    /// <summary>Vung di lai cua nhan vat nguoi choi (§6): x 200-1040, y 340-640.</summary>
    public static readonly Rect2 WalkZone = new(200, 340, 840, 300);

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
        SpawnCourtyard();
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

    /// <summary>
    /// San theo anh mau (§6): 1 nhan vat nguoi choi (tuong dai dien trong ho so,
    /// xuat phat (627, 576), duy nhat di chuyen duoc) + 3 NPC dung yen.
    /// Toa do NPC do tu anh mau (tam sprite): Dao (404, 297), Lam (900, 305);
    /// Position dat o chan nhan vat nen cong them chieu cao sprite (~33px).
    /// </summary>
    private void SpawnCourtyard()
    {
        var gm = GameManager.Instance;
        var profile = gm?.Profile;
        var avatarId = !string.IsNullOrEmpty(profile?.AvatarHeroId) ? profile.AvatarHeroId : "quan_vu";
        var playerName = !string.IsNullOrEmpty(profile?.PlayerName) ? profile.PlayerName : "Chúa Công";
        var playerSub = profile != null ? $"Lv.{profile.Level}" : string.Empty;

        MainCharacter = CreateCharacter(
            avatarId, playerName, playerSub,
            isMain: true, isGuest: false,
            position: new Vector2(627, 576),
            wander: false);
        MainCharacter.CharacterClicked += OnPlayerCharacterClicked;

        var npcs = new[]
        {
            ("dieu_thuyen", "Điêu Thuyền", "Cây Đào", new Vector2(404, 330),
             "Hoa đào năm nay nở sớm, Chúa Công có muốn ngắm cùng thiếp không?"),
            ("gia_cat_luong", "Gia Cát Lượng", "Quân Sư", new Vector2(900, 340),
             "Thiên thời, địa lợi, nhân hòa — ba thứ đã đủ, chỉ chờ Chúa Công xuất chinh."),
            ("tao_nhan", "Thương Nhân", "Cửa Hàng", new Vector2(760, 340),
             "Hàng mới về mỗi ngày, Chúa Công ghé xem có gì hợp ý không?"),
        };
        foreach (var (heroId, name, sub, position, dialogue) in npcs)
        {
            var npc = CreateCharacter(
                heroId, name, sub,
                isMain: false, isGuest: false,
                position: position,
                wander: false);
            var capturedDialogue = dialogue;
            var capturedName = name;
            npc.CharacterClicked += _ =>
            {
                if (heroId == "tao_nhan")
                {
                    SceneNavigator.GoToShop();
                    return;
                }
                EventBus.Instance?.EmitSignal(
                    EventBus.SignalName.SystemNotification, capturedName, capturedDialogue, "npc");
            };
        }
    }

    private void OnPlayerCharacterClicked(HubCharacter character)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.FocusHeroId = character.HeroId;
        }
        SceneNavigator.GoToHeroCollection();
    }

    /// <summary>Tao mot nhan vat tren san (nhan vat nguoi choi hoac NPC tinh).</summary>
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
