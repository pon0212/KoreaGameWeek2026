using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TastePassport.World
{
    /// <summary>Builds a small, runtime-owned low-poly character with simple idle and walk poses.</summary>
    public sealed class WorldCharacterModelBuilder : MonoBehaviour
    {
        private readonly List<Mesh> ownedMeshes = new List<Mesh>();
        private readonly List<Material> ownedMaterials = new List<Material>();

        private Mesh cubeMesh;
        private Mesh octahedronMesh;
        private Transform modelRoot;
        private Transform leftArm;
        private Transform rightArm;
        private Transform leftLeg;
        private Transform rightLeg;
        private Transform torso;
        private Vector3 rootOrigin;
        private bool isPlayer;
        private bool walking;
        private float phase;

        public static WorldCharacterModelBuilder Create(Transform parent, string characterId, bool isPlayer)
        {
            if (parent == null)
            {
                Debug.LogWarning("Cannot create a 3D character: an owned runtime parent is required.");
                return null;
            }
            if (string.IsNullOrEmpty(characterId))
            {
                Debug.LogWarning("Cannot create a 3D character: the character id is empty.", parent);
                return null;
            }

            var character = new GameObject("3D Character - " + characterId);
            character.transform.SetParent(parent, false);
            var builder = character.AddComponent<WorldCharacterModelBuilder>();
            builder.Initialize(characterId, isPlayer);
            return builder;
        }

        public void SetWalking(bool value)
        {
            walking = isPlayer && value;
        }

        public void SetFacingLeft(bool value)
        {
            if (modelRoot != null)
                modelRoot.localScale = new Vector3(value ? -1f : 1f, 1f, 1f);
        }

        private void Initialize(string characterId, bool playerCharacter)
        {
            isPlayer = playerCharacter;
            Shader shader = FindSurfaceShader();
            if (shader == null)
            {
                Debug.LogError("Cannot build the 3D character " + characterId + ": no supported surface shader is included in this player.", this);
                return;
            }

            cubeMesh = OwnMesh(CreateCubeMesh());
            octahedronMesh = OwnMesh(CreateOctahedronMesh());
            Material skin = CreateMaterial(shader, new Color(0.78f, 0.54f, 0.40f));
            Material outfit = CreateMaterial(shader, GetOutfitColor(characterId, playerCharacter));
            Material garment = CreateMaterial(shader, GetGarmentColor(characterId, playerCharacter));
            Material hair = CreateMaterial(shader, new Color(0.18f, 0.14f, 0.13f));
            Material accent = CreateMaterial(shader, playerCharacter ? new Color(0.92f, 0.69f, 0.29f) : new Color(0.83f, 0.62f, 0.34f));
            Material dark = CreateMaterial(shader, new Color(0.22f, 0.20f, 0.19f));

            modelRoot = new GameObject("Character Rig").transform;
            modelRoot.SetParent(transform, false);
            rootOrigin = modelRoot.localPosition;

            // All dimensions are authored at 4.2 units tall. Session parent transforms keep the
            // original sprite scale and target coordinates, so collider/save movement is untouched.
            CreateBox(modelRoot, "Travel Coat", new Vector3(0f, 2.10f, 0f), new Vector3(1.58f, 1.62f, 0.62f), outfit);
            CreateBox(modelRoot, "Coat Hem", new Vector3(0f, 1.36f, -0.01f), new Vector3(1.64f, 0.18f, 0.65f), garment);
            CreateBox(modelRoot, "Waist Sash", new Vector3(0f, 1.79f, -0.34f), new Vector3(1.48f, 0.16f, 0.08f), accent);
            CreateBox(modelRoot, "Collar", new Vector3(0f, 2.77f, -0.03f), new Vector3(0.60f, 0.29f, 0.66f), garment);

            CreateSphere(modelRoot, "Neck", new Vector3(0f, 2.94f, 0f), new Vector3(0.47f, 0.47f, 0.45f), skin);
            CreateSphere(modelRoot, "Head", new Vector3(0f, 3.43f, -0.02f), new Vector3(1.03f, 1.08f, 0.87f), skin);
            CreateSphere(modelRoot, "Hair", new Vector3(0f, 3.79f, 0.01f), new Vector3(1.12f, 0.58f, 0.93f), hair);
            CreateSphere(modelRoot, "Left Eye", new Vector3(-0.23f, 3.48f, -0.40f), new Vector3(0.075f, 0.10f, 0.07f), dark);
            CreateSphere(modelRoot, "Right Eye", new Vector3(0.23f, 3.48f, -0.40f), new Vector3(0.075f, 0.10f, 0.07f), dark);
            CreateSphere(modelRoot, "Nose", new Vector3(0f, 3.30f, -0.47f), new Vector3(0.12f, 0.14f, 0.10f), skin);
            CreateBox(modelRoot, "Shoulder Scarf", new Vector3(0.52f, 2.46f, -0.32f), new Vector3(0.38f, 0.32f, 0.11f), accent);

            if (playerCharacter)
                CreateBox(modelRoot, "Traveler Satchel", new Vector3(-0.86f, 1.88f, 0.27f), new Vector3(0.42f, 0.74f, 0.27f), garment);

            leftArm = CreateLimb(modelRoot, "Left Arm", new Vector3(-0.90f, 2.57f, -0.01f), outfit, true);
            rightArm = CreateLimb(modelRoot, "Right Arm", new Vector3(0.90f, 2.57f, -0.01f), outfit, true);
            leftLeg = CreateLimb(modelRoot, "Left Leg", new Vector3(-0.43f, 1.21f, 0f), garment, false);
            rightLeg = CreateLimb(modelRoot, "Right Leg", new Vector3(0.43f, 1.21f, 0f), garment, false);

            CreateBox(leftLeg, "Left Shoe", new Vector3(0f, -0.62f, -0.16f), new Vector3(0.56f, 0.23f, 0.55f), dark);
            CreateBox(rightLeg, "Right Shoe", new Vector3(0f, -0.62f, -0.16f), new Vector3(0.56f, 0.23f, 0.55f), dark);
            torso = modelRoot.Find("Travel Coat");
        }

        private Transform CreateLimb(Transform parent, string name, Vector3 pivot, Material material, bool arm)
        {
            var limb = new GameObject(name).transform;
            limb.SetParent(parent, false);
            limb.localPosition = pivot;
            Vector3 segmentPosition = arm ? new Vector3(0f, -0.43f, 0f) : new Vector3(0f, -0.45f, 0f);
            Vector3 segmentScale = arm ? new Vector3(0.46f, 1.03f, 0.48f) : new Vector3(0.54f, 1.00f, 0.53f);
            CreateBox(limb, name + " Sleeve", segmentPosition, segmentScale, material);
            Material skin = ownedMaterials.Count > 0 ? ownedMaterials[0] : material;
            if (arm) CreateSphere(limb, name + " Hand", new Vector3(0f, -0.90f, -0.02f), new Vector3(0.43f, 0.42f, 0.42f), skin);
            return limb;
        }

        private void Update()
        {
            if (modelRoot == null) return;
            phase += Time.deltaTime * (walking ? 11.5f : isPlayer ? 2.1f : 1.7f);
            float gait = walking ? Mathf.Sin(phase) : 0f;
            float idle = Mathf.Sin(phase) * 0.018f;
            modelRoot.localPosition = rootOrigin + Vector3.up * (walking ? Mathf.Abs(Mathf.Sin(phase * 2f)) * 0.035f : idle);

            if (isPlayer && walking)
            {
                leftLeg.localRotation = Quaternion.Euler(0f, 0f, gait * 31f);
                rightLeg.localRotation = Quaternion.Euler(0f, 0f, -gait * 31f);
                leftArm.localRotation = Quaternion.Euler(0f, 0f, -gait * 24f);
                rightArm.localRotation = Quaternion.Euler(0f, 0f, gait * 24f);
            }
            else
            {
                float idleSway = Mathf.Sin(phase) * (isPlayer ? 1.2f : 2.3f);
                leftLeg.localRotation = Quaternion.Euler(0f, 0f, idleSway * 0.15f);
                rightLeg.localRotation = Quaternion.Euler(0f, 0f, -idleSway * 0.15f);
                leftArm.localRotation = Quaternion.Euler(0f, 0f, idleSway);
                rightArm.localRotation = Quaternion.Euler(0f, 0f, -idleSway);
            }
            if (torso != null) torso.localScale = new Vector3(1.58f, 1.62f + Mathf.Sin(phase) * 0.015f, 0.62f);
        }

        private void CreateBox(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            CreatePart(parent, name, cubeMesh, position, scale, material);
        }

        private void CreateSphere(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            CreatePart(parent, name, octahedronMesh, position, scale, material);
        }

        private Transform CreatePart(Transform parent, string name, Mesh mesh, Vector3 position, Vector3 scale, Material material)
        {
            var part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = part.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return part.transform;
        }

        private Material CreateMaterial(Shader shader, Color color)
        {
            var material = new Material(shader) { name = "3D Character Material", color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            ownedMaterials.Add(material);
            return material;
        }

        private Mesh OwnMesh(Mesh mesh)
        {
            ownedMeshes.Add(mesh);
            return mesh;
        }

        private static Shader FindSurfaceShader()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            return shader;
        }

        private static Color GetOutfitColor(string id, bool playerCharacter)
        {
            if (playerCharacter) return new Color(0.20f, 0.46f, 0.56f);
            if (id == "nhung") return new Color(0.63f, 0.31f, 0.25f);
            if (id == "lan") return new Color(0.24f, 0.47f, 0.57f);
            if (id == "gabo") return new Color(0.35f, 0.47f, 0.39f);
            return new Color(0.32f, 0.39f, 0.56f);
        }

        private static Color GetGarmentColor(string id, bool playerCharacter)
        {
            if (playerCharacter) return new Color(0.69f, 0.49f, 0.29f);
            if (id == "nhung") return new Color(0.82f, 0.66f, 0.44f);
            if (id == "lan") return new Color(0.77f, 0.56f, 0.30f);
            if (id == "gabo") return new Color(0.80f, 0.79f, 0.68f);
            return new Color(0.75f, 0.67f, 0.53f);
        }

        private static Mesh CreateCubeMesh()
        {
            var mesh = new Mesh { name = "3D Character Cube" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f,-0.5f,-0.5f), new Vector3(-0.5f,0.5f,-0.5f), new Vector3(0.5f,0.5f,-0.5f), new Vector3(0.5f,-0.5f,-0.5f),
                new Vector3(-0.5f,-0.5f,0.5f), new Vector3(-0.5f,0.5f,0.5f), new Vector3(0.5f,0.5f,0.5f), new Vector3(0.5f,-0.5f,0.5f)
            };
            mesh.triangles = new[]
            {
                0,1,2, 0,2,3, 4,7,6, 4,6,5, 0,4,5, 0,5,1,
                3,2,6, 3,6,7, 1,5,6, 1,6,2, 0,3,7, 0,7,4
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateOctahedronMesh()
        {
            var mesh = new Mesh { name = "3D Character Low Poly Sphere" };
            mesh.vertices = new[] { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
            mesh.triangles = new[]
            {
                0,4,3, 0,2,4, 0,5,2, 0,3,5,
                1,3,4, 1,4,2, 1,2,5, 1,5,3
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void OnDestroy()
        {
            for (int i = 0; i < ownedMaterials.Count; i++) DestroyOwned(ownedMaterials[i]);
            for (int i = 0; i < ownedMeshes.Count; i++) DestroyOwned(ownedMeshes[i]);
            ownedMaterials.Clear();
            ownedMeshes.Clear();
        }

        private static void DestroyOwned(Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }
    }
}
