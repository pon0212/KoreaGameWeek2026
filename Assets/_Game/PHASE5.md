# Phần 5 — Cốt truyện chính

## Nội dung hoàn thành

- Hoàn thiện tiến trình bốn chương trên hồ sơ hiện có. Gặp Nhung mở Chương 2; các mốc Hội An mở Jeonju và Chương 3; lời mời ngày hội mở Chương 4.
- Chỉ mở ngày hội sau sáu nhiệm vụ chính và khi cả đèn lồng Hội An lẫn sản phẩm Hanji Jeonju đã được lưu để trưng bày.
- Thay câu đố ngẫu nhiên có giới hạn thời gian bằng bảy góc triển lãm. Người chơi ghép dấu ấn đúng, tự chọn vật trang trí và nối ba cặp hoạt động Việt–Hàn. Mỗi bước được lưu riêng để tiếp tục sau khi rời màn hình.
- Hoàn thành ngày hội lưu trạng thái câu chuyện, chương 4 và dấu ấn kết thúc. Trang cuối lần lượt hiện không gian ngày hội, bốn nhân vật và lời kết có thể nghe đọc trên Android.
- Sau kết thúc, bản đồ vẫn mở các lối khám phá Hội An và Jeonju; Hộ chiếu và bản đồ hiển thị trạng thái hoàn thành.
- Bổ sung nội dung ngày hội bằng tiếng Việt và tiếng Hàn; tiếng Việt tiếp tục là ngôn ngữ mặc định.

## Kiểm tra

- `dotnet build TastePassport_KGW2026.sln -c Debug` — đạt, 0 cảnh báo và 0 lỗi.
- `TastePassportWorldSmoke.RunPhaseFiveChecks` — đạt: điều kiện mở truyện, lưu chương hoàn tất, mô hình bảy góc, vật trang trí, liên kết văn hóa, khôi phục tiến độ và chuỗi dịch Việt–Hàn.
- `TastePassportSmoke.Run` — đạt trên Unity 6000.6.7f1: sáu nhiệm vụ, hai xưởng, khóa/mở ngày hội, bảy dấu ấn và vật trang trí, ba cặp hoạt động, lưu kết thúc, bốn chân dung và mở lại hai thế giới tự do.

## Giới hạn

- Đoạn kết là hoạt ảnh giao diện fade/scale, chưa phải video hoặc cảnh 3D dựng sẵn.
- Bản dựng Windows và Android được kiểm tra ở Phần 7; chưa có kết quả từ thiết bị Android thực tế.
