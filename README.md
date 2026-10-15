# Tam Quốc 3D - Project Pixel Tactics (.NET 8 & Godot 4.3 Mono)

Dự án Game Chiến thuật Nhập vai Theo lượt 2D Pixel chủ đề Tam Quốc (Turn-Based Strategy RPG).

---

## 📖 Tài Liệu Dự Án (Documentation)

- **[Tài liệu Kiến trúc & Thư mục chi tiết (Dành cho AI & Dev)](file:///home/annhan/Documents/src/tamquoc3d/docs/ARCHITECTURE_AND_DIRECTORIES.md)**: Chi tiết cấu trúc thư mục, chức năng từng file, luồng dữ liệu, quy chuẩn code.
- **[Tài liệu Game Design Document (GDD)](file:///home/annhan/Documents/src/tamquoc3d/docs/Game%20Design%20Document%20(GDD)%20-%202D%20Pixel%20Turn-Based%20S.md)**
- **[01. Cơ chế Chiến đấu & Đội hình 3x3](file:///home/annhan/Documents/src/tamquoc3d/docs/01%20C%C6%A1%20ch%E1%BA%BF%20Chi%E1%BA%BFn%20%C4%91%E1%BA%A5u%20%26%20%C4%90%E1%BB%99i%20h%C3%ACnh.md)**
- **[02. Thuộc tính & Phát triển Nhân vật](file:///home/annhan/Documents/src/tamquoc3d/docs/02%20Thu%E1%BB%99c%20t%C3%ADnh%20%26%20Ph%C3%A1t%20tri%E1%BB%83n%20Nh%C3%A2n%20v%E1%BA%ADt.md)**

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

## 📁 Cấu Trúc Tổng Thể

- `core/`: Nhân hệ thống dùng chung (EventBus, GameManager, Interfaces, Constants, SoundManager).
- `data/`: Tầng dữ liệu thuần (Custom Resources C# và database file `.tres`).
- `features/`: Các module độc lập (`battle`, `deck_builder`, `campaign`).
- `assets/`: Tài nguyên hình ảnh, âm thanh chuẩn hóa (`heroes/`, `environments/`, `ui/`, `vfx/`, `common/`).
