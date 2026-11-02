using TastePassport.Save;
using TastePassport.Story;

namespace TastePassport.Gameplay
{
    public enum FestivalPlacementResult
    {
        InvalidStation,
        MissingSelection,
        WrongStation,
        DuplicateMark,
        AlreadyPlaced,
        Placed
    }

    public enum FestivalDecorationResult
    {
        InvalidStation,
        StationNotReady,
        MissingSelection,
        AlreadyDecorated,
        Decorated
    }

    public enum FestivalSelectionResult
    {
        InvalidSelection,
        Selected
    }

    public enum FestivalConnectionResult
    {
        MissingVietnamActivity,
        MissingKoreaActivity,
        WrongPair,
        AlreadyConnected,
        Connected
    }

    /// <summary>Restorable state for the seven shared-culture festival stations.</summary>
    public sealed class FestivalPreparationRun
    {
        private static readonly string[] StationIds = { "hoian", "jeonju", "food", "craft", "arts", "exchange", "memory" };
        private static readonly int[] MarksByStation = { 0, 4, 1, 3, 2, 5, 6 };
        private static readonly string[] DecorationIds = { "lantern", "paper-flower", "postcard" };
        private static readonly string[] LinkIds = { "architecture", "food", "craft" };
        private static readonly string[] VietnamActivityIds = { "chuacau", "caolau", "lantern" };
        private static readonly string[] KoreaActivityIds = { "hanok", "bibimbap", "hanji" };

        private readonly bool[] placed = new bool[7];
        private readonly int[] placedMarks = { -1, -1, -1, -1, -1, -1, -1 };
        private readonly string[] decorations = new string[7];
        private readonly bool[] connected = new bool[3];

        public int SelectedMarkIndex { get; private set; } = -1;
        public string SelectedDecorationId { get; private set; }
        public string SelectedVietnamActivityId { get; private set; }
        public string SelectedKoreaActivityId { get; private set; }

        public int PlacedCount => Count(placed);
        public int DecoratedCount
        {
            get
            {
                int count = 0;
                foreach (string decoration in decorations) if (!string.IsNullOrEmpty(decoration)) count++;
                return count;
            }
        }
        public int ConnectedCount => Count(connected);
        public bool Complete => PlacedCount == StationIds.Length && DecoratedCount == StationIds.Length && ConnectedCount == LinkIds.Length;

        public FestivalPreparationRun(PlayerProfile profile)
        {
            for (int station = 0; station < StationIds.Length; station++)
            {
                if (StoryProgressService.HasImprint(profile, GetPlacementImprintId(station)))
                {
                    placed[station] = true;
                    placedMarks[station] = MarksByStation[station];
                }
                foreach (string decorationId in DecorationIds)
                {
                    if (!StoryProgressService.HasImprint(profile, GetDecorationImprintId(station, decorationId))) continue;
                    decorations[station] = decorationId;
                    break;
                }
            }
            for (int i = 0; i < LinkIds.Length; i++)
                connected[i] = StoryProgressService.HasImprint(profile, GetConnectionImprintId(LinkIds[i]));
        }

        public static int StationCount => StationIds.Length;
        public static int MarkCount => 7;
        public static int DecorationCount => DecorationIds.Length;
        public static int LinkCount => LinkIds.Length;

        public static string GetStationId(int index) => IsValidIndex(index, StationIds.Length) ? StationIds[index] : null;
        public static int GetRequiredMarkIndex(int stationIndex) => IsValidIndex(stationIndex, MarksByStation.Length) ? MarksByStation[stationIndex] : -1;
        public static string GetDecorationId(int index) => IsValidIndex(index, DecorationIds.Length) ? DecorationIds[index] : null;
        public static string GetLinkId(int index) => IsValidIndex(index, LinkIds.Length) ? LinkIds[index] : null;
        public static string GetVietnamActivityId(int index) => IsValidIndex(index, VietnamActivityIds.Length) ? VietnamActivityIds[index] : null;
        public static string GetKoreaActivityId(int index) => IsValidIndex(index, KoreaActivityIds.Length) ? KoreaActivityIds[index] : null;
        public static string GetPlacementImprintId(int stationIndex) =>
            IsValidIndex(stationIndex, StationIds.Length) ? "festival-corner-" + StationIds[stationIndex] + "-ready" : null;
        public static string GetPlacementStoryFlagId(int stationIndex) =>
            IsValidIndex(stationIndex, StationIds.Length) ? "festival-placement-" + StationIds[stationIndex] : null;
        public static string GetDecorationImprintId(int stationIndex, string decorationId) =>
            IsValidIndex(stationIndex, StationIds.Length) && IsValidDecoration(decorationId)
                ? "festival-decor-" + StationIds[stationIndex] + "-" + decorationId : null;
        public static string GetDecorationStoryFlagId(int stationIndex, string decorationId) =>
            IsValidIndex(stationIndex, StationIds.Length) && IsValidDecoration(decorationId)
                ? "festival-decoration-" + StationIds[stationIndex] + "-" + decorationId : null;
        public static string GetConnectionImprintId(string linkId) => Find(LinkIds, linkId) >= 0 ? "festival-link-" + linkId : null;
        public static string GetConnectionStoryFlagId(string linkId) => Find(LinkIds, linkId) >= 0 ? "festival-connected-" + linkId : null;

        public bool IsStationPlaced(int stationIndex) => IsValidIndex(stationIndex, placed.Length) && placed[stationIndex];
        public bool IsStationDecorated(int stationIndex) => IsValidIndex(stationIndex, decorations.Length) && !string.IsNullOrEmpty(decorations[stationIndex]);
        public bool IsMarkPlaced(int markIndex) => IsValidIndex(markIndex, MarkCount) && IsMarkPlacedInternal(markIndex);
        public int GetPlacedMarkForStation(int stationIndex) => IsStationPlaced(stationIndex) ? placedMarks[stationIndex] : -1;
        public string GetDecorationForStation(int stationIndex) => IsValidIndex(stationIndex, decorations.Length) ? decorations[stationIndex] : null;
        public bool IsLinkConnected(string linkId)
        {
            int index = Find(LinkIds, linkId);
            return index >= 0 && connected[index];
        }

        public bool SelectMark(int markIndex)
        {
            if (!IsValidIndex(markIndex, MarkCount) || IsMarkPlacedInternal(markIndex)) return false;
            SelectedMarkIndex = markIndex;
            return true;
        }

        public FestivalPlacementResult TryPlaceAt(int stationIndex)
        {
            if (!IsValidIndex(stationIndex, StationIds.Length)) return FestivalPlacementResult.InvalidStation;
            if (placed[stationIndex]) return FestivalPlacementResult.AlreadyPlaced;
            if (!IsValidIndex(SelectedMarkIndex, MarkCount)) return FestivalPlacementResult.MissingSelection;
            if (IsMarkPlacedInternal(SelectedMarkIndex)) return FestivalPlacementResult.DuplicateMark;
            if (SelectedMarkIndex != MarksByStation[stationIndex]) return FestivalPlacementResult.WrongStation;

            placed[stationIndex] = true;
            placedMarks[stationIndex] = SelectedMarkIndex;
            SelectedMarkIndex = -1;
            return FestivalPlacementResult.Placed;
        }

        public bool SelectDecoration(string decorationId)
        {
            if (!IsValidDecoration(decorationId)) return false;
            SelectedDecorationId = decorationId;
            return true;
        }

        public FestivalDecorationResult TryDecorateAt(int stationIndex)
        {
            if (!IsValidIndex(stationIndex, StationIds.Length)) return FestivalDecorationResult.InvalidStation;
            if (!placed[stationIndex]) return FestivalDecorationResult.StationNotReady;
            if (!string.IsNullOrEmpty(decorations[stationIndex])) return FestivalDecorationResult.AlreadyDecorated;
            if (!IsValidDecoration(SelectedDecorationId)) return FestivalDecorationResult.MissingSelection;

            decorations[stationIndex] = SelectedDecorationId;
            SelectedDecorationId = null;
            return FestivalDecorationResult.Decorated;
        }

        public FestivalSelectionResult SelectVietnamActivity(string activityId)
        {
            if (Find(VietnamActivityIds, activityId) < 0) return FestivalSelectionResult.InvalidSelection;
            SelectedVietnamActivityId = activityId;
            return FestivalSelectionResult.Selected;
        }

        public FestivalSelectionResult SelectKoreaActivity(string activityId)
        {
            if (Find(KoreaActivityIds, activityId) < 0) return FestivalSelectionResult.InvalidSelection;
            SelectedKoreaActivityId = activityId;
            return FestivalSelectionResult.Selected;
        }

        public FestivalConnectionResult TryConnectActivities()
        {
            if (string.IsNullOrEmpty(SelectedVietnamActivityId)) return FestivalConnectionResult.MissingVietnamActivity;
            if (string.IsNullOrEmpty(SelectedKoreaActivityId)) return FestivalConnectionResult.MissingKoreaActivity;

            int vietnamIndex = Find(VietnamActivityIds, SelectedVietnamActivityId);
            int koreaIndex = Find(KoreaActivityIds, SelectedKoreaActivityId);
            if (vietnamIndex < 0 || vietnamIndex != koreaIndex) return FestivalConnectionResult.WrongPair;
            if (connected[vietnamIndex]) return FestivalConnectionResult.AlreadyConnected;

            connected[vietnamIndex] = true;
            SelectedVietnamActivityId = null;
            SelectedKoreaActivityId = null;
            return FestivalConnectionResult.Connected;
        }

        private bool IsMarkPlacedInternal(int markIndex)
        {
            for (int i = 0; i < placedMarks.Length; i++)
                if (placed[i] && placedMarks[i] == markIndex) return true;
            return false;
        }

        private static bool IsValidDecoration(string decorationId) => Find(DecorationIds, decorationId) >= 0;
        private static bool IsValidIndex(int index, int count) => index >= 0 && index < count;
        private static int Count(bool[] values)
        {
            int count = 0;
            foreach (bool value in values) if (value) count++;
            return count;
        }
        private static int Find(string[] values, string value)
        {
            if (string.IsNullOrEmpty(value)) return -1;
            for (int i = 0; i < values.Length; i++) if (values[i] == value) return i;
            return -1;
        }
    }
}
