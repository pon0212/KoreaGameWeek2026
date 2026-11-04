using System;
using System.Collections.Generic;
using TastePassport.Localization;
using TastePassport.Save;
using TastePassport.Story;
using TastePassport.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TastePassport.World
{
    public sealed class WalkableWorldSession : MonoBehaviour
    {
        private const float CharacterVisualScale = 0.22f;
        private static readonly Color Ink = GameUiTheme.TextPrimary;
        private static readonly Color Jade = GameUiTheme.Primary;
        private static readonly Color Paper = GameUiTheme.Surface;

        private readonly List<WorldInteractable> targets = new List<WorldInteractable>();
        private PlayerProfile profile;
        private ProfileStore store;
        private WorldDefinition world;
        private GameObject runtimeRoot;
        private GameObject ownedEventSystem;
        private Camera worldCamera;
        private Canvas worldHudCanvas;
        private WorldPlayerController player;
        private WorldDioramaBuilder townDiorama;
        private Text actionLabel;
        private Text dialogueLabel;
        private Text speakerLabel;
        private Text objectiveLabel;
        private Text objectiveDescriptionLabel;
        private Text journalObjectiveLabel;
        private Text journalProgressLabel;
        private Text dialogueObjectiveLabel;
        private Text controlsObjectiveLabel;
        private Image dialoguePortrait;
        private GameObject dialoguePanel;
        private GameObject controlsGuidePanel;
        private GameObject questJournalPanel;
        private Button actionButton;
        private Button dialogueContinueButton;
        private Button dialogueNarrationButton;
        private Button optionalDialogueButton;
        private Button dialogueActivityButton;
        private Button controlsGuideCloseButton;
        private Text dialogueKindLabel;
        private Text controlsGuideLabel;
        private RectTransform dialogueCardRect;
        private RectTransform journalCardRect;
        private RectTransform controlsCardRect;
        private RectTransform navigationRect;
        private CanvasGroup touchStickGroup;
        private CanvasGroup headerGroup;
        private RectTransform safeAreaRect;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;
        private bool showingOptionalDialogue;
        private bool controlsGuideSaveFailed;
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
        // Retain the programmatic status contract used by existing world integrations.
        // Visible controls use named talk/discovery prompts and separate dialogue actions.
        public string InteractionButtonLabel
        {
            get
            {
                if (actionLabel == null) return string.Empty;
                if (dialogueOpen)
                {
                    string activityId = lastInteraction?.Target?.ActivityId;
                    return string.IsNullOrEmpty(activityId) ? Localized("world_continue") :
                        Localized("world_start_activity") + " · " + Localized(activityId == "lantern-craft"
                            ? "activity_lantern" : activityId == "hanji-craft" ? "activity_hanji" : "world_continue");
                }
                WorldInteractable nearest = FindNearestTarget();
                return nearest != null ? Localized("world_action") + " · " + Localized(nearest.Definition.NameKey)
                    : Localized("world_approach");
            }
        }
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

                townDiorama = WorldDioramaBuilder.Build(runtimeRoot.transform, world);
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
            StoryNarration.Stop();
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
                OpenDialogue(null);
                RefreshActionButton();
                return null;
            }

            lastInteraction = interaction;
            speakerLabel.text = Localized(nearest.Definition.IsNpc
                ? nearest.Definition.SpeakerKey : nearest.Definition.NameKey);
            SetDialoguePortrait(nearest.Definition.IsNpc ? nearest.Definition.SpriteResourcePath : null);
            dialogueLabel.text = Localized(lastInteraction.DialogueKey);
            OpenDialogue(nearest.Definition);
            RefreshCurrentObjective();
            RefreshActionButton();
            return lastInteraction;
        }

        public void DismissDialogue()
        {
            StoryNarration.Stop();
            dialogueOpen = false;
            showingOptionalDialogue = false;
            lastInteraction = null;
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            RefreshMovementAvailability();
            RefreshActionButton();
        }

        private void Update()
        {
            if (!isOpen) return;
            if (safeAreaRect != null && (lastSafeArea != Screen.safeArea ||
                lastScreenSize != new Vector2Int(Screen.width, Screen.height)))
            {
                ApplySafeArea(safeAreaRect);
                WorldSpaceCanvasFitter.Configure(worldHudCanvas, worldCamera);
                LayoutOverlayCards();
            }
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.escapeKey.wasPressedThisFrame || keyboard.backspaceKey.wasPressedThisFrame)
                {
                    if (controlsGuidePanel != null && controlsGuidePanel.activeSelf) CloseControlsGuide();
                    else if (questJournalPanel != null && questJournalPanel.activeSelf) ToggleQuestJournal();
                    else if (dialogueOpen) DismissDialogue();
                    else Close();
                    return;
                }
                if (IsUtilityPanelOpen())
                {
                    if (keyboard.eKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                    {
                        if (controlsGuidePanel.activeSelf) CloseControlsGuide();
                        else ToggleQuestJournal();
                    }
                    return;
                }
                if (keyboard.eKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                {
                    InvokeVisibleButton(dialogueOpen ? dialogueContinueButton : actionButton);
                    return;
                }
                if (dialogueOpen)
                {
                    if (keyboard.qKey.wasPressedThisFrame) InvokeVisibleButton(optionalDialogueButton);
                    if (keyboard.fKey.wasPressedThisFrame) InvokeVisibleButton(dialogueActivityButton);
                    return;
                }
                if (keyboard.hKey.wasPressedThisFrame) OpenControlsGuide();
                else if (keyboard.jKey.wasPressedThisFrame) ToggleQuestJournal();
                else if (keyboard.pKey.wasPressedThisFrame) onPassportRequested?.Invoke();
            }
            UpdateMovementObjective();
            RefreshCurrentObjective();
            RefreshActionButton();
        }

        private void LateUpdate()
        {
            // The gameplay camera follows the player; keep its fitted world-space HUD attached
            // to the same viewport without changing the camera or parenting the HUD to it.
            if (isOpen && worldHudCanvas != null && worldCamera != null)
                WorldSpaceCanvasFitter.Configure(worldHudCanvas, worldCamera);
        }

        private static void InvokeVisibleButton(Button button)
        {
            if (button != null && button.gameObject.activeInHierarchy && button.interactable)
                button.onClick.Invoke();
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
            townDiorama = null;
            worldHudCanvas = null;
            worldCamera = null;
            actionLabel = null;
            dialogueLabel = null;
            speakerLabel = null;
            objectiveLabel = null;
            objectiveDescriptionLabel = null;
            journalObjectiveLabel = null;
            journalProgressLabel = null;
            dialogueObjectiveLabel = null;
            controlsObjectiveLabel = null;
            dialoguePortrait = null;
            dialoguePanel = null;
            controlsGuidePanel = null;
            questJournalPanel = null;
            actionButton = null;
            dialogueContinueButton = null;
            optionalDialogueButton = null;
            dialogueActivityButton = null;
            dialogueKindLabel = null;
            controlsGuideLabel = null;
            dialogueCardRect = null;
            journalCardRect = null;
            controlsCardRect = null;
            navigationRect = null;
            touchStickGroup = null;
            headerGroup = null;
            safeAreaRect = null;
            showingOptionalDialogue = false;
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
            visualObject.transform.localScale = Vector3.one * CharacterVisualScale;
            WorldCharacterModelBuilder character = WorldCharacterModelBuilder.Create(visualObject.transform, "traveler", true);
            if (character == null)
                Debug.LogError("DẤU XỨ: Could not build the 3D player character.", visualObject);
            playerObject.AddComponent<Rigidbody2D>();
            playerObject.AddComponent<CircleCollider2D>();
            player = playerObject.AddComponent<WorldPlayerController>();
            player.Initialize(worldCamera, world, startPosition);
            player.SetCharacterModel(character);
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
                target.Configure(definition, townDiorama);
                if (definition.Id == "lantern-display")
                    target.ApplyLanternColor(InventoryService.GetLanternColorId(InventoryService.GetCraftedLantern(profile)));
                else if (definition.Id == "hanji-display")
                {
                    string hanji = InventoryService.GetLatestCraftedHanji(profile);
                    target.ApplyHanjiVariant(InventoryService.GetHanjiShapeId(hanji), InventoryService.GetHanjiColorId(hanji));
                }
                targets.Add(target);
            }
        }

        private void CreateHud()
        {
            EnsureEventSystem();
            var canvasObject = new GameObject("World HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(runtimeRoot.transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = worldCamera;
            canvas.sortingOrder = 100;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = GameUiTheme.ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            var safeAreaObject = new GameObject("World Safe Area", typeof(RectTransform));
            safeAreaRect = safeAreaObject.GetComponent<RectTransform>();
            safeAreaRect.SetParent(canvasRect, false);
            ApplySafeArea(safeAreaRect);

            var hudControls = new GameObject("World HUD Controls", typeof(RectTransform), typeof(CanvasGroup));
            var hudControlsRect = hudControls.GetComponent<RectTransform>();
            hudControlsRect.SetParent(safeAreaRect, false);
            hudControlsRect.anchorMin = Vector2.zero;
            hudControlsRect.anchorMax = Vector2.one;
            hudControlsRect.offsetMin = hudControlsRect.offsetMax = Vector2.zero;
            headerGroup = hudControls.GetComponent<CanvasGroup>();
            var header = CreatePanel("World Objective Card", hudControlsRect, new Vector2(0f, 1f), Vector2.one, Paper);
            SetFixedRect(header, new Vector2(0f, 1f), new Vector2(16f, -16f), new Vector2(-32f, 112f));
            CreateText("World Title", header.transform, new Vector2(0.02f, 0.60f), new Vector2(0.98f, 0.98f),
                Localized(world.NameKey == "hoian" ? "world_hoian_title" : "world_jeonju_title"),
                20, GameUiTheme.Muted, TextAnchor.MiddleLeft, FontStyle.Normal);
            objectiveLabel = CreateText("World Objective Label", header.transform, new Vector2(0.02f, 0.05f), new Vector2(0.98f, 0.64f),
                string.Empty, 26, Jade, TextAnchor.MiddleLeft, FontStyle.Bold);
            var navigation = CreatePanel("World Navigation", hudControlsRect, Vector2.one, Vector2.one, Color.clear);
            navigationRect = navigation.GetComponent<RectTransform>();
            navigation.GetComponent<Image>().raycastTarget = false;
            SetFixedRect(navigation, Vector2.one, new Vector2(-16f, -140f), new Vector2(536f, 64f));
            CreateButton(navigation.transform, "World Back", "‹ " + Localized("world_map_short") + " · Esc",
                Vector2.zero, new Vector2(.33f, 1f), Vector2.zero, Vector2.zero, Close, GameUiTheme.Cream, Ink, 18);
            CreateButton(navigation.transform, "World Quest Journal", Localized("world_book") + " · J",
                new Vector2(.35f, 0f), new Vector2(.72f, 1f), Vector2.zero, Vector2.zero, ToggleQuestJournal, GameUiTheme.Cream, Ink, 18);
            CreateButton(navigation.transform, "World Controls Help", Localized("world_help") + " · H",
                new Vector2(.74f, 0f), Vector2.one, Vector2.zero, Vector2.zero, OpenControlsGuide, GameUiTheme.Cream, Ink, 18);

            CreateTouchStick(safeAreaRect);
            actionButton = CreateButton(safeAreaRect, "World Interaction", Localized("world_approach"),
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 40f), new Vector2(310f, 112f),
                OnActionButton, Jade, Paper, 22);
            actionLabel = actionButton.GetComponentInChildren<Text>();

            dialoguePanel = CreatePanel("World Dialogue", safeAreaRect, Vector2.zero, Vector2.one,
                new Color(Ink.r, Ink.g, Ink.b, 0.16f));
            var dialogueCard = CreatePanel("Conversation Card", dialoguePanel.transform,
                Vector2.one * 0.5f, Vector2.one * 0.5f, Paper);
            dialogueCardRect = dialogueCard.GetComponent<RectTransform>();
            var portraitObject = new GameObject("Dialogue Portrait", typeof(RectTransform), typeof(Image));
            portraitObject.transform.SetParent(dialogueCard.transform, false);
            RectTransform portraitRect = portraitObject.GetComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0.04f, 0.72f);
            portraitRect.anchorMax = new Vector2(0.24f, 0.97f);
            portraitRect.offsetMin = Vector2.zero;
            portraitRect.offsetMax = Vector2.zero;
            dialoguePortrait = portraitObject.GetComponent<Image>();
            dialoguePortrait.preserveAspect = true;
            dialoguePortrait.raycastTarget = false;
            dialoguePortrait.enabled = false;
            dialogueKindLabel = CreateText("Dialogue Kind", dialogueCard.transform, new Vector2(0.26f, 0.87f), new Vector2(0.97f, 0.97f),
                string.Empty, 18, GameUiTheme.Muted, TextAnchor.MiddleLeft, FontStyle.Normal);
            speakerLabel = CreateText("Dialogue Speaker", dialogueCard.transform, new Vector2(0.26f, 0.72f), new Vector2(0.97f, 0.89f),
                string.Empty, 28, Jade, TextAnchor.MiddleLeft, FontStyle.Bold);
            dialogueLabel = CreateText("Dialogue Copy", dialogueCard.transform, new Vector2(0.03f, 0.40f), new Vector2(0.97f, 0.72f),
                string.Empty, 23, Ink, TextAnchor.UpperLeft, FontStyle.Normal);
            dialogueLabel.resizeTextForBestFit = true;
            dialogueLabel.resizeTextMinSize = 18;
            dialogueLabel.resizeTextMaxSize = 23;
            dialogueLabel.verticalOverflow = VerticalWrapMode.Truncate;
            dialogueObjectiveLabel = CreateText("Dialogue Objective", dialogueCard.transform,
                new Vector2(.03f, .27f), new Vector2(.97f, .39f), string.Empty,
                20, Jade, TextAnchor.MiddleLeft, FontStyle.Bold);
            dialogueContinueButton = CreateButton(dialogueCard.transform, "Dialogue Continue", string.Empty,
                new Vector2(0.03f, 0f), new Vector2(0.31f, 0f), new Vector2(0f, 16f), new Vector2(0f, GameUiTheme.MinimumTapTarget),
                ContinueVisibleDialogue, GameUiTheme.Cream, Ink, 20);
            dialogueNarrationButton = CreateButton(dialogueCard.transform, "Dialogue Listen", Localized("read_story"),
                new Vector2(0.345f, 0f), new Vector2(0.655f, 0f), new Vector2(0f, 16f), new Vector2(0f, GameUiTheme.MinimumTapTarget),
                SpeakCurrentDialogue, GameUiTheme.Cream, Ink, 20);
            optionalDialogueButton = CreateButton(dialogueCard.transform, "Dialogue Ask More", Localized("world_ask_more") + " · Q",
                new Vector2(0.69f, 0f), new Vector2(0.97f, 0f), new Vector2(0f, 16f), new Vector2(0f, GameUiTheme.MinimumTapTarget),
                ShowOptionalDialogue, GameUiTheme.Cream, Ink, 20);
            dialogueActivityButton = CreateButton(dialogueCard.transform, "Dialogue Activity", string.Empty,
                new Vector2(0.03f, 0f), new Vector2(0.97f, 0f), new Vector2(0f, 88f), new Vector2(0f, 88f),
                StartDialogueActivity, Jade, Paper, 22);
            dialoguePanel.SetActive(false);

            CreateQuestJournal(safeAreaRect);
            CreateControlsGuide(safeAreaRect);
            worldHudCanvas = canvas;
            if (!WorldSpaceCanvasFitter.Configure(canvas, worldCamera))
                throw new InvalidOperationException("The walkable world HUD could not be fitted to its camera.");
            LayoutOverlayCards();
            RefreshMovementAvailability();
        }

        private static void SetFixedRect(GameObject target, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var rect = target.GetComponent<RectTransform>();
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private void LayoutOverlayCards()
        {
            // Use the actual camera-fitted logical canvas, including its inset safe area.
            if (safeAreaRect == null) return;
            Vector2 available = safeAreaRect.rect.size;
            if (navigationRect != null)
                navigationRect.sizeDelta = new Vector2(Mathf.Min(536f, available.x - 32f), 64f);
            if (actionButton != null)
                actionButton.GetComponent<RectTransform>().sizeDelta = new Vector2(
                    Mathf.Min(310f, (available.x - 48f) * 0.52f), 112f);
            if (touchStick != null)
            {
                float stickSize = Mathf.Min(192f, (available.x - 72f) * 0.40f);
                touchStick.GetComponent<RectTransform>().sizeDelta = Vector2.one * stickSize;
            }
            if (dialogueCardRect != null)
                dialogueCardRect.sizeDelta = new Vector2(Mathf.Min(680f, available.x * 0.92f), Mathf.Min(620f, available.y * 0.94f));
            if (journalCardRect != null)
                journalCardRect.sizeDelta = new Vector2(Mathf.Min(640f, available.x * 0.92f), Mathf.Min(580f, available.y * 0.80f));
            if (controlsCardRect != null)
                controlsCardRect.sizeDelta = new Vector2(Mathf.Min(640f, available.x * 0.92f), Mathf.Min(340f, available.y * 0.70f));
        }

        private void ApplySafeArea(RectTransform safeRect)
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            Rect safeArea = Screen.safeArea;
            safeRect.anchorMin = new Vector2(safeArea.xMin / Screen.width, safeArea.yMin / Screen.height);
            safeRect.anchorMax = new Vector2(safeArea.xMax / Screen.width, safeArea.yMax / Screen.height);
            safeRect.offsetMin = Vector2.zero;
            safeRect.offsetMax = Vector2.zero;
            lastSafeArea = safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        }

        private void CreateQuestJournal(RectTransform parent)
        {
            questJournalPanel = CreatePanel("World Quest Journal Panel", parent, Vector2.zero, Vector2.one,
                new Color(Ink.r, Ink.g, Ink.b, 0.16f));
            var card = CreatePanel("Quest Journal Card", questJournalPanel.transform,
                Vector2.one * 0.5f, Vector2.one * 0.5f, Paper);
            journalCardRect = card.GetComponent<RectTransform>();
            CreateText("World Quest Journal Title", card.transform, new Vector2(0.03f, 0.85f), new Vector2(0.97f, 0.97f),
                Localized("world_book"), 22, GameUiTheme.Muted, TextAnchor.MiddleLeft, FontStyle.Normal);
            journalObjectiveLabel = CreateText("World Quest Journal Current", card.transform,
                new Vector2(.03f, .68f), new Vector2(.97f, .85f), string.Empty, 28, Jade, TextAnchor.MiddleLeft, FontStyle.Bold);
            objectiveDescriptionLabel = CreateText("World Quest Journal Objective", card.transform,
                new Vector2(0.03f, 0.46f), new Vector2(0.97f, 0.68f), string.Empty, 23, Ink, TextAnchor.MiddleLeft, FontStyle.Normal);
            journalProgressLabel = CreateText("World Quest Journal Progress", card.transform,
                new Vector2(.03f, .32f), new Vector2(.97f, .46f), string.Empty, 20, GameUiTheme.Muted, TextAnchor.MiddleLeft, FontStyle.Normal);
            CreateButton(card.transform, "World Passport", Localized("world_passport") + " · P",
                new Vector2(.05f, 0f), new Vector2(.95f, 0f), new Vector2(0f, 100f), new Vector2(0f, 64f),
                () => onPassportRequested?.Invoke(), Jade, Paper, 22);
            CreateButton(card.transform, "Close World Quest Journal", Localized("world_close") + " · E / Esc",
                new Vector2(0.05f, 0f), new Vector2(0.95f, 0f), new Vector2(0f, 20f), new Vector2(0f, GameUiTheme.MinimumTapTarget),
                ToggleQuestJournal, GameUiTheme.Cream, Ink, 20);
            questJournalPanel.SetActive(false);
        }

        private void CreateControlsGuide(RectTransform parent)
        {
            controlsGuidePanel = CreatePanel("World Controls Guide", parent, Vector2.zero, Vector2.one,
                new Color(Ink.r, Ink.g, Ink.b, 0.12f));
            var card = CreatePanel("Controls Guide Card", controlsGuidePanel.transform,
                Vector2.one * 0.5f, Vector2.one * 0.5f, Paper);
            controlsCardRect = card.GetComponent<RectTransform>();
            CreateText("World Controls Guide Title", card.transform, new Vector2(0.04f, 0.82f), new Vector2(0.96f, 0.98f),
                Localized("world_next_step"), 18, GameUiTheme.Muted, TextAnchor.MiddleLeft, FontStyle.Normal);
            controlsObjectiveLabel = CreateText("World Controls Guide Objective", card.transform,
                new Vector2(.04f, .62f), new Vector2(.96f, .84f), string.Empty, 26, Jade, TextAnchor.MiddleLeft, FontStyle.Bold);
            controlsGuideLabel = CreateText("World Controls Guide Copy", card.transform, new Vector2(0.04f, 0.28f), new Vector2(0.96f, 0.62f),
                Localized("world_controls_copy"), 20, Ink, TextAnchor.MiddleLeft, FontStyle.Normal);
            controlsGuideCloseButton = CreateButton(card.transform, "Close World Controls Guide", Localized("world_controls_close") + " · E",
                new Vector2(0.05f, 0f), new Vector2(0.95f, 0f), new Vector2(0f, 20f), new Vector2(0f, GameUiTheme.MinimumTapTarget),
                CloseControlsGuide, Jade, Paper, 20);
            controlsGuidePanel.SetActive(!StoryProgressService.HasFlag(profile, "world-controls-guide-seen"));
        }

        private bool IsUtilityPanelOpen() =>
            (controlsGuidePanel != null && controlsGuidePanel.activeSelf) ||
            (questJournalPanel != null && questJournalPanel.activeSelf);

        private void RefreshMovementAvailability()
        {
            bool canMove = !dialogueOpen && !IsUtilityPanelOpen();
            player?.SetMovementEnabled(canMove);
            if (headerGroup != null)
            {
                headerGroup.interactable = canMove;
                headerGroup.blocksRaycasts = canMove;
            }
            // Keep the stick object active so an in-flight pointer can still receive PointerUp.
            if (touchStickGroup != null)
            {
                touchStickGroup.alpha = canMove ? 1f : 0f;
                touchStickGroup.blocksRaycasts = canMove;
                touchStickGroup.interactable = canMove;
            }
            player?.SetTouchInput(Vector2.zero);
        }

        private void OpenControlsGuide()
        {
            if (dialogueOpen || controlsGuidePanel == null) return;
            questJournalPanel.SetActive(false);
            controlsGuideSaveFailed = false;
            controlsGuideLabel.text = Localized("world_controls_copy");
            controlsGuideCloseButton.GetComponentInChildren<Text>().text = Localized("world_controls_close") + " · E";
            controlsGuidePanel.SetActive(true);
            RefreshMovementAvailability();
            RefreshActionButton();
        }

        private void CloseControlsGuide()
        {
            if (controlsGuideSaveFailed)
            {
                DismissControlsGuideWithoutSaving();
                return;
            }
            var candidate = new PlayerProfile();
            candidate.CopyFrom(profile);
            StoryProgressService.AddFlag(candidate, "world-controls-guide-seen");
            if (!TryPersistCandidate(candidate))
            {
                controlsGuideSaveFailed = true;
                if (controlsGuideLabel != null)
                    controlsGuideLabel.text = Localized("world_controls_save_error");
                if (controlsGuideCloseButton != null)
                    controlsGuideCloseButton.GetComponentInChildren<Text>().text = Localized("world_controls_skip_save") + " · E";
                return;
            }
            controlsGuideSaveFailed = false;
            if (controlsGuidePanel != null) controlsGuidePanel.SetActive(false);
            RefreshMovementAvailability();
            RefreshActionButton();
        }

        private void DismissControlsGuideWithoutSaving()
        {
            // The profile remains untouched after the failed candidate save, so the guide reappears next visit.
            controlsGuideSaveFailed = false;
            if (controlsGuidePanel != null) controlsGuidePanel.SetActive(false);
            RefreshMovementAvailability();
            RefreshActionButton();
        }

        private void ToggleQuestJournal()
        {
            if (dialogueOpen || questJournalPanel == null) return;
            controlsGuidePanel.SetActive(false);
            questJournalPanel.SetActive(!questJournalPanel.activeSelf);
            RefreshMovementAvailability();
            RefreshActionButton();
        }

        private void UpdateMovementObjective()
        {
            if (dialogueOpen || IsUtilityPanelOpen() || world == null || world.Id != "hoian" || player == null || !player.HasMoved ||
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
            if (journalObjectiveLabel != null) journalObjectiveLabel.text = Localized(next.TitleKey);
            if (dialogueObjectiveLabel != null) dialogueObjectiveLabel.text = Localized("world_next_step") + " · " + Localized(next.TitleKey);
            if (controlsObjectiveLabel != null) controlsObjectiveLabel.text = Localized(next.TitleKey);
            if (journalProgressLabel != null)
                journalProgressLabel.text = Localized("world_imprints") + ": " + (profile.worldImprints?.Count ?? 0) +
                    "\n" + Localized("culture_cards") + ": " + (profile.culturalCards?.Count ?? 0);

            if (changed)
                foreach (WorldInteractable target in targets)
                    if (target != null) target.SetQuestHighlighted(target.Definition.Id == next.TargetId);
        }

        private void CreateTouchStick(RectTransform parent)
        {
            GameObject stickObject = CreatePanel("Touch Movement Stick", parent, Vector2.zero, Vector2.zero,
                new Color(Jade.r, Jade.g, Jade.b, 0.72f));
            RectTransform stickRect = stickObject.GetComponent<RectTransform>();
            stickRect.anchorMin = Vector2.zero;
            stickRect.anchorMax = Vector2.zero;
            stickRect.pivot = Vector2.zero;
            stickRect.anchoredPosition = new Vector2(24f, 28f);
            stickRect.sizeDelta = new Vector2(192f, 192f);
            var stickImage = stickObject.GetComponent<Image>();
            stickImage.raycastTarget = true;
            touchStickGroup = stickObject.AddComponent<CanvasGroup>();
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
            actionButton.gameObject.SetActive(!dialogueOpen && !IsUtilityPanelOpen());
            if (dialogueOpen || IsUtilityPanelOpen()) return;
            WorldInteractable nearest = FindNearestTarget();
            actionLabel.text = nearest != null
                ? Localized(nearest.Definition.IsNpc ? "world_talk_to" : "world_discover") + " · " +
                    Localized(nearest.Definition.NameKey) + "\nE / Space"
                : Localized("world_approach");
            actionButton.interactable = nearest != null;
        }

        private void OnActionButton() => ContinueVisibleDialogue();

        private void OpenDialogue(WorldInteractableDefinition definition)
        {
            StoryNarration.Stop();
            showingOptionalDialogue = false;
            dialogueOpen = true;
            // Direct integrations may interact before dismissing the first-use guide.
            // Hiding it here is transient and does not record its persisted seen flag.
            controlsGuidePanel.SetActive(false);
            questJournalPanel.SetActive(false);
            dialoguePanel.SetActive(true);
            bool isNpc = definition != null && definition.IsNpc;
            dialogueKindLabel.text = Localized(isNpc ? "world_conversation" : "world_discovery");
            // Objects retain discovery copy and their own title rather than an NPC portrait.
            dialoguePortrait.gameObject.SetActive(isNpc);
            var speakerRect = speakerLabel.rectTransform;
            var kindRect = dialogueKindLabel.rectTransform;
            speakerRect.anchorMin = new Vector2(isNpc ? 0.26f : 0.03f, 0.72f);
            kindRect.anchorMin = new Vector2(isNpc ? 0.26f : 0.03f, 0.87f);
            optionalDialogueButton.gameObject.SetActive(isNpc && !string.IsNullOrEmpty(definition.OptionalDialogueKey));
            dialogueNarrationButton.gameObject.SetActive(isNpc);
            dialogueContinueButton.GetComponent<RectTransform>().anchorMax = new Vector2(isNpc ? 0.31f : 0.49f, 0f);
            dialogueNarrationButton.GetComponent<RectTransform>().anchorMin = new Vector2(isNpc ? 0.345f : 0.51f, 0f);
            dialogueNarrationButton.GetComponent<RectTransform>().anchorMax = new Vector2(isNpc ? 0.655f : 0.97f, 0f);
            optionalDialogueButton.GetComponent<RectTransform>().anchorMin = new Vector2(0.69f, 0f);
            optionalDialogueButton.GetComponent<RectTransform>().anchorMax = new Vector2(0.97f, 0f);
            if (!isNpc)
            {
                dialogueContinueButton.GetComponent<RectTransform>().anchorMax = new Vector2(0.49f, 0f);
                optionalDialogueButton.GetComponent<RectTransform>().anchorMin = new Vector2(0.51f, 0f);
            }
            bool hasActivity = isNpc && !string.IsNullOrEmpty(definition.ActivityId);
            dialogueActivityButton.gameObject.SetActive(hasActivity);
            if (hasActivity)
                dialogueActivityButton.GetComponentInChildren<Text>().text = Localized(definition.ActivityId == "lantern-craft"
                    ? "activity_lantern" : definition.ActivityId == "hanji-craft" ? "activity_hanji" : "world_start_activity") + " · F";
            dialogueContinueButton.GetComponentInChildren<Text>().text = Localized("world_close") + " · E / Space";
            RefreshMovementAvailability();
        }

        private void ShowOptionalDialogue()
        {
            WorldInteractableDefinition target = lastInteraction?.Target;
            if (!dialogueOpen || IsUtilityPanelOpen() || target == null || !target.IsNpc ||
                string.IsNullOrEmpty(target.OptionalDialogueKey)) return;
            // This is view state only: no profile writes, story transactions, or rewards.
            StoryNarration.Stop();
            showingOptionalDialogue = true;
            dialogueLabel.text = Localized(target.OptionalDialogueKey);
            optionalDialogueButton.gameObject.SetActive(false);
            dialogueContinueButton.GetComponentInChildren<Text>().text = Localized("world_return_dialogue") + " · E";
        }

        private void RestorePrimaryDialogue()
        {
            if (!dialogueOpen || lastInteraction == null) return;
            StoryNarration.Stop();
            dialogueLabel.text = Localized(lastInteraction.DialogueKey);
            OpenDialogue(lastInteraction.Target);
        }

        private void SpeakCurrentDialogue()
        {
            if (!dialogueOpen || dialogueLabel == null || dialogueNarrationButton == null ||
                !dialogueNarrationButton.gameObject.activeSelf) return;
            StoryNarration.Speak(profile.language, dialogueLabel.text);
        }

        private void StartDialogueActivity()
        {
            WorldInteractableDefinition target = lastInteraction?.Target;
            if (!dialogueOpen || IsUtilityPanelOpen() || target == null || !target.IsNpc ||
                string.IsNullOrEmpty(target.ActivityId)) return;
            string activityId = target.ActivityId;
            Action<string> callback = onActivityRequested;
            Close();
            callback?.Invoke(activityId);
        }

        private void ContinueVisibleDialogue()
        {
            if (IsUtilityPanelOpen()) return;
            if (!dialogueOpen) InteractNearest();
            else if (showingOptionalDialogue) RestorePrimaryDialogue();
            else DismissDialogue();
        }

        public void ContinueDialogue()
        {
            // Preserve the legacy activity route for programmatic callers. Touch and
            // keyboard continue use ContinueVisibleDialogue; the activity CTA calls StartDialogueActivity directly.
            if (!dialogueOpen) InteractNearest();
            else if (!string.IsNullOrEmpty(lastInteraction?.Target?.ActivityId)) StartDialogueActivity();
            else DismissDialogue();
        }

        private Button CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPosition, Vector2 size, Action clicked, Color background, Color foreground, int fontSize)
        {
            GameObject buttonObject = CreatePanel(name, parent, anchorMin, anchorMax, background);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.pivot = new Vector2(anchorMin.x == anchorMax.x ? anchorMin.x : 0.5f,
                anchorMin.y == anchorMax.y ? anchorMin.y : 0.5f);
            rect.anchoredPosition = anchoredPosition;
            // Stretch anchors already contribute width/height; fixed axes need a full touch target.
            if (Mathf.Approximately(anchorMin.x, anchorMax.x)) size.x = Mathf.Max(GameUiTheme.MinimumTapTarget, size.x);
            if (Mathf.Approximately(anchorMin.y, anchorMax.y)) size.y = Mathf.Max(GameUiTheme.MinimumTapTarget, size.y);
            rect.sizeDelta = size;
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            var colors = button.colors;
            colors.highlightedColor = Color.Lerp(Color.white, GameUiTheme.Secondary, .12f);
            colors.pressedColor = Color.Lerp(Color.white, GameUiTheme.Ink, .12f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = GameUiTheme.Disabled;
            button.colors = colors;
            button.onClick.AddListener(() => clicked?.Invoke());
            CreateText("Label", buttonObject.transform, Vector2.zero, Vector2.one, label, fontSize, foreground, TextAnchor.MiddleCenter, FontStyle.Bold);
            buttonObject.AddComponent<GameUiButtonMotion>();
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
