using System;
using Godot;
using TamQuoc3D.Core.Navigation;
using TamQuoc3D.Core.Network;

namespace TamQuoc3D.Features.Auth;

[GlobalClass]
public partial class LoginController : Control
{
    private LineEdit _usernameInput;
    private LineEdit _passwordInput;
    private LineEdit _displayNameInput;
    private Label _statusLabel;
    private Button _submitButton;
    private Button _modeButton;
    private NetworkManager _networkManager;
    private bool _isRegisterMode;
    private bool _isSubmitting;
    private bool _sessionRestoreAttempted;

    public override void _Ready()
    {
        BuildLayout();
        _networkManager = NetworkManager.Instance;
        if (_networkManager == null)
        {
            SetStatus("Không tìm thấy NetworkManager.");
            _submitButton.Disabled = true;
            return;
        }

        _networkManager.DatabaseConnectionChanged += OnDatabaseConnectionChanged;
        _ = InitializeLoginAsync();
    }

    public override void _ExitTree()
    {
        if (_networkManager != null && GodotObject.IsInstanceValid(_networkManager))
        {
            _networkManager.DatabaseConnectionChanged -= OnDatabaseConnectionChanged;
        }
    }

    private void BuildLayout()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(new ColorRect
        {
            Color = new Color(0.055f, 0.04f, 0.075f),
            MouseFilter = MouseFilterEnum.Ignore,
            AnchorsPreset = (int)LayoutPreset.FullRect,
            AnchorRight = 1,
            AnchorBottom = 1
        });

        var center = new CenterContainer
        {
            AnchorsPreset = (int)LayoutPreset.FullRect,
            AnchorRight = 1,
            AnchorBottom = 1
        };
        AddChild(center);

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(440, 0)
        };
        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.09f, 0.15f, 0.98f),
            BorderColor = new Color(0.72f, 0.53f, 0.22f),
            BorderWidthBottom = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            BorderWidthTop = 2,
            ContentMarginLeft = 30,
            ContentMarginRight = 30,
            ContentMarginTop = 26,
            ContentMarginBottom = 26
        };
        panel.AddThemeStyleboxOverride("panel", panelStyle);
        center.AddChild(panel);

        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 14);
        panel.AddChild(content);

        var title = new Label
        {
            Text = "TAM QUỐC",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 28);
        title.Modulate = new Color(0.96f, 0.8f, 0.42f);
        content.AddChild(title);

        var subtitle = new Label
        {
            Text = "Đăng nhập để vào Đại Bản Doanh",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        subtitle.AddThemeFontSizeOverride("font_size", 15);
        content.AddChild(subtitle);

        _displayNameInput = CreateInput("Tên hiển thị", false);
        _displayNameInput.Visible = false;
        content.AddChild(_displayNameInput);

        _usernameInput = CreateInput("Tên tài khoản", false);
        content.AddChild(_usernameInput);

        _passwordInput = CreateInput("Mật khẩu (tối thiểu 4 ký tự)", true);
        content.AddChild(_passwordInput);

        _statusLabel = new Label
        {
            Text = "Đang kết nối máy chủ...",
            AutowrapMode = TextServer.AutowrapMode.Word
        };
        _statusLabel.AddThemeFontSizeOverride("font_size", 13);
        _statusLabel.CustomMinimumSize = new Vector2(360, 38);
        content.AddChild(_statusLabel);

        _submitButton = new Button
        {
            Text = "ĐĂNG NHẬP",
            CustomMinimumSize = new Vector2(0, 48),
            Disabled = true
        };
        _submitButton.Pressed += OnSubmitPressed;
        content.AddChild(_submitButton);

        _modeButton = new Button
        {
            Text = "Chưa có tài khoản? Đăng ký",
            Flat = true
        };
        _modeButton.Pressed += ToggleMode;
        content.AddChild(_modeButton);
    }

    private static LineEdit CreateInput(string placeholder, bool secret)
    {
        return new LineEdit
        {
            PlaceholderText = placeholder,
            Secret = secret,
            CustomMinimumSize = new Vector2(0, 42),
            ClearButtonEnabled = true
        };
    }

    private async System.Threading.Tasks.Task InitializeLoginAsync()
    {
        bool connected = await _networkManager.CheckHealthAsync();
        UpdateConnectionState(connected);
        if (!connected)
        {
            return;
        }

        await TryRestoreSessionAsync();
    }

    private void OnDatabaseConnectionChanged(bool connected)
    {
        if (!GodotObject.IsInstanceValid(this)) return;
        UpdateConnectionState(connected);
        if (connected && _networkManager.HasSavedSession)
        {
            _ = TryRestoreSessionAsync();
        }
    }

    private async System.Threading.Tasks.Task TryRestoreSessionAsync()
    {
        if (_sessionRestoreAttempted || !_networkManager.HasSavedSession) return;
        _sessionRestoreAttempted = true;
        var restoreResult = await _networkManager.TryRestoreSessionAsync();
        if (restoreResult.Success)
        {
            SceneNavigator.GoToHub();
        }
        else if (!string.IsNullOrEmpty(restoreResult.Message))
        {
            SetStatus(restoreResult.Message);
        }
    }

    private void UpdateConnectionState(bool connected)
    {
        _submitButton.Disabled = !connected || _isSubmitting;
        if (connected)
        {
            SetStatus("Máy chủ sẵn sàng. Hãy đăng nhập hoặc tạo tài khoản mới.");
        }
        else
        {
            SetStatus("Không kết nối được máy chủ. Đang thử kết nối lại...");
        }
    }

    private void ToggleMode()
    {
        if (_isSubmitting) return;
        _isRegisterMode = !_isRegisterMode;
        _displayNameInput.Visible = _isRegisterMode;
        _submitButton.Text = _isRegisterMode ? "TẠO TÀI KHOẢN" : "ĐĂNG NHẬP";
        _modeButton.Text = _isRegisterMode
            ? "Đã có tài khoản? Đăng nhập"
            : "Chưa có tài khoản? Đăng ký";
        SetStatus(_isRegisterMode
            ? "Tạo tài khoản mới để bắt đầu cuộc hành trình."
            : "Đăng nhập bằng tên tài khoản và mật khẩu.");
    }

    private async void OnSubmitPressed()
    {
        if (_isSubmitting) return;

        string username = _usernameInput.Text.Trim();
        string password = _passwordInput.Text;
        string displayName = _displayNameInput.Text.Trim();
        if (username.Length < 3 || password.Length < 4 ||
            (_isRegisterMode && string.IsNullOrWhiteSpace(displayName)))
        {
            SetStatus(_isRegisterMode
                ? "Nhập tên tài khoản, tên hiển thị và mật khẩu ít nhất 4 ký tự."
                : "Nhập tên tài khoản và mật khẩu ít nhất 4 ký tự.");
            return;
        }

        _isSubmitting = true;
        _submitButton.Disabled = true;
        _modeButton.Disabled = true;
        SetStatus(_isRegisterMode ? "Đang tạo tài khoản..." : "Đang đăng nhập...");

        NetworkManager.AuthOperationResult result = _isRegisterMode
            ? await _networkManager.RegisterAsync(username, password, displayName)
            : await _networkManager.LoginAsync(username, password);

        if (!result.Success)
        {
            SetStatus(result.Message);
            FinishSubmitting();
            return;
        }

        SetStatus("Đang tải dữ liệu người chơi...");
        if (!await _networkManager.SyncPlayerDataAsync())
        {
            SetStatus("Đã xác thực nhưng chưa tải được dữ liệu. Kiểm tra kết nối rồi thử đăng nhập lại.");
            await _networkManager.LogoutAsync();
            FinishSubmitting();
            return;
        }

        SceneNavigator.GoToHub();
    }

    private void FinishSubmitting()
    {
        _isSubmitting = false;
        _submitButton.Disabled = !_networkManager.IsDatabaseConnected;
        _modeButton.Disabled = false;
    }

    private void SetStatus(string text)
    {
        if (_statusLabel != null)
        {
            _statusLabel.Text = text;
        }
    }
}
