class_name SkillCutIn
extends CanvasLayer
## SkillCutIn - Banner tuyệt kỹ nộ chiêu điện ảnh phong cách Tam Quốc

signal finished

func show_cut_in(hero_data: HeroData, skill_name: String, speed_mult: float = 1.0) -> void:
	layer = 100

	# 1. Dark overlay
	var overlay = ColorRect.new()
	overlay.size = Vector2(1280, 720)
	overlay.color = Color(0, 0, 0, 0.65)
	add_child(overlay)

	# 2. Horizontal Banner
	var banner = ColorRect.new()
	banner.size = Vector2(1280, 140)
	banner.position = Vector2(0, 290)

	var elem_color = Color(0.8, 0.2, 0.2, 0.85)
	match hero_data.element:
		HeroData.Element.WATER: elem_color = Color(0.15, 0.45, 0.85, 0.85)
		HeroData.Element.WIND:  elem_color = Color(0.2, 0.7, 0.4, 0.85)
		HeroData.Element.LIGHT: elem_color = Color(0.9, 0.75, 0.2, 0.85)
		HeroData.Element.DARK:  elem_color = Color(0.5, 0.2, 0.7, 0.85)
		_: elem_color = Color(0.7, 0.3, 0.15, 0.85)

	banner.color = elem_color
	add_child(banner)

	# Gold border lines
	var top_line = ColorRect.new()
	top_line.size = Vector2(1280, 3)
	top_line.position = Vector2(0, 290)
	top_line.color = Color(1.0, 0.85, 0.3)
	add_child(top_line)

	var bot_line = ColorRect.new()
	bot_line.size = Vector2(1280, 3)
	bot_line.position = Vector2(0, 427)
	bot_line.color = Color(1.0, 0.85, 0.3)
	add_child(bot_line)

	# 3. Hero Portrait (Left)
	var portrait = TextureRect.new()
	var p_path = "res://assets/sprites/portraits/%s.png" % hero_data.hero_id
	var tex = load(p_path)
	if tex:
		portrait.texture = tex
	portrait.size = Vector2(110, 110)
	portrait.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	portrait.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	portrait.position = Vector2(160, 305)
	add_child(portrait)

	# 4. Text Container (Hero Name + Skill Name)
	var hero_lbl = Label.new()
	hero_lbl.text = "⚡ TUYỆT KỸ: %s [%s]" % [hero_data.hero_name.to_upper(), hero_data.title]
	hero_lbl.position = Vector2(300, 310)
	hero_lbl.size = Vector2(800, 30)
	hero_lbl.add_theme_color_override("font_color", Color(1.0, 0.9, 0.5))
	hero_lbl.add_theme_font_size_override("font_size", 18)
	add_child(hero_lbl)

	var skill_lbl = Label.new()
	skill_lbl.text = skill_name.to_upper()
	skill_lbl.position = Vector2(300, 340)
	skill_lbl.size = Vector2(800, 60)
	skill_lbl.add_theme_color_override("font_color", Color.WHITE)
	skill_lbl.add_theme_color_override("font_shadow_color", Color(0, 0, 0, 0.9))
	skill_lbl.add_theme_constant_override("shadow_offset_x", 2)
	skill_lbl.add_theme_constant_override("shadow_offset_y", 3)
	skill_lbl.add_theme_font_size_override("font_size", 38)
	add_child(skill_lbl)

	# Sound
	if SoundManager.instance:
		SoundManager.instance.play_rage()

	# Animation timing
	var dur = 0.55 / speed_mult
	var tw = create_tween()
	# Slide in from left
	banner.position.x = -1280
	top_line.position.x = -1280
	bot_line.position.x = 1280
	portrait.position.x = -200
	skill_lbl.scale = Vector2(1.5, 1.5)

	tw.set_parallel(true)
	tw.tween_property(banner, "position:x", 0.0, 0.15 / speed_mult).set_trans(Tween.TRANS_QUAD)
	tw.tween_property(top_line, "position:x", 0.0, 0.15 / speed_mult)
	tw.tween_property(bot_line, "position:x", 0.0, 0.15 / speed_mult)
	tw.tween_property(portrait, "position:x", 160.0, 0.15 / speed_mult)
	tw.tween_property(skill_lbl, "scale", Vector2(1.0, 1.0), 0.15 / speed_mult)

	# Hold then fade out
	tw.chain().tween_interval(dur * 0.6)
	tw.chain().tween_property(self, "offset:y", -720.0, 0.18 / speed_mult).set_trans(Tween.TRANS_QUAD)
	tw.tween_callback(func():
		finished.emit()
		queue_free()
	)
