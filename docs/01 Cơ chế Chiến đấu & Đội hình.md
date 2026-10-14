# 01. Cơ chế Chiến đấu & Đội hình

<aside>
⚔️

Chuyên mục: Cơ chế Chiến đấu cốt lõi

Tài liệu chi tiết về hệ thống dàn trận theo hàng (Tiền quân, Trung quân, Hậu quân), cơ chế Sĩ khí và quy tắc giao tranh.

</aside>

## 1. Cấu trúc Đội hình (Formation Layout)

Mỗi bên tham chiến gồm 3 vị trí chiến lược chính:

| Vị trí | Vai trò chính | Đặc điểm chiến thuật & Khả năng |
| --- | --- | --- |
| **Tiền quân (Frontline)** | Tanker, Khiên chắn | Đứng đầu gánh chịu sát thương trực diện; chỉ số HP, Vật phòng, Sách phòng cao. Sở hữu Né tránh để chặn địch tích sĩ khí, Đỡ đòn để phản công. |
| **Trung quân (Midline)** | Sát thương Chủ lực (DPS) | Đứng sau Tiền quân; tập trung tối đa sát thương Vật lý hoặc Sách lược; chỉ số Tốc độ và Bạo kích cao để dứt điểm nhanh. |
| **Hậu quân (Backline)** | Khống chế (CC) & Hỗ trợ (Support) | Đứng hàng cuối; sở hữu kỹ năng khống chế diện rộng (Choáng, Khóa chiêu, Giảm sĩ khí) hoặc buff máu/sĩ khí; cực kỳ phụ thuộc vào Tốc độ. |

---

## 2. Quy tắc Tầm đánh & Chọn Mục tiêu (Targeting Rules)

1. **Đòn đánh thường (Melee Standard):** Bắt buộc phải tấn công mục tiêu thuộc **Tiền quân** còn sống. Khi Tiền quân ngã xuống mới đánh tới Trung quân, rồi đến Hậu quân.
2. **Kỹ năng Tầm xa / Pháp thuật (Ranged / Magic Skill):** Có thể nhắm thẳng vào Trung quân hoặc Hậu quân của đối phương mà không bị cản bởi Tiền quân.
3. **Kỹ năng Sát thủ / Đột kích (Flank / Ambush):** Nhảy thẳng ra sau hàng ngũ địch để tấn công và khống chế Hậu quân.
4. **Kỹ năng Xuyên thấu (Piercing Line):** Đánh thẳng từ Tiền quân xuyên thấu đến Hậu quân trên cùng một đường thẳng.
5. **Kỹ năng Diện rộng (AoE):**
    - *Row AoE:* Đánh toàn bộ các tướng trong cùng 1 hàng (ví dụ: sát thương toàn bộ Trung quân).
    - *Full Board AoE:* Gây sát thương lên toàn bộ đội hình địch.

---

## 3. Hệ thống Tốc độ & Thứ tự Lượt (Speed & Turn Priority)

- **Tốc độ toàn đội:** Đội nào có tổng điểm Tốc độ của các tướng cao hơn sẽ giành quyền **Đi trước toàn diện** (ưu tiên xuất chiêu trước).
- **Tốc độ cá nhân:** Trong cùng một đội hoặc giữa các tướng cùng lượt, thứ tự ra tay được quyết định bởi chỉ số **Mẫn tiệp / Tốc độ** riêng của từng tướng.

---

## 4. Cơ chế Sĩ khí & Kỹ năng Nộ (Morale System)

Sĩ khí là tài nguyên quyết định việc thi triển Tuyệt kỹ (Nộ chiêu) trong trận đấu:

- **Sĩ khí ban đầu:** Mặc định khi vào trận là **50** hoặc **100** (tùy thuộc vào Trang bị, Hồn tướng, Trận pháp).
- **Quy tắc tích lũy Sĩ khí:**
    - Đánh thường trúng mục tiêu: **+25 Sĩ khí**
    - Bị địch tấn công trúng: **+25 Sĩ khí**
    - *Né tránh thành công:* Người né không nhận sát thương và **kẻ tấn công KHÔNG được tăng Sĩ khí**.
- **Thi triển Nộ Chiêu:**
    - Khi Sĩ khí đạt **$\ge 100$**, tướng sẽ tiêu hao toàn bộ Sĩ khí để tung ra **Kỹ năng đặc biệt** ở lượt kế tiếp.
    - Một số tướng Hậu quân có kỹ năng **Hồi Sĩ khí cho đồng đội** hoặc **Giảm Sĩ khí của địch** để kiểm soát nhịp độ trận đấu.

---

## 5. Tương khắc Hệ & Nguyên tố (Elemental Affinities)

- **Vòng tương khắc cơ bản:** Lửa 🔥 $\rightarrow$ Gió/Cây 🍃 $\rightarrow$ Nước 💧 $\rightarrow$ Lửa 🔥.
- **Hệ đặc biệt:** Ánh sáng ☀️ $\longleftrightarrow$ Bóng tối 🌑 (Khắc chế lẫn nhau).
- **Hiệu số tương khắc:**
    - Khắc hệ đối thủ: $+30\%$ Sát thương gây ra, $+15\%$ Tỉ lệ Bạo kích.
    - Bị đối thủ khắc: $-20\%$ Sát thương gây ra.