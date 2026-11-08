# Dấu Xứ Việt Hàn — Tales of Two Lands

Một game phiêu lưu văn hóa 2D/2.5D phong cách truyện tranh giấy thủ công, đưa người chơi đi bộ khám phá Hội An và Jeonju.

## Mở và chạy

- Engine: Unity `6000.6.7f1`, URP.
- Scene chính: `Assets/_Game/Scenes/TastePassport_MVP.unity`.
- Mở project trong Unity Hub bằng Unity `6000.6.7f1`, mở scene trên rồi nhấn **Play**. Chọn tỉ lệ Game view dọc, chẳng hạn 9:16.
- PC dùng bàn phím/chuột; Android dùng điều khiển chạm. Bản Android mới nhất đã dựng tại `Builds/Android/TastePassport.apk`.

Lần đầu chơi, người chơi chọn ngôn ngữ, tạo hồ sơ và chọn nhân vật. Những lần sau có thể tiếp tục từ checkpoint đã lưu. Save nằm cục bộ trên thiết bị; ảnh kỷ niệm được lưu thành tệp JPEG riêng và được liên kết từ Sổ Dấu Xứ.

## Nội dung đã triển khai

- Hai khu vực đi bộ có va chạm, camera follow, NPC, điểm tương tác và nhiệm vụ: Hội An và Jeonju.
- Sáu hoạt động có thao tác và điều kiện hoàn thành riêng: khám phá/lắp ghép Chùa Cầu, chế tác đèn lồng, nấu Cao Lầu, lắp Hanok, chế tác Hanji và chuẩn bị Bibimbap.
- Tiến trình, vật phẩm, dấu ấn văn hóa, bưu thiếp hai mặt, ảnh, nhật ký và bố cục trang được lưu qua các phiên.
- Đèn lồng và sản phẩm Hanji đã chế tác xuất hiện trong thế giới sau khi tải lại. Ngày hội chỉ mở theo tiến trình; gian trưng bày kiểm tra vật phẩm người chơi sở hữu. Sau đoạn kết, có thể tiếp tục khám phá và chơi lại.
- Giao diện tiếng Việt/tiếng Hàn, phụ đề, âm lượng nhạc/hiệu ứng/ambience và lời dẫn. Android gọi Text-to-Speech theo ngôn ngữ; khi không có voice, phụ đề vẫn hiển thị.

## Tài liệu

- [Ma trận truy vết Use Case](docs/USECASE_TRACEABILITY.md)
- [Báo cáo kiểm thử](docs/TEST_REPORT.md)
- [Kiến trúc hiện tại](docs/ARCHITECTURE.md)
- [Quyết định triển khai](docs/IMPLEMENTATION_DECISIONS.md)
- [Báo cáo giai đoạn 7](Assets/_Game/PHASE7.md)

## Trạng thái kiểm thử

Ngày 2026-11-09, Unity GUI smoke trên Direct3D11 và các smoke theo từng phân hệ đạt; solution C# build được với 0 lỗi và 3 cảnh báo xung đột assembly của .NET/Unity. APK Android tạo thành công và chữ ký được xác nhận. AVD hiện chỉ cung cấp OpenGL ES 3.0 trong khi APK khai báo ES 3.1, nên chưa nghiệm thu hình ảnh/gameplay/TTS trên Android. Không có thiết bị Android vật lý trong lần kiểm tra này. Chi tiết và log được liệt kê trong [báo cáo kiểm thử](docs/TEST_REPORT.md).
