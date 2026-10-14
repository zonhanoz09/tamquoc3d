# 05. Kiến trúc Kỹ thuật & Hạ tầng

<aside>
🛠️

Chuyên mục: Kiến trúc Kỹ thuật & Hạ tầng Mạng

Thiết kế kiến trúc hệ thống Client - Server, cơ chế Authoritative Server chống gian lận và đồng bộ dữ liệu Cross-platform.

</aside>

## 1. Công nghệ Phát triển (Tech Stack)

- **Client Engine:** **Unity** hoặc **Godot Engine**
    - Ưu điểm: Hiệu năng xử lý 2D Pixel vượt trội, hỗ trợ Pixel Perfect Camera, Build đa nền tảng (PC/Steam, iOS, Android, WebGL) từ một nguồn mã nguồn duy nhất.
- **Backend Server:** **Go (Golang)** hoặc **Node.js (TypeScript)**
    - Ưu điểm: Xử lý concurrency cực tốt, độ trễ thấp, tiêu tốn ít RAM máy chủ.
- **Cơ sở dữ liệu:**
    - **PostgreSQL:** Lưu trữ dữ liệu tài khoản, túi đồ, tướng, lịch sử giao dịch (Đảm bảo tính toàn vẹn ACID).
    - **Redis:** Lưu Cache phiên đăng nhập, Bảng xếp hạng PvP (Leaderboard) theo thời gian thực.

---

## 2. Kiến trúc Chống Gian lận (Server-Authoritative Combat)

Do tính chất của game **Turn-based**, toàn bộ logic trận đấu có thể kiểm soát chặt chẽ để **chống hack $100\%$**:

```
[ Client ] ──( Gửi Hành động / Lượt / Seed )──> [ Game Server ]
                                                      │
                                           [ Server tính toán sát thương, ]
                                           [ né tránh, đỡ đòn, sĩ khí     ]
                                                      │
[ Client hiển thị Animation ] <──( Trả về Kết quả Chuẩn )────┘
```

- **Client chỉ đóng vai trò hiển thị:** Client nhận dữ liệu kết quả từ server và chạy hiệu ứng hoạt họa (VFX/Animation).
- **RNG Seed Validation:** Mọi xác suất ngẫu nhiên (Bạo kích, Né tránh, Đỡ đòn) đều sinh từ `Seed` do Server cấp; Client không thể can thiệp sửa đổi kết quả.

---

## 3. Giao thức Mạng (Network Protocols)

- **RESTful API / gRPC (HTTPS):**
    - Dùng cho các tác vụ ngoài trận: Đăng nhập, Nâng cấp tướng, Mua sắm Shop, Quay Gacha, Nhận thư.
- **WebSockets / UDP (Nakama / Photon):**
    - Dùng cho Đấu trường Real-time Arena (RTA) và Kênh Chat cộng đồng.
    - Tự động xử lý cơ chế mất kết nối (Reconnect) trong 30 giây mà không bị xử thua ngay.

---

## 4. Đồng bộ Đa nền tảng (Cross-Play & Account Linking)

- Đăng nhập thông qua hệ thống tài khoản tập trung: Apple Sign-In, Google Play Games, Steam OpenID, Email/Pass.
- Dữ liệu tiến trình chơi đồng bộ tức thì trên Cloud: Người chơi có thể cày cuốc trên PC và tiếp tục trải nghiệm trên Mobile khi ra ngoài.