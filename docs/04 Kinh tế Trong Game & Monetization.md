# 04. Kinh tế Trong Game & Monetization

<aside>
💎

Chuyên mục: Kinh tế Trong Game & Kiếm tiền

Thiết kế vòng tuần hoàn tài nguyên, cân bằng nguồn thu - xả (Faucets & Sinks) và các gói sản phẩm IAP / Gacha bền vững.

</aside>

## 1. Hệ thống Tiền tệ & Tài nguyên

| Loại tiền tệ | Nguồn nhận (Faucets) | Nơi tiêu thụ (Sinks) | Tính chất |
| --- | --- | --- | --- |
| **Vàng (Gold)** | Chiến dịch, Phó bản vàng, Nhiệm vụ ngày | Cường hóa trang bị, Nâng cấp kỹ năng, Tẩy luyện | Soft Currency (Tiền tệ cơ bản) |
| **Kim Cương (Gems)** | Nạp tiền, Thưởng PvP, Vượt ải lần đầu, Sự kiện | Quay Gacha, Mua Thể lực, Mua lượt đấu trường | Hard Currency (Tiền tệ cao cấp) |
| **Thể Lực (Stamina)** | Tự hồi phục (5 phút/điểm), Nhận giờ ăn trưa/tối | Vượt ải cốt truyện, Cày phụ bản trang bị | Energy Gate (Kiểm soát tốc độ cày) |
| **Xu Đấu Trường** | Đạt thứ hạng cao, Thắng trận PvP | Đổi mảnh tướng hiếm, Đá quý Tốc độ | Token Shop |
| **Cống Hiến Bang** | Điểm danh bang, Đánh Boss bang | Mua kỹ năng bang hội, Mua trang bị Cam | Guild Currency |

---

## 2. Hệ thống Triệu Hồi Tướng (Gacha & Pity System)

### 2.1 Tỉ lệ Triệu hồi (Drop Rates)

- **Tướng UR / Thần Tướng:** $1.5\% - 2.0\%$
- **Tướng SSR / Danh Tướng:** $8.0\%$
- **Tướng SR / Tướng Tinh Anh:** $25.0\%$
- **Tướng R & Mảnh Tướng:** $65.0\%$

### 2.2 Cơ chế Bảo Hiểm (Pity Mechanism)

- **Bảo hiểm mềm (Soft Pity):** Tăng dần tỉ lệ ra tướng UR bắt đầu từ lượt quay thứ 60.
- **Bảo hiểm cứng (Hard Pity):** Chắc chắn ra tướng UR ở lượt quay thứ **80**.
- **Định hướng Tướng (Banner Guarantee):** Nếu tướng UR đầu tiên không phải tướng Banner, tướng UR kế tiếp chắc chắn là tướng Banner ($100\%$).

---

## 3. Các Mô hình Doanh thu & Gói IAP

1. **Thẻ Tháng (Monthly Pass):**
    - Giá rẻ ($4.99$), nhận ngay một lượng Kim cương và nhận đều đặn mỗi ngày kèm Thể lực. Tối ưu chuyển đổi người chơi trả phí lần đầu (Conversion Rate).
2. **Vé Mùa (Battle Pass / Season Pass):**
    - Chu kỳ 30 - 45 ngày; làm nhiệm vụ ngày/tuần để thăng cấp Pass.
    - Free Pass cho quà cơ bản; Premium Pass mở khóa Skin Pixel giới hạn và Tướng SSR độc quyền.
3. **Gói Nạp Lần Đầu (First Top-up Pack):**
    - Nạp mệnh giá bất kỳ nhận ngay 1 Tướng Tiền Quân xịn (Triệu Vân/Điển Vi) giúp tân thủ dễ dàng vượt ải giai đoạn đầu.
4. **Trang Phục Pixel Thẩm Mỹ (Pixel Skins):**
    - Hiệu ứng biến đổi hoạt họa chiêu thức và khung viền Avatar đẹp mắt, **hoàn toàn không tăng chỉ số** để bảo vệ tính công bằng trong PvP.