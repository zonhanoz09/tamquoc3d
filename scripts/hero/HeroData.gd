class_name HeroData
extends Resource

enum Element { FIRE, WIND, WATER, LIGHT, DARK, NEUTRAL }
enum Position { FRONT, MID, BACK }
enum Rarity { R, SR, SSR, UR }

@export var hero_id: String = ""
@export var hero_name: String = "Unknown Hero"
@export var title: String = ""
@export var element: Element = Element.NEUTRAL
@export var position: Position = Position.FRONT
@export var rarity: Rarity = Rarity.R
@export var base_stats: HeroStats

## Dùng Array thông thường để assign runtime không bị lỗi
var skills: Array = []
