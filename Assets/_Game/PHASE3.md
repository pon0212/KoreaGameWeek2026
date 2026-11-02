# DẤU XỨ VIỆT HÀN — Báo cáo Phase 3: Chùa Cầu, Cao Lầu và đèn lồng

**Cập nhật:** 03/11/2026
**Ngôn ngữ mặc định:** Tiếng Việt; vẫn có thể chuyển sang tiếng Hàn.

## Đã triển khai

- Nhiệm vụ kiến trúc Hội An nay xoay quanh sáu chi tiết quan sát ở Chùa Cầu: mái ngói, lối đi có mái che, ngói âm dương, miếu thờ, kết cấu gỗ và biển tên Lai Viễn Kiều.
- Khám phá Chùa Cầu trong thế giới đi bộ hoặc hoàn tất nhiệm vụ kiến trúc đều mở thẻ văn hóa Chùa Cầu. Thẻ ghi riêng mốc đầu thế kỷ XVII, năm 1653 và năm 1719; chuyện Namazu được gọi rõ là truyền thuyết dân gian.
- Hoàn tất mini-game Cao Lầu mở thẻ văn hóa món ăn, nêu sợi mì, thịt xá xíu, rau sống và bánh ram; thẻ giải thích trò chơi đã giản lược quy trình và liên kết nguồn tham khảo Hội An.
- Cô Lan là NPC hướng dẫn nghề đèn. Người chơi chọn một trong ba dáng khung và bốn màu, sau đó kéo thả tấm vải và tua rua vào đúng ô; trên màn hình cảm ứng có thể chạm vật liệu rồi chạm ô.
- Sản phẩm dùng ID biến thể có lưu dáng/màu, mở thẻ nghề đèn, thêm dấu mở khu trưng bày, hiện trong Hành trang và xuất hiện lại ở Hội An sau khi tải hồ sơ.
- Hộ chiếu mở được Hành trang và Thẻ văn hóa. Hồ sơ schema 3 thêm hai danh sách mới; dữ liệu schema 1/2 được migration và giữ tiến độ trước đó.
- Các nội dung lịch sử và nghề thủ công được diễn giải từ nguồn của Trung tâm Quản lý Bảo tồn Di sản Văn hóa Hội An; nguồn đầy đủ nằm tại [docs/cultural-content-sources.md](../../docs/cultural-content-sources.md).

## Kiểm tra

- `dotnet build TastePassport_KGW2026.sln -c Debug`: thành công, 0 cảnh báo và 0 lỗi.
- Unity `RunPhaseThreeChecks`: đạt — migration schema 1/2, thẻ/vật phẩm, giới hạn túi đồ, logic ghép vật liệu, kéo thả chấp nhận/từ chối, nhãn Việt/Hàn, vị trí đèn trong thế giới và metadata sprite.
- Unity `TastePassportSmoke.Run`: đạt — luồng tạo hồ sơ, sáu nhiệm vụ, phát lại Hội An, bưu thiếp, đổi Việt↔Hàn, mở thẻ Cao Lầu sau nhiệm vụ, làm đèn từ giao diện, mở lại biến thể trong Hành trang và ba thẻ văn hóa.
- Kịch bản lưu có chủ đích khi đường dẫn sai ghi thông báo lỗi; các dòng lỗi lưu đó thuộc bài kiểm tra thất bại giao dịch, không phải lỗi của smoke run.

## Giới hạn còn lại

- Hình đèn là một sprite minh họa dùng chung; màu áp dụng tint nhẹ toàn ảnh và lựa chọn dáng được lưu/hiển thị bằng nhãn, nhưng khung chưa được vẽ lại theo từng hình. Chi tiết này được ghi ở `Resources/Art/ASSET_PROVENANCE.md`.
- Đã chạy smoke Unity headless, chưa kiểm tra thao tác kéo thả thật trên điện thoại hoặc nhiều tỷ lệ màn hình.
- Đây chỉ là Phase 3 của kế hoạch rebuild. Các nghề và chương Jeonju, Chương 4, hoạt động Bài Chòi/Pansori/Hanji, nhạc/âm thanh môi trường, hoàn thiện ngày hội và build phát hành vẫn còn ở các giai đoạn sau.
