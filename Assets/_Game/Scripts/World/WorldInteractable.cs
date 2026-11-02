using TastePassport.Save;
using TastePassport.Story;
using UnityEngine;

namespace TastePassport.World
{
    public sealed class WorldInteractionResult
    {
        public readonly WorldInteractableDefinition Target;
        public readonly string DialogueKey;
        public readonly bool IsFirstVisit;
        public readonly bool StateChanged;

        public WorldInteractionResult(WorldInteractableDefinition target, string dialogueKey, bool isFirstVisit, bool stateChanged)
        {
            Target = target;
            DialogueKey = dialogueKey;
            IsFirstVisit = isFirstVisit;
            StateChanged = stateChanged;
        }
    }

    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class WorldInteractable : MonoBehaviour
    {
        private static Sprite markerSprite;
        private WorldInteractableDefinition definition;

        public WorldInteractableDefinition Definition => definition;

        public void Configure(WorldInteractableDefinition target)
        {
            definition = target;
            gameObject.name = "World Target - " + target.Id;
            transform.position = target.Position;

            var circle = GetComponent<CircleCollider2D>();
            circle.isTrigger = true;
            circle.radius = target.IsNpc ? 0.55f : target.InteractionRadius;

            var renderer = GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = gameObject.AddComponent<SpriteRenderer>();
            if (!string.IsNullOrEmpty(target.SpriteResourcePath))
            {
                renderer.sprite = Resources.Load<Sprite>(target.SpriteResourcePath);
                renderer.sortingOrder = target.IsNpc ? 10 : 9;
                transform.localScale = Vector3.one * (target.IsNpc ? 0.22f : 0.14f);
                renderer.color = Color.white;
            }
            else
            {
                renderer.sprite = GetMarkerSprite();
                renderer.color = target.Id == "chuacau" ? new Color(0.96f, 0.69f, 0.24f) : new Color(0.25f, 0.54f, 0.54f);
                renderer.sortingOrder = 8;
                transform.localScale = Vector3.one * 0.48f;
            }
        }

        public void ApplyLanternColor(string colorId)
        {
            if (definition == null || definition.Id != "lantern-display") return;
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer == null) return;
            if (colorId == "red") renderer.color = new Color(1f, 0.91f, 0.89f);
            else if (colorId == "blue") renderer.color = new Color(0.88f, 0.94f, 1f);
            else if (colorId == "green") renderer.color = new Color(0.90f, 0.96f, 0.87f);
            else if (colorId == "gold") renderer.color = new Color(1f, 0.96f, 0.82f);
        }

        public bool IsWithinRange(Vector2 position)
        {
            return definition != null && Vector2.Distance(position, definition.Position) <= definition.InteractionRadius;
        }

        public string ResolveDialogueKey(PlayerProfile profile)
        {
            if (definition == null) return string.Empty;
            return StoryProgressService.HasFlag(profile, definition.StoryFlag)
                ? definition.RepeatDialogueKey
                : definition.FirstDialogueKey;
        }

        public WorldInteractionResult Interact(PlayerProfile profile)
        {
            if (definition == null || profile == null) return null;
            bool firstVisit = !StoryProgressService.HasFlag(profile, definition.StoryFlag);
            string dialogueKey = firstVisit ? definition.FirstDialogueKey : definition.RepeatDialogueKey;
            bool changed = StoryProgressService.RecordInteraction(profile, definition.StoryFlag, definition.ImprintId);
            if (definition.Id == "chuacau") changed |= InventoryService.RecordChuaCauDiscovery(profile);
            return new WorldInteractionResult(definition, dialogueKey, firstVisit, changed);
        }

        private static Sprite GetMarkerSprite()
        {
            if (markerSprite != null) return markerSprite;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "World Point of Interest Marker";
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float outer = size * 0.47f;
            float inner = size * 0.31f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    byte alpha = distance <= outer && distance >= inner ? (byte)255 : (byte)0;
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            markerSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            markerSprite.name = "World Point of Interest Marker";
            return markerSprite;
        }
    }
}
