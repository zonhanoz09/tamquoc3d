extends Node2D
## BattleScene - Màn hình chiến đấu Tam Quốc chuẩn Formation:
## - 1 Tiền quân tiên phong (Frontline)
## - Tối đa 3 Trung quân (Midline)
## - Tối đa 3 Hậu quân (Backline)

var battle_manager: BattleManager
var sound_manager: SoundManager

# Camera & Shake
var camera: Camera2D
var _shake_amount: float = 0.0

# Hero Visuals Storage
var _player_visuals: Array[HeroVisual] = []
var _enemy_visuals: Array[HeroVisual] = []

# UI Nodes
var round_label: Label
var log_label: Label
var speed_btn: Button
var pause_btn: Button
var restart_btn: Button
var log_panel: PanelContainer
var timeline_container: HBoxContainer
var result_banner: Control

var _log_lines: Array = []
const MAX_LOG_LINES = 12

func _get_hero(id: String) -> HeroData:
	var db = get_node_or_null("/root/HeroDatabase")
	if db:
		return db.get_hero(id)
	return null

func _ready() -> void:
	_setup_camera()
	_setup_sound()
	_setup_background()
	_setup_ui()
	_setup_battle()

func _process(delta: float) -> void:
	if _shake_amount > 0:
		_shake_amount = maxf(0.0, _shake_amount - delta * 25.0)
		camera.offset = Vector2(
			randf_range(-_shake_amount, _shake_amount),
			randf_range(-_shake_amount, _shake_amount)
		)

func trigger_screen_shake(amount: float = 8.0) -> void:
	_shake_amount = amount

func _setup_camera() -> void:
	camera = Camera2D.new()
	camera.position = Vector2(640, 360)
	add_child(camera)

func _setup_sound() -> void:
	sound_manager = SoundManager.new()
	add_child(sound_manager)

func _setup_background() -> void:
	var bg_sprite = Sprite2D.new()
	var bg_tex = load("res://assets/sprites/backgrounds/battlefield.png")
	if bg_tex:
		bg_sprite.texture = bg_tex
	bg_sprite.position = Vector2(640, 360)
	bg_sprite.z_index = 0
	add_child(bg_sprite)

func _setup_ui() -> void:
	var ui_layer = CanvasLayer.new()
	ui_layer.layer = 10
	add_child(ui_layer)

	# 1. Top HUD Header (Round + Turn Timeline)
	var top_bar = PanelContainer.new()
	top_bar.position = Vector2(240, 10)
	top_bar.size = Vector2(800, 50)
	var sb_top = StyleBoxFlat.new()
	sb_top.bg_color = Color(0.1, 0.08, 0.15, 0.85)
	sb_top.set_border_width_all(2)
	sb_top.border_color = Color(0.85, 0.7, 0.25)
	sb_top.set_corner_radius_all(6)
	top_bar.add_theme_stylebox_override("panel", sb_top)
	ui_layer.add_child(top_bar)

	var top_hbox = HBoxContainer.new()
	top_hbox.custom_minimum_size = Vector2(780, 44)
	top_hbox.alignment = BoxContainer.ALIGNMENT_CENTER
	top_bar.add_child(top_hbox)

	round_label = Label.new()
	round_label.text = "⚔️ HIỆP 1"
	round_label.add_theme_font_size_override("font_size", 18)
	round_label.add_theme_color_override("font_color", Color(1.0, 0.85, 0.3))
	top_hbox.add_child(round_label)

	var sep = VSeparator.new()
	sep.custom_minimum_size = Vector2(20, 0)
	top_hbox.add_child(sep)

	timeline_container = HBoxContainer.new()
	timeline_container.alignment = BoxContainer.ALIGNMENT_CENTER
	top_hbox.add_child(timeline_container)

	# 2. Control Buttons (Top Right)
	var ctrl_box = HBoxContainer.new()
	ctrl_box.position = Vector2(1060, 14)
	ctrl_box.size = Vector2(200, 36)
	ui_layer.add_child(ctrl_box)

	speed_btn = Button.new()
	speed_btn.text = "Tốc độ: x1"
	speed_btn.custom_minimum_size = Vector2(90, 32)
	speed_btn.pressed.connect(_on_speed_toggle)
	ctrl_box.add_child(speed_btn)

	pause_btn = Button.new()
	pause_btn.text = "Tạm dừng"
	pause_btn.custom_minimum_size = Vector2(85, 32)
	pause_btn.pressed.connect(_on_pause_toggle)
	ctrl_box.add_child(pause_btn)

	# 3. Combat Log (Bottom Left)
	log_panel = PanelContainer.new()
	log_panel.position = Vector2(16, 540)
	log_panel.size = Vector2(360, 165)
	var sb_log = StyleBoxFlat.new()
	sb_log.bg_color = Color(0.05, 0.05, 0.08, 0.75)
	sb_log.set_border_width_all(1)
	sb_log.border_color = Color(0.4, 0.4, 0.5, 0.6)
	sb_log.set_corner_radius_all(4)
	log_panel.add_theme_stylebox_override("panel", sb_log)
	ui_layer.add_child(log_panel)

	var log_vbox = VBoxContainer.new()
	log_panel.add_child(log_vbox)

	var log_title = Label.new()
	log_title.text = "📜 NHẬT KÝ CHIẾN TRẬN"
	log_title.add_theme_font_size_override("font_size", 11)
	log_title.add_theme_color_override("font_color", Color(0.7, 0.7, 0.8))
	log_vbox.add_child(log_title)

	log_label = Label.new()
	log_label.text = "Khởi tạo trận đánh..."
	log_label.autowrap_mode = TextServer.AUTOWRAP_WORD
	log_label.add_theme_font_size_override("font_size", 11)
	log_label.add_theme_color_override("font_color", Color(0.9, 0.9, 0.95))
	log_vbox.add_child(log_label)

	# 4. Result Banner
	result_banner = Control.new()
	result_banner.position = Vector2(0, 0)
	result_banner.size = Vector2(1280, 720)
	result_banner.visible = false
	ui_layer.add_child(result_banner)

	var res_bg = ColorRect.new()
	res_bg.size = Vector2(1280, 720)
	res_bg.color = Color(0, 0, 0, 0.7)
	result_banner.add_child(res_bg)

	var res_lbl = Label.new()
	res_lbl.name = "ResultText"
	res_lbl.position = Vector2(0, 260)
	res_lbl.size = Vector2(1280, 80)
	res_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	res_lbl.add_theme_font_size_override("font_size", 48)
	result_banner.add_child(res_lbl)

	restart_btn = Button.new()
	restart_btn.text = "🔄 ĐẤU LẠI"
	restart_btn.position = Vector2(560, 380)
	restart_btn.size = Vector2(160, 48)
	restart_btn.pressed.connect(_on_restart_pressed)
	result_banner.add_child(restart_btn)

## ──────────────────────────────────────────────────────────
## BỐ CỤC VỊ TRÍ ĐỨNG CHUẨN TAM QUỐC
## - Tiền quân: DUY NHẤT 1 VỊ TRÍ (nhô lên hàng đầu ở giữa)
## - Trung quân: Tối đa 3 vị trí (Trên, Giữa, Dưới)
## - Hậu quân: Tối đa 3 vị trí (Trên, Giữa, Dưới)
## ──────────────────────────────────────────────────────────
func get_slot_position(team: int, row: int, col: int) -> Vector2:
	if team == 0:
		# Player (Bên trái)
		match row:
			0: # Tiền quân (1 ô chính giữa)
				return Vector2(530, 520)
			1: # Trung quân (3 ô: Trên, Giữa, Dưới)
				match col:
					0: return Vector2(370, 420)
					1: return Vector2(340, 520)
					_: return Vector2(370, 620)
			_: # Hậu quân (3 ô: Trên, Giữa, Dưới)
				match col:
					0: return Vector2(190, 390)
					1: return Vector2(160, 490)
					_: return Vector2(190, 590)
	else:
		# Enemy (Bên phải đối xứng)
		match row:
			0: # Tiền quân (1 ô chính giữa)
				return Vector2(750, 520)
			1: # Trung quân (3 ô: Trên, Giữa, Dưới)
				match col:
					0: return Vector2(910, 420)
					1: return Vector2(940, 520)
					_: return Vector2(910, 620)
			_: # Hậu quân (3 ô: Trên, Giữa, Dưới)
				match col:
					0: return Vector2(1090, 390)
					1: return Vector2(1120, 490)
					_: return Vector2(1090, 590)

func _setup_battle() -> void:
	battle_manager = BattleManager.new()
	add_child(battle_manager)

	battle_manager.log_message.connect(_on_log)
	battle_manager.battle_ended.connect(_on_battle_ended)
	battle_manager.round_started.connect(_on_round_started)
	battle_manager.hero_turn_started.connect(_on_hero_turn_started)
	battle_manager.hero_turn_ended.connect(_on_hero_turn_ended)
	battle_manager.hero_acted.connect(_on_hero_acted)
	battle_manager.hero_died.connect(_on_hero_died)

	_start_battle_with_roster()

func _start_battle_with_roster() -> void:
	for v in _player_visuals + _enemy_visuals:
		if is_instance_valid(v):
			v.queue_free()
	_player_visuals.clear()
	_enemy_visuals.clear()

	# Player Team: 1 Tiền quân (Tào Nhân), 2 Trung quân (Quan Vũ, Triệu Vân), 2 Hậu quân (Gia Cát Lượng, Tiểu Kiều)
	var player_party: Array = [
		_get_hero("tao_nhan"),
		_get_hero("quan_vu"),
		_get_hero("trieu_van"),
		_get_hero("gia_cat_luong"),
		_get_hero("tieu_kieu"),
	]

	# Enemy Team: 1 Tiền quân (Điển Vi), 2 Trung quân (Lữ Bố, Hoàng Trung), 2 Hậu quân (Bàng Thống, Sái Văn Cơ)
	var enemy_party: Array = [
		_get_hero("dien_vi"),
		_get_hero("lu_bo"),
		_get_hero("hoang_trung"),
		_get_hero("bang_thong"),
		_get_hero("sai_van_co"),
	]

	battle_manager.setup_battle(player_party, enemy_party)

	# Spawn Player Visuals
	for bh in battle_manager.team0:
		var vis = HeroVisual.new()
		vis.position = get_slot_position(0, bh.slot_row, bh.slot_col)
		vis.home_position = vis.position
		vis.z_index = int(vis.position.y / 10.0)
		add_child(vis)
		vis.setup(bh, false)
		_player_visuals.append(vis)

	# Spawn Enemy Visuals
	for bh in battle_manager.team1:
		var vis = HeroVisual.new()
		vis.position = get_slot_position(1, bh.slot_row, bh.slot_col)
		vis.home_position = vis.position
		vis.z_index = int(vis.position.y / 10.0)
		add_child(vis)
		vis.setup(bh, true)
		_enemy_visuals.append(vis)

	_update_timeline()
	battle_manager.call_deferred("start_battle")

func _update_timeline() -> void:
	for c in timeline_container.get_children():
		c.queue_free()

	if not battle_manager.turn_manager:
		return

	var queue = battle_manager.turn_manager.turn_queue
	for bh in queue:
		if not bh.stats.is_alive():
			continue
		var avatar_rect = TextureRect.new()
		var p_tex = load("res://assets/sprites/portraits/%s.png" % bh.data.hero_id)
		if p_tex:
			avatar_rect.texture = p_tex
		avatar_rect.custom_minimum_size = Vector2(32, 32)
		avatar_rect.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
		var border = Color(0.2, 0.8, 1.0) if bh.team == 0 else Color(1.0, 0.3, 0.3)
		avatar_rect.modulate = border
		timeline_container.add_child(avatar_rect)

## ──────────────────────────────────────────────────────────
## XỬ LÝ SỰ KIỆN CHIẾN ĐẤU & ANIMATION
## ──────────────────────────────────────────────────────────

func _on_round_started(r_num: int) -> void:
	round_label.text = "⚔️ HIỆP %d" % r_num
	_update_timeline()

func _on_hero_turn_started(hero: BattleHero) -> void:
	if hero.node and hero.node.has_method("set_active_turn"):
		hero.node.set_active_turn(true)

func _on_hero_turn_ended(hero: BattleHero) -> void:
	if hero.node and hero.node.has_method("set_active_turn"):
		hero.node.set_active_turn(false)

func _on_hero_acted(hero: BattleHero, action_data: Dictionary) -> void:
	var is_rage: bool = action_data.get("is_rage", false)
	var skill: SkillData = action_data.get("skill")
	var targets: Array = action_data.get("targets", [])
	var results: Array = action_data.get("results", [])

	var attacker_node: HeroVisual = hero.node as HeroVisual
	if not attacker_node:
		return

	# 1. Nếu là Nộ chiêu -> Hiển thị Cut-in Banner & Aura
	if is_rage:
		var cutin = SkillCutIn.new()
		add_child(cutin)
		cutin.show_cut_in(hero.data, skill.skill_name, battle_manager.battle_speed)
		CombatVFX.spawn_rage_aura(self, attacker_node.position)
		trigger_screen_shake(10.0)
		await cutin.finished

	# 2. Thực hiện Animation Tấn công
	if targets.is_empty():
		return

	var primary_target: BattleHero = targets[0]
	var primary_node: HeroVisual = primary_target.node as HeroVisual
	var target_pos = primary_node.position if primary_node else attacker_node.position

	var on_impact = func():
		_apply_action_effects(hero, skill, results)

	var on_complete = func():
		for res in results:
			var t_hero: BattleHero = res["target"]
			if t_hero.node and t_hero.node.has_method("update_bars"):
				t_hero.node.update_bars(true)
				t_hero.node.update_status_icons()
		attacker_node.update_bars(true)
		_update_timeline()

	if skill.skill_type in [SkillData.SkillType.MELEE, SkillData.SkillType.FLANK]:
		attacker_node.animate_attack(target_pos, on_impact, on_complete, battle_manager.battle_speed)
	else:
		attacker_node.animate_ranged_attack(target_pos, on_impact, on_complete, battle_manager.battle_speed)

func _apply_action_effects(attacker: BattleHero, _skill: SkillData, results: Array) -> void:
	for res in results:
		var t_hero: BattleHero = res["target"]
		var t_node: HeroVisual = t_hero.node as HeroVisual
		if not t_node:
			continue

		var pos = t_node.position

		# Support / Heal
		if res["is_support"]:
			CombatVFX.spawn_water(self, pos + Vector2(0, -30))
			sound_manager.play_heal()
			t_node.show_floating_text("+%d HP" % res["heal_amount"], Color(0.2, 0.9, 0.3))
			continue

		# Né tránh
		if res["is_dodge"]:
			sound_manager.play_dodge()
			t_node.animate_hurt(true, false, battle_manager.battle_speed)
			t_node.show_floating_text("NÉ TRÁNH!", Color(0.8, 0.8, 0.85))
			continue

		# Đỡ đòn
		if res["is_block"]:
			sound_manager.play_block()
			t_node.animate_hurt(false, true, battle_manager.battle_speed)
			t_node.show_floating_text("ĐỠ ĐÒN! -%d" % res["damage"], Color(0.4, 0.8, 1.0))
			CombatVFX.spawn_impact(self, pos + Vector2(0, -30), false)
			continue

		# Trúng đòn bình thường hoặc Bạo kích
		var is_crit: bool = res["is_crit"]
		if is_crit:
			sound_manager.play_crit()
			trigger_screen_shake(9.0)
			CombatVFX.spawn_impact(self, pos + Vector2(0, -30), true)
			t_node.show_floating_text("BẠO KÍCH! -%d" % res["damage"], Color(1.0, 0.2, 0.2), true)
		else:
			sound_manager.play_hit()
			CombatVFX.spawn_slash(self, pos + Vector2(0, -30), attacker.team == 1)
			t_node.show_floating_text("-%d" % res["damage"], Color(1.0, 0.9, 0.3))

		# Elemental VFX
		match attacker.data.element:
			HeroData.Element.FIRE: CombatVFX.spawn_fire(self, pos + Vector2(0, -30))
			HeroData.Element.WIND: CombatVFX.spawn_wind(self, pos + Vector2(0, -30))
			HeroData.Element.WATER: CombatVFX.spawn_water(self, pos + Vector2(0, -30))

		t_node.animate_hurt(false, false, battle_manager.battle_speed)

func _on_hero_died(hero: BattleHero) -> void:
	if hero.node and hero.node.has_method("animate_death"):
		hero.node.animate_death(battle_manager.battle_speed)
	_update_timeline()

func _on_log(text: String) -> void:
	_log_lines.append(text)
	if _log_lines.size() > MAX_LOG_LINES:
		_log_lines.pop_front()
	if log_label:
		log_label.text = "\n".join(_log_lines)

func _on_battle_ended(winner_team: int) -> void:
	var is_player_win = (winner_team == 0)
	var title = "🏆 ĐẠI THẮNG QUANG VINH!" if is_player_win else "💀 QUÂN TA BẠI TRẬN!"
	var col = Color(1.0, 0.85, 0.2) if is_player_win else Color(0.9, 0.3, 0.3)

	if sound_manager:
		sound_manager.play_victory()

	result_banner.visible = true
	var lbl = result_banner.get_node("ResultText") as Label
	lbl.text = title
	lbl.add_theme_color_override("font_color", col)

func _on_speed_toggle() -> void:
	if battle_manager.battle_speed == 1.0:
		battle_manager.battle_speed = 2.0
		speed_btn.text = "Tốc độ: x2"
	elif battle_manager.battle_speed == 2.0:
		battle_manager.battle_speed = 4.0
		speed_btn.text = "Tốc độ: x4"
	else:
		battle_manager.battle_speed = 1.0
		speed_btn.text = "Tốc độ: x1"

func _on_pause_toggle() -> void:
	battle_manager.is_paused = not battle_manager.is_paused
	pause_btn.text = "Tiếp tục" if battle_manager.is_paused else "Tạm dừng"

func _on_restart_pressed() -> void:
	result_banner.visible = false
	battle_manager.is_battle_active = false
	_start_battle_with_roster()
