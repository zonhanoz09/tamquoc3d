# Game Design Document (GDD) - 2D Pixel Turn-Based Strategy RPG

<aside>
🎮

Game Design Document (GDD) - Trung Tâm Quản Lý Dự Án

Tài liệu thiết kế trò chơi tổng thể cho dự án **2D Pixel Turn-Based Strategy RPG** (Cross-platform). Tài liệu đã được module hóa thành 6 trang chuyên mục con để dễ dàng theo dõi, mở rộng và bàn giao cho đội ngũ phát triển.

</aside>

## 📌 Tổng quan Dự án (Project Summary)

- **Tên dự án (Working Title):** Project Pixel Tactics
- **Thể loại:** Turn-based Strategy RPG (Chiến thuật / Nhập vai theo lượt)
- **Phong cách đồ họa:** 2D Pixel Art chất lượng cao (HD-2D / Modern Retro)
- **Nền tảng:** Cross-platform (PC / Web / iOS / Android)
- **Cơ chế chiến thuật cốt lõi:** Dàn trận 3 hàng (Tiền quân - Trung quân - Hậu quân), Hệ thống Tốc độ & Sĩ khí (Morale / Nộ chiêu)

---

## 🧭 Cây Tài Liệu Chuyên Mục (GDD Modules)

[01. Cơ chế Chiến đấu & Đội hình](https://app.notion.com/p/01-C-ch-Chi-n-u-i-h-nh-48ab2a9240704f639d3bd0b7769e075b?pvs=21)

*Cấu trúc Tiền quân - Trung quân - Hậu quân, Quy tắc tầm đánh/mục tiêu, Tốc độ xuất chiêu, Cơ chế Sĩ khí & Nộ chiêu, Hệ thống tương khắc nguyên tố.*

[02. Thuộc tính & Phát triển Nhân vật](https://app.notion.com/p/02-Thu-c-t-nh-Ph-t-tri-n-Nh-n-v-t-1f9a66ad9bdd416c94a47c9e6c5bf990?pvs=21)

*Chỉ số gốc (Str/Agi/Int/HP), Chỉ số chiến đấu (Vật/Sách công thủ), Chỉ số đột biến % (Bạo kích, Né tránh, Đỡ đòn phản công), Hướng dẫn build ngọc theo vị trí.*

[03. Chế độ Chơi & Hệ thống Online](https://app.notion.com/p/03-Ch-Ch-i-H-th-ng-Online-73e1d8a624cc48df94e96d6d5ae4cbdd?pvs=21)

*Chiến dịch cốt truyện PvE, Phó bản tài nguyên, Tháp vô tận, Mê cung Rogue-like, Đấu trường Xếp hạng (Async), Đấu trường RTA thời gian thực (Pick/Ban), Bang hội.*

[04. Kinh tế Trong Game & Monetization](https://app.notion.com/p/04-Kinh-t-Trong-Game-Monetization-4e90864370e648808f1ce4a29641095f?pvs=21)

*Hệ thống tiền tệ (Vàng, Kim cương, Thể lực), Thuật toán Gacha & Pity bảo hiểm, Battle Pass, Monthly Pass, Trang phục Pixel thẩm mỹ không pay-to-win.*

[05. Kiến trúc Kỹ thuật & Hạ tầng](https://app.notion.com/p/05-Ki-n-tr-c-K-thu-t-H-t-ng-333290d50d714e88b11f97410ca4a431?pvs=21)

*Tech stack (Unity/Godot + Go/Node.js + PostgreSQL + Redis), Cơ chế Server-authoritative chống hack 100%, Giao thức REST + WebSockets, Đồng bộ Cloud Cross-play.*

[06. Lộ trình Triển khai & Cột mốc](https://app.notion.com/p/06-L-tr-nh-Tri-n-khai-C-t-m-c-c2f54e26aa0041a4baed5a2ba7d353fd?pvs=21)

*Kế hoạch 6 giai đoạn phát triển từ Prototype (Tháng 1-2) đến Soft Launch & Global Release (Tháng 11-12).*

---

## 🔄 Vòng Lặp Trò Chơi Cốt Lõi (Core Game Loop)

```
[ Thu thập & Nâng cấp Tướng ] ➔ [ Xây dựng Đội hình & Chiến thuật Hàng ]
             ▲                                             ▼
             │                                   [ Tham gia Trận chiến ]
             │                                (PvE / Campaign / PvP RTA)
             │                                             ▼
     [ Nhận Tài nguyên & Phần thưởng ] ◄───────── [ Chiến thắng / Thất bại ]
```