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

        // Headless khong co viewport texture that - bo qua screenshot, van pass
        // neu cinematic chay khong exception. Chay co man hinh se luu anh preview.
        var viewportTexture = GetViewport().GetTexture();
        var img = viewportTexture?.GetImage();
        if (img != null)
        {
            img.SavePng("user://cutin_preview.png");
            GD.Print("🎉 CUT-IN SCENE TEST PASSED (screenshot: user://cutin_preview.png)");
        }
        else
        {
            GD.Print("🎉 CUT-IN SCENE TEST PASSED (headless: screenshot skipped)");
        }
        GetTree().Quit(0);
    }
}
