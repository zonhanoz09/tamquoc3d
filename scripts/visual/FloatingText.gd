class_name FloatingText
extends Node2D
## FloatingText - Hiển thị số sát thương và chữ hiệu ứng nảy trên chiến trường

var text: String = ""
var text_color: Color = Color.WHITE
var is_crit: bool = false
var font_size: int = 16

func _ready() -> void:
	z_index = 50

	var label = Label.new()
	label.text = text
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER

	# Styling
	label.add_theme_color_override("font_color", text_color)
	label.add_theme_color_override("font_shadow_color", Color(0, 0, 0, 0.9))
	label.add_theme_constant_override("shadow_offset_x", 1)
	label.add_theme_constant_override("shadow_offset_y", 2)
	label.add_theme_constant_override("shadow_outline_size", 2)

	if is_crit:
		label.add_theme_font_size_override("font_size", 22)
		scale = Vector2(1.5, 1.5)
	else:
		label.add_theme_font_size_override("font_size", 16)

	label.position = Vector2(-70, -15)
	label.size = Vector2(140, 30)
	add_child(label)

	# Animation
	var tw = create_tween().set_parallel(true)
	var target_y = position.y - (45 if not is_crit else 60)
	var rand_x = randf_range(-15.0, 15.0)

	tw.tween_property(self, "position:y", target_y, 0.7).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	tw.tween_property(self, "position:x", position.x + rand_x, 0.7)

	if is_crit:
		# Punch scale
		var scale_tw = create_tween()
		scale_tw.tween_property(self, "scale", Vector2(1.8, 1.8), 0.15)
		scale_tw.tween_property(self, "scale", Vector2(1.1, 1.1), 0.2)

	# Fade out
	var fade_tw = create_tween()
	fade_tw.tween_interval(0.35)
	fade_tw.tween_property(self, "modulate:a", 0.0, 0.35)
	fade_tw.tween_callback(queue_free)
