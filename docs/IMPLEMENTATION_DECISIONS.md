# Quyết định triển khai

## Quyết định hiện hành

1. **Tiếp tục Unity hiện có.** Repository đang dùng Unity `6000.6.7f1`, C#, URP và scene chơi được. Không tạo project thay thế để giữ lại hành vi, nội dung và thay đổi đang có.
2. **Dùng scene và runtime builder hiện tại.** `TastePassport_MVP.unity` là điểm vào; `PassportApp`, `WalkableWorldSession` và `WorldDioramaBuilder` kết nối UI với hai world.
3. **Giữ save offline và có version.** `ProfileStore` ghi profile JSON có backup. Ảnh JPEG được lưu ngoài JSON và metadata/event nằm trong profile để file save không phình theo ảnh.
4. **Chỉ commit tiến trình sau khi save thành công.** Các luồng hoạt động tạo candidate profile; UI/live state nhận kết quả sau khi lưu. Completion/reward là idempotent để chơi lại không nhân đôi xu.
5. **Tách quy tắc miền khỏi composition root khi mở rộng.** Giữ `PassportApp` làm nơi nối UI trong đợt này; luật tiến trình, inventory, ảnh, festival và gameplay nằm trong service/state machine riêng.
6. **Dùng tài liệu art làm định hướng, không coi là asset sản xuất.** Hình tham chiếu dưới `reference/art/` định hướng màu và chủ đề. UI giấy màu kem/tranh phong cảnh và các runtime meshes hiện hữu được giữ lại; thay art cần sprite tối ưu và quyền sử dụng phù hợp.
7. **Giữ phụ đề làm fallback của TTS.** Android chọn locale `vi-VN`/`ko-KR`; khi thiết bị thiếu engine/voice, lời thoại vẫn hiện trên màn hình. TTS thật chưa được xác nhận trên thiết bị tương thích trong đợt kiểm tra.
8. **Không khẳng định nghiệm thu Android chỉ từ APK build.** APK đã được dựng và ký kiểm tra, nhưng AVD khả dụng chỉ hỗ trợ GLES 3.0, thấp hơn GLES 3.1 khai báo. Cần thiết bị/AVD tương thích để nghiệm thu render, input, lưu dữ liệu và TTS.
9. **Duy trì rà soát ngôn ngữ Hàn.** Khóa dịch và luồng chuyển ngôn ngữ đã được kiểm tra; văn phong, thuật ngữ văn hóa và phát âm cần người bản ngữ duyệt.

## Nguồn yêu cầu

Prompt, cốt truyện, sơ đồ Use Case và ảnh tham chiếu được đọc từ checkout tham chiếu `TastePassport_Main`; chúng là nội dung yêu cầu dự án. Bảng trạng thái và bằng chứng triển khai được duy trì trong [ma trận truy vết](USECASE_TRACEABILITY.md).
