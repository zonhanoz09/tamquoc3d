# Hướng Dẫn Kiến Trúc & Cấu Trúc Thư Mục

Tài liệu mô tả cấu trúc hiện hành của **Tam Quốc 3D (Project Pixel Tactics)** và quy ước tổ chức game client Godot.

## 1. Nền tảng và nguyên tắc

- **Engine:** Godot 4.3 Mono
- **Runtime:** .NET 8 / C# 12
- **Kiến trúc client:** feature-based, dùng C# và Godot resources (`.tres`)
- **Độ phân giải thiết kế:** 1280 × 720
- **Lệnh kiểm tra:** `dotnet build tamquoc3d.csproj`; chạy thử project bằng `godot --headless --path . --quit-after 20`

### Quy ước

1. Logic game và script gắn vào Node/Resource viết bằng C#; không thêm GDScript.
2. Các lớp Godot dùng `partial`; thêm `[GlobalClass]` khi cần đăng ký custom type cho Inspector.
3. Namespace theo module, ví dụ `TamQuoc3D.Core.*`, `TamQuoc3D.Data.*`, `TamQuoc3D.Features.*`. Thư mục con phân loại trách nhiệm, không bắt buộc namespace phải phản chiếu toàn bộ đường dẫn.
4. Đặt scene, controller và UI trong feature sở hữu chúng. Tập trung thành phần thực sự dùng chung trong `core/`, dữ liệu dùng chung trong `data/`, và tài nguyên chia sẻ trong `assets/`.
5. Giữ nguyên đường dẫn `res://` nhất quán trong scene, code, cấu hình và test khi di chuyển resource.

## 2. Cấu trúc repository

```text
res://
├── project.godot                 # Cấu hình project, autoload và scene khởi chạy
├── core/                         # Hệ thống dùng chung cho game client
│   ├── autoload/                 # EventBus, GameManager
│   ├── audio/                    # SoundManager
│   ├── constants/                # Hằng số và enum dùng chung
│   ├── interfaces/               # Hợp đồng dùng chung
│   ├── navigation/               # SceneNavigator và định tuyến màn hình
│   └── network/                  # NetworkManager và giao tiếp client-server
├── data/                         # Schema và dữ liệu game
│   ├── resources/                # HeroData, SkillData, StageData, MapConfig...
│   ├── database/                 # HeroDatabase và các resource .tres
│   │   ├── chapters/
│   │   ├── heroes/
│   │   ├── maps/
│   │   ├── skills/
│   │   └── stages/
│   └── sql/
├── features/                     # Các module gameplay/UI độc lập
│   ├── auth/scenes/                # LoginScene.tscn và LoginController.cs
│   ├── arena/scenes/              # ArenaScene.cs và ArenaScene.tscn
│   ├── battle/
│   │   ├── scenes/               # BattleScene.cs và BattleScene.tscn
│   │   ├── controllers/          # BattleManager, TurnManager, CombatFormulas...
│   │   ├── entities/             # BattleHero, HeroBattleNode, health bar, VFX...
│   │   └── grid/                 # Grid3x3, CellNode, PedestalSlot
│   ├── campaign/
│   │   ├── scenes/               # CampaignScene.cs và CampaignScene.tscn
│   │   ├── controllers/          # CampaignController
│   │   └── ui/                    # Chapter map, stage node, panel và popup
│   ├── deck_builder/
│   │   ├── scenes/               # DeckBuilderScene.tscn
│   │   ├── controllers/          # DeckBuilderController
│   │   └── ui/                    # Formation grid/slot, hero card/filter
│   ├── guild/scenes/             # GuildScene.cs và GuildScene.tscn
│   ├── hero_collection/scenes/   # HeroCollectionScene.cs và scene
│   └── hub/
│       ├── scenes/               # HubController.cs và HubScene.tscn
│       └── ui/                    # HubBackground, EventNotification và widget Hub
├── assets/                       # Tài nguyên dùng chung hoặc theo nội dung
│   ├── common/                   # Audio, fonts, icons, shaders
│   ├── environments/             # Battlefield và thành phần môi trường dùng chung
│   ├── heroes/                   # Nội dung đóng gói theo hero
│   ├── ui/                        # Texture UI dùng chung và theo feature
│   └── vfx/                       # VFX dùng chung
├── tests/                        # Test script và test scene Godot
├── tools/                        # Công cụ phát triển và pipeline asset
├── docs/                         # Tài liệu kỹ thuật và thiết kế
└── server/                       # ASP.NET Core 8 API độc lập với game client Godot
    ├── Authentication/           # Bearer session handler
    ├── Contracts/                # Request/response DTOs
    ├── Endpoints/                # Minimal API route groups
    ├── Middleware/               # RFC 7807 error handling
    └── Services/                 # Auth, PostgreSQL and game logic
```

### Quy ước bên trong mỗi feature

- `scenes/`: file `.tscn` và script C# điều khiển root scene tương ứng.
- `controllers/`: điều phối logic của feature, không gắn với root scene cụ thể.
- `ui/`: widget và component trình bày chỉ dùng trong feature.
- Giữ thư mục chuyên biệt như `battle/entities/` và `battle/grid/` khi chúng giúp mô tả rõ vai trò.
- Không chuyển resource dùng chung khỏi `data/` hoặc tài nguyên dùng chung khỏi `assets/` vào một feature.

## 3. Khởi chạy và điều hướng

Godot nạp autoload được khai báo trong `project.godot` (`EventBus`, `GameManager`, `HeroDatabase`, `NetworkManager`), sau đó mở màn hình xác thực:

```text
res://features/auth/scenes/LoginScene.tscn
```

Sau khi đăng nhập/đăng ký thành công và tải dữ liệu tài khoản, client chuyển tới Hub. Phiên truy cập được cache tại `user://auth_session.json`; mật khẩu không được lưu trong client. `core/navigation/SceneNavigator.cs` quản lý các đường dẫn màn hình. Scene Hub và battle:

```text
res://features/hub/scenes/HubScene.tscn
res://features/battle/scenes/BattleScene.tscn
```

Khi đổi vị trí scene hoặc script, cập nhật đồng bộ `project.godot`, `SceneNavigator`, đường dẫn `ExtResource` trong `.tscn`, cùng test/tool/config có tham chiếu đến resource đó.

## 4. Kết nối database

Game client **không kết nối trực tiếp PostgreSQL**. Luồng dữ liệu có chủ đích:

```text
Godot (NetworkManager)
    -> ASP.NET Core 8 REST API (server/)
        -> PostgreSQL (profile, wallet, heroes, formations, progression...)
        -> Redis (health/availability)
```

- Backend chạy .NET 8, truy cập PostgreSQL qua Npgsql và được build từ `server/TamQuoc3D.Server.csproj`. Client Godot vẫn gọi cùng base URL `http://localhost:8000`.
- Khởi chạy các dịch vụ phát triển từ thư mục repository bằng `docker compose up -d --build postgres redis game-server`.
- Godot gọi Game Server tại `http://localhost:8000` qua `core/network/NetworkManager.cs`; cấu hình Compose truyền địa chỉ PostgreSQL nội bộ cho backend.
- `/api/auth/register`, `/api/auth/login`, `/api/auth/session` và `/api/auth/logout` cấp/quản lý bearer session. Mật khẩu được hash PBKDF2-HMAC-SHA256 310.000 vòng tương thích với tài khoản hiện tại; token ngẫu nhiên có thể thu hồi, chỉ lưu SHA-256 hash trong PostgreSQL. Các API dữ liệu người chơi yêu cầu bearer token và lấy user ID từ session, không nhận user ID tùy ý từ client.
- Bảng xác thực và session được khai báo trong `data/sql/03_account_auth.sql`. Với database đã tồn tại, áp dụng migration bằng `docker compose exec -T postgres psql -U tamquoc -d tamquoc_db -v ON_ERROR_STOP=1 -f /docker-entrypoint-initdb.d/03_account_auth.sql`.
- Danh mục hero, skill và talent nằm trong `data/sql/00_hero_catalog.sql`. Tiến trình sở hữu nằm riêng trong `user_heroes`, `user_hero_skills` và `user_hero_talents`; tài khoản mới nhận một `default_hero` không có skill, chỉ số cơ bản và tên hiển thị được lấy từ `display_name`. Với database đã tồn tại, áp dụng catalog, các migration theo thứ tự:
  `docker compose exec -T postgres psql -U tamquoc -d tamquoc_db -v ON_ERROR_STOP=1 -f /docker-entrypoint-initdb.d/00_hero_catalog.sql`
  `docker compose exec -T postgres psql -U tamquoc -d tamquoc_db -v ON_ERROR_STOP=1 -f /docker-entrypoint-initdb.d/04_hero_progress_migration.sql`
  `docker compose exec -T postgres psql -U tamquoc -d tamquoc_db -v ON_ERROR_STOP=1 -f /docker-entrypoint-initdb.d/05_default_user_hero.sql`
  Migration 04 chuyển tiến trình JSON cũ sang các bảng quan hệ; migration 05 thêm tên riêng cho hero; sau đó rebuild/restart game server.
- OpenAPI/Swagger UI có tại `http://localhost:8000/docs`; các request mẫu nằm trong `server/TamQuoc3D.Server.http`.
- `GET /health` phải xác nhận `database_connected: true`. Huy hiệu trên Hub báo trạng thái PostgreSQL, không chỉ việc HTTP server phản hồi.
- Nếu backend hoặc PostgreSQL chưa sẵn sàng lúc mở game, `NetworkManager` kiểm tra lại mỗi 5 giây và tự đồng bộ khi kết nối thành công.
- Khi database online, NetworkManager tải profile, wallet và formation rồi cập nhật `GameManager`. Bạc (`silver`) cấp cho số vàng UI; vàng premium (`gold`) cấp cho KNB UI; dữ liệu profile được lưu thành cache cục bộ cho chế độ offline.
- Khi chưa đăng nhập hoặc backend/database offline, game ở màn hình đăng nhập và không mở Hub. Sau khi xác thực thành công, dữ liệu profile được lưu cục bộ để cache; cần kết nối backend để đăng nhập và lấy dữ liệu authoritative.

Kiểm tra kết nối backend và database:

```bash
docker compose ps
curl http://localhost:8000/health
godot --headless --path . res://tests/TestClientServerConnection.tscn --quit-after 600
```

Không đưa thông tin xác thực PostgreSQL vào client, resource Godot hoặc repository công khai; chỉ backend mới được giữ quyền truy cập database.

## 5. Tổ chức dữ liệu

- Custom resource schema đặt tại `data/resources/`.
- Các bản ghi `.tres` đặt tại `data/database/`, chia theo loại nội dung như `heroes/`, `skills/`, `maps/`, `chapters/` và `stages/`.
- `HeroDatabase` là autoload quản lý dữ liệu hero; không nhân bản dữ liệu dùng chung trong từng feature.
- Tài nguyên đồ họa, âm thanh và VFX đặt trong `assets/`; asset riêng của hero có thể đóng gói dưới `assets/heroes/<hero_id>/`.

## 6. Kiểm tra thay đổi

Sau khi thay đổi code hoặc đường dẫn resource:

1. Build: `dotnet build tamquoc3d.csproj`
2. Kiểm tra khởi động project: `godot --headless --path . --quit-after 20`
3. Chạy test scene phù hợp, ví dụ: `godot --headless --path . res://tests/TestHubScreen.tscn`
4. Đảm bảo không còn tham chiếu `res://` đến vị trí cũ và các scene liên quan nạp được.
