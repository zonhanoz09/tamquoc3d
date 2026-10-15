using Godot;

namespace TamQuoc3D.Hero;

[GlobalClass]
public partial class HeroData : Resource
{
    public enum ElementType { Fire, Wind, Water, Light, Dark, Neutral }
    public enum PositionType { Front, Mid, Back }
    public enum RarityType { R, Sr, Ssr, Ur }

    [Export] public string HeroId { get; set; } = "";
    [Export] public string HeroName { get; set; } = "Unknown Hero";
    [Export] public string Title { get; set; } = "";
    [Export] public ElementType Element { get; set; } = ElementType.Neutral;
    [Export] public PositionType Position { get; set; } = PositionType.Front;
    [Export] public RarityType Rarity { get; set; } = RarityType.R;
    [Export] public HeroStats? BaseStats { get; set; }

    public Godot.Collections.Array<SkillData> Skills { get; set; } = new();
}
