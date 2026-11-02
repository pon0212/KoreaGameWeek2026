# Phase 4 — Hoàn thiện Jeonju

## Nội dung

- Thêm Bác Kim và xưởng Hanji vào Jeonju. Người chơi có thể mở xưởng từ bản đồ sau khi Jeonju được mở khóa hoặc gặp Bác Kim trong thế giới đi bộ.
- Tạo hoạt động Hanji bằng các nút phù hợp với chạm và chuột: chọn màu, dáng quạt/dấu sách/hoa giấy, họa tiết, rồi thực hiện lần lượt cắt, gấp và trang trí.
- Lưu thành phẩm, thẻ Hanji và dấu ấn mở khu trưng bày bằng bản hồ sơ tạm; chỉ thay hồ sơ đang chơi sau khi tệp lưu thành công.
- Hiển thị dáng và màu sản phẩm trong kết quả, Hành trang và khu trưng bày Jeonju. Khu trưng bày chỉ xuất hiện sau khi dấu ấn chế tác được lưu.
- Hoàn thành nhiệm vụ Hanok và trò Bibimbap mở thẻ tương ứng. Khám phá Hanok, Gyeonggijeon, Pansori và chế tác Hanji mở đủ năm thẻ văn hóa Jeonju.
- Thêm nội dung tiếng Việt và tiếng Hàn cho hoạt động, thẻ văn hóa và nguồn tham khảo. Tiếng Việt vẫn là ngôn ngữ mặc định; hồ sơ có thể chuyển sang tiếng Hàn.
- Nguồn văn hóa Jeonju được ghi trong [jeonju-cultural-sources.md](../../docs/jeonju-cultural-sources.md). Hồ sơ Hanji UNESCO được mô tả là đang xét cho năm 2026, chưa được ghi danh.

## Kiểm tra

- `dotnet build TastePassport_KGW2026.sln -c Debug` — đạt, 0 cảnh báo và 0 lỗi.
- Unity `TastePassport.EditorTools.TastePassportWorldSmoke.RunPhaseFourChecks` — đạt: lưu bộ sưu tập, nội dung Jeonju tiếng Việt/tiếng Hàn, ba sprite Hanji và trưng bày sau khi tải lại.
- Unity `TastePassport.EditorTools.TastePassportSmoke.Run` — đạt: hành trình sáu nhiệm vụ, chuyển ngôn ngữ, đủ năm thẻ Jeonju, chế tác Hanji, Hành trang và khu trưng bày lưu bền vững.

## Giới hạn hình ảnh

Màu hiện được áp nhẹ lên toàn hình; họa tiết được lưu và ghi tên trong biến thể sản phẩm, chưa vẽ thành lớp hình riêng. Ba dáng sản phẩm có minh họa riêng. Chưa kiểm tra tỉ lệ hiển thị trên thiết bị Android thực tế.
