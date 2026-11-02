using TastePassport.Save;

namespace TastePassport.Gameplay
{
    public enum HanjiCraftActionResult
    {
        MissingDesign,
        OutOfOrder,
        AlreadyApplied,
        Applied,
        Completed
    }

    /// <summary>Deterministic state for the small, touch-friendly Hanji workshop activity.</summary>
    public sealed class HanjiCraftRun
    {
        public string PaperColorId { get; private set; }
        public string ShapeId { get; private set; }
        public string MotifId { get; private set; }
        public bool CutApplied { get; private set; }
        public bool FoldApplied { get; private set; }
        public bool DecorationApplied { get; private set; }
        public bool Complete => !string.IsNullOrEmpty(PaperColorId) && !string.IsNullOrEmpty(ShapeId) &&
                                !string.IsNullOrEmpty(MotifId) && CutApplied && FoldApplied && DecorationApplied;

        public bool SelectPaperColor(string colorId)
        {
            if (Complete || !InventoryService.IsHanjiColor(colorId)) return false;
            PaperColorId = colorId;
            return true;
        }

        public bool SelectShape(string shapeId)
        {
            if (Complete || !InventoryService.IsHanjiShape(shapeId)) return false;
            ShapeId = shapeId;
            return true;
        }

        public bool SelectMotif(string motifId)
        {
            if (Complete || !InventoryService.IsHanjiMotif(motifId)) return false;
            MotifId = motifId;
            return true;
        }

        public HanjiCraftActionResult ApplyAction(string actionId)
        {
            if (Complete) return HanjiCraftActionResult.AlreadyApplied;
            if (string.IsNullOrEmpty(PaperColorId) || string.IsNullOrEmpty(ShapeId) || string.IsNullOrEmpty(MotifId))
                return HanjiCraftActionResult.MissingDesign;
            if ((actionId == "cut" && CutApplied) || (actionId == "fold" && FoldApplied) ||
                (actionId == "decorate" && DecorationApplied)) return HanjiCraftActionResult.AlreadyApplied;

            if (!CutApplied)
            {
                if (actionId != "cut") return HanjiCraftActionResult.OutOfOrder;
                CutApplied = true;
                return HanjiCraftActionResult.Applied;
            }
            if (!FoldApplied)
            {
                if (actionId != "fold") return HanjiCraftActionResult.OutOfOrder;
                FoldApplied = true;
                return HanjiCraftActionResult.Applied;
            }
            if (!DecorationApplied)
            {
                if (actionId != "decorate") return HanjiCraftActionResult.OutOfOrder;
                DecorationApplied = true;
                return HanjiCraftActionResult.Completed;
            }
            return HanjiCraftActionResult.AlreadyApplied;
        }
    }
}
