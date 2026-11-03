# Phase 6 — Hoàn thiện hình ảnh, âm thanh và giao diện

## Nội dung hoàn thành

- Thêm `GameAudioManager` để điều phối nhạc chương hiện có, hiệu ứng giao diện/sự kiện và âm môi trường riêng.
- Bổ sung ba mức âm lượng có lưu độc lập: nhạc, hiệu ứng và âm môi trường; thêm nút tắt tiếng và giữ khóa `tp_sound` để tương thích với lời đọc hiện có.
- Thêm ba thanh trượt có nhãn tiếng Việt và tiếng Hàn. Âm lượng được giới hạn trong khoảng 0–1.
- Tạo âm môi trường và hiệu ứng ngắn bằng mã; giữ nhạc chương sẵn có. Không thêm bản thu thực địa hay thư viện âm thanh có bản quyền.
- Thêm hoạt ảnh thu/phóng khi lật bưu thiếp, phát tiếng lật trang và giữ nội dung mặt sau sau hoạt ảnh.
- Chỉnh bố cục menu, trang ngày hội và đoạn kết để nội dung tiếng Việt nằm gọn trong khung dọc.

## Tệp và tài nguyên

- Tạo `Assets/_Game/Scripts/UI/GameAudioManager.cs` và `.meta`.
- Sửa `PassportApp.cs`, `GameText.cs`, `TastePassportSmokeDriver.cs`, `TastePassportUiCaptureDriver.cs` và `TastePassportUiCapture.cs`.
- Không thay Scene hoặc Prefab trong phase này. Tái sử dụng hình ảnh hiện có; không thêm tệp ảnh hoặc tệp âm thanh độc lập. Âm môi trường và hiệu ứng mới được tổng hợp bằng mã.
- Đặc tả và kế hoạch nằm tại `docs/superpowers/specs/2026-11-03-audio-ui-polish-design.md` và `docs/superpowers/plans/2026-11-03-audio-ui-polish.md`.

## Kiểm thử

- `dotnet build TastePassport_KGW2026.sln -c Debug` — đạt, 0 cảnh báo và 0 lỗi.
- `TastePassport.EditorTools.TastePassportSmoke.Run` — đạt: sáu nhiệm vụ, hai xưởng, bảy trạm ngày hội, đổi ngôn ngữ Việt–Hàn, lật bưu thiếp có hoạt ảnh, cài đặt âm thanh và tiếp tục khám phá sau kết thúc.
- `TastePassport.EditorTools.TastePassportWorldSmoke.RunPhaseSixAudioChecks` — đạt: ba kênh được lưu riêng, tắt tiếng và giới hạn giá trị.
- Chụp và rà soát 10 màn hình ở mỗi kích thước 720×1280 và 1080×2400. Kết quả ở `Logs/UiPreviewPhase6/`; log xác nhận ở `Logs/TastePassportPhase6UiCaptureFinal.log`.

## Giới hạn

- Âm nhạc chương có sẵn; âm môi trường và hiệu ứng mới là âm tổng hợp, chưa có bản thu nhạc cụ truyền thống.
- Lời đọc Android cần dịch vụ TTS trên thiết bị hoặc trình giả lập; phase này chưa kiểm tra phát âm thanh trên thiết bị Android thật.
- Ảnh chụp giao diện xác nhận bố cục dọc ở hai kích thước, chưa thay thế kiểm tra trên nhiều mẫu điện thoại.

## Mở và kiểm tra trong Unity

Mở scene `Assets/_Game/Scenes/TastePassport_MVP.unity`, nhấn Play, vào **Cài đặt** để thử ba thanh âm lượng và nút tắt tiếng. Mở Hộ chiếu để xem bưu thiếp đã mở khóa, sau đó nhấn **Lật bưu thiếp**.
