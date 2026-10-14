## HeroDatabase - Autoload singleton chứa dữ liệu tất cả tướng
## KHÔNG dùng class_name vì đã là autoload singleton (gây conflict)
extends Node

var _heroes: Dictionary = {}  ## key: hero_id → HeroData

func _ready() -> void:
	_register_starter_heroes()

func _register_starter_heroes() -> void:
	# TIỀN QUÂN (5 tướng)
	_register(_make_trieu_van())
	_register(_make_dien_vi())
	_register(_make_truong_phi())
	_register(_make_tao_nhan())
	_register(_make_hoang_cai())

	# TRUNG QUÂN (6 tướng)
	_register(_make_quan_vu())
	_register(_make_lu_bo())
	_register(_make_chu_du())
	_register(_make_hoang_trung())
	_register(_make_ma_sieu())
	_register(_make_dieu_thuyen())

	# HẬU QUÂN (4 tướng)
	_register(_make_gia_cat_luong())
	_register(_make_tieu_kieu())
	_register(_make_bang_thong())
	_register(_make_sai_van_co())

	print("HeroDatabase: Loaded %d heroes" % _heroes.size())

func _register(data: HeroData) -> void:
	_heroes[data.hero_id] = data

func get_hero(id: String) -> HeroData:
	return _heroes.get(id, null)

func get_all_heroes() -> Array:
	return _heroes.values()

## ──────────────────────────────────────────────────────────
## TIỀN QUÂN (FRONTLINE - Tanker, Đỡ đòn, Phản kích, Né tránh)
## ──────────────────────────────────────────────────────────

func _make_trieu_van() -> HeroData:
	var stats = HeroStats.new()
	stats.hp_max = 2800; stats.strength = 90; stats.agi = 65; stats.intel = 30; stats.speed = 90
	stats.phys_atk = 280; stats.phys_def = 180; stats.mag_atk = 60; stats.mag_def = 100
	stats.crit_rate = 0.08; stats.dodge_rate = 0.15; stats.block_rate = 0.12
	stats.hit_rate = 0.92; stats.morale_init = 50

	var sk1 = SkillData.new()
	sk1.skill_name = "Bạch Long Thương"; sk1.skill_type = SkillData.SkillType.MELEE
	sk1.morale_cost = -1; sk1.damage_multiplier = 1.0

	var sk2 = SkillData.new()
	sk2.skill_name = "Long Đảm Thần Uy"; sk2.skill_type = SkillData.SkillType.PIERCE
	sk2.morale_cost = 100; sk2.damage_multiplier = 2.2
	sk2.effects = [{"type": SkillData.EffectType.STUN, "chance": 0.3, "duration": 1}]

	var d = HeroData.new()
	d.hero_id = "trieu_van"; d.hero_name = "Triệu Vân"; d.title = "Thần Uy Thiên Tướng"
	d.element = HeroData.Element.WATER; d.position = HeroData.Position.FRONT
	d.rarity = HeroData.Rarity.SSR; d.base_stats = stats; d.skills = [sk1, sk2]
	return d

func _make_dien_vi() -> HeroData:
	var stats = HeroStats.new()
	stats.hp_max = 3200; stats.strength = 100; stats.agi = 45; stats.intel = 20; stats.speed = 75
	stats.phys_atk = 260; stats.phys_def = 220; stats.mag_atk = 40; stats.mag_def = 80
	stats.crit_rate = 0.05; stats.dodge_rate = 0.05; stats.block_rate = 0.25
	stats.hit_rate = 0.92; stats.morale_init = 50

	var sk1 = SkillData.new()
	sk1.skill_name = "Song Kích Trọng Kích"; sk1.skill_type = SkillData.SkillType.MELEE
	sk1.morale_cost = -1; sk1.damage_multiplier = 1.1

	var sk2 = SkillData.new()
	sk2.skill_name = "Hộ Chủ Quyết Tử"; sk2.skill_type = SkillData.SkillType.AOE_ROW
	sk2.morale_cost = 100; sk2.damage_multiplier = 1.8

	var d = HeroData.new()
	d.hero_id = "dien_vi"; d.hero_name = "Điển Vi"; d.title = "Cổ Chi Ác Lai"
	d.element = HeroData.Element.FIRE; d.position = HeroData.Position.FRONT
	d.rarity = HeroData.Rarity.SSR; d.base_stats = stats; d.skills = [sk1, sk2]
	return d

func _make_truong_phi() -> HeroData:
	var stats = HeroStats.new()
	stats.hp_max = 3000; stats.strength = 105; stats.agi = 50; stats.intel = 25; stats.speed = 80
	stats.phys_atk = 300; stats.phys_def = 190; stats.mag_atk = 30; stats.mag_def = 75
	stats.crit_rate = 0.10; stats.dodge_rate = 0.05; stats.block_rate = 0.15
	stats.hit_rate = 0.90; stats.morale_init = 50

	var sk1 = SkillData.new()
	sk1.skill_name = "Bát Xà Mâu Trảm"; sk1.skill_type = SkillData.SkillType.MELEE
	sk1.morale_cost = -1; sk1.damage_multiplier = 1.15

	var sk2 = SkillData.new()
	sk2.skill_name = "Nộ Hống Bản Cầu"; sk2.skill_type = SkillData.SkillType.AOE_ROW
	sk2.morale_cost = 100; sk2.damage_multiplier = 1.9
	sk2.effects = [{"type": SkillData.EffectType.STUN, "chance": 0.45, "duration": 1}]

	var d = HeroData.new()
	d.hero_id = "truong_phi"; d.hero_name = "Trương Phi"; d.title = "Vạn Nhân Địch"
	d.element = HeroData.Element.FIRE; d.position = HeroData.Position.FRONT
	d.rarity = HeroData.Rarity.SSR; d.base_stats = stats; d.skills = [sk1, sk2]
	return d

func _make_tao_nhan() -> HeroData:
	var stats = HeroStats.new()
	stats.hp_max = 3500; stats.strength = 85; stats.agi = 40; stats.intel = 45; stats.speed = 70
	stats.phys_atk = 210; stats.phys_def = 260; stats.mag_atk = 50; stats.mag_def = 130
	stats.crit_rate = 0.03; stats.dodge_rate = 0.03; stats.block_rate = 0.35
	stats.hit_rate = 0.90; stats.morale_init = 50

	var sk1 = SkillData.new()
	sk1.skill_name = "Kim Cương Kích"; sk1.skill_type = SkillData.SkillType.MELEE
	sk1.morale_cost = -1; sk1.damage_multiplier = 0.95

	var sk2 = SkillData.new()
	sk2.skill_name = "Trầm Lực Thiết Bích"; sk2.skill_type = SkillData.SkillType.SUPPORT
	sk2.morale_cost = 100; sk2.damage_multiplier = 0.8
	sk2.effects = [{"type": SkillData.EffectType.BUFF_DEF, "chance": 1.0, "duration": 2, "value": 50}]

	var d = HeroData.new()
	d.hero_id = "tao_nhan"; d.hero_name = "Tào Nhân"; d.title = "Thiết Bích Tướng Quân"
	d.element = HeroData.Element.NEUTRAL; d.position = HeroData.Position.FRONT
	d.rarity = HeroData.Rarity.SSR; d.base_stats = stats; d.skills = [sk1, sk2]
	return d

func _make_hoang_cai() -> HeroData:
	var stats = HeroStats.new()
	stats.hp_max = 2900; stats.strength = 88; stats.agi = 48; stats.intel = 35; stats.speed = 76
	stats.phys_atk = 240; stats.phys_def = 175; stats.mag_atk = 40; stats.mag_def = 95
	stats.crit_rate = 0.06; stats.dodge_rate = 0.08; stats.block_rate = 0.20
	stats.hit_rate = 0.92; stats.morale_init = 50

	var sk1 = SkillData.new()
	sk1.skill_name = "Thiết Tiết Kích"; sk1.skill_type = SkillData.SkillType.MELEE
	sk1.morale_cost = -1; sk1.damage_multiplier = 1.0

	var sk2 = SkillData.new()
	sk2.skill_name = "Khổ Nhục Hỏa Thiuyền"; sk2.skill_type = SkillData.SkillType.MELEE
	sk2.morale_cost = 100; sk2.damage_multiplier = 2.4
	sk2.effects = [{"type": SkillData.EffectType.DEBUFF_DEF, "chance": 0.8, "duration": 2, "value": -25}]

	var d = HeroData.new()
	d.hero_id = "hoang_cai"; d.hero_name = "Hoàng Cái"; d.title = "Tam Triều Nguyên Lão"
	d.element = HeroData.Element.WATER; d.position = HeroData.Position.FRONT
	d.rarity = HeroData.Rarity.SR; d.base_stats = stats; d.skills = [sk1, sk2]
	return d

## ──────────────────────────────────────────────────────────
## TRUNG QUÂN (MIDLINE - Sát thương vật lý/phép, Bạo kích, Đột kích)
## ──────────────────────────────────────────────────────────

func _make_quan_vu() -> HeroData:
	var stats = HeroStats.new()
	stats.hp_max = 2200; stats.strength = 110; stats.agi = 80; stats.intel = 45; stats.speed = 100
	stats.phys_atk = 340; stats.phys_def = 130; stats.mag_atk = 80; stats.mag_def = 90
	stats.crit_rate = 0.12; stats.dodge_rate = 0.05; stats.block_rate = 0.05
	stats.hit_rate = 0.95; stats.morale_init = 50

	var sk1 = SkillData.new()
	sk1.skill_name = "Thanh Long Yển Nguyệt Đao"; sk1.skill_type = SkillData.SkillType.MELEE
	sk1.morale_cost = -1; sk1.damage_multiplier = 1.2

	var sk2 = SkillData.new()
	sk2.skill_name = "Qua Ngũ Quan Trảm Lục Tướng"; sk2.skill_type = SkillData.SkillType.AOE_ROW
	sk2.morale_cost = 100; sk2.damage_multiplier = 2.0

	var d = HeroData.new()
	d.hero_id = "quan_vu"; d.hero_name = "Quan Vũ"; d.title = "Vũ Thánh Thiên Tôn"
	d.element = HeroData.Element.FIRE; d.position = HeroData.Position.MID
	d.rarity = HeroData.Rarity.UR; d.base_stats = stats; d.skills = [sk1, sk2]
	return d

func _make_lu_bo() -> HeroData:
	var stats = HeroStats.new()
	stats.hp_max = 2400; stats.strength = 120; stats.agi = 100; stats.intel = 30; stats.speed = 115
	stats.phys_atk = 380; stats.phys_def = 110; stats.mag_atk = 60; stats.mag_def = 70
	stats.crit_rate = 0.18; stats.dodge_rate = 0.08; stats.block_rate = 0.0
	stats.hit_rate = 0.95; stats.morale_init = 50

	var sk1 = SkillData.new()
	sk1.skill_name = "Phương Thiên Họa Kích"; sk1.skill_type = SkillData.SkillType.MELEE
	sk1.morale_cost = -1; sk1.damage_multiplier = 1.3

	var sk2 = SkillData.new()
	sk2.skill_name = "Vô Song Chiến Thần"; sk2.skill_type = SkillData.SkillType.RANGED
	sk2.morale_cost = 100; sk2.damage_multiplier = 2.8
	sk2.effects = [{"type": SkillData.EffectType.DEBUFF_DEF, "chance": 1.0, "duration": 2, "value": -30}]

	var d = HeroData.new()
	d.hero_id = "lu_bo"; d.hero_name = "Lữ Bố"; d.title = "Nhân Trung Lữ Bố"
	d.element = HeroData.Element.WIND; d.position = HeroData.Position.MID
	d.rarity = HeroData.Rarity.UR; d.base_stats = stats; d.skills = [sk1, sk2]
	return d

func _make_chu_du() -> HeroData:
	var stats = HeroStats.new()
	stats.hp_max = 2000; stats.strength = 60; stats.agi = 95; stats.intel = 118; stats.speed = 110
	stats.phys_atk = 140; stats.phys_def = 100; stats.mag_atk = 350; stats.mag_def = 135
	stats.crit_rate = 0.15; stats.dodge_rate = 0.07; stats.block_rate = 0.0
	stats.hit_rate = 0.96; stats.morale_init = 50

	var sk1 = SkillData.new()
	sk1.skill_name = "Hỏa Kiếm Vũ"; sk1.skill_type = SkillData.SkillType.MAGIC
	sk1.morale_cost = -1; sk1.damage_multiplier = 1.05; sk1.is_magic_damage = true

	var sk2 = SkillData.new()
	sk2.skill_name = "Đại Hỏa Xích Bích"; sk2.skill_type = SkillData.SkillType.AOE_FULL
	sk2.morale_cost = 100; sk2.damage_multiplier = 1.95; sk2.is_magic_damage = true
	sk2.effects = [{"type": SkillData.EffectType.DEBUFF_DEF, "chance": 0.5, "duration": 2, "value": -20}]

	var d = HeroData.new()
	d.hero_id = "chu_du"; d.hero_name = "Chu Du"; d.title = "Mỹ Chu Lang"
	d.element = HeroData.Element.FIRE; d.position = HeroData.Position.MID
	d.rarity = HeroData.Rarity.UR; d.base_stats = stats; d.skills = [sk1, sk2]
	return d

func _make_hoang_trung() -> HeroData:
	var stats = HeroStats.new()
	stats.hp_max = 1900; stats.strength = 95; stats.agi = 90; stats.intel = 50; stats.speed = 102
	stats.phys_atk = 330; stats.phys_def = 105; stats.mag_atk = 50; stats.mag_def = 85
	stats.crit_rate = 0.22; stats.dodge_rate = 0.06; stats.block_rate = 0.0
	stats.hit_rate = 1.0; stats.morale_init = 50

	var sk1 = SkillData.new()
	sk1.skill_name = "Tiễn Tiệt Không"; sk1.skill_type = SkillData.SkillType.RANGED
	sk1.morale_cost = -1; sk1.damage_multiplier = 1.1

	var sk2 = SkillData.new()
	sk2.skill_name = "Bách Bộ Xuyên Dương"; sk2.skill_type = SkillData.SkillType.PIERCE
	sk2.morale_cost = 100; sk2.damage_multiplier = 2.4

	var d = HeroData.new()
	d.hero_id = "hoang_trung"; d.hero_name = "Hoàng Trung"; d.title = "Thần Xạ Lão Tướng"
	d.element = HeroData.Element.WIND; d.position = HeroData.Position.MID
	d.rarity = HeroData.Rarity.SSR; d.base_stats = stats; d.skills = [sk1, sk2]
	return d

func _make_ma_sieu() -> HeroData:
	var stats = HeroStats.new()
	stats.hp_max = 2150; stats.strength = 105; stats.agi = 105; stats.intel = 35; stats.speed = 118
	stats.phys_atk = 350; stats.phys_def = 115; stats.mag_atk = 40; stats.mag_def = 75
	stats.crit_rate = 0.16; stats.dodge_rate = 0.12; stats.block_rate = 0.05
	stats.hit_rate = 0.94; stats.morale_init = 50

	var sk1 = SkillData.new()
	sk1.skill_name = "Tây Lương Mã Đao"; sk1.skill_type = SkillData.SkillType.MELEE
	sk1.morale_cost = -1; sk1.damage_multiplier = 1.15

	var sk2 = SkillData.new()
	sk2.skill_name = "Thần Uy Đột Kích"; sk2.skill_type = SkillData.SkillType.FLANK
	sk2.morale_cost = 100; sk2.damage_multiplier = 2.5
	sk2.effects = [{"type": SkillData.EffectType.DEBUFF_ATK, "chance": 0.6, "duration": 2, "value": -25}]

	var d = HeroData.new()
	d.hero_id = "ma_sieu"; d.hero_name = "Mã Siêu"; d.title = "Cẩm Mã Siêu"
	d.element = HeroData.Element.WIND; d.position = HeroData.Position.MID
	d.rarity = HeroData.Rarity.SSR; d.base_stats = stats; d.skills = [sk1, sk2]
	return d

func _make_dieu_thuyen() -> HeroData:
	var stats = HeroStats.new()
	stats.hp_max = 1800; stats.strength = 45; stats.agi = 100; stats.intel = 105; stats.speed = 114
	stats.phys_atk = 110; stats.phys_def = 90; stats.mag_atk = 310; stats.mag_def = 120
	stats.crit_rate = 0.10; stats.dodge_rate = 0.14; stats.block_rate = 0.0
	stats.hit_rate = 0.95; stats.morale_init = 50

	var sk1 = SkillData.new()
	sk1.skill_name = "Lụa Hồng Mê Hoặc"; sk1.skill_type = SkillData.SkillType.MAGIC
	sk1.morale_cost = -1; sk1.damage_multiplier = 1.0; sk1.is_magic_damage = true

	var sk2 = SkillData.new()
	sk2.skill_name = "Bế Nguyệt Tuyệt Sắc"; sk2.skill_type = SkillData.SkillType.AOE_ROW
	sk2.morale_cost = 100; sk2.damage_multiplier = 1.6; sk2.is_magic_damage = true
	sk2.effects = [
		{"type": SkillData.EffectType.SILENCE, "chance": 0.6, "duration": 1},
		{"type": SkillData.EffectType.DRAIN_MORALE, "chance": 1.0, "duration": 0, "value": -30}
	]

	var d = HeroData.new()
	d.hero_id = "dieu_thuyen"; d.hero_name = "Điêu Thuyền"; d.title = "Tuyệt Thế Giai Nhân"
	d.element = HeroData.Element.DARK; d.position = HeroData.Position.MID
	d.rarity = HeroData.Rarity.SSR; d.base_stats = stats; d.skills = [sk1, sk2]
	return d

## ──────────────────────────────────────────────────────────
## HẬU QUÂN (BACKLINE - Pháp thuật diện rộng, Khống chế, Trị liệu & Sĩ khí)
## ──────────────────────────────────────────────────────────

func _make_gia_cat_luong() -> HeroData:
	var stats = HeroStats.new()
	stats.hp_max = 1600; stats.strength = 35; stats.agi = 85; stats.intel = 130; stats.speed = 120
	stats.phys_atk = 80; stats.phys_def = 70; stats.mag_atk = 360; stats.mag_def = 140
	stats.crit_rate = 0.10; stats.dodge_rate = 0.05; stats.block_rate = 0.0
	stats.hit_rate = 0.95; stats.morale_init = 50

	var sk1 = SkillData.new()
	sk1.skill_name = "Bát Trận Đồ"; sk1.skill_type = SkillData.SkillType.MAGIC
	sk1.morale_cost = -1; sk1.damage_multiplier = 1.0; sk1.is_magic_damage = true

	var sk2 = SkillData.new()
	sk2.skill_name = "Lửa Xích Bích"; sk2.skill_type = SkillData.SkillType.AOE_FULL
	sk2.morale_cost = 100; sk2.damage_multiplier = 1.6; sk2.is_magic_damage = true
	sk2.effects = [{"type": SkillData.EffectType.SILENCE, "chance": 0.5, "duration": 1}]

	var d = HeroData.new()
	d.hero_id = "gia_cat_luong"; d.hero_name = "Gia Cát Lượng"; d.title = "Ngọa Long Tiên Sinh"
	d.element = HeroData.Element.WIND; d.position = HeroData.Position.BACK
	d.rarity = HeroData.Rarity.UR; d.base_stats = stats; d.skills = [sk1, sk2]
	return d

func _make_tieu_kieu() -> HeroData:
	var stats = HeroStats.new()
	stats.hp_max = 1400; stats.strength = 25; stats.agi = 90; stats.intel = 110; stats.speed = 125
	stats.phys_atk = 60; stats.phys_def = 60; stats.mag_atk = 300; stats.mag_def = 120
	stats.crit_rate = 0.08; stats.dodge_rate = 0.08; stats.block_rate = 0.0
	stats.hit_rate = 0.90; stats.morale_init = 50

	var sk1 = SkillData.new()
	sk1.skill_name = "Khúc Nhạc Hồi Sinh"; sk1.skill_type = SkillData.SkillType.SUPPORT
	sk1.morale_cost = -1; sk1.damage_multiplier = 1.2

	var sk2 = SkillData.new()
	sk2.skill_name = "Điệu Vũ Thiên Kiều"; sk2.skill_type = SkillData.SkillType.AOE_FULL
	sk2.morale_cost = 100; sk2.damage_multiplier = 0.0
	sk2.effects = [
		{"type": SkillData.EffectType.GRANT_MORALE, "chance": 1.0, "duration": 0, "value": 30},
		{"type": SkillData.EffectType.DRAIN_MORALE, "chance": 1.0, "duration": 0, "value": -20}
	]

	var d = HeroData.new()
	d.hero_id = "tieu_kieu"; d.hero_name = "Tiểu Kiều"; d.title = "Giang Nam Nhị Kiều"
	d.element = HeroData.Element.WATER; d.position = HeroData.Position.BACK
	d.rarity = HeroData.Rarity.SSR; d.base_stats = stats; d.skills = [sk1, sk2]
	return d

func _make_bang_thong() -> HeroData:
	var stats = HeroStats.new()
	stats.hp_max = 1550; stats.strength = 30; stats.agi = 82; stats.intel = 126; stats.speed = 117
	stats.phys_atk = 75; stats.phys_def = 68; stats.mag_atk = 345; stats.mag_def = 138
	stats.crit_rate = 0.09; stats.dodge_rate = 0.06; stats.block_rate = 0.0
	stats.hit_rate = 0.94; stats.morale_init = 50

	var sk1 = SkillData.new()
	sk1.skill_name = "Hỏa Long Chú"; sk1.skill_type = SkillData.SkillType.MAGIC
	sk1.morale_cost = -1; sk1.damage_multiplier = 1.0; sk1.is_magic_damage = true

	var sk2 = SkillData.new()
	sk2.skill_name = "Liên Hoàn Hỏa Kế"; sk2.skill_type = SkillData.SkillType.AOE_ROW
	sk2.morale_cost = 100; sk2.damage_multiplier = 1.85; sk2.is_magic_damage = true
	sk2.effects = [{"type": SkillData.EffectType.SILENCE, "chance": 0.65, "duration": 1}]

	var d = HeroData.new()
	d.hero_id = "bang_thong"; d.hero_name = "Bàng Thống"; d.title = "Phượng Sồ Tiên Sinh"
	d.element = HeroData.Element.FIRE; d.position = HeroData.Position.BACK
	d.rarity = HeroData.Rarity.SSR; d.base_stats = stats; d.skills = [sk1, sk2]
	return d

func _make_sai_van_co() -> HeroData:
	var stats = HeroStats.new()
	stats.hp_max = 1450; stats.strength = 20; stats.agi = 88; stats.intel = 115; stats.speed = 122
	stats.phys_atk = 50; stats.phys_def = 65; stats.mag_atk = 320; stats.mag_def = 130
	stats.crit_rate = 0.05; stats.dodge_rate = 0.07; stats.block_rate = 0.0
	stats.hit_rate = 0.92; stats.morale_init = 50

	var sk1 = SkillData.new()
	sk1.skill_name = "Hồ Gia Khúc"; sk1.skill_type = SkillData.SkillType.SUPPORT
	sk1.morale_cost = -1; sk1.damage_multiplier = 1.3

	var sk2 = SkillData.new()
	sk2.skill_name = "Thập Bát Phách Linh"; sk2.skill_type = SkillData.SkillType.SUPPORT
	sk2.morale_cost = 100; sk2.damage_multiplier = 2.0
	sk2.effects = [
		{"type": SkillData.EffectType.GRANT_MORALE, "chance": 1.0, "duration": 0, "value": 35},
		{"type": SkillData.EffectType.BUFF_SPEED, "chance": 1.0, "duration": 2, "value": 20}
	]

	var d = HeroData.new()
	d.hero_id = "sai_van_co"; d.hero_name = "Sái Văn Cơ"; d.title = "Tuyệt Đại Tài Nữ"
	d.element = HeroData.Element.WATER; d.position = HeroData.Position.BACK
	d.rarity = HeroData.Rarity.SSR; d.base_stats = stats; d.skills = [sk1, sk2]
	return d
