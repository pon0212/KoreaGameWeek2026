# Kiến trúc hiện tại

## Nền tảng và điểm vào

- Unity `6000.6.7f1`, C# và Universal Render Pipeline; scene vào game là `Assets/_Game/Scenes/TastePassport_MVP.unity`.
- `PassportApp` điều phối menu, hồ sơ, bản đồ, Sổ Dấu Xứ, hoạt động, cài đặt, ngày hội và đoạn kết.
- UI được dựng bằng runtime code. `WalkableWorldSession` cùng `WorldDioramaBuilder` tạo hai khu vực đi bộ, nhân vật, NPC, địa điểm, collision, camera và các vật thể thay đổi theo tiến trình đã lưu.

## Gameplay

- `WorldCatalog` và `MapObjectiveService` mô tả Hội An/Jeonju, cổng mở khóa và mục tiêu.
- `WorldInteractable`, `WorldPlayerController` và `WalkableWorldSession` xử lý điều khiển, phạm vi tương tác, hội thoại và checkpoint.
- Sáu hoạt động dùng state machine riêng: `ExplorationRun`, `LanternCraftRun`, `RecipeRun` và `HanjiCraftRun`. Các nút chạm/kéo thả trong `PassportApp` cập nhật state; điều kiện thắng được xác định từ thao tác và dữ liệu hoạt động.
- `FestivalPreparationRun` kiểm tra điều kiện mở ngày hội, vật phẩm sở hữu và các lựa chọn của từng trạm. Hoàn tất truyện không khóa hai bản đồ hoặc replay.
- `StoryProgressService`, `ProgressService` và `InventoryService` cập nhật dấu ấn, nhiệm vụ, vật phẩm, phần thưởng và lịch sử. Phần thưởng completion được chống nhận lặp.

## Save, nhật ký và ảnh

- `PlayerProfile` là schema save cục bộ có version; `ProfileStore` chuẩn hóa dữ liệu, ghi an toàn và phục hồi từ backup.
- Kết quả hoạt động được ghi vào candidate profile và chỉ thay profile đang chạy sau khi lưu thành công.
- `MemoryPhotoService` lưu ảnh chụp JPEG riêng, còn profile giữ metadata/đường dẫn và timeline event. Gallery, postcard, dấu ấn và layout trang nhật ký đọc từ save.
- Khi dựng lại world, `WorldDioramaBuilder` dùng dấu ấn và biến thể vật phẩm đã sở hữu để hiển thị đèn lồng/Hanji tương ứng.

## Localization, âm thanh và lời dẫn

- `GameText` cung cấp chuỗi tiếng Việt và tiếng Hàn.
- `GameAudioManager` lưu riêng âm lượng nhạc, hiệu ứng và ambience. Ambience/hiệu ứng được tổng hợp khi chạy; chưa dùng bản thu nhạc cụ truyền thống.
- `StoryNarration` gọi adapter TTS theo locale và giữ phụ đề trên màn hình. Android bridge yêu cầu voice vi-VN/ko-KR; fallback giữ phần chữ nếu không thể phát giọng.

## Luồng dữ liệu

```text
UI / thao tác chạm / tương tác world
        → state machine hoạt động hoặc dịch vụ tiến trình
        → candidate PlayerProfile
        → ProfileStore (save thành công)
        → cập nhật profile đang chạy
        → dựng lại UI, sổ và vật thể trong world từ save
```

Checkpoint, cấu hình ngôn ngữ, cài đặt, kết quả mini-game, vật phẩm, dấu ấn, sự kiện nhật ký, metadata ảnh và trạng thái ngày hội được lưu cục bộ.

## Đối chiếu và giới hạn đã biết

- Ma trận hiện ghi nhận 38 DONE và 1 PARTIAL trên 39 Use Case. PARTIAL là xác nhận Android TTS bằng voice/thiết bị thật; chi tiết bằng chứng ở [báo cáo kiểm thử](TEST_REPORT.md) và [ma trận](USECASE_TRACEABILITY.md).
- Unity GUI smoke Direct3D11, smoke từng phân hệ, solution build và tạo APK đã chạy thành công. Build C# gần nhất có 0 lỗi và 3 cảnh báo MSB3277 về xung đột assembly Unity/.NET.
- Chưa duyệt đồ họa, điều khiển, audio hoặc TTS trên điện thoại/AVD tương thích. AVD sẵn có báo OpenGL ES 3.0 trong khi APK yêu cầu ES 3.1.
- Nội dung Hàn cần người bản ngữ duyệt. Hình world hiện dùng minh họa UI và runtime meshes 2.5D; đây chưa phải bộ sprite vẽ tay hoàn chỉnh.
- `PassportApp` vẫn là composition root lớn và phần lớn UI tạo động; đây là điểm cần cân nhắc nếu tiếp tục tách module/prefab.
