class_name HeroStats
extends Resource

## Thuộc tính gốc
@export var hp_max: int = 1000
@export var strength: int = 50   ## Lực lượng (đổi từ str để tránh trùng built-in)
@export var agi: int = 50        ## Mẫn tiệp
@export var intel: int = 50      ## Trí lực
@export var speed: int = 100     ## Tốc độ hành động

## Thuộc tính chiến đấu
@export var phys_atk: int = 100
@export var phys_def: int = 50
@export var mag_atk: int = 80
@export var mag_def: int = 50

## Thuộc tính đặc biệt %
@export_range(0.0, 1.0) var crit_rate: float = 0.05
@export_range(0.0, 1.0) var anti_crit: float = 0.0
@export_range(0.0, 1.0) var hit_rate: float = 0.9
@export_range(0.0, 1.0) var dodge_rate: float = 0.05
@export_range(0.0, 1.0) var block_rate: float = 0.0
@export_range(0.0, 1.0) var pierce_rate: float = 0.0

## Sĩ khí
@export var morale_init: int = 50

## Runtime state
var hp_current: int = 0
var morale_current: int = 0

func _init() -> void:
	hp_current = hp_max
	morale_current = morale_init

func reset() -> void:
	hp_current = hp_max
	morale_current = morale_init

func is_alive() -> bool:
	return hp_current > 0

func take_damage(amount: int) -> int:
	var actual = clampi(amount, 0, hp_current)
	hp_current -= actual
	return actual

func heal(amount: int) -> int:
	var actual = clampi(amount, 0, hp_max - hp_current)
	hp_current += actual
	return actual

func gain_morale(amount: int) -> void:
	morale_current = clampi(morale_current + amount, 0, 200)

func consume_morale() -> bool:
	if morale_current >= 100:
		morale_current -= 100
		return true
	return false

func can_rage() -> bool:
	return morale_current >= 100
