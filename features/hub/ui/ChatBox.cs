using System.Collections.Generic;
using Godot;
using TamQuoc3D.Core.Autoload;

namespace TamQuoc3D.Features.Hub.Ui;

/// <summary>
/// ChatBox - Khung chat thu nhỏ phong cách MMO chiến thuật góc dưới bên trái,
/// hỗ trợ các kênh Thế Giới, Bang Hội, Hệ Thống, và nhắn tin tương tác.
/// </summary>
[GlobalClass]
public partial class ChatBox : PanelContainer
{
    private ScrollContainer _scroll;
    private VBoxContainer _messageList;
    private LineEdit _inputField;
    private Button _sendBtn;
    private OptionButton _channelSelector;

    public override void _Ready()
    {
        BuildLayout();
        ConnectSignals();
        SeedInitialMessages();
    }

    private void BuildLayout()
    {
        CustomMinimumSize = new Vector2(360, 130);
        Size = new Vector2(360, 130);

        var styleBox = new StyleBoxTexture
        {
            Texture = GD.Load<Texture2D>("res://assets/ui/hub/chat_box_bg.png")
        };
        AddThemeStyleboxOverride("panel", styleBox);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_top", 6);
        margin.AddThemeConstantOverride("margin_bottom", 6);
        AddChild(margin);

        var vBox = new VBoxContainer();
        vBox.AddThemeConstantOverride("separation", 4);
        margin.AddChild(vBox);

        // Header / Channel selector row
        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 6);
        vBox.AddChild(header);

        var channelLabel = new Label
        {
            Text = "Kênh:",
            Modulate = new Color(0.85f, 0.8f, 0.7f)
        };
        channelLabel.AddThemeFontSizeOverride("font_size", 10);
        header.AddChild(channelLabel);

        _channelSelector = new OptionButton
        {
            CustomMinimumSize = new Vector2(90, 20)
        };
        _channelSelector.AddThemeFontSizeOverride("font_size", 10);
        _channelSelector.AddItem("Thế Giới", 0);
        _channelSelector.AddItem("Bang Hội", 1);
        _channelSelector.AddItem("Hệ Thống", 2);
        _channelSelector.Select(0);
        header.AddChild(_channelSelector);

        // Scrollable message list
        _scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(344, 64),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        vBox.AddChild(_scroll);

        _messageList = new VBoxContainer();
        _messageList.AddThemeConstantOverride("separation", 2);
        _messageList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _scroll.AddChild(_messageList);

        // Input & Send row
        var inputRow = new HBoxContainer();
        inputRow.AddThemeConstantOverride("separation", 4);
        vBox.AddChild(inputRow);

        _inputField = new LineEdit
        {
            PlaceholderText = "Nhập nội dung chat...",
            CustomMinimumSize = new Vector2(275, 24),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _inputField.AddThemeFontSizeOverride("font_size", 11);
        _inputField.TextSubmitted += (text) => SendMessage();
        inputRow.AddChild(_inputField);

        _sendBtn = new Button
        {
            Text = "Gửi",
            CustomMinimumSize = new Vector2(50, 24)
        };
        _sendBtn.AddThemeFontSizeOverride("font_size", 11);
        var sendBtnStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.6f, 0.2f, 0.25f),
            BorderColor = new Color(0.9f, 0.75f, 0.3f),
            BorderWidthBottom = 1,
            BorderWidthTop = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            CornerRadiusBottomLeft = 3,
            CornerRadiusBottomRight = 3,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3
        };
        _sendBtn.AddThemeStyleboxOverride("normal", sendBtnStyle);
        _sendBtn.Pressed += SendMessage;
        inputRow.AddChild(_sendBtn);
    }

    private void SeedInitialMessages()
    {
        // Chi tin he thong that - khong gia lap tin nhan nguoi choi khac.
        AddMessage("Hệ Thống", "Chào mừng Chúa Công bước vào đại nghiệp phân tranh Tam Quốc!", "sys");
        AddMessage("Hệ Thống", "Kênh Hệ Thống hiển thị thông báo trong game. Chat Thế Giới sắp ra mắt.", "sys");
    }

    private void ConnectSignals()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.ChatMessageReceived += AddMessage;
        }
    }

    public void AddMessage(string sender, string message, string channel)
    {
        var lbl = new RichTextLabel
        {
            FitContent = true,
            BbcodeEnabled = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Pass
        };
        lbl.AddThemeFontSizeOverride("normal_font_size", 10);

        string channelTag = channel switch
        {
            "sys" or "Hệ Thống" => "[color=#ffd700][Hệ Thống][/color]",
            "guild" or "Bang Hội" => "[color=#40c0ff][Bang][/color]",
            _ => "[color=#ff9040][Thế Giới][/color]"
        };

        string senderColor = channel switch
        {
            "sys" or "Hệ Thống" => "#ffdd40",
            "guild" or "Bang Hội" => "#60e0ff",
            _ => "#fffae0"
        };

        lbl.Text = $"{channelTag} [color={senderColor}][b]{sender}[/b][/color]: {message}";
        _messageList.AddChild(lbl);

        // Keep maximum 30 messages
        while (_messageList.GetChildCount() > 30)
        {
            _messageList.GetChild(0).QueueFree();
        }

        // Auto scroll to bottom on next frame
        Callable.From(() =>
        {
            _scroll.ScrollVertical = (int)_scroll.GetVScrollBar().MaxValue;
        }).CallDeferred();
    }

    private void SendMessage()
    {
        string text = _inputField.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        // Chua co server chat realtime: khong gia lap gui thanh cong.
        _inputField.Clear();
        AddMessage("Hệ Thống", "Chat thế giới sắp ra mắt — tin nhắn chưa được gửi.", "sys");
        EventBus.Instance?.EmitSignal(EventBus.SignalName.SystemNotification,
            "Chat", "Chat thế giới sắp ra mắt.", "info");
    }
}
