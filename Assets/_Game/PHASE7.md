# Phase 7 — Kiểm thử và dựng bản phát hành

## Kết quả kiểm thử

- Unity `6000.6.7f1`; `dotnet build TastePassport_KGW2026.sln -c Debug` — đạt, 0 cảnh báo và 0 lỗi.
- `TastePassport.EditorTools.TastePassportSmoke.Run` — đạt trên scene MVP: tạo hồ sơ, sáu nhiệm vụ, phần thưởng và lưu tiến trình, hai hoạt động chế tác, ngày hội bảy trạm, chuyển Việt–Hàn, bưu thiếp có hoạt ảnh và khám phá tự do sau khi hoàn tất truyện.
- `TastePassport.EditorTools.TastePassportWorldSmoke.RunPhaseSixAudioChecks` — đạt: ba kênh âm lượng, tắt tiếng và giới hạn giá trị.
- `TastePassport.EditorTools.TastePassportWorldSmoke.RunAndroidGradleTempChecks` — đạt: xác nhận Unity dùng thư mục tạm của Windows cho lệnh Gradle khi Editor thoát.
- Rà soát log build và smoke cuối: không thấy `MissingReferenceException`, `NullReferenceException` hoặc thông báo script tham chiếu bị thiếu.
- Scene game được dựng là `Assets/_Game/Scenes/TastePassport_MVP.unity`. Không có Prefab độc lập trong `Assets`; các scene mẫu/template hiện có không nằm trong bản dựng MVP.

## Bản dựng

- **Windows:** `TastePassportBuild.BuildWindows` hoàn tất thành công; log `Logs/TastePassportPhase7WindowsBuildFinal.log`; tệp `Builds/Windows/TastePassport.exe`.
- Đã khởi chạy bản Windows độc lập; menu hiện tên game, tagline tiếng Việt, lời giới thiệu và nút tiếp tục mà không gặp lỗi khởi động. Chưa chơi thủ công hết một hành trình dài trên Windows.
- **Android:** `TastePassportBuild.BuildAndroid` hoàn tất thành công từ môi trường mặc định của máy; log `Logs/TastePassportPhase7AndroidBuildFinal.log`; tệp `Builds/Android/TastePassport.apk` (49.870.122 byte).
- APK có package `com.DefaultCompany.TastePassport_KGW2026`, orientation dọc, `minSdkVersion 26`, `targetSdkVersion 37`; chữ ký APK v2 được `apksigner` xác nhận.
- Lỗi loopback của Gradle trên Windows đã được khắc phục bằng cách đặt `TEMP`/`TMP` an toàn trong tiến trình Unity trước build Android và khi Editor thoát. Thay đổi chỉ áp dụng cho tiến trình Unity hiện tại, không sửa biến môi trường hệ thống.

## Giới hạn kiểm thử

- Đã cài APK bằng `adb install -r` lên AVD `TastePassportPhone`, không gỡ ứng dụng hoặc xóa dữ liệu trước đó. Emulator đồ họa phần mềm chỉ cung cấp OpenGL ES 3.0 trong khi APK yêu cầu OpenGL ES 3.1; Unity hiện cảnh báo phần cứng không đáp ứng và sau khi tiếp tục báo không khởi tạo được Graphics API.
- Đã thử renderer GPU máy chủ AMD: SurfaceFlinger báo OpenGL ES 3.1 và Activity của game khởi chạy, nhưng không thu được khung hình menu ổn định để xác nhận gameplay Android. Vì vậy chưa tính cài đặt/điều khiển/âm thanh/TTS/tiếp tục save trên Android là kiểm thử đạt. Cấu hình AVD đã được khôi phục nguyên trạng sau thử nghiệm.
- Không có điện thoại Android vật lý kết nối. Chưa có số đo FPS, CPU hoặc bộ nhớ trên thiết bị Android; không khẳng định hiệu năng mobile đã được đo.
- Windows đã build thành công, menu tiếng Việt đã được mở trong bản độc lập và gameplay smoke đã chạy trong Unity Editor; chưa thực hiện một lượt chơi thủ công dài trên Windows.

## Mở và kiểm tra

Mở project bằng Unity `6000.6.7f1`, chọn scene `Assets/_Game/Scenes/TastePassport_MVP.unity` rồi nhấn Play. Có thể dùng **Tools → Taste Passport → Build Windows MVP** để dựng Windows hoặc **Tools → Taste Passport → Build Android APK** để dựng APK dọc.
