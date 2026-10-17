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
ChapterId = 1
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
    path = os.path.join(STAGES_DIR, stage["filename"])
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

def main():
    print("📜 Generating Stages & Chapters Data...")
    for s in stages_info:
        generate_stage_tres(s)
    generate_chapters()
    print("✅ All data .tres files generated!")

if __name__ == "__main__":
    main()
