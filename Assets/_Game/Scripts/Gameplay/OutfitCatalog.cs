using System;
using TastePassport.Save;

namespace TastePassport.Gameplay
{
    public enum OutfitCulture { Shared, Vietnamese, Korean }

    [Serializable]
    public sealed class OutfitDefinition
    {
        public int Id;
        public string NameKey;
        public OutfitCulture Culture;
        public int Price;
        public int Avatar;

        public OutfitDefinition(int id, string key, OutfitCulture culture, int price, int avatar)
        { Id = id; NameKey = key; Culture = culture; Price = price; Avatar = avatar; }

        public bool IsOwned(PlayerProfile profile) => profile.ownedOutfits != null && profile.ownedOutfits.Contains(Id);
    }

    public static class OutfitCatalog
    {
        public static readonly OutfitDefinition[] All =
        {
            new OutfitDefinition(0, "outfit_traveler", OutfitCulture.Shared, 0, 0),
            new OutfitDefinition(1, "outfit_ao_dai", OutfitCulture.Vietnamese, 0, 1),
            new OutfitDefinition(2, "outfit_hanbok", OutfitCulture.Korean, 0, 2),
            new OutfitDefinition(3, "outfit_lantern", OutfitCulture.Vietnamese, 80, 0),
            new OutfitDefinition(4, "outfit_hanji", OutfitCulture.Korean, 80, 1)
        };

        public static OutfitDefinition Get(int id)
        {
            foreach (var item in All) if (item.Id == id) return item;
            return All[0];
        }

        public static bool TryUnlock(PlayerProfile profile, int id)
        {
            var outfit = Get(id);
            if (outfit.IsOwned(profile) || profile.coins < outfit.Price) return false;
            profile.coins -= outfit.Price;
            if (profile.ownedOutfits == null) profile.ownedOutfits = new System.Collections.Generic.List<int>();
            profile.ownedOutfits.Add(outfit.Id);
            return true;
        }
    }
}
