using System.Threading.Tasks;
using Godot;
using TamQuoc3D.Data.Database;
using TamQuoc3D.Data.Resources;
using TamQuoc3D.Features.Battle.Entities;

namespace TamQuoc3D.Tests;

public partial class TestCutInScene : Node2D
{
    public override void _Ready()
    {
        RunTestAsync();
    }

    private async void RunTestAsync()
    {
        // Background
        var bg = new Sprite2D();
        var bgTex = GD.Load<Texture2D>("res://assets/environments/battlefields/dau_truong/battlefield.png");
        if (bgTex != null) bg.Texture = bgTex;
        bg.Position = new Vector2(640, 360);
        AddChild(bg);

        var db = new HeroDatabase();
        db.GenerateDefaultHeroDatabase();
        var hero = db.GetHero("trieu_van");
        var skill = hero.Skills[1]; // Rage skill

        var uiLayer = new CanvasLayer();
        AddChild(uiLayer);

        _ = HeroCutInCinematic.PlayAsync(uiLayer, hero, skill, false, 1.0f);

        // Wait for slide-in animation to reach center (durIn = 0.20s, hold = 0.45s)
        await ToSignal(GetTree().CreateTimer(0.35f), SceneTreeTimer.SignalName.Timeout);

        var img = GetViewport().GetTexture().GetImage();
        img.SavePng("/home/annhan/.gemini/antigravity-acp/brain/e35a8943-7e35-41e5-bb56-e9936734cac2/live_cutin_cinematic_preview.png");
        GD.Print("SAVED_CUTIN_SCREENSHOT");
    }
}
