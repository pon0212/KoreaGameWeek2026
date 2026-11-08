# Thiết kế 3D toàn diện cho Dấu Xứ Việt Hàn

## Mục tiêu

Chuyển toàn bộ trải nghiệm nhìn thấy của game sang phong cách 3D: menu, chọn ngôn ngữ/hồ sơ, bản đồ, hộ chiếu, hội thoại, thế giới đi bộ, hoạt động văn hóa, kết quả và cài đặt. Người chơi vẫn có thể đọc nội dung Việt/Hàn, chạm các nút trên Android và tiếp tục cốt truyện với save hiện có.

## Hiện trạng

- `PassportApp` dựng mọi trang bằng uGUI trên một Canvas `ScreenSpaceOverlay`.
- Hai phố đi bộ là ảnh nền 2D với player/NPC dùng `SpriteRenderer` và điều khiển `Rigidbody2D`.
- Các mini-game là UI/ảnh 2D với logic và callback đã có.
- Repo không có model 3D, rig hay animation 3D; dự án dùng Unity 6 URP, Input System và uGUI.

## Phương án được chọn

Tạo game **low-poly 3D tự chứa trong project**. Các trang UI chuyển sang Canvas World Space đặt trên bảng/thẻ/sổ hộ chiếu 3D; `GraphicRaycaster` và Input System tiếp tục nhận chạm. Hai phố có mesh kiến trúc, đạo cụ, player và NPC low-poly thật, với chuyển động đơn giản. Các hoạt động giữ nguyên luật chơi và dữ liệu nhưng được trình bày bằng bàn/đạo cụ 3D phù hợp từng hoạt động.

### Các phương án đã cân nhắc

1. **Dùng model 3D thực tế có rig từ asset pack.** Hình ảnh có thể chi tiết nhưng repo không có asset, cần chọn nguồn/license, tải và tối ưu thêm; chưa phù hợp để triển khai tự chứa.
2. **Dựng low-poly 3D bằng mesh/runtime helper trong project.** Không thêm package hoặc tải model, có thể chuyển đồng bộ mọi màn hình và giữ nguyên gameplay/save; tạo hình đơn giản hơn model nghệ thuật có sẵn. Đây là phương án được chọn.
3. **Chỉ thêm diorama 3D sau các trang UI phẳng.** Nhẹ hơn nhưng không đáp ứng yêu cầu giao diện 3D cho menu, hộ chiếu và toàn bộ game.

## Phạm vi

- Tất cả Canvas của game dùng cách trình bày 3D và hiển thị qua camera; chữ, nút và vùng chạm vẫn sắc nét, có vùng nhận chạm đủ lớn.
- Menu, bản đồ và hộ chiếu có bố cục mới gọn, đặt trên sân khấu 3D/book prop.
- Thế giới đi bộ có phố 3D, avatar player/NPC 3D và HUD 3D; chuyển động/tương tác hiện có được giữ nguyên.
- Các mini-game/kết quả/cài đặt cũng dùng stage/props 3D tương ứng; luật chơi, score, rewards và callbacks hiện có giữ nguyên.
- Không đổi save schema, mission ID, story flags, localization keys hiện có hoặc điều kiện mở khóa.

## Mục tiêu Android

Thiết kế cho Android tầm trung, giữ tối thiểu 30 FPS trong cảnh 3D; 60 FPS là mục tiêu tốt nhất trên máy đủ mạnh. Dùng ít mesh/material dùng chung, hình học gọn và không dùng shadow map thời gian thực.

## Giả định triển khai

Vì không có model 3D trong repo, avatar low-poly và đạo cụ sẽ được dựng bằng hình học đơn giản trong Unity, có animation idle/walk ngắn. UGUI vẫn dùng cho chữ và nút, nhưng Canvas đặt trong không gian 3D; đây là giao diện 3D chạm được, không phải hệ thống vật lý 3D riêng cho từng nút.

## Tiêu chí hoàn thành

- Menu, hộ chiếu, bản đồ, hội thoại, mọi hoạt động và hai thế giới đi bộ đều được trình bày trong cảnh 3D.
- Android touch vẫn mở trang, di chuyển, tương tác NPC và sử dụng điều khiển hoạt động.
- Người chơi có thể đi từ đầu đến kết thúc cốt truyện với tiến trình/profile hiện có.
- Camera, Canvas, mesh, material và render texture được giải phóng đúng lúc; không sửa các tệp dirty ngoài phạm vi.
