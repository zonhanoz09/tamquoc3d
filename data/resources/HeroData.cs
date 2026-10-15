using Godot;

namespace TamQuoc3D.Data.Resources;

/// <summary>
/// HeroData - Dinh nghia cau hinh tuong duoi dang Custom Resource (.tres).
/// </summary>
[GlobalClass]
public partial class HeroData : Resource
{
    public enum ElementType
    {
        None,
        Fire,   // Hoa (khac Phong)
        Water,  // Thuy (khac Hoa)
        Wind,   // Phong (khac Thuy)
        Light,  // Quang (khac Am)
        Dark    // Am (khac Quang)
    }

    public enum PositionType
    {
        Front, // Tien quan (Chiu don, khong che)
        Mid,   // Trung quan (Sat thuong chu luc)
        Back   // Hau quan (Ho tro, hoi phuc, phep thuat)
    }

    public enum RarityType
    {
        R,
        Sr,
        Ssr,
        Ur
    }

    [Export] public string HeroId { get; set; } = string.Empty;
    [Export] public string HeroName { get; set; } = string.Empty;
    [Export] public string Title { get; set; } = string.Empty;
    [Export] public ElementType Element { get; set; } = ElementType.None;
    [Export] public PositionType Position { get; set; } = PositionType.Mid;
    [Export] public RarityType Rarity { get; set; } = RarityType.Ssr;

    [Export] public HeroStats BaseStats { get; set; } = new();
    [Export] public Godot.Collections.Array<SkillData> Skills { get; set; } = [];

    public SkillData GetNormalAttack()
    {
        return Skills.Count > 0 ? Skills[0] : null;
    }

    public SkillData GetRageSkill()
    {
        return Skills.Count > 1 ? Skills[1] : null;
    }
}
