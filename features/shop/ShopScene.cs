using System.Collections.Generic;
using Godot;
using TamQuoc3D.Core.Autoload;
using TamQuoc3D.Core.Navigation;
using TamQuoc3D.Core.Network;
using TamQuoc3D.Data.Database;
using TamQuoc3D.Data.Resources;

namespace TamQuoc3D.Features.Shop;

/// <summary>
/// ShopScene (Phase 4) - Cua hang + Tui do:
/// Danh muc lay tu server (shop_items) - gia va vat pham nhan duoc do server quyet dinh.
/// Mua truc tiep tru vi server; vat pham ITEM vao user_inventories.
/// Phan Tui do: liet ke vat pham, dung Sach Kinh Nghiem (+300 EXP) cho tuong chon.
/// Che do offline: dung danh muc du phong local va vi/tui do local cua GameManager.
/// </summary>
[GlobalClass]
public partial class ShopScene : Control
{
    private sealed class LocalShopItem
    {
        public string ItemId;
        public string Name;
        public string Description;
        public string Currency;
        public int Price;
        public string GrantType;
        public int GrantAmount;
        public string GrantItemId;
    }

    private static readonly List<LocalShopItem> FallbackCatalog =
    [
        new() { ItemId = "the_luc_dan", Name = "Thể Lực Đan", Description = "Hồi ngay 30 Thể Lực", Currency = "gold", Price = 20, GrantType = "STAMINA", GrantAmount = 30 },
        new() { ItemId = "the_luc_dai", Name = "Đại Thể Lực Đan", Description = "Hồi ngay 120 Thể Lực", Currency = "gold", Price = 70, GrantType = "STAMINA", GrantAmount = 120 },
        new() { ItemId = "tui_bac_lon", Name = "Túi Bạc Lớn", Description = "Nhận ngay 5.000 Bạc", Currency = "gold", Price = 45, GrantType = "SILVER", GrantAmount = 5000 },
        new() { ItemId = "sach_exp", Name = "Sách Kinh Nghiệm", Description = "Vào Túi Đồ: dùng cho 1 tướng +300 EXP", Currency = "silver", Price = 400, GrantType = "ITEM", GrantAmount = 1, GrantItemId = "sach_exp" }
    ];

    private Label _walletLabel;
    private Label _statusLabel;
    private GridContainer _shopGrid;
    private VBoxContainer _inventoryList;
    private OptionButton _heroPicker;

    public override void _Ready()
    {
        BuildUi();
        _ = LoadShopAsync();
        _ = LoadInventoryAsync();
        RefreshWallet();
    }

    private bool Online => NetworkManager.Instance != null
        && NetworkManager.Instance.IsServerOnline
        && NetworkManager.Instance.IsAuthenticated;

    // ==================== UI ====================

    private void BuildUi()
    {
        var bg = new ColorRect { Color = new Color(0.08f, 0.1f, 0.07f) };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        AddChild(margin);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 10);
        margin.AddChild(root);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 12);
        root.AddChild(header);

        var backBtn = new Button { Text = "⬅ Quay lại", CustomMinimumSize = new Vector2(110, 40) };
        backBtn.Pressed += () => SceneNavigator.GoBack();
        header.AddChild(backBtn);

        var title = new Label { Text = "🛒 CỬA HÀNG" };
        title.AddThemeFontSizeOverride("font_size", 26);
        title.AddThemeColorOverride("font_color", new Color(0.6f, 1.0f, 0.6f));
        header.AddChild(title);

        header.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.Expand });
        _walletLabel = new Label();
        _walletLabel.AddThemeFontSizeOverride("font_size", 16);
        header.AddChild(_walletLabel);

        _statusLabel = new Label { Text = "" };
        _statusLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.9f, 1.0f));
        root.AddChild(_statusLabel);

        _shopGrid = new GridContainer { Columns = 4 };
        _shopGrid.AddThemeConstantOverride("h_separation", 10);
        _shopGrid.AddThemeConstantOverride("v_separation", 10);
        root.AddChild(_shopGrid);

        var invTitle = new Label { Text = "🎒 TÚI ĐỒ" };
        invTitle.AddThemeFontSizeOverride("font_size", 20);
        invTitle.AddThemeColorOverride("font_color", new Color(1.0f, 0.85f, 0.4f));
        root.AddChild(invTitle);

        var pickerRow = new HBoxContainer();
        pickerRow.AddThemeConstantOverride("separation", 8);
        root.AddChild(pickerRow);
        var pickerLabel = new Label { Text = "Dùng Sách EXP cho tướng:" };
        pickerRow.AddChild(pickerLabel);
        _heroPicker = new OptionButton { CustomMinimumSize = new Vector2(220, 36) };
        pickerRow.AddChild(_heroPicker);
        RefreshHeroPicker();

        var invScroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.Expand,
            SizeFlagsVertical = SizeFlags.Expand
        };
        root.AddChild(invScroll);
        _inventoryList = new VBoxContainer();
        _inventoryList.AddThemeConstantOverride("separation", 6);
        invScroll.AddChild(_inventoryList);
    }

    private void RefreshHeroPicker()
    {
        _heroPicker.Clear();
        var manager = GameManager.Instance;
        if (manager == null) return;
        foreach (var hero in manager.OwnedHeroes)
        {
            if (hero != null) _heroPicker.AddItem(hero.HeroName);
        }
        if (_heroPicker.ItemCount == 0 && HeroDatabase.Instance != null)
        {
            foreach (var hero in HeroDatabase.Instance.GetAllHeroes())
            {
                _heroPicker.AddItem(hero.HeroName);
            }
        }
    }

    private string PickedHeroId()
    {
        var manager = GameManager.Instance;
        int idx = _heroPicker.Selected;
        if (manager != null && idx >= 0 && idx < manager.OwnedHeroes.Count)
        {
            return manager.OwnedHeroes[idx]?.HeroId;
        }
        if (HeroDatabase.Instance != null)
        {
            var all = new List<HeroData>(HeroDatabase.Instance.GetAllHeroes());
            if (idx >= 0 && idx < all.Count)
            {
                return all[idx].HeroId;
            }
        }
        return null;
    }

    // ==================== Shop ====================

    private async System.Threading.Tasks.Task LoadShopAsync()
    {
        var items = new List<LocalShopItem>();
        if (Online)
        {
            var remote = await NetworkManager.Instance.FetchShopItemsAsync();
            foreach (var dto in remote)
            {
                items.Add(new LocalShopItem
                {
                    ItemId = dto.ItemId,
                    Name = dto.Name,
                    Description = dto.Description,
                    Currency = dto.Currency,
                    Price = dto.Price,
                    GrantType = dto.GrantType,
                    GrantAmount = dto.GrantAmount,
                    GrantItemId = dto.GrantItemId
                });
            }
        }
        if (items.Count == 0) items.AddRange(FallbackCatalog);

        foreach (var child in _shopGrid.GetChildren())
        {
            _shopGrid.RemoveChild(child);
            child.QueueFree();
        }
        foreach (var item in items)
        {
            _shopGrid.AddChild(BuildShopCard(item));
        }
    }

    private Control BuildShopCard(LocalShopItem item)
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(240, 150) };
        var sb = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.15f, 0.11f, 0.98f),
            BorderColor = new Color(0.45f, 0.7f, 0.4f)
        };
        sb.SetBorderWidthAll(2);
        sb.SetCornerRadiusAll(10);
        sb.ContentMarginLeft = 12;
        sb.ContentMarginRight = 12;
        sb.ContentMarginTop = 10;
        sb.ContentMarginBottom = 10;
        panel.AddThemeStyleboxOverride("panel", sb);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 6);
        panel.AddChild(vbox);

        var nameLabel = new Label { Text = item.Name };
        nameLabel.AddThemeFontSizeOverride("font_size", 17);
        vbox.AddChild(nameLabel);

        var descLabel = new Label
        {
            Text = item.Description,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        descLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.85f, 0.8f));
        vbox.AddChild(descLabel);

        vbox.AddChild(new Control { SizeFlagsVertical = SizeFlags.Expand });

        string currencyIcon = item.Currency == "gold" ? "💎" : "🪙";
        var buyBtn = new Button
        {
            Text = $"Mua — {item.Price:N0} {currencyIcon}",
            CustomMinimumSize = new Vector2(0, 40)
        };
        buyBtn.Pressed += async () => await OnBuy(item);
        vbox.AddChild(buyBtn);
        return panel;
    }

    private async System.Threading.Tasks.Task OnBuy(LocalShopItem item)
    {
        if (Online)
        {
            var result = await NetworkManager.Instance.BuyShopItemAsync(item.ItemId, 1);
            if (result == null)
            {
                _statusLabel.Text = "❌ Mua thất bại (không đủ tiền hoặc lỗi server).";
                return;
            }
            _statusLabel.Text = $"✅ Đã mua {item.Name} (-{result.TotalCost:N0} {(result.Currency == "gold" ? "💎" : "🪙")}).";
            await NetworkManager.Instance.FetchWalletAsync();
        }
        else if (!BuyOffline(item))
        {
            _statusLabel.Text = "❌ Không đủ tiền để mua (offline).";
            return;
        }
        else
        {
            _statusLabel.Text = $"✅ Đã mua {item.Name} (offline).";
        }

        RefreshWallet();
        _ = LoadInventoryAsync();
    }

    private bool BuyOffline(LocalShopItem item)
    {
        var manager = GameManager.Instance;
        if (manager == null) return false;
        bool paid = item.Currency == "gold"
            ? manager.SpendGems(item.Price)
            : manager.SpendGold(item.Price);
        if (!paid) return false;

        switch (item.GrantType)
        {
            case "STAMINA":
                manager.Stamina = System.Math.Min(manager.MaxStamina, manager.Stamina + item.GrantAmount);
                break;
            case "SILVER":
                manager.AddGold(item.GrantAmount);
                break;
            case "ITEM":
                string key = item.GrantItemId ?? item.ItemId;
                manager.LocalInventory[key] = manager.LocalInventory.GetValueOrDefault(key) + item.GrantAmount;
                break;
        }
        return true;
    }

    // ==================== Inventory ====================

    private async System.Threading.Tasks.Task LoadInventoryAsync()
    {
        foreach (var child in _inventoryList.GetChildren())
        {
            _inventoryList.RemoveChild(child);
            child.QueueFree();
        }

        var rows = new List<(string ItemId, long Quantity)>();
        if (Online)
        {
            var remote = await NetworkManager.Instance.FetchInventoryAsync();
            foreach (var dto in remote) rows.Add((dto.ItemId, dto.Quantity));
        }
        else
        {
            var manager = GameManager.Instance;
            if (manager != null)
            {
                foreach (var (itemId, qty) in manager.LocalInventory)
                {
                    if (qty > 0) rows.Add((itemId, qty));
                }
            }
        }

        if (rows.Count == 0)
        {
            _inventoryList.AddChild(new Label { Text = "Túi đồ trống. Mua Sách Kinh Nghiệm ở cửa hàng để tăng EXP tướng!" });
            return;
        }

        foreach (var (itemId, qty) in rows)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 10);
            var label = new Label
            {
                Text = $"{ItemDisplayName(itemId)}  x{qty}",
                CustomMinimumSize = new Vector2(320, 36)
            };
            row.AddChild(label);
            if (itemId == "sach_exp")
            {
                var useBtn = new Button { Text = "📖 Dùng (+300 EXP)", CustomMinimumSize = new Vector2(170, 36) };
                useBtn.Pressed += async () => await OnUseExpBook();
                row.AddChild(useBtn);
            }
            _inventoryList.AddChild(row);
        }
    }

    private async System.Threading.Tasks.Task OnUseExpBook()
    {
        string heroId = PickedHeroId();
        if (string.IsNullOrEmpty(heroId))
        {
            _statusLabel.Text = "⚠️ Chưa chọn tướng để dùng Sách EXP.";
            return;
        }

        if (Online)
        {
            var result = await NetworkManager.Instance.UseItemAsync("sach_exp", heroId);
            if (result == null)
            {
                _statusLabel.Text = "❌ Dùng vật phẩm thất bại.";
                return;
            }
            _statusLabel.Text = $"📖 +{result.ExpGranted} EXP! Tướng đạt cấp {result.Level}.";
        }
        else
        {
            var manager = GameManager.Instance;
            if (manager == null || manager.LocalInventory.GetValueOrDefault("sach_exp") <= 0)
            {
                _statusLabel.Text = "❌ Không còn Sách EXP trong túi (offline).";
                return;
            }
            manager.LocalInventory["sach_exp"] -= 1;
            if (!manager.HeroProgressById.TryGetValue(heroId, out var progress))
            {
                progress = new GameManager.HeroProgress();
                manager.HeroProgressById[heroId] = progress;
            }
            progress.Exp += 300;
            progress.Level = System.Math.Min(60, 1 + (int)(progress.Exp / GameManager.ExpPerLevel));
            _statusLabel.Text = $"📖 +300 EXP! Tướng đạt cấp {progress.Level} (offline).";
        }

        _ = LoadInventoryAsync();
    }

    private void RefreshWallet()
    {
        var manager = GameManager.Instance;
        if (manager == null) return;
        _walletLabel.Text = $"🪙 {manager.Gold:N0} Bạc    💎 {manager.Gems:N0} Kim Cương    ⚡ {manager.Stamina}/{manager.MaxStamina}";
    }

    private static string ItemDisplayName(string itemId) => itemId switch
    {
        "sach_exp" => "📖 Sách Kinh Nghiệm",
        _ => itemId
    };
}
