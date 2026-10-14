class_name TurnManager
extends RefCounted

## Quản lý thứ tự lượt chiến đấu theo Speed
## GDD: Đội có tổng Speed cao hơn → đi trước toàn diện
##      Trong cùng lượt → thứ tự theo Speed cá nhân

signal round_started(round_number: int)

var round_number: int = 0
var turn_queue: Array = []
var current_hero: BattleHero = null

## Tất cả tướng trong trận [team0, team1]
var all_heroes: Array = []

func setup(team0: Array, team1: Array) -> void:
	all_heroes.clear()
	all_heroes.append_array(team0)
	all_heroes.append_array(team1)
	round_number = 0
	_rebuild_queue()

func _rebuild_queue() -> void:
	## Xây dựng thứ tự lượt cho round mới
	var alive = all_heroes.filter(func(h): return h.is_alive and h.stats.is_alive())

	## Tính tổng Speed 2 đội
	var speed0 = 0
	var speed1 = 0
	for h in alive:
		if h.team == 0:
			speed0 += h.stats.speed
		else:
			speed1 += h.stats.speed

	## Đội có Speed cao hơn đi trước
	var priority_team = 0 if speed0 >= speed1 else 1

	## Sắp xếp: đội ưu tiên trước, trong mỗi đội sort theo speed cá nhân giảm dần
	var team_priority: Array = alive.filter(func(h): return h.team == priority_team)
	var team_other: Array = alive.filter(func(h): return h.team != priority_team)

	team_priority.sort_custom(func(a, b): return a.stats.speed > b.stats.speed)
	team_other.sort_custom(func(a, b): return a.stats.speed > b.stats.speed)

	turn_queue.clear()
	## Xen kẽ: 1 từ đội ưu tiên, 1 từ đội kia
	var i = 0
	var j = 0
	while i < team_priority.size() or j < team_other.size():
		if i < team_priority.size():
			turn_queue.append(team_priority[i])
			i += 1
		if j < team_other.size():
			turn_queue.append(team_other[j])
			j += 1

func start_next_round() -> void:
	round_number += 1
	_rebuild_queue()
	round_started.emit(round_number)

func get_next_hero() -> BattleHero:
	## Lấy tướng tiếp theo còn sống trong queue
	while not turn_queue.is_empty():
		var hero = turn_queue.pop_front()
		if hero.is_alive and hero.stats.is_alive():
			current_hero = hero
			return hero
	return null

func is_round_over() -> bool:
	return turn_queue.is_empty()

func peek_next_n(n: int) -> Array:
	## Xem trước n tướng tiếp theo (cho UI turn order)
	var result: Array = []
	var checked = 0
	for hero in turn_queue:
		if checked >= n:
			break
		if hero.is_alive and hero.stats.is_alive():
			result.append(hero)
			checked += 1
	return result
