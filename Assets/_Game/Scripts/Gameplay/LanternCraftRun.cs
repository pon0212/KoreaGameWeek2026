using TastePassport.Save;

namespace TastePassport.Gameplay
{
    public enum LanternCraftInputResult
    {
        MissingDesign,
        WrongMaterial,
        AlreadyFilled,
        Placed,
        Completed
    }

    /// <summary>Small deterministic craft state used by the lantern activity and editor smoke checks.</summary>
    public sealed class LanternCraftRun
    {
        public const string FabricSlotId = "fabric-slot";
        public const string TasselSlotId = "tassel-slot";
        public const string FabricMaterialId = "fabric";
        public const string TasselMaterialId = "tassel";

        public string FrameId { get; private set; }
        public string ColorId { get; private set; }
        public bool FabricPlaced { get; private set; }
        public bool TasselPlaced { get; private set; }
        public bool Complete => !string.IsNullOrEmpty(FrameId) && !string.IsNullOrEmpty(ColorId) && FabricPlaced && TasselPlaced;

        public bool SelectFrame(string frameId)
        {
            if (Complete || !InventoryService.IsLanternFrame(frameId)) return false;
            FrameId = frameId;
            return true;
        }

        public bool SelectColor(string colorId)
        {
            if (Complete || !InventoryService.IsLanternColor(colorId)) return false;
            ColorId = colorId;
            return true;
        }

        public LanternCraftInputResult TryPlaceMaterial(string slotId, string materialId)
        {
            if (string.IsNullOrEmpty(FrameId) || string.IsNullOrEmpty(ColorId))
                return LanternCraftInputResult.MissingDesign;

            bool fabricSlot = slotId == FabricSlotId;
            bool tasselSlot = slotId == TasselSlotId;
            if (!fabricSlot && !tasselSlot) return LanternCraftInputResult.WrongMaterial;
            if ((fabricSlot && materialId != FabricMaterialId) || (tasselSlot && materialId != TasselMaterialId))
                return LanternCraftInputResult.WrongMaterial;
            if ((fabricSlot && FabricPlaced) || (tasselSlot && TasselPlaced))
                return LanternCraftInputResult.AlreadyFilled;

            if (fabricSlot) FabricPlaced = true;
            else TasselPlaced = true;
            return Complete ? LanternCraftInputResult.Completed : LanternCraftInputResult.Placed;
        }
    }
}
