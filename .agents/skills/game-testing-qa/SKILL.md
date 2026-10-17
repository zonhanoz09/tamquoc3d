---
name: game-testing-qa
description: Testing standards, headless test execution, and CI/QA verification for Tam Quoc 3D
triggers:
  - test
  - qa
  - headless
  - verification
  - regression
  - validation
---

# Game Testing & QA Automation Skill

This skill defines the testing protocols, headless execution procedures, and quality assurance standards for **Tam Quoc 3D**.

## 1. Automated Headless Test Execution (CRITICAL)
In Godot 4.3 with C# Mono:
- **DO NOT** run `godot -s script.cs` on classes that inherit from `Node2D`, `Control`, or `Node`. The `-s` flag expects a `SceneTree` or `MainLoop` implementation and will hang indefinitely.
- **ALWAYS** run tests using the scene path (`.tscn`):
  ```bash
  godot --headless res://tests/TestClientServerConnection.tscn
  ```
- Or run with a safety timeout/frame limiter:
  ```bash
  godot --headless res://tests/TestHubScreen.tscn --quit-after 100
  ```

## 2. Standard Test Suite Locations
- [`tests/TestHubScreen.tscn`](file:///home/annhan/Documents/src/tamquoc3d/tests/TestHubScreen.tscn): Verifies TopBar, BottomNavBar, ShortcutBar, EventBus notifications.
- [`tests/TestFormationSelector.tscn`](file:///home/annhan/Documents/src/tamquoc3d/tests/TestFormationSelector.tscn): Verifies 3x3 formation grid placement, hero assignments, and slot validation.
- [`tests/TestFormationToBattle.tscn`](file:///home/annhan/Documents/src/tamquoc3d/tests/TestFormationToBattle.tscn): Verifies transition from Deck Builder to Battle Arena with hero stats.
- [`tests/TestClientServerConnection.tscn`](file:///home/annhan/Documents/src/tamquoc3d/tests/TestClientServerConnection.tscn): Verifies full 6-step integration with Docker FastAPI + PostgreSQL.

## 3. Creating a New Headless Test Scene
1. Create C# script inheriting from `Node2D`:
   ```csharp
   using Godot;

   [GlobalClass]
   public partial class TestFeatureX : Node2D
   {
       public override async void _Ready()
       {
           GD.Print("=== RUNNING TEST: Feature X ===");
           bool success = await RunTestsAsync();
           if (success)
           {
               GD.Print("✅ ALL TESTS PASSED!");
               GetTree().Quit(0); // Exit code 0 for success
           }
           else
           {
               GD.PrintErr("❌ TEST FAILED!");
               GetTree().Quit(1); // Exit code 1 for failure
           }
       }
   }
   ```
2. Create `.tscn` file instantiating the script as root node.
3. Run headlessly:
   ```bash
   godot --headless res://tests/TestFeatureX.tscn
   ```

## 4. Docker Backend Health Check
```bash
# Verify all backend services are healthy
docker compose ps --filter "health=healthy"

# Direct health endpoint check
curl -s http://localhost:8000/health | jq .
```
