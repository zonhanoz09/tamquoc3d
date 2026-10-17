using System.Collections.Generic;
using Godot;
using TamQuoc3D.Data.Resources;

namespace TamQuoc3D.Features.Campaign;

/// <summary>
/// ChapterMap - Ban do hien thi cac ai ai (StageNode) theo loi di chien thuat (Snake layout) va cac duong noi Line2D.
/// </summary>
[GlobalClass]
public partial class ChapterMap : Control
{
    [Signal]
    public delegate void StageSelectedEventHandler(StageData stageData);

    private TextureRect _bgRect;
    private Control _linesContainer;
    private Control _nodesContainer;
    private readonly List<StageNode> _stageNodes = [];

    // Predefined tactical map waypoint positions for up to 6 stages (1280x720 screen)
    private static readonly Vector2[] Waypoints =
    [
        new Vector2(160, 480), // 1-1: Bien gioi
        new Vector2(340, 320), // 1-2: Rung truc
        new Vector2(530, 460), // 1-3: Tieu Bai
        new Vector2(720, 300), // 1-4: Tranh Hung
        new Vector2(910, 440), // 1-5: Cong Lac Duong
        new Vector2(1080, 270) // 1-6: Boss Dong Trac
    ];

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(1280, 720);
        BuildMapHierarchy();
    }

    private void BuildMapHierarchy()
    {
        // 1. Background image
        _bgRect = new TextureRect
        {
            CustomMinimumSize = new Vector2(1280, 720),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            TextureFilter = TextureFilterEnum.Nearest
        };
        AddChild(_bgRect);

        // 2. Lines layer (drawn under nodes)
        _linesContainer = new Control
        {
            CustomMinimumSize = new Vector2(1280, 720),
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(_linesContainer);

        // 3. Stage nodes layer
        _nodesContainer = new Control
        {
            CustomMinimumSize = new Vector2(1280, 720)
        };
        AddChild(_nodesContainer);
    }

    public void LoadChapter(ChapterData chapter, Dictionary<int, int> starsData, int highestUnlockedStage)
    {
        if (chapter == null) return;

        // Load Background
        if (!string.IsNullOrEmpty(chapter.BackgroundImage) && ResourceLoader.Exists(chapter.BackgroundImage))
        {
            _bgRect.Texture = GD.Load<Texture2D>(chapter.BackgroundImage);
        }

        // Clear previous nodes & lines
        foreach (Node child in _nodesContainer.GetChildren())
        {
            child.QueueFree();
        }
        foreach (Node child in _linesContainer.GetChildren())
        {
            child.QueueFree();
        }
        _stageNodes.Clear();

        var stages = chapter.Stages;
        if (stages == null || stages.Count == 0) return;

        // Create Path Lines between consecutive waypoints
        for (int i = 0; i < stages.Count - 1 && i < Waypoints.Length - 1; i++)
        {
            var p1 = Waypoints[i] + new Vector2(50, 40); // Center of node
            var p2 = Waypoints[i + 1] + new Vector2(50, 40);

            bool isPathUnlocked = (i + 1) < highestUnlockedStage;
            Color lineColor = isPathUnlocked ? new Color(1f, 0.85f, 0.2f, 0.85f) : new Color(0.35f, 0.4f, 0.5f, 0.5f);

            var line = new Line2D
            {
                Width = 5.0f,
                DefaultColor = lineColor,
                Antialiased = true
            };
            line.AddPoint(p1);
            line.AddPoint(p2);
            _linesContainer.AddChild(line);
        }

        // Instantiate StageNodes
        for (int i = 0; i < stages.Count && i < Waypoints.Length; i++)
        {
            var stage = stages[i];
            int stageId = stage.StageId;
            int stars = starsData != null && starsData.TryGetValue(stageId, out int s) ? s : 0;
            bool isUnlocked = stageId <= highestUnlockedStage;
            bool isCurrentTarget = stageId == highestUnlockedStage;

            var node = new StageNode
            {
                Position = Waypoints[i]
            };
            node.StageClicked += (chId, stId) => OnStageNodeClicked(stage);

            _nodesContainer.AddChild(node);
            node.SetData(stage, stars, isUnlocked, isCurrentTarget);
            _stageNodes.Add(node);
        }
    }

    private void OnStageNodeClicked(StageData stage)
    {
        EmitSignal(SignalName.StageSelected, stage);
    }
}
