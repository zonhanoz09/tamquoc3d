class_name CombatVFX
extends Node2D
## CombatVFX - Quản lý sinh hiệu ứng Pixel VFX trên chiến trường

static func spawn_slash(parent: Node, pos: Vector2, flip_h: bool = false) -> void:
	var spr = Sprite2D.new()
	var tex = load("res://assets/sprites/vfx/slash.png")
	if tex:
		spr.texture = tex
	spr.position = pos
	spr.scale = Vector2(1.8, 1.8)
	spr.flip_h = flip_h
	spr.z_index = 40
	parent.add_child(spr)

	var tw = spr.create_tween()
	tw.tween_property(spr, "scale", Vector2(2.4, 2.4), 0.18)
	tw.parallel().tween_property(spr, "modulate:a", 0.0, 0.22)
	tw.tween_callback(spr.queue_free)

static func spawn_impact(parent: Node, pos: Vector2, is_crit: bool = false) -> void:
	var spr = Sprite2D.new()
	var tex = load("res://assets/sprites/vfx/impact.png")
	if tex:
		spr.texture = tex
	spr.position = pos
	spr.scale = Vector2(1.5, 1.5) if not is_crit else Vector2(2.5, 2.5)
	spr.z_index = 42
	parent.add_child(spr)

	var tw = spr.create_tween()
	tw.tween_property(spr, "rotation", deg_to_rad(90.0 if not is_crit else 180.0), 0.25)
	tw.parallel().tween_property(spr, "scale", spr.scale * 1.3, 0.2)
	tw.parallel().tween_property(spr, "modulate:a", 0.0, 0.25)
	tw.tween_callback(spr.queue_free)

static func spawn_fire(parent: Node, pos: Vector2) -> void:
	var spr = Sprite2D.new()
	var tex = load("res://assets/sprites/vfx/fire.png")
	if tex:
		spr.texture = tex
	spr.position = pos
	spr.scale = Vector2(2.0, 2.0)
	spr.z_index = 41
	parent.add_child(spr)

	var tw = spr.create_tween()
	tw.tween_property(spr, "scale", Vector2(2.8, 2.8), 0.3)
	tw.parallel().tween_property(spr, "modulate:a", 0.0, 0.35)
	tw.tween_callback(spr.queue_free)

static func spawn_wind(parent: Node, pos: Vector2) -> void:
	var spr = Sprite2D.new()
	var tex = load("res://assets/sprites/vfx/wind.png")
	if tex:
		spr.texture = tex
	spr.position = pos
	spr.scale = Vector2(2.2, 2.2)
	spr.z_index = 41
	parent.add_child(spr)

	var tw = spr.create_tween()
	tw.tween_property(spr, "position:y", pos.y - 25, 0.3)
	tw.parallel().tween_property(spr, "rotation", deg_to_rad(120), 0.3)
	tw.parallel().tween_property(spr, "modulate:a", 0.0, 0.35)
	tw.tween_callback(spr.queue_free)

static func spawn_water(parent: Node, pos: Vector2) -> void:
	var spr = Sprite2D.new()
	var tex = load("res://assets/sprites/vfx/water.png")
	if tex:
		spr.texture = tex
	spr.position = pos
	spr.scale = Vector2(1.8, 1.8)
	spr.z_index = 41
	parent.add_child(spr)

	var tw = spr.create_tween()
	tw.tween_property(spr, "position:y", pos.y - 30, 0.4)
	tw.parallel().tween_property(spr, "scale", Vector2(2.4, 2.4), 0.4)
	tw.parallel().tween_property(spr, "modulate:a", 0.0, 0.4)
	tw.tween_callback(spr.queue_free)

static func spawn_rage_aura(parent: Node, pos: Vector2) -> Node:
	var spr = Sprite2D.new()
	var tex = load("res://assets/sprites/vfx/rage_aura.png")
	if tex:
		spr.texture = tex
	spr.position = pos + Vector2(0, 10)
	spr.scale = Vector2(1.2, 0.7)
	spr.z_index = 25
	parent.add_child(spr)

	var tw = spr.create_tween().set_loops(3)
	tw.tween_property(spr, "scale", Vector2(1.4, 0.9), 0.25)
	tw.tween_property(spr, "scale", Vector2(1.1, 0.65), 0.25)
	return spr
