class_name CombatFormulas
extends RefCounted

## Bảng tương khắc nguyên tố
## Returns multiplier cho damage
static func element_multiplier(attacker_element: int, defender_element: int) -> float:
	var e = HeroData.Element
	# Lửa → Gió → Nước → Lửa
	var beats: Dictionary = {
		e.FIRE:  e.WIND,
		e.WIND:  e.WATER,
		e.WATER: e.FIRE,
		e.LIGHT: e.DARK,
		e.DARK:  e.LIGHT,
	}
	if beats.get(attacker_element, -1) == defender_element:
		return 1.30  ## Khắc hệ: +30% sát thương
	# Bị khắc ngược lại
	if beats.get(defender_element, -1) == attacker_element:
		return 0.80  ## Bị khắc: -20% sát thương
	return 1.0

## Crit multiplier cộng thêm từ nguyên tố khắc
static func element_crit_bonus(attacker_element: int, defender_element: int) -> float:
	var e = HeroData.Element
	var beats: Dictionary = {
		e.FIRE:  e.WIND,
		e.WIND:  e.WATER,
		e.WATER: e.FIRE,
		e.LIGHT: e.DARK,
		e.DARK:  e.LIGHT,
	}
	if beats.get(attacker_element, -1) == defender_element:
		return 0.15  ## +15% tỉ lệ bạo kích khi khắc hệ
	return 0.0

## Tính sát thương vật lý cơ bản
## Returns: { "damage": int, "is_crit": bool, "is_dodge": bool, "is_block": bool, "is_counter": bool }
static func calc_physical_damage(
	attacker: HeroStats,
	defender: HeroStats,
	attacker_element: int,
	defender_element: int,
	skill_multiplier: float = 1.0
) -> Dictionary:
	var result = {
		"damage": 0,
		"is_crit": false,
		"is_dodge": false,
		"is_block": false,
		"is_counter": false,
		"morale_gain_attacker": 0,
		"morale_gain_defender": 0,
	}

	# Né tránh kiểm tra trước
	var effective_dodge = maxf(0.0, defender.dodge_rate - attacker.hit_rate + 0.9)
	if randf() < effective_dodge:
		result["is_dodge"] = true
		# Né thành công: kẻ tấn công KHÔNG nhận Sĩ khí
		result["morale_gain_attacker"] = 0
		result["morale_gain_defender"] = 0
		return result

	# Đỡ đòn (Block)
	var effective_block = maxf(0.0, defender.block_rate - attacker.pierce_rate)
	if randf() < effective_block:
		result["is_block"] = true
		result["is_counter"] = true

	# Bạo kích (Crit)
	var eff_crit = attacker.crit_rate + element_crit_bonus(attacker_element, defender_element)
	eff_crit = maxf(0.0, eff_crit - defender.anti_crit)
	var crit_multiplier = 1.0
	if randf() < eff_crit:
		result["is_crit"] = true
		crit_multiplier = randf_range(1.5, 2.0)

	# Tính sát thương
	var elem_mult = element_multiplier(attacker_element, defender_element)
	var base_dmg = float(attacker.phys_atk) * skill_multiplier * elem_mult * crit_multiplier
	var reduced = base_dmg - float(defender.phys_def)
	if result["is_block"]:
		reduced *= 0.5  # Đỡ đòn: giảm 50%

	result["damage"] = maxi(1, int(reduced))

	# Sĩ khí
	result["morale_gain_attacker"] = 25
	result["morale_gain_defender"] = 25

	return result

## Tính sát thương phép thuật/sách lược
static func calc_magic_damage(
	attacker: HeroStats,
	defender: HeroStats,
	attacker_element: int,
	defender_element: int,
	skill_multiplier: float = 1.0
) -> Dictionary:
	var result = {
		"damage": 0,
		"is_crit": false,
		"is_dodge": false,
		"is_block": false,
		"is_counter": false,
		"morale_gain_attacker": 25,
		"morale_gain_defender": 25,
	}

	var eff_crit = attacker.crit_rate + element_crit_bonus(attacker_element, defender_element)
	eff_crit = maxf(0.0, eff_crit - defender.anti_crit)
	var crit_multiplier = 1.0
	if randf() < eff_crit:
		result["is_crit"] = true
		crit_multiplier = randf_range(1.5, 2.0)

	var elem_mult = element_multiplier(attacker_element, defender_element)
	var base_dmg = float(attacker.mag_atk) * skill_multiplier * elem_mult * crit_multiplier
	var reduced = base_dmg - float(defender.mag_def)
	result["damage"] = maxi(1, int(reduced))

	return result

## Tính hồi máu
static func calc_heal(caster: HeroStats, skill_multiplier: float = 1.0) -> int:
	return int(float(caster.mag_atk) * skill_multiplier)
