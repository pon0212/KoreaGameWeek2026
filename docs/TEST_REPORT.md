# Báo cáo kiểm thử — 2026-11-09

## Kết quả đạt

| Kiểm tra | Phương thức | Kết quả |
|---|---|---|
| C# solution build | `dotnet build TastePassport_KGW2026.sln -c Debug --no-restore -p:FrameworkPathOverride=<Unity>/Editor/Data/MonoBleedingEdge/lib/mono/4.7.1-api` | Thành công, 0 lỗi, 3 cảnh báo MSB3277 về xung đột assembly Unity/.NET |
| World session | `TastePassportWorldSmoke.RunWorldSessionChecks` | PASS: truyện đầu, nút bỏ qua VI/KO chỉ ở lần đầu, save được giữ, hội thoại lặp, Chùa Cầu, save lỗi, camera/checkpoint |
| Persistence/architecture | `TastePassportWorldSmoke.RunArchitectureAndPersistenceChecks` | PASS: puzzle kiến trúc riêng, JPEG bền vững, timeline, migration v3, journal layout/vật phẩm sở hữu, festival, narration fallback và World HUD |
| Phase 4 — Jeonju/Hanji | `TastePassportWorldSmoke.RunPhaseFourChecks` | PASS: thẻ văn hóa, lắp Hanok, ba sản phẩm Hanji, thứ tự thao tác và display sau reload |
| Phase 5 — ngày hội/hậu truyện | `TastePassportWorldSmoke.RunPhaseFiveChecks` | PASS: bốn chương, khóa mở đúng điều kiện, 7 trạm, 3 liên kết, idempotent completion, save/reload và Free Exploration |
| Phase 6 — audio | `TastePassportWorldSmoke.RunPhaseSixAudioChecks` | PASS: âm lượng nhạc/SFX/ambience, mute, giới hạn giá trị; narration settings/fallback cũng pass trong architecture smoke |
| Full runtime GUI smoke | `TastePassport.EditorTools.TastePassportSmoke.Run`, Unity 6000.6.7f1, Direct3D11 | PASS: tạo hồ sơ, di chuyển/collision/camera follow, cả hai world, sáu hoạt động, replay không cộng xu, craft, ảnh camera Hội An và Bibimbap, festival, kết thúc và khám phá tự do |
| Android package build | `TastePassportBuild.BuildAndroid` bằng Editor 6000.6.7f1 có Android Build Support | PASS, Exit 0 |
| Android package inspection | `aapt dump badging/xmltree`, `apksigner verify` | Package `com.DefaultCompany.TastePassport_KGW2026`, portrait, min SDK 26, target SDK 37, OpenGL ES 3.1, chữ ký APK v2 hợp lệ |
| AVD install/launch | `adb install -r`, `adb shell monkey` trên `TastePassportPhone` | Cài thành công; activity mở và process còn chạy sau 5 giây |

Log chính nằm ở `Logs/taste-passport-gui-smoke-final.log` (lần chạy GUI mới nhất), `Logs/taste-passport-gui-smoke-retry.log`, `Logs/taste-passport-runtime-final.log`, `Logs/taste-passport-architecture-final.log`, `Logs/taste-passport-phase4-final.log`, `Logs/taste-passport-phase5-final.log`, `Logs/taste-passport-phase6audio-final.log` và `Logs/taste-passport-android-build-final.log`.

GUI smoke xác nhận ảnh chụp thật được ghi thành JPEG ngoài JSON, record và event `photo-captured` được nạp lại từ save. Ảnh Hội An và Bibimbap được tạo trong profile QA tạm; driver xóa các file QA sau khi kết thúc. Smoke no-graphics được dùng để xác nhận logic, không được tính là kiểm tra render hoặc camera.

## Android — giới hạn thực tế

- APK mới: `Builds/Android/TastePassport.apk`, **50,199,086 byte**, được build ngày 2026-11-09.
- AVD báo `ro.opengles.version=196608` (OpenGL ES 3.0); manifest APK yêu cầu `0x30001` (OpenGL ES 3.1). Vì vậy chưa xác nhận được hình ảnh, điều khiển, audio, TTS hoặc lưu/tiếp tục trên Android.
- Trong logcat xuất hiện `ClassNotFoundException: com.google.android.play.core.assetpacks.AssetPackManager`. App process còn sống sau 5 giây, nhưng lỗi class này chưa được xác định nguyên nhân trên thiết bị tương thích.
- Không có điện thoại Android vật lý kết nối. Không khẳng định hiệu năng mobile hoặc TTS Android đã được nghiệm thu.

## Lưu ý môi trường và phạm vi

- .NET SDK trên máy không cài reference pack 4.7.1 toàn cục; build dùng bộ API 4.7.1 đi kèm Unity qua `FrameworkPathOverride`. Ba cảnh báo còn lại là MSB3277 reference-version conflicts; Unity Editor compile và player build thành công.
- Có hai cài đặt Unity cùng phiên bản: `6000.6.7f1-x86_64` dùng để chạy GUI/PC smoke; `6000.6.7f1` có Android Build Support, SDK/NDK và OpenJDK để dựng APK.
- `Builds/Windows/TastePassport.exe` cũ hơn mã hiện tại; PC được kiểm tra trong Unity Editor, chưa dựng lại standalone Windows trong đợt này.
- UI preview 720×1280 được rà theo phong cách giấy kem/tranh màu nước trước lần bổ sung nút bỏ qua. Nút mới được kiểm tra bằng world-session smoke và layout/safe-area checks; chưa có màn hình Android thật để duyệt mỹ thuật.
- Một lần GUI smoke đầu tiên bị lệch về menu sau Editor reimport khi driver chờ form hồ sơ. Driver đã được bổ sung retry; lần chạy Direct3D11 cuối cùng hoàn tất PASS.
