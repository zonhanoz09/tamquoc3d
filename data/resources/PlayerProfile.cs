using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace TamQuoc3D.Data.Resources;

/// <summary>
/// PlayerProfile - Lưu trữ dữ liệu hồ sơ nhân vật, cấp VIP, tiền tệ và tiến trình của Chúa Công.
/// </summary>
[GlobalClass]
public partial class PlayerProfile : Resource
{
    private const string DefaultSavePath = "user://player_profile.json";

    [Export] public string PlayerId { get; set; } = "user_8888";
    [Export] public string PlayerName { get; set; } = "Chúa Công";
    [Export] public int Level { get; set; } = 80;
    [Export] public int VipLevel { get; set; } = 11;
    [Export] public long CombatPower { get; set; }
    [Export] public string CurrentTitle { get; set; } = "[Địa Chủ]";
    [Export] public long Gold { get; set; } = 2145370; // 214 vạn
    [Export] public int Diamond { get; set; } = 15000;   // 150 vạn / 15k knb
    [Export] public int Stamina { get; set; } = 100;
    [Export] public int MaxStamina { get; set; } = 145;
    [Export] public long StaminaRefillTimeUnix { get; set; } = 0;
    [Export] public int GuildId { get; set; } = 101;
    [Export] public string GuildName { get; set; } = "Long Hổ Môn";
    [Export] public string AvatarHeroId { get; set; } = "default_hero";
    [Export] public int ArenaRank { get; set; } = 8;
    [Export] public int TowerMaxFloor { get; set; } = 68;
    [Export] public Godot.Collections.Array<string> OwnedHeroIds { get; set; } = [];

    public DateTime StaminaRefillTime
    {
        get => StaminaRefillTimeUnix > 0
            ? DateTimeOffset.FromUnixTimeSeconds(StaminaRefillTimeUnix).LocalDateTime
            : DateTime.Now.AddSeconds(1483);
        set => StaminaRefillTimeUnix = new DateTimeOffset(value).ToUnixTimeSeconds();
    }

    public static PlayerProfile CreateDefault()
    {
        var profile = new PlayerProfile
        {
            PlayerId = "user_8888",
            PlayerName = "Long Tướng Quân",
            Level = 80,
            VipLevel = 11,
            CurrentTitle = "[Địa Chủ]",
            Gold = 2145370,
            Diamond = 150000,
            Stamina = 100,
            MaxStamina = 145,
            StaminaRefillTime = DateTime.Now.AddMinutes(24).AddSeconds(43),
            GuildId = 101,
            GuildName = "Long Hổ Môn",
            AvatarHeroId = "default_hero",
            ArenaRank = 8,
            TowerMaxFloor = 68,
            OwnedHeroIds = ["default_hero"]
        };
        return profile;
    }

    public void Save(string path = null)
    {
        string targetPath = string.IsNullOrEmpty(path) ? DefaultSavePath : path;
        try
        {
            var dto = new ProfileDto
            {
                PlayerId = PlayerId,
                PlayerName = PlayerName,
                Level = Level,
                VipLevel = VipLevel,
                CombatPower = CombatPower,
                CurrentTitle = CurrentTitle,
                Gold = Gold,
                Diamond = Diamond,
                Stamina = Stamina,
                MaxStamina = MaxStamina,
                StaminaRefillTimeUnix = StaminaRefillTimeUnix,
                GuildId = GuildId,
                GuildName = GuildName,
                AvatarHeroId = AvatarHeroId,
                ArenaRank = ArenaRank,
                TowerMaxFloor = TowerMaxFloor,
                OwnedHeroIds = new List<string>(OwnedHeroIds)
            };

            string json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
            using var file = FileAccess.Open(targetPath, FileAccess.ModeFlags.Write);
            if (file != null)
            {
                file.StoreString(json);
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[PlayerProfile] Save failed: {ex.Message}");
        }
    }

    public static PlayerProfile Load(string path = null)
    {
        string targetPath = string.IsNullOrEmpty(path) ? DefaultSavePath : path;
        if (!FileAccess.FileExists(targetPath))
        {
            return CreateDefault();
        }

        try
        {
            using var file = FileAccess.Open(targetPath, FileAccess.ModeFlags.Read);
            if (file == null) return CreateDefault();

            string json = file.GetAsText();
            var dto = JsonSerializer.Deserialize<ProfileDto>(json);
            if (dto == null) return CreateDefault();

            var profile = new PlayerProfile
            {
                PlayerId = dto.PlayerId,
                PlayerName = dto.PlayerName,
                Level = dto.Level,
                VipLevel = dto.VipLevel,
                CombatPower = dto.CombatPower,
                CurrentTitle = dto.CurrentTitle,
                Gold = dto.Gold,
                Diamond = dto.Diamond,
                Stamina = dto.Stamina,
                MaxStamina = dto.MaxStamina,
                StaminaRefillTimeUnix = dto.StaminaRefillTimeUnix,
                GuildId = dto.GuildId,
                GuildName = dto.GuildName,
                AvatarHeroId = dto.AvatarHeroId,
                ArenaRank = dto.ArenaRank,
                TowerMaxFloor = dto.TowerMaxFloor
            };

            profile.OwnedHeroIds.Clear();
            if (dto.OwnedHeroIds != null)
            {
                foreach (var id in dto.OwnedHeroIds)
                {
                    profile.OwnedHeroIds.Add(id);
                }
            }

            return profile;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[PlayerProfile] Load failed: {ex.Message}. Falling back to default.");
            return CreateDefault();
        }
    }

    private class ProfileDto
    {
        public string PlayerId { get; set; }
        public string PlayerName { get; set; }
        public int Level { get; set; }
        public int VipLevel { get; set; }
        public long CombatPower { get; set; }
        public string CurrentTitle { get; set; }
        public long Gold { get; set; }
        public int Diamond { get; set; }
        public int Stamina { get; set; }
        public int MaxStamina { get; set; }
        public long StaminaRefillTimeUnix { get; set; }
        public int GuildId { get; set; }
        public string GuildName { get; set; }
        public string AvatarHeroId { get; set; }
        public int ArenaRank { get; set; }
        public int TowerMaxFloor { get; set; }
        public List<string> OwnedHeroIds { get; set; }
    }
}
