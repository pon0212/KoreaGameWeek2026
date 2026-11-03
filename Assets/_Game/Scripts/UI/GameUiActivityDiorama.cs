using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TastePassport.UI
{
    /// <summary>Owns a reusable low-poly 3D scene behind the passport and activity pages.</summary>
    public sealed class GameUiActivityDiorama : MonoBehaviour
    {
        private const int StageLayer = 31;
        private static int nextStageId;

        private readonly List<Material> ownedMaterials = new List<Material>();
        private readonly List<Mesh> ownedMeshes = new List<Mesh>();
        private readonly Dictionary<string, GameObject> scenes = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, GameObject> variants = new Dictionary<string, GameObject>();

        private GameObject stageRoot;
        private Camera stageCamera;
        private RenderTexture texture;
        private Mesh cubeMesh;
        private Mesh octahedronMesh;
        private Mesh cylinderMesh;
        private Mesh roofMesh;
        private Material woodMaterial;
        private Material paperMaterial;
        private Material wallMaterial;
        private Material roofMaterial;
        private Material accentMaterial;
        private Material jadeMaterial;
        private Material vermilionMaterial;
        private Material goldMaterial;
        private Material blueMaterial;
        private Material foodMaterial;
        private string activityId = "passport";
        private bool koreanPalette;
        private bool stageVisible = true;
        private bool initialized;

        public Texture StageTexture => texture;

        public static GameUiActivityDiorama Ensure(Transform owner)
        {
            if (owner == null)
            {
                Debug.LogWarning("Cannot create an activity diorama without an owner.");
                return null;
            }

            GameUiActivityDiorama existing = owner.GetComponentInChildren<GameUiActivityDiorama>(true);
            if (existing != null) return existing;

            var holder = new GameObject("3D Passport and Activity Stage");
            holder.transform.SetParent(owner, false);
            return holder.AddComponent<GameUiActivityDiorama>();
        }

        public void SetActivity(string id, bool korean)
        {
            activityId = string.IsNullOrEmpty(id) ? "passport" : id.ToLowerInvariant();
            koreanPalette = korean;
            if (!initialized) return;
            ApplyPalette();
            ApplyActivityVisibility();
        }

        public void SetVisible(bool visible)
        {
            stageVisible = visible;
            if (stageCamera != null) stageCamera.enabled = stageVisible && isActiveAndEnabled;
        }

        private void OnEnable()
        {
            if (!initialized) Initialize();
            if (stageCamera != null) stageCamera.enabled = stageVisible && isActiveAndEnabled;
        }

        private void OnDisable()
        {
            if (stageCamera != null) stageCamera.enabled = false;
        }

        private void Initialize()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null)
            {
                Debug.LogError("Cannot create the activity diorama: no supported unlit shader was found.", this);
                return;
            }

            // Keep this camera and its layer-31 geometry far from the passport backdrop camera.
            stageRoot = new GameObject("Activity Diorama Render World");
            stageRoot.hideFlags = HideFlags.HideInHierarchy;
            stageRoot.transform.position = new Vector3(30000f + nextStageId++ * 100f, 30000f, 30000f);

            texture = new RenderTexture(720, 1280, 16, RenderTextureFormat.ARGB32)
            {
                name = "Activity Diorama Texture",
                antiAliasing = 1,
                filterMode = FilterMode.Bilinear
            };
            texture.Create();
            if (!texture.IsCreated())
            {
                Debug.LogError("Cannot create the activity diorama render texture.", this);
                RenderTexture failedTexture = texture;
                texture = null;
                failedTexture.Release();
                DestroyOwned(failedTexture);
                DestroyOwned(stageRoot);
                stageRoot = null;
                return;
            }

            GameObject cameraObject = new GameObject("Activity Diorama Camera");
            cameraObject.transform.SetParent(stageRoot.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 4.8f, -12.4f);
            cameraObject.transform.localRotation = Quaternion.LookRotation(new Vector3(0f, 1.1f, 0f) - cameraObject.transform.localPosition, Vector3.up);
            stageCamera = cameraObject.AddComponent<Camera>();
            stageCamera.enabled = false;
            stageCamera.orthographic = true;
            stageCamera.orthographicSize = 4.8f;
            stageCamera.nearClipPlane = 0.1f;
            stageCamera.farClipPlane = 30f;
            stageCamera.cullingMask = 1 << StageLayer;
            stageCamera.clearFlags = CameraClearFlags.SolidColor;
            stageCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            stageCamera.targetTexture = texture;
            stageCamera.allowHDR = false;
            stageCamera.allowMSAA = false;
            stageCamera.useOcclusionCulling = false;

            cubeMesh = OwnMesh(CreateCubeMesh());
            octahedronMesh = OwnMesh(CreateOctahedronMesh());
            cylinderMesh = OwnMesh(CreateCylinderMesh(10));
            roofMesh = OwnMesh(CreateRoofMesh());
            woodMaterial = CreateMaterial(shader, new Color(0.34f, 0.22f, 0.15f));
            paperMaterial = CreateMaterial(shader, new Color(0.95f, 0.87f, 0.69f));
            wallMaterial = CreateMaterial(shader, new Color(0.83f, 0.63f, 0.42f));
            roofMaterial = CreateMaterial(shader, new Color(0.66f, 0.24f, 0.19f));
            accentMaterial = CreateMaterial(shader, new Color(0.34f, 0.23f, 0.19f));
            jadeMaterial = CreateMaterial(shader, new Color(0.23f, 0.48f, 0.39f));
            vermilionMaterial = CreateMaterial(shader, new Color(0.78f, 0.28f, 0.20f));
            goldMaterial = CreateMaterial(shader, new Color(0.92f, 0.66f, 0.27f));
            blueMaterial = CreateMaterial(shader, new Color(0.28f, 0.43f, 0.52f));
            foodMaterial = CreateMaterial(shader, new Color(0.76f, 0.58f, 0.32f));

            BuildScenes();
            initialized = true;
            ApplyPalette();
            ApplyActivityVisibility();
            stageCamera.enabled = stageVisible && isActiveAndEnabled;
        }

        private void BuildScenes()
        {
            BuildPassportScene();
            BuildPostcardScene();
            BuildArchitectureScene();
            BuildRecipeScene();
            BuildMatchingScene();
            BuildLanternScene();
            BuildHanjiScene();
            BuildFestivalScene();
        }

        private void BuildPassportScene()
        {
            Transform scene = NewScene("passport");
            BuildDesk(scene);

            CreateBox(scene, "Passport cover left", new Vector3(-0.88f, 0.60f, -0.12f), new Vector3(1.85f, 0.18f, 2.15f), jadeMaterial);
            CreateBox(scene, "Passport cover right", new Vector3(0.88f, 0.60f, -0.12f), new Vector3(1.85f, 0.18f, 2.15f), jadeMaterial);
            CreateBox(scene, "Passport left page", new Vector3(-0.86f, 0.72f, -0.08f), new Vector3(1.67f, 0.07f, 1.98f), paperMaterial);
            CreateBox(scene, "Passport right page", new Vector3(0.86f, 0.72f, -0.08f), new Vector3(1.67f, 0.07f, 1.98f), paperMaterial);
            CreateBox(scene, "Passport spine", new Vector3(0f, 0.68f, -0.08f), new Vector3(0.18f, 0.16f, 2.18f), accentMaterial);
            CreateBox(scene, "Passport gold border left", new Vector3(-0.88f, 0.77f, -0.08f), new Vector3(1.46f, 0.025f, 0.035f), goldMaterial);
            CreateBox(scene, "Passport gold border right", new Vector3(0.88f, 0.77f, -0.08f), new Vector3(1.46f, 0.025f, 0.035f), goldMaterial);
            CreateCylinder(scene, "Passport stamp", new Vector3(-1.27f, 0.79f, -0.52f), new Vector3(0.31f, 0.06f, 0.31f), vermilionMaterial, false);
            CreateCylinder(scene, "Passport culture seal", new Vector3(1.24f, 0.79f, 0.40f), new Vector3(0.32f, 0.06f, 0.32f), jadeMaterial, false);
            CreateBox(scene, "Passport page mark one", new Vector3(-0.80f, 0.79f, 0.16f), new Vector3(0.62f, 0.025f, 0.075f), accentMaterial);
            CreateBox(scene, "Passport page mark two", new Vector3(0.84f, 0.79f, -0.43f), new Vector3(0.66f, 0.025f, 0.075f), accentMaterial);
            CreateCylinder(scene, "Passport pen", new Vector3(2.12f, 0.92f, 0.03f), new Vector3(0.10f, 0.10f, 1.78f), accentMaterial, true);
            CreateOctahedron(scene, "Passport ink stone", new Vector3(-2.25f, 0.95f, -0.10f), new Vector3(0.48f, 0.32f, 0.46f), blueMaterial);
            CreateBox(scene, "Passport ribbon", new Vector3(0.11f, 0.61f, -1.15f), new Vector3(0.14f, 0.045f, 0.62f), vermilionMaterial);
            AddResultVariant(scene, "passport", new Vector3(0f, 1.12f, -0.45f), goldMaterial);
        }

        private void BuildPostcardScene()
        {
            Transform scene = NewScene("postcard");
            BuildDesk(scene);
            CreateBox(scene, "Postcard backing", new Vector3(0f, 0.62f, -0.10f), new Vector3(4.8f, 0.18f, 2.92f), vermilionMaterial);
            CreateBox(scene, "Postcard paper", new Vector3(0f, 0.75f, -0.10f), new Vector3(4.52f, 0.07f, 2.65f), paperMaterial);
            CreateBox(scene, "Postcard landscape hill", new Vector3(-1.14f, 0.81f, 0.52f), new Vector3(1.88f, 0.04f, 0.68f), jadeMaterial);
            CreateBox(scene, "Postcard landscape house", new Vector3(-1.03f, 1.01f, -0.30f), new Vector3(0.80f, 0.42f, 0.70f), wallMaterial);
            CreateMeshPart(scene, "Postcard landscape roof", roofMesh, new Vector3(-1.03f, 1.26f, -0.30f), new Vector3(1.02f, 0.50f, 0.82f), roofMaterial);
            CreateBox(scene, "Postcard stamp", new Vector3(1.71f, 0.83f, -0.77f), new Vector3(0.78f, 0.04f, 0.58f), blueMaterial);
            CreateBox(scene, "Postcard message line one", new Vector3(1.32f, 0.82f, 0.02f), new Vector3(1.35f, 0.03f, 0.06f), accentMaterial);
            CreateBox(scene, "Postcard message line two", new Vector3(1.32f, 0.82f, 0.34f), new Vector3(1.35f, 0.03f, 0.06f), accentMaterial);
            AddResultVariant(scene, "postcard", new Vector3(0f, 1.18f, -0.55f), goldMaterial);
        }

        private void BuildArchitectureScene()
        {
            Transform scene = NewScene("architecture");
            BuildDisplayPlinth(scene);
            CreateBox(scene, "Architecture miniature wall", new Vector3(0f, 0.10f, 0.12f), new Vector3(3.00f, 1.65f, 1.35f), wallMaterial);
            CreateMeshPart(scene, "Architecture miniature roof", roofMesh, new Vector3(0f, 1.06f, 0.12f), new Vector3(3.52f, 1.05f, 1.65f), roofMaterial);
            CreateBox(scene, "Architecture doorway", new Vector3(0f, -0.12f, -0.59f), new Vector3(0.54f, 0.98f, 0.08f), accentMaterial);
            CreateBox(scene, "Architecture beam", new Vector3(0f, 0.63f, -0.60f), new Vector3(2.86f, 0.12f, 0.09f), accentMaterial);
            CreateBox(scene, "Architecture bridge deck", new Vector3(0f, -0.38f, 1.17f), new Vector3(3.00f, 0.24f, 0.70f), woodMaterial);
            CreateBox(scene, "Architecture bridge railing near", new Vector3(0f, -0.05f, 0.83f), new Vector3(3.00f, 0.12f, 0.10f), vermilionMaterial);
            CreateBox(scene, "Architecture bridge railing far", new Vector3(0f, -0.05f, 1.51f), new Vector3(3.00f, 0.12f, 0.10f), vermilionMaterial);
            AddVariant("architecture-assembly", scene, new Vector3(0f, 1.75f, -0.55f), goldMaterial);
            Transform assembly = variants["architecture-assembly"].transform;
            CreateBox(assembly, "Assembly fragment one", new Vector3(-0.49f, 0.20f, 0f), new Vector3(0.40f, 0.42f, 0.36f), wallMaterial);
            CreateMeshPart(assembly, "Assembly fragment two", roofMesh, new Vector3(0f, 0.22f, 0f), new Vector3(0.50f, 0.40f, 0.40f), roofMaterial);
            CreateBox(assembly, "Assembly fragment three", new Vector3(0.49f, 0.20f, 0f), new Vector3(0.40f, 0.42f, 0.36f), accentMaterial);
            AddResultVariant(scene, "architecture", new Vector3(2.00f, 0.88f, -0.75f), goldMaterial);
        }

        private void BuildRecipeScene()
        {
            Transform scene = NewScene("recipe");
            BuildDesk(scene);
            CreateOctahedron(scene, "Recipe serving bowl", new Vector3(0f, 0.94f, -0.12f), new Vector3(2.35f, 0.78f, 1.82f), blueMaterial);
            CreateCylinder(scene, "Recipe bowl rim", new Vector3(0f, 1.15f, -0.12f), new Vector3(2.18f, 0.09f, 1.58f), paperMaterial, false);
            CreateOctahedron(scene, "Recipe noodles rice", new Vector3(0f, 1.20f, -0.10f), new Vector3(1.72f, 0.31f, 1.26f), foodMaterial);
            CreateOctahedron(scene, "Recipe greens", new Vector3(-0.46f, 1.43f, -0.21f), new Vector3(0.68f, 0.19f, 0.55f), jadeMaterial);
            CreateOctahedron(scene, "Recipe garnish red", new Vector3(0.41f, 1.43f, -0.10f), new Vector3(0.44f, 0.18f, 0.44f), vermilionMaterial);
            CreateOctahedron(scene, "Recipe egg", new Vector3(0.10f, 1.45f, 0.22f), new Vector3(0.50f, 0.20f, 0.42f), goldMaterial);
            CreateCylinder(scene, "Recipe chopstick one", new Vector3(1.82f, 0.88f, -0.10f), new Vector3(0.09f, 0.09f, 1.74f), woodMaterial, true);
            CreateCylinder(scene, "Recipe chopstick two", new Vector3(2.03f, 0.88f, -0.10f), new Vector3(0.09f, 0.09f, 1.74f), accentMaterial, true);
            AddVariant("recipe-mix", scene, new Vector3(0f, 2.13f, -0.25f), goldMaterial);
            CreateCylinder(variants["recipe-mix"].transform, "Recipe mixing spoon", new Vector3(0.44f, 0.05f, 0.12f), new Vector3(0.13f, 0.13f, 0.88f), woodMaterial, true);
            AddVariant("recipe-fail", scene, new Vector3(0f, 1.80f, -0.20f), accentMaterial);
            CreateBox(variants["recipe-fail"].transform, "Recipe retry marker", new Vector3(0f, 0.30f, -0.04f), new Vector3(0.64f, 0.08f, 0.16f), vermilionMaterial);
            AddResultVariant(scene, "recipe", new Vector3(0f, 1.98f, -0.45f), goldMaterial);
        }

        private void BuildMatchingScene()
        {
            Transform scene = NewScene("matching");
            BuildDisplayPlinth(scene);
            float[] x = { -1.55f, 0f, 1.55f };
            for (int i = 0; i < x.Length; i++)
            {
                CreateBox(scene, "Culture matching card " + i, new Vector3(x[i], 0.12f, 0.02f), new Vector3(1.20f, 0.18f, 1.75f), paperMaterial);
                CreateCylinder(scene, "Culture matching seal " + i, new Vector3(x[i], 0.27f, -0.28f), new Vector3(0.66f, 0.10f, 0.66f), i == 1 ? goldMaterial : jadeMaterial, false);
            }
            CreateLanternProp(scene, "Matching lantern", new Vector3(-1.55f, 0.72f, -0.08f), 0.80f, vermilionMaterial);
            BuildBowlProp(scene, "Matching cuisine bowl", new Vector3(0f, 0.81f, -0.08f), blueMaterial);
            CreateMeshPart(scene, "Matching bridge token", roofMesh, new Vector3(1.55f, 0.64f, -0.04f), new Vector3(0.90f, 0.65f, 0.72f), vermilionMaterial);
            AddResultVariant(scene, "matching", new Vector3(0f, 1.92f, -0.45f), goldMaterial);
        }

        private void BuildLanternScene()
        {
            Transform scene = NewScene("lantern");
            BuildDesk(scene);
            CreateBox(scene, "Lantern workbench", new Vector3(0f, 0.32f, -0.05f), new Vector3(4.30f, 0.24f, 2.25f), woodMaterial);
            CreateLanternProp(scene, "Craft lantern", new Vector3(0f, 1.43f, -0.20f), 1.55f, vermilionMaterial);
            CreateBox(scene, "Lantern bamboo strip one", new Vector3(-1.75f, 0.62f, -0.40f), new Vector3(0.13f, 0.10f, 1.25f), goldMaterial);
            CreateBox(scene, "Lantern bamboo strip two", new Vector3(-1.49f, 0.62f, -0.40f), new Vector3(0.13f, 0.10f, 1.25f), woodMaterial);
            CreateCylinder(scene, "Lantern craft brush", new Vector3(1.80f, 0.76f, -0.18f), new Vector3(0.15f, 0.15f, 1.40f), jadeMaterial, true);
            AddResultVariant(scene, "lantern", new Vector3(0f, 2.70f, -0.35f), goldMaterial);
        }

        private void BuildHanjiScene()
        {
            Transform scene = NewScene("hanji");
            BuildDesk(scene);
            CreateBox(scene, "Hanji worktable", new Vector3(0f, 0.30f, -0.05f), new Vector3(4.35f, 0.25f, 2.25f), woodMaterial);
            CreateHanjiFan(scene, new Vector3(-1.12f, 0.84f, -0.17f), 1.20f);
            CreateHanjiFlower(scene, new Vector3(0.48f, 0.95f, -0.28f), 0.75f);
            CreateBox(scene, "Hanji paper sheet", new Vector3(1.53f, 0.48f, 0.55f), new Vector3(0.92f, 0.08f, 1.13f), paperMaterial);
            CreateCylinder(scene, "Hanji scissors handle left", new Vector3(1.70f, 0.63f, -0.62f), new Vector3(0.27f, 0.08f, 0.27f), vermilionMaterial, false);
            CreateCylinder(scene, "Hanji scissors handle right", new Vector3(2.06f, 0.63f, -0.62f), new Vector3(0.27f, 0.08f, 0.27f), vermilionMaterial, false);
            CreateBox(scene, "Hanji scissors blade one", new Vector3(1.84f, 0.62f, -0.19f), new Vector3(0.09f, 0.06f, 0.86f), blueMaterial);
            CreateBox(scene, "Hanji scissors blade two", new Vector3(1.96f, 0.62f, -0.19f), new Vector3(0.09f, 0.06f, 0.86f), blueMaterial);
            AddResultVariant(scene, "hanji", new Vector3(0f, 2.28f, -0.35f), goldMaterial);
        }

        private void BuildFestivalScene()
        {
            Transform scene = NewScene("festival");
            BuildDisplayPlinth(scene);
            CreateBox(scene, "Festival arch left", new Vector3(-1.25f, 0.78f, 0.10f), new Vector3(0.24f, 1.68f, 0.24f), vermilionMaterial);
            CreateBox(scene, "Festival arch right", new Vector3(1.25f, 0.78f, 0.10f), new Vector3(0.24f, 1.68f, 0.24f), blueMaterial);
            CreateBox(scene, "Festival arch lintel", new Vector3(0f, 1.58f, 0.10f), new Vector3(2.74f, 0.24f, 0.32f), goldMaterial);
            CreateMeshPart(scene, "Festival roof", roofMesh, new Vector3(0f, 1.85f, 0.10f), new Vector3(3.05f, 0.58f, 0.50f), jadeMaterial);
            CreateLanternProp(scene, "Festival Vietnam lantern", new Vector3(-2.03f, 1.06f, -0.30f), 0.72f, vermilionMaterial);
            CreateLanternProp(scene, "Festival Korea lantern", new Vector3(2.03f, 1.06f, -0.30f), 0.72f, blueMaterial);
            CreateBox(scene, "Festival culture table", new Vector3(0f, 0.18f, 0.95f), new Vector3(3.60f, 0.28f, 0.58f), woodMaterial);
            CreateBox(scene, "Festival identity card Vietnam", new Vector3(-0.92f, 0.43f, 0.88f), new Vector3(0.92f, 0.08f, 0.42f), vermilionMaterial);
            CreateBox(scene, "Festival identity card Korea", new Vector3(0.92f, 0.43f, 0.88f), new Vector3(0.92f, 0.08f, 0.42f), blueMaterial);
            AddResultVariant(scene, "festival", new Vector3(0f, 2.40f, -0.52f), goldMaterial);
        }

        private void BuildDesk(Transform parent)
        {
            CreateBox(parent, "Diorama tabletop", new Vector3(0f, 0.10f, 0.30f), new Vector3(7.20f, 0.24f, 4.25f), woodMaterial);
            CreateBox(parent, "Diorama table leg left", new Vector3(-2.70f, -0.62f, 0.42f), new Vector3(0.24f, 1.28f, 0.24f), accentMaterial);
            CreateBox(parent, "Diorama table leg right", new Vector3(2.70f, -0.62f, 0.42f), new Vector3(0.24f, 1.28f, 0.24f), accentMaterial);
            CreateBox(parent, "Diorama table front", new Vector3(0f, -0.48f, -1.60f), new Vector3(7.18f, 0.26f, 0.24f), woodMaterial);
        }

        private void BuildDisplayPlinth(Transform parent)
        {
            CreateBox(parent, "Culture display plinth", new Vector3(0f, -0.76f, 0.10f), new Vector3(6.50f, 0.42f, 3.65f), jadeMaterial);
            CreateBox(parent, "Culture display surface", new Vector3(0f, -0.51f, 0.10f), new Vector3(6.25f, 0.12f, 3.40f), woodMaterial);
        }

        private void BuildBowlProp(Transform parent, string name, Vector3 position, Material material)
        {
            CreateOctahedron(parent, name + " body", position, new Vector3(1.08f, 0.66f, 0.90f), material);
            CreateCylinder(parent, name + " rim", position + Vector3.up * 0.16f, new Vector3(0.95f, 0.09f, 0.75f), paperMaterial, false);
            CreateOctahedron(parent, name + " filling", position + Vector3.up * 0.27f, new Vector3(0.68f, 0.22f, 0.54f), foodMaterial);
        }

        private void CreateLanternProp(Transform parent, string name, Vector3 position, float size, Material color)
        {
            CreateBox(parent, name + " top", position + Vector3.up * (size * 0.56f), new Vector3(size * 0.54f, size * 0.15f, size * 0.54f), goldMaterial);
            CreateOctahedron(parent, name + " paper body", position, new Vector3(size * 0.62f, size * 0.76f, size * 0.56f), color);
            CreateCylinder(parent, name + " lower ring", position + Vector3.down * (size * 0.45f), new Vector3(size * 0.50f, size * 0.11f, size * 0.50f), accentMaterial, false);
            CreateBox(parent, name + " tassel", position + Vector3.down * (size * 0.72f), new Vector3(size * 0.07f, size * 0.45f, size * 0.07f), vermilionMaterial);
        }

        private void CreateHanjiFan(Transform parent, Vector3 position, float size)
        {
            CreateOctahedron(parent, "Hanji fan paper", position, new Vector3(size * 1.35f, size * 0.18f, size * 0.92f), vermilionMaterial);
            CreateCylinder(parent, "Hanji fan handle", position + Vector3.down * (size * 0.35f), new Vector3(size * 0.12f, size * 0.12f, size * 0.95f), woodMaterial, true);
            for (int i = -2; i <= 2; i++)
            {
                var rib = CreateBox(parent, "Hanji fan rib " + i,
                    position + new Vector3(i * size * 0.22f, size * 0.09f, 0f),
                    new Vector3(size * 0.045f, size * 0.07f, size * 0.80f), goldMaterial);
                rib.localRotation = Quaternion.Euler(0f, i * 7f, 0f);
            }
        }

        private void CreateHanjiFlower(Transform parent, Vector3 position, float size)
        {
            CreateOctahedron(parent, "Hanji flower center", position, Vector3.one * (size * 0.26f), goldMaterial);
            for (int i = 0; i < 5; i++)
            {
                float angle = i * (Mathf.PI * 2f / 5f);
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * size * 0.38f;
                CreateOctahedron(parent, "Hanji flower petal " + i, position + offset,
                    new Vector3(size * 0.42f, size * 0.18f, size * 0.34f), jadeMaterial);
            }
        }

        private void AddResultVariant(Transform scene, string sceneKey, Vector3 position, Material material)
        {
            AddVariant(sceneKey + "-result", scene, position, material);
            GameObject result = variants[sceneKey + "-result"];
            CreateCylinder(result.transform, "Result medal base", Vector3.zero, new Vector3(0.78f, 0.16f, 0.78f), goldMaterial, false);
            CreateOctahedron(result.transform, "Result medal", new Vector3(0f, 0.58f, 0f), new Vector3(0.58f, 0.68f, 0.48f), material);
            CreateBox(result.transform, "Result ribbon left", new Vector3(-0.26f, 0.12f, -0.01f), new Vector3(0.18f, 0.50f, 0.13f), vermilionMaterial);
            CreateBox(result.transform, "Result ribbon right", new Vector3(0.26f, 0.12f, -0.01f), new Vector3(0.18f, 0.50f, 0.13f), blueMaterial);
        }

        private void AddVariant(string id, Transform parent, Vector3 position, Material material)
        {
            var variant = new GameObject("3D variant " + id);
            variant.layer = StageLayer;
            variant.transform.SetParent(parent, false);
            variant.transform.localPosition = position;
            variants[id] = variant;
            CreateOctahedron(variant.transform, id + " keepsake", Vector3.zero, new Vector3(0.82f, 0.92f, 0.66f), material);
            CreateBox(variant.transform, id + " display stand", Vector3.down * 0.72f, new Vector3(1.08f, 0.20f, 0.76f), woodMaterial);
            variant.SetActive(false);
        }

        private Transform NewScene(string id)
        {
            var scene = new GameObject("Activity Scene - " + id);
            scene.layer = StageLayer;
            scene.transform.SetParent(stageRoot.transform, false);
            scenes.Add(id, scene);
            return scene.transform;
        }

        private void ApplyPalette()
        {
            if (wallMaterial == null) return;
            wallMaterial.color = koreanPalette ? new Color(0.84f, 0.76f, 0.62f) : new Color(0.86f, 0.65f, 0.43f);
            roofMaterial.color = koreanPalette ? new Color(0.28f, 0.38f, 0.44f) : new Color(0.69f, 0.25f, 0.19f);
            accentMaterial.color = koreanPalette ? new Color(0.24f, 0.27f, 0.26f) : new Color(0.39f, 0.24f, 0.18f);
            jadeMaterial.color = koreanPalette ? new Color(0.29f, 0.49f, 0.48f) : new Color(0.28f, 0.50f, 0.40f);
            foodMaterial.color = koreanPalette ? new Color(0.82f, 0.62f, 0.29f) : new Color(0.77f, 0.55f, 0.31f);
        }

        private void ApplyActivityVisibility()
        {
            string sceneKey = ResolveSceneKey(activityId);
            foreach (KeyValuePair<string, GameObject> scene in scenes)
                scene.Value.SetActive(scene.Key == sceneKey);
            foreach (KeyValuePair<string, GameObject> variant in variants)
                variant.Value.SetActive(variant.Key == activityId);
        }

        private static string ResolveSceneKey(string id)
        {
            if (id.StartsWith("architecture")) return "architecture";
            if (id.StartsWith("recipe")) return "recipe";
            if (id.StartsWith("matching")) return "matching";
            if (id.StartsWith("lantern")) return "lantern";
            if (id.StartsWith("hanji")) return "hanji";
            if (id.StartsWith("festival")) return "festival";
            return id == "postcard" ? "postcard" : "passport";
        }

        private Transform CreateBox(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            return CreateMeshPart(parent, name, cubeMesh, position, scale, material);
        }

        private Transform CreateOctahedron(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            return CreateMeshPart(parent, name, octahedronMesh, position, scale, material);
        }

        private Transform CreateCylinder(Transform parent, string name, Vector3 position, Vector3 scale, Material material, bool horizontal)
        {
            Transform part = CreateMeshPart(parent, name, cylinderMesh, position, scale, material);
            if (horizontal) part.localRotation = Quaternion.Euler(90f, 0f, 0f);
            return part;
        }

        private Transform CreateMeshPart(Transform parent, string name, Mesh mesh, Vector3 position, Vector3 scale, Material material)
        {
            var part = new GameObject(name);
            part.layer = StageLayer;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return part.transform;
        }

        private Mesh OwnMesh(Mesh mesh)
        {
            ownedMeshes.Add(mesh);
            return mesh;
        }

        private Material CreateMaterial(Shader shader, Color color)
        {
            var material = new Material(shader) { color = color };
            ownedMaterials.Add(material);
            return material;
        }

        private static Mesh CreateCubeMesh()
        {
            var mesh = new Mesh { name = "Activity Diorama Cube" };
            mesh.vertices = new[]
            {
                new Vector3(-.5f,-.5f,-.5f), new Vector3(.5f,-.5f,-.5f), new Vector3(.5f,.5f,-.5f), new Vector3(-.5f,.5f,-.5f),
                new Vector3(-.5f,-.5f,.5f), new Vector3(.5f,-.5f,.5f), new Vector3(.5f,.5f,.5f), new Vector3(-.5f,.5f,.5f)
            };
            mesh.triangles = new[] { 0,2,1, 0,3,2, 5,6,4, 6,7,4, 4,3,0, 4,7,3, 1,2,5, 2,6,5, 3,7,2, 2,7,6, 4,0,5, 5,0,1 };
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Mesh CreateOctahedronMesh()
        {
            var mesh = new Mesh { name = "Activity Diorama Low Poly Sphere" };
            mesh.vertices = new[]
            {
                Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back
            };
            mesh.triangles = new[] { 0,2,4, 0,4,3, 0,3,5, 0,5,2, 1,4,2, 1,3,4, 1,5,3, 1,2,5 };
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Mesh CreateCylinderMesh(int sides)
        {
            var mesh = new Mesh { name = "Activity Diorama Low Poly Cylinder" };
            var vertices = new Vector3[sides * 2 + 2];
            vertices[sides * 2] = new Vector3(0f, -0.5f, 0f);
            vertices[sides * 2 + 1] = new Vector3(0f, 0.5f, 0f);
            for (int i = 0; i < sides; i++)
            {
                float angle = i * Mathf.PI * 2f / sides;
                float x = Mathf.Cos(angle) * 0.5f;
                float z = Mathf.Sin(angle) * 0.5f;
                vertices[i] = new Vector3(x, -0.5f, z);
                vertices[i + sides] = new Vector3(x, 0.5f, z);
            }

            var triangles = new List<int>(sides * 12);
            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                triangles.Add(i); triangles.Add(next); triangles.Add(i + sides);
                triangles.Add(next); triangles.Add(next + sides); triangles.Add(i + sides);
                triangles.Add(sides * 2); triangles.Add(next); triangles.Add(i);
                triangles.Add(sides * 2 + 1); triangles.Add(i + sides); triangles.Add(next + sides);
            }
            mesh.vertices = vertices;
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Mesh CreateRoofMesh()
        {
            var mesh = new Mesh { name = "Activity Diorama Gable Roof" };
            mesh.vertices = new[]
            {
                new Vector3(-.5f,0,-.5f), new Vector3(.5f,0,-.5f), new Vector3(0,1,-.5f),
                new Vector3(-.5f,0,.5f), new Vector3(.5f,0,.5f), new Vector3(0,1,.5f)
            };
            mesh.triangles = new[] { 0,2,1, 3,4,5, 0,3,5, 0,5,2, 1,2,5, 1,5,4, 0,1,4, 0,4,3 };
            mesh.RecalculateNormals();
            return mesh;
        }

        private void OnDestroy()
        {
            if (stageCamera != null) stageCamera.targetTexture = null;
            if (texture != null)
            {
                texture.Release();
                DestroyOwned(texture);
            }
            if (stageRoot != null) DestroyOwned(stageRoot);
            foreach (Mesh mesh in ownedMeshes) if (mesh != null) DestroyOwned(mesh);
            foreach (Material material in ownedMaterials) if (material != null) DestroyOwned(material);
        }

        private static void DestroyOwned(Object owned)
        {
            if (owned == null) return;
            if (Application.isPlaying) Object.Destroy(owned);
            else Object.DestroyImmediate(owned);
        }
    }
}
