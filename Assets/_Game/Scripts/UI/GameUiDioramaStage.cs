using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TastePassport.UI
{
    /// <summary>A reusable, isolated backdrop for the passport UI.</summary>
    public sealed class GameUiDioramaStage : MonoBehaviour
    {
        private const int StageLayer = 31;
        private static int nextStageId;
        private readonly List<Material> materials = new List<Material>();
        private GameObject stageRoot;
        private GameObject vietnam;
        private GameObject korea;
        private Camera stageCamera;
        private RenderTexture texture;
        private Mesh cubeMesh;
        private Mesh roofMesh;
        private Transform lanternLeft;
        private Transform lanternRight;
        private bool stageVisible = true;
        private bool koreanDestination;

        public Texture StageTexture => texture;

        public static GameUiDioramaStage Ensure(Transform owner)
        {
            if (owner == null)
            {
                Debug.LogWarning("Cannot create a UI diorama without an owner.");
                return null;
            }

            var existing = owner.GetComponentInChildren<GameUiDioramaStage>(true);
            if (existing != null) return existing;
            var holder = new GameObject("UI Diorama Stage");
            holder.transform.SetParent(owner, false);
            return holder.AddComponent<GameUiDioramaStage>();
        }

        private void Initialize()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null)
            {
                Debug.LogError("Cannot create the UI diorama: no supported unlit shader was found.", this);
                return;
            }

            // A unique, remote location prevents other stage instances sharing the layer
            // from entering this camera's small orthographic frustum.
            stageRoot = new GameObject("UI Diorama World");
            stageRoot.transform.position = new Vector3(10000f + nextStageId++ * 100f, 10000f, 10000f);
            stageRoot.hideFlags = HideFlags.HideInHierarchy;

            texture = new RenderTexture(720, 1280, 16, RenderTextureFormat.ARGB32)
            {
                name = "UI Diorama Texture",
                antiAliasing = 1
            };
            texture.Create();

            var cameraObject = new GameObject("Diorama Camera");
            cameraObject.transform.SetParent(stageRoot.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 4.2f, -12f);
            cameraObject.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
            stageCamera = cameraObject.AddComponent<Camera>();
            stageCamera.enabled = stageVisible && isActiveAndEnabled;
            stageCamera.orthographic = true;
            stageCamera.orthographicSize = 6.4f;
            stageCamera.nearClipPlane = 0.1f;
            stageCamera.farClipPlane = 30f;
            stageCamera.cullingMask = 1 << StageLayer;
            stageCamera.clearFlags = CameraClearFlags.SolidColor;
            stageCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            stageCamera.targetTexture = texture;
            stageCamera.allowHDR = false;
            stageCamera.allowMSAA = false;
            stageCamera.useOcclusionCulling = false;

            cubeMesh = CreateCube();
            roofMesh = CreateRoof();
            var cream = CreateMaterial(shader, new Color(0.91f, 0.79f, 0.59f));
            var ochre = CreateMaterial(shader, new Color(0.82f, 0.56f, 0.25f));
            var dark = CreateMaterial(shader, new Color(0.27f, 0.22f, 0.20f));
            var red = CreateMaterial(shader, new Color(0.70f, 0.25f, 0.19f));
            var green = CreateMaterial(shader, new Color(0.35f, 0.48f, 0.35f));
            var stone = CreateMaterial(shader, new Color(0.71f, 0.68f, 0.59f));
            var blue = CreateMaterial(shader, new Color(0.33f, 0.42f, 0.46f));

            vietnam = NewGroup("Hoi An", stageRoot.transform);
            korea = NewGroup("Jeonju", stageRoot.transform);
            BuildGround(vietnam.transform, green, stone);
            BuildGround(korea.transform, green, stone);
            BuildHouse(vietnam.transform, -2.1f, 1.2f, ochre, red, dark, false);
            BuildHouse(vietnam.transform, 2.0f, 1.6f, cream, red, dark, false);
            BuildHouse(korea.transform, -2.1f, 1.3f, cream, blue, dark, true);
            BuildHouse(korea.transform, 2.0f, 1.7f, cream, blue, dark, true);
            lanternLeft = BuildLantern(vietnam.transform, -1.3f, red, dark);
            lanternRight = BuildLantern(vietnam.transform, 1.2f, ochre, dark);
            SetDestination(koreanDestination);
        }

        public void SetDestination(bool korean)
        {
            koreanDestination = korean;
            if (vietnam == null || korea == null) return;
            vietnam.SetActive(!korean);
            korea.SetActive(korean);
        }

        public void SetStageVisible(bool visible)
        {
            stageVisible = visible;
            if (stageCamera != null) stageCamera.enabled = visible && isActiveAndEnabled;
        }

        private void OnEnable()
        {
            if (stageRoot == null) Initialize();
            if (stageCamera != null) stageCamera.enabled = stageVisible && isActiveAndEnabled;
        }

        private void OnDisable()
        {
            if (stageCamera != null) stageCamera.enabled = false;
        }

        private void Update()
        {
            if (!stageVisible || lanternLeft == null || !vietnam.activeSelf) return;
            float sway = Mathf.Sin(Time.time * 0.8f) * 4f;
            lanternLeft.localRotation = Quaternion.Euler(0f, 0f, sway);
            lanternRight.localRotation = Quaternion.Euler(0f, 0f, -sway);
        }

        private static GameObject NewGroup(string name, Transform parent)
        {
            var group = new GameObject(name);
            group.transform.SetParent(parent, false);
            return group;
        }

        private Material CreateMaterial(Shader shader, Color color)
        {
            var material = new Material(shader) { color = color };
            materials.Add(material);
            return material;
        }

        private void Part(Transform parent, string name, Mesh mesh, Material material, Vector3 position, Vector3 scale)
        {
            var part = NewGroup(name, parent);
            part.layer = StageLayer;
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void BuildGround(Transform parent, Material grass, Material path)
        {
            Part(parent, "Ground", cubeMesh, grass, new Vector3(0, -0.3f, 2f), new Vector3(8f, 0.5f, 8f));
            Part(parent, "Path", cubeMesh, path, new Vector3(0, -0.02f, -0.3f), new Vector3(1.8f, 0.08f, 4f));
        }

        private void BuildHouse(Transform parent, float x, float z, Material wall, Material roof, Material trim, bool hanok)
        {
            Part(parent, "House", cubeMesh, wall, new Vector3(x, 1.25f, z), new Vector3(2.8f, 2.5f, 2.1f));
            Part(parent, "Roof", roofMesh, roof, new Vector3(x, 2.5f, z), new Vector3(hanok ? 3.5f : 3.1f, 0.9f, 2.7f));
            Part(parent, "Door", cubeMesh, trim, new Vector3(x, 0.75f, z - 1.08f), new Vector3(0.65f, 1.5f, 0.1f));
            Part(parent, "Window Left", cubeMesh, trim, new Vector3(x - 0.95f, 1.35f, z - 1.08f), new Vector3(0.48f, 0.65f, 0.1f));
            Part(parent, "Window Right", cubeMesh, trim, new Vector3(x + 0.95f, 1.35f, z - 1.08f), new Vector3(0.48f, 0.65f, 0.1f));
            if (hanok)
            {
                Part(parent, "Timber Beam", cubeMesh, trim, new Vector3(x, 2.2f, z - 1.1f), new Vector3(2.8f, 0.13f, 0.12f));
                Part(parent, "Timber Post Left", cubeMesh, trim, new Vector3(x - 1.28f, 1.25f, z - 1.1f), new Vector3(0.12f, 2.5f, 0.12f));
                Part(parent, "Timber Post Right", cubeMesh, trim, new Vector3(x + 1.28f, 1.25f, z - 1.1f), new Vector3(0.12f, 2.5f, 0.12f));
            }
        }

        private Transform BuildLantern(Transform parent, float x, Material color, Material trim)
        {
            var pivot = NewGroup("Lantern", parent).transform;
            pivot.localPosition = new Vector3(x, 3.7f, -0.5f);
            Part(pivot, "Cord", cubeMesh, trim, new Vector3(0, -0.2f, 0), new Vector3(0.035f, 0.4f, 0.035f));
            Part(pivot, "Lantern Body", cubeMesh, color, new Vector3(0, -0.55f, 0), new Vector3(0.38f, 0.45f, 0.38f));
            return pivot;
        }

        private static Mesh CreateCube()
        {
            // Flat-shaded faces are unnecessary for unlit materials; share eight vertices.
            var mesh = new Mesh { name = "Diorama Cube" };
            mesh.vertices = new[] { new Vector3(-.5f,-.5f,-.5f), new Vector3(.5f,-.5f,-.5f), new Vector3(.5f,.5f,-.5f), new Vector3(-.5f,.5f,-.5f), new Vector3(-.5f,-.5f,.5f), new Vector3(.5f,-.5f,.5f), new Vector3(.5f,.5f,.5f), new Vector3(-.5f,.5f,.5f) };
            mesh.triangles = new[] { 0,2,1, 0,3,2, 5,6,4, 6,7,4, 4,3,0, 4,7,3, 1,2,5, 2,6,5, 3,7,2, 2,7,6, 4,0,5, 5,0,1 };
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Mesh CreateRoof()
        {
            var mesh = new Mesh { name = "Diorama Roof" };
            mesh.vertices = new[] { new Vector3(-.5f,0,-.5f), new Vector3(.5f,0,-.5f), new Vector3(0,1,-.5f), new Vector3(-.5f,0,.5f), new Vector3(.5f,0,.5f), new Vector3(0,1,.5f) };
            mesh.triangles = new[] { 0,2,1, 3,4,5, 0,3,5, 0,5,2, 1,2,5, 1,5,4, 0,1,4, 0,4,3 };
            mesh.RecalculateNormals();
            return mesh;
        }

        private void OnDestroy()
        {
            if (stageCamera != null) stageCamera.targetTexture = null;
            if (texture != null) { texture.Release(); Destroy(texture); }
            if (stageRoot != null) Destroy(stageRoot);
            if (cubeMesh != null) Destroy(cubeMesh);
            if (roofMesh != null) Destroy(roofMesh);
            foreach (var material in materials) if (material != null) Destroy(material);
        }
    }
}
