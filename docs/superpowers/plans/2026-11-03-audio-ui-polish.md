# Kế hoạch Phần 6 — Âm thanh và hoàn thiện giao diện

**Mục tiêu:** Bổ sung ba kênh âm lượng có lưu, âm môi trường và hiệu ứng âm thanh sự kiện; làm bưu thiếp lật trang có hoạt ảnh; kiểm tra giao diện song ngữ.

**Thiết kế:** `docs/superpowers/specs/2026-11-03-audio-ui-polish-design.md`.

## Tác vụ

### 1. Cài đặt âm thanh

- [x] Viết kiểm tra cho lưu/giới hạn âm lượng và trạng thái tắt tiếng.
- [x] Tạo `GameAudioManager` bổ sung cho nhạc chương và tiếng chạm hiện có.
- [x] Thêm âm môi trường tổng hợp, các tiếng báo sự kiện và ba thanh trượt Cài đặt.
- [x] Kiểm tra các kênh, trạng thái tắt tiếng và nhãn Việt–Hàn.

### 2. Bưu thiếp và báo cáo

- [x] Thêm hoạt ảnh lật trang và tiếng lật.
- [x] Cập nhật smoke để chờ hoạt ảnh và xác nhận nội dung mặt sau.
- [x] Chụp/kiểm tra các màn hình chính ở hai độ phân giải dọc.
- [x] Viết `Assets/_Game/PHASE6.md`, dựng lại và kiểm tra trước khi commit/push.
