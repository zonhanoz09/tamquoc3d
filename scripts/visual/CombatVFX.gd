class_name CombatVFX
extends Node2D
## CombatVFX - Quản lý sinh hiệu ứng Pixel VFX chuyên biệt theo từng tướng và kỹ năng

## ──────────────────────────────────────────────────────────
## ĐÒN ĐÁNH THƯỜNG RIÊNG BIỆT CHO TỪNG TƯỚNG (15 TƯỚNG)
## ──────────────────────────────────────────────────────────

static func spawn_hero_attack_vfx(parent: Node, hero_id: String, target_pos: Vector2, attacker_pos: Vector2, is_enemy: bool, is_crit: bool = false) -> void:
	var hit_pos = target_pos + Vector2(0, -30)

	# Hiệu ứng nổ va chạm chung cho đòn bạo kích
	if is_crit:
		spawn_impact(parent, hit_pos, true)

	match hero_id:
		"trieu_van":
			# Triệu Vân: Bạch Long Thương - Đâm thương xanh lam bạc chớp nhoáng
			_spawn_linear_pierce(parent, hit_pos, "vfx_spear_blue", is_enemy, Vector2(1.8, 1.8), 0.22)
		"dien_vi":
			# Điển Vi: Song Kích Trọng Kích - Đôi kích chém chéo chữ X rực lửa
			_spawn_burst_vfx(parent, hit_pos, "vfx_cross_axes", Vector2(1.8, 1.8), 0.28, true)
		"truong_phi":
			# Trương Phi: Bát Xà Mâu Trảm - Quạt xà mâu cuồng bạo rực lửa
			_spawn_slash_vfx(parent, hit_pos, "vfx_serpent_spear", is_enemy, Vector2(1.8, 1.8), 0.25)
		"tao_nhan":
			# Tào Nhân: Kim Cương Kích - Chấn động khiên kim loại tóe hoa lửa
			_spawn_burst_vfx(parent, hit_pos, "vfx_shield_strike", Vector2(1.6, 1.6), 0.24, false)
		"hoang_cai":
			# Hoàng Cái: Thiết Tiết Kích - Đập roi sắt tóe sóng nước
			_spawn_burst_vfx(parent, hit_pos, "vfx_water_smash", Vector2(1.7, 1.7), 0.3, false)
		"quan_vu":
			# Quan Vũ: Thanh Long Yển Nguyệt Đao - Trảm bán nguyệt rồng xanh ngọc bích
			_spawn_slash_vfx(parent, hit_pos, "vfx_crescent_blade_green", is_enemy, Vector2(1.9, 1.9), 0.26)
		"lu_bo":
			# Lữ Bố: Phương Thiên Họa Kích - Vết chém huyết sắc ma thần cuồng bạo
			_spawn_slash_vfx(parent, hit_pos, "vfx_halberd_crimson", is_enemy, Vector2(2.0, 2.0), 0.28)
		"chu_du":
			# Chu Du: Hỏa Kiếm Vũ - Kiếm vũ bốc lửa xé gió
			_spawn_burst_vfx(parent, hit_pos, "vfx_fire_sword", Vector2(1.8, 1.8), 0.25, false)
		"hoang_trung":
			# Hoàng Trung: Tiễn Tiệt Không - Tên hoàng kim bay vun vút từ vị trí bắn
			_spawn_projectile(parent, attacker_pos + Vector2(0, -35), hit_pos, "vfx_arrow_gold", is_enemy, 0.2)
		"ma_sieu":
			# Mã Siêu: Tây Lương Mã Đao - Nhát chém kỵ đao cuồng phong bạc
			_spawn_slash_vfx(parent, hit_pos, "vfx_cavalry_blade", is_enemy, Vector2(1.8, 1.8), 0.22)
		"dieu_thuyen":
			# Điêu Thuyền: Lụa Hồng Mê Hoặc - Lụa hồng xoáy hoa hồng huyền ảo
			_spawn_burst_vfx(parent, hit_pos, "vfx_silk_ribbon", Vector2(1.8, 1.8), 0.32, true)
		"gia_cat_luong":
			# Gia Cát Lượng: Bát Trận Đồ - Trận đồ bát quái xoay tròn dưới chân mục tiêu
			_spawn_burst_vfx(parent, target_pos + Vector2(0, -10), "vfx_bagua_circle", Vector2(1.6, 1.6), 0.35, true)
		"tieu_kieu":
			# Tiểu Kiều: Khúc Nhạc Hồi Sinh - Nốt nhạc nước trong trẻo hồi phục/gây sát thương
			_spawn_burst_vfx(parent, hit_pos, "vfx_music_water", Vector2(1.6, 1.6), 0.32, false)
		"bang_thong":
			# Bàng Thống: Hỏa Long Chú - Phù chú vuốt phượng bốc cháy
			_spawn_burst_vfx(parent, hit_pos, "vfx_fire_talisman", Vector2(1.7, 1.7), 0.3, false)
		"sai_van_co":
			# Sái Văn Cơ: Hồ Gia Khúc - Sóng âm đàn tranh hoàng kim thanh thoát
			_spawn_burst_vfx(parent, hit_pos, "vfx_guqin_waves", Vector2(1.6, 1.6), 0.3, false)
		_:
			# Mặc định: Vết chém và tia lửa
			spawn_slash(parent, hit_pos, is_enemy)

## ──────────────────────────────────────────────────────────
## KỸ NĂNG RIÊNG BIỆT CHO TỪNG TƯỚNG (15 TƯỚNG)
## ──────────────────────────────────────────────────────────

static func spawn_hero_skill_vfx(parent: Node, hero_id: String, target_pos: Vector2, attacker_pos: Vector2, all_target_positions: Array, is_enemy: bool) -> void:
	var hit_pos = target_pos + Vector2(0, -30)

	match hero_id:
		"trieu_van":
			# Long Đảm Thần Uy: Rồng trắng phi thân chọc thủng hàng ngũ
			_spawn_dragon_rush(parent, attacker_pos + Vector2(0, -35), target_pos + Vector2(0, -35), "vfx_dragon_white", is_enemy)
		"dien_vi":
			# Hộ Chủ Quyết Tử: Cắm kích nổ tung dung nham gai đất
			_spawn_magma_quake(parent, target_pos + Vector2(0, -10), all_target_positions)
		"truong_phi":
			# Nộ Hống Bản Cầu: Sóng âm chấn thiên sư tử hống làm rung chuyển toàn hàng
			_spawn_sonic_wave_row(parent, target_pos + Vector2(0, -25), all_target_positions)
		"tao_nhan":
			# Trầm Lực Thiết Bích: Pháo đài khiên hoàng kim bất khả xâm phạm bao bọc đồng đội
			_spawn_fortress_barrier(parent, attacker_pos + Vector2(0, -30), all_target_positions)
		"hoang_cai":
			# Khổ Nhục Hỏa Thuyền: Chiến hạm bốc cháy nổ tung hàng địch
			_spawn_fire_ship_charge(parent, target_pos + Vector2(0, -20), is_enemy)
		"quan_vu":
			# Qua Ngũ Quan Trảm Lục Tướng: Thanh Long thần đao khổng lồ trảm từ thiên đình
			_spawn_divine_dragon_blade(parent, target_pos + Vector2(0, -20), all_target_positions, is_enemy)
		"lu_bo":
			# Vô Song Chiến Thần: Ma thần thiên thạch kích giáng thế nổ hắc viêm
			_spawn_demonic_meteor_strike(parent, target_pos + Vector2(0, -25), all_target_positions)
		"chu_du":
			# Đại Hỏa Xích Bích: Hỏa hải cuộn trào thiêu đốt toàn bộ chiến trường
			_spawn_red_cliff_inferno(parent, all_target_positions)
		"hoang_trung":
			# Bách Bộ Xuyên Dương: Thần tiễn chùm tia laser hoàng kim xuyên thấu
			_spawn_sniper_hyperbeam(parent, attacker_pos + Vector2(0, -35), target_pos + Vector2(0, -35), is_enemy)
		"ma_sieu":
			# Thần Uy Đột Kích: Thiết kỵ ảo ảnh dẫm đạp cuồng phong lốc xoáy
			_spawn_phantom_cavalry(parent, attacker_pos, target_pos, is_enemy)
		"dieu_thuyen":
			# Bế Nguyệt Tuyệt Sắc: Trăng khuyết huyền ảo giáng thế gieo rắc câm lặng
			_spawn_moonlight_descent(parent, target_pos + Vector2(0, -35), all_target_positions)
		"gia_cat_luong":
			# Lửa Xích Bích / Đông Phong Bát Trận: Pháp trận thiên lôi cuồng phong sấm sét
			_spawn_divine_storm_formation(parent, all_target_positions)
		"tieu_kieu":
			# Điệu Vũ Thiên Kiều: Đóa sen hồi sinh bừng nở tắm mát đồng đội
			_spawn_sacred_lotus_heal(parent, all_target_positions)
		"bang_thong":
			# Liên Hoàn Hỏa Kế: Dây xích phượng hoàng bốc lửa trói chặt và phát nổ
			_spawn_phoenix_chain_explosion(parent, all_target_positions)
		"sai_van_co":
			# Thập Bát Phách Linh: 18 khúc linh phách ngọc bích chữa lành
			_spawn_celestial_zither_rings(parent, all_target_positions)
		_:
			# Mặc định: Vết chém lớn bạo kích
			spawn_impact(parent, hit_pos, true)

## ──────────────────────────────────────────────────────────
## HÀM HỖ TRỢ SINH HIỆU ỨNG TÙY BIẾN (CANVAS MATERIAL ADDITIVE)
## ──────────────────────────────────────────────────────────

static func _create_vfx_sprite(tex_name: String) -> Sprite2D:
	var spr = Sprite2D.new()
	var tex = load("res://assets/sprites/vfx/%s.png" % tex_name)
	if tex:
		spr.texture = tex
	var mat = CanvasItemMaterial.new()
	mat.blend_mode = CanvasItemMaterial.BLEND_MODE_ADD
	spr.material = mat
	spr.z_index = 45
	return spr

static func _spawn_linear_pierce(parent: Node, pos: Vector2, tex: String, is_enemy: bool, scale_val: Vector2, dur: float) -> void:
	var spr = _create_vfx_sprite(tex)
	spr.position = pos
	spr.scale = Vector2(0.3, scale_val.y)
	spr.flip_h = is_enemy
	parent.add_child(spr)

	var tw = spr.create_tween()
	tw.tween_property(spr, "scale:x", scale_val.x, dur * 0.4).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	tw.parallel().tween_property(spr, "position:x", pos.x + (-20 if is_enemy else 20), dur * 0.4)
	tw.tween_property(spr, "modulate:a", 0.0, dur * 0.6)
	tw.tween_callback(spr.queue_free)

static func _spawn_burst_vfx(parent: Node, pos: Vector2, tex: String, scale_val: Vector2, dur: float, rotate_eff: bool = false) -> void:
	var spr = _create_vfx_sprite(tex)
	spr.position = pos
	spr.scale = scale_val * 0.6
	parent.add_child(spr)

	var tw = spr.create_tween()
	tw.tween_property(spr, "scale", scale_val * 1.25, dur * 0.5).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
	if rotate_eff:
		tw.parallel().tween_property(spr, "rotation", deg_to_rad(45.0), dur)
	tw.tween_property(spr, "scale", scale_val * 1.4, dur * 0.5)
	tw.parallel().tween_property(spr, "modulate:a", 0.0, dur * 0.5)
	tw.tween_callback(spr.queue_free)

static func _spawn_slash_vfx(parent: Node, pos: Vector2, tex: String, is_enemy: bool, scale_val: Vector2, dur: float) -> void:
	var spr = _create_vfx_sprite(tex)
	spr.position = pos
	spr.scale = scale_val * 0.7
	spr.flip_h = is_enemy
	parent.add_child(spr)

	var tw = spr.create_tween()
	tw.tween_property(spr, "scale", scale_val * 1.3, dur * 0.45).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	tw.tween_property(spr, "modulate:a", 0.0, dur * 0.55)
	tw.tween_callback(spr.queue_free)

static func _spawn_projectile(parent: Node, from_pos: Vector2, to_pos: Vector2, tex: String, is_enemy: bool, dur: float) -> void:
	var spr = _create_vfx_sprite(tex)
	spr.position = from_pos
	spr.scale = Vector2(1.5, 1.5)
	spr.flip_h = is_enemy
	parent.add_child(spr)

	var tw = spr.create_tween()
	tw.tween_property(spr, "position", to_pos, dur).set_trans(Tween.TRANS_LINEAR)
	tw.tween_callback(func():
		spawn_impact(parent, to_pos, false)
		spr.queue_free()
	)

## ──────────────────────────────────────────────────────────
## KỸ NĂNG ĐẶC BIỆT CHI TIẾT
## ──────────────────────────────────────────────────────────

static func _spawn_dragon_rush(parent: Node, from_pos: Vector2, to_pos: Vector2, tex: String, is_enemy: bool) -> void:
	var spr = _create_vfx_sprite(tex)
	spr.position = from_pos
	spr.scale = Vector2(1.8, 1.8)
	spr.flip_h = is_enemy
	parent.add_child(spr)

	var tw = spr.create_tween()
	tw.tween_property(spr, "position", to_pos, 0.35).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	tw.parallel().tween_property(spr, "scale", Vector2(2.5, 2.5), 0.35)
	tw.tween_property(spr, "modulate:a", 0.0, 0.25)
	tw.tween_callback(spr.queue_free)

static func _spawn_magma_quake(parent: Node, center_pos: Vector2, targets: Array) -> void:
	for t_pos in targets:
		var spr = _create_vfx_sprite("vfx_magma_eruption")
		spr.position = t_pos + Vector2(0, -20)
		spr.scale = Vector2(0.5, 0.5)
		parent.add_child(spr)

		var tw = spr.create_tween()
		tw.tween_property(spr, "scale", Vector2(2.0, 2.0), 0.3).set_trans(Tween.TRANS_BOUNCE).set_ease(Tween.EASE_OUT)
		tw.tween_property(spr, "modulate:a", 0.0, 0.4)
		tw.tween_callback(spr.queue_free)

static func _spawn_sonic_wave_row(parent: Node, center_pos: Vector2, targets: Array) -> void:
	var spr = _create_vfx_sprite("vfx_sonic_roar")
	spr.position = center_pos
	spr.scale = Vector2(1.0, 1.0)
	parent.add_child(spr)

	var tw = spr.create_tween()
	tw.tween_property(spr, "scale", Vector2(3.5, 3.5), 0.45).set_trans(Tween.TRANS_EXPO).set_ease(Tween.EASE_OUT)
	tw.parallel().tween_property(spr, "modulate:a", 0.0, 0.5)
	tw.tween_callback(spr.queue_free)

static func _spawn_fortress_barrier(parent: Node, center_pos: Vector2, allies: Array) -> void:
	for a_pos in allies:
		var spr = _create_vfx_sprite("vfx_fortress_shield")
		spr.position = a_pos + Vector2(0, -35)
		spr.scale = Vector2(0.5, 0.5)
		parent.add_child(spr)

		var tw = spr.create_tween()
		tw.tween_property(spr, "scale", Vector2(1.6, 1.6), 0.3).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
		tw.tween_interval(0.4)
		tw.tween_property(spr, "modulate:a", 0.0, 0.3)
		tw.tween_callback(spr.queue_free)

static func _spawn_fire_ship_charge(parent: Node, target_pos: Vector2, is_enemy: bool) -> void:
	var spr = _create_vfx_sprite("vfx_fire_ship")
	spr.position = target_pos + (Vector2(80, -20) if is_enemy else Vector2(-80, -20))
	spr.scale = Vector2(1.8, 1.8)
	spr.flip_h = is_enemy
	parent.add_child(spr)

	var tw = spr.create_tween()
	tw.tween_property(spr, "position", target_pos + Vector2(0, -20), 0.35).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN)
	tw.tween_callback(func():
		spawn_fire(parent, target_pos + Vector2(0, -20))
		spawn_impact(parent, target_pos + Vector2(0, -20), true)
	)
	tw.tween_property(spr, "scale", Vector2(2.5, 2.5), 0.25)
	tw.parallel().tween_property(spr, "modulate:a", 0.0, 0.25)
	tw.tween_callback(spr.queue_free)

static func _spawn_divine_dragon_blade(parent: Node, target_pos: Vector2, targets: Array, is_enemy: bool) -> void:
	var spr = _create_vfx_sprite("vfx_dragon_blade_ultimate")
	spr.position = target_pos + Vector2(0, -70)
	spr.scale = Vector2(1.2, 1.2)
	spr.flip_h = is_enemy
	parent.add_child(spr)

	var tw = spr.create_tween()
	tw.tween_property(spr, "position:y", target_pos.y - 15, 0.3).set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_IN)
	tw.parallel().tween_property(spr, "scale", Vector2(2.6, 2.6), 0.3)
	tw.tween_callback(func():
		for t in targets:
			spawn_impact(parent, t + Vector2(0, -25), true)
	)
	tw.tween_property(spr, "modulate:a", 0.0, 0.35)
	tw.tween_callback(spr.queue_free)

static func _spawn_demonic_meteor_strike(parent: Node, target_pos: Vector2, targets: Array) -> void:
	var spr = _create_vfx_sprite("vfx_demonic_meteor")
	spr.position = target_pos + Vector2(0, -120)
	spr.scale = Vector2(1.0, 1.0)
	parent.add_child(spr)

	var tw = spr.create_tween()
	tw.tween_property(spr, "position", target_pos + Vector2(0, -20), 0.35).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN)
	tw.tween_callback(func():
		for t in targets:
			spawn_fire(parent, t + Vector2(0, -20))
			spawn_impact(parent, t + Vector2(0, -20), true)
	)
	tw.tween_property(spr, "scale", Vector2(3.0, 3.0), 0.4).set_trans(Tween.TRANS_EXPO).set_ease(Tween.EASE_OUT)
	tw.parallel().tween_property(spr, "modulate:a", 0.0, 0.4)
	tw.tween_callback(spr.queue_free)

static func _spawn_red_cliff_inferno(parent: Node, targets: Array) -> void:
	for t in targets:
		var spr = _create_vfx_sprite("vfx_inferno_storm")
		spr.position = t + Vector2(0, -25)
		spr.scale = Vector2(0.8, 0.8)
		parent.add_child(spr)

		var tw = spr.create_tween()
		tw.tween_property(spr, "scale", Vector2(2.2, 2.2), 0.4).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
		tw.parallel().tween_property(spr, "position:y", t.y - 45, 0.5)
		tw.tween_property(spr, "modulate:a", 0.0, 0.3)
		tw.tween_callback(spr.queue_free)

static func _spawn_sniper_hyperbeam(parent: Node, from_pos: Vector2, to_pos: Vector2, is_enemy: bool) -> void:
	var spr = _create_vfx_sprite("vfx_sniper_beam")
	spr.position = (from_pos + to_pos) / 2.0
	spr.scale = Vector2(2.4, 2.0)
	spr.flip_h = is_enemy
	parent.add_child(spr)

	var tw = spr.create_tween()
	tw.tween_property(spr, "scale:y", 3.0, 0.25).set_trans(Tween.TRANS_ELASTIC).set_ease(Tween.EASE_OUT)
	tw.tween_property(spr, "modulate:a", 0.0, 0.3)
	tw.tween_callback(spr.queue_free)

static func _spawn_phantom_cavalry(parent: Node, from_pos: Vector2, to_pos: Vector2, is_enemy: bool) -> void:
	var spr = _create_vfx_sprite("vfx_cavalry_charge")
	spr.position = from_pos
	spr.scale = Vector2(1.8, 1.8)
	spr.flip_h = is_enemy
	parent.add_child(spr)

	var tw = spr.create_tween()
	tw.tween_property(spr, "position", to_pos, 0.35).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	tw.parallel().tween_property(spr, "scale", Vector2(2.4, 2.4), 0.35)
	tw.tween_property(spr, "modulate:a", 0.0, 0.25)
	tw.tween_callback(spr.queue_free)

static func _spawn_moonlight_descent(parent: Node, target_pos: Vector2, targets: Array) -> void:
	var spr = _create_vfx_sprite("vfx_crescent_moon")
	spr.position = target_pos + Vector2(0, -60)
	spr.scale = Vector2(0.8, 0.8)
	parent.add_child(spr)

	var tw = spr.create_tween()
	tw.tween_property(spr, "position:y", target_pos.y - 20, 0.35).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	tw.parallel().tween_property(spr, "scale", Vector2(2.0, 2.0), 0.35)
	tw.tween_interval(0.2)
	tw.tween_property(spr, "modulate:a", 0.0, 0.35)
	tw.tween_callback(spr.queue_free)

static func _spawn_divine_storm_formation(parent: Node, targets: Array) -> void:
	for t in targets:
		var spr = _create_vfx_sprite("vfx_divine_storm")
		spr.position = t + Vector2(0, -30)
		spr.scale = Vector2(0.6, 0.6)
		parent.add_child(spr)

		var tw = spr.create_tween()
		tw.tween_property(spr, "scale", Vector2(2.2, 2.2), 0.45).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
		tw.parallel().tween_property(spr, "rotation", deg_to_rad(90), 0.5)
		tw.tween_property(spr, "modulate:a", 0.0, 0.35)
		tw.tween_callback(spr.queue_free)

static func _spawn_sacred_lotus_heal(parent: Node, allies: Array) -> void:
	for a in allies:
		var spr = _create_vfx_sprite("vfx_lotus_healing")
		spr.position = a + Vector2(0, -25)
		spr.scale = Vector2(0.5, 0.5)
		parent.add_child(spr)

		var tw = spr.create_tween()
		tw.tween_property(spr, "scale", Vector2(1.8, 1.8), 0.4).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
		tw.parallel().tween_property(spr, "position:y", a.y - 45, 0.5)
		tw.tween_property(spr, "modulate:a", 0.0, 0.3)
		tw.tween_callback(spr.queue_free)

static func _spawn_phoenix_chain_explosion(parent: Node, targets: Array) -> void:
	for t in targets:
		var spr = _create_vfx_sprite("vfx_phoenix_chains")
		spr.position = t + Vector2(0, -25)
		spr.scale = Vector2(1.0, 1.0)
		parent.add_child(spr)

		var tw = spr.create_tween()
		tw.tween_property(spr, "scale", Vector2(2.0, 2.0), 0.3).set_trans(Tween.TRANS_ELASTIC).set_ease(Tween.EASE_OUT)
		tw.tween_callback(func():
			spawn_fire(parent, t + Vector2(0, -20))
		)
		tw.tween_property(spr, "modulate:a", 0.0, 0.3)
		tw.tween_callback(spr.queue_free)

static func _spawn_celestial_zither_rings(parent: Node, allies: Array) -> void:
	for a in allies:
		var spr = _create_vfx_sprite("vfx_celestial_zither")
		spr.position = a + Vector2(0, -25)
		spr.scale = Vector2(0.8, 0.8)
		parent.add_child(spr)

		var tw = spr.create_tween()
		tw.tween_property(spr, "scale", Vector2(2.2, 2.2), 0.45).set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_OUT)
		tw.tween_property(spr, "modulate:a", 0.0, 0.35)
		tw.tween_callback(spr.queue_free)

## ──────────────────────────────────────────────────────────
## HÀM TƯƠNG THÍCH CŨ
## ──────────────────────────────────────────────────────────

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
	tw.tween_property(spr, "scale", Vector2(2.4, 2.4), 0.22)
	tw.parallel().tween_property(spr, "modulate:a", 0.0, 0.25)
	tw.tween_callback(spr.queue_free)

static func spawn_impact(parent: Node, pos: Vector2, is_crit: bool = false) -> void:
	var spr = Sprite2D.new()
	var tex = load("res://assets/sprites/vfx/impact.png")
	if tex:
		spr.texture = tex
	spr.position = pos
	spr.scale = Vector2(1.6, 1.6) if not is_crit else Vector2(2.6, 2.6)
	spr.z_index = 42
	parent.add_child(spr)

	var tw = spr.create_tween()
	tw.tween_property(spr, "rotation", deg_to_rad(90.0 if not is_crit else 180.0), 0.3)
	tw.parallel().tween_property(spr, "scale", spr.scale * 1.3, 0.25)
	tw.parallel().tween_property(spr, "modulate:a", 0.0, 0.3)
	tw.tween_callback(spr.queue_free)

static func spawn_fire(parent: Node, pos: Vector2) -> void:
	var spr = Sprite2D.new()
	var tex = load("res://assets/sprites/vfx/fire.png")
	if tex:
		spr.texture = tex
	spr.position = pos
	spr.scale = Vector2(2.2, 2.2)
	spr.z_index = 41
	parent.add_child(spr)

	var tw = spr.create_tween()
	tw.tween_property(spr, "scale", Vector2(3.0, 3.0), 0.35)
	tw.parallel().tween_property(spr, "modulate:a", 0.0, 0.4)
	tw.tween_callback(spr.queue_free)

static func spawn_wind(parent: Node, pos: Vector2) -> void:
	var spr = Sprite2D.new()
	var tex = load("res://assets/sprites/vfx/wind.png")
	if tex:
		spr.texture = tex
	spr.position = pos
	spr.scale = Vector2(2.4, 2.4)
	spr.z_index = 41
	parent.add_child(spr)

	var tw = spr.create_tween()
	tw.tween_property(spr, "position:y", pos.y - 30, 0.35)
	tw.parallel().tween_property(spr, "rotation", deg_to_rad(120), 0.35)
	tw.parallel().tween_property(spr, "modulate:a", 0.0, 0.4)
	tw.tween_callback(spr.queue_free)

static func spawn_water(parent: Node, pos: Vector2) -> void:
	var spr = Sprite2D.new()
	var tex = load("res://assets/sprites/vfx/water.png")
	if tex:
		spr.texture = tex
	spr.position = pos
	spr.scale = Vector2(2.0, 2.0)
	spr.z_index = 41
	parent.add_child(spr)

	var tw = spr.create_tween()
	tw.tween_property(spr, "position:y", pos.y - 35, 0.45)
	tw.parallel().tween_property(spr, "scale", Vector2(2.6, 2.6), 0.45)
	tw.parallel().tween_property(spr, "modulate:a", 0.0, 0.45)
	tw.tween_callback(spr.queue_free)

static func spawn_rage_aura(parent: Node, pos: Vector2) -> Node:
	var spr = Sprite2D.new()
	var tex = load("res://assets/sprites/vfx/rage_aura.png")
	if tex:
		spr.texture = tex
	spr.position = pos + Vector2(0, 10)
	spr.scale = Vector2(1.3, 0.8)
	spr.z_index = 25
	parent.add_child(spr)

	var tw = spr.create_tween().set_loops(3)
	tw.tween_property(spr, "scale", Vector2(1.6, 1.0), 0.3)
	tw.tween_property(spr, "scale", Vector2(1.2, 0.7), 0.3)
	return spr
