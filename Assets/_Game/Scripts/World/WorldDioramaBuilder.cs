using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TastePassport.World
{
    /// <summary>Builds the owned, low-poly town dressing that sits behind the walkable characters.</summary>
    public sealed class WorldDioramaBuilder : MonoBehaviour
    {
        private readonly List<Material> ownedMaterials = new List<Material>();
        private readonly List<Mesh> ownedMeshes = new List<Mesh>();

        private Mesh cubeMesh;
        private Mesh octahedronMesh;
        private Mesh cylinderMesh;
        private Mesh gableMesh;
        private Shader surfaceShader;
        private Material wallMaterial;
        private Material alternateWallMaterial;
        private Material trimMaterial;
        private Material roofMaterial;
        private Material darkRoofMaterial;
        private Material streetMaterial;
        private Material pathMaterial;
        private Material woodMaterial;
        private Material lanternMaterial;
        private Material craftedLanternMaterial;
        private Material paperMaterial;
        private Material paperAccentMaterial;
        private Material gardenMaterial;
        private Material skyMaterial;
        private Material hillMaterial;
        private Material cloudMaterial;
        private Transform lanternDisplay;
        private Transform hanjiDisplay;
        private Transform fanShape;
        private Transform flowerShape;
        private Transform bookmarkShape;

        public static WorldDioramaBuilder Build(Transform parent, WorldDefinition world)
        {
            if (parent == null)
            {
                Debug.LogWarning("Cannot build the 3D town: an owned runtime parent is required.");
                return null;
            }
            if (world == null || string.IsNullOrEmpty(world.Id) || world.Interactables == null)
            {
                Debug.LogWarning("Cannot build the 3D town: the destination definition is missing or incomplete.", parent);
                return null;
            }

            var root = new GameObject("Low Poly Town Diorama");
            root.transform.SetParent(parent, false);
            var builder = root.AddComponent<WorldDioramaBuilder>();
            builder.Initialize(world);
            return builder;
        }

        private void Initialize(WorldDefinition world)
        {
            bool korean = world.Id == "jeonju";
            surfaceShader = FindSurfaceShader();
            if (surfaceShader == null)
            {
                Debug.LogError("Cannot build the 3D town: no supported surface shader is included in this player.", this);
                return;
            }

            cubeMesh = OwnMesh(CreateCubeMesh("Town Cube"));
            octahedronMesh = OwnMesh(CreateOctahedronMesh("Town Low Poly Sphere"));
            cylinderMesh = OwnMesh(CreateCylinderMesh("Town Low Poly Cylinder", 10));
            gableMesh = OwnMesh(CreateGableMesh("Town Gable Roof"));

            wallMaterial = CreateMaterial(surfaceShader, korean ? new Color(0.86f, 0.78f, 0.62f) : new Color(0.88f, 0.68f, 0.43f));
            alternateWallMaterial = CreateMaterial(surfaceShader, korean ? new Color(0.75f, 0.68f, 0.55f) : new Color(0.94f, 0.78f, 0.55f));
            trimMaterial = CreateMaterial(surfaceShader, korean ? new Color(0.30f, 0.24f, 0.20f) : new Color(0.33f, 0.24f, 0.18f));
            roofMaterial = CreateMaterial(surfaceShader, korean ? new Color(0.24f, 0.34f, 0.40f) : new Color(0.66f, 0.24f, 0.19f));
            darkRoofMaterial = CreateMaterial(surfaceShader, korean ? new Color(0.16f, 0.23f, 0.28f) : new Color(0.47f, 0.20f, 0.16f));
            streetMaterial = CreateMaterial(surfaceShader, korean ? new Color(0.48f, 0.51f, 0.48f) : new Color(0.51f, 0.43f, 0.34f));
            pathMaterial = CreateMaterial(surfaceShader, korean ? new Color(0.72f, 0.68f, 0.57f) : new Color(0.81f, 0.69f, 0.50f));
            woodMaterial = CreateMaterial(surfaceShader, korean ? new Color(0.38f, 0.29f, 0.21f) : new Color(0.42f, 0.27f, 0.16f));
            lanternMaterial = CreateMaterial(surfaceShader, new Color(0.88f, 0.39f, 0.22f));
            craftedLanternMaterial = CreateMaterial(surfaceShader, new Color(0.88f, 0.39f, 0.22f));
            paperMaterial = CreateMaterial(surfaceShader, new Color(0.93f, 0.84f, 0.65f));
            paperAccentMaterial = CreateMaterial(surfaceShader, new Color(0.35f, 0.48f, 0.43f));
            gardenMaterial = CreateMaterial(surfaceShader, new Color(0.31f, 0.44f, 0.34f));
            skyMaterial = CreateMaterial(surfaceShader, korean ? new Color(0.60f, 0.76f, 0.82f) : new Color(0.80f, 0.72f, 0.60f));
            hillMaterial = CreateMaterial(surfaceShader, korean ? new Color(0.36f, 0.52f, 0.47f) : new Color(0.43f, 0.53f, 0.39f));
            cloudMaterial = CreateMaterial(surfaceShader, new Color(0.92f, 0.87f, 0.74f));

            BuildBackdrop(world);
            BuildStreet(world);
            BuildTownRow(world, korean);
            if (korean) BuildJeonjuLandmarks(world);
            else BuildHoiAnLandmarks(world);
            CreateSoftFillLight();
        }

        public void ApplyLanternColor(string colorId)
        {
            if (lanternDisplay == null || craftedLanternMaterial == null) return;
            craftedLanternMaterial.color = colorId == "blue" ? new Color(0.34f, 0.57f, 0.82f) :
                colorId == "green" ? new Color(0.38f, 0.68f, 0.43f) :
                colorId == "gold" ? new Color(0.94f, 0.68f, 0.25f) :
                new Color(0.88f, 0.30f, 0.22f);
        }

        public void ApplyHanjiVariant(string shapeId, string colorId)
        {
            if (hanjiDisplay == null) return;

            bool showBookmark = shapeId == "bookmark";
            bool showFlower = shapeId == "flower";
            if (bookmarkShape != null) bookmarkShape.gameObject.SetActive(showBookmark);
            if (flowerShape != null) flowerShape.gameObject.SetActive(showFlower);
            if (fanShape != null) fanShape.gameObject.SetActive(!showBookmark && !showFlower);

            if (paperAccentMaterial == null) return;
            paperAccentMaterial.color = colorId == "indigo" ? new Color(0.31f, 0.40f, 0.69f) :
                colorId == "vermilion" ? new Color(0.77f, 0.28f, 0.22f) :
                colorId == "gold" ? new Color(0.82f, 0.59f, 0.22f) :
                colorId == "jade" ? new Color(0.24f, 0.56f, 0.42f) :
                new Color(0.31f, 0.40f, 0.69f);
        }

        private void BuildStreet(WorldDefinition world)
        {
            // The movement lane follows the saved spawn row (y = -1.8 in both towns),
            // not the midpoint of the full camera/world bounds. This matches the space
            // between the existing north/south collision barriers and the NPC row.
            float laneCenter = world.SpawnPoint.y;
            float width = world.MaximumPosition.x - world.MinimumPosition.x + 0.45f;
            CreateBox(transform, "Raised Street Foundation", new Vector3(0f, laneCenter - 0.02f, 0.58f),
                new Vector3(width, 1.13f, 0.68f), streetMaterial);
            CreateBox(transform, "Walkable Stone Path", new Vector3(0f, laneCenter + 0.05f, 0.18f),
                new Vector3(width - 0.28f, 0.56f, 0.26f), pathMaterial);

            int pavers = Mathf.Clamp(Mathf.RoundToInt(width / 0.82f), 8, 18);
            for (int i = 0; i < pavers; i++)
            {
                float x = world.MinimumPosition.x + 0.35f + i * (width - 0.7f) / Mathf.Max(1, pavers - 1);
                CreateBox(transform, "Stone Paver " + i, new Vector3(x, laneCenter + 0.05f, 0.02f),
                    new Vector3(0.58f, 0.10f, 0.08f), i % 2 == 0 ? pathMaterial : streetMaterial);
            }

            CreateBox(transform, "Back Garden Strip", new Vector3(0f, 0.40f, 1.08f),
                new Vector3(width, 0.30f, 0.80f), gardenMaterial);
            for (int i = 0; i < 7; i++)
            {
                float x = world.MinimumPosition.x + 0.95f + i * (width - 1.9f) / 6f;
                CreateSphere(transform, "Garden Shrub " + i, new Vector3(x, 0.67f + (i % 2) * 0.08f, 0.76f),
                    new Vector3(0.42f, 0.42f, 0.42f), gardenMaterial);
            }
        }

        private void BuildBackdrop(WorldDefinition world)
        {
            float width = world.MaximumPosition.x - world.MinimumPosition.x + 2.2f;
            CreateBox(transform, "3D Sky Backdrop", new Vector3(0f, 0f, 3.45f), new Vector3(width, 7.55f, 0.12f), skyMaterial);
            for (int i = 0; i < 5; i++)
            {
                float x = world.MinimumPosition.x + 0.5f + i * (width - 1f) / 4f;
                float height = 0.78f + (i % 3) * 0.14f;
                CreateMeshPart(transform, "Distant Hill " + i, gableMesh,
                    new Vector3(x, 2.15f, 2.95f + (i % 2) * 0.08f), new Vector3(1.88f, height, 0.09f), hillMaterial);
            }
            for (int i = 0; i < 4; i++)
            {
                float x = world.MinimumPosition.x + 1.5f + i * (width - 3f) / 3f;
                CreateSphere(transform, "Distant Cloud " + i, new Vector3(x, 2.72f + (i % 2) * 0.22f, 2.76f),
                    new Vector3(0.66f, 0.28f, 0.15f), cloudMaterial);
            }
        }

        private void CreateSoftFillLight()
        {
            var lightObject = new GameObject("Soft Town Light", typeof(Light));
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localRotation = Quaternion.Euler(42f, -28f, 0f);
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.93f, 0.82f);
            light.intensity = 0.85f;
            light.shadows = LightShadows.None;
        }

        private void BuildTownRow(WorldDefinition world, bool korean)
        {
            float[] positions =
            {
                world.MinimumPosition.x + 1.45f,
                world.MinimumPosition.x + 4.45f,
                world.MaximumPosition.x - 4.45f,
                world.MaximumPosition.x - 1.45f
            };
            float[] widths = { 2.25f, 2.55f, 2.40f, 2.25f };
            float[] heights = { 1.28f, 1.48f, 1.36f, 1.20f };
            for (int i = 0; i < positions.Length; i++)
            {
                BuildHouse(transform, "Street House " + (i + 1), positions[i], 0.43f, widths[i], heights[i], 0.88f,
                    i % 2 == 0 ? wallMaterial : alternateWallMaterial,
                    i % 2 == 0 ? roofMaterial : darkRoofMaterial, korean);
            }

            // Roof edge lines and carved timber eaves make the depth visible even on a flat phone view.
            for (int i = 0; i < 6; i++)
            {
                float x = world.MinimumPosition.x + 1.2f + i * (world.MaximumPosition.x - world.MinimumPosition.x - 2.4f) / 5f;
                CreateBox(transform, "Street Lantern Post " + i, new Vector3(x, -0.10f, 0.27f),
                    new Vector3(0.09f, 1.08f, 0.12f), woodMaterial);
                CreateBox(transform, "Street Lantern Arm " + i, new Vector3(x + 0.22f, 0.35f, 0.25f),
                    new Vector3(0.50f, 0.08f, 0.13f), woodMaterial);
                CreateLantern(transform, "Street Lantern " + i, new Vector3(x + 0.40f, 0.10f, 0.15f), 0.42f);
            }
        }

        private void BuildHoiAnLandmarks(WorldDefinition world)
        {
            WorldInteractableDefinition bridge = Find(world, "chuacau");
            if (bridge != null) BuildBridge(bridge.Position);
            WorldInteractableDefinition lantern = Find(world, "lantern-display");
            if (lantern != null) BuildLanternStand(lantern.Position);
        }

        private void BuildJeonjuLandmarks(WorldDefinition world)
        {
            WorldInteractableDefinition hanok = Find(world, "hanok");
            if (hanok != null)
                BuildHouse(transform, "Hanok Landmark", hanok.Position.x, hanok.Position.y + 0.89f,
                    1.62f, 0.68f, 0.65f, wallMaterial, roofMaterial, true);

            WorldInteractableDefinition palace = Find(world, "gyeonggijeon");
            if (palace != null) BuildPalaceGate(palace.Position);

            WorldInteractableDefinition pansori = Find(world, "pansori");
            if (pansori != null) BuildDrum(pansori.Position);

            WorldInteractableDefinition hanji = Find(world, "hanji-display");
            if (hanji != null) BuildHanjiStand(hanji.Position);
        }

        private void BuildHouse(Transform parent, string name, float x, float baseY, float width, float wallHeight,
            float depth, Material walls, Material roof, bool hanok)
        {
            float z = 0.82f;
            CreateBox(parent, name + " Walls", new Vector3(x, baseY + wallHeight * 0.5f, z),
                new Vector3(width, wallHeight, depth), walls);
            CreateMeshPart(parent, name + " Gabled Roof", gableMesh,
                new Vector3(x, baseY + wallHeight, z - 0.04f), new Vector3(width * 0.58f, hanok ? 0.63f : 0.47f, depth * 1.28f), roof);

            float windowSize = hanok ? 0.23f : 0.20f;
            float windowY = baseY + wallHeight * 0.57f;
            CreateBox(parent, name + " Left Window", new Vector3(x - width * 0.27f, windowY, z - depth * 0.52f),
                new Vector3(windowSize, windowSize * 1.18f, 0.05f), trimMaterial);
            CreateBox(parent, name + " Right Window", new Vector3(x + width * 0.27f, windowY, z - depth * 0.52f),
                new Vector3(windowSize, windowSize * 1.18f, 0.05f), trimMaterial);
            CreateBox(parent, name + " Door", new Vector3(x, baseY + wallHeight * 0.20f, z - depth * 0.54f),
                new Vector3(width * 0.20f, wallHeight * 0.39f, 0.055f), woodMaterial);
            CreateBox(parent, name + " Eaves", new Vector3(x, baseY + wallHeight, z - depth * 0.12f),
                new Vector3(width * 1.17f, 0.10f, depth * 1.35f), trimMaterial);
        }

        private void BuildBridge(Vector2 target)
        {
            float x = target.x;
            float y = target.y + 1.14f;
            CreateBox(transform, "Hoi An Bridge - Lower Pier", new Vector3(x, y - 0.23f, 0.55f),
                new Vector3(2.05f, 0.28f, 0.86f), woodMaterial);
            CreateBox(transform, "Hoi An Bridge - Deck", new Vector3(x, y, 0.22f),
                new Vector3(2.22f, 0.17f, 0.92f), pathMaterial);
            for (int side = -1; side <= 1; side += 2)
            {
                CreateBox(transform, "Bridge Post " + side, new Vector3(x + side * 0.83f, y + 0.34f, 0.13f),
                    new Vector3(0.14f, 0.66f, 0.18f), woodMaterial);
                CreateBox(transform, "Bridge Rail " + side, new Vector3(x + side * 0.83f, y + 0.57f, 0.08f),
                    new Vector3(0.12f, 0.10f, 0.20f), trimMaterial);
            }
            CreateBox(transform, "Bridge Roof Beam", new Vector3(x, y + 0.84f, 0.23f),
                new Vector3(1.95f, 0.12f, 0.44f), darkRoofMaterial);
            CreateLantern(transform, "Bridge Hanging Lantern", new Vector3(x, y + 0.51f, -0.05f), 0.48f);
        }

        private void BuildPalaceGate(Vector2 target)
        {
            float x = target.x;
            float y = target.y + 1.05f;
            for (int side = -1; side <= 1; side += 2)
            {
                CreateBox(transform, "Palace Gate Pillar " + side, new Vector3(x + side * 0.49f, y + 0.50f, 0.38f),
                    new Vector3(0.20f, 1.05f, 0.32f), trimMaterial);
                CreateBox(transform, "Palace Gate Base " + side, new Vector3(x + side * 0.49f, y + 0.06f, 0.36f),
                    new Vector3(0.37f, 0.15f, 0.48f), pathMaterial);
            }
            CreateBox(transform, "Palace Gate Header", new Vector3(x, y + 1.07f, 0.28f),
                new Vector3(1.38f, 0.18f, 0.52f), woodMaterial);
            CreateMeshPart(transform, "Palace Gate Roof", gableMesh, new Vector3(x, y + 1.16f, 0.17f),
                new Vector3(0.87f, 0.43f, 0.56f), roofMaterial);
        }

        private void BuildDrum(Vector2 target)
        {
            float x = target.x;
            float y = target.y + 0.39f;
            CreateCylinder(transform, "Pansori Drum Body", new Vector3(x, y, 0.45f),
                new Vector3(0.69f, 0.64f, 0.57f), woodMaterial, true);
            CreateBox(transform, "Pansori Drum Strap", new Vector3(x, y, 0.13f),
                new Vector3(0.13f, 0.73f, 0.05f), trimMaterial);
            CreateCylinder(transform, "Pansori Drum Head", new Vector3(x, y + 0.33f, 0.40f),
                new Vector3(0.68f, 0.10f, 0.57f), paperMaterial, true);
        }

        private void BuildLanternStand(Vector2 target)
        {
            float x = target.x;
            float y = target.y + 0.35f;
            CreateBox(transform, "Lantern Display Counter", new Vector3(x, y, 0.56f),
                new Vector3(1.02f, 0.15f, 0.62f), woodMaterial);
            CreateBox(transform, "Lantern Display Pedestal", new Vector3(x, y - 0.37f, 0.61f),
                new Vector3(0.72f, 0.70f, 0.44f), wallMaterial);
            lanternDisplay = new GameObject("Crafted Lantern Display").transform;
            lanternDisplay.SetParent(transform, false);
            lanternDisplay.localPosition = new Vector3(x, y + 0.64f, 0.23f);
            CreateLantern(lanternDisplay, "Lantern", Vector3.zero, 0.85f, true);
        }

        private void BuildHanjiStand(Vector2 target)
        {
            float x = target.x;
            float y = target.y + 0.29f;
            CreateBox(transform, "Hanji Craft Table", new Vector3(x, y, 0.58f),
                new Vector3(1.20f, 0.17f, 0.69f), woodMaterial);
            for (int side = -1; side <= 1; side += 2)
                CreateBox(transform, "Hanji Table Leg " + side, new Vector3(x + side * 0.43f, y - 0.34f, 0.61f),
                    new Vector3(0.12f, 0.60f, 0.15f), woodMaterial);

            hanjiDisplay = new GameObject("Hanji Product Display").transform;
            hanjiDisplay.SetParent(transform, false);
            hanjiDisplay.localPosition = new Vector3(x, y + 0.43f, 0.21f);
            fanShape = CreateMeshPart(hanjiDisplay, "Hanji Fan", gableMesh, new Vector3(0f, 0.13f, 0f),
                new Vector3(0.48f, 0.48f, 0.14f), paperAccentMaterial);
            CreateBox(fanShape, "Fan Rib", new Vector3(0f, -0.17f, -0.12f), new Vector3(0.10f, 0.40f, 0.08f), woodMaterial);

            bookmarkShape = CreateBox(hanjiDisplay, "Hanji Bookmark", new Vector3(0f, 0.13f, 0f),
                new Vector3(0.27f, 0.83f, 0.12f), paperAccentMaterial);
            CreateBox(bookmarkShape, "Bookmark Tassel", new Vector3(0f, -0.49f, -0.02f),
                new Vector3(0.09f, 0.18f, 0.08f), woodMaterial);

            flowerShape = new GameObject("Hanji Flower").transform;
            flowerShape.SetParent(hanjiDisplay, false);
            CreateSphere(flowerShape, "Flower Center", new Vector3(0f, 0.16f, 0f),
                new Vector3(0.24f, 0.24f, 0.16f), paperAccentMaterial);
            for (int i = 0; i < 5; i++)
            {
                float angle = i * 72f * Mathf.Deg2Rad;
                CreateSphere(flowerShape, "Flower Petal " + i,
                    new Vector3(Mathf.Cos(angle) * 0.22f, 0.16f + Mathf.Sin(angle) * 0.22f, 0.02f),
                    new Vector3(0.26f, 0.26f, 0.14f), paperMaterial);
            }
            bookmarkShape.gameObject.SetActive(false);
            flowerShape.gameObject.SetActive(false);
        }

        private void CreateLantern(Transform parent, string name, Vector3 position, float size, bool crafted = false)
        {
            CreateBox(parent, name + " Cap", position + new Vector3(0f, size * 0.52f, 0f),
                new Vector3(size * 0.66f, size * 0.13f, size * 0.36f), woodMaterial);
            CreateOctahedron(parent, name + " Shade", position, new Vector3(size * 0.55f, size * 0.62f, size * 0.42f),
                crafted ? craftedLanternMaterial : lanternMaterial);
            CreateBox(parent, name + " Base", position + new Vector3(0f, -size * 0.49f, 0f),
                new Vector3(size * 0.68f, size * 0.11f, size * 0.38f), woodMaterial);
            CreateBox(parent, name + " Hanging Cord", position + new Vector3(0f, size * 0.70f, 0f),
                new Vector3(size * 0.06f, size * 0.27f, size * 0.06f), woodMaterial);
        }

        private WorldInteractableDefinition Find(WorldDefinition world, string id)
        {
            foreach (WorldInteractableDefinition definition in world.Interactables)
                if (definition != null && definition.Id == id) return definition;
            return null;
        }

        private Transform CreateBox(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            return CreateMeshPart(parent, name, cubeMesh, position, scale, material);
        }

        private Transform CreateSphere(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            return CreateMeshPart(parent, name, octahedronMesh, position, scale, material);
        }

        private Transform CreateOctahedron(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            return CreateSphere(parent, name, position, scale, material);
        }

        private Transform CreateCylinder(Transform parent, string name, Vector3 position, Vector3 scale, Material material, bool horizontal)
        {
            Transform part = CreateMeshPart(parent, name, cylinderMesh, position, scale, material);
            if (horizontal) part.localRotation = Quaternion.Euler(90f, 0f, 0f);
            return part;
        }

        private Transform CreateMeshPart(Transform parent, string name, Mesh mesh, Vector3 position, Vector3 scale, Material material)
        {
            var part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            MeshFilter filter = part.GetComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = part.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return part.transform;
        }

        private Material CreateMaterial(Shader shader, Color color)
        {
            if (shader == null) return null;
            var material = new Material(shader) { name = "3D Town - " + color, color = color };
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

        private static Mesh CreateCubeMesh(string name)
        {
            var mesh = new Mesh { name = name };
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

        private static Mesh CreateOctahedronMesh(string name)
        {
            var mesh = new Mesh { name = name };
            mesh.vertices = new[]
            {
                Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back
            };
            mesh.triangles = new[]
            {
                0,4,3, 0,2,4, 0,5,2, 0,3,5,
                1,3,4, 1,4,2, 1,2,5, 1,5,3
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateCylinderMesh(string name, int sides)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int i = 0; i <= sides; i++)
            {
                float angle = Mathf.PI * 2f * i / sides;
                float x = Mathf.Cos(angle) * 0.5f;
                float z = Mathf.Sin(angle) * 0.5f;
                vertices.Add(new Vector3(x, -0.5f, z));
                vertices.Add(new Vector3(x, 0.5f, z));
            }
            int bottomCenter = vertices.Count;
            vertices.Add(new Vector3(0f, -0.5f, 0f));
            int topCenter = vertices.Count;
            vertices.Add(new Vector3(0f, 0.5f, 0f));
            for (int i = 0; i < sides; i++)
            {
                int a = i * 2;
                int b = a + 1;
                int c = a + 2;
                int d = a + 3;
                triangles.AddRange(new[] { a, b, c, b, d, c });
                int nextBottom = (i + 1) * 2;
                int nextTop = nextBottom + 1;
                triangles.AddRange(new[] { bottomCenter, a, nextBottom, topCenter, nextTop, b });
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateGableMesh(string name)
        {
            var mesh = new Mesh { name = name };
            mesh.vertices = new[]
            {
                new Vector3(-1f,0f,-0.5f), new Vector3(1f,0f,-0.5f), new Vector3(0f,1f,-0.5f),
                new Vector3(-1f,0f,0.5f), new Vector3(1f,0f,0.5f), new Vector3(0f,1f,0.5f)
            };
            mesh.triangles = new[]
            {
                0,2,1, 3,4,5, 0,4,3, 0,1,4,
                0,5,2, 0,3,5, 1,5,4, 1,2,5
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

        private static void DestroyOwned(UnityEngine.Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }
    }
}
