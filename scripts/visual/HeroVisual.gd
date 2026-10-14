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
var hp_bg: ColorRect
var hp_ghost_bar: ColorRect
var hp_bar: ColorRect
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

	# 5. Overhead UI (Thanh máu dài thanh mảnh 4px & tên tướng tinh tế)
	_build_overhead_ui()

func _build_overhead_ui() -> void:
	var overhead = Node2D.new()
	overhead.position = Vector2(0, -112)
	overhead.z_index = 35
	add_child(overhead)

	# ── LINE 1: HÀNG TÊN TƯỚNG (Gọn gàng ngay trên thanh máu) ──
	# Element Icon (Viên kim cương nguyên tố nhỏ gọn)
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
	elem_icon.scale = Vector2(0.65, 0.65)
	elem_icon.position = Vector2(-60, -9)
	overhead.add_child(elem_icon)

	# Name Label (Chính giữa trên thanh máu)
	name_label = Label.new()
	name_label.text = battle_hero.data.hero_name
	name_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	name_label.position = Vector2(-54, -18)
	name_label.size = Vector2(108, 16)
	name_label.add_theme_font_size_override("font_size", 11)
	name_label.add_theme_color_override("font_shadow_color", Color.BLACK)
	name_label.add_theme_constant_override("shadow_offset_x", 1)
	name_label.add_theme_constant_override("shadow_offset_y", 1)
	name_label.add_theme_constant_override("shadow_outline_size", 1)

	var rarity_color = Color(0.85, 0.85, 0.9)
	match battle_hero.data.rarity:
		HeroData.Rarity.UR:  rarity_color = Color(1.0, 0.35, 0.35)
		HeroData.Rarity.SSR: rarity_color = Color(1.0, 0.88, 0.25)
		HeroData.Rarity.SR:  rarity_color = Color(0.75, 0.5, 0.95)
	name_label.add_theme_color_override("font_color", rarity_color)
	overhead.add_child(name_label)

	# ── LINE 2: THANH MÁU DÀI THANH MẢNH CHUẨN 4PX (Dài 108px, cao 4px) ──
	# Viền tối bên ngoài thanh máu 4px
	var hp_border = ColorRect.new()
	hp_border.position = Vector2(-55, -1)
	hp_border.size = Vector2(110, 6)
	hp_border.color = Color(0.02, 0.02, 0.04, 0.95)
	overhead.add_child(hp_border)

	# Nền lòng trong thanh máu 4px
	hp_bg = ColorRect.new()
	hp_bg.position = Vector2(-54, 0)
	hp_bg.size = Vector2(108, 4)
	hp_bg.color = Color(0.1, 0.08, 0.12, 0.9)
	overhead.add_child(hp_bg)

	# Vạch tụt máu trễ 4px (Màu vàng sáng)
	hp_ghost_bar = ColorRect.new()
	hp_ghost_bar.position = Vector2(-54, 0)
	hp_ghost_bar.size = Vector2(108, 4)
	hp_ghost_bar.color = Color(0.98, 0.88, 0.38, 0.9)
	overhead.add_child(hp_ghost_bar)

	# Thanh máu chính 4px (Màu xanh ngọc tươi sáng)
	hp_bar = ColorRect.new()
	hp_bar.position = Vector2(-54, 0)
	hp_bar.size = Vector2(108, 4)
	hp_bar.color = Color(0.22, 0.88, 0.42)
	overhead.add_child(hp_bar)

	# ── LINE 0: STATUS ICONS ROW ──
	status_row = HBoxContainer.new()
	status_row.position = Vector2(-54, -32)
	status_row.size = Vector2(108, 12)
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
	var hp_ratio = clampf(float(current_hp) / float(max_hp), 0.0, 1.0)
	var target_w = 108.0 * hp_ratio

	# Dynamic HP Color
	if hp_ratio > 0.5:
		hp_bar.color = Color(0.22, 0.88, 0.42) # Xanh ngọc tươi sáng
	elif hp_ratio > 0.25:
		hp_bar.color = Color(0.96, 0.76, 0.16) # Vàng cam
	else:
		hp_bar.color = Color(0.96, 0.24, 0.24) # Đỏ thẫm nguy cấp

	# Smooth HP transition with Trailing Ghost Bar (Chuẩn 4px)
	if animate:
		if _hp_tween:
			_hp_tween.kill()
		_hp_tween = create_tween()
		_hp_tween.tween_property(hp_bar, "size:x", target_w, 0.18).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)

		if _ghost_tween:
			_ghost_tween.kill()
		_ghost_tween = create_tween()
		_ghost_tween.tween_interval(0.2)
		_ghost_tween.tween_property(hp_ghost_bar, "size:x", target_w, 0.45).set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)
	else:
		hp_bar.size.x = target_w
		hp_ghost_bar.size.x = target_w

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
