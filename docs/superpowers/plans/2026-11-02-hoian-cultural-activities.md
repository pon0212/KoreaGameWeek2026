# Hoài An Cultural Activities Implementation Plan

> **Giai đoạn:** Phase 3 — Hoi An World
> **Mục tiêu:** Biến Chùa Cầu và làm đèn lồng thành nhiệm vụ có dữ kiện văn hóa, phần thưởng lưu được và dấu đổi thay nhìn thấy lại trong thế giới.

## Phạm vi

- Giữ Scene mở đầu và các vòng nhiệm vụ hiện có.
- Nâng hồ sơ lên schema 3, thêm túi đồ và bộ thẻ văn hóa có migration từ schema 1/2.
- Câu đố kiến trúc Hội An dùng chi tiết Chùa Cầu và mở thẻ văn hóa sau khi hoàn thành.
- Cô Lan giao hoạt động làm đèn lồng: chọn khung, chọn màu, kéo vật liệu vào vị trí, hoàn thiện sản phẩm.
- Lưu kiểu đèn đã chọn, thẻ văn hóa và dấu ấn; đèn đã làm xuất hiện trong khu trưng bày khi vào lại Hội An.
- Tiếng Việt mặc định, đủ bản dịch tiếng Hàn.

## Bước triển khai

1. **[x] Hồ sơ và bộ sưu tập** — thêm danh sách vật phẩm/thẻ văn hóa, migration và `InventoryService`; kiểm tra schema 1/2/3, dữ liệu trùng/không hợp lệ, lưu và tải lại.
2. **[x] Chùa Cầu** — thay các đối tượng khám phá chung bằng các chi tiết quan sát Chùa Cầu; thêm thẻ văn hóa được mở khóa cùng dấu nhiệm vụ và ghi nguồn văn hóa.
3. **[x] Chế tác đèn lồng** — thêm model phiên chơi có điều kiện hoàn thành, lựa chọn khung/màu và hai vật liệu phải được thả đúng vị trí; UI hỗ trợ chuột và cảm ứng.
4. **[x] Kết nối game và thế giới** — Cô Lan mở hoạt động từ thế giới đi bộ; kết quả lưu vật phẩm/thẻ/dấu theo giao dịch; hình đèn đã hoàn thiện tồn tại khi rời và mở lại Hội An; Hộ chiếu mở được bộ sưu tập.
5. **[x] Kiểm tra và báo cáo** — build runtime/editor, chạy migration, điều kiện mini-game, kéo-thả, lưu/tải lại, world-session và smoke toàn hành trình; cập nhật báo cáo Phase 3.

## Điều kiện nghiệm thu

- Không thể hoàn tất đèn nếu thiếu khung, màu hoặc một trong hai vật liệu.
- Hoàn tất nhiệm vụ thêm đúng một thẻ văn hóa và một sản phẩm tùy chỉnh vào hồ sơ; chơi lại không nhân bản phần thưởng.
- Nếu ghi hồ sơ lỗi, trang trí và dấu nhiệm vụ không bị cập nhật nửa chừng.
- Chùa Cầu mở trang văn hóa bằng ba chi tiết kiến trúc; nội dung không khẳng định truyền thuyết là sử liệu.
- Cô Lan, nút điều khiển, mục tiêu và phản hồi hiển thị tiếng Việt mặc định; đổi sang tiếng Hàn vẫn hoạt động.
- Đèn đã làm được hiển thị lại trong Hội An sau khi mở lại game.

## Nguồn nội dung

- Chùa Cầu: bài giới thiệu của Trung tâm Quản lý Bảo tồn Di sản Văn hóa Hội An, trong đó tách thông tin lịch sử khỏi truyền thuyết dân gian.
- Nghề đèn lồng: bài viết về nghệ nhân và cách chuẩn bị gỗ/tre làm khung của Cổng thông tin Di sản Hội An.
- Nguồn và các diễn giải dùng trong game được ghi tại `docs/cultural-content-sources.md`.
