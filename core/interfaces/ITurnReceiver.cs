namespace TamQuoc3D.Core.Interfaces;

/// <summary>
/// Giao dien nhan tin hieu luot hanh dong trong he thong Turn-Based.
/// </summary>
public interface ITurnReceiver
{
    int GetSpeed();
    void OnTurnStart();
    void OnTurnEnd();
}
