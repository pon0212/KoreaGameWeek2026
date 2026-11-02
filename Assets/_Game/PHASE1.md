# DẤU XỨ VIỆT HÀN – Báo cáo giai đoạn nền tảng

**Tên tiếng Anh:** TALES OF TWO LANDS
**Tagline:** Mỗi vùng đất một câu chuyện – Mỗi hành trình một dấu ấn.
**Cập nhật:** 02/11/2026

## Mở và kiểm tra trong Unity

Mở project bằng Unity **6000.6.7f1**, mở Scene `Assets/_Game/Scenes/TastePassport_MVP.unity` rồi nhấn **Play**. Tạo hồ sơ hoặc tiếp tục hồ sơ đã lưu. Giao diện mặc định là tiếng Việt; có thể đổi sang tiếng Hàn tại trang Ngôn ngữ hoặc Cài đặt.

Từ Bản đồ, chọn **Đi bộ khám phá Hội An**. Dùng cần gạt cảm ứng hoặc WASD/phím mũi tên để di chuyển; tiến lại gần Nhung hoặc dấu địa điểm rồi chọn **Tương tác** (phím E/Space trên máy tính). Nút **Về bản đồ** hoặc Escape sẽ lưu vị trí và quay lại bản đồ. Jeonju mở sau khi hoàn thành ba mốc chính Hội An: kiến trúc, Cao Lầu và giao lưu văn hóa.

Hồ sơ offline nằm tại `Application.persistentDataPath/taste_passport_profile.json`; bản sao dự phòng dùng đuôi `.bak`.

## Đã triển khai trong giai đoạn này

- Thương hiệu DẤU XỨ VIỆT HÀN, tagline đã chốt, tiếng Việt mặc định và lựa chọn tiếng Hàn.
- Hai khu vực đi bộ Hội An và Jeonju dùng ảnh nền riêng; người chơi di chuyển bằng bàn phím hoặc cần gạt cảm ứng.
- Nhung, Ga Bo (가보), Chùa Cầu và điểm khám phá Hanok có tương tác; lời thoại đầu tiên và lời thoại khi quay lại khác nhau.
- Hồ sơ schema 2 lưu chương, cờ cốt truyện, dấu ấn thế giới và checkpoint. Tọa độ khi tải được giới hạn trong bản đồ; camera cũ được khôi phục khi thoát.
- Bản đồ có điều kiện mở khóa Jeonju; Hộ chiếu hiển thị chương hiện tại và các dấu ấn đã lưu.
- Sáu vòng nhiệm vụ và ba bưu thiếp hai mặt vốn có vẫn còn dùng được. Thứ tự mở khóa nay đi từ các mốc Hội An sang Jeonju; hoàn tất nhiệm vụ lưu dấu vào hồ sơ.
- Đã thêm ba hình nhân vật PNG nền trong suốt cho người chơi, Nhung và Ga Bo. Nguồn tạo và giới hạn được ghi trong `Assets/_Game/Resources/Art/ASSET_PROVENANCE.md`.

## Chưa hoàn thành trong bản thiết kế tổng thể

Đây là nền tảng chơi được, chưa phải toàn bộ game cuối cùng. Các phần sau vẫn cần phát triển:

- Hoàn chỉnh đủ bốn chương, các đoạn chuyển cảnh và ngày hội giao lưu Chương 4; hiện trò chơi mở đầu tại Hội An và mở Jeonju theo mốc nhiệm vụ.
- Các hoạt động riêng cho làm đèn lồng, Hanji, Bài Chòi, Gyeonggijeon, Pansori, chợ, nghề thủ công và nhiệm vụ phụ. Sáu vòng hiện có vẫn là giải đố khám phá, chế biến công thức giản lược và ghép địa điểm.
- Môi trường đi bộ chi tiết hơn với kiến trúc và nhiều điểm đến tương tác, cùng hoạt ảnh nhân vật, hiệu ứng, nhạc và âm thanh môi trường.
- Hoàn thiện túi đồ, bộ sưu tập sản phẩm, sắp đặt gian hội và hệ thống nhiệm vụ cho toàn bộ cốt truyện.
- Chưa build phiên bản Windows/Android mới trong giai đoạn này và chưa thử trên thiết bị thật. Máy hiện có Unity Android Build Support, SDK/NDK và OpenJDK; điều đó chưa đồng nghĩa đã kiểm thử Android.

## Scene, asset và âm thanh

- Scene/prefab: không chỉnh sửa; vẫn dùng `TastePassport_MVP.unity` làm Scene khởi chạy. Thế giới đi bộ và HUD được tạo lúc chạy bằng code.
- Asset mới: `Traveler.png`, `Nhung.png`, `GaBo.png` cùng metadata Unity. Hai ảnh nền Hội An/Jeonju hiện có được giữ riêng.
- Âm thanh mới: chưa tạo trong giai đoạn này.
- Các thay đổi chưa commit có trước của người dùng được giữ nguyên và không đưa vào phần triển khai này.

## Kiểm thử đã chạy

- `dotnet build Assembly-CSharp.csproj`: 0 warning, 0 error.
- `dotnet build Assembly-CSharp-Editor.csproj`: 0 warning, 0 error.
- Unity batch `RunProfileAndStoryChecks`: đạt — chuyển schema 1, giữ tiến độ cũ, lưu và tải lại dấu ấn.
- Unity batch `RunWorldBoundsChecks`: đạt — giới hạn tọa độ và kiểm tra ảnh nền hai điểm đến khác nhau.
- Unity batch `RunWorldSessionChecks`: đạt — lời thoại đầu/lặp, dấu không bị nhân đôi, nút Việt/Hàn, lưu lúc pause/quit, khôi phục camera và checkpoint.
- Unity batch `TastePassportSmoke.Run`: đạt — mặc định tiếng Việt, bản đồ và hai lần vào thế giới, mở khóa Jeonju, sáu nhiệm vụ, tiến độ lưu lại, ba bưu thiếp và chuyển sang tiếng Hàn.

Các smoke test gọi trực tiếp callback của nút; chưa thay thế kiểm tra chạm/kéo trên điện thoại, quan sát bố cục trên nhiều thiết bị, chạy game bằng tay hoặc kiểm tra build phát hành.
