---
name: godot-csharp-architecture
description: Guidelines, design patterns, and safety rules for Godot 4.3 C# (.NET 8) in Tam Quoc 3D
triggers:
  - godot
  - csharp
  - dotnet
  - architecture
  - autoload
  - thread-safety
---

# Godot 4.3 C# (.NET 8) Architecture Skill

This skill governs the development and maintenance of C# scripts, nodes, scenes, and systems in **Tam Quoc 3D (Project Pixel Tactics)**.

## 1. Core Principles
1. **Strictly C# (.NET 8)**: Never write or introduce GDScript (`.gd`) files. All codebase is 100% C#.
2. **Every Node & Resource Class**:
   - Must use `public partial class ClassName : BaseType`.
   - Must be annotated with `[GlobalClass]` if it is instantiated or referenced as a custom type in the Godot inspector.
   - Never omit the `partial` keyword, as Godot source generators require it for interop.

## 2. Directory & Layer Organization
Follow the 3-tier modular architecture:
- `core/`: Core engine systems, Autoloads, interfaces, event buses, networking.
  - `core/autoload/`: Engine-wide singletons (`GameManager`, `EventBus`, `HeroDatabase`).
  - `core/network/`: Network client, HTTP/WS communication (`NetworkManager`).
  - `core/interfaces/`: Contracts (`ISkill`, `IUnit`, `IStatusEffect`).
  - `core/constants/`: Enums, game constants (`GameEnums.cs`).
- `data/`: Data schemas, resources, and databases.
  - `data/resources/`: Resource classes inheriting from `Resource` (`HeroData`, `SkillData`, `ItemData`).
  - `data/database/`: `.tres` data instances (`heroes/`, `skills/`, `items/`).
  - `data/sql/`: Database schema, migrations, seed data.
- `features/`: Isolated feature modules containing `.tscn` scenes and controller C# scripts.
  - `features/hub/`: Main Hub, Navigation, TopBar, BottomNavBar.
  - `features/deck_builder/`: Formation setup, drag-and-drop 3x3 grid.
  - `features/battle/`: Tactical turn-based battle engine, combat UI.
  - `features/campaign/`: Story stages, world map.

## 3. Multi-Threading & Godot Thread Safety (CRITICAL)
Godot 4.3 scene tree manipulation and signal emission are **not thread-safe**. Calling `EventBus.Instance.EmitSignal(...)` or modifying node trees from background worker threads (like `System.Net.Http` async tasks or C# `Task.Run()`) will crash or trigger:
> `Caller thread can't call this function in this node`

### Safe Dispatch Pattern:
Always marshal callbacks to the Godot Main Thread using `Callable.From().CallDeferred()`:
```csharp
// In NetworkManager or any async service:
private void SafeEmitEventBus(Action emitAction)
{
    if (EventBus.Instance != null && GodotObject.IsInstanceValid(EventBus.Instance))
    {
        Callable.From(emitAction).CallDeferred();
    }
}

// Usage:
SafeEmitEventBus(() => EventBus.Instance.EmitSignal(
    EventBus.SignalName.CurrencyChanged,
    "silver",
    silverAmount
));
```

## 4. Node Disposal & Signal Management (CRITICAL)
Whenever a node connects to events or delegates from static singletons (like `EventBus`), it **must** disconnect in `_ExitTree()` to prevent memory leaks and `ObjectDisposedException`:
```csharp
public override void _Ready()
{
    EventBus.Instance.CurrencyChanged += OnCurrencyChanged;
}

public override void _ExitTree()
{
    if (EventBus.Instance != null && GodotObject.IsInstanceValid(EventBus.Instance))
    {
        EventBus.Instance.CurrencyChanged -= OnCurrencyChanged;
    }
}

private void OnCurrencyChanged(string currencyType, long amount)
{
    if (!GodotObject.IsInstanceValid(this)) return;
    // Update UI
}
```

## 5. Type Ambiguity Prevention
Avoid ambiguous references between `Godot` and `System` namespaces:
- `HttpClient`: Godot has `Godot.HttpClient`, and .NET has `System.Net.Http.HttpClient`.
  - Always alias: `using NetHttpClient = System.Net.Http.HttpClient;`
- `Timer`: `Godot.Timer` vs `System.Threading.Timer` vs `System.Timers.Timer`.
- `Environment`: `Godot.Environment` vs `System.Environment`.

## 6. Build & Execution Validation
Always validate any C# change:
```bash
# 1. Compile C# project
dotnet build /home/annhan/Documents/src/tamquoc3d/tamquoc3d.csproj

# 2. Run Godot headless test
godot --headless --path /home/annhan/Documents/src/tamquoc3d --quit-after 20
```
