class_name HeroVisual
extends Node2D
## HeroVisual - Thực thể đồ họa & hoạt họa HD-2D của vị tướng trong trận đấu

var battle_hero: BattleHero
var is_enemy: bool = false
var home_position: Vector2 = Vector2.ZERO

# Child Nodes
var pedestal: Sprite2D
var skill_ready_aura: Sprite2D
var active_pedestal: Sprite2D
var sprite: Sprite2D

# Smooth Overhead Bars
var hp_ghost_bar: ProgressBar
var hp_bar: ProgressBar
var hp_label: Label
var name_label: Label
var elem_icon: Sprite2D
var status_row: HBoxContainer

# Animation State
var _idle_tween: Tween
var _hp_tween: Tween
var _ghost_tween: Tween
var _active_tween: Tween
var _skill_ready_tween: Tween
var _is_dead: bool = false

func setup(bh: BattleHero, enemy: bool) -> void:
	battle_hero = bh
	is_enemy = enemy
	battle_hero.node = self
	_build_visuals()
	update_bars(false)
	play_idle()

func _build_visuals() -> void:
	# 1. Bệ đứng võ đài chiến thuật (Tactical Pedestal)
	pedestal = Sprite2D.new()
	var p_name = "pedestal_enemy" if is_enemy else "pedestal_player"
	var p_tex = load("res://assets/sprites/ui/%s.png" % p_name)
	if p_tex:
		pedestal.texture = p_tex
	pedestal.position = Vector2(0, 0)
	pedestal.scale = Vector2(1.15, 1.15)
	pedestal.z_index = 0
	add_child(pedestal)

	# 2. Hiệu ứng dưới chân khi tích đầy Nộ/Kỹ năng (Skill Ready Blazing Aura)
	skill_ready_aura = Sprite2D.new()
	var sr_tex = load("res://assets/sprites/ui/pedestal_skill_ready.png")
	if sr_tex:
		skill_ready_aura.texture = sr_tex
	skill_ready_aura.position = Vector2(0, 0)
	skill_ready_aura.scale = Vector2(1.15, 1.15)
	skill_ready_aura.visible = false
	skill_ready_aura.z_index = 1
	add_child(skill_ready_aura)

	# 3. Vòng sáng bệ đứng khi tới lượt hành động (Active Turn Glow)
	active_pedestal = Sprite2D.new()
	var a_tex = load("res://assets/sprites/ui/pedestal_active.png")
	if a_tex:
		active_pedestal.texture = a_tex
	active_pedestal.position = Vector2(0, 0)
	active_pedestal.scale = Vector2(1.15, 1.15)
	active_pedestal.visible = false
	active_pedestal.z_index = 2
	add_child(active_pedestal)

	# 4. Hero Sprite (64x64 HD Spritesheet)
	# Căn chỉnh để gót chân (y = 56 trong frame 64x64) đặt chuẩn xác tại y = 0 trên bệ đứng
	sprite = Sprite2D.new()
	var spr_path = "res://assets/sprites/heroes/%s.png" % battle_hero.data.hero_id
	var tex = load(spr_path)
	if tex:
		sprite.texture = tex
		sprite.hframes = 4
		sprite.frame = 0
	sprite.position = Vector2(0, -48) # Đưa gót chân về chính xác y = 0
	sprite.scale = Vector2(2.0, 2.0)
	sprite.flip_h = is_enemy
	sprite.z_index = 3
	add_child(sprite)

	# 5. Overhead UI (Thanh máu dài & tên tướng)
	_build_overhead_ui()

func _build_overhead_ui() -> void:
	var overhead = Node2D.new()
	overhead.position = Vector2(0, -114)
	overhead.z_index = 35
	add_child(overhead)

	# Element Icon
	elem_icon = Sprite2D.new()
	var elem_name = "neutral"
	match battle_hero.data.element:
		HeroData.Element.FIRE: elem_name = "fire"
		HeroData.Element.WATER: elem_name = "water"
		HeroData.Element.WIND: elem_name = "wind"
		HeroData.Element.LIGHT: elem_name = "light"
		HeroData.Element.DARK: elem_name = "dark"
	var e_tex = load("res://assets/sprites/ui/elem_%s.png" % elem_name)
	if e_tex:
		elem_icon.texture = e_tex
	elem_icon.scale = Vector2(0.9, 0.9)
	elem_icon.position = Vector2(-70, 4)
	overhead.add_child(elem_icon)

	# Name Label
	name_label = Label.new()
	name_label.text = battle_hero.data.hero_name
	name_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	name_label.position = Vector2(-60, -22)
	name_label.size = Vector2(120, 18)
	name_label.add_theme_font_size_override("font_size", 13)
	name_label.add_theme_color_override("font_shadow_color", Color.BLACK)
	name_label.add_theme_constant_override("shadow_offset_x", 1)
	name_label.add_theme_constant_override("shadow_offset_y", 1)
	name_label.add_theme_constant_override("shadow_outline_size", 2)

	var rarity_color = Color(0.85, 0.85, 0.9)
	match battle_hero.data.rarity:
		HeroData.Rarity.UR:  rarity_color = Color(1.0, 0.35, 0.35)
		HeroData.Rarity.SSR: rarity_color = Color(1.0, 0.88, 0.25)
		HeroData.Rarity.SR:  rarity_color = Color(0.75, 0.5, 0.95)
	name_label.add_theme_color_override("font_color", rarity_color)
	overhead.add_child(name_label)

	# ── HP GHOST BAR (Vạch tụt máu trễ - dài 116px) ──
	hp_ghost_bar = ProgressBar.new()
	hp_ghost_bar.position = Vector2(-58, 0)
	hp_ghost_bar.size = Vector2(116, 9)
	hp_ghost_bar.show_percentage = false
	hp_ghost_bar.max_value = battle_hero.stats.hp_max
	hp_ghost_bar.value = battle_hero.stats.hp_current

	var sb_ghost_bg = StyleBoxFlat.new()
	sb_ghost_bg.bg_color = Color(0.12, 0.1, 0.15, 0.9)
	sb_ghost_bg.set_border_width_all(1)
	sb_ghost_bg.border_color = Color(0.05, 0.05, 0.08)
	sb_ghost_bg.set_corner_radius_all(2)
	hp_ghost_bar.add_theme_stylebox_override("background", sb_ghost_bg)

	var sb_ghost_fill = StyleBoxFlat.new()
	sb_ghost_fill.bg_color = Color(0.95, 0.85, 0.3, 0.85)
	sb_ghost_fill.set_corner_radius_all(2)
	hp_ghost_bar.add_theme_stylebox_override("fill", sb_ghost_fill)
	overhead.add_child(hp_ghost_bar)

	# ── HP MAIN BAR (Thanh máu chính - dài 116px) ──
	hp_bar = ProgressBar.new()
	hp_bar.position = Vector2(-58, 0)
	hp_bar.size = Vector2(116, 9)
	hp_bar.show_percentage = false
	hp_bar.max_value = battle_hero.stats.hp_max
	hp_bar.value = battle_hero.stats.hp_current

	var sb_empty = StyleBoxEmpty.new()
	hp_bar.add_theme_stylebox_override("background", sb_empty)

	var sb_fill = StyleBoxFlat.new()
	sb_fill.bg_color = Color(0.2, 0.88, 0.35)
	sb_fill.set_border_width_all(1)
	sb_fill.border_color = Color(0.1, 0.45, 0.2)
	sb_fill.set_corner_radius_all(2)
	hp_bar.add_theme_stylebox_override("fill", sb_fill)
	overhead.add_child(hp_bar)

	# HP Numeric Label ("2,450 / 2,800")
	hp_label = Label.new()
	hp_label.text = "%d/%d" % [battle_hero.stats.hp_current, battle_hero.stats.hp_max]
	hp_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	hp_label.position = Vector2(-58, -1)
	hp_label.size = Vector2(116, 11)
	hp_label.add_theme_font_size_override("font_size", 9)
	hp_label.add_theme_color_override("font_color", Color(1.0, 1.0, 1.0, 0.95))
	hp_label.add_theme_color_override("font_shadow_color", Color(0, 0, 0, 0.9))
	hp_label.add_theme_constant_override("shadow_outline_size", 1)
	overhead.add_child(hp_label)

	# Status Icons Row
	status_row = HBoxContainer.new()
	status_row.position = Vector2(-58, -36)
	status_row.size = Vector2(116, 14)
	status_row.alignment = BoxContainer.ALIGNMENT_CENTER
	overhead.add_child(status_row)

func set_active_turn(active: bool) -> void:
	active_pedestal.visible = active
	if active:
		if _active_tween:
			_active_tween.kill()
		_active_tween = create_tween().set_loops()
		_active_tween.tween_property(active_pedestal, "modulate:a", 1.0, 0.3)
		_active_tween.tween_property(active_pedestal, "modulate:a", 0.4, 0.3)
	else:
		if _active_tween:
			_active_tween.kill()
		active_pedestal.modulate.a = 1.0

func set_skill_ready(ready: bool) -> void:
	if _is_dead or not is_instance_valid(skill_ready_aura):
		return
	if ready:
		if not skill_ready_aura.visible or _skill_ready_tween == null:
			skill_ready_aura.visible = true
			if _skill_ready_tween:
				_skill_ready_tween.kill()
			_skill_ready_tween = create_tween().set_loops()
			_skill_ready_tween.tween_property(skill_ready_aura, "scale", Vector2(1.25, 1.25), 0.4).set_trans(Tween.TRANS_SINE)
			_skill_ready_tween.parallel().tween_property(skill_ready_aura, "modulate", Color(1.1, 1.05, 0.7, 1.0), 0.4)
			_skill_ready_tween.tween_property(skill_ready_aura, "scale", Vector2(1.10, 1.10), 0.4).set_trans(Tween.TRANS_SINE)
			_skill_ready_tween.parallel().tween_property(skill_ready_aura, "modulate", Color(1.0, 0.8, 0.4, 0.65), 0.4)
	else:
		skill_ready_aura.visible = false
		if _skill_ready_tween:
			_skill_ready_tween.kill()
			_skill_ready_tween = null

func update_bars(animate: bool = true) -> void:
	if _is_dead:
		return

	var current_hp = battle_hero.stats.hp_current
	var max_hp = battle_hero.stats.hp_max
	var hp_ratio = float(current_hp) / float(max_hp)

	hp_label.text = "%d/%d" % [current_hp, max_hp]

	# Dynamic HP Color
	var fill_style = hp_bar.get_theme_stylebox("fill") as StyleBoxFlat
	if fill_style:
		if hp_ratio > 0.5:
			fill_style.bg_color = Color(0.2, 0.88, 0.35)
		elif hp_ratio > 0.25:
			fill_style.bg_color = Color(0.95, 0.75, 0.15)
		else:
			fill_style.bg_color = Color(0.95, 0.22, 0.22)

	# Smooth HP transition with Trailing Ghost Bar
	if animate:
		if _hp_tween:
			_hp_tween.kill()
		_hp_tween = create_tween()
		_hp_tween.tween_property(hp_bar, "value", float(current_hp), 0.18).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)

		if _ghost_tween:
			_ghost_tween.kill()
		_ghost_tween = create_tween()
		_ghost_tween.tween_interval(0.2)
		_ghost_tween.tween_property(hp_ghost_bar, "value", float(current_hp), 0.45).set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)
	else:
		hp_bar.value = current_hp
		hp_ghost_bar.value = current_hp

	# Hiệu ứng chân khi tích đầy Nộ chiêu / Kỹ năng (100 Sĩ khí)
	var current_morale = battle_hero.stats.morale_current
	var is_max_rage = (current_morale >= 100)
	set_skill_ready(is_max_rage)

func update_status_icons() -> void:
	for child in status_row.get_children():
		child.queue_free()

	for st in battle_hero.status_effects:
		var t = st["type"]
		var s_name = ""
		match t:
			SkillData.EffectType.STUN: s_name = "stun"
			SkillData.EffectType.SILENCE: s_name = "silence"
			SkillData.EffectType.BUFF_ATK: s_name = "buff_atk"
			SkillData.EffectType.BUFF_DEF: s_name = "buff_def"
			SkillData.EffectType.BUFF_SPEED: s_name = "buff_spd"
			SkillData.EffectType.DEBUFF_ATK: s_name = "debuff_atk"
			SkillData.EffectType.DEBUFF_DEF: s_name = "debuff_def"

		if s_name != "":
			var tex_rect = TextureRect.new()
			tex_rect.texture = load("res://assets/sprites/ui/status_%s.png" % s_name)
			tex_rect.custom_minimum_size = Vector2(14, 14)
			tex_rect.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
			status_row.add_child(tex_rect)

## ──────────────────────────────────────────────────────────
## ANIMATION METHODS
## ──────────────────────────────────────────────────────────

func play_idle() -> void:
	if _is_dead:
		return
	if _idle_tween:
		_idle_tween.kill()

	_idle_tween = create_tween().set_loops()
	_idle_tween.tween_callback(func(): sprite.frame = 0)
	_idle_tween.tween_interval(0.4)
	_idle_tween.tween_callback(func(): sprite.frame = 1)
	_idle_tween.tween_interval(0.4)

func animate_attack(target_pos: Vector2, on_impact: Callable, on_complete: Callable, speed_mult: float = 1.0) -> void:
	if _idle_tween:
		_idle_tween.kill()

	var tw = create_tween()
	var attack_dest = target_pos + (Vector2(55, 0) if is_enemy else Vector2(-55, 0))
	var dur_dash = 0.16 / speed_mult
	var dur_hold = 0.12 / speed_mult
	var dur_back = 0.18 / speed_mult

	# 1. Dash to target
	tw.tween_property(self, "position", attack_dest, dur_dash).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	tw.tween_callback(func():
		sprite.frame = 2
		if SoundManager.instance:
			sound_attack()
		on_impact.call()
	)

	# 2. Hold attack pose
	tw.tween_interval(dur_hold)

	# 3. Return home
	tw.tween_callback(func(): sprite.frame = 0)
	tw.tween_property(self, "position", home_position, dur_back).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN)
	tw.tween_callback(func():
		play_idle()
		on_complete.call()
	)

func sound_attack() -> void:
	if SoundManager.instance:
		SoundManager.instance.play_slash()

func animate_ranged_attack(_target_pos: Vector2, on_impact: Callable, on_complete: Callable, speed_mult: float = 1.0) -> void:
	if _idle_tween:
		_idle_tween.kill()

	var tw = create_tween()
	var dur_cast = 0.2 / speed_mult
	var dur_hold = 0.15 / speed_mult

	# Step slightly forward
	var step_forward = home_position + (Vector2(-18, 0) if is_enemy else Vector2(18, 0))
	tw.tween_property(self, "position", step_forward, 0.1 / speed_mult)
	tw.tween_callback(func():
		sprite.frame = 2
		if SoundManager.instance:
			SoundManager.instance.play_magic()
		on_impact.call()
	)
	tw.tween_interval(dur_cast + dur_hold)
	tw.tween_property(self, "position", home_position, 0.1 / speed_mult)
	tw.tween_callback(func():
		sprite.frame = 0
		play_idle()
		on_complete.call()
	)

func animate_hurt(is_dodge: bool, _is_block: bool, speed_mult: float = 1.0) -> void:
	if _is_dead:
		return
	if _idle_tween:
		_idle_tween.kill()

	var tw = create_tween()
	var dur = 0.15 / speed_mult

	if is_dodge:
		# Lướt lùi né tránh
		var dodge_off = Vector2(28, 0) if is_enemy else Vector2(-28, 0)
		tw.tween_property(self, "position", home_position + dodge_off, dur * 0.5)
		tw.tween_property(self, "modulate:a", 0.5, dur * 0.5)
		tw.tween_property(self, "position", home_position, dur * 0.5)
		tw.parallel().tween_property(self, "modulate:a", 1.0, dur * 0.5)
		tw.tween_callback(play_idle)
		return

	# Trúng đòn: Chớp trắng & giật lùi
	sprite.frame = 3
	var knockback = Vector2(18, 0) if is_enemy else Vector2(-18, 0)
	tw.tween_property(self, "position", home_position + knockback, dur * 0.4)
	tw.parallel().tween_property(sprite, "modulate", Color(2.5, 2.5, 2.5), dur * 0.4)

	tw.tween_property(self, "position", home_position, dur * 0.6)
	tw.parallel().tween_property(sprite, "modulate", Color.WHITE, dur * 0.6)
	tw.tween_callback(func():
		sprite.frame = 0
		play_idle()
	)

func animate_death(speed_mult: float = 1.0) -> void:
	_is_dead = true
	set_skill_ready(false)
	set_active_turn(false)
	if _idle_tween:
		_idle_tween.kill()

	sprite.frame = 3
	var tw = create_tween()
	var dur = 0.45 / speed_mult
	tw.tween_property(self, "position:y", position.y + 12, dur)
	tw.parallel().tween_property(self, "modulate", Color(0.2, 0.2, 0.2, 0.0), dur)
	tw.tween_callback(func(): visible = false)

func show_floating_text(txt: String, color: Color, is_crit: bool = false) -> void:
	var ft = FloatingText.new()
	ft.text = txt
	ft.text_color = color
	ft.is_crit = is_crit
	ft.position = position + Vector2(0, -75)
	get_parent().add_child(ft)
