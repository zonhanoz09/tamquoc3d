class_name BattleManager
extends Node

## Điều phối toàn bộ trận đấu theo GDD
## Server-authoritative: BattleManager tính toán kết quả, Visual chỉ hiển thị

signal battle_started
signal battle_ended(winner_team: int)  ## 0 = Player thắng, 1 = Enemy thắng
signal round_started(round_number: int)
signal hero_turn_started(hero: BattleHero)
signal hero_turn_ended(hero: BattleHero)
signal hero_acted(hero: BattleHero, action_data: Dictionary)
signal hero_died(hero: BattleHero)
signal damage_dealt(target: BattleHero, amount: int, is_crit: bool)
signal morale_changed(hero: BattleHero, new_value: int)
signal log_message(text: String)

var turn_manager: TurnManager
var team0: Array = []  ## Player
var team1: Array = []  ## Enemy
var is_battle_active: bool = false
var battle_speed: float = 1.0  ## 1.0 = x1, 2.0 = x2, 4.0 = x4
var is_paused: bool = false

## Setup trận đấu với 2 danh sách HeroData
func setup_battle(
	player_heroes: Array,
	enemy_heroes: Array
) -> void:
	team0.clear()
	team1.clear()

	# Assign heroes vào slot theo thứ tự [Front0, Front1, Mid0, Mid1, Back0, Back1]
	var rows = [0, 0, 1, 1, 2, 2]
	var cols = [0, 1, 0, 1, 0, 1]

	for i in range(mini(player_heroes.size(), 6)):
		var bh = BattleHero.new(player_heroes[i], 0, rows[i], cols[i])
		team0.append(bh)

	for i in range(mini(enemy_heroes.size(), 6)):
		var bh = BattleHero.new(enemy_heroes[i], 1, rows[i], cols[i])
		team1.append(bh)

	turn_manager = TurnManager.new()
	turn_manager.setup(team0, team1)

func start_battle() -> void:
	if team0.is_empty() or team1.is_empty():
		push_error("BattleManager: Cần ít nhất 1 tướng mỗi bên!")
		return
	is_battle_active = true
	battle_started.emit()
	_log("⚔️ Trận chiến bắt đầu!")
	_run_battle()

## Vòng lặp chính bất đồng bộ
func _run_battle() -> void:
	while is_battle_active:
		turn_manager.start_next_round()
		round_started.emit(turn_manager.round_number)
		_log("--- Round %d ---" % turn_manager.round_number)

		while not turn_manager.is_round_over() and is_battle_active:
			while is_paused:
				await get_tree().create_timer(0.1).timeout

			var hero = turn_manager.get_next_hero()
			if hero == null:
				break

			if not hero.stats.is_alive():
				continue

			# Tick status effects đầu lượt
			hero.tick_status_effects()

			# Skip nếu bị Choáng
			if hero.is_stunned():
				_log("  🌀 %s bị CHOÁNG, bỏ lượt!" % hero.get_display_name())
				if hero.node and hero.node.has_method("show_floating_text"):
					hero.node.show_floating_text("CHOÁNG!", Color(1.0, 0.8, 0.2))
				await get_tree().create_timer(0.3 / battle_speed).timeout
				continue

			hero_turn_started.emit(hero)

			# Thực hiện hành động lượt này
			await _execute_hero_turn(hero)

			hero_turn_ended.emit(hero)

			# Kiểm tra kết thúc trận
			var winner = _check_winner()
			if winner >= 0:
				is_battle_active = false
				battle_ended.emit(winner)
				_log("🏆 Đội %d CHIẾN THẮNG!" % winner)
				return

		await get_tree().create_timer(0.2 / battle_speed).timeout

## Thực hiện lượt của 1 tướng
func _execute_hero_turn(hero: BattleHero) -> void:
	var skill_to_use: SkillData = null
	var is_rage = false

	# Kiểm tra Nộ chiêu
	if not hero.is_silenced() and hero.stats.can_rage() and hero.data.skills.size() > 0:
		for sk in hero.data.skills:
			if sk.morale_cost == 100:
				skill_to_use = sk
				is_rage = true
				break

	# Nếu không có Nộ chiêu → đòn đánh thường
	if skill_to_use == null:
		skill_to_use = _get_normal_attack(hero)

	if skill_to_use == null:
		_log("  %s không có hành động!" % hero.get_display_name())
		return

	# Chọn mục tiêu theo targeting rules
	var targets = _resolve_targets(hero, skill_to_use)
	if targets.is_empty():
		_log("  %s không có mục tiêu!" % hero.get_display_name())
		return

	if is_rage:
		hero.stats.consume_morale()
		_log("  💥 %s thi triển NỘ CHIÊU: %s!" % [hero.get_display_name(), skill_to_use.skill_name])
	else:
		_log("  ⚔️ %s tấn công!" % hero.get_display_name())

	# Thu thập kết quả sát thương / hiệu ứng lên từng mục tiêu
	var action_data = {
		"attacker": hero,
		"skill": skill_to_use,
		"is_rage": is_rage,
		"targets": targets,
		"results": []
	}

	for target in targets:
		var res = _calculate_skill_result(hero, target, skill_to_use)
		action_data["results"].append(res)

	# Gửi signal cho Visual xử lý animation
	hero_acted.emit(hero, action_data)

	# Chờ animation hoàn tất qua delegate/timer
	var anim_dur = 0.55 / battle_speed
	if is_rage:
		anim_dur = 0.95 / battle_speed
	await get_tree().create_timer(anim_dur).timeout

func _get_normal_attack(hero: BattleHero) -> SkillData:
	for sk in hero.data.skills:
		if sk.morale_cost == -1:
			return sk
	var basic = SkillData.new()
	basic.skill_name = "Đòn Đánh Thường"
	basic.skill_type = SkillData.SkillType.MELEE
	basic.morale_cost = -1
	basic.damage_multiplier = 1.0
	return basic

## Targeting Rules theo GDD
func _resolve_targets(attacker: BattleHero, skill: SkillData) -> Array:
	var enemy_team = team1 if attacker.team == 0 else team0
	var ally_team  = team0 if attacker.team == 0 else team1
	var alive_enemies = enemy_team.filter(func(h): return h.stats.is_alive())
	var alive_allies  = ally_team.filter(func(h): return h.stats.is_alive())
	var targets: Array = []

	match skill.skill_type:
		SkillData.SkillType.MELEE:
			targets = _get_frontmost_heroes(alive_enemies, 1)

		SkillData.SkillType.RANGED, SkillData.SkillType.MAGIC:
			targets = _get_priority_target(alive_enemies)

		SkillData.SkillType.FLANK:
			var back = alive_enemies.filter(func(h): return h.slot_row == 2)
			if back.is_empty():
				back = alive_enemies.filter(func(h): return h.slot_row == 1)
			if back.is_empty():
				back = alive_enemies
			targets = [back[0]] if not back.is_empty() else []

		SkillData.SkillType.PIERCE:
			var col = attacker.slot_col
			targets = alive_enemies.filter(func(h): return h.slot_col == col)
			if targets.is_empty():
				targets = _get_frontmost_heroes(alive_enemies, 1)

		SkillData.SkillType.AOE_ROW:
			targets = _get_fullest_row(alive_enemies)

		SkillData.SkillType.AOE_FULL:
			targets = alive_enemies.duplicate()

		SkillData.SkillType.SUPPORT:
			if alive_allies.is_empty():
				return []
			alive_allies.sort_custom(func(a, b):
				return a.stats.hp_current < b.stats.hp_current)
			targets = [alive_allies[0]]

	return targets

func _get_frontmost_heroes(enemies: Array, count: int) -> Array:
	for row in [0, 1, 2]:
		var row_heroes = enemies.filter(func(h): return h.slot_row == row)
		if not row_heroes.is_empty():
			return row_heroes.slice(0, mini(count, row_heroes.size()))
	return []

func _get_priority_target(enemies: Array) -> Array:
	if enemies.is_empty():
		return []
	for row in [2, 1, 0]:
		var row_h = enemies.filter(func(h): return h.slot_row == row)
		if not row_h.is_empty():
			return [row_h[0]]
	return [enemies[0]]

func _get_fullest_row(enemies: Array) -> Array:
	var best_row = -1
	var best_count = 0
	for row in [0, 1, 2]:
		var c = enemies.filter(func(h): return h.slot_row == row).size()
		if c > best_count:
			best_count = c
			best_row = row
	return enemies.filter(func(h): return h.slot_row == best_row)

## Tính toán kết quả đòn đánh
func _calculate_skill_result(attacker: BattleHero, target: BattleHero, skill: SkillData) -> Dictionary:
	var res = {
		"target": target,
		"is_support": skill.skill_type == SkillData.SkillType.SUPPORT,
		"heal_amount": 0,
		"damage": 0,
		"is_crit": false,
		"is_dodge": false,
		"is_block": false,
		"is_counter": false,
		"is_dead": false
	}

	if res["is_support"]:
		var heal_amt = CombatFormulas.calc_heal(attacker.stats, skill.damage_multiplier)
		target.stats.heal(heal_amt)
		res["heal_amount"] = heal_amt
		_log("    💚 %s hồi %d HP cho %s" % [attacker.get_display_name(), heal_amt, target.get_display_name()])
		return res

	var combat_res: Dictionary
	if skill.is_magic_damage:
		combat_res = CombatFormulas.calc_magic_damage(
			attacker.stats, target.stats,
			attacker.get_element(), target.get_element(),
			skill.damage_multiplier
		)
	else:
		combat_res = CombatFormulas.calc_physical_damage(
			attacker.stats, target.stats,
			attacker.get_element(), target.get_element(),
			skill.damage_multiplier
		)

	res["is_dodge"] = combat_res["is_dodge"]
	res["is_crit"] = combat_res["is_crit"]
	res["is_block"] = combat_res["is_block"]
	res["is_counter"] = combat_res["is_counter"]

	if res["is_dodge"]:
		_log("    💨 %s NÉ TRÁNH thành công!" % target.get_display_name())
		damage_dealt.emit(target, 0, false)
		return res

	var dmg: int = combat_res["damage"]
	res["damage"] = dmg
	target.stats.take_damage(dmg)
	damage_dealt.emit(target, dmg, res["is_crit"])

	var log_txt = "    💥 %s gây %d sát thương lên %s" % [
		attacker.get_display_name(), dmg, target.get_display_name()
	]
	if res["is_crit"]: log_txt += " [BẠO KÍCH!]"
	if res["is_block"]: log_txt += " [ĐỠ ĐÒN]"
	_log(log_txt)

	# Sĩ khí
	attacker.stats.gain_morale(combat_res["morale_gain_attacker"])
	target.stats.gain_morale(combat_res["morale_gain_defender"])
	morale_changed.emit(attacker, attacker.stats.morale_current)
	morale_changed.emit(target, target.stats.morale_current)

	# Phản công nếu đỡ đòn
	if res["is_counter"] and target.stats.is_alive():
		_log("    🔄 %s PHẢN CÔNG!" % target.get_display_name())
		var counter_result = CombatFormulas.calc_physical_damage(
			target.stats, attacker.stats,
			target.get_element(), attacker.get_element(), 1.0
		)
		if not counter_result["is_dodge"]:
			attacker.stats.take_damage(counter_result["damage"])
			damage_dealt.emit(attacker, counter_result["damage"], counter_result["is_crit"])
			if not attacker.stats.is_alive():
				_handle_death(attacker)

	# Status effects
	for eff in skill.effects:
		if randf() < eff.get("chance", 1.0):
			target.apply_status(eff["type"], eff.get("duration", 1))

	# Kiểm tra chết
	if not target.stats.is_alive():
		_handle_death(target)
		res["is_dead"] = true

	return res

func _handle_death(hero: BattleHero) -> void:
	hero.is_alive = false
	hero_died.emit(hero)
	_log("    💀 %s đã ngã xuống!" % hero.get_display_name())

func _check_winner() -> int:
	var t0_alive = team0.any(func(h): return h.stats.is_alive())
	var t1_alive = team1.any(func(h): return h.stats.is_alive())
	if not t1_alive:
		return 0  ## Player thắng
	if not t0_alive:
		return 1  ## Enemy thắng
	return -1    ## Trận chưa kết thúc

func _log(msg: String) -> void:
	print(msg)
	log_message.emit(msg)
