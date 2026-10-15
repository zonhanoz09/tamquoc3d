namespace TamQuoc3D.Core.Interfaces;

/// <summary>
/// Giao dien dinh nghia hieu ung trang thai (Buff / Debuff / Control).
/// </summary>
public interface IEffect
{
    int EffectType { get; }
    int Duration { get; set; }
    int Value { get; }
    void OnApply(IEntity target);
    void OnRemove(IEntity target);
}
