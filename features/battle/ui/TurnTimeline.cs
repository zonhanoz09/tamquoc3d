using System.Collections.Generic;
using Godot;
using TamQuoc3D.Features.Battle.Entities;

namespace TamQuoc3D.Features.Battle.UI;

/// <summary>
/// TurnTimeline - Thanh thu tu hanh dong cua hiep hien tai (Phase 4 Battle HUD).
/// Hien avatar cac tuong con song theo dung thu tu TurnQueue, ke tu luot sap toi.
/// Tach khoi BattleScene de tai su dung cho HUD/man hinh khac.
/// </summary>
[GlobalClass]
public partial class TurnTimeline : HBoxContainer
{
    public TurnTimeline()
    {
        Alignment = AlignmentMode.Center;
        AddThemeConstantOverride("separation", 8);
    }

    /// <summary>Ve lai timeline tu hang doi luot va vi tri luot hien tai.</summary>
    public void Refresh(IReadOnlyList<BattleHero> queue, int currentIndex)
    {
        foreach (var child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }

        if (queue == null) return;

        int start = Mathf.Clamp(currentIndex, 0, queue.Count);
        for (int i = start; i < queue.Count; i++)
        {
            var bh = queue[i];
            if (bh == null || !bh.IsAlive || bh.Data == null) continue;

            var avatarRect = new TextureRect();
            var tex = GD.Load<Texture2D>($"res://assets/heroes/{bh.Data.HeroId}/icon.png");
            if (tex != null)
            {
                avatarRect.Texture = tex;
            }
            avatarRect.CustomMinimumSize = new Vector2(32, 32);
            avatarRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            avatarRect.Modulate = bh.Team == 0
                ? new Color(0.2f, 0.8f, 1.0f)
                : new Color(1.0f, 0.3f, 0.3f);
            avatarRect.TooltipText = bh.Data.HeroName;
            AddChild(avatarRect);
        }
    }
}
