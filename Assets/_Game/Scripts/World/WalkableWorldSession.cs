using System;
using System.Collections.Generic;
using TastePassport.Localization;
using TastePassport.Save;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TastePassport.World
{
    public sealed class WalkableWorldSession : MonoBehaviour
    {
        private const float PlayerSpriteScale = 0.22f;
        private const float CameraSize = 3.3f;
        private static readonly Color Ink = new Color(0.12f, 0.16f, 0.17f);
        private static readonly Color Jade = new Color(0.13f, 0.42f, 0.39f);
        private static readonly Color Paper = new Color(0.98f, 0.94f, 0.83f);

        private readonly List<WorldInteractable> targets = new List<WorldInteractable>();
        private PlayerProfile profile;
        private ProfileStore store;
        private WorldDefinition world;
        private GameObject runtimeRoot;
        private GameObject ownedEventSystem;
        private Camera worldCamera;
        private WorldPlayerController player;
        private Text actionLabel;
        private Text dialogueLabel;
        private Text speakerLabel;
        private GameObject dialoguePanel;
        private Button actionButton;
        private WorldTouchStick touchStick;
        private Action onClose;
        private WorldInteractionResult lastInteraction;
        private bool hasCameraSnapshot;
        private bool originalOrthographic;
        private float originalOrthographicSize;
        private Vector3 originalCameraPosition;
        private Quaternion originalCameraRotation;
        private bool ownsCamera;
        private bool isOpen;
        private bool dialogueOpen;

        public bool IsOpen => isOpen;
        public bool LastSaveSucceeded { get; private set; } = true;
        public Vector2 PlayerPosition => player != null ? player.Position : Vector2.zero;
        public string InteractionButtonLabel => actionLabel != null ? actionLabel.text : string.Empty;
        public string CurrentDialogueText => dialogueLabel != null ? dialogueLabel.text : string.Empty;

        public void Open(string worldId, PlayerProfile playerProfile, ProfileStore profileStore, Action closed)
        {
            if (isOpen) throw new InvalidOperationException("A walkable world session is already open");

            profile = playerProfile ?? new PlayerProfile();
            store = profileStore ?? new ProfileStore();
            profile.language = profile.language == "ko" ? "ko" : "vi";
            world = WorldCatalog.Get(worldId);
            onClose = closed;
            lastInteraction = null;
            dialogueOpen = false;
            LastSaveSucceeded = true;
            targets.Clear();
            try
            {
                worldCamera = FindWorldCamera();
                CaptureCamera();
                runtimeRoot = new GameObject("Dau Xu World - " + world.Id);
                Vector2 startPosition = profile.lastWorldId == world.Id
                    ? new Vector2(profile.lastWorldX, profile.lastWorldY)
                    : world.SpawnPoint;

                CreateBackground();
                CreatePlayer(startPosition);
                CreateInteractables();
                CreateHud();
                isOpen = true;
                RefreshActionButton();
                SaveCheckpoint();
            }
            catch
            {
                isOpen = false;
                ReleaseRuntimeObjects();
                onClose = null;
                throw;
            }
        }

        public void Close()
        {
            if (!isOpen) return;
            SaveCheckpoint();
            isOpen = false;
            dialogueOpen = false;
            ReleaseRuntimeObjects();

            Action callback = onClose;
            onClose = null;
            callback?.Invoke();
        }

        public void SetPlayerPosition(Vector2 position)
        {
            if (player == null) return;
            player.SetPosition(position);
            RefreshActionButton();
        }

        public WorldInteractionResult InteractNearest()
        {
            if (!isOpen || player == null) return null;
            if (dialogueOpen)
            {
                DismissDialogue();
                return null;
            }

            WorldInteractable nearest = FindNearestTarget();
            if (nearest == null) return null;

            var candidate = new PlayerProfile();
            candidate.CopyFrom(profile);
            WorldInteractionResult interaction = nearest.Interact(candidate);
            if (interaction == null) return null;
            if (!TryPersistCandidate(candidate))
            {
                lastInteraction = null;
                speakerLabel.text = Localized("dauxu_speaker");
                dialogueLabel.text = Localized("save_error");
                dialoguePanel.SetActive(true);
                dialogueOpen = true;
                RefreshActionButton();
                return null;
            }

            lastInteraction = interaction;
            speakerLabel.text = Localized(nearest.Definition.SpeakerKey);
            dialogueLabel.text = Localized(lastInteraction.DialogueKey);
            dialoguePanel.SetActive(true);
            dialogueOpen = true;
            RefreshActionButton();
            return lastInteraction;
        }

        public void DismissDialogue()
        {
            dialogueOpen = false;
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            RefreshActionButton();
        }

        private void Update()
        {
            if (!isOpen) return;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.backspaceKey.wasPressedThisFrame)
                {
                    Close();
                    return;
                }
                if (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
                    InteractNearest();
            }
            RefreshActionButton();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && isOpen) SaveCheckpoint();
        }

        private void OnApplicationQuit()
        {
            if (isOpen) SaveCheckpoint();
        }

        private void OnDestroy()
        {
            if (!isOpen) return;
            SaveCheckpoint();
            isOpen = false;
            dialogueOpen = false;
            ReleaseRuntimeObjects();
            onClose = null;
        }

        private void ReleaseRuntimeObjects()
        {
            player?.SetTouchInput(Vector2.zero);
            RestoreCamera();
            if (runtimeRoot != null)
            {
                runtimeRoot.SetActive(false);
                DestroyOwnedObject(runtimeRoot);
                runtimeRoot = null;
            }
            if (ownedEventSystem != null)
            {
                DestroyOwnedObject(ownedEventSystem);
                ownedEventSystem = null;
            }
            targets.Clear();
            player = null;
            worldCamera = null;
            actionLabel = null;
            dialogueLabel = null;
            speakerLabel = null;
            dialoguePanel = null;
            actionButton = null;
            touchStick = null;
        }

        private Camera FindWorldCamera()
        {
            Camera camera = Camera.main;
            if (camera == null) camera = FindAnyObjectByType<Camera>();
            if (camera != null) return camera;

            var cameraObject = new GameObject("Dau Xu World Camera");
            cameraObject.tag = "MainCamera";
            ownsCamera = true;
            return cameraObject.AddComponent<Camera>();
        }

        private void CaptureCamera()
        {
            hasCameraSnapshot = worldCamera != null && !ownsCamera;
            if (!hasCameraSnapshot) return;
            originalOrthographic = worldCamera.orthographic;
            originalOrthographicSize = worldCamera.orthographicSize;
            originalCameraPosition = worldCamera.transform.position;
            originalCameraRotation = worldCamera.transform.rotation;
        }

        private void RestoreCamera()
        {
            if (ownsCamera)
            {
                if (worldCamera != null) DestroyOwnedObject(worldCamera.gameObject);
                worldCamera = null;
                ownsCamera = false;
                return;
            }
            if (!hasCameraSnapshot || worldCamera == null) return;
            worldCamera.orthographic = originalOrthographic;
            worldCamera.orthographicSize = originalOrthographicSize;
            worldCamera.transform.position = originalCameraPosition;
            worldCamera.transform.rotation = originalCameraRotation;
            hasCameraSnapshot = false;
        }

        private void CreateBackground()
        {
            Sprite sprite = Resources.Load<Sprite>(world.BackgroundResourcePath);
            var background = new GameObject("Destination Background");
            background.transform.SetParent(runtimeRoot.transform, false);
            background.transform.position = new Vector3(0f, (world.MinimumPosition.y + world.MaximumPosition.y) * 0.5f, 3f);
            var renderer = background.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = -100;
            if (sprite != null)
            {
                float widthScale = (world.MaximumPosition.x - world.MinimumPosition.x) / sprite.bounds.size.x;
                float heightScale = (CameraSize * 2.05f) / sprite.bounds.size.y;
                background.transform.localScale = Vector3.one * Mathf.Max(widthScale, heightScale);
            }
            else
            {
                Debug.LogError("DẤU XỨ: Missing world background resource " + world.BackgroundResourcePath);
            }
        }

        private void CreatePlayer(Vector2 startPosition)
        {
            var playerObject = new GameObject("Player - " + profile.playerName);
            playerObject.transform.SetParent(runtimeRoot.transform, false);
            playerObject.transform.localScale = Vector3.one * PlayerSpriteScale;
            var spriteRenderer = playerObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = Resources.Load<Sprite>("Art/Characters/Traveler");
            spriteRenderer.sortingOrder = 20;
            if (spriteRenderer.sprite == null)
                Debug.LogError("DẤU XỨ: Missing player sprite Resources/Art/Characters/Traveler");
            playerObject.AddComponent<Rigidbody2D>();
            playerObject.AddComponent<CircleCollider2D>();
            player = playerObject.AddComponent<WorldPlayerController>();
            player.Initialize(worldCamera, world, startPosition);
        }

        private void CreateInteractables()
        {
            foreach (WorldInteractableDefinition definition in world.Interactables)
            {
                var targetObject = new GameObject("World Target - " + definition.Id);
                targetObject.transform.SetParent(runtimeRoot.transform, false);
                var target = targetObject.AddComponent<WorldInteractable>();
                target.Configure(definition);
                targets.Add(target);
                if (definition.IsNpc && targetObject.GetComponent<SpriteRenderer>()?.sprite == null)
                    Debug.LogError("DẤU XỨ: Missing character sprite for " + definition.Id);
            }
        }

        private void CreateHud()
        {
            EnsureEventSystem();
            var canvasObject = new GameObject("World HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(runtimeRoot.transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            var topPanel = CreatePanel("World Header", canvasRect, new Vector2(0f, 0.84f), Vector2.one,
                new Color(Paper.r, Paper.g, Paper.b, 0.94f));
            CreateText("World Title", topPanel.transform, new Vector2(0f, 0.35f), new Vector2(0.72f, 0.94f),
                Localized(world.NameKey == "hoian" ? "world_hoian_title" : "world_jeonju_title"), 37, Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            CreateText("Movement Hint", topPanel.transform, new Vector2(0f, 0.04f), new Vector2(0.72f, 0.55f),
                Localized("world_move_hint"), 23, Ink, TextAnchor.MiddleLeft, FontStyle.Normal);
            CreateButton(topPanel.transform, "World Back", Localized("world_back"),
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -18f), new Vector2(270f, 82f), Close, Paper, Ink, 25);

            CreateTouchStick(canvasRect);
            actionButton = CreateButton(canvasRect, "World Interaction", Localized("world_approach"),
                Vector2.zero, Vector2.zero, new Vector2(1640f, 145f), new Vector2(380f, 116f), OnActionButton, Jade, Color.white, 28);
            RectTransform actionRect = actionButton.GetComponent<RectTransform>();
            actionRect.anchorMin = new Vector2(1f, 0f);
            actionRect.anchorMax = new Vector2(1f, 0f);
            actionRect.pivot = new Vector2(1f, 0f);
            actionRect.anchoredPosition = new Vector2(-36f, 36f);
            actionLabel = actionButton.GetComponentInChildren<Text>();

            dialoguePanel = CreatePanel("World Dialogue", canvasRect, new Vector2(0.17f, 0.035f), new Vector2(0.83f, 0.29f),
                new Color(Paper.r, Paper.g, Paper.b, 0.97f));
            dialoguePanel.SetActive(false);
            speakerLabel = CreateText("Dialogue Speaker", dialoguePanel.transform, new Vector2(0.035f, 0.66f), new Vector2(0.97f, 0.97f),
                string.Empty, 26, Jade, TextAnchor.MiddleLeft, FontStyle.Bold);
            dialogueLabel = CreateText("Dialogue Copy", dialoguePanel.transform, new Vector2(0.035f, 0.06f), new Vector2(0.97f, 0.70f),
                string.Empty, 26, Ink, TextAnchor.MiddleLeft, FontStyle.Normal);
        }

        private void CreateTouchStick(RectTransform parent)
        {
            GameObject stickObject = CreatePanel("Touch Movement Stick", parent, Vector2.zero, Vector2.zero,
                new Color(Jade.r, Jade.g, Jade.b, 0.42f));
            RectTransform stickRect = stickObject.GetComponent<RectTransform>();
            stickRect.anchorMin = Vector2.zero;
            stickRect.anchorMax = Vector2.zero;
            stickRect.pivot = Vector2.zero;
            stickRect.anchoredPosition = new Vector2(36f, 36f);
            stickRect.sizeDelta = new Vector2(216f, 216f);
            var stickImage = stickObject.GetComponent<Image>();
            stickImage.raycastTarget = true;
            var stick = stickObject.AddComponent<WorldTouchStick>();
            var handle = CreatePanel("Touch Stick Handle", stickRect, new Vector2(0.30f, 0.30f), new Vector2(0.70f, 0.70f),
                new Color(Paper.r, Paper.g, Paper.b, 0.94f));
            handle.GetComponent<Image>().raycastTarget = false;
            stick.Initialize(handle.GetComponent<RectTransform>(), player.SetTouchInput);
            touchStick = stick;
        }

        private void EnsureEventSystem()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                ownedEventSystem = new GameObject("World Event System", typeof(EventSystem));
                eventSystem = ownedEventSystem.GetComponent<EventSystem>();
            }
            if (eventSystem.GetComponent<BaseInputModule>() == null)
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        private WorldInteractable FindNearestTarget()
        {
            if (player == null) return null;
            WorldInteractable nearest = null;
            float nearestDistance = float.MaxValue;
            foreach (WorldInteractable target in targets)
            {
                if (target == null || !target.IsWithinRange(player.Position)) continue;
                float distance = Vector2.Distance(player.Position, target.Definition.Position);
                if (distance < nearestDistance)
                {
                    nearest = target;
                    nearestDistance = distance;
                }
            }
            return nearest;
        }

        private void RefreshActionButton()
        {
            if (actionButton == null || actionLabel == null) return;
            if (dialogueOpen)
            {
                actionLabel.text = Localized("world_continue");
                actionButton.interactable = true;
                return;
            }
            WorldInteractable nearest = FindNearestTarget();
            bool canInteract = nearest != null;
            actionLabel.text = canInteract
                ? Localized("world_action") + " · " + Localized(nearest.Definition.NameKey)
                : Localized("world_approach");
            actionButton.interactable = canInteract;
        }

        private void OnActionButton()
        {
            if (dialogueOpen) DismissDialogue();
            else InteractNearest();
        }

        private Button CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPosition, Vector2 size, Action clicked, Color background, Color foreground, int fontSize)
        {
            GameObject buttonObject = CreatePanel(name, parent, anchorMin, anchorMax, background);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.pivot = anchorMin == anchorMax ? anchorMin : rect.pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            button.onClick.AddListener(() => clicked?.Invoke());
            CreateText("Label", buttonObject.transform, Vector2.zero, Vector2.one, label, fontSize, foreground, TextAnchor.MiddleCenter, FontStyle.Bold);
            return button;
        }

        private GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            var rect = panelObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = panelObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return panelObject;
        }

        private Text CreateText(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            string value, int size, Color color, TextAnchor alignment, FontStyle style)
        {
            var textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(14f, 4f);
            rect.offsetMax = new Vector2(-14f, -4f);
            var text = textObject.GetComponent<Text>();
            text.font = Resources.Load<Font>("TastePassportFont");
            if (text.font == null) text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.fontStyle = style;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private string Localized(string key) => GameText.Get(profile != null ? profile.language : "vi", key,
            profile != null ? profile.playerName : null);

        private bool SaveCheckpoint()
        {
            if (profile == null || store == null || world == null)
            {
                LastSaveSucceeded = false;
                return false;
            }

            var candidate = new PlayerProfile();
            candidate.CopyFrom(profile);
            return TryPersistCandidate(candidate);
        }

        private bool TryPersistCandidate(PlayerProfile candidate)
        {
            if (candidate == null || profile == null || store == null || world == null)
            {
                LastSaveSucceeded = false;
                return false;
            }

            candidate.lastWorldId = world.Id;
            if (player != null)
            {
                Vector2 position = player.Position;
                candidate.lastWorldX = position.x;
                candidate.lastWorldY = position.y;
            }
            if (!store.TrySave(candidate))
            {
                LastSaveSucceeded = false;
                Debug.LogError("DẤU XỨ: Failed to save the current world checkpoint.");
                return false;
            }

            profile.CopyFrom(candidate);
            LastSaveSucceeded = true;
            return true;
        }

        private static void DestroyOwnedObject(GameObject target)
        {
            if (target == null) return;
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }
    }
}
