using System;
using System.Collections.Generic;
using Godot;
using TamQuoc3D.Hero;

namespace TamQuoc3D.Data;

[GlobalClass]
public partial class HeroDatabase : Node
{
    public static HeroDatabase? Instance { get; private set; }

    private readonly Dictionary<string, HeroData> _heroes = new();

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        RegisterStarterHeroes();
        GD.Print($"HeroDatabase: Loaded {_heroes.Count} heroes (C# .NET 8)");
    }

    public void RegisterStarterHeroes()
    {
        _heroes.Clear();

        // TIỀN QUÂN (5 tướng)
        Register(MakeTrieuVan());
        Register(MakeDienVi());
        Register(MakeTruongPhi());
        Register(MakeTaoNhan());
        Register(MakeHoangCai());

        // TRUNG QUÂN (6 tướng)
        Register(MakeQuanVu());
        Register(MakeLuBo());
        Register(MakeChuDu());
        Register(MakeHoangTrung());
        Register(MakeMaSieu());
        Register(MakeDieuThuyen());

        // HẬU QUÂN (4 tướng)
        Register(MakeGiaCatLuong());
        Register(MakeTieuKieu());
        Register(MakeBangThong());
        Register(MakeSaiVanCo());
    }

    public void Register(HeroData data)
    {
        _heroes[data.HeroId] = data;
    }

    public HeroData? GetHero(string id)
    {
        return _heroes.GetValueOrDefault(id);
    }

    public IEnumerable<HeroData> GetAllHeroes()
    {
        return _heroes.Values;
    }

    // ──────────────────────────────────────────────────────────
    // TIỀN QUÂN (FRONTLINE)
    // ──────────────────────────────────────────────────────────

    private static HeroData MakeTrieuVan()
    {
        var stats = new HeroStats
        {
            HpMax = 2800, Strength = 90, Agi = 65, Intel = 30, Speed = 90,
            PhysAtk = 280, PhysDef = 180, MagAtk = 60, MagDef = 100,
            CritRate = 0.08f, DodgeRate = 0.15f, BlockRate = 0.12f,
            HitRate = 0.92f, MoraleInit = 50
        };

        var sk1 = new SkillData
        {
            SkillName = "Bạch Long Thương",
            Type = SkillData.SkillType.Melee,
            MoraleCost = -1,
            DamageMultiplier = 1.0f
        };

        var sk2 = new SkillData
        {
            SkillName = "Long Đảm Thần Uy",
            Type = SkillData.SkillType.Pierce,
            MoraleCost = 100,
            DamageMultiplier = 2.2f
        };
        sk2.Effects.Add(new Godot.Collections.Dictionary {
            { "type", (int)SkillData.EffectType.Stun },
            { "chance", 0.3f },
            { "duration", 1 }
        });

        return new HeroData
        {
            HeroId = "trieu_van",
            HeroName = "Triệu Vân",
            Title = "Thần Uy Thiên Tướng",
            Element = HeroData.ElementType.Water,
            Position = HeroData.PositionType.Front,
            Rarity = HeroData.RarityType.Ssr,
            BaseStats = stats,
            Skills = new Godot.Collections.Array<SkillData> { sk1, sk2 }
        };
    }

    private static HeroData MakeDienVi()
    {
        var stats = new HeroStats
        {
            HpMax = 3200, Strength = 100, Agi = 45, Intel = 20, Speed = 75,
            PhysAtk = 260, PhysDef = 220, MagAtk = 40, MagDef = 80,
            CritRate = 0.05f, DodgeRate = 0.05f, BlockRate = 0.25f,
            HitRate = 0.92f, MoraleInit = 50
        };

        var sk1 = new SkillData
        {
            SkillName = "Song Kích Trọng Kích",
            Type = SkillData.SkillType.Melee,
            MoraleCost = -1,
            DamageMultiplier = 1.1f
        };

        var sk2 = new SkillData
        {
            SkillName = "Hộ Chủ Quyết Tử",
            Type = SkillData.SkillType.AoeRow,
            MoraleCost = 100,
            DamageMultiplier = 1.8f
        };

        return new HeroData
        {
            HeroId = "dien_vi",
            HeroName = "Điển Vi",
            Title = "Cổ Chi Ác Lai",
            Element = HeroData.ElementType.Fire,
            Position = HeroData.PositionType.Front,
            Rarity = HeroData.RarityType.Ssr,
            BaseStats = stats,
            Skills = new Godot.Collections.Array<SkillData> { sk1, sk2 }
        };
    }

    private static HeroData MakeTruongPhi()
    {
        var stats = new HeroStats
        {
            HpMax = 3000, Strength = 105, Agi = 50, Intel = 25, Speed = 80,
            PhysAtk = 300, PhysDef = 190, MagAtk = 30, MagDef = 75,
            CritRate = 0.10f, DodgeRate = 0.05f, BlockRate = 0.15f,
            HitRate = 0.90f, MoraleInit = 50
        };

        var sk1 = new SkillData
        {
            SkillName = "Bát Xà Mâu Trảm",
            Type = SkillData.SkillType.Melee,
            MoraleCost = -1,
            DamageMultiplier = 1.15f
        };

        var sk2 = new SkillData
        {
            SkillName = "Nộ Hống Bản Cầu",
            Type = SkillData.SkillType.AoeRow,
            MoraleCost = 100,
            DamageMultiplier = 1.9f
        };
        sk2.Effects.Add(new Godot.Collections.Dictionary {
            { "type", (int)SkillData.EffectType.Stun },
            { "chance", 0.45f },
            { "duration", 1 }
        });

        return new HeroData
        {
            HeroId = "truong_phi",
            HeroName = "Trương Phi",
            Title = "Vạn Nhân Địch",
            Element = HeroData.ElementType.Fire,
            Position = HeroData.PositionType.Front,
            Rarity = HeroData.RarityType.Ssr,
            BaseStats = stats,
            Skills = new Godot.Collections.Array<SkillData> { sk1, sk2 }
        };
    }

    private static HeroData MakeTaoNhan()
    {
        var stats = new HeroStats
        {
            HpMax = 3500, Strength = 85, Agi = 40, Intel = 45, Speed = 70,
            PhysAtk = 210, PhysDef = 260, MagAtk = 50, MagDef = 130,
            CritRate = 0.03f, DodgeRate = 0.03f, BlockRate = 0.35f,
            HitRate = 0.90f, MoraleInit = 50
        };

        var sk1 = new SkillData
        {
            SkillName = "Kim Cương Kích",
            Type = SkillData.SkillType.Melee,
            MoraleCost = -1,
            DamageMultiplier = 0.95f
        };

        var sk2 = new SkillData
        {
            SkillName = "Trầm Lực Thiết Bích",
            Type = SkillData.SkillType.Support,
            MoraleCost = 100,
            DamageMultiplier = 0.8f
        };
        sk2.Effects.Add(new Godot.Collections.Dictionary {
            { "type", (int)SkillData.EffectType.BuffDef },
            { "chance", 1.0f },
            { "duration", 2 },
            { "value", 50 }
        });

        return new HeroData
        {
            HeroId = "tao_nhan",
            HeroName = "Tào Nhân",
            Title = "Thiết Bích Tướng Quân",
            Element = HeroData.ElementType.Neutral,
            Position = HeroData.PositionType.Front,
            Rarity = HeroData.RarityType.Ssr,
            BaseStats = stats,
            Skills = new Godot.Collections.Array<SkillData> { sk1, sk2 }
        };
    }

    private static HeroData MakeHoangCai()
    {
        var stats = new HeroStats
        {
            HpMax = 2900, Strength = 88, Agi = 48, Intel = 35, Speed = 76,
            PhysAtk = 240, PhysDef = 175, MagAtk = 40, MagDef = 95,
            CritRate = 0.06f, DodgeRate = 0.08f, BlockRate = 0.20f,
            HitRate = 0.92f, MoraleInit = 50
        };

        var sk1 = new SkillData
        {
            SkillName = "Thiết Tiết Kích",
            Type = SkillData.SkillType.Melee,
            MoraleCost = -1,
            DamageMultiplier = 1.0f
        };

        var sk2 = new SkillData
        {
            SkillName = "Khổ Nhục Hỏa Thiuyền",
            Type = SkillData.SkillType.Melee,
            MoraleCost = 100,
            DamageMultiplier = 2.4f
        };
        sk2.Effects.Add(new Godot.Collections.Dictionary {
            { "type", (int)SkillData.EffectType.DebuffDef },
            { "chance", 0.8f },
            { "duration", 2 },
            { "value", -25 }
        });

        return new HeroData
        {
            HeroId = "hoang_cai",
            HeroName = "Hoàng Cái",
            Title = "Tam Triều Nguyên Lão",
            Element = HeroData.ElementType.Water,
            Position = HeroData.PositionType.Front,
            Rarity = HeroData.RarityType.Sr,
            BaseStats = stats,
            Skills = new Godot.Collections.Array<SkillData> { sk1, sk2 }
        };
    }

    // ──────────────────────────────────────────────────────────
    // TRUNG QUÂN (MIDLINE)
    // ──────────────────────────────────────────────────────────

    private static HeroData MakeQuanVu()
    {
        var stats = new HeroStats
        {
            HpMax = 2200, Strength = 110, Agi = 80, Intel = 45, Speed = 100,
            PhysAtk = 340, PhysDef = 130, MagAtk = 80, MagDef = 90,
            CritRate = 0.12f, DodgeRate = 0.05f, BlockRate = 0.05f,
            HitRate = 0.95f, MoraleInit = 50
        };

        var sk1 = new SkillData
        {
            SkillName = "Thanh Long Yển Nguyệt Đao",
            Type = SkillData.SkillType.Melee,
            MoraleCost = -1,
            DamageMultiplier = 1.2f
        };

        var sk2 = new SkillData
        {
            SkillName = "Qua Ngũ Quan Trảm Lục Tướng",
            Type = SkillData.SkillType.AoeRow,
            MoraleCost = 100,
            DamageMultiplier = 2.0f
        };

        return new HeroData
        {
            HeroId = "quan_vu",
            HeroName = "Quan Vũ",
            Title = "Vũ Thánh Thiên Tôn",
            Element = HeroData.ElementType.Fire,
            Position = HeroData.PositionType.Mid,
            Rarity = HeroData.RarityType.Ur,
            BaseStats = stats,
            Skills = new Godot.Collections.Array<SkillData> { sk1, sk2 }
        };
    }

    private static HeroData MakeLuBo()
    {
        var stats = new HeroStats
        {
            HpMax = 2400, Strength = 120, Agi = 100, Intel = 30, Speed = 115,
            PhysAtk = 380, PhysDef = 110, MagAtk = 60, MagDef = 70,
            CritRate = 0.18f, DodgeRate = 0.08f, BlockRate = 0.0f,
            HitRate = 0.95f, MoraleInit = 50
        };

        var sk1 = new SkillData
        {
            SkillName = "Phương Thiên Họa Kích",
            Type = SkillData.SkillType.Melee,
            MoraleCost = -1,
            DamageMultiplier = 1.3f
        };

        var sk2 = new SkillData
        {
            SkillName = "Vô Song Chiến Thần",
            Type = SkillData.SkillType.Ranged,
            MoraleCost = 100,
            DamageMultiplier = 2.8f
        };
        sk2.Effects.Add(new Godot.Collections.Dictionary {
            { "type", (int)SkillData.EffectType.DebuffDef },
            { "chance", 1.0f },
            { "duration", 2 },
            { "value", -30 }
        });

        return new HeroData
        {
            HeroId = "lu_bo",
            HeroName = "Lữ Bố",
            Title = "Nhân Trung Lữ Bố",
            Element = HeroData.ElementType.Wind,
            Position = HeroData.PositionType.Mid,
            Rarity = HeroData.RarityType.Ur,
            BaseStats = stats,
            Skills = new Godot.Collections.Array<SkillData> { sk1, sk2 }
        };
    }

    private static HeroData MakeChuDu()
    {
        var stats = new HeroStats
        {
            HpMax = 2000, Strength = 60, Agi = 95, Intel = 118, Speed = 110,
            PhysAtk = 140, PhysDef = 100, MagAtk = 350, MagDef = 135,
            CritRate = 0.15f, DodgeRate = 0.07f, BlockRate = 0.0f,
            HitRate = 0.96f, MoraleInit = 50
        };

        var sk1 = new SkillData
        {
            SkillName = "Hỏa Kiếm Vũ",
            Type = SkillData.SkillType.Magic,
            MoraleCost = -1,
            DamageMultiplier = 1.05f,
            IsMagicDamage = true
        };

        var sk2 = new SkillData
        {
            SkillName = "Đại Hỏa Xích Bích",
            Type = SkillData.SkillType.AoeFull,
            MoraleCost = 100,
            DamageMultiplier = 1.95f,
            IsMagicDamage = true
        };
        sk2.Effects.Add(new Godot.Collections.Dictionary {
            { "type", (int)SkillData.EffectType.DebuffDef },
            { "chance", 0.5f },
            { "duration", 2 },
            { "value", -20 }
        });

        return new HeroData
        {
            HeroId = "chu_du",
            HeroName = "Chu Du",
            Title = "Mỹ Chu Lang",
            Element = HeroData.ElementType.Fire,
            Position = HeroData.PositionType.Mid,
            Rarity = HeroData.RarityType.Ur,
            BaseStats = stats,
            Skills = new Godot.Collections.Array<SkillData> { sk1, sk2 }
        };
    }

    private static HeroData MakeHoangTrung()
    {
        var stats = new HeroStats
        {
            HpMax = 1900, Strength = 95, Agi = 90, Intel = 50, Speed = 102,
            PhysAtk = 330, PhysDef = 105, MagAtk = 50, MagDef = 85,
            CritRate = 0.22f, DodgeRate = 0.06f, BlockRate = 0.0f,
            HitRate = 1.0f, MoraleInit = 50
        };

        var sk1 = new SkillData
        {
            SkillName = "Tiễn Tiệt Không",
            Type = SkillData.SkillType.Ranged,
            MoraleCost = -1,
            DamageMultiplier = 1.1f
        };

        var sk2 = new SkillData
        {
            SkillName = "Bách Bộ Xuyên Dương",
            Type = SkillData.SkillType.Pierce,
            MoraleCost = 100,
            DamageMultiplier = 2.4f
        };

        return new HeroData
        {
            HeroId = "hoang_trung",
            HeroName = "Hoàng Trung",
            Title = "Thần Xạ Lão Tướng",
            Element = HeroData.ElementType.Wind,
            Position = HeroData.PositionType.Mid,
            Rarity = HeroData.RarityType.Ssr,
            BaseStats = stats,
            Skills = new Godot.Collections.Array<SkillData> { sk1, sk2 }
        };
    }

    private static HeroData MakeMaSieu()
    {
        var stats = new HeroStats
        {
            HpMax = 2150, Strength = 105, Agi = 105, Intel = 35, Speed = 118,
            PhysAtk = 350, PhysDef = 115, MagAtk = 40, MagDef = 75,
            CritRate = 0.16f, DodgeRate = 0.12f, BlockRate = 0.05f,
            HitRate = 0.94f, MoraleInit = 50
        };

        var sk1 = new SkillData
        {
            SkillName = "Tây Lương Mã Đao",
            Type = SkillData.SkillType.Melee,
            MoraleCost = -1,
            DamageMultiplier = 1.15f
        };

        var sk2 = new SkillData
        {
            SkillName = "Thần Uy Đột Kích",
            Type = SkillData.SkillType.Flank,
            MoraleCost = 100,
            DamageMultiplier = 2.5f
        };
        sk2.Effects.Add(new Godot.Collections.Dictionary {
            { "type", (int)SkillData.EffectType.DebuffAtk },
            { "chance", 0.6f },
            { "duration", 2 },
            { "value", -25 }
        });

        return new HeroData
        {
            HeroId = "ma_sieu",
            HeroName = "Mã Siêu",
            Title = "Cẩm Mã Siêu",
            Element = HeroData.ElementType.Wind,
            Position = HeroData.PositionType.Mid,
            Rarity = HeroData.RarityType.Ssr,
            BaseStats = stats,
            Skills = new Godot.Collections.Array<SkillData> { sk1, sk2 }
        };
    }

    private static HeroData MakeDieuThuyen()
    {
        var stats = new HeroStats
        {
            HpMax = 1800, Strength = 45, Agi = 100, Intel = 105, Speed = 114,
            PhysAtk = 110, PhysDef = 90, MagAtk = 310, MagDef = 120,
            CritRate = 0.10f, DodgeRate = 0.14f, BlockRate = 0.0f,
            HitRate = 0.95f, MoraleInit = 50
        };

        var sk1 = new SkillData
        {
            SkillName = "Lụa Hồng Mê Hoặc",
            Type = SkillData.SkillType.Magic,
            MoraleCost = -1,
            DamageMultiplier = 1.0f,
            IsMagicDamage = true
        };

        var sk2 = new SkillData
        {
            SkillName = "Bế Nguyệt Tuyệt Sắc",
            Type = SkillData.SkillType.AoeRow,
            MoraleCost = 100,
            DamageMultiplier = 1.6f,
            IsMagicDamage = true
        };
        sk2.Effects.Add(new Godot.Collections.Dictionary {
            { "type", (int)SkillData.EffectType.Silence },
            { "chance", 0.6f },
            { "duration", 1 }
        });
        sk2.Effects.Add(new Godot.Collections.Dictionary {
            { "type", (int)SkillData.EffectType.DrainMorale },
            { "chance", 1.0f },
            { "duration", 0 },
            { "value", -30 }
        });

        return new HeroData
        {
            HeroId = "dieu_thuyen",
            HeroName = "Điêu Thuyền",
            Title = "Tuyệt Thế Giai Nhân",
            Element = HeroData.ElementType.Dark,
            Position = HeroData.PositionType.Mid,
            Rarity = HeroData.RarityType.Ssr,
            BaseStats = stats,
            Skills = new Godot.Collections.Array<SkillData> { sk1, sk2 }
        };
    }

    // ──────────────────────────────────────────────────────────
    // HẬU QUÂN (BACKLINE)
    // ──────────────────────────────────────────────────────────

    private static HeroData MakeGiaCatLuong()
    {
        var stats = new HeroStats
        {
            HpMax = 1600, Strength = 35, Agi = 85, Intel = 130, Speed = 120,
            PhysAtk = 80, PhysDef = 70, MagAtk = 360, MagDef = 140,
            CritRate = 0.10f, DodgeRate = 0.05f, BlockRate = 0.0f,
            HitRate = 0.95f, MoraleInit = 50
        };

        var sk1 = new SkillData
        {
            SkillName = "Bát Trận Đồ",
            Type = SkillData.SkillType.Magic,
            MoraleCost = -1,
            DamageMultiplier = 1.0f,
            IsMagicDamage = true
        };

        var sk2 = new SkillData
        {
            SkillName = "Lửa Xích Bích",
            Type = SkillData.SkillType.AoeFull,
            MoraleCost = 100,
            DamageMultiplier = 1.6f,
            IsMagicDamage = true
        };
        sk2.Effects.Add(new Godot.Collections.Dictionary {
            { "type", (int)SkillData.EffectType.Silence },
            { "chance", 0.5f },
            { "duration", 1 }
        });

        return new HeroData
        {
            HeroId = "gia_cat_luong",
            HeroName = "Gia Cát Lượng",
            Title = "Ngọa Long Tiên Sinh",
            Element = HeroData.ElementType.Wind,
            Position = HeroData.PositionType.Back,
            Rarity = HeroData.RarityType.Ur,
            BaseStats = stats,
            Skills = new Godot.Collections.Array<SkillData> { sk1, sk2 }
        };
    }

    private static HeroData MakeTieuKieu()
    {
        var stats = new HeroStats
        {
            HpMax = 1400, Strength = 25, Agi = 90, Intel = 110, Speed = 125,
            PhysAtk = 60, PhysDef = 60, MagAtk = 300, MagDef = 120,
            CritRate = 0.08f, DodgeRate = 0.08f, BlockRate = 0.0f,
            HitRate = 0.90f, MoraleInit = 50
        };

        var sk1 = new SkillData
        {
            SkillName = "Khúc Nhạc Hồi Sinh",
            Type = SkillData.SkillType.Support,
            MoraleCost = -1,
            DamageMultiplier = 1.2f
        };

        var sk2 = new SkillData
        {
            SkillName = "Điệu Vũ Thiên Kiều",
            Type = SkillData.SkillType.AoeFull,
            MoraleCost = 100,
            DamageMultiplier = 0.0f
        };
        sk2.Effects.Add(new Godot.Collections.Dictionary {
            { "type", (int)SkillData.EffectType.GrantMorale },
            { "chance", 1.0f },
            { "duration", 0 },
            { "value", 30 }
        });
        sk2.Effects.Add(new Godot.Collections.Dictionary {
            { "type", (int)SkillData.EffectType.DrainMorale },
            { "chance", 1.0f },
            { "duration", 0 },
            { "value", -20 }
        });

        return new HeroData
        {
            HeroId = "tieu_kieu",
            HeroName = "Tiểu Kiều",
            Title = "Giang Nam Nhị Kiều",
            Element = HeroData.ElementType.Water,
            Position = HeroData.PositionType.Back,
            Rarity = HeroData.RarityType.Ssr,
            BaseStats = stats,
            Skills = new Godot.Collections.Array<SkillData> { sk1, sk2 }
        };
    }

    private static HeroData MakeBangThong()
    {
        var stats = new HeroStats
        {
            HpMax = 1550, Strength = 30, Agi = 82, Intel = 126, Speed = 117,
            PhysAtk = 75, PhysDef = 68, MagAtk = 345, MagDef = 138,
            CritRate = 0.09f, DodgeRate = 0.06f, BlockRate = 0.0f,
            HitRate = 0.94f, MoraleInit = 50
        };

        var sk1 = new SkillData
        {
            SkillName = "Hỏa Long Chú",
            Type = SkillData.SkillType.Magic,
            MoraleCost = -1,
            DamageMultiplier = 1.0f,
            IsMagicDamage = true
        };

        var sk2 = new SkillData
        {
            SkillName = "Liên Hoàn Hỏa Kế",
            Type = SkillData.SkillType.AoeRow,
            MoraleCost = 100,
            DamageMultiplier = 1.85f,
            IsMagicDamage = true
        };
        sk2.Effects.Add(new Godot.Collections.Dictionary {
            { "type", (int)SkillData.EffectType.Silence },
            { "chance", 0.65f },
            { "duration", 1 }
        });

        return new HeroData
        {
            HeroId = "bang_thong",
            HeroName = "Bàng Thống",
            Title = "Phượng Sồ Tiên Sinh",
            Element = HeroData.ElementType.Fire,
            Position = HeroData.PositionType.Back,
            Rarity = HeroData.RarityType.Ssr,
            BaseStats = stats,
            Skills = new Godot.Collections.Array<SkillData> { sk1, sk2 }
        };
    }

    private static HeroData MakeSaiVanCo()
    {
        var stats = new HeroStats
        {
            HpMax = 1450, Strength = 20, Agi = 88, Intel = 115, Speed = 122,
            PhysAtk = 50, PhysDef = 65, MagAtk = 320, MagDef = 130,
            CritRate = 0.05f, DodgeRate = 0.07f, BlockRate = 0.0f,
            HitRate = 0.92f, MoraleInit = 50
        };

        var sk1 = new SkillData
        {
            SkillName = "Hồ Gia Khúc",
            Type = SkillData.SkillType.Support,
            MoraleCost = -1,
            DamageMultiplier = 1.3f
        };

        var sk2 = new SkillData
        {
            SkillName = "Thập Bát Phách Linh",
            Type = SkillData.SkillType.Support,
            MoraleCost = 100,
            DamageMultiplier = 2.0f
        };
        sk2.Effects.Add(new Godot.Collections.Dictionary {
            { "type", (int)SkillData.EffectType.GrantMorale },
            { "chance", 1.0f },
            { "duration", 0 },
            { "value", 35 }
        });
        sk2.Effects.Add(new Godot.Collections.Dictionary {
            { "type", (int)SkillData.EffectType.BuffSpeed },
            { "chance", 1.0f },
            { "duration", 2 },
            { "value", 20 }
        });

        return new HeroData
        {
            HeroId = "sai_van_co",
            HeroName = "Sái Văn Cơ",
            Title = "Tuyệt Đại Tài Nữ",
            Element = HeroData.ElementType.Water,
            Position = HeroData.PositionType.Back,
            Rarity = HeroData.RarityType.Ssr,
            BaseStats = stats,
            Skills = new Godot.Collections.Array<SkillData> { sk1, sk2 }
        };
    }
}
