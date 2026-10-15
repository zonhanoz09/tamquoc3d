namespace TamQuoc3D.Core.Interfaces;

/// <summary>
/// Giao dien dinh nghia hanh vi va thong so ky nang.
/// </summary>
public interface ISkill
{
    string SkillId { get; }
    string SkillName { get; }
    int MoraleCost { get; }
    float DamageMultiplier { get; }
    bool IsRageSkill { get; }
}
