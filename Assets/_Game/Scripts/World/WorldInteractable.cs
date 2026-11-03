using TastePassport.Save;
using TastePassport.Story;
using TastePassport.UI;
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
        private WorldInteractableDefinition definition;
        private WorldDioramaBuilder townVisual;
        private Transform questMarker;
        private Vector3 questMarkerBasePosition;

        public WorldInteractableDefinition Definition => definition;

        private void Update()
        {
            if (questMarker == null || !questMarker.gameObject.activeSelf) return;
            Vector3 position = questMarkerBasePosition;
            position.y += Mathf.Sin(Time.unscaledTime * 4f) * 0.08f;
            questMarker.localPosition = position;
        }

        public void Configure(WorldInteractableDefinition target)
        {
            Configure(target, null);
        }

        public void Configure(WorldInteractableDefinition target, WorldDioramaBuilder townBuilder)
        {
            if (target == null)
            {
                Debug.LogWarning("Cannot configure a world target without a definition.", this);
                return;
            }
            definition = target;
            townVisual = townBuilder;
            gameObject.name = "World Target - " + target.Id;
            transform.position = target.Position;

            var circle = GetComponent<CircleCollider2D>();
            circle.isTrigger = true;
            circle.radius = target.IsNpc ? 0.55f : target.InteractionRadius;

            // Legacy character and landmark artwork is kept for dialogue portraits and print surfaces.
            // Nothing in the walkable world is rendered as a sprite after the 3D scene conversion.
            foreach (SpriteRenderer sprite in GetComponentsInChildren<SpriteRenderer>(true))
                sprite.enabled = false;

            float interactionScale = target.IsNpc ? 0.22f :
                string.IsNullOrEmpty(target.SpriteResourcePath) ? 0.48f : 0.14f;
            transform.localScale = Vector3.one * interactionScale;
            if (target.IsNpc)
            {
                if (WorldCharacterModelBuilder.Create(transform, target.Id, false) == null)
                    Debug.LogError("DẤU XỨ: Could not build the 3D NPC model for " + target.Id + ".", this);
            }
        }

        public void SetQuestHighlighted(bool highlighted)
        {
            if (!highlighted)
            {
                if (questMarker != null) questMarker.gameObject.SetActive(false);
                return;
            }

            if (questMarker == null)
            {
                var markerObject = new GameObject("Quest Marker - " + (definition == null ? "target" : definition.Id));
                questMarker = markerObject.transform;
                questMarker.SetParent(transform, false);
                float parentScale = Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.y));
                questMarker.localScale = Vector3.one / parentScale;
                questMarkerBasePosition = new Vector3(0f, 1.25f / parentScale, -0.5f);
                questMarker.localPosition = questMarkerBasePosition;

                var markerText = markerObject.AddComponent<TextMesh>();
                markerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                markerText.text = "!";
                markerText.fontSize = 96;
                markerText.characterSize = 0.55f;
                markerText.anchor = TextAnchor.MiddleCenter;
                markerText.alignment = TextAlignment.Center;
                markerText.color = GameUiTheme.Lantern;
                var meshRenderer = markerObject.GetComponent<MeshRenderer>();
                meshRenderer.sortingOrder = 40;
            }

            questMarker.gameObject.SetActive(true);
        }

        public void ApplyLanternColor(string colorId)
        {
            if (definition == null || definition.Id != "lantern-display") return;
            townVisual?.ApplyLanternColor(colorId);
        }

        public void ApplyHanjiVariant(string shapeId, string colorId)
        {
            if (definition == null || definition.Id != "hanji-display") return;
            townVisual?.ApplyHanjiVariant(shapeId, colorId);
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
            if (definition.Id == "nhung") changed |= StoryProgressService.AdvanceToChapter(profile, 2);
            else if (definition.Id == "chuacau") changed |= InventoryService.RecordChuaCauDiscovery(profile);
            else if (definition.Id == "hanok") changed |= InventoryService.AddCultureCard(profile, InventoryService.HanokCardId);
            else if (definition.Id == "gyeonggijeon") changed |= InventoryService.AddCultureCard(profile, InventoryService.GyeonggijeonCardId);
            else if (definition.Id == "pansori") changed |= InventoryService.AddCultureCard(profile, InventoryService.PansoriCardId);
            return new WorldInteractionResult(definition, dialogueKey, firstVisit, changed);
        }
    }
}
