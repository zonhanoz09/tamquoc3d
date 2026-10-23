#!/usr/bin/env python3
"""
tools/generate_stage_data.py
Generates .tres resource files for Chapter 1 stages and Chapter 1 & 2 definitions.
"""

import os

STAGES_DIR = "data/database/stages/ch01"
CHAPTERS_DIR = "data/database/chapters"
os.makedirs(STAGES_DIR, exist_ok=True)
os.makedirs(CHAPTERS_DIR, exist_ok=True)

stages_info = [
    {
        "filename": "stage_01_01.tres",
        "id": 1,
        "key": "1-1",
        "name": "Tuần Tra Biên Giới",
        "desc": "Biên giới Đại Hán xuất hiện toán thổ phỉ áo vàng. Hãy phái binh trinh sát dẹp loạn.",
        "type": 0, # Normal
        "power": 1500,
        "stamina": 3,
        "enemies": [
            ("tao_nhan", 0, 0, 5),
            ("tao_nhan", 0, 2, 5),
            ("sai_van_co", 2, 1, 5)
        ],
        "first_gold": 500, "first_dia": 50, "first_exp": 200,
        "norm_gold": 200, "norm_exp": 100,
        "star2": 3, "star3": 5
    },
    {
        "filename": "stage_01_02.tres",
        "id": 2,
        "key": "1-2",
        "name": "Phục Kích Rừng Trúc",
        "desc": "Địch quân lợi dụng địa hình rừng trúc hiểm trở để mai phục quân ta.",
        "type": 0,
        "power": 2200,
        "stamina": 3,
        "enemies": [
            ("dien_vi", 0, 0, 7),
            ("hoang_cai", 0, 2, 7),
            ("hoang_trung", 2, 0, 7),
            ("tieu_kieu", 2, 2, 7)
        ],
        "first_gold": 600, "first_dia": 60, "first_exp": 250,
        "norm_gold": 250, "norm_exp": 120,
        "star2": 3, "star3": 6
    },
    {
        "filename": "stage_01_03.tres",
        "id": 3,
        "key": "1-3",
        "name": "Qua Ải Tiểu Bái",
        "desc": "Cửa ải kiên cố ngăn lối hành quân. Kho lương dồi dào chờ đoạt lấy.",
        "type": 2, # Resource
        "power": 3000,
        "stamina": 4,
        "enemies": [
            ("tao_nhan", 0, 0, 10),
            ("hoang_cai", 0, 2, 10),
            ("dien_vi", 1, 1, 10),
            ("bang_thong", 2, 1, 10)
        ],
        "first_gold": 800, "first_dia": 80, "first_exp": 350,
        "norm_gold": 300, "norm_exp": 150,
        "star2": 3, "star3": 7
    },
    {
        "filename": "stage_01_04.tres",
        "id": 4,
        "key": "1-4",
        "name": "Tranh Hùng Hạ Binh",
        "desc": "Quân cơ mưu lược đối đầu trực diện giữa đồng bằng trống trải.",
        "type": 0,
        "power": 4200,
        "stamina": 4,
        "enemies": [
            ("dien_vi", 0, 1, 12),
            ("trieu_van", 1, 0, 12),
            ("ma_sieu", 1, 2, 12),
            ("chu_du", 2, 1, 12)
        ],
        "first_gold": 1000, "first_dia": 100, "first_exp": 450,
        "norm_gold": 350, "norm_exp": 180,
        "star2": 4, "star3": 8
    },
    {
        "filename": "stage_01_05.tres",
        "id": 5,
        "key": "1-5",
        "name": "Cổng Thành Lạc Dương",
        "desc": "Hộ vệ quân hoàng thành canh giữ nghiêm ngặt. Trận chiến quyết định tiền tiêu.",
        "type": 3, # Elite
        "power": 5800,
        "stamina": 5,
        "enemies": [
            ("tao_nhan", 0, 0, 15),
            ("dien_vi", 0, 2, 15),
            ("quan_vu", 1, 1, 15),
            ("gia_cat_luong", 2, 0, 15),
            ("tieu_kieu", 2, 2, 15)
        ],
        "first_gold": 1500, "first_dia": 150, "first_exp": 600,
        "norm_gold": 400, "norm_exp": 200,
        "star2": 4, "star3": 9
    },
    {
        "filename": "stage_01_boss.tres",
        "id": 6,
        "key": "1-6",
        "name": "Đổng Trác — Bạo Quân",
        "desc": "Tướng soái bạo ngược Đổng Trác thống lĩnh đại quân Tây Lương cùng Lữ Bố nghênh chiến!",
        "type": 1, # Boss
        "power": 8500,
        "stamina": 6,
        "enemies": [
            ("dien_vi", 0, 0, 18),
            ("tao_nhan", 0, 2, 18),
            ("lu_bo", 1, 1, 20),
            ("dieu_thuyen", 2, 0, 18),
            ("chu_du", 2, 2, 18)
        ],
        "first_gold": 3000, "first_dia": 300, "first_exp": 1200,
        "norm_gold": 800, "norm_exp": 500,
        "star2": 4, "star3": 10
    }
]

def generate_stage_tres(stage):
    sub_resources = []
    sub_refs = []
    
    for idx, (hid, row, col, lvl) in enumerate(stage["enemies"]):
        sub_id = f"Resource_{idx + 1:05d}"
        sub_refs.append(f'SubResource("{sub_id}")')
        sub_resources.append(f"""[sub_resource type="Resource" id="{sub_id}"]
script = ExtResource("1_enemy")
HeroId = "{hid}"
Row = {row}
Col = {col}
OverrideLevel = {lvl}
""")

    content = f"""[gd_resource type="Resource" script_class="StageData" load_steps={len(sub_resources) + 3} format=3]

[ext_resource type="Script" path="res://data/resources/StageData.cs" id="1_stage"]
[ext_resource type="Script" path="res://data/resources/EnemyEntry.cs" id="1_enemy"]

{"".join(sub_resources)}
[resource]
script = ExtResource("1_stage")
ChapterId = {stage.get("chapter", 1)}
StageId = {stage["id"]}
StageKey = "{stage["key"]}"
StageName = "{stage["name"]}"
Description = "{stage["desc"]}"
Type = {stage["type"]}
RecommendedPower = {stage["power"]}
StaminaCost = {stage["stamina"]}
EnemyFormation = [{", ".join(sub_refs)}]
FirstClearGold = {stage["first_gold"]}
FirstClearDiamond = {stage["first_dia"]}
FirstClearExp = {stage["first_exp"]}
FirstClearItems = []
NormalGold = {stage["norm_gold"]}
NormalExp = {stage["norm_exp"]}
Star2Condition = {stage["star2"]}
Star3Condition = {stage["star3"]}
"""
    out_dir = stage.get("dir", STAGES_DIR)
    os.makedirs(out_dir, exist_ok=True)
    path = os.path.join(out_dir, stage["filename"])
    with open(path, "w", encoding="utf-8") as f:
        f.write(content)
    print(f"  -> Generated {path}")

def generate_chapters():
    # Chapter 1
    ch1_content = """[gd_resource type="Resource" script_class="ChapterData" load_steps=8 format=3]

[ext_resource type="Script" path="res://data/resources/ChapterData.cs" id="1_ch"]
[ext_resource type="Resource" path="res://data/database/stages/ch01/stage_01_01.tres" id="2_s1"]
[ext_resource type="Resource" path="res://data/database/stages/ch01/stage_01_02.tres" id="3_s2"]
[ext_resource type="Resource" path="res://data/database/stages/ch01/stage_01_03.tres" id="4_s3"]
[ext_resource type="Resource" path="res://data/database/stages/ch01/stage_01_04.tres" id="5_s4"]
[ext_resource type="Resource" path="res://data/database/stages/ch01/stage_01_05.tres" id="6_s5"]
[ext_resource type="Resource" path="res://data/database/stages/ch01/stage_01_boss.tres" id="7_s6"]

[resource]
script = ExtResource("1_ch")
ChapterId = 1
ChapterName = "Chương 1: Khởi Binh Đại Hán"
Subtitle = "Dẹp loạn Hoàng Cân, khởi binh định thiên hạ"
BackgroundImage = "res://assets/ui/activities/campaign/chapter_bg_ch01.png"
BgmTrack = ""
Stages = [ExtResource("2_s1"), ExtResource("3_s2"), ExtResource("4_s3"), ExtResource("5_s4"), ExtResource("6_s5"), ExtResource("7_s6")]
TotalStars = 18
UnlockRequirement = 0
"""
    with open(os.path.join(CHAPTERS_DIR, "chapter_01.tres"), "w", encoding="utf-8") as f:
        f.write(ch1_content)
    print("  -> Generated chapter_01.tres")

    # Chapter 2
    ch2_content = """[gd_resource type="Resource" script_class="ChapterData" load_steps=2 format=3]

[ext_resource type="Script" path="res://data/resources/ChapterData.cs" id="1_ch"]

[resource]
script = ExtResource("1_ch")
ChapterId = 2
ChapterName = "Chương 2: Quyết Chiến Xích Bích"
Subtitle = "Hỏa thiêu liên hoàn thuyền, định thế chân vạc"
BackgroundImage = "res://assets/ui/activities/campaign/chapter_bg_ch02.png"
BgmTrack = ""
Stages = []
TotalStars = 18
UnlockRequirement = 12
"""
    with open(os.path.join(CHAPTERS_DIR, "chapter_02.tres"), "w", encoding="utf-8") as f:
        f.write(ch2_content)
    print("  -> Generated chapter_02.tres")

CHAPTER2_STAGES = [
    {
        "filename": "stage_02_01.tres", "chapter": 2, "dir": "data/database/stages/ch02",
        "id": 7, "key": "2-1", "name": "Liên Minh Tôn Lưu",
        "desc": "Tôn Quyền và Lưu Bị kết minh tại Sài Tang, chuẩn bị nghênh chiến đại quân Tào Tháo.",
        "type": 0, "power": 9500, "stamina": 5,
        "enemies": [("hoang_cai", 0, 1, 22), ("chu_du", 1, 0, 22), ("bang_thong", 1, 2, 22), ("tieu_kieu", 2, 1, 22)],
        "first_gold": 3500, "first_dia": 350, "first_exp": 1400,
        "norm_gold": 900, "norm_exp": 600, "star2": 4, "star3": 8
    },
    {
        "filename": "stage_02_02.tres", "chapter": 2, "dir": "data/database/stages/ch02",
        "id": 8, "key": "2-2", "name": "Thuyền Cỏ Mượn Tên",
        "desc": "Gia Cát Lượng dùng thuyền cỏ mượn mười vạn mũi tên giữa sương mù sông Trường Giang.",
        "type": 0, "power": 11500, "stamina": 5,
        "enemies": [("tao_nhan", 0, 0, 25), ("dien_vi", 0, 2, 25), ("hoang_trung", 1, 1, 25), ("sai_van_co", 2, 0, 25), ("chu_du", 2, 2, 25)],
        "first_gold": 4200, "first_dia": 420, "first_exp": 1700,
        "norm_gold": 1100, "norm_exp": 720, "star2": 4, "star3": 9
    },
    {
        "filename": "stage_02_03.tres", "chapter": 2, "dir": "data/database/stages/ch02",
        "id": 9, "key": "2-3", "name": "Khổ Nhục Kế",
        "desc": "Hoàng Cái chịu đòn roi trá hàng, kho lửa Xích Bích chờ thời cơ bùng cháy.",
        "type": 2, "power": 13500, "stamina": 6,
        "enemies": [("hoang_cai", 0, 1, 28), ("dien_vi", 1, 0, 28), ("truong_phi", 1, 2, 28), ("bang_thong", 2, 1, 28)],
        "first_gold": 5000, "first_dia": 500, "first_exp": 2000,
        "norm_gold": 1300, "norm_exp": 850, "star2": 4, "star3": 9
    },
    {
        "filename": "stage_02_04.tres", "chapter": 2, "dir": "data/database/stages/ch02",
        "id": 10, "key": "2-4", "name": "Hỏa Thiêu Ô Sào",
        "desc": "Đột kích kho lương địch giữa đêm, lửa cháy rực trời làm rối loạn quân tâm.",
        "type": 0, "power": 16000, "stamina": 6,
        "enemies": [("dien_vi", 0, 0, 30), ("tao_nhan", 0, 2, 30), ("trieu_van", 1, 1, 30), ("dieu_thuyen", 2, 0, 30), ("gia_cat_luong", 2, 2, 30)],
        "first_gold": 5800, "first_dia": 580, "first_exp": 2300,
        "norm_gold": 1500, "norm_exp": 1000, "star2": 5, "star3": 10
    },
    {
        "filename": "stage_02_05.tres", "chapter": 2, "dir": "data/database/stages/ch02",
        "id": 11, "key": "2-5", "name": "Liên Hoàn Thuyền",
        "desc": "Chiến thuyền địch xích sắt nối liền, chỉ chờ một mồi lửa đông nam phong.",
        "type": 3, "power": 19000, "stamina": 7,
        "enemies": [("truong_phi", 0, 1, 32), ("quan_vu", 1, 0, 32), ("ma_sieu", 1, 2, 32), ("chu_du", 2, 0, 32), ("bang_thong", 2, 2, 32)],
        "first_gold": 6800, "first_dia": 680, "first_exp": 2600,
        "norm_gold": 1750, "norm_exp": 1100, "star2": 5, "star3": 10
    },
    {
        "filename": "stage_02_boss.tres", "chapter": 2, "dir": "data/database/stages/ch02",
        "id": 12, "key": "2-6", "name": "Tào Tháo — Ngụy Vương",
        "desc": "Tào Tháo thân chinh dẫn thủy quân quyết chiến Xích Bích, thế trận một mất một còn!",
        "type": 1, "power": 24000, "stamina": 8,
        "enemies": [("dien_vi", 0, 0, 35), ("truong_phi", 0, 2, 35), ("tao_nhan", 1, 1, 38), ("lu_bo", 2, 0, 35), ("dieu_thuyen", 2, 2, 35)],
        "first_gold": 8000, "first_dia": 800, "first_exp": 3000,
        "norm_gold": 2000, "norm_exp": 1200, "star2": 5, "star3": 11
    }
]

CHAPTER3_STAGES = [
    {
        "filename": "stage_03_01.tres", "chapter": 3, "dir": "data/database/stages/ch03",
        "id": 13, "key": "3-1", "name": "Tiến Quân Giang Lăng",
        "desc": "Thừa thắng truy kích, đại quân tiến xuống Giang Lăng củng cố thế chân vạc.",
        "type": 0, "power": 26000, "stamina": 7,
        "enemies": [("truong_phi", 0, 1, 38), ("hoang_trung", 1, 0, 38), ("ma_sieu", 1, 2, 38), ("tieu_kieu", 2, 1, 38)],
        "first_gold": 9000, "first_dia": 900, "first_exp": 3200,
        "norm_gold": 2200, "norm_exp": 1400, "star2": 5, "star3": 10
    },
    {
        "filename": "stage_03_02.tres", "chapter": 3, "dir": "data/database/stages/ch03",
        "id": 14, "key": "3-2", "name": "Đoạt Lấy Kinh Châu",
        "desc": "Kinh Châu bốn chiến, các lộ chư hầu cùng dòm ngó miếng mồi béo bở.",
        "type": 0, "power": 30000, "stamina": 7,
        "enemies": [("quan_vu", 0, 0, 40), ("dien_vi", 0, 2, 40), ("gia_cat_luong", 1, 1, 40), ("chu_du", 2, 0, 40), ("bang_thong", 2, 2, 40)],
        "first_gold": 10500, "first_dia": 1050, "first_exp": 3700,
        "norm_gold": 2600, "norm_exp": 1600, "star2": 5, "star3": 11
    },
    {
        "filename": "stage_03_03.tres", "chapter": 3, "dir": "data/database/stages/ch03",
        "id": 15, "key": "3-3", "name": "Hợp Phì Đại Chiến",
        "desc": "Trương Liêu uy chấn Hạp Phì, tám trăm tử sĩ phá vòng vây mười vạn quân.",
        "type": 2, "power": 34000, "stamina": 8,
        "enemies": [("tao_nhan", 0, 1, 42), ("truong_phi", 1, 0, 42), ("dien_vi", 1, 2, 42), ("dieu_thuyen", 2, 1, 42)],
        "first_gold": 12000, "first_dia": 1200, "first_exp": 4200,
        "norm_gold": 3000, "norm_exp": 1800, "star2": 5, "star3": 11
    },
    {
        "filename": "stage_03_04.tres", "chapter": 3, "dir": "data/database/stages/ch03",
        "id": 16, "key": "3-4", "name": "Định Quân Sơn",
        "desc": "Hoàng Trung lão tướng một đao chém Hạ Hầu Uyên trên đỉnh Định Quân Sơn.",
        "type": 0, "power": 38000, "stamina": 8,
        "enemies": [("hoang_cai", 0, 0, 45), ("hoang_trung", 0, 2, 45), ("trieu_van", 1, 1, 45), ("ma_sieu", 2, 0, 45), ("tieu_kieu", 2, 2, 45)],
        "first_gold": 14000, "first_dia": 1400, "first_exp": 4800,
        "norm_gold": 3500, "norm_exp": 2100, "star2": 6, "star3": 12
    },
    {
        "filename": "stage_03_05.tres", "chapter": 3, "dir": "data/database/stages/ch03",
        "id": 17, "key": "3-5", "name": "Thủy Yêm Thất Quân",
        "desc": "Quan Vũ uy chấn Hoa Hạ, nước lũ nhấn chìm bảy đạo quân Tào trong đêm mưa.",
        "type": 3, "power": 42000, "stamina": 9,
        "enemies": [("quan_vu", 0, 1, 48), ("truong_phi", 1, 0, 48), ("dien_vi", 1, 2, 48), ("chu_du", 2, 0, 48), ("gia_cat_luong", 2, 2, 48)],
        "first_gold": 16000, "first_dia": 1600, "first_exp": 5400,
        "norm_gold": 4000, "norm_exp": 2400, "star2": 6, "star3": 12
    },
    {
        "filename": "stage_03_boss.tres", "chapter": 3, "dir": "data/database/stages/ch03",
        "id": 18, "key": "3-6", "name": "Quyết Chiến Hứa Xương",
        "desc": "Trận quyết chiến cuối cùng trước cổng Hứa Xương - thiên hạ về một mối!",
        "type": 1, "power": 48000, "stamina": 10,
        "enemies": [("truong_phi", 0, 0, 50), ("quan_vu", 0, 2, 50), ("lu_bo", 1, 1, 55), ("gia_cat_luong", 2, 0, 50), ("chu_du", 2, 2, 50)],
        "first_gold": 18000, "first_dia": 1800, "first_exp": 6000,
        "norm_gold": 4500, "norm_exp": 2600, "star2": 6, "star3": 13
    }
]

def generate_chapter_file(chapter_id, name, subtitle, bg, stages, total_stars, unlock, filename):
    lines = [f'[gd_resource type="Resource" script_class="ChapterData" load_steps={len(stages) + 2} format=3]', ""]
    lines.append('[ext_resource type="Script" path="res://data/resources/ChapterData.cs" id="1_ch"]')
    refs = []
    for i, s in enumerate(stages):
        ext_id = f"{i + 2}_s{i + 1}"
        refs.append(f'ExtResource("{ext_id}")')
        lines.append(f'[ext_resource type="Resource" path="res://{s["dir"]}/{s["filename"]}" id="{ext_id}"]')
    lines.append("")
    lines.append("[resource]")
    lines.append('script = ExtResource("1_ch")')
    lines.append(f"ChapterId = {chapter_id}")
    lines.append(f'ChapterName = "{name}"')
    lines.append(f'Subtitle = "{subtitle}"')
    lines.append(f'BackgroundImage = "{bg}"')
    lines.append('BgmTrack = ""')
    lines.append(f"Stages = [{', '.join(refs)}]")
    lines.append(f"TotalStars = {total_stars}")
    lines.append(f"UnlockRequirement = {unlock}")
    content = "\n".join(lines) + "\n"
    with open(os.path.join(CHAPTERS_DIR, filename), "w", encoding="utf-8") as f:
        f.write(content)
    print(f"  -> Generated {filename}")

def main():
    print("📜 Generating Stages & Chapters Data...")
    for s in stages_info:
        generate_stage_tres(s)
    generate_chapters()
    for s in CHAPTER2_STAGES:
        generate_stage_tres(s)
    for s in CHAPTER3_STAGES:
        generate_stage_tres(s)
    generate_chapter_file(2, "Chương 2: Quyết Chiến Xích Bích",
        "Hỏa thiêu liên hoàn thuyền, định thế chân vạc",
        "res://assets/ui/activities/campaign/chapter_bg_ch02.png",
        CHAPTER2_STAGES, 18, 12, "chapter_02.tres")
    generate_chapter_file(3, "Chương 3: Định Đỉnh Giang Sơn",
        "Tam phân thiên hạ, nhất thống giang sơn",
        "res://assets/ui/activities/campaign/chapter_bg_ch02.png",
        CHAPTER3_STAGES, 18, 24, "chapter_03.tres")
    print("✅ All data .tres files generated!")

if __name__ == "__main__":
    main()
