# 06. Lộ trình Triển khai & Cột mốc

<aside>
🗺️

Chuyên mục: Lộ trình Triển khai & Cột mốc Dự án

Kế hoạch phát triển dự án từ giai đoạn Prototype đến khi chính thức phát hành (Soft Launch & Global Release).

</aside>

## Danh sách Cột mốc Phát triển (Milestones)

### Giai đoạn 1: Prototype & Core Combat Validation (Tháng 1 - 2)

- [x] Xây dựng bộ khung chiến đấu 3 hàng (Tiền quân, Trung quân, Hậu quân) trên Engine.
- [x] Lập trình công thức tính sát thương, né tránh, đỡ đòn, bạo kích.
- [x] Hoàn thiện cơ chế Sĩ khí và thi triển Nộ chiêu.
- [x] Thử nghiệm cân bằng nội bộ cho 6 tướng mẫu đầu tiên.

### Giai đoạn 2: Nghệ thuật Pixel & Hoạt họa (Tháng 3 - 4)

- [x] Thiết kế Concept & Sprite Pixel Art cho 15 nhân vật đầu tiên (Bạch ngân giáp, Hắc kim giáp, Bát xà mâu, Thanh long đao, v.v.).
- [x] Tạo Animation hoàn chỉnh: Idle (nhịp thở), Attack (lướt chém/pháp thuật), Skill (Nộ khí phát quang & Cut-in Banner), Hurt (chớp trắng & giật lùi), Death (ngã gục tan biến).
- [x] Thiết kế hiệu ứng chiêu thức (Pixel VFX: chém, lửa Xích Bích, lốc gió, nước hồi phục, impact bạo kích, screen shake) và hệ thống âm thanh chiến trận Chiptune 16-bit tổng hợp (SoundManager).
- [x] Hoàn thiện giao diện người dùng (UI/UX): Bố cục chiến trường 3 hàng chuẩn, thanh máu trên đầu tướng, thanh Timeline thứ tự hành động, bộ điều khiển tốc độ (x1, x2, x4), tạm dừng, nhật ký chiến trận retro.

### Giai đoạn 3: Backend, Database & PvE Campaign (Tháng 5 - 6)

- [ ] Xây dựng hệ thống Server-authoritative và cơ sở dữ liệu tài khoản người chơi.
- [ ] Hoàn thiện Chapter 1 đến Chapter 5 chế độ Cốt truyện chính.
- [ ] Tích hợp phó bản tài nguyên hàng ngày và Tháp vô tận.

### Giai đoạn 4: Kinh tế, Gacha & Đấu trường Async (Tháng 7 - 8)

- [ ] Lập trình hệ thống Triệu hồi Gacha với thuật toán Pity bảo hiểm.
- [ ] Tích hợp Shop, Túi đồ, Hệ thống Khảm Bảo thạch và Trang bị Set.
- [ ] Hoàn thiện Đấu trường xếp hạng tự động (Asynchronous Arena).
- [ ] Tiến hành thử nghiệm Alpha Test kín (Closed Alpha).

### Giai đoạn 5: Real-Time Arena & Cross-platform Build (Tháng 9 - 10)

- [ ] Tích hợp WebSockets/Nakama cho chế độ Real-Time Arena (RTA) kèm Pick/Ban.
- [ ] Build và kiểm thử đa nền tảng trên PC (Steam/Web), iOS và Android.
- [ ] Tối ưu hóa hiệu năng, giảm dung lượng bộ cài đặt (dưới 300MB).
- [ ] Mở đăng ký trước và triển khai Closed Beta Test (CBT).

### Giai đoạn 6: Cân bằng Game, LiveOps & Soft Launch (Tháng 11 - 12)

- [ ] Cân bằng chỉ số toàn diện dựa trên dữ liệu phản hồi từ CBT.
- [ ] Tích hợp cổng thanh toán IAP (In-App Purchases), Battle Pass.
- [ ] Triển khai Soft Launch tại một số thị trường mục tiêu trước khi phát hành toàn cầu.
