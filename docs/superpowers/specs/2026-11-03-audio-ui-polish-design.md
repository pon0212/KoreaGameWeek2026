# Phần 6 — Âm thanh và hoàn thiện giao diện

## Mục tiêu

Giữ phong cách sách tranh ấm áp và các minh họa hiện có; hoàn thiện cài đặt âm thanh và phản hồi trang bưu thiếp bằng những thành phần đã có trong dự án.

## Phương án

1. **Bổ sung bộ điều khiển âm thanh cho hệ thống hiện có (chọn).** Giữ nhạc chương và tiếng chạm hiện có, thêm âm thanh môi trường tạo bằng mã, các tiếng báo ngắn, ba thanh âm lượng riêng và trạng thái tắt tiếng được lưu.
2. Thay toàn bộ âm thanh bằng các tệp nhạc và thu âm mới. Cách này cần nguồn âm thanh có quyền sử dụng và thời gian rà soát văn hóa; hiện không có thư viện âm thanh được cấp phép trong dự án.
3. Chỉ đổi màu nút tắt tiếng. Cách này không đáp ứng yêu cầu chỉnh riêng nhạc, hiệu ứng và âm môi trường.

## Thiết kế

- `GameAudioManager` điều phối nhạc nền chương thông qua `StageAudioDirector`, dùng kênh hiệu ứng hiện có cho tiếng chạm và hiệu ứng sự kiện, đồng thời quản lý một nguồn âm môi trường riêng.
- Âm lượng nhạc, hiệu ứng và âm môi trường được lưu riêng trong `PlayerPrefs`. Cài đặt tắt tiếng tiếp tục dùng khóa `tp_sound` để giữ tương thích với lời đọc Android.
- Trang Cài đặt có ba thanh trượt có nhãn tiếng Việt/Hàn và nút tắt/bật tiếng. Cài đặt không phụ thuộc hồ sơ tiến trình.
- Âm môi trường và tiếng báo là âm thanh tổng hợp nhẹ, tạo bằng mã, không dùng mẫu thu có bản quyền và không được giới thiệu như bản thu thực địa hay nhạc chuyên nghiệp.
- Bưu thiếp thu nhỏ theo chiều ngang, đổi mặt ở điểm giữa và mở lại; tiếng lật trang được phát cùng hiệu ứng. Chuyển trang fade hiện có và hiệu ứng hoa anh đào tiếp tục dùng.
- Bổ sung tiếng báo cho đóng dấu, hoàn thành thủ công, mở khóa và hoàn tất ngày hội.

## Kiểm tra

- Kiểm tra lưu, tải lại, giới hạn 0–1 và tắt tiếng.
- Smoke UI xác nhận đủ ba thanh trượt và nhãn ở cả tiếng Việt lẫn tiếng Hàn.
- Smoke gameplay chờ hoạt ảnh lật bưu thiếp kết thúc trước khi xác nhận mặt sau.
- Chạy kiểm tra hình ảnh ở hai độ phân giải dọc sau khi cập nhật giao diện.

## Giới hạn

Âm nhạc và âm môi trường vẫn là bản tổng hợp procedural; chưa có nhạc cụ truyền thống được thu/ghi âm bởi nghệ sĩ. Android TTS cần thiết bị hoặc trình giả lập có dịch vụ TTS.
