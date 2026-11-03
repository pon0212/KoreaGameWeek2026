# Thiết kế UI/UX — Khám phá và hội thoại NPC

## Ý định

Làm mới toàn bộ giao diện *Dấu Xứ Việt Hàn — Tales of Two Lands* để người chơi mới hiểu cách bắt đầu, biết mục tiêu kế tiếp và dễ trò chuyện với nhân vật trong thế giới đi bộ. Game vẫn là chuyến phiêu lưu văn hóa 2D, ưu tiên điện thoại Android màn hình dọc; thao tác bàn phím/chuột trên Windows tiếp tục được hỗ trợ.

## Bối cảnh hiện tại

- `PassportApp` dựng menu, chọn ngôn ngữ, tạo hồ sơ, bản đồ, hộ chiếu, hoạt động, kết quả, bộ sưu tập và cài đặt bằng uGUI lúc chạy.
- `WalkableWorldSession` dựng HUD đi bộ, cần điều khiển cảm ứng, mục tiêu, sổ nhiệm vụ, hướng dẫn và khung thoại.
- Người chơi có thể đi đến gần NPC, bấm nút tương tác và xem lời thoại đầu tiên/lặp lại. Cô Lan và Bác Kim có thể chuyển tiếp sang hoạt động chế tác.
- `WorldCatalog`, `StoryProgressService`, hồ sơ lưu cục bộ và `GameText` hiện giữ NPC, cờ truyện, tiến trình, bản dịch Việt–Hàn.
- HUD hiện có cần gạt, nút thao tác và khung thoại cơ bản; menu/bản đồ là các trang cuộn dài. Thiết kế mới phải làm rõ bước kế tiếp và vai trò các nút trên màn hình nhỏ.

## Mục tiêu

1. Dùng một ngôn ngữ hình ảnh và quy tắc điều hướng nhất quán từ menu đến kết thúc chương.
2. Cho người chơi biết rõ đang ở đâu, cần làm gì tiếp theo và cách thực hiện.
3. Làm NPC dễ nhận ra, dễ tiếp cận và có hội thoại nhiều hành động hơn một nút “Tương tác”.
4. Giữ chữ tiếng Việt/Hàn đọc rõ, điều khiển cảm ứng đủ lớn, hỗ trợ safe area và bố cục dọc.
5. Giữ nguyên sáu vòng nhiệm vụ, hệ thống phần thưởng, nội dung truyện chính và tiến trình save hiện có.

## Ngoài phạm vi và điều cần tránh

- Không chuyển game thành visual novel thuần hoặc bỏ tự do đi bộ.
- Không thêm quan hệ thân thiết, lựa chọn làm đổi kết thúc, nhánh truyện, điều kiện thưởng mới hay dịch vụ mạng.
- Không đổi save schema hoặc xóa/chuyển đổi tiến trình cũ.
- Không dựa riêng vào màu sắc để báo trạng thái; không đặt vùng chạm quan trọng quá sát mép hoặc dưới vùng che của điện thoại.
- Không thêm sự kiện/văn hóa mới chưa được kiểm chứng nguồn chỉ để lấp chỗ cho hội thoại.

## Phương án đã cân nhắc

1. **Khám phá + hội thoại rõ ràng (được chọn):** làm mới tất cả trang và lớp HUD, giữ di chuyển tự do, thêm cuộc hội thoại dạng thẻ nhân vật với lựa chọn hỏi thêm và bắt đầu hoạt động. Các lựa chọn không làm rẽ nhánh truyện. Đây là hướng người dùng đã chọn vì cải thiện tương tác mà không đòi thay đổi save và vòng chơi.
2. **Visual novel tương tác:** hội thoại toàn màn hình, nhịp kể và lựa chọn hỏi đáp chiếm trọng tâm. Hấp dẫn khi đọc truyện nhưng làm mờ phần khám phá, cần viết thêm nhiều đoạn thoại.
3. **Truyện phân nhánh:** lựa chọn đổi quan hệ, nhiệm vụ và kết thúc. Tạo quyền quyết định mạnh nhưng đòi thêm nội dung, trạng thái lưu, QA và cân bằng truyện; không phù hợp mục tiêu giữ cốt truyện tuyến tính hiện có.

## Hướng hình ảnh và tương tác

- Giữ cảm giác sổ hành trình giấy thủ công Việt–Hàn nhưng tăng độ gọn: nền giấy sáng, chữ mực đậm, xanh ngọc làm hành động chính, đỏ son/vàng đèn làm dấu nhấn; dùng một hệ bo góc, viền, khoảng cách và cấp chữ chung.
- Ưu tiên tên màn hình, mục tiêu và một hành động chính; đẩy nội dung phụ vào tab hoặc thẻ thu gọn thay vì xếp mọi nút theo một cột dài.
- Dùng chân dung nhân vật, tên người nói và nội dung ngắn theo từng lượt; nút có cả biểu tượng lẫn nhãn. Bố cục phải co giãn theo safe area và tỷ lệ dọc.
- Duy trì ngôn ngữ đang chọn (Việt/Hàn), âm đọc nếu thiết bị hỗ trợ, phản hồi âm thanh hiện có và cách lưu hồ sơ hiện tại.

### Sơ đồ màn hình đi bộ và hội thoại

```text
┌ Chương · Hội An                  [Hộ chiếu] [Tạm dừng]
│ Mục tiêu: Gặp Nhung                         [Nhiệm vụ]
│             KHÔNG GIAN KHÁM PHÁ
│   Nhân vật / địa điểm có dấu chỉ đường
│
│ [Cần gạt di chuyển]            [Nói chuyện với Nhung]
└──────────────────────────────────────────────┘

Khi bấm nói chuyện:
┌ Chân dung │ Nhung                  [Đóng]
│           │ “Mừng bạn đến Hội An...”
│           │ [▶ Nghe]                 [Tiếp tục]
│ [Hỏi thêm về Hội An] [Bắt đầu hoạt động cùng cô Lan]
└──────────────────────────────────────────────┘
```

### Luồng NPC

1. Khi người chơi chưa ở gần, HUD giữ lời nhắc mục tiêu và dấu chỉ đường trên NPC/địa điểm cần đến.
2. Khi ở trong bán kính tương tác, nút ngữ cảnh hiện tên đích đến, ví dụ **“Nói chuyện với Nhung”**, **“Khám phá Chùa Cầu”** hoặc **“Xem đèn lồng”**. Khi không có đích gần, nút không tranh chỗ với điều khiển.
3. Khi mở hội thoại, khóa di chuyển để tránh vừa đi vừa chạm nút thoại. Thẻ hội thoại hiển thị chân dung (nếu có), tên, lời thoại, nút nghe, tiếp tục và đóng.
4. Mục **“Hỏi thêm”** hiện thêm lời thoại/văn bản văn hóa tùy nhân vật, rồi quay về thẻ hội thoại. Nội dung là tùy chọn, không đổi tiến trình truyện hay thưởng.
5. NPC có hoạt động hiển thị CTA riêng với tên hoạt động, chẳng hạn **“Làm đèn lồng cùng cô Lan”**. Chỉ mở hoạt động khi người chơi chủ động chọn CTA. Khi trở lại làng, mục tiêu và tiến trình được giữ như hiện nay.
6. Với bản đồ/đồ vật không phải NPC, dùng thẻ khám phá ngắn thay cho chân dung; vẫn có nút đóng và cách tiếp tục rõ ràng.

## Phạm vi các trang

- **Menu và làm quen:** nhận diện game, tiếp tục/bắt đầu, ngôn ngữ, nhân vật, hộ chiếu và cài đặt. Lần chơi đầu có chỉ dẫn ngắn; không yêu cầu người chơi tự đoán phải bấm gì.
- **Bản đồ:** hai vùng Hội An/Jeonju, điều kiện mở khóa dễ hiểu, tiến độ dấu ấn và một thẻ “Bước tiếp theo” nổi bật. Nội dung vùng được nhóm theo khám phá, ẩm thực và giao lưu; nhiệm vụ khóa phải giải thích điều kiện mở.
- **Thế giới đi bộ:** HUD mục tiêu rút gọn, nút nhiệm vụ/hộ chiếu, hướng dẫn lần đầu có thể mở lại, cần gạt bên trái và nút ngữ cảnh bên phải; hội thoại không che nút cốt yếu hơn mức cần thiết.
- **Các hoạt động:** ghép mảnh, nấu ăn, ghép văn hóa, làm đèn lồng, làm Hanji và ngày hội dùng chung thứ bậc tiêu đề, hướng dẫn, tiến độ, xác nhận và hành động chính; mỗi hoạt động giữ cách chơi lõi riêng.
- **Kết quả và sổ hành trình:** làm rõ vật phẩm/con dấu nhận được, điểm/sao và bước kế tiếp. Hộ chiếu, kho đồ, thẻ văn hóa, bưu thiếp, tủ đồ và cài đặt dùng chung phong cách, nhãn trạng thái và điều hướng trở lại.

## Tương thích và giới hạn kỹ thuật

- Tiếp tục Unity 6.6.7f1, scene hiện có, uGUI/EventSystem, font và ảnh trong dự án; không cài package mới.
- Không đổi `PlayerProfile`, `ProfileStore`, các ID nhiệm vụ, cờ truyện, dấu ấn hay điều kiện mở khóa. Trạng thái đóng/mở bảng hỏi thêm chỉ tồn tại trong phiên UI.
- Bản địa hóa mọi nhãn và hội thoại mới qua `GameText` cho tiếng Việt và tiếng Hàn; tiếng Việt vẫn là mặc định/fallback.
- Hỗ trợ Android portrait và Windows keyboard/mouse. Nút đóng/back không được làm mất tiến trình; ESC/Backspace tiếp tục trả về an toàn.
- Có các thay đổi chưa commit trong workspace trước khi bắt đầu task này. Khi triển khai, chỉ chạm tệp cần cho UI và giữ nguyên các thay đổi không liên quan.

## Tiêu chí chấp nhận

- Người chơi mới tìm được đường từ menu đến thế giới mà không cần đoán điểm bắt đầu; hướng dẫn điều khiển nêu rõ cách đi, tương tác và mở lại trợ giúp.
- Trên cả hai ngôn ngữ, người chơi nhận biết được NPC/đích đang cần đến, biết khi nào có thể tương tác và nút sẽ làm gì.
- Hội thoại có tên/ảnh người nói khi sẵn có, chữ đọc được trên màn hình dọc, đóng/tiếp tục được; người chơi có thể hỏi thêm hoặc bắt đầu hoạt động nếu lựa chọn đó được hỗ trợ.
- Tương tác Nhung, Cô Lan, Ga Bo, Bác Kim, landmark và vật phẩm tiếp tục ghi cùng cờ/dấu ấn hiện có; nhiệm vụ đúng vẫn được mở theo thứ tự hiện tại.
- Có thể đi hết sáu hoạt động, mở hộ chiếu/bản đồ/kho đồ/cài đặt rồi quay lại mà không mất tiến trình hoặc gặp nhãn điều khiển Việt/Hàn lẫn nhau.
- Bố cục không đặt CTA quan trọng ngoài safe area hoặc chồng lên cần gạt; mục tiêu, trạng thái khóa và hành động chính không chỉ phân biệt bằng màu.

## Phần để người triển khai quyết định

Có thể tách component giao diện dùng chung khỏi `PassportApp`/`WalkableWorldSession` nếu làm giảm trùng lặp mà không mở rộng phạm vi thành tái cấu trúc gameplay. Chi tiết chuyển động, hoạt ảnh nhẹ, thứ tự tab và cách sắp xếp trên desktop được chốt trong lúc triển khai miễn các tiêu chí chấp nhận và nội dung đã chọn được giữ nguyên.
