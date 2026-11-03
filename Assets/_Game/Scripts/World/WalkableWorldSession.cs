using System;
using System.Collections.Generic;
using TastePassport.Localization;
using TastePassport.Save;
using TastePassport.Story;
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
        private Text objectiveLabel;
        private Text objectiveDescriptionLabel;
        private Image dialoguePortrait;
        private GameObject dialoguePanel;
        private GameObject controlsGuidePanel;
        private GameObject questJournalPanel;
        private Button actionButton;
        private WorldTouchStick touchStick;
        private Action onClose;
        private Action<string> onActivityRequested;
        private Action onPassportRequested;
        private WorldInteractionResult lastInteraction;
        private WorldObjectiveDefinition currentObjective;
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
        public string CurrentObjectiveKey => currentObjective != null ? currentObjective.TitleKey : string.Empty;
        public string QuestTargetId => currentObjective != null ? currentObjective.TargetId : null;

        public void Open(string worldId, PlayerProfile playerProfile, ProfileStore profileStore, Action closed)
        {
            Open(worldId, playerProfile, profileStore, closed, null);
        }

        public void Open(string worldId, PlayerProfile playerProfile, ProfileStore profileStore, Action closed,
            Action<string> activityRequested)
        {
            Open(worldId, playerProfile, profileStore, closed, activityRequested, null);
        }

        public void Open(string worldId, PlayerProfile playerProfile, ProfileStore profileStore, Action closed,
            Action<string> activityRequested, Action passportRequested)
        {
            if (isOpen) throw new InvalidOperationException("A walkable world session is already open");

            profile = playerProfile ?? new PlayerProfile();
            store = profileStore ?? new ProfileStore();
            profile.language = profile.language == "ko" ? "ko" : "vi";
            world = WorldCatalog.Get(worldId);
            onClose = closed;
            onActivityRequested = activityRequested;
            onPassportRequested = passportRequested;
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
                CreateStreetCollision();
                CreatePlayer(startPosition);
                CreateInteractables();
                CreateHud();
                isOpen = true;
                RefreshCurrentObjective();
                RefreshActionButton();
                SaveCheckpoint();
            }
            catch
            {
                isOpen = false;
                ReleaseRuntimeObjects();
                onClose = null;
                onActivityRequested = null;
                onPassportRequested = null;
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
            onActivityRequested = null;
            onPassportRequested = null;
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
                SetDialoguePortrait(null);
                dialogueLabel.text = Localized("save_error");
                dialoguePanel.SetActive(true);
                dialogueOpen = true;
                RefreshActionButton();
                return null;
            }

            lastInteraction = interaction;
            speakerLabel.text = Localized(nearest.Definition.SpeakerKey);
            SetDialoguePortrait(nearest.Definition.SpriteResourcePath);
            dialogueLabel.text = Localized(lastInteraction.DialogueKey);
            dialoguePanel.SetActive(true);
            dialogueOpen = true;
            player.SetMovementEnabled(false);
            RefreshCurrentObjective();
            RefreshActionButton();
            return lastInteraction;
        }

        public void DismissDialogue()
        {
            dialogueOpen = false;
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            player?.SetMovementEnabled(true);
            RefreshActionButton();
        }

        private void Update()
        {
            if (!isOpen) return;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.backspaceKey.wasPressedThisFrame)
                {
                    if (controlsGuidePanel != null && controlsGuidePanel.activeSelf)
                    {
                        CloseControlsGuide();
                        return;
                    }
                    if (questJournalPanel != null && questJournalPanel.activeSelf)
                    {
                        questJournalPanel.SetActive(false);
                        return;
                    }
                    if (dialogueOpen)
                    {
                        DismissDialogue();
                        return;
                    }
                    Close();
                    return;
                }
                if (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
                    OnActionButton();
            }
            UpdateMovementObjective();
            RefreshCurrentObjective();
            RefreshActionButton();
        }

        public void OnApplicationPause(bool paused)
        {
            if (paused && isOpen) SaveCheckpoint();
        }

        public void OnApplicationQuit()
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
            onActivityRequested = null;
            onPassportRequested = null;
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
            objectiveLabel = null;
            objectiveDescriptionLabel = null;
            dialoguePortrait = null;
            dialoguePanel = null;
            controlsGuidePanel = null;
            questJournalPanel = null;
            actionButton = null;
            touchStick = null;
            currentObjective = null;
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

        private void CreateStreetCollision()
        {
            // The destination art is a painted backdrop. These hidden static edges turn its visible
            // building fronts and foreground planters into a simple, continuous walkable lane.
            CreateStreetBarrier("Collision - North Street Houses", new Vector2(0f, -1.02f), new Vector2(20f, 0.20f));
            CreateStreetBarrier("Collision - South Street Edge", new Vector2(0f, -2.55f), new Vector2(20f, 0.20f));
        }

        private void CreateStreetBarrier(string objectName, Vector2 position, Vector2 size)
        {
            var barrier = new GameObject(objectName);
            barrier.transform.SetParent(runtimeRoot.transform, false);
            barrier.transform.position = position;
            var collider = barrier.AddComponent<BoxCollider2D>();
            collider.size = size;
        }

        private void SetDialoguePortrait(string resourcePath)
        {
            if (dialoguePortrait == null) return;
            dialoguePortrait.sprite = string.IsNullOrEmpty(resourcePath) ? null : Resources.Load<Sprite>(resourcePath);
            dialoguePortrait.enabled = dialoguePortrait.sprite != null;
        }

        private void CreatePlayer(Vector2 startPosition)
        {
            var playerObject = new GameObject("Player - " + profile.playerName);
            playerObject.transform.SetParent(runtimeRoot.transform, false);
            var visualObject = new GameObject("Character Visual");
            visualObject.transform.SetParent(playerObject.transform, false);
            visualObject.transform.localScale = Vector3.one * PlayerSpriteScale;
            var spriteRenderer = visualObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = Resources.Load<Sprite>("Art/Characters/Traveler");
            spriteRenderer.sortingOrder = 20;
            if (spriteRenderer.sprite == null)
                Debug.LogError("DẤU XỨ: Missing player sprite Resources/Art/Characters/Traveler");
            playerObject.AddComponent<Rigidbody2D>();
            playerObject.AddComponent<CircleCollider2D>();
            player = playerObject.AddComponent<WorldPlayerController>();
            player.Initialize(worldCamera, world, startPosition);
            player.SetVisual(spriteRenderer);
        }

        private void CreateInteractables()
        {
            foreach (WorldInteractableDefinition definition in world.Interactables)
            {
                if (!string.IsNullOrEmpty(definition.RequiredImprintId) &&
                    !StoryProgressService.HasImprint(profile, definition.RequiredImprintId)) continue;
                var targetObject = new GameObject("World Target - " + definition.Id);
                targetObject.transform.SetParent(runtimeRoot.transform, false);
                var target = targetObject.AddComponent<WorldInteractable>();
                target.Configure(definition);
                if (definition.Id == "lantern-display")
                    target.ApplyLanternColor(InventoryService.GetLanternColorId(InventoryService.GetCraftedLantern(profile)));
                else if (definition.Id == "hanji-display")
                {
                    string hanji = InventoryService.GetLatestCraftedHanji(profile);
                    target.ApplyHanjiVariant(InventoryService.GetHanjiShapeId(hanji), InventoryService.GetHanjiColorId(hanji));
                }
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
            scaler.referenceResolution = new Vector2(720f, 1280f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            var safeAreaObject = new GameObject("World Safe Area", typeof(RectTransform));
            RectTransform safeRect = safeAreaObject.GetComponent<RectTransform>();
            safeRect.SetParent(canvasRect, false);
            ApplySafeArea(safeRect);

            var topPanel = CreatePanel("World Header", safeRect, new Vector2(0f, 0.84f), Vector2.one,
                new Color(Paper.r, Paper.g, Paper.b, 0.94f));
            CreateText("World Title", topPanel.transform, new Vector2(0f, 0.60f), new Vector2(0.58f, 1f),
                Localized(world.NameKey == "hoian" ? "world_hoian_title" : "world_jeonju_title"), 31, Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            CreateText("Movement Hint", topPanel.transform, new Vector2(0f, 0.36f), new Vector2(0.62f, 0.68f),
                Localized("world_move_hint"), 18, Ink, TextAnchor.MiddleLeft, FontStyle.Normal);
            objectiveLabel = CreateText("World Objective Label", topPanel.transform, new Vector2(0f, 0.02f), new Vector2(0.58f, 0.42f),
                string.Empty, 22, Jade, TextAnchor.MiddleLeft, FontStyle.Bold);
            CreateButton(topPanel.transform, "World Back", Localized("world_back"),
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-8f, -8f), new Vector2(140f, 62f), Close, Paper, Ink, 18);
            CreateButton(topPanel.transform, "World Controls Help", "?",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-156f, -8f), new Vector2(62f, 62f), OpenControlsGuide,
                new Color(0.92f, 0.63f, 0.21f), Ink, 26);
            CreateButton(topPanel.transform, "World Quest Journal", Localized("world_journal"),
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-8f, 7f), new Vector2(136f, 56f), ToggleQuestJournal, Jade, Color.white, 17);
            CreateButton(topPanel.transform, "World Passport", Localized("passport"),
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-152f, 7f), new Vector2(136f, 56f),
                () => onPassportRequested?.Invoke(), Paper, Ink, 17);

            CreateTouchStick(safeRect);
            actionButton = CreateButton(safeRect, "World Interaction", Localized("world_approach"),
                Vector2.zero, Vector2.zero, new Vector2(0f, 0f), new Vector2(236f, 92f), OnActionButton, Jade, Color.white, 20);
            RectTransform actionRect = actionButton.GetComponent<RectTransform>();
            actionRect.anchorMin = new Vector2(1f, 0f);
            actionRect.anchorMax = new Vector2(1f, 0f);
            actionRect.pivot = new Vector2(1f, 0f);
            actionRect.anchoredPosition = new Vector2(-24f, 30f);
            actionLabel = actionButton.GetComponentInChildren<Text>();

            dialoguePanel = CreatePanel("World Dialogue", safeRect, new Vector2(0.06f, 0.25f), new Vector2(0.94f, 0.43f),
                new Color(Paper.r, Paper.g, Paper.b, 0.97f));
            dialoguePanel.SetActive(false);
            var portraitObject = new GameObject("Dialogue Portrait", typeof(RectTransform), typeof(Image));
            portraitObject.transform.SetParent(dialoguePanel.transform, false);
            RectTransform portraitRect = portraitObject.GetComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0.025f, 0.22f);
            portraitRect.anchorMax = new Vector2(0.20f, 0.82f);
            portraitRect.offsetMin = Vector2.zero;
            portraitRect.offsetMax = Vector2.zero;
            dialoguePortrait = portraitObject.GetComponent<Image>();
            dialoguePortrait.preserveAspect = true;
            dialoguePortrait.raycastTarget = false;
            dialoguePortrait.enabled = false;
            speakerLabel = CreateText("Dialogue Speaker", dialoguePanel.transform, new Vector2(0.22f, 0.66f), new Vector2(0.97f, 0.97f),
                string.Empty, 22, Jade, TextAnchor.MiddleLeft, FontStyle.Bold);
            dialogueLabel = CreateText("Dialogue Copy", dialoguePanel.transform, new Vector2(0.22f, 0.06f), new Vector2(0.97f, 0.70f),
                string.Empty, 20, Ink, TextAnchor.MiddleLeft, FontStyle.Normal);

            CreateQuestJournal(safeRect);
            CreateControlsGuide(safeRect);
        }

        private void ApplySafeArea(RectTransform safeRect)
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            Rect safeArea = Screen.safeArea;
            safeRect.anchorMin = new Vector2(safeArea.xMin / Screen.width, safeArea.yMin / Screen.height);
            safeRect.anchorMax = new Vector2(safeArea.xMax / Screen.width, safeArea.yMax / Screen.height);
            safeRect.offsetMin = Vector2.zero;
            safeRect.offsetMax = Vector2.zero;
        }

        private void CreateQuestJournal(RectTransform parent)
        {
            questJournalPanel = CreatePanel("World Quest Journal Panel", parent, new Vector2(0.08f, 0.43f),
                new Vector2(0.92f, 0.65f), new Color(Paper.r, Paper.g, Paper.b, 0.98f));
            CreateText("World Quest Journal Title", questJournalPanel.transform, new Vector2(0.03f, 0.68f),
                new Vector2(0.96f, 0.96f), Localized("world_journal"), 24, Jade, TextAnchor.MiddleLeft, FontStyle.Bold);
            objectiveDescriptionLabel = CreateText("World Quest Journal Objective", questJournalPanel.transform,
                new Vector2(0.03f, 0.10f), new Vector2(0.96f, 0.72f), string.Empty, 20, Ink, TextAnchor.MiddleLeft, FontStyle.Normal);
            CreateButton(questJournalPanel.transform, "Close World Quest Journal", "×",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-22f, -22f), new Vector2(48f, 48f),
                ToggleQuestJournal, Paper, Ink, 24);
            questJournalPanel.SetActive(false);
        }

        private void CreateControlsGuide(RectTransform parent)
        {
            controlsGuidePanel = CreatePanel("World Controls Guide", parent, new Vector2(0.08f, 0.39f),
                new Vector2(0.92f, 0.67f), new Color(Paper.r, Paper.g, Paper.b, 0.98f));
            CreateText("World Controls Guide Title", controlsGuidePanel.transform, new Vector2(0.04f, 0.68f),
                new Vector2(0.96f, 0.96f), Localized("world_controls_title"), 25, Jade, TextAnchor.MiddleCenter, FontStyle.Bold);
            CreateText("World Controls Guide Copy", controlsGuidePanel.transform, new Vector2(0.05f, 0.28f),
                new Vector2(0.95f, 0.72f), Localized("world_controls_copy"), 20, Ink, TextAnchor.MiddleCenter, FontStyle.Normal);
            CreateButton(controlsGuidePanel.transform, "Close World Controls Guide", Localized("world_controls_close"),
                new Vector2(0.22f, 0.03f), new Vector2(0.78f, 0.30f), Vector2.zero, Vector2.zero,
                CloseControlsGuide, Jade, Color.white, 19);
            controlsGuidePanel.SetActive(!StoryProgressService.HasFlag(profile, "world-controls-guide-seen"));
        }

        private void OpenControlsGuide()
        {
            if (controlsGuidePanel != null) controlsGuidePanel.SetActive(true);
        }

        private void CloseControlsGuide()
        {
            var candidate = new PlayerProfile();
            candidate.CopyFrom(profile);
            StoryProgressService.AddFlag(candidate, "world-controls-guide-seen");
            if (!TryPersistCandidate(candidate))
            {
                if (objectiveDescriptionLabel != null) objectiveDescriptionLabel.text = Localized("save_error");
                return;
            }
            if (controlsGuidePanel != null) controlsGuidePanel.SetActive(false);
        }

        private void ToggleQuestJournal()
        {
            if (questJournalPanel != null) questJournalPanel.SetActive(!questJournalPanel.activeSelf);
        }

        private void UpdateMovementObjective()
        {
            if (world == null || world.Id != "hoian" || player == null || !player.HasMoved ||
                !LastSaveSucceeded || StoryProgressService.HasFlag(profile, "tutorial-nhung-reached")) return;

            WorldInteractable nhung = targets.Find(target => target != null && target.Definition.Id == "nhung");
            if (nhung == null || !nhung.IsWithinRange(player.Position)) return;

            var candidate = new PlayerProfile();
            candidate.CopyFrom(profile);
            StoryProgressService.AddFlag(candidate, "tutorial-nhung-reached");
            if (TryPersistCandidate(candidate)) RefreshCurrentObjective();
        }

        private void RefreshCurrentObjective()
        {
            if (world == null || profile == null) return;
            WorldObjectiveDefinition next = WorldObjectiveService.GetCurrent(world.Id, profile);
            if (next == null) return;

            bool changed = currentObjective == null || currentObjective.Id != next.Id;
            currentObjective = next;
            if (objectiveLabel != null && (changed || objectiveLabel.text != Localized(next.TitleKey)))
                objectiveLabel.text = Localized(next.TitleKey);
            if (objectiveDescriptionLabel != null && (changed || objectiveDescriptionLabel.text != Localized(next.DescriptionKey)))
                objectiveDescriptionLabel.text = Localized(next.DescriptionKey);

            if (changed)
                foreach (WorldInteractable target in targets)
                    if (target != null) target.SetQuestHighlighted(target.Definition.Id == next.TargetId);
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
                string activityId = lastInteraction == null || lastInteraction.Target == null ? null : lastInteraction.Target.ActivityId;
                actionLabel.text = string.IsNullOrEmpty(activityId)
                    ? Localized("world_continue")
                    : Localized("world_start_activity") + " · " + Localized(activityId == "lantern-craft"
                        ? "activity_lantern" : activityId == "hanji-craft" ? "activity_hanji" : "world_continue");
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

        private void OnActionButton() => ContinueDialogue();

        public void ContinueDialogue()
        {
            if (!dialogueOpen)
            {
                InteractNearest();
                return;
            }
            if (dialogueOpen)
            {
                string activityId = lastInteraction == null || lastInteraction.Target == null ? null : lastInteraction.Target.ActivityId;
                if (!string.IsNullOrEmpty(activityId))
                {
                    Action<string> callback = onActivityRequested;
                    Close();
                    callback?.Invoke(activityId);
                }
                else DismissDialogue();
            }
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
