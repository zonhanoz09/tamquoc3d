class_name HeroVisual
extends Node2D
## HeroVisual - Thực thể đồ họa & hoạt họa HD-2D của vị tướng trong trận đấu

var battle_hero: BattleHero
var is_enemy: bool = false
var home_position: Vector2 = Vector2.ZERO

# Child Nodes
var sprite: Sprite2D
var shadow: Sprite2D
var active_ring: Sprite2D

# Smooth Overhead Bars
var hp_ghost_bar: ProgressBar
var hp_bar: ProgressBar
var hp_label: Label
var morale_bar: ProgressBar
var morale_glow: ColorRect
var rage_badge: Label
var name_label: Label
var elem_icon: Sprite2D
var status_row: HBoxContainer

# Animation State
var _idle_tween: Tween
var _hp_tween: Tween
var _ghost_tween: Tween
var _morale_tween: Tween
var _is_dead: bool = false

func setup(bh: BattleHero, enemy: bool) -> void:
	battle_hero = bh
	is_enemy = enemy
	battle_hero.node = self
	_build_visuals()
	update_bars(false)
	play_idle()

func _build_visuals() -> void:
	# 1. Shadow dưới chân
	shadow = Sprite2D.new()
	var shadow_img = Image.create(48, 18, false, Image.FORMAT_RGBA8)
	for y in range(18):
		for x in range(48):
			var dx = (x - 24) / 24.0
			var dy = (y - 9) / 9.0
			if dx * dx + dy * dy <= 1.0:
				shadow_img.set_pixel(x, y, Color(0, 0, 0, 0.45))
	shadow.texture = ImageTexture.create_from_image(shadow_img)
	shadow.position = Vector2(0, 28)
	add_child(shadow)

	# 2. Active Turn Selection Ring (Vòng sáng dưới chân khi tới lượt)
	active_ring = Sprite2D.new()
	var ring_img = Image.create(54, 22, false, Image.FORMAT_RGBA8)
	for y in range(22):
		for x in range(54):
			var dx = (x - 27) / 27.0
			var dy = (y - 11) / 11.0
			var d = dx * dx + dy * dy
			if d <= 1.0 and d >= 0.6:
				ring_img.set_pixel(x, y, Color(1.0, 0.88, 0.25, 0.95))
	active_ring.texture = ImageTexture.create_from_image(ring_img)
	active_ring.position = Vector2(0, 28)
	active_ring.visible = false
	add_child(active_ring)

	# 3. Hero Sprite (64x64 HD Spritesheet: 4 frames)
	sprite = Sprite2D.new()
	var spr_path = "res://assets/sprites/heroes/%s.png" % battle_hero.data.hero_id
	var tex = load(spr_path)
	if tex:
		sprite.texture = tex
		sprite.hframes = 4
		sprite.frame = 0
	sprite.scale = Vector2(2.0, 2.0)
	sprite.flip_h = is_enemy
	add_child(sprite)

	# 4. Overhead UI
	_build_overhead_ui()

func _build_overhead_ui() -> void:
	var overhead = Node2D.new()
	overhead.position = Vector2(0, -68)
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
	elem_icon.position = Vector2(-54, -2)
	overhead.add_child(elem_icon)

	# Name Label
	name_label = Label.new()
	name_label.text = battle_hero.data.hero_name
	name_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	name_label.position = Vector2(-50, -20)
	name_label.size = Vector2(100, 18)
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

	# ── HP GHOST BAR (Vạch tụt máu trễ phong cách game đối kháng) ──
	hp_ghost_bar = ProgressBar.new()
	hp_ghost_bar.position = Vector2(-42, 0)
	hp_ghost_bar.size = Vector2(84, 8)
	hp_ghost_bar.show_percentage = false
	hp_ghost_bar.max_value = battle_hero.stats.hp_max
	hp_ghost_bar.value = battle_hero.stats.hp_current

	var sb_ghost_bg = StyleBoxFlat.new()
	sb_ghost_bg.bg_color = Color(0.12, 0.1, 0.15, 0.9)
	sb_ghost_bg.set_border_width_all(1)
	sb_ghost_bg.border_color = Color(0.05, 0.05, 0.08)
	hp_ghost_bar.add_theme_stylebox_override("background", sb_ghost_bg)

	var sb_ghost_fill = StyleBoxFlat.new()
	sb_ghost_fill.bg_color = Color(0.95, 0.85, 0.3, 0.85) # Vàng cam mờ
	hp_ghost_bar.add_theme_stylebox_override("fill", sb_ghost_fill)
	overhead.add_child(hp_ghost_bar)

	# ── HP MAIN BAR ──
	hp_bar = ProgressBar.new()
	hp_bar.position = Vector2(-42, 0)
	hp_bar.size = Vector2(84, 8)
	hp_bar.show_percentage = false
	hp_bar.max_value = battle_hero.stats.hp_max
	hp_bar.value = battle_hero.stats.hp_current

	var sb_empty = StyleBoxEmpty.new()
	hp_bar.add_theme_stylebox_override("background", sb_empty)

	var sb_fill = StyleBoxFlat.new()
	sb_fill.bg_color = Color(0.2, 0.88, 0.35)
	sb_fill.set_border_width_all(1)
	sb_fill.border_color = Color(0.1, 0.45, 0.2)
	hp_bar.add_theme_stylebox_override("fill", sb_fill)
	overhead.add_child(hp_bar)

	# HP Numeric Label ("2,450 / 2,800")
	hp_label = Label.new()
	hp_label.text = "%d/%d" % [battle_hero.stats.hp_current, battle_hero.stats.hp_max]
	hp_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	hp_label.position = Vector2(-42, -1)
	hp_label.size = Vector2(84, 10)
	hp_label.add_theme_font_size_override("font_size", 9)
	hp_label.add_theme_color_override("font_color", Color(1.0, 1.0, 1.0, 0.95))
	hp_label.add_theme_color_override("font_shadow_color", Color(0, 0, 0, 0.9))
	hp_label.add_theme_constant_override("shadow_outline_size", 1)
	overhead.add_child(hp_label)

	# ── MORALE / ENERGY BAR (Thanh Sĩ Khí) ──
	morale_bar = ProgressBar.new()
	morale_bar.position = Vector2(-42, 10)
	morale_bar.size = Vector2(84, 5)
	morale_bar.show_percentage = false
	morale_bar.max_value = 100
	morale_bar.value = battle_hero.stats.morale_current

	var sb_m_bg = StyleBoxFlat.new()
	sb_m_bg.bg_color = Color(0.08, 0.08, 0.12, 0.9)
	sb_m_bg.set_border_width_all(1)
	sb_m_bg.border_color = Color(0.04, 0.04, 0.06)
	morale_bar.add_theme_stylebox_override("background", sb_m_bg)

	var sb_m_fill = StyleBoxFlat.new()
	sb_m_fill.bg_color = Color(1.0, 0.65, 0.1) # Cam vàng năng lượng
	morale_bar.add_theme_stylebox_override("fill", sb_m_fill)
	overhead.add_child(morale_bar)

	# Morale Glow Border (Phát sáng khi đầy 100 Sĩ khí)
	morale_glow = ColorRect.new()
	morale_glow.position = Vector2(-44, 9)
	morale_glow.size = Vector2(88, 7)
	morale_glow.color = Color(1.0, 0.85, 0.2, 0.0)
	overhead.add_child(morale_glow)

	rage_badge = Label.new()
	rage_badge.text = "⚡ RAGE READY!"
	rage_badge.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	rage_badge.position = Vector2(-44, 16)
	rage_badge.size = Vector2(88, 12)
	rage_badge.add_theme_font_size_override("font_size", 9)
	rage_badge.add_theme_color_override("font_color", Color(1.0, 0.9, 0.3))
	rage_badge.add_theme_color_override("font_shadow_color", Color.BLACK)
	rage_badge.visible = false
	overhead.add_child(rage_badge)

	# Status Icons Row
	status_row = HBoxContainer.new()
	status_row.position = Vector2(-42, -34)
	status_row.size = Vector2(84, 14)
	status_row.alignment = BoxContainer.ALIGNMENT_CENTER
	overhead.add_child(status_row)

func set_active_turn(active: bool) -> void:
	active_ring.visible = active
	if active:
		var tw = active_ring.create_tween().set_loops()
		tw.tween_property(active_ring, "scale", Vector2(1.15, 1.15), 0.3)
		tw.tween_property(active_ring, "scale", Vector2(0.95, 0.95), 0.3)
	else:
		active_ring.scale = Vector2.ONE

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
			fill_style.bg_color = Color(0.2, 0.88, 0.35) # Xanh lục khỏe mạnh
		elif hp_ratio > 0.25:
			fill_style.bg_color = Color(0.95, 0.75, 0.15) # Vàng cảnh báo
		else:
			fill_style.bg_color = Color(0.95, 0.22, 0.22) # Đỏ nguy cấp

	# Smooth HP transition with Trailing Ghost Bar
	if animate:
		if _hp_tween:
			_hp_tween.kill()
		_hp_tween = create_tween()
		_hp_tween.tween_property(hp_bar, "value", float(current_hp), 0.18).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)

		if _ghost_tween:
			_ghost_tween.kill()
		_ghost_tween = create_tween()
		_ghost_tween.tween_interval(0.2) # Trễ 0.2s rồi tụt dần
		_ghost_tween.tween_property(hp_ghost_bar, "value", float(current_hp), 0.45).set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)
	else:
		hp_bar.value = current_hp
		hp_ghost_bar.value = current_hp

	# Smooth Morale transition
	var current_morale = battle_hero.stats.morale_current
	if animate:
		if _morale_tween:
			_morale_tween.kill()
		_morale_tween = create_tween()
		_morale_tween.tween_property(morale_bar, "value", float(current_morale), 0.25).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	else:
		morale_bar.value = current_morale

	# Morale Max Full Glow (Sĩ khí đạt 100)
	var is_max_rage = (current_morale >= 100)
	rage_badge.visible = is_max_rage
	var m_fill = morale_bar.get_theme_stylebox("fill") as StyleBoxFlat
	if m_fill:
		if is_max_rage:
			m_fill.bg_color = Color(1.0, 0.95, 0.3)
			morale_glow.color = Color(1.0, 0.85, 0.2, 0.45)
		else:
			m_fill.bg_color = Color(1.0, 0.65, 0.1)
			morale_glow.color = Color(1.0, 0.85, 0.2, 0.0)

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
	var attack_dest = target_pos + (Vector2(45, 0) if is_enemy else Vector2(-45, 0))
	var dur_dash = 0.16 / speed_mult
	var dur_hold = 0.12 / speed_mult
	var dur_back = 0.18 / speed_mult

	# 1. Dash to target
	tw.tween_property(self, "position", attack_dest, dur_dash).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	tw.tween_callback(func():
		sprite.frame = 2 # Attack pose
		if SoundManager.instance:
			SoundManager.instance.play_slash()
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

func animate_ranged_attack(_target_pos: Vector2, on_impact: Callable, on_complete: Callable, speed_mult: float = 1.0) -> void:
	if _idle_tween:
		_idle_tween.kill()

	var tw = create_tween()
	var dur_cast = 0.2 / speed_mult
	var dur_hold = 0.15 / speed_mult

	# Step slightly forward
	var step_forward = home_position + (Vector2(-15, 0) if is_enemy else Vector2(15, 0))
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
		var dodge_off = Vector2(25, 0) if is_enemy else Vector2(-25, 0)
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
	if _idle_tween:
		_idle_tween.kill()

	sprite.frame = 3
	var tw = create_tween()
	var dur = 0.45 / speed_mult
	# Chìm dần và tan biến
	tw.tween_property(self, "position:y", position.y + 12, dur)
	tw.parallel().tween_property(self, "modulate", Color(0.2, 0.2, 0.2, 0.0), dur)
	tw.tween_callback(func(): visible = false)

func show_floating_text(txt: String, color: Color, is_crit: bool = false) -> void:
	var ft = FloatingText.new()
	ft.text = txt
	ft.text_color = color
	ft.is_crit = is_crit
	ft.position = position + Vector2(0, -45)
	get_parent().add_child(ft)
