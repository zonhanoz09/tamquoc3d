extends Node
## GameManager - Autoload toàn cục
## Quản lý trạng thái game, dữ liệu người chơi, chuyển scene

signal scene_changed(scene_name: String)

## Trạng thái game
enum GameState {
	MAIN_MENU,
	BATTLE,
	RESULT,
}

var current_state: GameState = GameState.MAIN_MENU
var player_team: Array[HeroData] = []
var gold: int = 0
var gems: int = 0
var stamina: int = 100
const MAX_STAMINA: int = 120

## Danh sách tướng player sở hữu
var owned_heroes: Array[HeroData] = []

func _ready() -> void:
	print("🎮 GameManager khởi động - Project Pixel Tactics")

func change_scene(path: String) -> void:
	scene_changed.emit(path)
	get_tree().change_scene_to_file(path)

func start_battle(player_heroes: Array[HeroData], _enemy_heroes: Array[HeroData] = []) -> void:
	current_state = GameState.BATTLE
	player_team = player_heroes

func add_gold(amount: int) -> void:
	gold += amount

func spend_gold(amount: int) -> bool:
	if gold < amount:
		return false
	gold -= amount
	return true

func add_gems(amount: int) -> void:
	gems += amount

func spend_gems(amount: int) -> bool:
	if gems < amount:
		return false
	gems -= amount
	return true
