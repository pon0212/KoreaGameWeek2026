using System;
using System.Collections.Generic;
using TastePassport.Save;
using TastePassport.UI;

namespace TastePassport.Learning
{
    [Serializable]
    public sealed class CulturalWord
    {
        public readonly string Id;
        public readonly string Vietnamese;
        public readonly string Korean;
        public readonly string KoreanPronunciation;
        public readonly string VietnamesePronunciation;
        public readonly string MeaningVi;
        public readonly string MeaningKo;
        public readonly string ExampleVi;
        public readonly string ExampleKo;
        public readonly int StageIndex; // 1 to 8
        public readonly string Category;
        public readonly TravelArtworkKind ArtworkKind;

        public CulturalWord(string id, string vietnamese, string korean, string koreanPronunciation,
            string vietnamesePronunciation, string meaningVi, string meaningKo,
            string exampleVi, string exampleKo, int stageIndex, string category,
            TravelArtworkKind artworkKind)
        {
            Id = id;
            Vietnamese = vietnamese;
            Korean = korean;
            KoreanPronunciation = koreanPronunciation;
            VietnamesePronunciation = vietnamesePronunciation;
            MeaningVi = meaningVi;
            MeaningKo = meaningKo;
            ExampleVi = exampleVi;
            ExampleKo = exampleKo;
            StageIndex = stageIndex;
            Category = category;
            ArtworkKind = artworkKind;
        }

        public string GetWord(string language) => language == "ko" ? Korean : Vietnamese;
        public string GetPronunciation(string language) => language == "ko" ? VietnamesePronunciation : KoreanPronunciation;
        public string GetMeaning(string language) => language == "ko" ? MeaningKo : MeaningVi;
        public string GetExample(string language) => language == "ko" ? ExampleKo : ExampleVi;
    }

    public static class VocabularyCatalog
    {
        public static readonly CulturalWord[] AllWords =
        {
            // Chặng 1: Dấu chân phố cổ (Hội An)
            new CulturalWord("xin_chao", "Xin chào", "안녕하세요", "an-nyeong-ha-se-yo", "xin chào",
                "Lời chào thân thiện mở đầu mọi cuộc gặp gỡ.", "모든 만남을 여는 따뜻한 인사말.",
                "Xin chào! Rất vui được gặp bạn ở Hội An.", "안녕하세요! 호이안에서 만나서 반가워요.",
                1, "Chào hỏi", TravelArtworkKind.Passport),
            new CulturalWord("chua_cau", "Chùa Cầu", "내원교 (쭈어꺼우)", "jjueo-keu-o", "chùa cầu",
                "Cây cầu ngói cổ kính, biểu tượng lịch sử của Hội An.", "호이안의 상징이자 역사가 깃든 목조 다리.",
                "Chùa Cầu soi bóng xuống dòng sông Hoài êm đềm.", "내원교가 잔잔한 호아이강에 비쳐요.",
                1, "Kiến trúc", TravelArtworkKind.HoiAn),
            new CulturalWord("pho_co", "Phố cổ", "올드타운", "ol-deu-ta-un", "phố cổ",
                "Khu phố rực rỡ mái ngói rêu phong và tường vàng hoài niệm.", "노란 벽과 기와지붕이 아름다운 옛 거리.",
                "Tản bộ ngắm phố cổ lúc hoàng hôn buông xuống.", "해 질 녘 고즈넉한 올드타운을 거닐어요.",
                1, "Địa danh", TravelArtworkKind.HoiAn),

            // Chặng 2: Sắc đèn Hội An
            new CulturalWord("den_long", "Đèn lồng", "등불 / 청사초롱", "deung-bul", "đèn lồng",
                "Chiếc đèn lụa thủ công thắp sáng lung linh phố Hội.", "비단으로 만든 호이안의 빛나는 전통 등불.",
                "Đèn lồng tỏa ánh sáng ấm áp khắp hiên nhà.", "등불이 처마 밑을 따뜻하게 밝혀요.",
                2, "Thủ công", TravelArtworkKind.HoiAn),
            new CulturalWord("thu_cong", "Thủ công", "수공예", "su-gong-ye", "thủ công",
                "Nghệ thuật khéo léo làm ra sản phẩm bằng đôi bàn tay.", "손으로 정성을 다해 만드는 전통 예술.",
                "Nghề làm đèn lồng thủ công đòi hỏi sự tỉ mỉ.", "전통 등불 수공예는 정교한 손길이 필요해요.",
                2, "Nghề truyền thống", TravelArtworkKind.HoiAn),

            // Chặng 3: Bí quyết Cao Lầu
            new CulturalWord("cao_lau", "Cao Lầu", "까오러우", "ka-o-reo-u", "cao lầu",
                "Món mì đặc sản độc nhất vô nhị chỉ có ở phố Hội.", "호이안에서만 맛볼 수 있는 특별한 전통 국수 요리.",
                "Tô Cao Lầu thơm lừng ăn kèm rau ghém Trà Quế.", "향긋한 채소와 함께 까오러우를 즐겨요.",
                3, "Ẩm thực", TravelArtworkKind.CaoLau),
            new CulturalWord("nguyen_lieu", "Nguyên liệu", "재료", "jae-ryo", "nguyên liệu",
                "Những thành phần tươi ngon làm nên món ăn đặc sắc.", "맛있는 요리를 완성하는 신선한 음식 재료.",
                "Chọn nguyên liệu tươi ngon tại chợ sớm Hội An.", "호이안 아침 시장에서 신선한 재료를 골라요.",
                3, "Ẩm thực", TravelArtworkKind.CaoLau),

            // Chặng 4: Âm vang phố Hội
            new CulturalWord("cam_on", "Cảm ơn", "감사합니다", "gam-sa-ham-ni-da", "cảm ơn",
                "Lời tri ân chân thành gửi đến người bạn đồng hành.", "함께해 준 친구에게 전하는 진심 어린 감사 인사.",
                "Cảm ơn bạn đã đồng hành trong chuyến đi này!", "이번 여행을 함께해 주셔서 감사합니다!",
                4, "Giao lưu", TravelArtworkKind.Friendship),
            new CulturalWord("di_san", "Di sản", "유산", "yu-san", "di sản",
                "Giá trị văn hóa quý báu được gìn giữ qua nhiều thế hệ.", "세대를 거쳐 소중히 이어져 온 문화적 보물.",
                "Hội An là di sản văn hóa thế giới được UNESCO công nhận.", "호이안은 유네스코가 인정한 세계문화유산이에요.",
                4, "Văn hóa", TravelArtworkKind.HoiAn),

            // Chặng 5: Ký ức làng Hanok (Jeonju)
            new CulturalWord("hanok", "Nhà Hanok", "한옥", "han-ok", "nhà ha-nốc",
                "Ngôi nhà truyền thống Hàn Quốc mái cong thanh thoát.", "부드러운 곡선의 기와지붕이 아름다운 한국 전통 가옥.",
                "Làng Hanok Jeonju giữ trọn nét bình yên cổ kính.", "전주 한옥마을은 고즈넉한 멋을 간직하고 있어요.",
                5, "Kiến trúc", TravelArtworkKind.Jeonju),
            new CulturalWord("mai_ngoi", "Mái ngói", "기와지붕", "gi-wa-ji-bung", "mái ngói",
                "Đường cong duyên dáng vươn lên giữa trời xanh Jeonju.", "전주의 푸른 하늘 아래 펼쳐진 기품 있는 지붕선.",
                "Mái ngói cong nhấp nhô tuyệt đẹp trong sương sớm.", "아침 안개 속 기와지붕의 곡선이 무척 아름다워요.",
                5, "Kiến trúc", TravelArtworkKind.Jeonju),

            // Chặng 6: Nghệ thuật Hanji
            new CulturalWord("hanji", "Giấy Hanji", "한지", "han-ji", "giấy han-ji",
                "Loại giấy truyền thống bền bỉ nghìn năm của xứ Hàn.", "천 년의 숨결을 간직한 한국의 전통 수제 종이.",
                "Giấy Hanji mềm mại được dùng làm quạt và đèn hoa.", "부드러운 한지로 부채와 꽃등을 만들어요.",
                6, "Thủ công", TravelArtworkKind.Jeonju),
            new CulturalWord("che_tac", "Chế tác", "제작하다", "je-jak-ha-da", "chế tác",
                "Quá trình nhào nặn và tạo hình sản phẩm nghệ thuật.", "마음과 정성을 담아 예술품을 만들어 가는 과정.",
                "Cùng nghệ nhân chế tác một món quà lưu niệm ý nghĩa.", "장인과 함께 뜻깊은 공예품을 제작해요.",
                6, "Thủ công", TravelArtworkKind.Jeonju),

            // Chặng 7: Sắc màu Bibimbap
            new CulturalWord("bibimbap", "Bibimbap", "비빔밥", "bi-bim-bap", "cơm trộn bi-bim-bap",
                "Món cơm trộn ngũ sắc trứ danh mang triết lý âm dương ngũ hành.", "오방색의 조화와 영양이 가득한 전주의 대표 요리.",
                "Bát cơm Bibimbap nóng hổi với màu sắc rực rỡ.", "알록달록 오방색이 고운 따뜻한 비빔밥 한 그릇.",
                7, "Ẩm thực", TravelArtworkKind.Bibimbap),
            new CulturalWord("tron_deu", "Trộn đều", "골고루 비비다", "gol-go-ru-bi-bi-da", "trộn đều",
                "Hành động hòa quyện tất cả nguyên liệu vào nhau.", "모든 재료의 맛이 하나로 어우러지도록 섞는 일.",
                "Trộn đều bát cơm trước khi cùng thưởng thức.", "맛있게 먹기 전에 비빔밥을 골고루 비벼요.",
                7, "Ẩm thực", TravelArtworkKind.Bibimbap),

            // Chặng 8: Ngày hội không biên giới
            new CulturalWord("giao_luu", "Giao lưu", "교류", "gyo-ryu", "giao lưu",
                "Sự gắn kết và chia sẻ nét đẹp văn hóa giữa hai miền đất.", "두 나라가 서로의 아름다운 문화를 나누고 잇는 일.",
                "Ngày hội giao lưu thắt chặt tình cảm Việt Nam - Hàn Quốc.", "문화 교류 축제를 통해 두 나라의 우정을 다져요.",
                8, "Lễ hội", TravelArtworkKind.Friendship),
            new CulturalWord("ngay_hoi", "Ngày hội", "축제", "chuk-je", "ngày hội",
                "Không gian lễ hội tưng bừng tràn ngập sắc màu và âm thanh.", "음악과 웃음이 가득한 활기찬 화합의 한마당.",
                "Cùng Nhung và Ga Bo trang hoàng cho ngày hội lớn.", "Nhung, Ga Bo와 함께 멋진 축제 공간을 꾸며요.",
                8, "Lễ hội", TravelArtworkKind.Friendship),
            new CulturalWord("tinh_ban", "Tình bạn", "우정", "u-jeong", "tình bạn",
                "Mối liên kết quý báu không biên giới giữa hai tâm hồn.", "국경을 넘어 마음으로 이어지는 소중한 인연.",
                "Tình bạn thắm thiết được lưu giữ mãi trong cuốn Dấu Xứ.", "두 나라 친구들의 따뜻한 우정이 수첩에 영원히 남아요.",
                8, "Tình cảm", TravelArtworkKind.Friendship)
        };

        public static CulturalWord FindById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < AllWords.Length; i++)
            {
                if (string.Equals(AllWords[i].Id, id, StringComparison.OrdinalIgnoreCase))
                    return AllWords[i];
            }
            return null;
        }

        public static List<CulturalWord> GetWordsForStage(int stageIndex)
        {
            var list = new List<CulturalWord>();
            for (int i = 0; i < AllWords.Length; i++)
            {
                if (AllWords[i].StageIndex == stageIndex)
                    list.Add(AllWords[i]);
            }
            return list;
        }

        public static void UnlockStageWords(PlayerProfile profile, int stageIndex)
        {
            if (profile == null) return;
            if (profile.learnedVocabulary == null) profile.learnedVocabulary = new List<string>();
            var words = GetWordsForStage(stageIndex);
            foreach (var w in words)
            {
                if (!profile.learnedVocabulary.Contains(w.Id))
                    profile.learnedVocabulary.Add(w.Id);
            }
        }
    }
}
