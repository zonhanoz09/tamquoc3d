# Tam Quốc 3D - Project Pixel Tactics (.NET 8 & Godot 4.3 Mono)

Dự án Game Chiến thuật Nhập vai Theo lượt 2D Pixel chủ đề Tam Quốc (Turn-Based Strategy RPG).

---

## 📖 Tài Liệu Dự Án (Documentation)

- **[Tài liệu Kiến trúc & Thư mục chi tiết (Dành cho AI & Dev)](docs/ARCHITECTURE_AND_DIRECTORIES.md)**: Chi tiết cấu trúc thư mục, chức năng từng file, luồng dữ liệu, quy chuẩn code.
- **[Tài liệu Game Design Document (GDD)](docs/Game%20Design%20Document%20(GDD)%20-%202D%20Pixel%20Turn-Based%20S.md)**
- **[01. Cơ chế Chiến đấu & Đội hình 3x3](docs/01%20C%C6%A1%20ch%E1%BA%BF%20Chi%E1%BA%BFn%20%C4%91%E1%BA%A5u%20%26%20%C4%90%E1%BB%99i%20h%C3%ACnh.md)**
- **[02. Thuộc tính & Phát triển Nhân vật](docs/02%20Thu%E1%BB%99c%20t%C3%ADnh%20%26%20Ph%C3%A1t%20tri%E1%BB%83n%20Nh%C3%A2n%20v%E1%BA%ADt.md)**

---

## 🛠️ Yêu Cầu Kỹ Thuật & Khởi Chạy

- **Engine:** Godot Engine 4.3 Mono (C#)
- **Runtime:** .NET SDK 8.0
- **Lệnh build C#:**
  ```bash
  dotnet build
  ```
- **Mở Editor Godot Mono:**
  ```bash
  godot -e
  ```
- **Chạy trực tiếp trận chiến:**
  ```bash
  godot
  ```

---

## 🚀 Chạy Server (Docker)

```bash
cp .env.example .env   # đặt POSTGRES_PASSWORD mới (mật khẩu cũ đã lộ trong git history - đừng dùng lại)
docker compose up -d   # PostgreSQL 16 + Redis 7 + ASP.NET Core server tại http://localhost:8000
```

- DB mới tự chạy các file `data/sql/00..08` theo thứ tự. DB đã tồn tại thì chạy tay các migration mới (`06_stage_rewards.sql`, `07_shop_items.sql`, `08_chapter_rewards.sql`).
- Nguyên tắc bảo mật: client **không bao giờ** gửi số thưởng/thể lực — server tự tra bảng `stage_rewards` / `shop_items` và trừ ví trong transaction.
- Redis được dùng làm read-through cache cho profile + tiến trình chiến dịch (TTL 60s, tự vô hiệu khi qua ải); Postgres vẫn là nguồn chuẩn, Redis sập thì API chạy thẳng xuống DB.

## ✅ Kiểm thử

```bash
dotnet build tamquoc3d.csproj        # client
dotnet build server/TamQuoc3D.Server.csproj
# Test headless (cần Godot 4.3 Mono + DOTNET_ROOT trỏ tới .NET SDK 8):
godot --headless --path . res://tests/TestCampaignMap.tscn
godot --headless --path . res://tests/TestManualBattle.tscn
godot --headless --path . res://tests/TestCombatDeterminism.tscn
```

GitHub Actions (`.github/workflows/ci.yml`) tự chạy build + toàn bộ test scene mỗi lần push.

## 📦 Export bản chơi

Cài **export templates bản Mono** (`Godot_v4.3-stable_mono_export_templates.tpz` — bản thường sẽ xuất game không nạp được C#) vào `~/.local/share/godot/export_templates/4.3.stable.mono/`, sau đó:

```bash
godot --headless --export-release "Linux" ../tamquoc3d-build/linux/TamQuoc3D.x86_64
godot --headless --export-release "Windows Desktop" ../tamquoc3d-build/windows/TamQuoc3D.exe
```

---

## 📁 Cấu Trúc Tổng Thể

- `core/`: Nhân hệ thống dùng chung (EventBus, GameManager, Interfaces, Constants, SoundManager).
- `data/`: Tầng dữ liệu thuần (Custom Resources C# và database file `.tres`).
- `features/`: Các module độc lập (`battle`, `deck_builder`, `campaign`).
- `assets/`: Tài nguyên hình ảnh, âm thanh chuẩn hóa (`heroes/`, `environments/`, `ui/`, `vfx/`, `common/`).
