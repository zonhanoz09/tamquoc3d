using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Godot;
using TamQuoc3D.Core.Autoload;
using TamQuoc3D.Data.Database;
using TamQuoc3D.Data.Resources;
using System.Net.Http.Headers;

// Prevent CS0104 ambiguity between Godot.HttpClient and System.Net.Http.HttpClient
using NetHttpClient = System.Net.Http.HttpClient;
using NetStringContent = System.Net.Http.StringContent;

namespace TamQuoc3D.Core.Network;

/// <summary>
/// NetworkManager - Autoload giao tiep voi Backend Game Server (ASP.NET Core 8 + PostgreSQL 16 + Redis 7).
/// Quan ly Auth, Wallet, Formations, Campaign Progress, Gacha va Replay chien dau.
/// </summary>
[GlobalClass]
public partial class NetworkManager : Node
{
    public static NetworkManager Instance { get; private set; }

    [Signal] public delegate void ServerConnectedEventHandler(bool isConnected);
    [Signal] public delegate void ServerDisconnectedEventHandler();
    [Signal] public delegate void DatabaseConnectionChangedEventHandler(bool isConnected);
    [Signal] public delegate void DataSyncedEventHandler();

    [Export] public string BaseUrl { get; set; } = "http://localhost:8000";
    public string CurrentUserId { get; private set; } = string.Empty;
    public bool IsAuthenticated => !string.IsNullOrEmpty(_sessionToken);
    public bool HasSavedSession => FileAccess.FileExists(SessionPath);

    public bool IsServerOnline { get; private set; } = false;
    public bool IsDatabaseConnected { get; private set; } = false;

    private NetHttpClient _http;
    private Timer _healthRetryTimer;
    private Task<bool> _healthCheckTask;
    private const string SessionPath = "user://auth_session.json";
    private string _sessionToken = string.Empty;
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
        _http = new NetHttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        GD.Print($"🌐 NetworkManager khởi tạo. Target Server: {BaseUrl}");
        _ = CheckHealthAsync();

        _healthRetryTimer = new Timer
        {
            WaitTime = 5,
            Autostart = true
        };
        _healthRetryTimer.Timeout += RefreshHealthStatus;
        AddChild(_healthRetryTimer);
    }

    public override void _ExitTree()
    {
        if (_healthRetryTimer != null)
        {
            _healthRetryTimer.Timeout -= RefreshHealthStatus;
            _healthRetryTimer.Stop();
        }

        _http?.Dispose();
        if (Instance == this) Instance = null;
    }

    private async void RefreshHealthStatus()
    {
        await CheckHealthAsync();
    }

    public Task<bool> CheckHealthAsync()
    {
        if (_healthCheckTask is { IsCompleted: false }) return _healthCheckTask;
        _healthCheckTask = CheckHealthCoreAsync();
        return _healthCheckTask;
    }

    private async Task<bool> CheckHealthCoreAsync()
    {
        try
        {
            var response = await _http.GetAsync($"{BaseUrl}/health");
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                var health = JsonSerializer.Deserialize<HealthDto>(json, _jsonOptions);
                if (health == null)
                {
                    GD.PrintErr("[NetworkManager] Health check returned an empty or invalid response.");
                }
                else
                {
                    bool wasDatabaseConnected = IsDatabaseConnected;
                    IsServerOnline = true;
                    IsDatabaseConnected = health.DatabaseConnected;
                    if (_healthRetryTimer != null)
                    {
                        _healthRetryTimer.WaitTime = IsDatabaseConnected ? 30 : 5;
                    }
                    Callable.From(() =>
                    {
                        EmitSignal(SignalName.ServerConnected, true);
                        EmitSignal(SignalName.DatabaseConnectionChanged, IsDatabaseConnected);
                    }).CallDeferred();

                    if (IsDatabaseConnected && !wasDatabaseConnected)
                    {
                        GD.Print("[NetworkManager] PostgreSQL connection established.");
                    }
                    else if (!IsDatabaseConnected)
                    {
                        GD.PrintErr("[NetworkManager] Game server is reachable, but PostgreSQL is disconnected.");
                    }

                    return IsDatabaseConnected;
                }
            }
            else
            {
                GD.PrintErr($"[NetworkManager] Health check failed with HTTP {(int)response.StatusCode} ({response.StatusCode}).");
            }
        }
        catch (Exception ex)
        {
            GD.Print($"[NetworkManager] Server health check offline: {ex.Message}");
        }

        IsServerOnline = false;
        IsDatabaseConnected = false;
        if (_healthRetryTimer != null)
        {
            _healthRetryTimer.WaitTime = 5;
        }
        Callable.From(() =>
        {
            EmitSignal(SignalName.ServerDisconnected);
            EmitSignal(SignalName.DatabaseConnectionChanged, false);
        }).CallDeferred();
        return false;
    }

    public async Task<bool> SyncPlayerDataAsync()
    {
        if (!IsServerOnline || !IsDatabaseConnected || !IsAuthenticated) return false;

        try
        {
            var profileDto = await FetchProfileAsync();
            var walletDto = await FetchWalletAsync();
            var formations = await FetchFormationsAsync();
            var heroes = await FetchHeroesAsync();

            if (profileDto == null || walletDto == null || formations == null || heroes == null)
            {
                GD.PrintErr("[NetworkManager] Player sync failed: profile, wallet, heroes, or formation data could not be loaded from the database.");
                return false;
            }

            if (GameManager.Instance == null)
            {
                GD.PrintErr("[NetworkManager] Player sync failed: GameManager is not available.");
                return false;
            }

            var manager = GameManager.Instance;
            manager.Profile ??= PlayerProfile.CreateDefault();
            manager.Profile.PlayerId = profileDto.Id;
            manager.Profile.PlayerName = profileDto.DisplayName;
            manager.Profile.AvatarHeroId = profileDto.AvatarId;
            manager.Profile.Level = profileDto.Level;
            manager.Profile.VipLevel = profileDto.VipLevel;
            manager.Profile.CombatPower = profileDto.CombatPower;
            manager.Profile.OwnedHeroIds.Clear();
            manager.OwnedHeroes.Clear();
            foreach (var heroDto in heroes)
            {
                manager.Profile.OwnedHeroIds.Add(heroDto.HeroId);
                var hero = HeroDatabase.Instance?.GetHero(heroDto.HeroId);
                if (hero != null)
                {
                    var ownedHero = (HeroData)hero.Duplicate(true);
                    if (!string.IsNullOrWhiteSpace(heroDto.HeroName))
                    {
                        ownedHero.HeroName = heroDto.HeroName;
                    }

                    HeroDatabase.Instance?.Register(ownedHero);
                    manager.OwnedHeroes.Add(ownedHero);
                }
                manager.HeroProgressById[heroDto.HeroId] = new GameManager.HeroProgress
                {
                    Level = heroDto.Level,
                    Star = heroDto.Star,
                    Exp = heroDto.Exp
                };
            }

            manager.Gold = walletDto.Silver;
            manager.Gems = (int)Math.Clamp(walletDto.Gold, 0, int.MaxValue);
            manager.Stamina = walletDto.Stamina;
            manager.Profile.MaxStamina = walletDto.MaxStamina;
            manager.Profile.Save();

            SafeEmitEventBus(EventBus.SignalName.ProfileLoaded);
            SafeEmitEventBus(EventBus.SignalName.VipLevelChanged, profileDto.VipLevel);
            SafeEmitEventBus(EventBus.SignalName.GoldChanged, 0, (int)manager.Gold);
            SafeEmitEventBus(EventBus.SignalName.DiamondChanged, 0, manager.Gems);
            SafeEmitEventBus(EventBus.SignalName.StaminaChanged, walletDto.Stamina, walletDto.MaxStamina);

            if (formations.Count > 0)
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

    public async Task<AuthOperationResult> RegisterAsync(string username, string password, string displayName)
    {
        var payload = new
        {
            username,
            password,
            display_name = displayName
        };
        return await SendAuthRequestAsync("/api/auth/register", payload);
    }

    public async Task<AuthOperationResult> LoginAsync(string username, string password)
    {
        return await SendAuthRequestAsync("/api/auth/login", new { username, password });
    }

    private async Task<AuthOperationResult> SendAuthRequestAsync(string endpoint, object payload)
    {
        if (!IsDatabaseConnected)
        {
            return new AuthOperationResult(false, "Không thể kết nối máy chủ. Vui lòng thử lại sau.");
        }

        try
        {
            string json = JsonSerializer.Serialize(payload, _jsonOptions);
            using var content = new NetStringContent(json, Encoding.UTF8, "application/json");
            using var response = await _http.PostAsync($"{BaseUrl}{endpoint}", content);
            string responseJson = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                return new AuthOperationResult(false, ReadApiError(responseJson, response.ReasonPhrase));
            }

            var authResponse = JsonSerializer.Deserialize<AuthResponseDto>(responseJson, _jsonOptions);
            if (authResponse == null || string.IsNullOrWhiteSpace(authResponse.UserId) ||
                string.IsNullOrWhiteSpace(authResponse.AccessToken))
            {
                GD.PrintErr($"[NetworkManager] Authentication endpoint returned an invalid response for {endpoint}.");
                return new AuthOperationResult(false, "Phản hồi đăng nhập không hợp lệ.");
            }

            ActivateSession(authResponse.UserId, authResponse.AccessToken, persist: true);
            return new AuthOperationResult(true, "Đăng nhập thành công.");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[NetworkManager] Authentication request failed: {ex.Message}");
            return new AuthOperationResult(false, "Không thể kết nối máy chủ. Vui lòng thử lại.");
        }
    }

    public async Task<AuthOperationResult> TryRestoreSessionAsync()
    {
        if (!FileAccess.FileExists(SessionPath))
        {
            return new AuthOperationResult(false, string.Empty);
        }

        try
        {
            using var file = FileAccess.Open(SessionPath, FileAccess.ModeFlags.Read);
            if (file == null)
            {
                GD.PrintErr($"[NetworkManager] Could not read saved session. Error: {FileAccess.GetOpenError()}.");
                return new AuthOperationResult(false, "Không thể đọc phiên đăng nhập đã lưu.");
            }

            var savedSession = JsonSerializer.Deserialize<SavedSessionDto>(file.GetAsText(), _jsonOptions);
            string restoredToken = savedSession == null ? string.Empty : ResolveSavedToken(savedSession);
            if (savedSession == null || string.IsNullOrWhiteSpace(savedSession.UserId) ||
                string.IsNullOrWhiteSpace(restoredToken))
            {
                ClearSavedSession();
                return new AuthOperationResult(false, string.Empty);
            }
            savedSession.AccessToken = restoredToken;

            ActivateSession(savedSession.UserId, savedSession.AccessToken, persist: false);
            using var response = await _http.GetAsync($"{BaseUrl}/api/auth/session");
            if (!response.IsSuccessStatusCode)
            {
                if ((int)response.StatusCode == 401)
                {
                    ClearSavedSession();
                }
                return new AuthOperationResult(false, ReadApiError(
                    await response.Content.ReadAsStringAsync(),
                    response.ReasonPhrase));
            }

            if (!await SyncPlayerDataAsync())
            {
                return new AuthOperationResult(false, "Đăng nhập được nhưng chưa tải được dữ liệu người chơi.");
            }

            return new AuthOperationResult(true, "Đã khôi phục phiên đăng nhập.");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[NetworkManager] Session restore failed: {ex.Message}");
            return new AuthOperationResult(false, "Không thể kiểm tra phiên đăng nhập.");
        }
    }

    public async Task LogoutAsync()
    {
        if (IsAuthenticated && IsServerOnline)
        {
            try
            {
                using var response = await _http.PostAsync($"{BaseUrl}/api/auth/logout", null);
                if (!response.IsSuccessStatusCode && (int)response.StatusCode != 401)
                {
                    GD.PrintErr($"[NetworkManager] Logout failed with HTTP {(int)response.StatusCode} ({response.StatusCode}).");
                    return;
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[NetworkManager] Logout request failed: {ex.Message}");
                return;
            }
        }

        ClearSavedSession();
    }

    private void ActivateSession(string userId, string accessToken, bool persist)
    {
        CurrentUserId = userId;
        _sessionToken = accessToken;
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (persist)
        {
            // Format v2: token khong con luu dang plaintext - duoc XOR voi khoa
            // suy ra tu user_id + machine id cua thiet bi (chi mo duoc tren may nay).
            string json = JsonSerializer.Serialize(
                new SavedSessionDto
                {
                    UserId = userId,
                    AccessToken = string.Empty,
                    Version = 2,
                    TokenProtected = ProtectToken(userId, accessToken)
                },
                _jsonOptions);
            using var file = FileAccess.Open(SessionPath, FileAccess.ModeFlags.Write);
            if (file == null)
            {
                GD.PrintErr($"[NetworkManager] Could not persist auth session. Error: {FileAccess.GetOpenError()}.");
                return;
            }
            file.StoreString(json);
        }
    }

    /// <summary>
    /// Khoa bao ve token: SHA-256(user_id + "|" + machine unique id), XOR lap lai.
    /// Muc dich: file session bi sao chep sang may khac se khong dung duoc,
    /// va token khong nam dang plaintext tren dia. Khong thay the duoc bao mat
    /// phia server (token van het han + thu hoi duoc qua /api/auth/logout).
    /// </summary>
    private static string ProtectToken(string userId, string token)
    {
        var key = TokenKey(userId);
        var bytes = Encoding.UTF8.GetBytes(token);
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)(bytes[i] ^ key[i % key.Length]);
        }
        return Convert.ToBase64String(bytes);
    }

    private static string UnprotectToken(string userId, string protectedToken)
    {
        try
        {
            var key = TokenKey(userId);
            var bytes = Convert.FromBase64String(protectedToken);
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(bytes[i] ^ key[i % key.Length]);
            }
            return Encoding.UTF8.GetString(bytes);
        }
        catch (FormatException)
        {
            return string.Empty;
        }
    }

    private static byte[] TokenKey(string userId)
    {
        string material = $"{userId}|{OS.GetUniqueId()}";
        return System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(material));
    }

    private static string ResolveSavedToken(SavedSessionDto saved)
    {
        if (saved.Version >= 2 && !string.IsNullOrWhiteSpace(saved.TokenProtected))
        {
            return UnprotectToken(saved.UserId, saved.TokenProtected);
        }
        // v1 (plaintext) - van chap nhan de khong dang xuat nguoi dung cu;
        // ActivateSession(persist) se ghi lai dang v2 o lan dang nhap tiep theo.
        return saved.AccessToken ?? string.Empty;
    }

    private void ClearSavedSession()
    {
        CurrentUserId = string.Empty;
        _sessionToken = string.Empty;
        _http.DefaultRequestHeaders.Authorization = null;
        if (FileAccess.FileExists(SessionPath) && !DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SessionPath)).Equals(Error.Ok))
        {
            GD.PrintErr("[NetworkManager] Failed to remove expired local auth session.");
        }
    }

    private static string ReadApiError(string responseJson, string fallback)
    {
        try
        {
            using var document = JsonDocument.Parse(responseJson);
            if (document.RootElement.TryGetProperty("detail", out var detail) &&
                detail.ValueKind == JsonValueKind.String)
            {
                return detail.GetString();
            }
        }
        catch (JsonException)
        {
            GD.PrintErr("[NetworkManager] API returned an invalid error response.");
        }

        return string.IsNullOrWhiteSpace(fallback) ? "Đã xảy ra lỗi. Vui lòng thử lại." : fallback;
    }

    public async Task<ProfileDto> FetchProfileAsync()
    {
        try
        {
            var res = await _http.GetAsync($"{BaseUrl}/api/profile");
            if (!res.IsSuccessStatusCode)
            {
                GD.PrintErr($"[NetworkManager] FetchProfile failed with HTTP {(int)res.StatusCode} ({res.StatusCode}).");
                return null;
            }

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
            var res = await _http.GetAsync($"{BaseUrl}/api/wallet");
            if (!res.IsSuccessStatusCode)
            {
                GD.PrintErr($"[NetworkManager] FetchWallet failed with HTTP {(int)res.StatusCode} ({res.StatusCode}).");
                return null;
            }

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
            var res = await _http.GetAsync($"{BaseUrl}/api/formations");
            if (!res.IsSuccessStatusCode)
            {
                GD.PrintErr($"[NetworkManager] FetchFormations failed with HTTP {(int)res.StatusCode} ({res.StatusCode}).");
                return null;
            }

            string json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<FormationDto>>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[NetworkManager] FetchFormations failed: {ex.Message}");
            return null;
        }
    }

    public async Task<List<OwnedHeroDto>> FetchHeroesAsync()
    {
        try
        {
            var res = await _http.GetAsync($"{BaseUrl}/api/heroes");
            if (!res.IsSuccessStatusCode)
            {
                GD.PrintErr($"[NetworkManager] FetchHeroes failed with HTTP {(int)res.StatusCode} ({res.StatusCode}).");
                return null;
            }

            string json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<OwnedHeroDto>>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[NetworkManager] FetchHeroes failed: {ex.Message}");
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

            var res = await _http.PostAsync($"{BaseUrl}/api/formations", content);
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

    /// <summary>Đồng bộ lại level/sao/EXP của mọi tướng từ server vào GameManager.HeroProgressById.</summary>
    public async Task RefreshHeroProgressAsync()
    {
        var heroes = await FetchHeroesAsync();
        var manager = GameManager.Instance;
        if (manager == null) return;
        foreach (var heroDto in heroes)
        {
            manager.HeroProgressById[heroDto.HeroId] = new GameManager.HeroProgress
            {
                Level = heroDto.Level,
                Star = heroDto.Star,
                Exp = heroDto.Exp
            };
        }
    }

    public class StarUpResultDto
    {
        public string HeroId { get; set; }
        public int Star { get; set; }
        public int GoldSpent { get; set; }
    }

    /// <summary>Lên sao tướng: server tự tính giá (50 KC x sao hiện tại) và trừ ví.</summary>
    public async Task<StarUpResultDto> StarUpHeroAsync(string heroId)
    {
        if (!IsServerOnline || !IsAuthenticated) return null;
        try
        {
            var payload = new { hero_id = heroId };
            string json = JsonSerializer.Serialize(payload, _jsonOptions);
            var content = new NetStringContent(json, Encoding.UTF8, "application/json");
            var res = await _http.PostAsync($"{BaseUrl}/api/heroes/star-up", content);
            if (!res.IsSuccessStatusCode)
            {
                GD.PrintErr($"[NetworkManager] StarUp failed: {res.StatusCode} {await res.Content.ReadAsStringAsync()}");
                return null;
            }
            string resJson = await res.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<StarUpResultDto>(resJson, _jsonOptions);
            if (result != null)
            {
                await FetchWalletAsync();
                await RefreshHeroProgressAsync();
            }
            return result;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[NetworkManager] StarUpHero error: {ex.Message}");
            return null;
        }
    }

    // ==================== Phase 4: Shop / Inventory / Arena / Tower ====================

    private async Task<T> GetDtoAsync<T>(string path) where T : class
    {
        if (!IsServerOnline || !IsAuthenticated) return null;
        try
        {
            var res = await _http.GetAsync($"{BaseUrl}{path}");
            if (!res.IsSuccessStatusCode) return null;
            string json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[NetworkManager] GET {path} failed: {ex.Message}");
            return null;
        }
    }

    private async Task<T> PostDtoAsync<T>(string path, object payload) where T : class
    {
        if (!IsServerOnline || !IsAuthenticated) return null;
        try
        {
            string json = JsonSerializer.Serialize(payload, _jsonOptions);
            var content = new NetStringContent(json, Encoding.UTF8, "application/json");
            var res = await _http.PostAsync($"{BaseUrl}{path}", content);
            if (!res.IsSuccessStatusCode)
            {
                GD.PrintErr($"[NetworkManager] POST {path} failed: {res.StatusCode} {await res.Content.ReadAsStringAsync()}");
                return null;
            }
            string resJson = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(resJson, _jsonOptions);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[NetworkManager] POST {path} failed: {ex.Message}");
            return null;
        }
    }

    public async Task<List<ShopItemDto>> FetchShopItemsAsync() =>
        await GetDtoAsync<List<ShopItemDto>>("/api/shop/items") ?? new List<ShopItemDto>();

    public async Task<ShopBuyResultDto> BuyShopItemAsync(string itemId, int quantity = 1)
    {
        var result = await PostDtoAsync<ShopBuyResultDto>("/api/shop/buy",
            new { item_id = itemId, quantity });
        if (result != null) await FetchWalletAsync();
        return result;
    }

    public async Task<List<InventoryItemDto>> FetchInventoryAsync() =>
        await GetDtoAsync<List<InventoryItemDto>>("/api/inventory") ?? new List<InventoryItemDto>();

    public async Task<UseItemResultDto> UseItemAsync(string itemId, string heroId)
    {
        var result = await PostDtoAsync<UseItemResultDto>("/api/inventory/use",
            new { item_id = itemId, hero_id = heroId });
        if (result != null) await RefreshHeroProgressAsync();
        return result;
    }

    public async Task<ArenaProfileDto> FetchArenaProfileAsync() =>
        await GetDtoAsync<ArenaProfileDto>("/api/arena/profile");

    public async Task<List<ArenaOpponentDto>> FetchArenaOpponentsAsync() =>
        await GetDtoAsync<List<ArenaOpponentDto>>("/api/arena/opponents") ?? new List<ArenaOpponentDto>();

    public async Task<TowerProgressDto> FetchTowerProgressAsync() =>
        await GetDtoAsync<TowerProgressDto>("/api/tower/progress");

    public async Task<TowerClearResultDto> ClearTowerFloorAsync(int floor)
    {
        var result = await PostDtoAsync<TowerClearResultDto>("/api/tower/clear", new { floor });
        if (result != null) await FetchWalletAsync();
        return result;
    }

    public async Task<List<CampaignProgressDto>> FetchCampaignProgressAsync()
    {
        try
        {
            var res = await _http.GetAsync($"{BaseUrl}/api/campaign/progress");
            if (!res.IsSuccessStatusCode) return null;

            string json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<CampaignProgressDto>>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[NetworkManager] FetchCampaignProgress failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Bao server da vuot ai. Client chi gui stage/stars/turns - thuong va the luc
    /// do server tu tinh theo bang stage_rewards (chong gian lan). Tra ve null khi
    /// offline hoac server tu choi. Sau khi clear, vi tien duoc dong bo lai tu server.
    /// </summary>
    public async Task<CampaignClearResultDto> ClearStageAsync(string stageId, int stars, int turns)
    {
        if (!IsServerOnline) return null;

        try
        {
            var payload = new
            {
                stage_id = stageId,
                stars = stars,
                turns = turns
            };

            string json = JsonSerializer.Serialize(payload, _jsonOptions);
            var content = new NetStringContent(json, Encoding.UTF8, "application/json");

            var res = await _http.PostAsync($"{BaseUrl}/api/campaign/clear", content);
            if (!res.IsSuccessStatusCode) return null;

            string resJson = await res.Content.ReadAsStringAsync();
            var dto = JsonSerializer.Deserialize<CampaignClearResultDto>(resJson, _jsonOptions);

            // Server vua cong EXP cho tuong trong doi hinh -> cap nhat level/sao local
            await RefreshHeroProgressAsync();

            // Re-sync wallet: server la nguon chuan cho Bac/Kim cuong/The luc
            var wallet = await FetchWalletAsync();
            if (wallet != null && GameManager.Instance != null)
            {
                GameManager.Instance.Gold = wallet.Gold;
                GameManager.Instance.Stamina = wallet.Stamina;
                SafeEmitEventBus(EventBus.SignalName.GoldChanged, 0, (int)wallet.Gold);
                SafeEmitEventBus(EventBus.SignalName.StaminaChanged, wallet.Stamina, wallet.MaxStamina);
            }
            return dto;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[NetworkManager] ClearStage failed: {ex.Message}");
            return null;
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

            var res = await _http.PostAsync($"{BaseUrl}/api/battle/simulate", content);
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

            var res = await _http.PostAsync($"{BaseUrl}/api/gacha/pull", content);
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
        if (EventBus.Instance == null || !GodotObject.IsInstanceValid(EventBus.Instance)) return;
        Callable.From(() =>
        {
            if (EventBus.Instance == null || !GodotObject.IsInstanceValid(EventBus.Instance)) return;
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
    public sealed class AuthOperationResult
    {
        public bool Success { get; }
        public string Message { get; }

        public AuthOperationResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }
    }

    public class AuthResponseDto
    {
        public string UserId { get; set; }
        public string AccessToken { get; set; }
    }

    public class SavedSessionDto
    {
        public string UserId { get; set; }
        public string AccessToken { get; set; }
        public int Version { get; set; } = 1;
        public string TokenProtected { get; set; }
    }

    public class HealthDto
    {
        public string Status { get; set; }
        public bool DatabaseConnected { get; set; }
        public bool RedisConnected { get; set; }
    }

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

    public class OwnedHeroDto
    {
        public string HeroId { get; set; }
        public string HeroName { get; set; }
        public List<JsonElement> Skills { get; set; }
        public int Star { get; set; }
        public int Level { get; set; }
        public long Exp { get; set; }
        public bool IsCommander { get; set; }
    }

    public class CampaignClearResultDto
    {
        public string Status { get; set; }
        public bool FirstClear { get; set; }
        public int RewardSilver { get; set; }
        public int RewardGold { get; set; }
        public int StaminaCost { get; set; }
        public int ExpGranted { get; set; }
    }

    public class CampaignProgressDto
    {
        public string StageId { get; set; }
        public int Stars { get; set; }
        public bool FirstClearClaimed { get; set; }
        public int ClearCount { get; set; }
        public int? BestRecordTurns { get; set; }
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
        public string DefenderName { get; set; }
        public long AttackerPower { get; set; }
        public long DefenderPower { get; set; }
        public BattleRewardsDto Rewards { get; set; }
        public int? MmrAfter { get; set; }
    }

    public class BattleRewardsDto
    {
        public int Silver { get; set; }
        public int ArenaTokens { get; set; }
    }

    public class ShopItemDto
    {
        public string ItemId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Currency { get; set; }
        public int Price { get; set; }
        public string GrantType { get; set; }
        public int GrantAmount { get; set; }
        public string GrantItemId { get; set; }
    }

    public class ShopBuyResultDto
    {
        public string ItemId { get; set; }
        public int Quantity { get; set; }
        public int TotalCost { get; set; }
        public string Currency { get; set; }
        public string GrantType { get; set; }
        public int GrantAmount { get; set; }
        public string GrantItemId { get; set; }
    }

    public class InventoryItemDto
    {
        public string ItemId { get; set; }
        public long Quantity { get; set; }
    }

    public class UseItemResultDto
    {
        public string HeroId { get; set; }
        public long Exp { get; set; }
        public int Level { get; set; }
        public int ExpGranted { get; set; }
    }

    public class ArenaProfileDto
    {
        public string UserId { get; set; }
        public string DisplayName { get; set; }
        public string RankTier { get; set; }
        public int Mmr { get; set; }
        public int WinCount { get; set; }
        public int LossCount { get; set; }
        public int CurrentWinningStreak { get; set; }
        public long CombatPower { get; set; }
    }

    public class ArenaOpponentDto
    {
        public string UserId { get; set; }
        public string DisplayName { get; set; }
        public int Mmr { get; set; }
        public int WinCount { get; set; }
        public int LossCount { get; set; }
        public long CombatPower { get; set; }
        public List<string> HeroIds { get; set; }
    }

    public class TowerProgressDto
    {
        public int HighestFloor { get; set; }
    }

    public class TowerClearResultDto
    {
        public string Status { get; set; }
        public int Floor { get; set; }
        public int HighestFloor { get; set; }
        public int RewardSilver { get; set; }
        public int RewardGold { get; set; }
    }
}
