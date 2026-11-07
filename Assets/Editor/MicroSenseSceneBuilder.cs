using System;
using System.Collections.Generic;
using System.IO;
using MicroSense;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace MicroSenseEditor
{
    public static class MicroSenseSceneBuilder
    {
        private const string PrototypeArtFolder = "Assets/Art/Prototype";
        private const string PlayerPrefabPath = "Assets/Prefabs/Players/CellAgent.prefab";
        private const string PickupPrefabPath = "Assets/Prefabs/Pickups/AbilityPickup.prefab";
        private const string AbilitySlotUpgradePrefabPath = "Assets/Prefabs/Pickups/AbilitySlotUpgrade.prefab";
        private const string GemCollectiblePrefabPath = "Assets/Prefabs/Pickups/GemCollectible.prefab";
        private const string PlatformPrefabPath = "Assets/Prefabs/Environment/Platform.prefab";
        private const string HiddenPlatformPrefabPath = "Assets/Prefabs/Environment/HiddenPlatform.prefab";
        private const string BreakablePrefabPath = "Assets/Prefabs/Environment/BreakablePlatform.prefab";
        private const string ClimbablePrefabPath = "Assets/Prefabs/Environment/ClimbableSurface.prefab";
        private const string PressureButtonPrefabPath = "Assets/Prefabs/Environment/PressureButton.prefab";
        private const string LinkedDoorPrefabPath = "Assets/Prefabs/Environment/LinkedDoor.prefab";
        private const string SightIconPath = "Assets/Art/Lisa_Icon/Sight.png";
        private const string SoundIconPath = "Assets/Art/Lisa_Icon/Sound.png";
        private const string TasteIconPath = "Assets/Art/Lisa_Icon/Taste.png";
        private const string TouchIconPath = "Assets/Art/Lisa_Icon/Touch.png";
        private const string ScenePath = "Assets/Scenes/MicroSense_Prototype.unity";
        private const bool PlayerOneAllowDoubleJump = false;
        private const bool PlayerTwoAllowDoubleJump = false;
        private const int LevelMaxAbilitiesPerPlayer = 2;

        private static readonly Color PlayerOneColor = new Color(0.35f, 0.92f, 0.82f);
        private static readonly Color PlayerTwoColor = new Color(1f, 0.48f, 0.59f);
        private static readonly Color PlatformColor = new Color(0.22f, 0.38f, 0.42f);
        private static readonly Color PlatformEdgeColor = new Color(0.48f, 0.76f, 0.67f);
        private static readonly Color SightColor = new Color(0.55f, 1f, 0.78f);
        private static readonly Color SoundColor = new Color(1f, 0.8f, 0.3f);
        private static readonly Color TasteColor = new Color(1f, 0.43f, 0.58f);
        private static readonly Color TouchColor = new Color(0.72f, 0.55f, 1f);
        private static readonly Color UpgradeColor = new Color(0.35f, 0.88f, 1f);
        private static readonly Color GemColor = new Color(0.3f, 0.72f, 1f);
        private static readonly Color PuzzleColor = new Color(0.96f, 0.68f, 0.22f);
        private static readonly Color PuzzleActiveColor = new Color(0.55f, 1f, 0.72f);

        private static Sprite _circleSprite;
        private static Sprite _roundedSprite;
        private static Material _lineMaterial;
        private static int _playerLayer;
        private static int _groundLayer;
        private static int _interactableLayer;

        [MenuItem("Micro-Sense/Build Prototype Scene")]
        public static void Build()
        {
            EnsureFolders();
            EnsureLayers();
            CreatePrototypeArt();
            CreatePrefabs();
            CreateScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("MICRO-SENSE: Prototype scene and prefabs built successfully.");
        }

        [MenuItem("Micro-Sense/Build Gem Collectible Prefab")]
        public static void BuildGemCollectiblePrefab()
        {
            EnsureFolders();
            EnsureLayers();
            CreatePrototypeArt();
            SaveGemCollectiblePrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(GemCollectiblePrefabPath);
            Debug.Log("MICRO-SENSE: Gem collectible prefab built successfully.");
        }

        private static void EnsureFolders()
        {
            string[] folders =
            {
                "Assets/Art",
                PrototypeArtFolder,
                "Assets/Prefabs",
                "Assets/Prefabs/Players",
                "Assets/Prefabs/Pickups",
                "Assets/Prefabs/Environment",
                "Assets/Scenes"
            };

            foreach (string folder in folders)
            {
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
            }
        }

        private static void EnsureLayers()
        {
            _playerLayer = EnsureLayer("Player", 8);
            _groundLayer = EnsureLayer("Ground", 9);
            _interactableLayer = EnsureLayer("Interactable", 10);
            AssetDatabase.SaveAssets();
        }

        private static int EnsureLayer(string layerName, int preferredIndex)
        {
            int existingLayer = LayerMask.NameToLayer(layerName);
            if (existingLayer >= 0)
            {
                return existingLayer;
            }

            UnityEngine.Object tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
            SerializedObject tagManager = new SerializedObject(tagManagerAsset);
            SerializedProperty layers = tagManager.FindProperty("layers");
            int index = string.IsNullOrEmpty(layers.GetArrayElementAtIndex(preferredIndex).stringValue)
                ? preferredIndex
                : FindEmptyLayer(layers);

            if (index < 0)
            {
                throw new InvalidOperationException($"No free user layer is available for {layerName}.");
            }

            layers.GetArrayElementAtIndex(index).stringValue = layerName;
            tagManager.ApplyModifiedProperties();
            return index;
        }

        private static int FindEmptyLayer(SerializedProperty layers)
        {
            for (int i = 8; i < 32; i++)
            {
                if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
                {
                    return i;
                }
            }

            return -1;
        }

        private static void CreatePrototypeArt()
        {
            CreateTextureAsset($"{PrototypeArtFolder}/Circle.png", CreateCirclePixels(64));
            CreateTextureAsset($"{PrototypeArtFolder}/RoundedSquare.png", CreateRoundedSquarePixels(64, 12));
            _circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{PrototypeArtFolder}/Circle.png");
            _roundedSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{PrototypeArtFolder}/RoundedSquare.png");

            string materialPath = $"{PrototypeArtFolder}/PrototypeLine.mat";
            _lineMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (_lineMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                _lineMaterial = new Material(shader) { name = "PrototypeLine" };
                AssetDatabase.CreateAsset(_lineMaterial, materialPath);
            }
        }

        private static Color32[] CreateCirclePixels(int size)
        {
            Color32[] pixels = new Color32[size * size];
            float center = (size - 1) * 0.5f;
            float radius = size * 0.46f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(radius - distance + 0.75f) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            return pixels;
        }

        private static Color32[] CreateRoundedSquarePixels(int size, int radius)
        {
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(radius - x, x - (size - 1 - radius));
                    float dy = Mathf.Max(radius - y, y - (size - 1 - radius));
                    float distance = new Vector2(Mathf.Max(0f, dx), Mathf.Max(0f, dy)).magnitude;
                    byte alpha = (byte)(distance <= radius ? 255 : 0);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            return pixels;
        }

        private static void CreateTextureAsset(string path, Color32[] pixels)
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 64f;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        private static void CreatePrefabs()
        {
            SavePlayerPrefab();
            SavePickupPrefab();
            SaveAbilitySlotUpgradePrefab();
            SaveGemCollectiblePrefab();
            SavePlatformPrefab(PlatformPrefabPath, EnvironmentPrefabType.Standard);
            SavePlatformPrefab(HiddenPlatformPrefabPath, EnvironmentPrefabType.Hidden);
            SavePlatformPrefab(BreakablePrefabPath, EnvironmentPrefabType.Breakable);
            SavePlatformPrefab(ClimbablePrefabPath, EnvironmentPrefabType.Climbable);
            SavePressureButtonPrefab();
            SaveLinkedDoorPrefab();
        }

        private static void SavePlayerPrefab()
        {
            GameObject root = new GameObject("CellAgent");
            root.layer = _playerLayer;

            SpriteRenderer bodyVisual = root.AddComponent<SpriteRenderer>();
            bodyVisual.sprite = _circleSprite;
            bodyVisual.color = Color.white;
            bodyVisual.sortingOrder = 10;

            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 4f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            CapsuleCollider2D collider = root.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.82f, 0.98f);

            PlayerController player = root.AddComponent<PlayerController>();
            PlayerAbilityController abilityController = root.GetComponent<PlayerAbilityController>();
            SightAbility sight = root.AddComponent<SightAbility>();
            SoundAbility sound = root.AddComponent<SoundAbility>();
            TasteAbility taste = root.AddComponent<TasteAbility>();
            TouchAbility touch = root.AddComponent<TouchAbility>();

            Transform groundCheck = CreateChild(root.transform, "GroundCheck", new Vector3(0f, -0.53f, 0f));
            Transform holdAnchor = CreateChild(root.transform, "HoldAnchor", new Vector3(0f, 0.15f, 0f));

            GameObject aura = CreateSpriteObject("Aura", root.transform, Vector3.zero, Vector3.one * 1.3f, new Color(1f, 1f, 1f, 0.14f), _circleSprite, 8);
            aura.transform.SetAsFirstSibling();
            CreateFace(root.transform);

            GameObject pulse = CreateSpriteObject("SoundPulse", root.transform, Vector3.zero, Vector3.one, new Color(1f, 0.82f, 0.35f, 0.6f), _circleSprite, 7);
            LineRenderer scanLine = CreateLineRenderer(root, "SightScan", 0.07f, 0.02f);
            LineRenderer trajectoryLine = CreateLineRenderer(root, "TasteTrajectory", 0.055f, 0.02f);

            PlayerInputBindings placeholderBindings = new PlayerInputBindings(Key.A, Key.D, Key.W, Key.S, Key.Q, Key.E);
            LayerMask groundMask = 1 << _groundLayer;
            player.Configure("Player", Color.white, placeholderBindings, groundCheck, groundMask);
            sight.Configure(7f, groundMask, scanLine);
            sound.Configure(3.2f, groundMask, true, pulse.transform);
            taste.Configure(null, holdAnchor, trajectoryLine, 1.65f, 13f);
            touch.Configure(0.9f, 4.5f, groundMask);
            abilityController.Configure(sight, sound, taste, touch);

            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void CreateFace(Transform parent)
        {
            CreateSpriteObject("EyeLeft", parent, new Vector3(-0.18f, 0.12f, 0f), Vector3.one * 0.13f, new Color(0.04f, 0.08f, 0.11f), _circleSprite, 12);
            CreateSpriteObject("EyeRight", parent, new Vector3(0.18f, 0.12f, 0f), Vector3.one * 0.13f, new Color(0.04f, 0.08f, 0.11f), _circleSprite, 12);
            GameObject mouth = CreateSpriteObject("Mouth", parent, new Vector3(0f, -0.16f, 0f), new Vector3(0.25f, 0.06f, 1f), new Color(0.04f, 0.08f, 0.11f), _roundedSprite, 12);
            mouth.transform.localRotation = Quaternion.Euler(0f, 0f, -4f);
        }

        private static void SavePickupPrefab()
        {
            GameObject root = CreateSpriteObject("AbilityPickup", null, Vector3.zero, Vector3.one, Color.white, _circleSprite, 5);
            root.layer = _interactableLayer;
            CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.5f;
            root.AddComponent<AbilityPickup>();
            root.AddComponent<AmbientDrift>().Configure(new Vector2(0f, 0.12f), 1.8f, 0f, 4f);
            CreateSpriteObject("Core", root.transform, Vector3.zero, Vector3.one * 0.45f, new Color(0.06f, 0.12f, 0.14f), _circleSprite, 6);
            PrefabUtility.SaveAsPrefabAsset(root, PickupPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void SaveAbilitySlotUpgradePrefab()
        {
            GameObject root = CreateSpriteObject(
                "AbilitySlotUpgrade",
                null,
                Vector3.zero,
                Vector3.one,
                UpgradeColor,
                _circleSprite,
                5);
            root.layer = _interactableLayer;
            CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.55f;
            root.AddComponent<AbilitySlotUpgrade>();
            root.AddComponent<AmbientDrift>().Configure(new Vector2(0f, 0.16f), 1.5f, 0f, 5f);

            CreateSpriteObject(
                "Core",
                root.transform,
                Vector3.zero,
                Vector3.one * 0.62f,
                new Color(0.05f, 0.15f, 0.2f),
                _circleSprite,
                6);
            CreateSpriteObject(
                "Plus Horizontal",
                root.transform,
                Vector3.zero,
                new Vector3(0.42f, 0.12f, 1f),
                UpgradeColor,
                _roundedSprite,
                7);
            CreateSpriteObject(
                "Plus Vertical",
                root.transform,
                Vector3.zero,
                new Vector3(0.12f, 0.42f, 1f),
                UpgradeColor,
                _roundedSprite,
                7);

            PrefabUtility.SaveAsPrefabAsset(root, AbilitySlotUpgradePrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void SaveGemCollectiblePrefab()
        {
            GameObject root = CreateSpriteObject(
                "GemCollectible",
                null,
                Vector3.zero,
                new Vector3(0.78f, 0.78f, 1f),
                GemColor,
                _roundedSprite,
                6);
            root.layer = _interactableLayer;
            root.transform.rotation = Quaternion.Euler(0f, 0f, 45f);

            BoxCollider2D trigger = root.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(0.9f, 0.9f);
            root.AddComponent<GemCollectible>();
            root.AddComponent<AmbientDrift>().Configure(new Vector2(0f, 0.16f), 1.7f, 0f, 6f);

            CreateSpriteObject(
                "Core",
                root.transform,
                Vector3.zero,
                new Vector3(0.42f, 0.42f, 1f),
                new Color(0.78f, 0.95f, 1f),
                _roundedSprite,
                7);
            CreateSpriteObject(
                "Facet",
                root.transform,
                new Vector3(-0.18f, 0.18f, 0f),
                new Vector3(0.12f, 0.3f, 1f),
                new Color(1f, 1f, 1f, 0.85f),
                _roundedSprite,
                8);

            PrefabUtility.SaveAsPrefabAsset(root, GemCollectiblePrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private enum EnvironmentPrefabType
        {
            Standard,
            Hidden,
            Breakable,
            Climbable
        }

        private static void SavePressureButtonPrefab()
        {
            GameObject root = new GameObject("PressureButton");
            root.layer = _interactableLayer;

            BoxCollider2D trigger = root.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(1.5f, 0.8f);
            trigger.offset = new Vector2(0f, 0.25f);

            CreateSpriteObject(
                "Base",
                root.transform,
                Vector3.zero,
                new Vector3(1.65f, 0.34f, 1f),
                new Color(0.16f, 0.25f, 0.28f),
                _roundedSprite,
                3);
            GameObject plate = CreateSpriteObject(
                "Plate",
                root.transform,
                new Vector3(0f, 0.23f, 0f),
                new Vector3(1.35f, 0.22f, 1f),
                PuzzleColor,
                _roundedSprite,
                4);
            GameObject indicator = CreateSpriteObject(
                "Indicator",
                root.transform,
                new Vector3(0f, 0.25f, 0f),
                new Vector3(0.42f, 0.08f, 1f),
                new Color(0.14f, 0.18f, 0.2f),
                _roundedSprite,
                5);

            root.AddComponent<PressureButton>().Configure(
                null,
                plate.transform,
                indicator.GetComponent<SpriteRenderer>(),
                new Vector3(0f, -0.14f, 0f));
            PrefabUtility.SaveAsPrefabAsset(root, PressureButtonPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void SaveLinkedDoorPrefab()
        {
            GameObject root = CreateSpriteObject(
                "LinkedDoor",
                null,
                Vector3.zero,
                Vector3.one,
                new Color(0.25f, 0.13f, 0.3f),
                _roundedSprite,
                3);
            root.layer = _groundLayer;
            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();

            List<SpriteRenderer> visuals = new List<SpriteRenderer> { root.GetComponent<SpriteRenderer>() };
            for (int i = -2; i <= 2; i++)
            {
                GameObject membrane = CreateSpriteObject(
                    $"Membrane {i + 3}",
                    root.transform,
                    new Vector3(0f, i * 0.18f, 0f),
                    new Vector3(0.78f, 0.08f, 1f),
                    i == 0 ? PuzzleActiveColor : PuzzleColor,
                    _roundedSprite,
                    4);
                visuals.Add(membrane.GetComponent<SpriteRenderer>());
            }

            root.AddComponent<LinkedDoor>().Configure(collider, visuals.ToArray());
            PrefabUtility.SaveAsPrefabAsset(root, LinkedDoorPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void SavePlatformPrefab(string path, EnvironmentPrefabType type)
        {
            string name = type switch
            {
                EnvironmentPrefabType.Hidden => "HiddenPlatform",
                EnvironmentPrefabType.Breakable => "BreakablePlatform",
                EnvironmentPrefabType.Climbable => "ClimbableSurface",
                _ => "Platform"
            };

            Color color = type switch
            {
                EnvironmentPrefabType.Hidden => SightColor,
                EnvironmentPrefabType.Breakable => SoundColor,
                EnvironmentPrefabType.Climbable => TouchColor,
                _ => PlatformColor
            };

            GameObject root = CreateSpriteObject(name, null, Vector3.zero, Vector3.one, color, _roundedSprite, 1);
            root.layer = _groundLayer;
            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();

            if (type == EnvironmentPrefabType.Hidden)
            {
                root.AddComponent<HiddenPlatform>().Configure(root.GetComponent<SpriteRenderer>());
                CreateSpriteObject("RevealGlow", root.transform, Vector3.zero, new Vector3(1.05f, 1.18f, 1f), new Color(SightColor.r, SightColor.g, SightColor.b, 0f), _roundedSprite, 0);
            }
            else if (type == EnvironmentPrefabType.Breakable)
            {
                List<SpriteRenderer> visuals = new List<SpriteRenderer> { root.GetComponent<SpriteRenderer>() };
                for (int i = -1; i <= 1; i++)
                {
                    GameObject crack = CreateSpriteObject(
                        $"Crack_{i + 2}",
                        root.transform,
                        new Vector3(i * 0.18f, i * 0.12f, 0f),
                        new Vector3(0.055f, 0.75f, 1f),
                        new Color(0.2f, 0.16f, 0.11f, 0.72f),
                        _roundedSprite,
                        2);
                    crack.transform.localRotation = Quaternion.Euler(0f, 0f, i * 18f + 12f);
                    visuals.Add(crack.GetComponent<SpriteRenderer>());
                }

                root.AddComponent<BreakablePlatform>().Configure(collider, visuals.ToArray());
            }
            else if (type == EnvironmentPrefabType.Climbable)
            {
                root.AddComponent<ClimbableSurface>().Configure(collider);
                for (int i = -2; i <= 2; i++)
                {
                    CreateSpriteObject(
                        $"Grip_{i + 3}",
                        root.transform,
                        new Vector3((i % 2) * 0.18f, i * 0.18f, 0f),
                        Vector3.one * 0.12f,
                        new Color(0.95f, 0.82f, 1f, 0.85f),
                        _circleSprite,
                        2);
                }
            }
            else
            {
                CreateSpriteObject("Edge", root.transform, new Vector3(0f, 0.34f, 0f), new Vector3(0.92f, 0.15f, 1f), PlatformEdgeColor, _roundedSprite, 2);
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void CreateScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Physics2D.gravity = new Vector2(0f, -9.81f);

            Transform backgroundRoot = new GameObject("Organic Background").transform;
            CreateBackground(backgroundRoot);

            Transform levelRoot = new GameObject("Level Geometry").transform;
            CreateStandardPlatform(levelRoot, "Left Tissue Floor", new Vector2(-10.5f, -4f), new Vector2(15f, 1f));
            CreateStandardPlatform(levelRoot, "Middle Tissue Floor", new Vector2(0f, -4f), new Vector2(4f, 1f));
            CreateStandardPlatform(levelRoot, "Right Tissue Floor", new Vector2(10.5f, -4f), new Vector2(15f, 1f));
            CreateStandardPlatform(levelRoot, "Taste Ledge", new Vector2(-1.6f, 0.7f), new Vector2(4.4f, 0.55f));
            CreateStandardPlatform(levelRoot, "Upper Cell Shelf", new Vector2(4.2f, 2.5f), new Vector2(4.5f, 0.55f));
            CreateStandardPlatform(levelRoot, "Left Boundary", new Vector2(-18.5f, 0f), new Vector2(1f, 9f));
            CreateStandardPlatform(levelRoot, "Right Boundary", new Vector2(18.5f, 0f), new Vector2(1f, 9f));

            HiddenPlatform[] hiddenPlatforms =
            {
                CreateHiddenPlatform(levelRoot, "Hidden Bridge A", new Vector2(-2.55f, -2.45f), new Vector2(1.2f, 0.4f)),
                CreateHiddenPlatform(levelRoot, "Hidden Bridge B", new Vector2(2.55f, -2.45f), new Vector2(1.2f, 0.4f))
            };

            BreakablePlatform[] breakables =
            {
                CreateBreakable(levelRoot, "Calcified Gate", new Vector2(8.4f, -1.25f), new Vector2(0.9f, 4.6f))
            };

            CreateClimbable(levelRoot, "Touch Wall", new Vector2(-5.2f, -0.75f), new Vector2(0.75f, 5.4f));

            Transform puzzleRoot = new GameObject("Pressure Door Puzzle").transform;
            LinkedDoor pressureDoor = CreateLinkedDoor(
                puzzleRoot,
                "Pressure Door",
                new Vector2(12.2f, -1.25f),
                new Vector2(0.9f, 4.6f));
            PressureButton[] pressureButtons =
            {
                CreatePressureButton(puzzleRoot, "Pressure Button Left", new Vector2(10.35f, -3.42f), pressureDoor),
                CreatePressureButton(puzzleRoot, "Pressure Button Right", new Vector2(14.05f, -3.42f), pressureDoor)
            };

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            PlayerController playerOne = CreatePlayer(
                playerPrefab,
                "Player 1",
                new Vector3(-15.2f, -2.75f, 0f),
                PlayerOneColor,
                new PlayerInputBindings(Key.A, Key.D, Key.W, Key.S, Key.Q, Key.E),
                PlayerOneAllowDoubleJump);
            PlayerController playerTwo = CreatePlayer(
                playerPrefab,
                "Player 2",
                new Vector3(-13.6f, -2.75f, 0f),
                PlayerTwoColor,
                new PlayerInputBindings(Key.LeftArrow, Key.RightArrow, Key.UpArrow, Key.DownArrow, Key.K, Key.L),
                PlayerTwoAllowDoubleJump);

            ConfigureTeammates(playerOne, playerTwo);

            Transform pickupRoot = new GameObject("Ability Pickups").transform;
            AbilityPickup[] pickups =
            {
                CreatePickup(pickupRoot, AbilityType.Sight, new Vector2(-12.2f, -2.55f), SightColor, 0f),
                CreatePickup(pickupRoot, AbilityType.Sound, new Vector2(-10.2f, -2.55f), SoundColor, 1.2f),
                CreatePickup(pickupRoot, AbilityType.Taste, new Vector2(-8.2f, -2.55f), TasteColor, 2.4f),
                CreatePickup(pickupRoot, AbilityType.Touch, new Vector2(-6.2f, -2.55f), TouchColor, 3.6f)
            };

            Transform upgradeRoot = new GameObject("Ability Slot Upgrades").transform;
            AbilitySlotUpgrade[] slotUpgrades = LevelMaxAbilitiesPerPlayer > 1
                ? new[]
                {
                    CreateAbilitySlotUpgrade(upgradeRoot, "Ability Slot Upgrade A", new Vector2(-7f, -2.55f), 0f),
                    CreateAbilitySlotUpgrade(upgradeRoot, "Ability Slot Upgrade B", new Vector2(4.2f, 3.35f), 1.8f)
                }
                : Array.Empty<AbilitySlotUpgrade>();

            Transform gemRoot = new GameObject("Gem Collectibles").transform;
            GemCollectible[] gems =
            {
                CreateGemCollectible(gemRoot, "Gem A", new Vector2(-1.6f, 1.55f), 0f),
                CreateGemCollectible(gemRoot, "Gem B", new Vector2(5.45f, 3.35f), 1.4f),
                CreateGemCollectible(gemRoot, "Gem C", new Vector2(14.75f, -2.55f), 2.8f)
            };

            LevelExit levelExit = CreateLevelExit(new Vector2(15.5f, -2f));
            CreateCamera(playerOne.transform, playerTwo.transform);

            GameObject managerObject = new GameObject("Game Manager");
            GameManager manager = managerObject.AddComponent<GameManager>();
            manager.Configure(
                LevelMaxAbilitiesPerPlayer,
                "01 - Burning Throat Caverns",
                "Reach the membrane gate together",
                new[] { playerOne, playerTwo },
                pickups,
                slotUpgrades,
                gems,
                hiddenPlatforms,
                breakables,
                pressureButtons,
                new[] { pressureDoor },
                levelExit);
            managerObject.AddComponent<PrototypeHud>().Configure(
                manager,
                LoadSpriteAsset(SightIconPath, "Sight_1"),
                LoadSpriteAsset(SoundIconPath, "Sound_2"),
                LoadSpriteAsset(TasteIconPath, "Taste_1"),
                LoadSpriteAsset(TouchIconPath, "Touch_0"));

            CreateWorldLabel("SIGHT", new Vector2(-12.2f, -1.55f), SightColor, 0.062f);
            CreateWorldLabel("SOUND", new Vector2(-10.2f, -1.55f), SoundColor, 0.062f);
            CreateWorldLabel("TASTE", new Vector2(-8.2f, -1.55f), TasteColor, 0.062f);
            CreateWorldLabel("TOUCH", new Vector2(-6.2f, -1.55f), TouchColor, 0.062f);
            CreateWorldLabel("MEMBRANE GATE", new Vector2(15.2f, 0.05f), new Color(0.67f, 1f, 0.82f), 0.11f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        }

        private static Sprite LoadSpriteAsset(string path, string spriteName)
        {
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Sprite sprite && sprite.name == spriteName)
                {
                    return sprite;
                }
            }

            Debug.LogWarning($"MICRO-SENSE: Could not find ability icon '{spriteName}' at '{path}'.");
            return null;
        }

        private static void CreateBackground(Transform parent)
        {
            Color deepTissue = new Color(0.17f, 0.09f, 0.19f, 0.9f);
            Color coral = new Color(0.64f, 0.2f, 0.28f, 0.28f);
            Color teal = new Color(0.1f, 0.45f, 0.43f, 0.2f);

            CreateSpriteObject("Backdrop", parent, new Vector3(0f, 0f, 4f), new Vector3(40f, 18f, 1f), deepTissue, _roundedSprite, -30);
            for (int i = 0; i < 14; i++)
            {
                float x = -17f + i * 2.65f;
                float y = -1f + Mathf.Sin(i * 1.17f) * 4.6f;
                Color color = i % 3 == 0 ? teal : coral;
                GameObject blob = CreateSpriteObject(
                    $"Cellular Form {i + 1}",
                    parent,
                    new Vector3(x, y, 3f),
                    new Vector3(2.2f + i % 3, 1.1f + i % 2, 1f),
                    color,
                    _circleSprite,
                    -20);
                blob.AddComponent<AmbientDrift>().Configure(new Vector2(0.08f, 0.18f), 0.35f + i * 0.025f, i * 0.7f, 3f);
            }

            for (int i = 0; i < 24; i++)
            {
                float x = -17f + (i * 3.7f) % 34f;
                float y = -3f + (i * 2.3f) % 10f;
                GameObject mote = CreateSpriteObject(
                    $"Mote {i + 1}",
                    parent,
                    new Vector3(x, y, 2f),
                    Vector3.one * (0.08f + (i % 4) * 0.025f),
                    new Color(0.83f, 0.93f, 0.72f, 0.38f),
                    _circleSprite,
                    -10);
                mote.AddComponent<AmbientDrift>().Configure(new Vector2(0.12f, 0.32f), 0.5f + i * 0.02f, i, 0f);
            }
        }

        private static PlayerController CreatePlayer(
            GameObject prefab,
            string playerName,
            Vector3 position,
            Color color,
            PlayerInputBindings bindings,
            bool allowDoubleJump)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = playerName.Replace(" ", string.Empty);
            instance.transform.position = position;

            Transform groundCheck = instance.transform.Find("GroundCheck");
            PlayerController player = instance.GetComponent<PlayerController>();
            player.Configure(playerName, color, bindings, groundCheck, 1 << _groundLayer, allowDoubleJump);

            SpriteRenderer body = instance.GetComponent<SpriteRenderer>();
            body.color = color;
            Transform aura = instance.transform.Find("Aura");
            if (aura != null)
            {
                aura.GetComponent<SpriteRenderer>().color = new Color(color.r, color.g, color.b, 0.18f);
            }

            return player;
        }

        private static void ConfigureTeammates(PlayerController playerOne, PlayerController playerTwo)
        {
            TasteAbility oneTaste = playerOne.GetComponent<TasteAbility>();
            TasteAbility twoTaste = playerTwo.GetComponent<TasteAbility>();
            oneTaste.Configure(playerTwo, playerOne.transform.Find("HoldAnchor"), FindLine(playerOne, "TasteTrajectory"), 1.65f, 13f);
            twoTaste.Configure(playerOne, playerTwo.transform.Find("HoldAnchor"), FindLine(playerTwo, "TasteTrajectory"), 1.65f, 13f);
        }

        private static LineRenderer FindLine(PlayerController player, string objectName)
        {
            foreach (LineRenderer line in player.GetComponentsInChildren<LineRenderer>(true))
            {
                if (line.gameObject.name == objectName)
                {
                    return line;
                }
            }

            LineRenderer[] lines = player.GetComponentsInChildren<LineRenderer>(true);
            return objectName == "TasteTrajectory" && lines.Length > 1 ? lines[1] : lines[0];
        }

        private static AbilityPickup CreatePickup(
            Transform parent,
            AbilityType ability,
            Vector2 position,
            Color color,
            float phase)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PickupPrefabPath);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = $"{ability} Pickup";
            instance.transform.position = position;
            instance.transform.localScale = Vector3.one * 0.72f;
            instance.GetComponent<SpriteRenderer>().color = color;
            instance.GetComponent<AbilityPickup>().Configure(ability);
            instance.GetComponent<AmbientDrift>().Configure(new Vector2(0f, 0.12f), 1.8f, phase, 4f);
            return instance.GetComponent<AbilityPickup>();
        }

        private static AbilitySlotUpgrade CreateAbilitySlotUpgrade(
            Transform parent,
            string name,
            Vector2 position,
            float phase)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AbilitySlotUpgradePrefabPath);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            instance.transform.position = position;
            instance.transform.localScale = Vector3.one * 0.82f;
            instance.GetComponent<AmbientDrift>().Configure(new Vector2(0f, 0.16f), 1.5f, phase, 5f);
            return instance.GetComponent<AbilitySlotUpgrade>();
        }

        private static GemCollectible CreateGemCollectible(
            Transform parent,
            string name,
            Vector2 position,
            float phase)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GemCollectiblePrefabPath);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            instance.transform.position = position;
            instance.GetComponent<AmbientDrift>().Configure(new Vector2(0f, 0.16f), 1.7f, phase, 6f);
            return instance.GetComponent<GemCollectible>();
        }

        private static void CreateStandardPlatform(Transform parent, string name, Vector2 position, Vector2 size)
        {
            CreateEnvironmentInstance(PlatformPrefabPath, parent, name, position, size);
        }

        private static HiddenPlatform CreateHiddenPlatform(Transform parent, string name, Vector2 position, Vector2 size)
        {
            return CreateEnvironmentInstance(HiddenPlatformPrefabPath, parent, name, position, size).GetComponent<HiddenPlatform>();
        }

        private static BreakablePlatform CreateBreakable(Transform parent, string name, Vector2 position, Vector2 size)
        {
            return CreateEnvironmentInstance(BreakablePrefabPath, parent, name, position, size).GetComponent<BreakablePlatform>();
        }

        private static ClimbableSurface CreateClimbable(Transform parent, string name, Vector2 position, Vector2 size)
        {
            return CreateEnvironmentInstance(ClimbablePrefabPath, parent, name, position, size).GetComponent<ClimbableSurface>();
        }

        private static LinkedDoor CreateLinkedDoor(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size)
        {
            return CreateEnvironmentInstance(LinkedDoorPrefabPath, parent, name, position, size)
                .GetComponent<LinkedDoor>();
        }

        private static PressureButton CreatePressureButton(
            Transform parent,
            string name,
            Vector2 position,
            LinkedDoor linkedDoor)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PressureButtonPrefabPath);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            instance.transform.position = position;
            instance.GetComponent<PressureButton>().Configure(
                linkedDoor,
                instance.transform.Find("Plate"),
                instance.transform.Find("Indicator").GetComponent<SpriteRenderer>(),
                new Vector3(0f, -0.14f, 0f));
            return instance.GetComponent<PressureButton>();
        }

        private static GameObject CreateEnvironmentInstance(string path, Transform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            instance.transform.position = position;
            instance.transform.localScale = new Vector3(size.x, size.y, 1f);
            return instance;
        }

        private static LevelExit CreateLevelExit(Vector2 position)
        {
            GameObject root = new GameObject("Level Exit");
            root.transform.position = position;
            BoxCollider2D trigger = root.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(2.2f, 3.1f);
            LevelExit levelExit = root.AddComponent<LevelExit>();
            levelExit.Configure(2);

            CreateSpriteObject("Exit Glow", root.transform, Vector3.zero, new Vector3(2.2f, 3.1f, 1f), new Color(0.42f, 1f, 0.75f, 0.16f), _roundedSprite, 3);
            CreateSpriteObject("Exit Core", root.transform, Vector3.zero, new Vector3(1.35f, 2.35f, 1f), new Color(0.08f, 0.24f, 0.25f, 0.86f), _roundedSprite, 4);
            return levelExit;
        }

        private static void CreateCamera(Transform playerOne, Transform playerTwo)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(-6f, 0f, -10f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 7f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.045f, 0.105f);
            camera.allowHDR = true;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraObject.AddComponent<SharedCameraController>().Configure(
                new[] { playerOne, playerTwo },
                new Vector2(-40f, -15f),
                new Vector2(40f, 20f));
        }

        private static void CreateWorldLabel(string text, Vector2 position, Color color, float characterSize)
        {
            GameObject labelObject = new GameObject($"Label {text}");
            labelObject.transform.position = position;
            TextMesh textMesh = labelObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 64;
            textMesh.characterSize = characterSize;
            textMesh.color = color;
            labelObject.GetComponent<MeshRenderer>().sortingOrder = 20;
        }

        private static Transform CreateChild(Transform parent, string name, Vector3 localPosition)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            return child.transform;
        }

        private static GameObject CreateSpriteObject(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Color color,
            Sprite sprite,
            int sortingOrder)
        {
            GameObject gameObject = new GameObject(name);
            if (parent != null)
            {
                gameObject.transform.SetParent(parent, false);
                gameObject.transform.localPosition = position;
            }
            else
            {
                gameObject.transform.position = position;
            }

            gameObject.transform.localScale = scale;
            SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return gameObject;
        }

        private static LineRenderer CreateLineRenderer(GameObject owner, string objectName, float startWidth, float endWidth)
        {
            GameObject lineObject = new GameObject(objectName);
            lineObject.transform.SetParent(owner.transform, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.material = _lineMaterial;
            line.useWorldSpace = true;
            line.startWidth = startWidth;
            line.endWidth = endWidth;
            line.numCapVertices = 3;
            line.sortingOrder = 25;
            line.enabled = false;
            return line;
        }
    }
}
