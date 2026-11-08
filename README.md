# Dấu Xứ Việt Hàn — Tales of Two Lands

Game phiêu lưu văn hóa 2D/2.5D phong cách truyện tranh giấy thủ công, đưa người chơi khám phá Hội An và Jeonju.

## Mở game

- Unity: `6000.6.7f1`, Universal Render Pipeline.
- Scene chính: `Assets/_Game/Scenes/TastePassport_MVP.unity`.
- Mở project bằng Unity Hub, chọn scene chính rồi nhấn **Play**. Dùng Game view dọc như 9:16.
- PC dùng bàn phím/chuột; Android có điều khiển chạm.
- Bản APK mới nhất: `Builds/Android/TastePassport.apk`.

Lần đầu chơi, người chơi xem truyện mở đầu, chọn ngôn ngữ, tạo hồ sơ và nhân vật. Lần sau có thể tiếp tục từ checkpoint đã lưu.

## Nội dung

- Hai khu vực đi bộ có collision, camera follow, NPC, tương tác và nhiệm vụ: Hội An và Jeonju.
- Sáu hoạt động có thao tác, điều kiện hoàn thành và phần thưởng riêng: khám phá/lắp ghép Chùa Cầu, chế tác đèn lồng, nấu Cao Lầu, lắp mô hình Hanok, chế tác Hanji và chuẩn bị Bibimbap.
- Save cục bộ giữ tiến trình, vật phẩm, dấu ấn, bưu thiếp hai mặt, sự kiện hành trình và bố cục nhật ký. Ảnh kỷ niệm được lưu thành JPEG riêng.
- Đèn lồng và sản phẩm Hanji đã chế tác xuất hiện trong thế giới. Ngày hội mở theo tiến trình, kiểm tra vật phẩm sở hữu và lưu bố trí. Sau đoạn kết có thể tiếp tục Free Exploration và chơi lại.
- UI tiếng Việt/tiếng Hàn; có phụ đề, âm lượng nhạc/hiệu ứng/ambience/lời dẫn và Android Text-to-Speech với fallback phụ đề.

## Tài liệu dự án

- [Ma trận truy vết Use Case](docs/USECASE_TRACEABILITY.md)
- [Báo cáo kiểm thử](docs/TEST_REPORT.md)
- [Kiến trúc](docs/ARCHITECTURE.md)
- [Quyết định triển khai](docs/IMPLEMENTATION_DECISIONS.md)
- [Báo cáo giai đoạn 7](Assets/_Game/PHASE7.md)

## Kết quả và giới hạn kiểm thử

Unity GUI smoke Direct3D11 kiểm tra luồng hồ sơ, sáu hoạt động, replay không lặp thưởng, ảnh camera Hội An/Bibimbap, ngày hội, đoạn kết và khám phá tự do. C# solution build đạt 0 lỗi và 3 cảnh báo xung đột assembly. APK đã build, chữ ký hợp lệ và cài/mở được trên AVD.

AVD hiện dùng OpenGL ES 3.0 trong khi APK yêu cầu ES 3.1, nên render, input, save/load, audio và TTS trên Android chưa được nghiệm thu. Chưa có điện thoại Android vật lý để kiểm tra. Chi tiết log và giới hạn nằm trong [báo cáo kiểm thử](docs/TEST_REPORT.md).
