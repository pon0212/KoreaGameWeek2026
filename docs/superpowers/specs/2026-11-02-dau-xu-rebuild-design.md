# Thiết kế: DẤU XỨ VIỆT HÀN – TALES OF TWO LANDS

## Ý định

Chuyển Unity MVP hiện có thành game phiêu lưu văn hóa Việt–Hàn có thể chơi và tiếp tục sau cốt truyện. Game hướng đến người chơi Android, có bản Windows để trình diễn, dùng tiếng Việt mặc định và hỗ trợ tiếng Hàn. Trọng tâm là những chuyến khám phá có nhân vật, nhiệm vụ và dấu ấn còn lại trong thế giới.

## Mục tiêu

- Giữ tên game **DẤU XỨ VIỆT HÀN – TALES OF TWO LANDS**, bốn chương cốt truyện, Nhung và Ga Bo (가보), Hội An và Jeonju.
- Giữ lại Unity scene, sáu vòng mini-game, save, localization, font và artwork đang dùng tốt.
- Bổ sung di chuyển tự do, camera theo người chơi, tương tác nhân vật và mốc khám phá trong từng thế giới.
- Lưu vị trí, chương, đối thoại đã gặp và dấu ấn; thay đổi vẫn tồn tại sau khi tải lại.
- Kết nối các hoạt động với hộ chiếu và giữ chế độ khám phá sau khi cốt truyện chính hoàn thành.
- Nâng dần sáu hoạt động thành Chùa Cầu, đèn lồng, Cao Lầu, Hanok, Hanji và Bibimbap; thêm nhiệm vụ ngày hội Việt–Hàn.

## Không thuộc mục tiêu

- Không thay Unity project, đổi phiên bản Unity hoặc nâng package.
- Không thêm backend, đăng nhập, phần thưởng trả phí, FOMO hay multiplayer.
- Không xóa scene, prefab, tài nguyên hoặc những thay đổi đang có của chủ dự án.
- Không tuyên bố build Android hay kiểm tra thiết bị nếu môi trường chưa có hỗ trợ tương ứng.

## Các phương án đã cân nhắc

1. **Viết lại thành nhiều scene và kiến trúc mới.** Dễ chia vùng chơi về lâu dài nhưng phải thay nhiều cấu hình, rủi ro làm hỏng save và sáu vòng chơi hiện có; không phù hợp với hạn nộp 11/11/2026.
2. **Chỉ đổi nội dung và giao diện trong scene hiện tại.** Rủi ro thấp và tận dụng được toàn bộ MVP, nhưng không đáp ứng yêu cầu điều khiển nhân vật, va chạm, camera và tương tác trong thế giới.
3. **Mở rộng tăng dần từ scene hiện tại.** Giữ entry scene, UI, save và gameplay domain; thêm lớp thế giới 2D có thể đi lại, migrate save, rồi nối các nhiệm vụ và chương theo từng phase. Đây là phương án được chọn theo chỉ dẫn bắt đầu ngay và giữ lại công việc đang hoạt động.

## Kiến trúc và quyết định triển khai

- Giữ `Assets/_Game/Scenes/TastePassport_MVP.unity` làm entry scene và giữ cấu trúc single-scene trước mắt. Scene hiện đã có `PassportApp`, `Canvas`, `EventSystem` và camera orthographic.
- Giữ các vòng khám phá, nấu ăn và ghép văn hóa; thay tên, hội thoại và quan hệ tiến trình để phục vụ câu chuyện mới.
- Thêm module `Scripts/World` cho dữ liệu địa điểm, controller người chơi, đối tượng tương tác và phiên khám phá. Dùng bàn phím trên Windows và nút cảm ứng/joystick trên Android.
- Dùng tranh Hội An và Jeonju hiện có làm nền riêng cho hai khu vực; nhân vật đồng hành và người chơi là sprite gốc, không ghép chung hai quốc gia thành một bản đồ đổi màu.
- Nâng hồ sơ từ schema 1 lên schema 2 có migration không mất dữ liệu. Lưu chương, cờ truyện, dấu ấn, khu vực và tọa độ.
- Tiếng Việt là ngôn ngữ mới mặc định; lựa chọn tiếng Hàn vẫn được lưu trong cài đặt. Nhung và Ga Bo luôn là NPC đồng hành, người chơi là nhân vật được điều khiển.
- Làm theo thứ tự lát cắt chơi được trước: menu/hồ sơ → Hội An đi lại và gặp Nhung → lưu dấu ấn → Jeonju với Ga Bo → nối các mini-game và bốn chương → polish và build.

## Ràng buộc

- Unity `6000.6.7f1`, URP 2D, uGUI và Input System đã cài trong project.
- Android là nền tảng chính; Windows dùng cho trình diễn.
- Không thay đổi `Packages/manifest.json` hoặc Unity version nếu không có yêu cầu mới.
- Working tree hiện có thay đổi chưa commit ở Android TTS, `UiFeedbackSound`, art metadata, `.utmp/` và `Assets/Editor/`; các nội dung này thuộc chủ dự án và phải được giữ nguyên, không đưa vào commit của phần rebuild.
- Unity `6000.6.7f1` đã cài tại `D:\UnityEditors\6000.6.7f1\Editor`; thư mục Android Build Support, SDK, NDK và OpenJDK đều có. Vẫn cần xác minh build APK và thiết bị thực tế trước khi báo đạt.

## Phạm vi lát cắt đầu tiên

Lát cắt đầu tiên sẽ thêm một phiên khám phá đi bộ trong từng bối cảnh, điều khiển bàn phím/cảm ứng, camera theo nhân vật, vùng va chạm, nút tương tác, hội thoại Nhung/Ga Bo, một dấu ấn được lưu và vị trí tiếp tục. Nó mở từ bản đồ hiện tại và đóng về lại UI hiện tại. Các nhiệm vụ thủ công chuyên biệt, Chùa Cầu đầy đủ, bốn chương và ngày hội tiếp tục ở các phase tiếp theo; không được xem là đã hoàn thành chỉ vì nền tảng này hoạt động.

## Việc còn lại cho người triển khai

Thực hiện migration save có kiểm tra dữ liệu v1/v2; tích hợp controller và đối tượng tương tác vào entry scene mà không sửa thủ công scene đang dùng; chuyển nội dung thành Nhung/Ga Bo và tiêu đề mới; kiểm chứng compile, lưu/tải và smoke gameplay bằng môi trường Unity nếu có. Sau đó tiếp tục các mini-game và cốt truyện từng phần.
