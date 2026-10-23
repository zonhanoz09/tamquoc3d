using System.Collections.Generic;
using Godot;
using TamQuoc3D.Core.Constants;
using TamQuoc3D.Data.Resources;

namespace TamQuoc3D.Data.Database;

/// <summary>
/// HeroDatabase - Quan ly co so du lieu tuong Data-Driven (.tres).
/// Nạp cac file resource tu res://data/database/heroes/ va cung cap API truy van.
/// </summary>
[GlobalClass]
public partial class HeroDatabase : Node
{
    public static HeroDatabase Instance { get; private set; }

    private readonly Dictionary<string, HeroData> _heroes = new();

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        LoadDatabase();
        GD.Print($"🏰 HeroDatabase: Nạp {_heroes.Count} tướng thành công (Data-Driven Architecture)");
    }

    public void LoadDatabase()
    {
        _heroes.Clear();

        // 1. Thử nạp từ thư mục res://data/database/heroes/
        using var dir = DirAccess.Open("res://data/database/heroes/");
        if (dir != null)
        {
            dir.ListDirBegin();
            string fileName = dir.GetNext();
            while (!string.IsNullOrEmpty(fileName))
            {
                if (!dir.CurrentIsDir() && (fileName.EndsWith(".tres") || fileName.EndsWith(".res")))
                {
                    var resPath = $"res://data/database/heroes/{fileName}";
                    var hero = GD.Load<HeroData>(resPath);
                    if (hero != null && !string.IsNullOrEmpty(hero.HeroId))
                    {
                        Register(hero);
                    }
                }
                fileName = dir.GetNext();
            }
        }

        // 2. Nếu thư mục chưa có file .tres (lần đầu khởi chạy), tự động sinh đầy đủ 15 tướng và lưu .tres
        if (_heroes.Count == 0)
        {
            GenerateDefaultHeroDatabase();
        }
    }

    public void Register(HeroData data)
    {
        _heroes[data.HeroId] = data;
    }

    public HeroData GetHero(string id)
    {
        return _heroes.GetValueOrDefault(id);
    }

    public IEnumerable<HeroData> GetAllHeroes()
    {
        return _heroes.Values;
    }

    public void GenerateDefaultHeroDatabase()
    {
        RegisterAndSave(MakeTrieuVan());
        RegisterAndSave(MakeDienVi());
        RegisterAndSave(MakeTruongPhi());
        RegisterAndSave(MakeTaoNhan());
        RegisterAndSave(MakeHoangCai());

        RegisterAndSave(MakeQuanVu());
        RegisterAndSave(MakeLuBo());
        RegisterAndSave(MakeChuDu());
        RegisterAndSave(MakeHoangTrung());
        RegisterAndSave(MakeMaSieu());
        RegisterAndSave(MakeDieuThuyen());

        RegisterAndSave(MakeGiaCatLuong());
        RegisterAndSave(MakeTieuKieu());
        RegisterAndSave(MakeBangThong());
        RegisterAndSave(MakeSaiVanCo());
    }

    private void RegisterAndSave(HeroData hero)
    {
        Register(hero);
        // Chi ghi .tres khi chay trong editor: ban export res:// la read-only,
        // va viec ghi de file da curate cung la nguon rewrite ngoai y muon.
        if (OS.HasFeature("editor"))
        {
            string savePath = $"res://data/database/heroes/{hero.HeroId}.tres";
            ResourceSaver.Save(hero, savePath);
        }
    }

    // ── TIỀN QUÂN ──
    private static HeroData MakeTrieuVan()
    {
        var h = new HeroData
        {
            HeroId = "trieu_van",
            HeroName = "Triệu Vân",
            Title = "Thường Sơn Triệu Tử Long",
            Element = HeroData.ElementType.Water,
            Position = BattlePosition.Front,
            Rarity = HeroData.RarityType.Ur,
            BaseStats = new HeroStats
            {
                HpMax = 2200, HpCurrent = 2200,
                AtkPhys = 195, DefPhys = 120, DefMag = 90,
                Speed = 118, DodgeRate = 0.18f, CritRate = 0.15f
            }
        };
        h.Skills.Add(new SkillData { SkillId = "tv_atk", SkillName = "Bạch Long Thương", Type = SkillData.SkillType.Melee, DamageMultiplier = 1.0f });
        var rage = new SkillData { SkillId = "tv_rage", SkillName = "Long Đảm Thần Uy", Type = SkillData.SkillType.AoeRow, MoraleCost = 100, DamageMultiplier = 1.6f };
        var eff = new Godot.Collections.Dictionary { { "type", (int)SkillData.EffectType.BuffSpeed }, { "duration", 2 }, { "val", 25 } };
        rage.Effects.Add(eff);
        h.Skills.Add(rage);
        return h;
    }

    private static HeroData MakeDienVi()
    {
        var h = new HeroData
        {
            HeroId = "dien_vi",
            HeroName = "Điển Vi",
            Title = "Cổ Chi Ác Lai",
            Element = HeroData.ElementType.Fire,
            Position = BattlePosition.Front,
            Rarity = HeroData.RarityType.Ssr,
            BaseStats = new HeroStats
            {
                HpMax = 2600, HpCurrent = 2600,
                AtkPhys = 175, DefPhys = 145, DefMag = 75,
                Speed = 92, CounterRate = 0.25f, BlockRate = 0.15f
            }
        };
        h.Skills.Add(new SkillData { SkillId = "dv_atk", SkillName = "Song Kích Trọng Kích", Type = SkillData.SkillType.Melee, DamageMultiplier = 1.0f });
        var rage = new SkillData { SkillId = "dv_rage", SkillName = "Hộ Chủ Quyết Tử", Type = SkillData.SkillType.Melee, MoraleCost = 100, DamageMultiplier = 2.2f };
        var eff = new Godot.Collections.Dictionary { { "type", (int)SkillData.EffectType.BuffDef }, { "duration", 2 }, { "val", 40 } };
        rage.Effects.Add(eff);
        h.Skills.Add(rage);
        return h;
    }

    private static HeroData MakeTruongPhi()
    {
        var h = new HeroData
        {
            HeroId = "truong_phi",
            HeroName = "Trương Phi",
            Title = "Vạn Nhân Địch",
            Element = HeroData.ElementType.Fire,
            Position = BattlePosition.Front,
            Rarity = HeroData.RarityType.Ssr,
            BaseStats = new HeroStats
            {
                HpMax = 2400, HpCurrent = 2400,
                AtkPhys = 185, DefPhys = 115, DefMag = 80,
                Speed = 98, CritRate = 0.12f
            }
        };
        h.Skills.Add(new SkillData { SkillId = "tp_atk", SkillName = "Bát Xà Mâu Trảm", Type = SkillData.SkillType.Melee, DamageMultiplier = 1.0f });
        var rage = new SkillData { SkillId = "tp_rage", SkillName = "Nộ Hống Bản Cầu", Type = SkillData.SkillType.AoeRow, MoraleCost = 100, DamageMultiplier = 1.4f };
        var eff = new Godot.Collections.Dictionary { { "type", (int)SkillData.EffectType.Stun }, { "duration", 1 }, { "chance", 0.45f } };
        rage.Effects.Add(eff);
        h.Skills.Add(rage);
        return h;
    }

    private static HeroData MakeTaoNhan()
    {
        var h = new HeroData
        {
            HeroId = "tao_nhan",
            HeroName = "Tào Nhân",
            Title = "Thiên Nhân Chi Địch",
            Element = HeroData.ElementType.Light,
            Position = BattlePosition.Front,
            Rarity = HeroData.RarityType.Ssr,
            BaseStats = new HeroStats
            {
                HpMax = 2800, HpCurrent = 2800,
                AtkPhys = 140, DefPhys = 160, DefMag = 110,
                Speed = 88, BlockRate = 0.28f
            }
        };
        h.Skills.Add(new SkillData { SkillId = "tn_atk", SkillName = "Kim Cương Kích", Type = SkillData.SkillType.Melee, DamageMultiplier = 0.9f });
        var rage = new SkillData { SkillId = "tn_rage", SkillName = "Trầm Lực Thiết Bích", Type = SkillData.SkillType.Buff, MoraleCost = 100, DamageMultiplier = 0.0f };
        var eff = new Godot.Collections.Dictionary { { "type", (int)SkillData.EffectType.Shield }, { "duration", 2 }, { "val", 500 } };
        rage.Effects.Add(eff);
        h.Skills.Add(rage);
        return h;
    }

    private static HeroData MakeHoangCai()
    {
        var h = new HeroData
        {
            HeroId = "hoang_cai",
            HeroName = "Hoàng Cái",
            Title = "Khổ Nhục Tiên Phong",
            Element = HeroData.ElementType.Water,
            Position = BattlePosition.Front,
            Rarity = HeroData.RarityType.Sr,
            BaseStats = new HeroStats
            {
                HpMax = 2300, HpCurrent = 2300,
                AtkPhys = 160, DefPhys = 125, DefMag = 85,
                Speed = 94, BlockRate = 0.15f
            }
        };
        h.Skills.Add(new SkillData { SkillId = "hc_atk", SkillName = "Thiết Tiết Kích", Type = SkillData.SkillType.Melee, DamageMultiplier = 1.0f });
        var rage = new SkillData { SkillId = "hc_rage", SkillName = "Khổ Nhục Hỏa Thuyền", Type = SkillData.SkillType.AoeRow, MoraleCost = 100, DamageMultiplier = 1.7f };
        var eff = new Godot.Collections.Dictionary { { "type", (int)SkillData.EffectType.Burn }, { "duration", 2 }, { "val", 80 } };
        rage.Effects.Add(eff);
        h.Skills.Add(rage);
        return h;
    }

    // ── TRUNG QUÂN ──
    private static HeroData MakeQuanVu()
    {
        var h = new HeroData
        {
            HeroId = "quan_vu",
            HeroName = "Quan Vũ",
            Title = "Mỹ Nhiễm Công",
            Element = HeroData.ElementType.Wind,
            Position = BattlePosition.Mid,
            Rarity = HeroData.RarityType.Ur,
            BaseStats = new HeroStats
            {
                HpMax = 1950, HpCurrent = 1950,
                AtkPhys = 240, DefPhys = 110, DefMag = 85,
                Speed = 108, CritRate = 0.22f, CritDmg = 1.75f
            }
        };
        h.Skills.Add(new SkillData { SkillId = "qv_atk", SkillName = "Thanh Long Yển Nguyệt", Type = SkillData.SkillType.Melee, DamageMultiplier = 1.1f });
        var rage = new SkillData { SkillId = "qv_rage", SkillName = "Qua Ngũ Quan Trảm Tướng", Type = SkillData.SkillType.Melee, MoraleCost = 100, DamageMultiplier = 2.8f };
        var eff = new Godot.Collections.Dictionary { { "type", (int)SkillData.EffectType.DebuffDef }, { "duration", 2 }, { "val", 35 } };
        rage.Effects.Add(eff);
        h.Skills.Add(rage);
        return h;
    }

    private static HeroData MakeLuBo()
    {
        var h = new HeroData
        {
            HeroId = "lu_bo",
            HeroName = "Lữ Bố",
            Title = "Chiến Thần Vô Song",
            Element = HeroData.ElementType.Dark,
            Position = BattlePosition.Mid,
            Rarity = HeroData.RarityType.Ur,
            BaseStats = new HeroStats
            {
                HpMax = 2100, HpCurrent = 2100,
                AtkPhys = 265, DefPhys = 115, DefMag = 70,
                Speed = 114, CritRate = 0.25f, CritDmg = 1.85f
            }
        };
        h.Skills.Add(new SkillData { SkillId = "lb_atk", SkillName = "Phương Thiên Họa Kích", Type = SkillData.SkillType.Melee, DamageMultiplier = 1.15f });
        var rage = new SkillData { SkillId = "lb_rage", SkillName = "Vô Song Chiến Thần", Type = SkillData.SkillType.AoeAll, MoraleCost = 100, DamageMultiplier = 1.85f };
        var eff = new Godot.Collections.Dictionary { { "type", (int)SkillData.EffectType.Stun }, { "duration", 1 }, { "chance", 0.35f } };
        rage.Effects.Add(eff);
        h.Skills.Add(rage);
        return h;
    }

    private static HeroData MakeChuDu()
    {
        var h = new HeroData
        {
            HeroId = "chu_du",
            HeroName = "Chu Du",
            Title = "Mỹ Chu Lang",
            Element = HeroData.ElementType.Fire,
            Position = BattlePosition.Mid,
            Rarity = HeroData.RarityType.Ssr,
            BaseStats = new HeroStats
            {
                HpMax = 1600, HpCurrent = 1600,
                AtkPhys = 60, AtkMag = 225, DefPhys = 75, DefMag = 125,
                Speed = 110, CritRate = 0.16f
            }
        };
        h.Skills.Add(new SkillData { SkillId = "cd_atk", SkillName = "Hỏa Kiếm Vũ", Type = SkillData.SkillType.Ranged, IsMagic = true, DamageMultiplier = 1.0f });
        var rage = new SkillData { SkillId = "cd_rage", SkillName = "Đại Hỏa Xích Bích", Type = SkillData.SkillType.AoeAll, IsMagic = true, MoraleCost = 100, DamageMultiplier = 1.55f };
        var eff = new Godot.Collections.Dictionary { { "type", (int)SkillData.EffectType.Burn }, { "duration", 3 }, { "val", 90 } };
        rage.Effects.Add(eff);
        h.Skills.Add(rage);
        return h;
    }

    private static HeroData MakeHoangTrung()
    {
        var h = new HeroData
        {
            HeroId = "hoang_trung",
            HeroName = "Hoàng Trung",
            Title = "Lão Tướng Thần Xạ",
            Element = HeroData.ElementType.Light,
            Position = BattlePosition.Mid,
            Rarity = HeroData.RarityType.Ssr,
            BaseStats = new HeroStats
            {
                HpMax = 1650, HpCurrent = 1650,
                AtkPhys = 230, DefPhys = 80, DefMag = 80,
                Speed = 106, CritRate = 0.28f, CritDmg = 1.90f
            }
        };
        h.Skills.Add(new SkillData { SkillId = "ht_atk", SkillName = "Tiễn Tiệt Không", Type = SkillData.SkillType.Ranged, DamageMultiplier = 1.05f });
        var rage = new SkillData { SkillId = "ht_rage", SkillName = "Bách Bộ Xuyên Dương", Type = SkillData.SkillType.Flank, MoraleCost = 100, DamageMultiplier = 2.6f };
        var eff = new Godot.Collections.Dictionary { { "type", (int)SkillData.EffectType.DebuffDef }, { "duration", 2 }, { "val", 30 } };
        rage.Effects.Add(eff);
        h.Skills.Add(rage);
        return h;
    }

    private static HeroData MakeMaSieu()
    {
        var h = new HeroData
        {
            HeroId = "ma_sieu",
            HeroName = "Mã Siêu",
            Title = "Cẩm Mã Siêu",
            Element = HeroData.ElementType.Wind,
            Position = BattlePosition.Mid,
            Rarity = HeroData.RarityType.Ssr,
            BaseStats = new HeroStats
            {
                HpMax = 1850, HpCurrent = 1850,
                AtkPhys = 215, DefPhys = 95, DefMag = 80,
                Speed = 120, CritRate = 0.18f
            }
        };
        h.Skills.Add(new SkillData { SkillId = "ms_atk", SkillName = "Tây Lương Mã Đao", Type = SkillData.SkillType.Melee, DamageMultiplier = 1.05f });
        var rage = new SkillData { SkillId = "ms_rage", SkillName = "Thần Uy Đột Kích", Type = SkillData.SkillType.Flank, MoraleCost = 100, DamageMultiplier = 2.3f };
        var eff = new Godot.Collections.Dictionary { { "type", (int)SkillData.EffectType.DebuffAtk }, { "duration", 2 }, { "val", 25 } };
        rage.Effects.Add(eff);
        h.Skills.Add(rage);
        return h;
    }

    private static HeroData MakeDieuThuyen()
    {
        var h = new HeroData
        {
            HeroId = "dieu_thuyen",
            HeroName = "Điêu Thuyền",
            Title = "Bế Nguyệt Mỹ Nhân",
            Element = HeroData.ElementType.Dark,
            Position = BattlePosition.Mid,
            Rarity = HeroData.RarityType.Ssr,
            BaseStats = new HeroStats
            {
                HpMax = 1550, HpCurrent = 1550,
                AtkPhys = 50, AtkMag = 200, DefPhys = 70, DefMag = 115,
                Speed = 116, DodgeRate = 0.16f
            }
        };
        h.Skills.Add(new SkillData { SkillId = "dt_atk", SkillName = "Lụa Hồng Mê Hoặc", Type = SkillData.SkillType.Ranged, IsMagic = true, DamageMultiplier = 1.0f });
        var rage = new SkillData { SkillId = "dt_rage", SkillName = "Bế Nguyệt Tuyệt Sắc", Type = SkillData.SkillType.AoeRow, IsMagic = true, MoraleCost = 100, DamageMultiplier = 1.45f };
        var eff = new Godot.Collections.Dictionary { { "type", (int)SkillData.EffectType.Silence }, { "duration", 2 }, { "chance", 0.60f } };
        rage.Effects.Add(eff);
        h.Skills.Add(rage);
        return h;
    }

    // ── HẬU QUÂN ──
    private static HeroData MakeGiaCatLuong()
    {
        var h = new HeroData
        {
            HeroId = "gia_cat_luong",
            HeroName = "Gia Cát Lượng",
            Title = "Ngọa Long Tiên Sinh",
            Element = HeroData.ElementType.Wind,
            Position = BattlePosition.Back,
            Rarity = HeroData.RarityType.Ur,
            BaseStats = new HeroStats
            {
                HpMax = 1650, HpCurrent = 1650,
                AtkPhys = 50, AtkMag = 250, DefPhys = 80, DefMag = 140,
                Speed = 112, CritRate = 0.15f
            }
        };
        h.Skills.Add(new SkillData { SkillId = "gcl_atk", SkillName = "Bát Trận Đồ", Type = SkillData.SkillType.Ranged, IsMagic = true, DamageMultiplier = 1.0f });
        var rage = new SkillData { SkillId = "gcl_rage", SkillName = "Đông Phong Bát Trận", Type = SkillData.SkillType.AoeAll, IsMagic = true, MoraleCost = 100, DamageMultiplier = 1.65f };
        var eff = new Godot.Collections.Dictionary { { "type", (int)SkillData.EffectType.Stun }, { "duration", 1 }, { "chance", 0.40f } };
        rage.Effects.Add(eff);
        h.Skills.Add(rage);
        return h;
    }

    private static HeroData MakeTieuKieu()
    {
        var h = new HeroData
        {
            HeroId = "tieu_kieu",
            HeroName = "Tiểu Kiều",
            Title = "Giang Nam Đệ Nhất Kiều",
            Element = HeroData.ElementType.Water,
            Position = BattlePosition.Back,
            Rarity = HeroData.RarityType.Ssr,
            BaseStats = new HeroStats
            {
                HpMax = 1500, HpCurrent = 1500,
                AtkPhys = 40, AtkMag = 180, DefPhys = 70, DefMag = 120,
                Speed = 105
            }
        };
        h.Skills.Add(new SkillData { SkillId = "tk_atk", SkillName = "Khúc Nhạc Hồi Sinh", Type = SkillData.SkillType.Heal, IsMagic = true, DamageMultiplier = 1.0f });
        var rage = new SkillData { SkillId = "tk_rage", SkillName = "Điệu Vũ Thiên Kiều", Type = SkillData.SkillType.Heal, IsMagic = true, MoraleCost = 100, DamageMultiplier = 1.6f, TargetCount = 5 };
        var eff = new Godot.Collections.Dictionary { { "type", (int)SkillData.EffectType.BuffAtk }, { "duration", 2 }, { "val", 25 } };
        rage.Effects.Add(eff);
        h.Skills.Add(rage);
        return h;
    }

    private static HeroData MakeBangThong()
    {
        var h = new HeroData
        {
            HeroId = "bang_thong",
            HeroName = "Bàng Thống",
            Title = "Phượng Sồ Tiên Sinh",
            Element = HeroData.ElementType.Fire,
            Position = BattlePosition.Back,
            Rarity = HeroData.RarityType.Ssr,
            BaseStats = new HeroStats
            {
                HpMax = 1580, HpCurrent = 1580,
                AtkPhys = 45, AtkMag = 235, DefPhys = 75, DefMag = 130,
                Speed = 108
            }
        };
        h.Skills.Add(new SkillData { SkillId = "bt_atk", SkillName = "Hỏa Long Chú", Type = SkillData.SkillType.Ranged, IsMagic = true, DamageMultiplier = 1.0f });
        var rage = new SkillData { SkillId = "bt_rage", SkillName = "Liên Hoàn Hỏa Kế", Type = SkillData.SkillType.AoeAll, IsMagic = true, MoraleCost = 100, DamageMultiplier = 1.6f };
        var eff = new Godot.Collections.Dictionary { { "type", (int)SkillData.EffectType.Burn }, { "duration", 3 }, { "val", 85 } };
        rage.Effects.Add(eff);
        h.Skills.Add(rage);
        return h;
    }

    private static HeroData MakeSaiVanCo()
    {
        var h = new HeroData
        {
            HeroId = "sai_van_co",
            HeroName = "Sái Văn Cơ",
            Title = "Tài Nữ Thi Ca",
            Element = HeroData.ElementType.Light,
            Position = BattlePosition.Back,
            Rarity = HeroData.RarityType.Sr,
            BaseStats = new HeroStats
            {
                HpMax = 1450, HpCurrent = 1450,
                AtkPhys = 40, AtkMag = 175, DefPhys = 65, DefMag = 115,
                Speed = 104
            }
        };
        h.Skills.Add(new SkillData { SkillId = "svc_atk", SkillName = "Hồ Gia Khúc", Type = SkillData.SkillType.Heal, IsMagic = true, DamageMultiplier = 0.95f });
        var rage = new SkillData { SkillId = "svc_rage", SkillName = "Thập Bát Phách Linh", Type = SkillData.SkillType.Heal, IsMagic = true, MoraleCost = 100, DamageMultiplier = 1.5f, TargetCount = 5 };
        var eff = new Godot.Collections.Dictionary { { "type", (int)SkillData.EffectType.BuffDef }, { "duration", 2 }, { "val", 25 } };
        rage.Effects.Add(eff);
        h.Skills.Add(rage);
        return h;
    }
}
