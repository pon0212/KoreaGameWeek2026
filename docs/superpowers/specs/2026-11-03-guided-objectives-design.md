# Thiết kế UX — Hướng dẫn bước tiếp theo

## Vấn đề hiện tại

Trang bản đồ liệt kê nhiều hoạt động nhưng không đánh dấu hoạt động nên làm trước. Trong màn đi bộ, HUD chỉ nói cách di chuyển bằng một dòng nhỏ; người chơi không thấy mục tiêu cốt truyện đang theo đuổi. Màn tạo/sửa nhân vật hiện bị giấu sau Hộ chiếu, nên người chơi có hồ sơ sẵn không biết cách mở lại.

## Mục tiêu

Luôn cho người chơi biết một việc cụ thể cần làm tiếp theo, chỉ rõ cách di chuyển và tương tác ở lần đầu vào thế giới đi bộ, mở được màn nhân vật ngay từ menu, và giữ nguyên tiến trình save hiện có.

## Phương án

1. **Thẻ mục tiêu luôn hiện và chỉ dẫn lần đầu (được chọn).** Thẻ trên bản đồ nêu một bước kế tiếp cùng nút mở hoạt động tương ứng; HUD trong thế giới đi bộ nêu đối tượng kế tiếp và làm nổi bật đối tượng đó. Một thẻ ngắn giải thích điều khiển khi vào thế giới lần đầu.
2. **Sổ nhiệm vụ riêng.** Có thể chứa nhiều mô tả hơn, nhưng buộc người chơi mở thêm màn hình để biết bước tiếp theo.
3. **Chỉ thêm hướng dẫn vào hội thoại.** Ít thay đổi giao diện nhưng chỉ giúp ở một số đoạn, không giữ mục tiêu hiện tại trước mắt người chơi.

## Thiết kế

- Trang bản đồ có một thẻ nổi bật phía trên danh sách thành phố. Thẻ hiện đúng một mục tiêu chưa hoàn thành theo thứ tự câu chuyện đã mở khóa và nút đưa thẳng đến nhiệm vụ, hoạt động chế tác hoặc ngày hội tương ứng.
- Thứ tự gợi ý dựa trên sáu nhiệm vụ chính đã có; sau đó nhắc chế tác đèn lồng/hanji còn thiếu; tiếp theo là chuẩn bị ngày hội; sau khi hoàn tất truyện thì gợi ý tiếp tục khám phá tự do.
- HUD của Hội An/Jeonju có mục tiêu riêng cho thế giới đang mở: tên NPC hoặc địa điểm cần tới, thao tác cần làm, cùng dấu hiệu trực quan trên đối tượng. Nếu không còn tương tác bắt buộc, thẻ hướng người chơi về lại bản đồ.
- Menu chính có nút **Nhân vật** ngay cạnh các lựa chọn hành trình. Nút mở trực tiếp màn tạo/sửa nhân vật; với hồ sơ cũ, tên và diện mạo đang dùng được điền sẵn, lưu lại không xóa tiến trình.
- Lần đầu mở thế giới đi bộ, hướng dẫn nổi bật chỉ đúng joystick cảm ứng và nút tương tác; trên PC dùng WASD/phím mũi tên và E/Space. Có nút đóng rõ ràng và nút trợ giúp để mở hướng dẫn lại; phần hướng dẫn không chặn chuyển động sau khi đóng.
- Nội dung có bản tiếng Việt và tiếng Hàn, dùng cỡ chữ và màu theo HUD hiện tại. Không thay đổi phần thưởng, điều kiện mở khóa hoặc cấu trúc save; trạng thái đã xem hướng dẫn dùng khóa cài đặt hiện có.

## Kiểm tra

- Hồ sơ mới nhận mục tiêu đầu tiên ở Hội An và nút đưa đến hoạt động đúng.
- Menu mở được màn nhân vật cho hồ sơ mới lẫn hồ sơ cũ; chỉnh sửa tên/diện mạo không làm mất nhiệm vụ hoặc save hiện có.
- Sau khi hoàn thành từng nhiệm vụ, mục tiêu chuyển sang nhiệm vụ kế tiếp; Jeonju không được gợi ý trước khi mở khóa.
- Thiếu sản phẩm chế tác thì mục tiêu nhắc đúng món còn thiếu; sau khi đủ điều kiện sẽ gợi ý ngày hội; sau khi hoàn thành truyện sẽ hiện khám phá tự do.
- Thế giới đi bộ làm nổi bật đúng tương tác tiếp theo; hướng dẫn tự hiện lần đầu, có thể đóng và mở lại bằng nút trợ giúp.
- Smoke xác nhận nhãn và hướng dẫn ở tiếng Việt/Hàn; kiểm tra build C# và gameplay smoke hiện có.

## Giới hạn

Đây là cải thiện hướng dẫn cho MVP 2D hiện tại; không thay đổi bối cảnh minh họa, bản đồ thế giới, điều khiển lõi hay thiết kế thành game 3D.
