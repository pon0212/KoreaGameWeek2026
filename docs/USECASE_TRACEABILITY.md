# Ma trận truy vết Use Case

Nguồn yêu cầu: `CODEX_MASTER_PROMPT.md`, `reference/use_case.drawio`, `reference/use_case_master.svg`, `reference/use_case_cultural.svg`, `reference/game_story.txt` và bộ ảnh `reference/art/` trong checkout tham chiếu `TastePassport_Main`.

**Quy tắc trạng thái:** DONE = có lối vào thực, hành vi và dữ liệu cần lưu đã được kiểm tra; PARTIAL = còn tiêu chí thiết bị/nội dung cần xác nhận; MISSING = chưa có; BROKEN = có nhưng không đạt tiêu chí.

## UC-01 — Khởi đầu hành trình

| ID | Kết quả cần đạt | Lối vào / hành vi thực | Lưu trạng thái | Bằng chứng | Trạng thái |
|---|---|---|---|---|---|
| UC-01.1 | Bắt đầu hành trình mới | Menu → ngôn ngữ → hồ sơ → vào Hội An | Tên, nhân vật, ngôn ngữ, checkpoint đầu | `PassportApp.CreateProfile`; GUI runtime smoke | DONE |
| UC-01.2 | Xem truyện mở đầu, có thao tác bỏ qua rõ ràng | Lần đầu gặp Nhung có nút “Bỏ qua phần mở đầu / 도입부 건너뛰기”; bấm đóng hội thoại | Cờ truyện và dấu ấn được ghi ngay khi bắt đầu cuộc gặp | `WalkableWorldSession.SkipOpeningStory`; `RunWorldSessionChecks` xác nhận nút chỉ hiện lần đầu và vẫn giữ save | DONE |
| UC-01.3 | Tạo hồ sơ, chọn nhân vật và áp dụng vào game | Form tạo hồ sơ; nhân vật xuất hiện trong world | Tên, avatar và profile | `PassportApp.CreateProfile`, `WorldCharacterModelBuilder`; GUI runtime smoke | DONE |
| UC-01.4 | Tiếp tục hành trình đã lưu | Menu → Tiếp tục | World và vị trí/checkpoint gần nhất | `ProfileStore`, world-session smoke, GUI runtime smoke | DONE |
| UC-01.5 | Xử lý save mới, save hỏng và bản dự phòng | Tự nạp khi mở game | Save có version, ghi atomic, phục hồi backup | `ProfileStore`; smoke kiểm tra backup recovery và migration | DONE |

## UC-02 — Khám phá văn hóa

| ID | Kết quả cần đạt | Lối vào / hành vi thực | Lưu trạng thái | Bằng chứng | Trạng thái |
|---|---|---|---|---|---|
| UC-02.1 | Khám phá Hội An và Jeonju, chuyển vùng đúng cốt truyện | Bản đồ → world có thể đi bộ; Jeonju mở sau cột mốc Hội An | World/checkpoint và cờ mở khóa | `WorldCatalog`, `WalkableWorldSession`, `MapObjectiveService`; GUI runtime smoke | DONE |
| UC-02.2 | Tương tác NPC theo phạm vi, hội thoại đầu/lặp lại | Đi đến gần NPC → trò chuyện / hỏi thêm | Cờ truyện, dấu ấn, thay đổi mục tiêu | `WorldInteractable`, `WalkableWorldSession`; world-session smoke | DONE |
| UC-02.3 | Nhận biết và tìm hiểu di tích | Chùa Cầu, Hanok và điểm văn hóa có lời kể/thẻ văn hóa | Discovery mark, cultural card, stamp | `WorldDioramaBuilder`, `WorldInteractable`; architecture/phase-four smoke | DONE |
| UC-02.4 | Nghe chuyện địa phương với phụ đề và nút phát/dừng | Nút nghe/dừng trong hội thoại và trang hoạt động; chữ vẫn hiển thị | Ngôn ngữ lấy từ hồ sơ; trạng thái phát là tạm thời | `StoryNarration`, `WalkableWorldSession`; narration settings smoke | DONE |
| UC-02.5 | Chụp ảnh thật trong game | World HUD → Máy ảnh; kết quả Bibimbap cũng có nút chụp | JPEG riêng, record và event có ngày/world/location | GUI Direct3D11 smoke kiểm tra ảnh Hội An và ảnh Bibimbap tồn tại, liên kết với timeline | DONE |
| UC-02.6 | Xem và khôi phục ảnh đã chụp | Sổ Dấu Xứ → ảnh/bưu thiếp/nhật ký | Ảnh tồn tại ngoài JSON; record được nạp lại | `MemoryPhotoService`, `PassportApp.DrawJournal`; photo-journal smoke và GUI runtime smoke | DONE |

## UC-03 — Nhiệm vụ và mini-game

| ID | Kết quả cần đạt | Lối vào / hành vi thực | Lưu trạng thái | Bằng chứng | Trạng thái |
|---|---|---|---|---|---|
| UC-03.1 | Nhận nhiệm vụ từ NPC hoặc địa điểm | Nhiệm vụ ở bản đồ; Lan/Bác Kim giao craft; landmark mở hoạt động riêng | Mục tiêu truyện và trạng thái nhiệm vụ | `WorldCatalog`, `MapObjectiveService`; world-session và GUI runtime smoke | DONE |
| UC-03.2 | Sáu hoạt động có thao tác và điều kiện thắng riêng | Chùa Cầu tìm chi tiết/lắp ghép; làm đèn lồng; Cao Lầu; lắp Hanok; thiết kế Hanji; Bibimbap | Kết quả chỉ được ghi khi state machine đạt điều kiện hoàn tất | `ExplorationRun`, `LanternCraftRun`, `RecipeRun`, `HanjiCraftRun`; GUI runtime và phase-four smoke | DONE |
| UC-03.3 | Chơi lại không nhân đôi thưởng | Kết quả → Chơi lại | playCount tăng; best score cập nhật; tiền thưởng completion chỉ nhận lần đầu | `ProgressService.Record`; GUI runtime smoke xác nhận playCount=2 và số xu không đổi | DONE |
| UC-03.4 | Chỉ hoàn tất khi thao tác thắng thật | Tìm/lắp đúng, đặt craft đúng thứ tự, nấu đủ bước | Completion và phần thưởng được commit qua save | Sáu hoạt động được chơi qua GUI runtime smoke; không có nút giả hoàn tất | DONE |
| UC-03.5 | Lưu điểm, lượt chơi và tiến trình | Trang kết quả và sổ | Best score/stars/playCount, stamps, events và cultural cards | `ProgressService`, `ProfileStore`; GUI runtime và photo-journal smoke | DONE |
| UC-03.6 | Dấu Ấn Sống làm thay đổi world sau load | Chế tác/khám phá → quay lại world | Imprint và biến thể vật phẩm đã craft | `StoryProgressService`, `WorldDioramaBuilder`; phase-four smoke xác nhận display sau reload | DONE |

## UC-04 — Sổ Dấu Xứ

| ID | Kết quả cần đạt | Lối vào / hành vi thực | Lưu trạng thái | Bằng chứng | Trạng thái |
|---|---|---|---|---|---|
| UC-04.1 | Mở Sổ Dấu Xứ khi đang chơi | Menu, world HUD hoặc bản đồ → sổ | Đọc từ profile hiện tại | `PassportApp.DrawPassport`; GUI runtime smoke | DONE |
| UC-04.2 | Xem con dấu và bưu thiếp hai mặt | Bộ sưu tập → mở thẻ → lật mặt | Unlock theo tiến trình; có thẻ Chùa Cầu riêng | `PostcardCatalog`; GUI runtime smoke kiểm tra bốn thẻ và thao tác lật; photo-journal smoke kiểm tra khóa/mở Chùa Cầu | DONE |
| UC-04.3 | Xem nhật ký theo thời gian và địa điểm | Sổ → dòng sự kiện/ảnh | UTC timestamp, world/location, event id | `PlayerProfile.journeyEvents`, `MemoryPhotoService`; photo-journal smoke | DONE |
| UC-04.4 | Trang trí trang nhật ký bằng ký ức/vật phẩm đã sở hữu | Sổ → chọn ảnh, dấu, vật phẩm và vị trí trang | Layout và lựa chọn được save | `JournalService.TrySavePage`; smoke từ chối ảnh sai world, layout sai và vật phẩm chưa sở hữu | DONE |
| UC-04.5 | Xem lại các dấu ấn sống | Sổ → danh sách Dấu Ấn | Đọc imprint đã lưu | `StoryProgressService`, passport UI; phase-four/five smoke | DONE |
| UC-04.6 | Chọn ảnh và lưu bố cục nhật ký | Sổ → chọn ảnh → bố trí → lưu | Photo id, imprint, item id, layout slot | `JournalService`; photo-journal smoke lưu rồi nạp lại trang | DONE |

## UC-05 — Ngày hội và hậu truyện

| ID | Kết quả cần đạt | Lối vào / hành vi thực | Lưu trạng thái | Bằng chứng | Trạng thái |
|---|---|---|---|---|---|
| UC-05.1 | Chỉ mở ngày hội sau các nhiệm vụ văn hóa chính | Hoàn tất bốn hoạt động chính và hai craft | Điều kiện được tính từ stamp/craft thực | `PassportApp.IsFirstJourneyComplete`; phase-five smoke kiểm tra từng điều kiện mở khóa | DONE |
| UC-05.2 | Bố trí gian và nối hoạt động hai nước | Bảy trạm; chọn dấu, đồ trang trí, ba cặp hoạt động | Từng trạm và liên kết được lưu | `FestivalPreparationRun`; phase-five và GUI runtime smoke | DONE |
| UC-05.3 | Chỉ dùng vật phẩm người chơi sở hữu | Chọn đèn/Hanji/ảnh đã lưu | Record giữ đúng item/photo id | `FestivalPreparationRun.IsOwnedDecorationItem`; phase-five smoke từ chối item không sở hữu | DONE |
| UC-05.4 | Kết quả phản ánh vật phẩm và bố trí đã chọn | Kết thúc lễ hội → màn reveal và hậu truyện | Station decorations, ảnh và cờ hoàn tất truyện | `DrawFinaleResult`, `CompleteFestival`; phase-five và GUI runtime smoke | DONE |
| UC-05.5 | Tiếp tục khám phá tự do sau đoạn kết | Kết thúc → bản đồ → Hội An hoặc Jeonju | Cờ main-story-completed không khóa world | Phase-five và GUI runtime smoke mở cả hai world | DONE |
| UC-05.6 | Quay lại địa điểm và chơi lại hoạt động | Bản đồ/world vẫn còn nhiệm vụ | Save cũ giữ nguyên; reward không lặp | GUI runtime smoke kiểm tra replay và map sau kết thúc | DONE |

## UC-06 — Cài đặt và hỗ trợ

| ID | Kết quả cần đạt | Lối vào / hành vi thực | Lưu trạng thái | Bằng chứng | Trạng thái |
|---|---|---|---|---|---|
| UC-06.1 | Chuyển giao diện Việt/Hàn | Menu/cài đặt → đổi ngôn ngữ | Ngôn ngữ trong profile | `GameText`, GUI runtime smoke và phase-four/five localization checks | DONE |
| UC-06.2 | Điều chỉnh nhạc, hiệu ứng, ambience và lời dẫn | Cài đặt → thanh âm lượng/tắt tiếng | Giá trị lưu riêng trong PlayerPrefs | Phase-six audio settings và narration settings smoke | DONE |
| UC-06.3 | Phát/dừng lời dẫn mà vẫn giữ phụ đề | Nút nghe/dừng trên hội thoại/truyện | Không thay đổi cốt truyện | `StoryNarration`, narration settings smoke | DONE |
| UC-06.4 | Android Text-to-Speech và fallback khi thiếu voice/thiết bị | Android bridge chọn vi-VN/ko-KR; PC trả fallback, chữ vẫn còn | Cài đặt lời dẫn riêng | Java bridge và fallback được compile/test; chưa có điện thoại Android/GLES phù hợp để xác nhận voice thật | PARTIAL |

## HA — Hoạt động văn hóa Hội An

| ID | Kết quả cần đạt | Lối vào / hành vi thực | Lưu trạng thái | Bằng chứng | Trạng thái |
|---|---|---|---|---|---|
| UC-HA-01 | Khám phá Chùa Cầu, tìm chi tiết và lưu thẻ/bưu thiếp | World → Chùa Cầu; hoạt động có chi tiết kiến trúc đặt đúng vị trí | Stamp, cultural card, postcard unlock | Architecture puzzle và photo-journal smoke; GUI runtime smoke | DONE |
| UC-HA-02 | Thiết kế, ráp và trưng bày đèn lồng | Lan/map → kéo thả khung, màu, vật liệu → craft | Vật phẩm craft, imprint; đèn xuất hiện tại world | Lantern drag/drop smoke, GUI runtime smoke, world display test | DONE |
| UC-HA-03 | Làm Cao Lầu theo thứ tự công đoạn | Bản đồ → recipe có thao tác từng bước | Điểm, stamp, cultural card và imprint | GUI runtime smoke chơi và lưu kết quả recipe | DONE |

## JJ — Hoạt động văn hóa Jeonju

| ID | Kết quả cần đạt | Lối vào / hành vi thực | Lưu trạng thái | Bằng chứng | Trạng thái |
|---|---|---|---|---|---|
| UC-JJ-01 | Lắp mô hình Hanok đúng chi tiết và hướng | World/map → tìm thành phần, xoay và lắp đúng slot | Stamp và cultural card | Architecture puzzle + phase-four smoke từ chối sai slot/hướng | DONE |
| UC-JJ-02 | Thiết kế sản phẩm Hanji và trưng bày | Bác Kim/map → chọn hình/màu/họa tiết, cắt/gấp/trang trí | Vật phẩm, thẻ văn hóa và imprint/display | Phase-four Hanji craft smoke xác nhận thứ tự, biến thể và display sau reload | DONE |
| UC-JJ-03 | Chuẩn bị Bibimbap và lưu ảnh món ăn | Recipe → chọn nguyên liệu theo bước → trộn → chụp ảnh | Điểm, stamp, cultural card, imprint, JPEG/timeline event | GUI runtime smoke chụp ảnh thật và kiểm tra file cùng save | DONE |

## Kết luận và giới hạn

- **38 DONE, 1 PARTIAL, 0 MISSING, 0 BROKEN** trong 39 Use Case.
- PARTIAL duy nhất là xác nhận Android TTS trên thiết bị/voice thật. APK đã build và cài được lên AVD, nhưng AVD chỉ cung cấp OpenGL ES 3.0 trong khi package khai báo ES 3.1.
- Localization Việt/Hàn đã được kiểm tra về khóa và luồng; nội dung tiếng Hàn vẫn cần người bản ngữ duyệt.
- UI dùng giấy kem và tranh phong cảnh màu nước; các nhân vật/đạo cụ trong world là 2.5D runtime meshes. Cần duyệt mỹ thuật cuối trên màn hình Android thật.
- GUI smoke hiện dùng scene và save thật của game; helper chụp preview UI trong `#if UNITY_EDITOR` chỉ tạo dữ liệu cố định để chụp trang minh họa, không tham gia gameplay hay được dùng làm bằng chứng hoàn tất nhiệm vụ.
