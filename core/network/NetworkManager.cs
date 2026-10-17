using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Godot;
using TamQuoc3D.Core.Autoload;
using TamQuoc3D.Data.Resources;
using NetHttpClient = System.Net.Http.HttpClient;
using NetStringContent = System.Net.Http.StringContent;

namespace TamQuoc3D.Core.Network;

/// <summary>
/// NetworkManager - Quản lý kết nối Client - Server giữa game Godot và máy chủ Authoritative Server (PostgreSQL/FastAPI/Redis).
/// Hỗ trợ đồng bộ hồ sơ, ví tiền tệ, lưu trữ đội hình 3x3, triệu hồi Gacha và mô phỏng chiến đấu qua mạng.
/// </summary>
[GlobalClass]
public partial class NetworkManager : Node
{
    public static NetworkManager Instance { get; private set; }

    [Signal] public delegate void ServerConnectedEventHandler(bool isConnected);
    [Signal] public delegate void DataSyncedEventHandler();

    [Export] public string BaseUrl { get; set; } = "http://localhost:8000";
    [Export] public string CurrentUserId { get; set; } = "a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11";

    public bool IsServerOnline { get; private set; } = false;

    private readonly NetHttpClient _http = new() { Timeout = TimeSpan.FromSeconds(5) };
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        // Read custom server URL from OS environment if provided
        string envUrl = OS.GetEnvironment("GAME_SERVER_URL");
        if (!string.IsNullOrEmpty(envUrl))
        {
            BaseUrl = envUrl.TrimEnd('/');
        }

        GD.Print($"🌐 NetworkManager khởi tạo. Target Server: {BaseUrl}");
        _ = InitializeConnectionAsync();
    }

    public async Task<bool> InitializeConnectionAsync()
    {
        bool ok = await CheckHealthAsync();
        IsServerOnline = ok;
        Callable.From(() => EmitSignal(SignalName.ServerConnected, ok)).CallDeferred();

        if (ok)
        {
            GD.Print("✅ Kết nối máy chủ Tam Quốc 3D thành công! Tiến hành đồng bộ dữ liệu...");
            await SyncAllWithServerAsync();
        }
        else
        {
            GD.Print("⚠️ Không thể kết nối tới Game Server. Chuyển sang chế độ Local/Offline.");
        }

        return ok;
    }

    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            var res = await _http.GetAsync($"{BaseUrl}/health");
            if (!res.IsSuccessStatusCode) return false;

            string json = await res.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("status").GetString() == "healthy";
        }
        catch (Exception ex)
        {
            GD.Print($"[NetworkManager] Healthcheck failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SyncAllWithServerAsync()
    {
        if (!IsServerOnline) return false;

        try
        {
            // 1. Fetch Profile
            var profileDto = await FetchProfileAsync();
            if (profileDto != null && GameManager.Instance?.Profile != null)
            {
                var p = GameManager.Instance.Profile;
                p.PlayerName = profileDto.DisplayName;
                p.Level = profileDto.Level;
                p.VipLevel = profileDto.VipLevel;
                p.AvatarHeroId = profileDto.AvatarId;
            }

            // 2. Fetch Wallet
            var walletDto = await FetchWalletAsync();
            if (walletDto != null && GameManager.Instance != null)
            {
                GameManager.Instance.Gold = walletDto.Gold;
                GameManager.Instance.Gems = walletDto.Gold > 0 ? (int)(walletDto.Gold / 10) : 150000;
                GameManager.Instance.Stamina = walletDto.Stamina;

                SafeEmitEventBus(EventBus.SignalName.GoldChanged, 0, (int)GameManager.Instance.Gold);
                SafeEmitEventBus(EventBus.SignalName.DiamondChanged, 0, GameManager.Instance.Gems);
                SafeEmitEventBus(EventBus.SignalName.StaminaChanged, walletDto.Stamina, walletDto.MaxStamina);
            }

            // 3. Fetch Formations
            var formations = await FetchFormationsAsync();
            if (formations != null && formations.Count > 0 && GameManager.Instance != null)
            {
                var pve = formations.Find(f => f.FormationType == "PVE_DEFAULT") ?? formations[0];
                var entries = ConvertGridLayoutToEntries(pve.GridLayout);
                if (entries.Count > 0)
                {
                    GameManager.Instance.CurrentFormation.Clear();
                    foreach (var e in entries)
                    {
                        GameManager.Instance.CurrentFormation.Add(e);
                    }
                }
            }

            Callable.From(() => EmitSignal(SignalName.DataSynced)).CallDeferred();
            SafeEmitEventBus(EventBus.SignalName.SystemNotification, "Máy Chủ", "Đồng bộ dữ liệu tài khoản thành công!", "sync");
            return true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[NetworkManager] Sync failed: {ex.Message}");
            return false;
        }
    }

    public async Task<ProfileDto> FetchProfileAsync()
    {
        try
        {
            var res = await _http.GetAsync($"{BaseUrl}/api/profile?user_id={CurrentUserId}");
            if (!res.IsSuccessStatusCode) return null;

            string json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ProfileDto>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[NetworkManager] FetchProfile failed: {ex.Message}");
            return null;
        }
    }

    public async Task<WalletDto> FetchWalletAsync()
    {
        try
        {
            var res = await _http.GetAsync($"{BaseUrl}/api/wallet?user_id={CurrentUserId}");
            if (!res.IsSuccessStatusCode) return null;

            string json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<WalletDto>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[NetworkManager] FetchWallet failed: {ex.Message}");
            return null;
        }
    }

    public async Task<List<FormationDto>> FetchFormationsAsync()
    {
        try
        {
            var res = await _http.GetAsync($"{BaseUrl}/api/formations?user_id={CurrentUserId}");
            if (!res.IsSuccessStatusCode) return null;

            string json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<FormationDto>>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[NetworkManager] FetchFormations failed: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> SaveFormationFromEntriesAsync(string formationType, IEnumerable<FormationEntry> entries)
    {
        if (!IsServerOnline) return false;

        try
        {
            var layout = new Dictionary<string, List<string>>
            {
                ["front"] = new List<string> { null, null, null },
                ["mid"] = new List<string> { null, null, null },
                ["back"] = new List<string> { null, null, null }
            };

            foreach (var entry in entries)
            {
                string rowKey = entry.Row switch
                {
                    0 => "front",
                    1 => "mid",
                    2 => "back",
                    _ => null
                };

                if (rowKey != null && entry.Col >= 0 && entry.Col < 3)
                {
                    layout[rowKey][entry.Col] = entry.HeroId;
                }
            }

            var payload = new
            {
                formation_type = formationType,
                grid_layout = layout
            };

            string json = JsonSerializer.Serialize(payload, _jsonOptions);
            var content = new NetStringContent(json, Encoding.UTF8, "application/json");

            var res = await _http.PostAsync($"{BaseUrl}/api/formations?user_id={CurrentUserId}", content);
            if (res.IsSuccessStatusCode)
            {
                GD.Print($"[NetworkManager] Đã lưu trận hình {formationType} lên máy chủ PostgreSQL!");
                SafeEmitEventBus(EventBus.SignalName.SystemNotification, "Trận Hình", "Đã lưu đội hình lên máy chủ!", "formation");
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[NetworkManager] SaveFormation failed: {ex.Message}");
            return false;
        }
    }

    public async Task<BattleSimResultDto> SimulateBattleAsync(string battleType = "ASYNC_ARENA", string defenderId = null)
    {
        try
        {
            var payload = new
            {
                battle_type = battleType,
                defender_id = defenderId ?? "b1ffcd88-8b1c-3df7-aa5c-5aa8ac270b22"
            };

            string json = JsonSerializer.Serialize(payload, _jsonOptions);
            var content = new NetStringContent(json, Encoding.UTF8, "application/json");

            var res = await _http.PostAsync($"{BaseUrl}/api/battle/simulate?user_id={CurrentUserId}", content);
            if (!res.IsSuccessStatusCode) return null;

            string resJson = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<BattleSimResultDto>(resJson, _jsonOptions);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[NetworkManager] SimulateBattle failed: {ex.Message}");
            return null;
        }
    }

    public async Task<GachaResultDto> PullGachaAsync(string bannerId, int pullCount)
    {
        try
        {
            var payload = new
            {
                banner_id = bannerId,
                pull_count = pullCount
            };

            string json = JsonSerializer.Serialize(payload, _jsonOptions);
            var content = new NetStringContent(json, Encoding.UTF8, "application/json");

            var res = await _http.PostAsync($"{BaseUrl}/api/gacha/pull?user_id={CurrentUserId}", content);
            if (!res.IsSuccessStatusCode) return null;

            string resJson = await res.Content.ReadAsStringAsync();
            var dto = JsonSerializer.Deserialize<GachaResultDto>(resJson, _jsonOptions);

            // Re-sync wallet after pull
            _ = FetchWalletAsync().ContinueWith(t =>
            {
                if (t.Result != null && GameManager.Instance != null)
                {
                    GameManager.Instance.Gold = t.Result.Gold;
                    SafeEmitEventBus(EventBus.SignalName.GoldChanged, 0, (int)t.Result.Gold);
                }
            });

            return dto;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[NetworkManager] PullGacha failed: {ex.Message}");
            return null;
        }
    }

    private void SafeEmitEventBus(StringName signalName, params Variant[] args)
    {
        if (EventBus.Instance == null) return;
        Callable.From(() =>
        {
            if (EventBus.Instance == null) return;
            switch (args.Length)
            {
                case 0:
                    EventBus.Instance.EmitSignal(signalName);
                    break;
                case 1:
                    EventBus.Instance.EmitSignal(signalName, args[0]);
                    break;
                case 2:
                    EventBus.Instance.EmitSignal(signalName, args[0], args[1]);
                    break;
                case 3:
                    EventBus.Instance.EmitSignal(signalName, args[0], args[1], args[2]);
                    break;
                default:
                    EventBus.Instance.EmitSignal(signalName, args);
                    break;
            }
        }).CallDeferred();
    }

    private List<FormationEntry> ConvertGridLayoutToEntries(Dictionary<string, List<string>> layout)
    {
        var entries = new List<FormationEntry>();
        if (layout == null) return entries;

        void ParseRow(string rowKey, int rowIdx)
        {
            if (layout.TryGetValue(rowKey, out var heroes) && heroes != null)
            {
                for (int col = 0; col < heroes.Count; col++)
                {
                    string hId = heroes[col];
                    if (!string.IsNullOrEmpty(hId))
                    {
                        entries.Add(new FormationEntry(hId, rowIdx, col));
                    }
                }
            }
        }

        ParseRow("front", 0);
        ParseRow("mid", 1);
        ParseRow("back", 2);

        return entries;
    }

    // -----------------------------------------------------------------------
    // DTO CLASSES
    // -----------------------------------------------------------------------
    public class ProfileDto
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string AvatarId { get; set; }
        public int Level { get; set; }
        public int VipLevel { get; set; }
        public long CombatPower { get; set; }
    }

    public class WalletDto
    {
        public long Silver { get; set; }
        public long Gold { get; set; }
        public long PaidGold { get; set; }
        public int Stamina { get; set; }
        public int MaxStamina { get; set; }
        public int ArenaTokens { get; set; }
    }

    public class FormationDto
    {
        public string FormationType { get; set; }
        public Dictionary<string, List<string>> GridLayout { get; set; }
        public bool IsActive { get; set; }
    }

    public class GachaItemDto
    {
        public string HeroId { get; set; }
        public string Rarity { get; set; }
        public int PityUr { get; set; }
        public int PitySsr { get; set; }
    }

    public class GachaResultDto
    {
        public List<GachaItemDto> Results { get; set; }
        public int PityUr { get; set; }
        public int PitySsr { get; set; }
        public int GoldSpent { get; set; }
    }

    public class BattleSimResultDto
    {
        public string ReplayId { get; set; }
        public long Seed { get; set; }
        public bool AttackerWin { get; set; }
        public int Rounds { get; set; }
        public List<string> Log { get; set; }
    }
}
