# 02. Thuộc tính & Phát triển Nhân vật

<aside>
📊

Chuyên mục: Thuộc tính & Phát triển Nhân vật

Tổng hợp toàn bộ chỉ số gốc, chỉ số chiến đấu, chỉ số phần trăm đặc biệt và công thức build tướng tối ưu theo từng vị trí.

</aside>

## 1. Thuộc tính Cơ bản (Thuộc tính Gốc)

Các chỉ số nền tảng tăng trưởng tự nhiên theo Level, tính năng *Tu luyện* và *Cường hóa*:

- **Lực lượng (Strength - Str):** Tăng mạnh **Vật công** và **Vật phòng**. Chỉ số cốt lõi của Chiến sĩ / Mãnh tướng.
- **Mẫn tiệp (Agility - Agi):** Tăng **Tốc độ** hành động và **Bạo kích**. Mẫn tiệp cao hơn giúp giành quyền ra đòn trước.
- **Trí lực (Intelligence - Int):** Tăng mạnh **Sách công** và **Sách phòng**. Chỉ số then chốt của Sách sĩ / Mưu sĩ / Hậu quân.
- **Sinh mệnh (Health Points - HP):** Tổng lượng máu của nhân vật. Khi HP về 0, nhân vật bị loại khỏi trận chiến.

---

## 2. Thuộc tính Chiến đấu (Trực tiếp)

- **Vật công (Physical Attack):** Sát thương gây ra từ đòn đánh thường hoặc kỹ năng vật lý.
- **Vật phòng (Physical Defense):** Giảm sát thương vật lý nhận vào từ đòn đánh của địch.
- **Sách công (Strategy Attack):** Sát thương phép/sách lược hoặc lượng máu hồi phục từ kỹ năng trị liệu.
- **Sách phòng (Strategy Defense):** Giảm sát thương phép/sách lược gánh chịu.
- **Tốc độ (Speed):** Quyết định thứ tự xuất chiêu trên chiến trường.

---

## 3. Thuộc tính Đặc biệt (Tỉ lệ Phần trăm %)

Nhận thêm từ *Khảm Bảo thạch (Đá quý)*, *Cánh*, *Trận pháp*, *Hồn tướng*:

| Chỉ số | Tên tiếng Anh | Cơ chế hoạt động trong trận đấu |
| --- | --- | --- |
| **Bạo kích** | Crit Rate | Tỉ lệ gây sát thương chí mạng (Gấp 1.5x - 2.0x sát thương thường). |
| **Kháng bạo** | Anti-Crit | Giảm tỉ lệ bị đối phương tấn công bạo kích. |
| **Chính xác** | Hit Rate | Tỉ lệ đánh trúng mục tiêu, trực tiếp khắc chế chỉ số Né tránh. |
| **Né tránh** | Dodge | Tỉ lệ né $100\%$ đòn đánh (không chịu sát thương, địch không được tăng Sĩ khí). Chỉ số vàng của Tiền quân. |
| **Phá kích** | Pierce | Tỉ lệ đánh xuyên giáp, làm giảm hiệu quả của chỉ số Đỡ đòn của đối phương. |
| **Đỡ đòn** | Block | Tỉ lệ giảm $50\%$ sát thương nhận vào và tự động **Phản công (Counter)** 1 đòn đánh thường. |
| **Sĩ khí** | Morale | Tích lũy để tung Nộ chiêu ($100+$ điểm). Đánh trúng $+25$, Bị đánh $+25$. |

---

## 4. Định hướng Build Chỉ số theo Vị trí

### 4.1 Tiền quân (Tanker / Tiên phong)

- *Tướng mẫu:* Điển Vi, Triệu Vân, Tào Nhân, Trương Phi.
- *Ưu tiên chỉ số & Đá quý:* **Sinh mệnh (HP) > Vật phòng / Sách phòng > Né tránh > Đỡ đòn**.
- *Chiến thuật:* Né tránh để ngắt nhịp Sĩ khí của địch, hoặc Đỡ đòn để phản kích liên tục bảo vệ đồng đội.

### 4.2 Trung quân (Sát thương Chủ lực - DPS)

- *Tướng mẫu:* Lữ Bố, Quan Vũ, Chu Du, Hoàng Trung.
- *Ưu tiên chỉ số & Đá quý:* **Lực lượng / Trí lực > Vật công / Sách công > Bạo kích > Chính xác > Tốc độ**.
- *Chiến thuật:* Dồn sát thương nhanh nhất, kết liễu Tiền quân địch hoặc đánh thọc sâu vào hàng sau.

### 4.3 Hậu quân (Hỗ trợ / Khống chế - Support & Control)

- *Tướng mẫu:* Gia Cát Lượng, Sái Văn Cơ, Tiểu Kiều, Bàng Thống.
- *Ưu tiên chỉ số & Đá quý:* **Tốc độ (Ưu tiên số 1) > Trí lực > Chính xác > Sách phòng**.
- *Chiến thuật:* Tốc độ cao nhất để xuất chiêu khống chế (Khóa chiêu, Choáng, Trừ Sĩ khí) hoặc buff trước khi đối phương kịp hành động.

---

## 5. Tiến trình Nâng cấp & Trang bị (Progression)

- **Cấp độ (Level) & Đột phá (Star Ascension):** Nâng trần cấp độ và hệ số tăng trưởng chỉ số gốc.
- **Cường hóa & Tẩy luyện (Refine):** Gia tăng chỉ số công thủ trực tiếp trên trang bị.
- **Khảm Bảo Thạch (Gem Socketing):** Bổ sung chỉ số đột biến % theo đúng vị trí.
- **Hệ thống Set Đồ (Equipment Sets):** Kích hoạt dòng ẩn (ví dụ: Set Tốc độ $+25\%$, Set Né tránh $+15\%$).