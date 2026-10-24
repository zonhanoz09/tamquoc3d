using Godot;
using System;
using TamQuoc3D.Core.Autoload;
using TamQuoc3D.Core.Network;

/// <summary>
/// Thanh kinh nghiệm đáy hub (§6): full-width, cao 16px, sát đáy màn hình
/// (y 704–720), nằm dưới cả ChatBox lẫn BottomNavBar.
/// Hiển thị Lv + tổng EXP thật từ server (user_profiles.exp, đồng bộ qua
/// NetworkManager.SyncPlayerDataAsync).
///
/// LƯU Ý TRUNG THỰC: server hiện KHÔNG định nghĩa công thức EXP lên cấp cho
/// người chơi (level/exp trong user_profiles không được cập nhật trong gameplay,
/// không có hàm curve nào phía server — khác với tướng dùng 300 EXP/cấp).
/// Tỉ lệ thanh tạm tính theo mốc 300 EXP/cấp (quy ước hiển thị, cùng mốc với
/// tướng) cho tới khi server định nghĩa công thức riêng — khi đó chỉ cần đổi
/// hằng số ExpPerLevel này. Offline/demo: chỉ hiện Lv, không vẽ tỉ lệ giả
/// (thanh để 0 khi chưa có EXP thật).
/// </summary>
[GlobalClass]
public partial class ExpBar : PanelContainer
{
    public const int ExpPerLevel = 300;

    private Label _levelLabel;
    private ProgressBar _bar;
    private Label _expLabel;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(1280, 16);
        Size = new Vector2(1280, 16);
        MouseFilter = MouseFilterEnum.Ignore;

        var bg = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.05f, 0.1f, 0.9f)
        };
        AddThemeStyleboxOverride("panel", bg);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 8);
        AddChild(hbox);

        _levelLabel = new Label();
        _levelLabel.AddThemeFontSizeOverride("font_size", 11);
        _levelLabel.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.4f));
        hbox.AddChild(_levelLabel);

        _bar = new ProgressBar();
        _bar.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _bar.CustomMinimumSize = new Vector2(0, 10);
        _bar.ShowPercentage = false;
        _bar.MaxValue = ExpPerLevel;
        _bar.AddThemeStyleboxOverride("background", new StyleBoxFlat
        {
            BgColor = new Color(0.15f, 0.15f, 0.2f, 1f)
        });
        _bar.AddThemeStyleboxOverride("fill", new StyleBoxFlat
        {
            BgColor = new Color(1f, 0.75f, 0.2f, 1f)
        });
        hbox.AddChild(_bar);

        _expLabel = new Label();
        _expLabel.AddThemeFontSizeOverride("font_size", 11);
        _expLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.9f));
        hbox.AddChild(_expLabel);

        RefreshFromGameManager();
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.DataSynced += RefreshFromGameManager;
        }
    }

    public void RefreshFromGameManager()
    {
        var profile = GameManager.Instance?.Profile;
        if (profile == null) return;
        _levelLabel.Text = $"Lv.{profile.Level}";
        var withinLevel = (int)(Math.Max(0, profile.Exp) % ExpPerLevel);
        _bar.Value = withinLevel;
        _expLabel.Text = $"EXP {profile.Exp:N0}";
    }
}
