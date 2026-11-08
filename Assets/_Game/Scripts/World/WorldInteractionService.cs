using System;
using TastePassport.Save;
using TastePassport.Story;

namespace TastePassport.World
{
    public static class WorldInteractionService
    {
        public static WorldInteractionResult Interact(PlayerProfile profile, WorldInteractableDefinition definition)
        {
            if (definition == null || profile == null) return null;
            if (!string.IsNullOrEmpty(definition.RequiredImprintId) &&
                !StoryProgressService.HasImprint(profile, definition.RequiredImprintId)) return null;
            bool firstVisit = !StoryProgressService.HasFlag(profile, definition.StoryFlag);
            string dialogueKey = firstVisit ? definition.FirstDialogueKey : definition.RepeatDialogueKey;
            bool changed = StoryProgressService.RecordInteraction(profile, definition.StoryFlag, definition.ImprintId);
            if (definition.Id == "nhung") changed |= StoryProgressService.AdvanceToChapter(profile, 2);
            else if (definition.Id == "chuacau") changed |= InventoryService.RecordChuaCauDiscovery(profile);
            else if (definition.Id == "hanok") changed |= InventoryService.AddCultureCard(profile, InventoryService.HanokCardId);
            else if (definition.Id == "gyeonggijeon") changed |= InventoryService.AddCultureCard(profile, InventoryService.GyeonggijeonCardId);
            else if (definition.Id == "pansori") changed |= InventoryService.AddCultureCard(profile, InventoryService.PansoriCardId);
            return new WorldInteractionResult(definition, dialogueKey, firstVisit, changed);
        }
    }
}
