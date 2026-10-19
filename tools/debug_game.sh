#!/usr/bin/env bash
# tools/debug_game.sh - Script ho tro debug game Tam Quoc 3D (Godot 4.3 C# .NET 8)
set -e

PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$PROJECT_DIR"

echo "=================================================="
echo "🎮 TAM QUOC 3D - .NET 8 / GODOT DEBUG UTILITY"
echo "=================================================="

# Mode check
if [ "$1" == "--monitor" ]; then
    echo "📊 Dang khoi chay dotnet-counters theo doi tien trinh Godot..."
    exec dotnet-counters monitor --name godot --counters System.Runtime
elif [ "$1" == "--trace" ]; then
    echo "📈 Dang ghi vet hieu nang (dotnet-trace) cho tien trinh Godot..."
    exec dotnet-trace collect --name godot
elif [ "$1" == "--dump" ]; then
    echo "💾 Dang tao file memory dump (dotnet-dump) cho tien trinh Godot..."
    exec dotnet-dump collect --name godot
fi

# 1. Build C# project in Debug mode
echo "🔨 [1/2] Bien dich C# (.NET 8 Debug)..."
dotnet build tamquoc3d.csproj /property:GenerateFullPaths=true

# 2. Launch Godot with debug flags
SCENE="${1:-res://features/hub/scenes/HubScene.tscn}"
echo "🚀 [2/2] Khoi chay Godot Debug voi Scene: $SCENE"
godot --debug --path "$PROJECT_DIR" "$SCENE"
