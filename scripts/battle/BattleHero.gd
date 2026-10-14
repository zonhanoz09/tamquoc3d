class_name BattleHero
extends RefCounted

## Đại diện một tướng đang trong trận đấu
## Kết hợp HeroData (static) + runtime state

var data: HeroData
var stats: HeroStats   ## Instance riêng trong trận (clone từ base_stats)
var team: int          ## 0 = Player, 1 = Enemy
var slot_row: int      ## 0=Front, 1=Mid, 2=Back
var slot_col: int      ## 0 hoặc 1 (tối đa 2 tướng/hàng)
var is_alive: bool = true
var status_effects: Array[Dictionary] = []
## Format: { "type": EffectType, "duration": int }

## Node tham chiếu (nếu có scene)
var node: Node = null

func _init(hero_data: HeroData, team_id: int, row: int, col: int) -> void:
	data = hero_data
	team = team_id
	slot_row = row
	slot_col = col
	# Clone stats để không ảnh hưởng resource gốc
	stats = hero_data.base_stats.duplicate(true)
	stats.reset()

func get_position() -> HeroData.Position:
	match slot_row:
		0: return HeroData.Position.FRONT
		1: return HeroData.Position.MID
		_: return HeroData.Position.BACK

func has_status(effect_type: int) -> bool:
	for e in status_effects:
		if e["type"] == effect_type:
			return true
	return false

func is_stunned() -> bool:
	return has_status(SkillData.EffectType.STUN)

func is_silenced() -> bool:
	return has_status(SkillData.EffectType.SILENCE)

func tick_status_effects() -> void:
	var remaining: Array[Dictionary] = []
	for e in status_effects:
		e["duration"] -= 1
		if e["duration"] > 0:
			remaining.append(e)
	status_effects = remaining

func apply_status(effect_type: int, duration: int) -> void:
	# Làm mới nếu đã có
	for e in status_effects:
		if e["type"] == effect_type:
			e["duration"] = maxi(e["duration"], duration)
			return
	status_effects.append({"type": effect_type, "duration": duration})

func get_display_name() -> String:
	return data.hero_name

func get_element() -> int:
	return data.element
