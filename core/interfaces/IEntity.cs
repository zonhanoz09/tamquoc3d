using Godot;

namespace TamQuoc3D.Core.Interfaces;

/// <summary>
/// Giao dien co ban cho moi thuc the tren chien truong.
/// </summary>
public interface IEntity
{
    string Id { get; }
    string Name { get; }
    bool IsAlive { get; }
    Vector2 Position { get; set; }
}
