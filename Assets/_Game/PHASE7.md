# Giai đoạn 7 — Kiểm thử tích hợp và bản dựng

## Phạm vi đã kiểm tra — 2026-11-09

- Scene chơi chính: `Assets/_Game/Scenes/TastePassport_MVP.unity`.
- Unity Editor GUI smoke Direct3D11: tạo hồ sơ, di chuyển/collision/camera, vào hai world, chơi sáu hoạt động, chụp ảnh Hội An và Bibimbap, kiểm tra replay không cộng xu, chuẩn bị ngày hội, xem đoạn kết và tiếp tục Free Exploration.
- World-session smoke: truyện mở đầu và nút bỏ qua, save/checkpoint, camera và các tương tác world.
- Persistence/architecture smoke: ảnh JPEG và timeline, save migration/recovery, journal layout, inventory gating của festival và fallback narration.
- Phase 4 smoke: hoạt động Jeonju/Hanji và vật thể trưng bày sau khi reload.
- Phase 5 smoke: các cổng tiến trình, bảy trạm, ba liên kết giao lưu, lưu/tải festival, đoạn kết và Free Exploration.
- Phase 6 smoke: ba kênh âm lượng, mute và giới hạn giá trị.

## Build và kiểm tra gói Android

- Solution C# build bằng Unity API profile .NET Framework 4.7.1: thành công, 0 lỗi, 3 cảnh báo MSB3277 về phiên bản assembly Unity/.NET.
- APK dựng bằng Unity `6000.6.7f1` có Android Build Support: `Builds/Android/TastePassport.apk`, 50,199,086 byte.
- Kiểm tra package: `com.DefaultCompany.TastePassport_KGW2026`, portrait, min SDK 26, target SDK 37, OpenGL ES 3.1; `apksigner` xác nhận chữ ký APK v2.
- Có thể đọc log và kết quả chi tiết trong [báo cáo kiểm thử](../../docs/TEST_REPORT.md).

## Giới hạn Android thực tế

APK đã cài và activity khởi chạy trong AVD `TastePassportPhone`; process còn sống ở lần quan sát sau 5 giây. AVD báo OpenGL ES 3.0, thấp hơn yêu cầu ES 3.1 trong manifest. Logcat còn có `ClassNotFoundException` cho `com.google.android.play.core.assetpacks.AssetPackManager`; nguyên nhân/chức năng ảnh hưởng chưa xác định. Vì vậy lần chạy này **không** xác nhận được render, gameplay, audio, save/load hay TTS Android. Không có điện thoại vật lý kết nối.

## Phạm vi còn mở

- `Builds/Windows/TastePassport.exe` cũ hơn mã hiện tại; PC được kiểm tra qua Unity Editor, chưa dựng lại standalone Windows trong đợt này.
- Chưa đo FPS, CPU hoặc bộ nhớ trên thiết bị Android.
- TTS Android với voice vi-VN/ko-KR cần được kiểm tra trên thiết bị có engine và OpenGL ES tương thích.
- UI tiếng Hàn và mỹ thuật cuối cần người bản ngữ/thiết bị thật duyệt.
- Kết quả 39 Use Case: 38 DONE, 1 PARTIAL; không có MISSING/BROKEN theo [ma trận truy vết](../../docs/USECASE_TRACEABILITY.md).

## Mở project

Mở project bằng Unity `6000.6.7f1`, chọn scene `Assets/_Game/Scenes/TastePassport_MVP.unity` rồi nhấn **Play**. Để tạo lại APK, dùng menu **Tools → Taste Passport → Build Android APK** trong Unity Editor có Android Build Support.
