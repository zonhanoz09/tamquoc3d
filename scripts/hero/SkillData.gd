class_name SkillData
extends Resource

enum SkillType {
	MELEE,
	RANGED,
	MAGIC,
	FLANK,
	PIERCE,
	AOE_ROW,
	AOE_FULL,
	SUPPORT,
}

enum EffectType {
	NONE,
	STUN,
	SILENCE,
	DRAIN_MORALE,
	GRANT_MORALE,
	HEAL,
	BUFF_ATK,
	BUFF_DEF,
	BUFF_SPEED,
	DEBUFF_ATK,
	DEBUFF_DEF,
}

@export var skill_name: String = "Unnamed Skill"
@export var description: String = ""
@export var skill_type: SkillType = SkillType.MELEE
@export var morale_cost: int = 100
@export var damage_multiplier: float = 1.0
@export var is_magic_damage: bool = false

## Dùng Array thông thường để có thể assign runtime
var effects: Array = []
## Format: { "type": EffectType.STUN, "chance": 0.5, "duration": 1, "value": 0 }

@export var animation_key: String = ""
