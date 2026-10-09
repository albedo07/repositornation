using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using Jotunn.Managers;
using Jotunn.Configs;
using Jotunn.Entities;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace AlbedosCustomClasses
{
    // v0.23.5 universal hover highlight for uGUI (Altar): a light glow while the pointer is over it.
    // Only enter/exit are handled, so ScrollRects keep receiving drag and scroll events.
    public class AltarHoverGlow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Outline Glow;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Glow != null) Glow.enabled = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (Glow != null) Glow.enabled = false;
        }

        private void OnDisable()
        {
            if (Glow != null) Glow.enabled = false;
        }
    }

    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInDependency("com.jotunn.jotunn", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGuid = "albedo.customclasses";
        public const string ModName = "Aethelborn Ascended - Altar";
        public const string ModVersion = "0.25.124";

        internal const string ClassDataKey = "AlbedoCustomClasses.Class";
        internal const string AdvancementDataKey = "AlbedoCustomClasses.Advancement";
        internal const string PaladinVitalityKey = "AlbedoCustomClasses.Paladin.Vitality";
        internal const string PaladinOffenseKey = "AlbedoCustomClasses.Paladin.Offense";
        internal const string PaladinPassiveKey = "AlbedoCustomClasses.Paladin.Passive";
        internal const string PriestOffenseKey = "AlbedoCustomClasses.Priest.Offense";
        internal static Plugin Instance;

        public static bool IsClassPanelOpen
        {
            get
            {
                return Instance != null &&
                       Instance._classPanel != null &&
                       Instance._classPanel.activeSelf;
            }
        }

        private ConfigEntry<int> _stoneCost;
        private ConfigEntry<int> _fineWoodCost;
        private ConfigEntry<int> _surtlingCoreCost;
        private ConfigEntry<bool> _allowClassChange;
        private ConfigEntry<bool> _allowAdvancementChange;

        private GameObject _classPanel;
        private GameObject _baseClassPage;
        private GameObject _advancementPage;
        private GameObject _confirmationPanel;
        private Text _currentClassText;
        private Text _currentAdvancementText;
        private Text _baseDetailTitle;
        private Text _baseDetailRole;
        private Text _baseDetailBody;
        private Text _advancementPageTitle;
        private Text _advancementBaseSummaryTitle;
        private Text _advancementBaseSummaryBody;
        private Text _advDetailTitle;
        private Text _advDetailRole;
        private Text _advDetailBody;
        private Text _chooseBaseButtonText;
        private Text _chooseAdvButtonText;
        private Text _confirmationText;
        private Button _chooseAdvButton;
        private string _focusedBaseClass = "Warrior";
        private string _focusedAdvancement = "Sword Master";
        private string _advancementParent = "Warrior";
        private string _pendingSelectionType = string.Empty;
        private string _pendingSelectionName = string.Empty;
        private string _pendingRequiredClass = string.Empty;
        private int _classUiPage;
        private float _resetConfirmUntil;

        private void Awake()
        {
            Instance = this;

            _stoneCost = Config.Bind("Shrine", "StoneCost", 10, "Stone needed to build the shrine.");
            _fineWoodCost = Config.Bind("Shrine", "FineWoodCost", 5, "Fine Wood needed to build the shrine.");
            _surtlingCoreCost = Config.Bind("Shrine", "SurtlingCoreCost", 1, "Surtling Cores needed to build the shrine.");

            _allowClassChange = Config.Bind(
                "Classes",
                "AllowClassChange",
                true,
                "For testing: allows the shrine to change an already selected class."
            );

            _allowAdvancementChange = Config.Bind(
                "Classes",
                "AllowAdvancementChange",
                true,
                "For testing: allows the shrine to change an already selected advancement."
            );

            PrefabManager.OnVanillaPrefabsAvailable += RegisterShrine;

            if (!GUIManager.IsHeadless())
            {
                GUIManager.OnCustomGUIAvailable += BuildClassPanel;
            }

            Logger.LogInfo(ModName + " v" + ModVersion + " loaded.");
        }

        private void OnDestroy()
        {
            if (_classPanel != null) Destroy(_classPanel);
            foreach (Sprite sprite in _altarSprites.Values) if (sprite != null) Destroy(sprite);
            foreach (Texture2D texture in _altarTextures) if (texture != null) Destroy(texture);
            _altarSprites.Clear();
            _altarTextures.Clear();
            PrefabManager.OnVanillaPrefabsAvailable -= RegisterShrine;

            if (!GUIManager.IsHeadless())
            {
                GUIManager.OnCustomGUIAvailable -= BuildClassPanel;
                GUIManager.BlockInput(false);
            }
        }

        private void Update()
        {
            if (IsClassPanelOpen) FitAltarToCanvas();
            if (IsClassPanelOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                CloseClassPanel();
            }
        }

        private void RegisterShrine()
        {
            try
            {
                PieceConfig config = new PieceConfig
                {
                    Name = "Altar of Blessings",
                    Description = "An ancient altar that grants its Blessings: here the Aethelborn choose their Class.",
                    PieceTable = PieceTables.Hammer,
                    Category = "Class",
                    Usage = new string[]
                    {
                        PieceUsages.Furniture,
                        PieceUsages.Misc
                    }
                };

                if (_stoneCost.Value > 0)
                    config.AddRequirement("Stone", _stoneCost.Value, true);
                if (_fineWoodCost.Value > 0)
                    config.AddRequirement("FineWood", _fineWoodCost.Value, true);
                if (_surtlingCoreCost.Value > 0)
                    config.AddRequirement("SurtlingCore", _surtlingCoreCost.Value, true);

                // Safe root piece with no built-in chair behavior.
                CustomPiece shrine = new CustomPiece(
                    "Albedo_ClassShrine",
                    "stone_floor_2x2",
                    config
                );

                GameObject root = shrine.PiecePrefab;

                if (shrine.Piece != null)
                {
                    shrine.Piece.m_craftingStation = null;
                    shrine.Piece.m_comfort = 0;
                }

                // Build a simple custom altar/shrine from vanilla visuals.
                BuildShrineVisual(root);

                // Add interaction.
                if (root.GetComponent<ClassShrineInteraction>() == null)
                {
                    root.AddComponent<ClassShrineInteraction>();
                }

                Logger.LogInfo("Class Shrine validity before add: " + shrine.IsValid());
                Logger.LogInfo("Class Shrine target PieceTable: " + shrine.PieceTable);

                PieceManager.Instance.AddPiece(shrine);

                Logger.LogInfo("Class Shrine registered. Usage: Furniture, Misc. Custom category: Class.");
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to register Class Shrine: " + ex);
            }
            finally
            {
                PrefabManager.OnVanillaPrefabsAvailable -= RegisterShrine;
            }
        }

        private void BuildShrineVisual(GameObject root)
        {
            // IMPORTANT: shrine registration remains the proven v0.4.1 path.
            // This method only changes the visual children of that working piece.
            root.transform.localScale = Vector3.one;

            // Low stepped altar instead of the old wall-shaped shrine.
            AttachVisual(root, "stone_floor", new Vector3(0f, 0.34f, 0f), Vector3.zero, new Vector3(1.05f, 0.25f, 1.05f));
            AttachVisual(root, "stone_floor", new Vector3(0f, 0.58f, 0f), Vector3.zero, new Vector3(0.72f, 0.20f, 0.72f));

            // Four smaller corner pillars frame the magical focus.
            AttachVisual(root, "stone_pillar", new Vector3(-0.77f, 0.05f, -0.77f), Vector3.zero, new Vector3(0.42f, 0.55f, 0.42f));
            AttachVisual(root, "stone_pillar", new Vector3(0.77f, 0.05f, -0.77f), Vector3.zero, new Vector3(0.42f, 0.55f, 0.42f));
            AttachVisual(root, "stone_pillar", new Vector3(-0.77f, 0.05f, 0.77f), Vector3.zero, new Vector3(0.42f, 0.55f, 0.42f));
            AttachVisual(root, "stone_pillar", new Vector3(0.77f, 0.05f, 0.77f), Vector3.zero, new Vector3(0.42f, 0.55f, 0.42f));

            // Rear rune tablet.
            AttachVisual(root, "stone_wall_2x1", new Vector3(0f, 0.93f, 0.93f), Vector3.zero, new Vector3(0.62f, 0.72f, 0.24f));

            // Dragon identity: restrained crest, horns and ember eyes.
            CreateDragonCrest(root);

            CreateShrineMagic(root);

            if (root.GetComponent<ShrineVisualPulse>() == null)
                root.AddComponent<ShrineVisualPulse>();
        }


        private void CreateDragonCrest(GameObject root)
        {
            Color stoneDragon = new Color(0.20f, 0.20f, 0.22f, 1f);
            Color ember = new Color(1f, 0.26f, 0.06f, 1f);

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "dragon_crest_head";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.42f, 0.69f);
            head.transform.localScale = new Vector3(0.42f, 0.30f, 0.22f);
            StripPrimitiveCollider(head);
            SetPrimitiveMaterial(head, stoneDragon, false);

            GameObject snout = GameObject.CreatePrimitive(PrimitiveType.Cube);
            snout.name = "dragon_crest_snout";
            snout.transform.SetParent(root.transform, false);
            snout.transform.localPosition = new Vector3(0f, 1.33f, 0.53f);
            snout.transform.localScale = new Vector3(0.26f, 0.13f, 0.25f);
            StripPrimitiveCollider(snout);
            SetPrimitiveMaterial(snout, stoneDragon, false);

            CreateDragonHorn(root, -0.22f, -24f);
            CreateDragonHorn(root, 0.22f, 24f);

            CreateDragonEye(root, -0.095f, ember);
            CreateDragonEye(root, 0.095f, ember);

            CreateDragonWingRune(root, "dragon_rune_left", -1f, ember);
            CreateDragonWingRune(root, "dragon_rune_right", 1f, ember);
        }

        private void CreateDragonHorn(GameObject root, float x, float zAngle)
        {
            GameObject horn = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            horn.name = x < 0f ? "dragon_horn_left" : "dragon_horn_right";
            horn.transform.SetParent(root.transform, false);
            horn.transform.localPosition = new Vector3(x, 1.64f, 0.72f);
            horn.transform.localEulerAngles = new Vector3(0f, 0f, zAngle);
            horn.transform.localScale = new Vector3(0.055f, 0.24f, 0.055f);
            StripPrimitiveCollider(horn);
            SetPrimitiveMaterial(horn, new Color(0.13f, 0.13f, 0.15f, 1f), false);
        }

        private void CreateDragonEye(GameObject root, float x, Color color)
        {
            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = x < 0f ? "dragon_eye_left" : "dragon_eye_right";
            eye.transform.SetParent(root.transform, false);
            eye.transform.localPosition = new Vector3(x, 1.45f, 0.565f);
            eye.transform.localScale = new Vector3(0.045f, 0.035f, 0.025f);
            StripPrimitiveCollider(eye);
            SetPrimitiveMaterial(eye, color, true);
        }

        private void CreateDragonWingRune(GameObject root, string name, float side, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(root.transform, false);
            obj.transform.localPosition = Vector3.zero;

            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 4;
            line.startWidth = 0.028f;
            line.endWidth = 0.028f;
            line.startColor = color;
            line.endColor = color;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                line.material = new Material(shader);

            line.SetPosition(0, new Vector3(0.16f * side, 1.39f, 0.54f));
            line.SetPosition(1, new Vector3(0.42f * side, 1.49f, 0.57f));
            line.SetPosition(2, new Vector3(0.57f * side, 1.34f, 0.60f));
            line.SetPosition(3, new Vector3(0.34f * side, 1.28f, 0.57f));
        }

        private void StripPrimitiveCollider(GameObject obj)
        {
            Collider collider = obj.GetComponent<Collider>();
            if (collider != null)
                DestroyImmediate(collider);
        }

        private void SetPrimitiveMaterial(GameObject obj, Color color, bool glow)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer == null)
                return;

            Material material = CreateGlowMaterial(color);
            if (material == null)
                return;

            if (!glow && material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", Color.black);

            renderer.material = material;
        }

        private void CreateShrineMagic(GameObject root)
        {
            Color coreColor = new Color(0.45f, 0.82f, 1f, 1f);
            Color goldColor = new Color(1f, 0.68f, 0.22f, 1f);

            GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "shrine_orb";
            orb.transform.SetParent(root.transform, false);
            orb.transform.localPosition = new Vector3(0f, 1.18f, 0f);
            orb.transform.localScale = new Vector3(0.26f, 0.26f, 0.26f);

            Collider orbCollider = orb.GetComponent<Collider>();
            if (orbCollider != null)
                DestroyImmediate(orbCollider);

            Renderer orbRenderer = orb.GetComponent<Renderer>();
            if (orbRenderer != null)
            {
                Material orbMaterial = CreateGlowMaterial(coreColor);
                if (orbMaterial != null)
                    orbRenderer.material = orbMaterial;
            }

            CreateShrineRing(root, "shrine_ring_a", new Vector3(0f, 1.18f, 0f), new Vector3(68f, 0f, 0f), 0.47f, coreColor, 0.025f);
            CreateShrineRing(root, "shrine_ring_b", new Vector3(0f, 1.18f, 0f), new Vector3(0f, 0f, 68f), 0.58f, goldColor, 0.022f);
            CreateShrineRing(root, "shrine_ring_floor", new Vector3(0f, 0.73f, 0f), Vector3.zero, 0.76f, goldColor, 0.035f);

            GameObject lightObject = new GameObject("shrine_light");
            lightObject.transform.SetParent(root.transform, false);
            lightObject.transform.localPosition = new Vector3(0f, 1.25f, 0f);

            Light glow = lightObject.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = coreColor;
            glow.range = 4.2f;
            glow.intensity = 1.35f;
            glow.shadows = LightShadows.None;
        }

        private Material CreateGlowMaterial(Color color)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            if (shader == null)
                return null;

            Material material = new Material(shader);
            material.color = color;

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 2.2f);
            }

            return material;
        }

        private void CreateShrineRing(GameObject root, string name, Vector3 position, Vector3 euler, float radius, Color color, float width)
        {
            GameObject ringObject = new GameObject(name);
            ringObject.transform.SetParent(root.transform, false);
            ringObject.transform.localPosition = position;
            ringObject.transform.localEulerAngles = euler;

            LineRenderer line = ringObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 49;
            line.startWidth = width;
            line.endWidth = width;
            line.startColor = color;
            line.endColor = color;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                line.material = new Material(shader);

            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = ((float)i / (float)(line.positionCount - 1)) * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
        }

        private void AttachVisual(GameObject root, string prefabName, Vector3 localPos, Vector3 localEuler, Vector3 localScale)
        {
            GameObject prefab = FindPrefab(prefabName);
            if (prefab == null)
            {
                Logger.LogWarning("Could not find visual prefab: " + prefabName);
                return;
            }

            GameObject child = Instantiate(prefab, root.transform);
            child.name = "visual_" + prefabName;
            child.transform.localPosition = localPos;
            child.transform.localEulerAngles = localEuler;
            child.transform.localScale = localScale;

            StripGameplayComponents(child);
        }

        private GameObject FindPrefab(string prefabName)
        {
            try
            {
                if (ZNetScene.instance != null)
                {
                    GameObject fromScene = ZNetScene.instance.GetPrefab(prefabName);
                    if (fromScene != null)
                        return fromScene;
                }
            }
            catch { }

            try
            {
                MethodInfo m = typeof(PrefabManager).GetMethod("GetPrefab", BindingFlags.Instance | BindingFlags.Public);
                if (m != null)
                {
                    object result = m.Invoke(PrefabManager.Instance, new object[] { prefabName });
                    return result as GameObject;
                }
            }
            catch { }

            return null;
        }

        private void StripGameplayComponents(GameObject obj)
        {
            // Use runtime type lookup here so Valheim 1.0's split assemblies
            // (for example SoftReferenceableAssets) are not compile-time dependencies.
            string[] componentTypes = new string[]
            {
                "Piece",
                "WearNTear",
                "ZNetView",
                "Chair",
                "CraftingStation",
                "StationExtension",
                "Container",
                "PrivateArea",
                "TeleportWorld"
            };

            for (int i = 0; i < componentTypes.Length; i++)
            {
                RemoveComponentsByTypeName(obj, componentTypes[i]);
            }
        }

        private void RemoveComponentsByTypeName(GameObject root, string typeName)
        {
            Type componentType = FindLoadedType(typeName);
            if (componentType == null || !typeof(Component).IsAssignableFrom(componentType))
                return;

            Component[] components = root.GetComponentsInChildren(componentType, true);
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] != null)
                    DestroyImmediate(components[i]);
            }
        }

        private Type FindLoadedType(string typeName)
        {
            Type cachedLookup;
            if (_typeLookupCache.TryGetValue(typeName, out cachedLookup)) return cachedLookup;
            cachedLookup = FindLoadedTypeUncached(typeName);
            _typeLookupCache[typeName] = cachedLookup;
            return cachedLookup;
        }

        private readonly Dictionary<string, Type> _typeLookupCache = new Dictionary<string, Type>();

        private Type FindLoadedTypeUncached(string typeName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            for (int i = 0; i < assemblies.Length; i++)
            {
                try
                {
                    Type direct = assemblies[i].GetType(typeName, false);
                    if (direct != null)
                        return direct;

                    Type[] types = assemblies[i].GetTypes();
                    for (int j = 0; j < types.Length; j++)
                    {
                        if (types[j] != null && types[j].Name == typeName)
                            return types[j];
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        // Altar presentation only. Selection/persistence/combat methods below are unchanged.
        private static readonly Color AltarGold = new Color(1f, 0.88f, 0.62f, 1f);
        private static readonly Color AltarInk = new Color(0.16f, 0.10f, 0.07f, 1f);
        private readonly Dictionary<string, Sprite> _altarSprites = new Dictionary<string, Sprite>();
        private readonly List<Texture2D> _altarTextures = new List<Texture2D>();
        private readonly Dictionary<string, Image> _altarBaseCards = new Dictionary<string, Image>();
        private readonly Dictionary<string, Image> _altarAdvCards = new Dictionary<string, Image>();
        private Text _altarSubtitle;
        private ScrollRect _altarBaseScroll;
        private ScrollRect _altarAdvScroll;

        private RectTransform AltarRect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private Sprite AltarSprite(string name)
        {
            Sprite cached;
            if (_altarSprites.TryGetValue(name, out cached)) return cached;
            Texture2D texture = null;
            try
            {
                // Use the same late-bound byte[] decoder as the existing installer-compatible UI.
                Type file = typeof(object).Assembly.GetType("System.IO.File");
                MethodInfo read = file.GetMethod("ReadAllBytes", new Type[] { typeof(string) });
                byte[] bytes = (byte[])read.Invoke(null, new object[] { Paths.PluginPath + "/ImmortalHeroesAssets/" + name });
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                Type decoder = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule");
                MethodInfo load = decoder.GetMethod("LoadImage", BindingFlags.Static | BindingFlags.Public, null,
                    new Type[] { typeof(Texture2D), typeof(byte[]), typeof(bool) }, null);
                if (!(bool)load.Invoke(null, new object[] { texture, bytes, false })) throw new Exception("PNG decode failed");
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;
                // uGUI handles sRGB textures; do not apply the skill tree's IMGUI-only gamma fix.
                cached = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                _altarTextures.Add(texture);
                _altarSprites[name] = cached;
                return cached;
            }
            catch (Exception ex)
            {
                if (texture != null) Destroy(texture);
                Logger.LogWarning("Altar art unavailable: " + name + ". Using readable fallback. " + ex.Message);
                _altarSprites[name] = null;
                return null;
            }
        }

        // x, y, width, height of every card frame in Altar_ClassCards.png (alpha > 50%), same order as names.
        private static readonly int[] AltarCardBounds = {
            8, 11, 580, 283,   594, 12, 582, 282,   1183, 12, 585, 282,
            8, 301, 580, 284,  595, 301, 581, 284,  1183, 301, 586, 288,
            8, 592, 580, 282,  595, 592, 581, 282,  1183, 592, 583, 282 };

        private Sprite AltarCardSprite(string name)
        {
            string key = "card:" + name;
            Sprite result;
            if (_altarSprites.TryGetValue(key, out result)) return result;
            // v0.24.0: Ranger / Acrobat / Bowmaster cards live in their own sheet (3 cards of 580x283).
            string[] ranger = { "Ranger", "Acrobat", "Bowmaster" };
            int rangerIndex = Array.IndexOf(ranger, name);
            if (rangerIndex >= 0)
            {
                Sprite rsheet = AltarSprite("Altar_RangerCards.png");
                if (rsheet == null) return null;
                float rsx = rsheet.texture.width / 1760f, rsy = rsheet.texture.height / 283f;
                result = Sprite.Create(rsheet.texture, new Rect(rangerIndex * 590f * rsx, 0f, 580f * rsx, 283f * rsy), new Vector2(0.5f, 0.5f));
                _altarSprites[key] = result;
                return result;
            }
            Sprite sheet = AltarSprite("Altar_ClassCards.png");
            if (sheet == null) return null;
            string[] names = { "Warrior", "Cleric", "Sorcerer", "Sword Master", "Mercenary", "Paladin", "Priest", "Wizard", "Spellcaster" };
            int index = Array.IndexOf(names, name);
            if (index < 0) return null;
            // v0.22.3: each painted frame sits at a slightly different offset/size inside its atlas
            // cell (up to 8 px), so cards drawn from whole cells looked shifted and resized against
            // each other. Use each card's measured frame bounds (atlas px, top-left origin, 1774x887).
            int[] b = AltarCardBounds;
            float sx = sheet.texture.width / 1774f, sy = sheet.texture.height / 887f;
            int k = index * 4;
            result = Sprite.Create(sheet.texture, new Rect(b[k] * sx, (887 - b[k + 1] - b[k + 3]) * sy, b[k + 2] * sx, b[k + 3] * sy), new Vector2(0.5f, 0.5f));
            _altarSprites[key] = result;
            return result;
        }

        private Sprite AltarLabelVeil()
        {
            Sprite result;
            if (_altarSprites.TryGetValue("label-veil", out result)) return result;
            Texture2D texture = new Texture2D(64, 32, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 64; x++)
                {
                    float edge = Mathf.Min(Mathf.Min(x / 8f, (63 - x) / 8f), Mathf.Min(y / 5f, (31 - y) / 5f));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(edge))));
                }
            texture.Apply(false, true);
            result = Sprite.Create(texture, new Rect(0, 0, 64, 32), new Vector2(0.5f, 0.5f));
            _altarTextures.Add(texture);
            _altarSprites["label-veil"] = result;
            return result;
        }

        private Image AltarImage(string name, Transform parent, Vector2 position, Vector2 size, Sprite sprite, Color color, bool blocksInput)
        {
            Image image = AltarRect(name, parent, position, size).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = blocksInput;
            return image;
        }

        private Text CreateWrappedText(Transform parent, string text, Vector2 position, float width, float height,
            int fontSize, Color color, bool bold, TextAnchor alignment)
        {
            Text label = AltarRect("AltarText", parent, position, new Vector2(width, height)).gameObject.AddComponent<Text>();
            label.font = bold ? GUIManager.Instance.AveriaSerifBold : GUIManager.Instance.AveriaSerif;
            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.supportRichText = true;
            label.raycastTarget = false;
            return label;
        }

        private Button AltarButton(Transform parent, string text, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            Image image = AltarImage("AltarButton", parent, position, size, AltarSprite("Confirm_Plaque.png"), Color.white, true);
            if (image.sprite == null) image.color = new Color(0.07f, 0.12f, 0.22f, 1f);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.93f, 0.77f, 1f);
            colors.pressedColor = new Color(0.70f, 0.76f, 0.85f, 1f);
            colors.disabledColor = new Color(0.48f, 0.48f, 0.48f, 0.85f);
            button.colors = colors;
            button.onClick.AddListener(action);
            AddAltarHoverGlow(image.gameObject);
            CreateWrappedText(image.transform, text, Vector2.zero, size.x - 38f, size.y - 6f, 17, AltarGold, true, TextAnchor.MiddleCenter);
            return button;
        }

        private static void AddAltarHoverGlow(GameObject go)
        {
            if (go == null) return;
            Outline glow = go.AddComponent<Outline>();
            glow.effectColor = new Color(1f, 0.95f, 0.72f, 0.90f);
            glow.effectDistance = new Vector2(2.5f, -2.5f);
            glow.enabled = false;
            AltarHoverGlow hover = go.AddComponent<AltarHoverGlow>();
            hover.Glow = glow;
        }

        // v0.23.5: a click on empty Altar space clears the highlighted card / skill.
        private void ClearAltarSelection()
        {
            RefreshAltarCards(_altarBaseCards, "");
            RefreshAltarCards(_altarAdvCards, "");
            List<Button>[] lists = { _altarBaseSkillButtons, _altarAdvSkillButtons };
            for (int l = 0; l < lists.Length; l++)
            {
                if (lists[l] == null) continue;
                foreach (Button item in lists[l]) { if (item == null) continue; ColorBlock c = item.colors; c.normalColor = new Color(1f, 1f, 1f, 0f); item.colors = c; }
            }
        }

        private void FitAltarToCanvas()
        {
            if (_classPanel == null) return;
            RectTransform parent = _classPanel.transform.parent as RectTransform;
            if (parent == null || parent.rect.width <= 0f || parent.rect.height <= 0f) return;
            float scale = Mathf.Min((parent.rect.width - 32f) / 1040f, (parent.rect.height - 32f) / 900f);
            scale = Mathf.Max(0.1f, Mathf.Min(1.15f, scale));
            _classPanel.transform.localScale = new Vector3(scale, scale, 1f);
        }

        private void BuildClassPanel()
        {
            if (GUIManager.CustomGUIFront == null) return;
            if (_classPanel != null) { _classPanel.SetActive(false); Destroy(_classPanel); }
            _altarBaseCards.Clear();
            _altarAdvCards.Clear();
            _classPanel = AltarRect("ImmortalHeroesAltar", GUIManager.CustomGUIFront.transform, Vector2.zero, new Vector2(1040f, 900f)).gameObject;
            Sprite art = AltarSprite("Altar_Background.png");
            Image backdrop = AltarImage("AltarBackdrop", _classPanel.transform, Vector2.zero, new Vector2(1040f, 900f), art,
                art == null ? new Color(0.86f, 0.79f, 0.65f, 1f) : Color.white, true);
            Button blank = backdrop.gameObject.AddComponent<Button>();
            blank.transition = Selectable.Transition.None;
            blank.onClick.AddListener(ClearAltarSelection);
            CreateWrappedText(_classPanel.transform, "ALTAR OF BLESSINGS", new Vector2(0f, 342f), 690f, 44f, 34, AltarGold, true, TextAnchor.MiddleCenter);
            _altarSubtitle = CreateWrappedText(_classPanel.transform, "AETHELBORN ASCENDED  •  CLASS SELECTION", new Vector2(0f, 318f), 690f, 24f, 15, AltarGold, false, TextAnchor.MiddleCenter);
            AltarButton(_classPanel.transform, "Reset", new Vector2(-440f, 402f), new Vector2(116f, 40f), ResetClassSelection);
            AltarButton(_classPanel.transform, "Close", new Vector2(440f, 402f), new Vector2(116f, 40f), CloseClassPanel);
            _currentClassText = CreateWrappedText(_classPanel.transform, "Base class: None", new Vector2(-135f, 285f), 260f, 27f, 18, AltarInk, true, TextAnchor.MiddleCenter);
            _currentAdvancementText = CreateWrappedText(_classPanel.transform, "Advancement: None", new Vector2(150f, 285f), 295f, 27f, 18, AltarInk, true, TextAnchor.MiddleCenter);
            BuildBaseClassPage();
            BuildAdvancementPage();
            BuildConfirmationPanel();
            CreateWrappedText(_classPanel.transform, "AETHELBORN ASCENDED", new Vector2(0f, -421f), 600f, 24f, 13, AltarGold, false, TextAnchor.MiddleCenter);
            FitAltarToCanvas();
            ShowBaseClassPage();
            _classPanel.SetActive(false);
        }

        private GameObject CreateUiGroup(string name)
        {
            return AltarRect(name, _classPanel.transform, Vector2.zero, new Vector2(1040f, 900f)).gameObject;
        }

        private sealed class AltarSkillEntry
        {
            public string Name, Id, Description;
            public AltarSkillEntry(string name, string id, string description) { Name = name; Id = id; Description = description; }
        }
        private Transform _altarBaseSkillFooter, _altarAdvSkillFooter;
        private Text _altarBaseMechanics, _altarAdvMechanics;
        private Text _altarBaseBlessing, _altarAdvBlessing, _altarBaseBlessingLabel, _altarAdvBlessingLabel;
        private ScrollRect _altarBaseBlessingScroll, _altarAdvBlessingScroll;
        private ScrollRect _altarBaseMechanicsScroll, _altarAdvMechanicsScroll;
        private readonly List<Button> _altarBaseSkillButtons = new List<Button>();
        private readonly List<Button> _altarAdvSkillButtons = new List<Button>();

        private void AltarRule(Transform parent, float x, float y, float width, float height)
        {
            AltarImage("GoldRule", parent, new Vector2(x,y), new Vector2(width,height), null, new Color(0.62f,0.40f,0.12f,0.8f), false);
            RectTransform diamond = AltarImage("GoldDiamond", parent, new Vector2(x,y), new Vector2(5f,5f), null, new Color(0.74f,0.48f,0.15f,1f), false).rectTransform;
            diamond.localEulerAngles = new Vector3(0f,0f,45f);
        }

        private ScrollRect AltarTextScroll(Transform parent, string name, Vector2 position, Vector2 size, out Text text)
        {
            RectTransform viewport = AltarRect(name, parent, position, size);
            Image target = viewport.gameObject.AddComponent<Image>(); target.color = Color.clear;
            viewport.gameObject.AddComponent<RectMask2D>();
            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 25f;
            text = CreateWrappedText(viewport,"",Vector2.zero,size.x - 16f,0f,16,AltarInk,false,TextAnchor.UpperLeft);
            RectTransform content = text.rectTransform;
            content.anchorMin = new Vector2(0f,1f); content.anchorMax = new Vector2(1f,1f);
            content.pivot = new Vector2(0.5f,1f); content.sizeDelta = new Vector2(-16f,0f);
            content.anchoredPosition = Vector2.zero;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            ContentSizeFitter fitter = text.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            Image track = AltarImage("DetailScrollTrack", parent, new Vector2(position.x + size.x / 2f - 2f, position.y), new Vector2(4f,size.y), null, new Color(0.6f,0.4f,0.15f,0.15f), true);
            Scrollbar bar = track.gameObject.AddComponent<Scrollbar>();
            Image handle = AltarImage("DetailScrollHandle", track.transform, Vector2.zero, new Vector2(4f,20f), null, new Color(0.64f,0.43f,0.15f,0.8f), true);
            handle.rectTransform.sizeDelta = Vector2.zero;
            bar.handleRect = handle.rectTransform; bar.targetGraphic = handle;
            bar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            return scroll;
        }

        private static string AltarOverview(string description)
        {
            // Keep the identity and playstyle; mechanics belong exclusively to a clicked skill.
            int mechanics = description.IndexOf("<b>CORE MECHANICS</b>", StringComparison.Ordinal);
            if (mechanics < 0) mechanics = description.IndexOf("<b>MECHANICS</b>", StringComparison.Ordinal);
            if (mechanics < 0) return description;
            int tail = description.IndexOf("<b>PLAYSTYLE</b>", mechanics, StringComparison.Ordinal);
            if (tail < 0) tail = description.IndexOf("<b>BEST FOR</b>", mechanics, StringComparison.Ordinal);
            return description.Substring(0, mechanics).TrimEnd() + (tail < 0 ? "" : "\n\n" + description.Substring(tail));
        }

        // v0.25.39 (user): the overview keeps IDENTITY + BEST FOR; every section in between (Blessing, Mastery,
        // Grace) goes to the middle panel. One section -> its own title becomes the panel title.
        private static void AltarSplit(string description, bool advancement, out string overview, out string header, out string body)
        {
            overview = description; header = advancement ? "MASTERY & GRACE" : "BLESSING"; body = string.Empty;
            if (string.IsNullOrEmpty(description)) return;
            string[] parts = description.Split(new string[] { "<b>" }, StringSplitOptions.RemoveEmptyEntries);
            System.Text.StringBuilder ov = new System.Text.StringBuilder();
            List<string> mids = new List<string>();
            for (int i = 0; i < parts.Length; i++)
            {
                string part = "<b>" + parts[i].TrimEnd();
                int close = part.IndexOf("</b>", StringComparison.Ordinal);
                string title = close > 3 ? part.Substring(3, close - 3).Trim() : string.Empty;
                if (title == "IDENTITY" || title == "BEST FOR") { if (ov.Length > 0) ov.Append("\n\n"); ov.Append(part); }
                else if (title == "CORE MECHANICS" || title == "MECHANICS" || title == "PLAYSTYLE") continue;
                else mids.Add(part);
            }
            if (ov.Length > 0) overview = ov.ToString();
            if (mids.Count == 1)
            {
                string only = mids[0];
                int close = only.IndexOf("</b>", StringComparison.Ordinal);
                header = only.Substring(3, close - 3).Trim();
                body = only.Substring(close + 4).Trim();
            }
            else body = string.Join("\n\n", mids.ToArray());
        }

        private void BuildAltarSkillDetails(Transform parent, bool advancement)
        {
            Text overview;
            ScrollRect overviewScroll = AltarTextScroll(parent,"ClassOverview",new Vector2(215f,110f),new Vector2(520f,120f),out overview);
            AltarRule(parent,215f,38f,520f,1f);
            // v0.25.39 (user): middle panel = Blessing (Mastery & Grace on the Advancement page); the clicked
            // skill's description lives bottom right, next to the skill list. Every text is its own scroll view.
            Text blessingLabel = CreateWrappedText(parent,"BLESSING",new Vector2(215f,20f),500f,26f,19,new Color(0.40f,0.20f,0.08f,1f),true,TextAnchor.MiddleLeft);
            blessingLabel.resizeTextForBestFit = true; blessingLabel.resizeTextMinSize = 12; blessingLabel.resizeTextMaxSize = 19;
            Text blessing;
            ScrollRect blessingScroll = AltarTextScroll(parent,"BlessingText",new Vector2(215f,-60f),new Vector2(520f,126f),out blessing);
            Transform footer = AltarRect("SkillFooter",parent,Vector2.zero,new Vector2(1040f,900f));
            AltarRule(footer,215f,-133f,520f,1f);
            AltarRule(footer,214f,-222f,1f,172f);
            AltarRule(footer,215f,-313f,520f,1f);
            CreateWrappedText(footer,"SKILLS",new Vector2(85f,-150f),240f,28f,20,new Color(0.40f,0.20f,0.08f,1f),true,TextAnchor.MiddleLeft);
            CreateWrappedText(footer,"SKILL",new Vector2(347f,-150f),246f,28f,20,new Color(0.40f,0.20f,0.08f,1f),true,TextAnchor.MiddleLeft);
            Text mechanics;
            ScrollRect mechanicsScroll = AltarTextScroll(footer,"SelectedSkillMechanics",new Vector2(347f,-237f),new Vector2(250f,138f),out mechanics);
            mechanics.fontSize = 14;
            if (advancement) { _advDetailBody=overview; _altarAdvScroll=overviewScroll; _altarAdvMechanics=mechanics; _altarAdvMechanicsScroll=mechanicsScroll; _altarAdvSkillFooter=footer; _altarAdvBlessing=blessing; _altarAdvBlessingLabel=blessingLabel; _altarAdvBlessingScroll=blessingScroll; }
            else { _baseDetailBody=overview; _altarBaseScroll=overviewScroll; _altarBaseMechanics=mechanics; _altarBaseMechanicsScroll=mechanicsScroll; _altarBaseSkillFooter=footer; _altarBaseBlessing=blessing; _altarBaseBlessingLabel=blessingLabel; _altarBaseBlessingScroll=blessingScroll; }
        }

        private Sprite AltarSkillIcon(string id)
        {
            string key="skill-icon:"+id;
            Sprite result;
            if (_altarSprites.TryGetValue(key,out result)) return result;
            // v0.23.9: every skill (Priest included) has its own Icon_<id>.png (tools/build_class_art.py).
            // Masteries have no icon.
            try
            {
                Type file=typeof(object).Assembly.GetType("System.IO.File");
                MethodInfo exists=file.GetMethod("Exists",new Type[]{typeof(string)});
                string name=id=="righteous_strike" ? "Icon_righteous_strike_Normal.png" : "Icon_"+id+".png";
                if(!(bool)exists.Invoke(null,new object[]{Paths.PluginPath+"/ImmortalHeroesAssets/"+name})) return null;
            }
            catch { return null; }
            // Reuse a cached sprite, without registering a second ownership entry.
            // v0.22.2: Icon_righteous_strike.png is the Ascended (Magenta) frame; the Altar shows
            // the Class skill, so it uses the normal Cyan icon.
            return AltarSprite(id=="righteous_strike" ? "Icon_righteous_strike_Normal.png" : "Icon_"+id+".png");
        }

        private void RefreshAltarSkillFooter(string className, bool advancement)
        {
            Transform footer=advancement ? _altarAdvSkillFooter : _altarBaseSkillFooter;
            Text mechanics=advancement ? _altarAdvMechanics : _altarBaseMechanics;
            ScrollRect scroll=advancement ? _altarAdvMechanicsScroll : _altarBaseMechanicsScroll;
            if(footer==null || mechanics==null) return;
            mechanics.text=string.Empty;
            scroll.verticalNormalizedPosition=1f;
            List<Button> buttons=advancement ? _altarAdvSkillButtons : _altarBaseSkillButtons;
            buttons.Clear();
            Transform old=footer.Find("SkillChoices");
            if(old!=null) { old.gameObject.SetActive(false); Destroy(old.gameObject); }
            Transform group=AltarRect("SkillChoices",footer,Vector2.zero,new Vector2(1040f,900f));
            AltarSkillEntry[] skills=AltarSkillEntries(className,false);
            mechanics.text="<i>Click a skill to read what it does.</i>";
            bool twoColumns=skills.Length>3;
            for(int i=0;i<skills.Length;i++)
            {
                int row=twoColumns ? i/2 : i, col=twoColumns ? i%2 : 0;
                float x=twoColumns ? 22f+col*127f : 84f;
                CreateAltarSkillChoice(group,skills[i],new Vector2(x,-186f-row*43f),twoColumns ? 124f : 250f,mechanics,scroll,buttons);
            }
        }

        private void CreateAltarSkillChoice(Transform parent, AltarSkillEntry entry, Vector2 position, float width, Text mechanics, ScrollRect scroll, List<Button> buttons, bool largeIcon = false)
        {
            Image row=AltarImage("Inspect_"+entry.Id,parent,position,new Vector2(width,largeIcon ? 66f : 40f),AltarLabelVeil(),new Color(1f,0.83f,0.48f,1f),true);
            Button button=row.gameObject.AddComponent<Button>(); button.targetGraphic=row;
            AddAltarHoverGlow(row.gameObject);
            ColorBlock colors=button.colors; colors.normalColor=new Color(1f,1f,1f,0f); colors.highlightedColor=new Color(1f,1f,1f,0.45f); colors.pressedColor=new Color(1f,1f,1f,0.7f); button.colors=colors;
            Sprite icon=AltarSkillIcon(entry.Id);
            Image tile=AltarImage("SkillIcon",row.transform,new Vector2(-width/2f+(largeIcon ? 27f : 17f),0f),new Vector2(largeIcon ? 48f : 30f,largeIcon ? 48f : 30f),icon,icon==null ? new Color(0.08f,0.15f,0.26f,1f) : Color.white,false);
            Outline outline=tile.gameObject.AddComponent<Outline>(); outline.effectColor=AltarGold; outline.effectDistance=new Vector2(1f,-1f);
            if(icon==null) CreateWrappedText(tile.transform,entry.Name.Substring(0,1),Vector2.zero,28f,28f,19,AltarGold,true,TextAnchor.MiddleCenter);
            Text nameText=CreateWrappedText(row.transform,entry.Name,new Vector2(largeIcon ? 28f : 18f,0f),width-(largeIcon ? 60f : 40f),largeIcon ? 66f : 40f,width>200f ? 15 : 13,AltarInk,false,TextAnchor.MiddleLeft);
            // v0.25.39: long names shrink to fit their row instead of being cut.
            nameText.resizeTextForBestFit=true; nameText.resizeTextMinSize=9; nameText.resizeTextMaxSize=width>200f ? 15 : 13;
            buttons.Add(button);
            button.onClick.AddListener(delegate {
                // Inspection only: no casts, tier changes, class selection or save writes.
                mechanics.text="<b>"+entry.Name+"</b>\n\n"+AltarSkillDescription(entry);
                Canvas.ForceUpdateCanvases();
                scroll.verticalNormalizedPosition=1f;
                foreach(Button item in buttons) { ColorBlock c=item.colors; c.normalColor=new Color(1f,1f,1f,item==button ? 0.60f : 0f); item.colors=c; }
            });
        }

        private string AltarSkillDescription(AltarSkillEntry entry)
        {
            // Read the existing Cleric-tree tooltip builder without linking Core to Advanced.
            // This preserves live configured values and existing meter/degree/percent units.
            try
            {
                Type type=Type.GetType("AlbedosCustomClassesAdvanced.AdvancedPlugin, AlbedosCustomClasses.Advanced");
                if(type!=null && Player.m_localPlayer!=null)
                {
                    object instance=type.GetField("Instance",BindingFlags.Public|BindingFlags.Static).GetValue(null);
                    if(instance!=null)
                    {
                        string[] fields={"ClericPaladinReferenceNodes","ClericPriestReferenceNodes"};
                        foreach(string field in fields)
                        {
                            Array nodes=type.GetField(field,BindingFlags.NonPublic|BindingFlags.Static).GetValue(null) as Array;
                            foreach(object node in nodes)
                            {
                                if((string)node.GetType().GetField("Id").GetValue(node)!=entry.Id) continue;
                                object[] args={Player.m_localPlayer,node,null};
                                string body=(string)type.GetMethod("IhBuildTooltip",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(instance,args);
                                // v0.20.4: the Altar shows the description only (the tree tooltip's first
                                // paragraph); Tier, damage and cost lines stay in the Skill Tree.
                                body=System.Text.RegularExpressions.Regex.Replace(body,@"</?color(?:=[^>]+)?>",string.Empty);
                                int cut=body.IndexOf("\n\n",StringComparison.Ordinal);
                                string lore=(cut>=0 ? body.Substring(0,cut) : body).Trim();
                                return lore.Length>0 && lore.IndexOf(" - ",StringComparison.Ordinal)<0 ? lore : entry.Description;
                            }
                        }
                    }
                }
            }
            catch(Exception ex) { Logger.LogDebug("Altar skill preview fallback: "+entry.Id+" "+ex.Message); }
            return entry.Description;
        }

        private static AltarSkillEntry[] AltarSkillEntries(string className, bool passive)
        {
            switch(className)
            {
                case "Warrior": return passive ? new AltarSkillEntry[] { new AltarSkillEntry("Warrior's Blessing","warrior_blessing","Hyper Armor against any hit below 30% of your Total HP. Parry strength x2. +20 Run and +20 Jump skill.") } : new AltarSkillEntry[] { new AltarSkillEntry("Heavy Slash","heavy_slash","0.7s heavy horizontal Slash. Inflicts Broken Bones."), new AltarSkillEntry("Impact Wave","impact_wave","Ground Projectile: Blunt + Pierce line wave that follows terrain."), new AltarSkillEntry("Impact Punch","impact_punch","0.5s punch, 2m x 2m. Blunt damage; Stuns Small enemies.") };
                case "Cleric": return passive ? new AltarSkillEntry[] { new AltarSkillEntry("Cleric's Blessing","cleric_blessing","Every Shield gets 1.5x Block Force and Block Armor. Wield a Staff and a Shield together. No movement penalty from Shields, Staves or one-handed Club weapons. +35 Max HP and +20% HP Regen.") } : new AltarSkillEntry[] { new AltarSkillEntry("Lightning Zap","lightning_zap","Heaven's wrath leaps from your palm in a cone, branding every foe it touches with a Zap that soon bursts."), new AltarSkillEntry("Righteous Strike","righteous_strike","Call down a pillar of holy lightning at your aim, smiting and exposing the wicked."), new AltarSkillEntry("Holy Wave","holy_wave","Release a warm tide of light that heals you and every ally it touches, and keeps mending them.") };
                case "Sorcerer": return passive ? new AltarSkillEntry[] { new AltarSkillEntry("Warlock (Blessing)","warlock","Creature melee damage -70% (mining and woodcutting are not affected). +65 Max Eitr, +35% Eitr Regen, and Eitr starts regenerating twice as fast. A Sorcerer cannot Block, Parry or equip Shields.") } : new AltarSkillEntry[] { new AltarSkillEntry("Flame Burst","flame_burst","10m Fire cone with Fire Burn."), new AltarSkillEntry("Glacial Descent","glacial_descent","Ground PAC: 5m Blunt + Frost impact."), new AltarSkillEntry("Stonefang Eruption","stonefang_eruption","Ground PAC: 5m Blunt + Pierce; Small Stun, Small/Big Cripple.") };
                case "Ranger": return passive ? new AltarSkillEntry[] { new AltarSkillEntry("Wildborn (Blessing)","wildborn","+20 Bows and +20 Dodge, a quick-shot Left Click chain and a Right Click charged shot. Infinite arrows: half your Bow damage plus half the arrow's with a full stack. Cannot Block or use Shields.") } : new AltarSkillEntry[] { new AltarSkillEntry("Piercing Arrow","piercing_arrow","A straight arrow that tears through every foe in its line and cripples the first."), new AltarSkillEntry("Tumble Shot","tumble_shot","Backflip away and loose a fan of arrows at your aim."), new AltarSkillEntry("Snare Trap","snare_trap","Set a hidden snare that holds small prey in place and slows the large.") };
                case "Acrobat": return passive ? new AltarSkillEntry[] { new AltarSkillEntry("Windstep (Mastery)","windstep","A second jump in mid-air, Dodge costs 50% less Stamina, all Stamina use -35%, fall damage -75% and falls never kill, every Ranger skill can be cast in the air, and each skill hit trims your shortest cooldown. Never wields a Crossbow."), new AltarSkillEntry("Tailwind (Grace)","tailwind","Wind lifts you and your allies: much faster feet, no movement penalties, higher jumps and no fall damage for 2 minutes.") } : new AltarSkillEntry[] { new AltarSkillEntry("Gale Volley","gale_volley","Spring back out of reach and loose a wide fan of arrows that blows small foes away."), new AltarSkillEntry("Cyclone Arrow","cyclone_arrow","A slow, spinning arrow of wind that drags small foes along its path."), new AltarSkillEntry("Swallow Dive","swallow_dive","Dash through the enemy line as a gust of wind, cutting everything you pass."), new AltarSkillEntry("Skyfall Barrage","skyfall_barrage","Leap high into the sky and rain arrows on the ground below."), new AltarSkillEntry("Somersault Dance","ricochet_arrow","Front flip high into the air and slam the ground with your foot, then backflip out of reach."), new AltarSkillEntry("Furious Winds (Ultimate)","furious_winds","A whirlwind of magical leaves shields you and shreds every foe that comes near.") };
                case "Bowmaster": return passive ? new AltarSkillEntry[] { new AltarSkillEntry("Deadeye (Mastery)","deadeye","Standing still builds Focus: more damage and range with every second. Fully charged shots hit harder. Crossbows reload 75% faster, keep their load and lose their movement penalty."), new AltarSkillEntry("Hawk's Vigil (Grace)","hawks_vigil","Reveal every enemy around you and sharpen your allies' aim.") } : new AltarSkillEntry[] { new AltarSkillEntry("Ballista Shot","ballista_shot","Hold to draw a siege-strength arrow that blasts through the enemy line."), new AltarSkillEntry("Arrow Rain","arrow_rain","Darken the sky over your aim with a crippling volley."), new AltarSkillEntry("Pinning Shot","pinning_shot","Nail your target to the ground and leave it open to your next shots."), new AltarSkillEntry("Explosive Arrow","explosive_arrow","An arrow that bursts into flame on impact, followed by a carpet of cluster bombs."), new AltarSkillEntry("Splitting Arrow","splitting_arrow","Two spread shots into a wide cone, then one great arrow that tears through the line."), new AltarSkillEntry("Starfall Volley (Ultimate)","starfall_volley","Call giant arrows down from the heavens across the battlefield.") };
                case "Sword Master": return passive ? new AltarSkillEntry[] { new AltarSkillEntry("The Way of the Sword (Mastery)","the_way_of_the_sword","With exactly one Sword: +20 Sword, +50% Attack Speed and no Sword movement penalty. Blocking or Dodging stops the rest of a Sword Master skill."), new AltarSkillEntry("Knight's Guidance (Grace)","knights_guidance","Lead every ally around you: faster movement, quicker stamina and less effort for every action, for 3 minutes.") } : new AltarSkillEntry[] { new AltarSkillEntry("Moonlight Splitter","moonlight_splitter","Three crescent waves of moonlight cleave through everything in their path."), new AltarSkillEntry("Crescent Cleave","crescent_cleave","Five giant crescent cleaves tear across the ground in a wide fan."), new AltarSkillEntry("Blade Storm","blade_storm","Rend space itself: a sphere of blades bursts at your aim, again and again."), new AltarSkillEntry("Frenzied Charge","frenzied_charge","Pull back, then dash forward with a thrust that launches small foes and stuns the large."), new AltarSkillEntry("Eclipse","eclipse","Your blade swells with magic for one sweeping slash all around you."), new AltarSkillEntry("Halfmoon Slash (Ultimate)","halfmoon_slash","A colossal half-moon slash, followed by its afterimage.") };
                case "Mercenary": return passive ? new AltarSkillEntry[] { new AltarSkillEntry("Warfreak (Mastery)","warfreak","Dual-wield any two one-handed physical weapons. Hyper Armor unless a single hit deals 60% of your Max HP or more. +10 Sword, Axe and Clubs, +25% Attack Speed with two one-handed physical weapons of the same type or a two-handed physical weapon, no physical weapon movement penalty, +30% Armor, 25% less damage taken while doing a skill (35% in the Ultimate). Unchained Fury triggers at 100 Fury for 20s."), new AltarSkillEntry("Battlecry (Grace)","battlecry","A war cry: you and nearby allies deal +15% damage to creatures for 1 minute and +25% damage to trees, rocks and ore for 3 minutes.") } : new AltarSkillEntry[] { new AltarSkillEntry("Stomp","stomp","Stomp the earth: a crushing impact, then an aftershock rolls outward."), new AltarSkillEntry("Circle Swing","circle_swing","Wind up and swing your weapon in a full circle, staggering everything around you."), new AltarSkillEntry("Bonecrusher","bonecrusher","Leap high and crash down, shattering the bones of everything below."), new AltarSkillEntry("Seismic Guillotine","seismic_guillotine","Tear a fissure through the ground to your aim, ending in a seismic explosion."), new AltarSkillEntry("Punishing Bomb","punishing_bomb","Bat a bomb into the enemy lines. It bursts on the first thing it touches and leaves them burning."), new AltarSkillEntry("Whirlwind (Ultimate)","whirlwind","Spin into a whirlwind of steel, carving everything that comes near.") };
                case "Paladin": return passive ? new AltarSkillEntry[] { new AltarSkillEntry("Heaven's Will (Mastery)","holy_trinity","+10% Magic Damage. With a Club-type weapon and a Shield: +15 Clubs, no Armor movement penalty, and Slash and Pierce each rise to at least 50% of your current Blunt damage. Holy Bulwark: with a Tower Shield your block covers 3x the area (front and sides) behind a holy force field."), new AltarSkillEntry("Heaven's Light (Grace)","heavens_light","Bless every ally around you with +40% Overall Defense and free them from equipment movement penalties for 1 minute.") } : new AltarSkillEntry[] { new AltarSkillEntry("Goddess Relic","goddess_relic","Summon a holy Cross that strikes with Blunt and Lightning and burns foes with Spirit fire."), new AltarSkillEntry("Judgement Hammer","judgement_hammer","Hurl a holy hammer that grows as it flies, crushing everything in its path."), new AltarSkillEntry("Shield Charge","shield_charge","Raise your shield and charge forward, trampling everyone who dares stand in your path."), new AltarSkillEntry("Angel Comet","fallen_angel","Leap to the heavens, then crash upon your enemies like a blazing comet."), new AltarSkillEntry("Ray of Hope","ray_of_hope","Raise your hand to the heavens, healing every ally around you, strengthening their attacks, removing every debuff and warding them against new ones for 12s."), new AltarSkillEntry("Electric Smite (Ultimate)","electric_smite","Leap and slam down, releasing sixteen Lightning Trails that tear across the ground.") };
                case "Priest": return passive ? new AltarSkillEntry[] { new AltarSkillEntry("Bless Thy Sinners (Mastery)","bless_thy_sinners","+10% Magic Damage. When you or an ally within 20m would die, survive at 1 HP and recover 50% HP over time with a burst of speed. Your own save recharges in 20 minutes, each ally's separately. With a Buckler, a Parry releases a Holy Shockwave, grants Hyper Armor and empowers your next skill."), new AltarSkillEntry("Heaven's Crucible (Grace)","grand_sigil","Wrap yourself and nearby allies in a holy Barrier that holds until it breaks.") } : new AltarSkillEntry[] { new AltarSkillEntry("Lightning Relic","lightning_relic","Plant a Cross of lightning. Its pulses punish nearby foes; place Holy Relic beside it to consecrate the ground."), new AltarSkillEntry("Holy Relic","holy_relic","Raise a sacred Cross that restores your allies and strengthens everyone within its light."), new AltarSkillEntry("Divine Intervention","divine_intervention","Answer danger with a burst of holy power, restoring allies and exposing enemies around you or an aimed Relic."), new AltarSkillEntry("Grand Cross","grand_cross","Carve a radiant X through the battlefield. Its crossing blades travel forward, burning every foe they touch."), new AltarSkillEntry("Heaven's Judgement","heavens_judgement","Call a barrage of holy beams around yourself or an aimed Relic, chilling the enemies caught beneath them."), new AltarSkillEntry("Lightning Tempest (Ultimate)","lightning_tempest","Unleash a restless storm, layering lightning and afflictions across the battlefield.") };
                case "Wizard": return passive ? new AltarSkillEntry[] { new AltarSkillEntry("Archmage (Mastery)","archmage","Charge normal Staff attacks (not Gun Staves) for up to 3 stacks, 1 per second; the first stack doubles the impact size. Spending 300 Eitr triggers Overcharge for 12s: +40% faster wind up, +40% Eitr Regen and +40% Magic Damage."), new AltarSkillEntry("Clockwork (Grace)","clockwork","Bend time for you and nearby allies: +30% Skill Damage and skills that start their cooldown are 50% faster to recover for 22s.") } : new AltarSkillEntry[] { new AltarSkillEntry("Gravity Dominion","gravity_dominion","Create a gravitational field at the targeted area that controls enemies inside it."), new AltarSkillEntry("Astral Greatblade","astral_greatblade","Wind up and aim an astral blade attack. Holding the charge increases its power."), new AltarSkillEntry("Frost Nova","frost_nova","Release an area burst of Frost around the caster."), new AltarSkillEntry("Meteor Fall","meteor_fall","Call a meteor onto the targeted ground area."), new AltarSkillEntry("Astral Railcannon","astral_railcannon","Assemble and fire an aimed magical beam through the area ahead."), new AltarSkillEntry("Elemental Cataclysm (Ultimate)","elemental_cataclysm","Charge and release the Archmage's large elemental area attack at the targeted location.") };
                case "Spellcaster": return passive ? new AltarSkillEntry[] { new AltarSkillEntry("Yin and Yang (Mastery)","yin_and_yang","With any Staff or Wand: attacks twice as fast, Eitr use -50%, +20% Eitr Regen, normal Staff/Wand damage -50%. No skill wind up, no Staff/Wand movement penalty. Dual Gun Staves fire together, twice as fast (each shot costs its Eitr), with perfect accuracy. Every skill is cast on the move."), new AltarSkillEntry("Rift Walker (Grace)","rift_walker","Place two linked portals up to 50m away and travel between them with E.") } : new AltarSkillEntry[] { new AltarSkillEntry("Arcane Phalanx","arcane_phalanx","Summon hovering arcane swords; Mouse1 launches them one by one, recast and click to fire them all."), new AltarSkillEntry("Arcane Phantom","afterimage_arsenal","Leave spectral copies of yourself that fire along with your attacks."), new AltarSkillEntry("Void Step","void_step","Teleport instantly to your aim, even mid-attack, and float down safely."), new AltarSkillEntry("Rift Echo","rift_echo","Open rifts behind your target that echo your attacks back through it."), new AltarSkillEntry("Gravity Blast","gravity_blast","Hurl a ball of darkness that grinds through every enemy and drags the small ones in."), new AltarSkillEntry("Arcane Rupture (Ultimate)","arcane_rupture","Rupture the ground at your aim again and again, up to three charges, while you keep moving.") };
            }
            return new AltarSkillEntry[0];
        }

        private void BuildBaseClassPage()
        {
            _baseClassPage = CreateUiGroup("DragonAltarBaseClassPage");
            AltarImage("BaseHeadingPlaque", _baseClassPage.transform, new Vector2(-291f, 218f), new Vector2(380f, 62f), AltarSprite("Confirm_Plaque.png"), Color.white, false);
            CreateWrappedText(_baseClassPage.transform, "BASE CLASSES", new Vector2(-291f, 218f), 350f, 42f, 28, AltarGold, true, TextAnchor.MiddleCenter);
            // v0.24.0: four Base Classes; the cards keep their art and layout, scaled to fit the column.
            CreateBaseClassNavRow("Warrior", "Front-line physical bruiser", 128f, Color.white);
            CreateBaseClassNavRow("Cleric", "Holy hybrid support", 4f, Color.white);
            CreateBaseClassNavRow("Sorcerer", "Eitr-first magic specialist", -120f, Color.white);
            CreateBaseClassNavRow("Ranger", "Agile bow hunter", -244f, Color.white);
            _baseDetailTitle = CreateWrappedText(_baseClassPage.transform, "", new Vector2(215f, 228f), 500f, 36f, 30, AltarGold, true, TextAnchor.MiddleCenter);
            _baseDetailRole = CreateWrappedText(_baseClassPage.transform, "", new Vector2(215f, 203f), 500f, 20f, 14, AltarGold, false, TextAnchor.MiddleCenter);
            BuildAltarSkillDetails(_baseClassPage.transform, false);
            Button choose = AltarButton(_baseClassPage.transform, "Choose Warrior", new Vector2(80f, -352f), new Vector2(248f, 56f), RequestBaseClassConfirmation);
            _chooseBaseButtonText = choose.GetComponentInChildren<Text>();
            AltarButton(_baseClassPage.transform, "View Advancements", new Vector2(345f, -352f), new Vector2(258f, 56f), OpenFocusedAdvancements);
        }

        private void AltarClassCard(Transform parent, string name, string role, float y, bool advancement)
        {
            Sprite sprite = AltarCardSprite(name);
            Image image = AltarImage("ClassCard_" + name, parent, new Vector2(-291f, y), new Vector2(388f, 150f), sprite,
                sprite == null ? new Color(0.90f, 0.84f, 0.72f, 1f) : Color.white, true);
            Outline outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.74f, 0.23f, 0.95f);
            outline.effectDistance = new Vector2(2f, -2f);
            outline.enabled = false;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 0.93f, 0.78f, 1f);
            colors.pressedColor = new Color(0.8f, 0.85f, 0.9f, 1f);
            button.colors = colors;
            button.onClick.AddListener(delegate { if (advancement) FocusAdvancement(name); else FocusBaseClass(name); });
            AddAltarHoverGlow(image.gameObject);
            // v0.20.4: a light veil only, so the card art stays visible behind the text (v0.25.1: 12%, barely visible);
            // a soft parchment glow on the letters keeps them readable on darker art.
            // v0.22.5: cards centred in the left column (opening x 30-563 of the 1349 px backdrop -> centre -291).
            // v0.22.3: centred on the card's art panel (measured 41%-98.5% of the card width -> centre
            // +78 px), name and role balanced around the panel's vertical centre.
            AltarImage("CardLabelParchment", image.transform, new Vector2(78f, 0f), new Vector2(222f, 124f), AltarLabelVeil(), new Color(0.98f, 0.92f, 0.80f, 0.12f), false);
            AltarCardTextGlow(CreateWrappedText(image.transform, AcDisplay(name).ToUpper(), new Vector2(78f, 19f), 198f, 58f, 23, AltarInk, true, TextAnchor.MiddleCenter));
            AltarCardTextGlow(CreateWrappedText(image.transform, role, new Vector2(78f, -22f), 194f, 40f, 15, AltarInk, false, TextAnchor.MiddleCenter));
            (advancement ? _altarAdvCards : _altarBaseCards)[name] = image;
        }

        private static void AltarCardTextGlow(Text label)
        {
            Outline glow = label.gameObject.AddComponent<Outline>();
            glow.effectColor = new Color(0.99f, 0.94f, 0.82f, 0.55f);
            glow.effectDistance = new Vector2(1.2f, -1.2f);
            Shadow halo = label.gameObject.AddComponent<Shadow>();
            halo.effectColor = new Color(0.99f, 0.94f, 0.82f, 0.35f);
            halo.effectDistance = new Vector2(-1.2f, 1.2f);
        }

        private void RefreshAltarCards(Dictionary<string, Image> cards, string selected)
        {
            foreach (KeyValuePair<string, Image> card in cards)
            {
                if (card.Value == null) continue;
                bool active = card.Key == selected;
                card.Value.GetComponent<Outline>().enabled = active;
                ColorBlock colors = card.Value.GetComponent<Button>().colors;
                colors.normalColor = active ? Color.white : new Color(0.88f, 0.88f, 0.88f, 1f);
                card.Value.GetComponent<Button>().colors = colors;
            }
        }

        private void CreateBaseClassNavRow(string className, string role, float y, Color accent)
        {
            AltarClassCard(_baseClassPage.transform, className, role, y, false);
            Image card;
            if (_altarBaseCards.TryGetValue(className, out card) && card != null)
                card.rectTransform.localScale = new Vector3(0.80f, 0.80f, 1f);
        }

        private void CreateAdvancementNavRow(Transform parent, string advancementName, string role, float y, Color accent)
        {
            AltarClassCard(parent, advancementName, role, y, true);
        }

        // v0.23.3 display names (internal ids stay "Wizard" / "Spellcaster" so saves keep working).
        internal static string AcDisplay(string ac)
        {
            if (ac == "Spellcaster") return "Horizon Walker";
            if (ac == "Wizard") return "Archmage";
            return ac;
        }

        private void BuildAdvancementPage()
        {
            _advancementPage = CreateUiGroup("DragonAltarAdvancementPage");
            AltarImage("AdvHeadingPlaque", _advancementPage.transform, new Vector2(-291f, 212f), new Vector2(380f, 82f), AltarSprite("Confirm_Plaque.png"), Color.white, false);
            _advancementPageTitle = CreateWrappedText(_advancementPage.transform, "CHOOSE AN\nADVANCEMENT", new Vector2(-291f, 212f), 340f, 67f, 25, AltarGold, true, TextAnchor.MiddleCenter);
            AltarButton(_advancementPage.transform, "‹ Base Classes", new Vector2(-291f, 146f), new Vector2(240f, 43f), ShowBaseClassPage);
            // v0.23.3: lighter veil (like the cards) and ONE text block (title + body) centred both ways
            // inside the veil's opaque part (veil 404x150, 8/64 + 5/32 soft edges -> ~300x104 usable),
            // shrunk to fit so nothing sticks out.
            AltarImage("FoundationParchment", _advancementPage.transform, new Vector2(-291f, -326f), new Vector2(404f, 150f), AltarLabelVeil(), new Color(0.98f, 0.92f, 0.8f, 0.55f), false);
            _advancementBaseSummaryTitle = CreateWrappedText(_advancementPage.transform, "", new Vector2(-291f, -326f), 296f, 100f, 15, AltarInk, false, TextAnchor.MiddleCenter);
            _advancementBaseSummaryTitle.supportRichText = true;
            _advancementBaseSummaryTitle.resizeTextForBestFit = true;
            _advancementBaseSummaryTitle.resizeTextMinSize = 9;
            _advancementBaseSummaryTitle.resizeTextMaxSize = 15;
            AltarCardTextGlow(_advancementBaseSummaryTitle);
            _advancementBaseSummaryBody = null;
            _advDetailTitle = CreateWrappedText(_advancementPage.transform, "", new Vector2(215f, 228f), 500f, 36f, 30, AltarGold, true, TextAnchor.MiddleCenter);
            _advDetailRole = CreateWrappedText(_advancementPage.transform, "", new Vector2(215f, 203f), 500f, 20f, 14, AltarGold, false, TextAnchor.MiddleCenter);
            BuildAltarSkillDetails(_advancementPage.transform, true);
            _chooseAdvButton = AltarButton(_advancementPage.transform, "Choose Advancement", new Vector2(215f, -352f), new Vector2(340f, 56f), RequestAdvancementConfirmation);
            _chooseAdvButtonText = _chooseAdvButton.GetComponentInChildren<Text>();
        }

        private void BuildConfirmationPanel()
        {
            _confirmationPanel = AltarImage("DragonAltarConfirmation", _classPanel.transform, Vector2.zero, new Vector2(1040f, 900f), null, new Color(0.02f, 0.04f, 0.08f, 0.86f), true).gameObject;
            Image parchment = AltarImage("ConfirmParchment", _confirmationPanel.transform, Vector2.zero, new Vector2(660f, 370f), null, new Color(0.96f, 0.89f, 0.75f, 1f), true);
            Outline border = parchment.gameObject.AddComponent<Outline>();
            border.effectColor = AltarGold;
            border.effectDistance = new Vector2(3f, -3f);
            CreateWrappedText(parchment.transform, "CONFIRM YOUR CHOICE", new Vector2(0f, 136f), 610f, 46f, 27, AltarInk, true, TextAnchor.MiddleCenter);
            _confirmationText = CreateWrappedText(parchment.transform, "", new Vector2(0f, 20f), 580f, 172f, 18, AltarInk, false, TextAnchor.MiddleCenter);
            AltarButton(parchment.transform, "Yes, Confirm", new Vector2(-150f, -125f), new Vector2(260f, 50f), ConfirmPendingSelection);
            AltarButton(parchment.transform, "Cancel", new Vector2(150f, -125f), new Vector2(260f, 50f), CancelConfirmation);
            _confirmationPanel.SetActive(false);
        }

        private void RebuildAdvancementNavigation()
        {
            _altarAdvCards.Clear();
            Transform old = _advancementPage.transform.Find("AdvancementNavigation");
            if (old != null)
            {
                old.gameObject.SetActive(false);
                Destroy(old.gameObject);
            }

            GameObject nav = new GameObject("AdvancementNavigation");
            nav.transform.SetParent(_advancementPage.transform, false);
            RectTransform rect = nav.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(980f, 710f);

            if (_advancementParent == "Warrior")
            {
                CreateAdvancementNavRow(nav.transform, "Sword Master", "Speed + spirit swordplay", 25f, new Color(0.72f, 0.86f, 1f, 1f));
                CreateAdvancementNavRow(nav.transform, "Mercenary", "Control + brutal AoE", -155f, new Color(1f, 0.72f, 0.35f, 1f));
            }
            else if (_advancementParent == "Cleric")
            {
                CreateAdvancementNavRow(nav.transform, "Paladin", "Holy impact + resilience", 25f, new Color(1f, 0.87f, 0.42f, 1f));
                CreateAdvancementNavRow(nav.transform, "Priest", "Relics + team support", -155f, new Color(0.60f, 0.91f, 1f, 1f));
            }
            else if (_advancementParent == "Ranger")
            {
                CreateAdvancementNavRow(nav.transform, "Acrobat", "Aerial wind archery", 25f, new Color(0.45f, 0.95f, 0.85f, 1f));
                CreateAdvancementNavRow(nav.transform, "Bowmaster", "Planted heavy artillery", -155f, new Color(0.55f, 0.85f, 0.45f, 1f));
            }
            else
            {
                CreateAdvancementNavRow(nav.transform, "Wizard", "Charged large-scale magic", 25f, new Color(0.78f, 0.42f, 1f, 1f));
                CreateAdvancementNavRow(nav.transform, "Spellcaster", "Rapid mobile spatial magic", -155f, new Color(0.92f, 0.50f, 1f, 1f));
            }
        }


        private void FocusBaseClass(string className)
        {
            _focusedBaseClass = className;
            if (_altarBaseScroll != null) _altarBaseScroll.verticalNormalizedPosition = 1f;
            UpdateBaseClassDetail();
        }

        private void UpdateBaseClassDetail()
        {
            RefreshAltarCards(_altarBaseCards, _focusedBaseClass);
            string role = GetBaseClassRole(_focusedBaseClass);
            Color accent = GetBaseClassAccent(_focusedBaseClass);

            if (_baseDetailTitle != null)
            {
                _baseDetailTitle.text = _focusedBaseClass.ToUpper();
                _baseDetailTitle.color = AltarGold;
            }
            if (_baseDetailRole != null)
            {
                _baseDetailRole.text = role.ToUpper();
                _baseDetailRole.color = AltarGold;
            }
            if (_baseDetailBody != null)
            {
                string ov, hd, bd;
                AltarSplit(GetBaseClassDescription(_focusedBaseClass), false, out ov, out hd, out bd);
                _baseDetailBody.text = ov;
                if (_altarBaseBlessing != null) _altarBaseBlessing.text = bd;
                if (_altarBaseBlessingLabel != null) _altarBaseBlessingLabel.text = hd;
                if (_altarBaseBlessingScroll != null) _altarBaseBlessingScroll.verticalNormalizedPosition = 1f;
            }
            RefreshAltarSkillFooter(_focusedBaseClass, false);
            if (_chooseBaseButtonText != null)
                _chooseBaseButtonText.text = "Choose " + _focusedBaseClass;
        }

        private void OpenFocusedAdvancements()
        {
            _advancementParent = _focusedBaseClass;
            _focusedAdvancement = GetFirstAdvancement(_advancementParent);
            ShowAdvancementPage();
        }

        private void ShowBaseClassPage()
        {
            _classUiPage = 0;
            if (_altarSubtitle != null) _altarSubtitle.text = "AETHELBORN ASCENDED  •  CLASS SELECTION";
            if (_confirmationPanel != null)
                _confirmationPanel.SetActive(false);
            if (_baseClassPage != null)
                _baseClassPage.SetActive(true);
            if (_advancementPage != null)
                _advancementPage.SetActive(false);
            UpdateBaseClassDetail();
        }

        private void ShowAdvancementPage()
        {
            _classUiPage = 1;
            if (_altarSubtitle != null) _altarSubtitle.text = "AETHELBORN ASCENDED  •  ADVANCEMENT SELECTION";
            if (_confirmationPanel != null)
                _confirmationPanel.SetActive(false);
            if (_baseClassPage != null)
                _baseClassPage.SetActive(false);
            if (_advancementPage != null)
                _advancementPage.SetActive(true);

            if (_advancementPageTitle != null)
                _advancementPageTitle.text = "CHOOSE AN\nADVANCEMENT";
            if (_advancementBaseSummaryTitle != null)
                _advancementBaseSummaryTitle.text = "<b>" + _advancementParent.ToUpper() + "</b>\n<b>" + GetBaseClassRole(_advancementParent).ToUpper() + "</b>\n\n" + GetBaseClassSummary(_advancementParent);
            if (_advancementBaseSummaryBody != null)
                _advancementBaseSummaryBody.text = GetBaseClassSummary(_advancementParent);

            RebuildAdvancementNavigation();
            FocusAdvancement(_focusedAdvancement);
        }

        private void FocusAdvancement(string advancementName)
        {
            _focusedAdvancement = advancementName;
            RefreshAltarCards(_altarAdvCards, advancementName);
            if (_altarAdvScroll != null) _altarAdvScroll.verticalNormalizedPosition = 1f;
            Color accent = GetAdvancementAccent(advancementName);

            if (_advDetailTitle != null)
            {
                _advDetailTitle.text = AcDisplay(advancementName).ToUpper();
                _advDetailTitle.color = AltarGold;
            }
            if (_advDetailRole != null)
            {
                _advDetailRole.text = GetAdvancementRole(advancementName).ToUpper();
                _advDetailRole.color = AltarGold;
            }
            if (_advDetailBody != null)
            {
                string ov, hd, bd;
                AltarSplit(GetAdvancementDescription(advancementName), true, out ov, out hd, out bd);
                _advDetailBody.text = ov;
                if (_altarAdvBlessing != null) _altarAdvBlessing.text = bd;
                if (_altarAdvBlessingLabel != null) _altarAdvBlessingLabel.text = hd;
                if (_altarAdvBlessingScroll != null) _altarAdvBlessingScroll.verticalNormalizedPosition = 1f;
            }
            RefreshAltarSkillFooter(advancementName, true);

            Player player = Player.m_localPlayer;
            string selectedClass = player == null ? string.Empty : GetSelectedClass(player);
            bool canChoose = selectedClass == _advancementParent;

            if (_chooseAdvButton != null)
                _chooseAdvButton.interactable = canChoose;
            if (_chooseAdvButtonText != null)
                _chooseAdvButtonText.text = canChoose ? "Choose " + advancementName : "Choose " + _advancementParent + " First";
        }

        private void RequestBaseClassConfirmation()
        {
            _pendingSelectionType = "Class";
            _pendingSelectionName = _focusedBaseClass;
            _pendingRequiredClass = string.Empty;

            if (_confirmationText != null)
            {
                _confirmationText.text =
                    "Choose <b>" + _focusedBaseClass + "</b> as your Base Class?\n\n" +
                    "Changing your Base Class clears any current Advancement and class-specific locked choices.\n\n" +
                    "Are you definite with this choice?";
            }
            ShowConfirmation();
        }

        private void RequestAdvancementConfirmation()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
                return;

            if (GetSelectedClass(player) != _advancementParent)
            {
                ShowCenterMessage("Choose " + _advancementParent + " as your Base Class first.");
                return;
            }

            _pendingSelectionType = "Advancement";
            _pendingSelectionName = _focusedAdvancement;
            _pendingRequiredClass = _advancementParent;

            if (_confirmationText != null)
            {
                _confirmationText.text =
                    "Advance from <b>" + _advancementParent + "</b> to <b>" + AcDisplay(_focusedAdvancement) + "</b>?\n\n" +
                    "This defines the specialized skills, passive mechanics and playstyle of this path.\n\n" +
                    "Are you definite with this choice?";
            }
            ShowConfirmation();
        }

        private void ShowConfirmation()
        {
            if (_baseClassPage != null)
                _baseClassPage.SetActive(false);
            if (_advancementPage != null)
                _advancementPage.SetActive(false);
            if (_confirmationPanel != null)
            {
                _confirmationPanel.SetActive(true);
                _confirmationPanel.transform.SetAsLastSibling();
            }
        }

        private void CancelConfirmation()
        {
            _pendingSelectionType = string.Empty;
            _pendingSelectionName = string.Empty;
            _pendingRequiredClass = string.Empty;

            if (_classUiPage == 1)
                ShowAdvancementPage();
            else
                ShowBaseClassPage();
        }

        private void ConfirmPendingSelection()
        {
            string type = _pendingSelectionType;
            string name = _pendingSelectionName;
            string required = _pendingRequiredClass;

            _pendingSelectionType = string.Empty;
            _pendingSelectionName = string.Empty;
            _pendingRequiredClass = string.Empty;

            if (type == "Class")
                ApplyClassSelection(name);
            else if (type == "Advancement")
                ApplyAdvancementSelection(name, required);
        }

        private string GetBaseClassRole(string className)
        {
            if (className == "Warrior") return "Front-line physical bruiser";
            if (className == "Cleric") return "Holy hybrid support";
            if (className == "Ranger") return "Agile bow hunter";
            return "Eitr-first magic specialist";
        }

        private Color GetBaseClassAccent(string className)
        {
            if (className == "Warrior") return new Color(1f, 0.63f, 0.28f, 1f);
            if (className == "Cleric") return new Color(0.55f, 0.86f, 1f, 1f);
            if (className == "Ranger") return new Color(0.50f, 0.90f, 0.45f, 1f);
            return new Color(0.78f, 0.52f, 1f, 1f);
        }

        private string GetBaseClassSummary(string className)
        {
            if (className == "Warrior")
                return "Heavy melee foundation built around Hyper Armor, Broken Bones and direct frontline pressure.";
            if (className == "Cleric")
                return "Holy battlemage foundation mixing Lightning pressure, healing and flexible divine equipment.";
            if (className == "Ranger")
                return "Bow-first hunter foundation built around piercing shots, evasive tumbles and traps.";
            return "Magic-first foundation built around Eitr management, elemental pressure and Staff/Wand combat.";
        }

        private string GetFirstAdvancement(string className)
        {
            if (className == "Warrior") return "Sword Master";
            if (className == "Cleric") return "Paladin";
            if (className == "Ranger") return "Acrobat";
            return "Wizard";
        }

        private Color GetAdvancementAccent(string advancementName)
        {
            if (advancementName == "Sword Master") return new Color(0.72f, 0.86f, 1f, 1f);
            if (advancementName == "Mercenary") return new Color(1f, 0.72f, 0.35f, 1f);
            if (advancementName == "Paladin") return new Color(1f, 0.87f, 0.42f, 1f);
            if (advancementName == "Priest") return new Color(0.60f, 0.91f, 1f, 1f);
            if (advancementName == "Wizard") return new Color(0.78f, 0.42f, 1f, 1f);
            if (advancementName == "Acrobat") return new Color(0.45f, 0.95f, 0.85f, 1f);
            if (advancementName == "Bowmaster") return new Color(0.55f, 0.85f, 0.45f, 1f);
            return new Color(0.92f, 0.50f, 1f, 1f);
        }

        private string GetAdvancementRole(string advancementName)
        {
            if (advancementName == "Sword Master") return "Fast sword specialist";
            if (advancementName == "Mercenary") return "Control and brutal AoE pressure";
            if (advancementName == "Paladin") return "Holy impact and resilient offense";
            if (advancementName == "Priest") return "Relics, healing and team support";
            if (advancementName == "Wizard") return "Charged large-scale magic";
            if (advancementName == "Acrobat") return "Aerial wind archery";
            if (advancementName == "Bowmaster") return "Planted heavy artillery";
            return "Rapid mobile spatial magic";
        }

        private string GetBaseClassDescription(string className)
        {
            if (className == "Warrior")
            {
                return "<b>IDENTITY</b>\nWarrior is the direct melee front-liner: simple to understand, difficult to bully, and built to stay close while forcing enemies to respect physical pressure.\n\n<b>WARRIOR'S BLESSING</b>\nHyper Armor: hits below 30% of your Max HP never interrupt you. Parry is doubled. +20 Run and +20 Jump.\n\n<b>BEST FOR</b>\nPlayers who want to commit to melee, trade confidently and control space with physical attacks, then specialize into speed (Sword Master) or overwhelming AoE (Mercenary).";
            }
            if (className == "Cleric")
            {
                return "<b>IDENTITY</b>\nCleric mixes holy offense with healing and protection. It deals real damage without giving up the ability to rescue itself or its party.\n\n<b>CLERIC'S BLESSING</b>\nEvery Shield gets 1.5x Block Force and Block Armor. A Staff and a Shield can be wielded together. No movement penalty from Shields, Staves or one-handed Club weapons. +35 Max HP and +20% HP Regen.\n\n<b>BEST FOR</b>\nPlayers who want to fight on the front line and keep everyone standing.";
            }
            if (className == "Ranger")
                return "<b>IDENTITY</b>\nRanger is the bow-first hunter: precise shots from range, evasive footwork and traps that decide where the fight happens.\n\n<b>RANGER'S BLESSING - WILDBORN</b>\n+20 Bows and +20 Dodge, -30% fall damage, no Bow movement penalty. Left Click fires a 4-shot chain at full-draw range; Right Click draws the charged shot (Left Click releases it). Arrows are infinite: you deal 50% of the Bow damage plus 50% of the arrow's while carrying a full stack. Rangers cannot Block or use Shields.\n\n<b>BEST FOR</b>\nPlayers who want to fight from range, then specialize into aerial wind archery (Acrobat) or planted heavy artillery (Bowmaster).";
            return "<b>IDENTITY</b>\nSorcerer is the magic-first base class. Its power comes from Eitr management and large spell effects rather than weapons.\n\n<b>SORCERER'S BLESSING - WARLOCK</b>\nCreature melee damage -70% (mining and woodcutting are not affected). +65 Max Eitr, +35% Eitr Regen, and Eitr starts regenerating twice as fast. A Sorcerer cannot Block, Parry or equip Shields.\n\n<b>BEST FOR</b>\nPlayers who want spell rotations, resource management and spectacular magic, then specialize into charged artillery (Wizard) or rapid mobile casting (Spellcaster).";
        }

        private string GetBaseClassSkills(string className)
        {
            if (className == "Warrior") return "<b>STARTER KIT</b>  Heavy Slash  |  Impact Wave  |  Impact Punch\n<b>ADVANCEMENTS</b>  Sword Master  |  Mercenary";
            if (className == "Cleric") return "<b>STARTER KIT</b>  Lightning Zap  |  Righteous Strike  |  Holy Wave\n<b>ADVANCEMENTS</b>  Paladin  |  Priest";
            if (className == "Ranger") return "<b>STARTER KIT</b>  Piercing Arrow  |  Tumble Shot  |  Snare Trap\n<b>ADVANCEMENTS</b>  Acrobat  |  Bowmaster";
            return "<b>STARTER KIT</b>  Flame Burst  |  Glacial Descent  |  Stonefang Eruption\n<b>ADVANCEMENTS</b>  Archmage  |  Horizon Walker";
        }

        private string GetAdvancementDescription(string advancementName)
        {
            if (advancementName == "Sword Master")
            {
                return "<b>IDENTITY</b>\nSword Master turns Warrior into a high-tempo sword specialist built around fast attacks, broad spirit slashes and precise burst windows.\n\n<b>THE WAY OF THE SWORD (MASTERY)</b>\nWith exactly one Sword: +20 Sword, +50% Attack Speed and no Sword movement penalty. Blocking or dodging cancels the rest of a skill sequence.\n\n<b>KNIGHT'S GUIDANCE (GRACE)</b>\nYou and allies within 10m move 1.5x faster, regenerate Stamina 40% faster and use 30% less Stamina for 3 minutes.\n\n<b>BEST FOR</b>\nAggressive players who want flashy sword pressure, ranged melee, charge management and a fast combo rhythm rather than tanking through everything.";
            }
            if (advancementName == "Mercenary")
            {
                return "<b>IDENTITY</b>\nMercenary is the brutal battlefield controller: heavy physical attacks, violent displacement and huge close-range pressure.\n\n<b>WARFREAK (MASTERY)</b>\nDual-wield any two one-handed physical weapons. +10 Sword, Axe and Clubs, +25% Attack Speed with two one-handed physical weapons of the same type or a two-handed physical weapon, no physical weapon movement penalty and +30% Armor. While doing a skill you take 25% less damage (35% during the Ultimate). Every hit builds Fury; at 100 Unchained Fury erupts for 20s.\n\n<b>BATTLECRY (GRACE)</b>\nYou and nearby allies deal +15% damage to creatures for 1 minute and +25% damage to trees, rocks and ore for 3 minutes.\n\n<b>BEST FOR</b>\nPlayers who enjoy crowd control, displacement, huge AoEs, mixed-weapon brutality and chaotic close-range pressure.";
            }
            if (advancementName == "Paladin")
            {
                return "<b>IDENTITY</b>\nPaladin is Cleric's holy bruiser: weapon and shield in hand, heavy holy strikes and the protection of the heavens.\n\n<b>HEAVEN'S WILL (MASTERY)</b>\n+10% Magic Damage. With a Club-type weapon and a Shield: +15 Clubs, no Armor movement penalty, and Slash and Pierce each rise to at least 50% of your current Blunt damage.\n\n<b>HEAVEN'S LIGHT (GRACE)</b>\nAllies around you gain +40% Overall Defense and lose equipment movement penalties for 1 minute.\n\n<b>BEST FOR</b>\nPlayers who want to lead the charge and hit hard while shielding the party.";
            }
            if (advancementName == "Priest")
            {
                return "<b>IDENTITY</b>\nPriest is the battlefield support: holy Relics anchor the fight, while heals, barriers and judgement rain from afar.\n\n<b>BLESS THY SINNERS (MASTERY)</b>\n+10% Magic Damage. When you or an ally within 20m would die, survive at 1 HP and recover 50% HP over time with a burst of speed. Your own save recharges in 20 minutes, each ally's separately. With a Buckler your Parry is doubled; a Parry grants 5s Hyper Armor, +35% Damage to your next skill (kept until used, 25s cooldown after) and a 10m Holy Shockwave that Stuns.\n\n<b>HEAVEN'S CRUCIBLE (GRACE)</b>\nWrap yourself and nearby allies in a holy Barrier that holds until it breaks.\n\n<b>BEST FOR</b>\nPlayers who want to keep everyone alive and control the battlefield.";
            }
            if (advancementName == "Wizard")
            {
                return "<b>IDENTITY</b>\nThe Archmage is the deliberate heavy-artillery caster: huge committed spells, charged Staff shots and overwhelming single releases.\n\n<b>ARCHMAGE (MASTERY)</b>\nCharge normal Staff attacks (not Gun Staves) for up to 3 stacks, 1 per second; the first stack doubles the impact size. Spending 300 Eitr triggers Overcharge for 12s: +40% faster wind up, +40% Eitr Regen and +40% Magic Damage.\n\n<b>BEST FOR</b>\nPlayers who want magical artillery, giant telegraphed attacks and the satisfaction of charging one disgusting hit instead of spraying dozens of smaller ones.";
            }
            if (advancementName == "Acrobat")
                return "<b>IDENTITY</b>\nThe Acrobat is the aerial skirmisher: always moving, always above the fight, raining wind-driven arrows from angles nobody expects.\n\n<b>WINDSTEP (MASTERY)</b>\nA second jump in mid-air, Dodge costs 50% less Stamina, all Stamina use -35%, fall damage -75% and falls never kill, every Ranger skill can be cast in the air, and each enemy hit by your skills trims 1s off your shortest cooldown (up to 3s per cast).\n\n<b>TAILWIND (GRACE)</b>\nYou and nearby allies move 50% faster, jump higher and take no fall damage for 2 minutes.\n\n<b>BEST FOR</b>\nPlayers who never want to touch the ground: fast cooldowns, dashes and shots from the air.";
            if (advancementName == "Bowmaster")
                return "<b>IDENTITY</b>\nThe Bowmaster is the planted artillery archer: long cooldowns, huge charged shots and volleys that decide a battle.\n\n<b>DEADEYE (MASTERY)</b>\nStanding still builds Focus (up to 5 stacks, +8% damage and +10% range each); moving drains it. Fully charged shots deal +30% damage. Crossbows reload 75% faster, stay loaded when unequipped and lose their movement penalty.\n\n<b>HAWK'S VIGIL (GRACE)</b>\nReveal enemies around you and give allies +20% ranged damage.\n\n<b>BEST FOR</b>\nPlayers who want to hold a position and land one devastating shot after another.";
            return "<b>IDENTITY</b>\nThe Horizon Walker is the mobile gunmage: sprint-casting, rapid Staff pressure, dual Gun Staves and spatial tricks everywhere.\n\n<b>YIN AND YANG (MASTERY)</b>\nWith any Staff or Wand: attacks twice as fast, Eitr use -50%, +20% Eitr Regen, normal Staff/Wand damage -50%. No skill wind up and no Staff/Wand movement penalty. Dual Gun Staves fire together, twice as fast (each shot costs its Eitr), with perfect accuracy. Every skill is cast on the move.\n\n<b>BEST FOR</b>\nPlayers who never want to stand still: constant fire, teleports, portals and clones.";
        }

        private string GetAdvancementSkills(string advancementName)
        {
            if (advancementName == "Spellcaster") return "<b>SKILLS</b>  Arcane Phalanx  |  Arcane Phantom  |  Void Step  |  Rift Echo  |  Gravity Blast  |  Arcane Rupture (Ultimate)\n<b>MASTERY</b>  Yin and Yang  |  <b>GRACE</b>  Rift Walker";
            if (advancementName == "Sword Master") return "<b>SKILLS</b>  Moonlight Splitter  |  Crescent Cleave  |  Blade Storm  |  Frenzied Charge  |  Eclipse  |  Halfmoon Slash (Ultimate)\n<b>MASTERY</b>  The Way of the Sword  |  <b>GRACE</b>  Knight's Guidance";
            if (advancementName == "Mercenary") return "<b>SKILLS</b>  Stomp  |  Circle Swing  |  Bonecrusher  |  Seismic Guillotine  |  Punishing Bomb  |  Whirlwind (Ultimate)\n<b>MASTERY</b>  Warfreak  |  <b>GRACE</b>  Battlecry";
            if (advancementName == "Paladin") return "<b>SKILLS</b>  Goddess Relic  |  Judgement Hammer  |  Shield Charge  |  Angel Comet  |  Ray of Hope  |  Electric Smite (Ultimate)\n<b>MASTERY</b>  Heaven's Will  |  <b>GRACE</b>  Heaven's Light";
            if (advancementName == "Priest") return "<b>SKILLS</b>  Lightning Relic  |  Holy Relic  |  Divine Intervention  |  Grand Cross  |  Heaven's Judgement  |  Lightning Tempest (Ultimate)\n<b>MASTERY</b>  Bless Thy Sinners  |  <b>GRACE</b>  Heaven's Crucible";
            if (advancementName == "Acrobat") return "<b>SKILLS</b>  Gale Volley  |  Cyclone Arrow  |  Swallow Dive  |  Skyfall Barrage  |  Somersault Dance  |  Furious Winds (Ultimate)\n<b>MASTERY</b>  Windstep  |  <b>GRACE</b>  Tailwind";
            if (advancementName == "Bowmaster") return "<b>SKILLS</b>  Ballista Shot  |  Arrow Rain  |  Pinning Shot  |  Explosive Arrow  |  Splitting Arrow  |  Starfall Volley (Ultimate)\n<b>MASTERY</b>  Deadeye  |  <b>GRACE</b>  Hawk's Vigil";
            if (advancementName == "Wizard") return "<b>SKILLS</b>  Meteor Fall  |  Gravity Dominion  |  Astral Railcannon  |  Astral Greatblade  |  Frost Nova  |  Elemental Cataclysm (Ultimate)\n<b>MASTERY</b>  Archmage  |  <b>GRACE</b>  Clockwork";
            return "<b>SKILLS</b>  Rift Echo  |  Void Step  |  Arcane Phalanx  |  Arcane Phantom  |  Arcane Rupture (Ultimate)\n<b>PASSIVE</b>  Riftwalker / Phase Flow + dual Gun Staff mastery";
        }

        internal void OpenClassPanel()
        {
            if (Player.m_localPlayer == null)
                return;

            if (_classPanel == null)
                BuildClassPanel();

            if (_classPanel == null)
                return;

            string selectedClass = GetSelectedClass(Player.m_localPlayer);
            string selectedAdvancement = GetSelectedAdvancement(Player.m_localPlayer);

            if (_currentClassText != null)
                _currentClassText.text = "Base class: " + (string.IsNullOrEmpty(selectedClass) ? "None" : selectedClass);

            if (_currentAdvancementText != null)
                _currentAdvancementText.text = "Advancement: " + (string.IsNullOrEmpty(selectedAdvancement) ? "None" : AcDisplay(selectedAdvancement));

            if (!string.IsNullOrEmpty(selectedClass))
                _focusedBaseClass = selectedClass;
            else
                _focusedBaseClass = "Warrior";

            _advancementParent = _focusedBaseClass;
            _focusedAdvancement = string.IsNullOrEmpty(selectedAdvancement) ? GetFirstAdvancement(_focusedBaseClass) : selectedAdvancement;
            ShowBaseClassPage();

            _classPanel.SetActive(true);
            GUIManager.BlockInput(true);
        }

        private void CloseClassPanel()
        {
            if (_classPanel != null)
                _classPanel.SetActive(false);
            GUIManager.BlockInput(false);
        }

        private void ApplyClassSelection(string className)
        {
            Player player = Player.m_localPlayer;
            if (player == null)
                return;

            string existing = GetSelectedClass(player);
            if (!string.IsNullOrEmpty(existing) && !_allowClassChange.Value)
            {
                ShowCenterMessage("Your class is already " + existing + ".");
                ShowBaseClassPage();
                return;
            }

            if (!SetSelectedClass(player, className))
            {
                Logger.LogError("Could not save class selection to player custom data.");
                ShowCenterMessage("Could not save class selection.");
                return;
            }

            if (existing != className)
            {
                ClearSelectedAdvancement(player);
                ClearPersistentClassChoices(player);
            }

            Logger.LogInfo("Selected class: " + className);
            ShowCenterMessage("Class selected: " + className);

            if (_currentClassText != null)
                _currentClassText.text = "Base class: " + className;
            if (_currentAdvancementText != null)
                _currentAdvancementText.text = "Advancement: None";

            _focusedBaseClass = className;
            _advancementParent = className;
            _focusedAdvancement = GetFirstAdvancement(className);
            ShowAdvancementPage();
        }

        private void ApplyAdvancementSelection(string advancementName, string requiredClass)
        {
            Player player = Player.m_localPlayer;
            if (player == null)
                return;

            string selectedClass = GetSelectedClass(player);
            if (selectedClass != requiredClass)
            {
                ShowCenterMessage("Choose " + requiredClass + " first.");
                return;
            }

            string existing = GetSelectedAdvancement(player);
            if (!string.IsNullOrEmpty(existing) && !_allowAdvancementChange.Value)
            {
                ShowCenterMessage("Your advancement is already " + existing + ".");
                ShowAdvancementPage();
                return;
            }

            if (!SetSelectedAdvancement(player, advancementName))
            {
                Logger.LogError("Could not save advancement selection to player custom data.");
                ShowCenterMessage("Could not save advancement selection.");
                return;
            }

            Logger.LogInfo("Selected advancement: " + advancementName);
            ShowCenterMessage("Advanced to " + advancementName);

            if (_currentAdvancementText != null)
                _currentAdvancementText.text = "Advancement: " + AcDisplay(advancementName);

            _focusedAdvancement = advancementName;
            ShowAdvancementPage();
        }

        private void ResetClassSelection()
        {
            Player player = Player.m_localPlayer;

            if (player == null)
                return;

            if (Time.time > _resetConfirmUntil)
            {
                _resetConfirmUntil = Time.time + 5f;
                ShowCenterMessage("Press Reset Class again within 5 seconds to confirm.");
                return;
            }

            IDictionary data = GetCustomData(player);

            if (data == null)
            {
                ShowCenterMessage("Could not reset class data.");
                return;
            }

            if (data.Contains(ClassDataKey))
                data.Remove(ClassDataKey);

            if (data.Contains(AdvancementDataKey))
                data.Remove(AdvancementDataKey);

            ClearPersistentClassChoices(player);

            _resetConfirmUntil = 0f;

            if (_currentClassText != null)
                _currentClassText.text = "Base class: None";

            if (_currentAdvancementText != null)
                _currentAdvancementText.text = "Advancement: None";

            RefreshFoodStats(player);

            Logger.LogInfo("Class reset completed.");
            ShowCenterMessage("Class reset. Locked passive choices were cleared.");
            ShowBaseClassPage();
        }


        private static void RefreshFoodStats(Player player)
        {
            if (player == null)
                return;

            try
            {
                MethodInfo method = typeof(Player).GetMethod(
                    "UpdateFood",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new Type[] { typeof(float), typeof(bool) },
                    null
                );

                if (method != null)
                    method.Invoke(player, new object[] { 0f, true });
            }
            catch
            {
                // Food stats will refresh naturally if this Valheim build exposes a different signature.
            }
        }

        private static void ClearPersistentClassChoices(Player player)
        {
            IDictionary data = GetCustomData(player);

            if (data == null)
                return;

            if (data.Contains(PaladinVitalityKey))
                data.Remove(PaladinVitalityKey);

            if (data.Contains(PaladinOffenseKey))
                data.Remove(PaladinOffenseKey);

            if (data.Contains(PaladinPassiveKey))
                data.Remove(PaladinPassiveKey);

            if (data.Contains(PriestOffenseKey))
                data.Remove(PriestOffenseKey);

            // Reset/change of the base Class returns its earned points for reassignment.
            // Preserve Level and bonus-point totals; clear chosen Tiers and branch loadouts.
            string[] treeKeys = { "ImmortalHeroes.Tiers", "ImmortalHeroes.Ascended",
                "ImmortalHeroes.HotbarLayout.", "ImmortalHeroes.HotbarLayout.Paladin",
                "ImmortalHeroes.HotbarLayout.Priest" };
            foreach (string key in treeKeys)
                if (data.Contains(key)) data.Remove(key);
        }

        internal static string GetSelectedClass(Player player)
        {
            if (player == null)
                return string.Empty;

            IDictionary data = GetCustomData(player);
            if (data == null || !data.Contains(ClassDataKey))
                return string.Empty;

            object value = data[ClassDataKey];
            return value == null ? string.Empty : value.ToString();
        }

        private static bool SetSelectedClass(Player player, string className)
        {
            IDictionary data = GetCustomData(player);
            if (data == null)
                return false;

            data[ClassDataKey] = className;
            return true;
        }

        internal static string GetSelectedAdvancement(Player player)
        {
            if (player == null)
                return string.Empty;

            IDictionary data = GetCustomData(player);
            if (data == null || !data.Contains(AdvancementDataKey))
                return string.Empty;

            object value = data[AdvancementDataKey];
            return value == null ? string.Empty : value.ToString();
        }

        private static bool SetSelectedAdvancement(Player player, string advancementName)
        {
            IDictionary data = GetCustomData(player);
            if (data == null)
                return false;

            data[AdvancementDataKey] = advancementName;
            return true;
        }

        private static void ClearSelectedAdvancement(Player player)
        {
            IDictionary data = GetCustomData(player);
            if (data == null)
                return;

            if (data.Contains(AdvancementDataKey))
                data.Remove(AdvancementDataKey);
        }

        private static IDictionary GetCustomData(Player player)
        {
            try
            {
                FieldInfo field = typeof(Player).GetField(
                    "m_customData",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                if (field == null)
                    return null;

                return field.GetValue(player) as IDictionary;
            }
            catch
            {
                return null;
            }
        }

        private static void ShowCenterMessage(string message)
        {
            if (MessageHud.instance != null)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, message);
            }
        }
    }

    public class ShrineVisualPulse : MonoBehaviour
    {
        private Transform _orb;
        private Transform _ringA;
        private Transform _ringB;
        private Light _light;
        private Vector3 _orbBasePosition;

        private void Start()
        {
            _orb = transform.Find("shrine_orb");
            _ringA = transform.Find("shrine_ring_a");
            _ringB = transform.Find("shrine_ring_b");

            Transform lightTransform = transform.Find("shrine_light");
            if (lightTransform != null)
                _light = lightTransform.GetComponent<Light>();

            if (_orb != null)
                _orbBasePosition = _orb.localPosition;
        }

        private void Update()
        {
            float time = Time.time;

            if (_orb != null)
                _orb.localPosition = _orbBasePosition + Vector3.up * (Mathf.Sin(time * 1.9f) * 0.045f);

            if (_ringA != null)
                _ringA.Rotate(0f, 28f * Time.deltaTime, 0f, Space.Self);

            if (_ringB != null)
                _ringB.Rotate(0f, -21f * Time.deltaTime, 0f, Space.Self);

            if (_light != null)
                _light.intensity = 1.25f + Mathf.Sin(time * 2.2f) * 0.23f;
        }
    }

    public class ClassShrineInteraction : MonoBehaviour, Hoverable, Interactable
    {
        public float GetHoverOffset()
        {
            return 1.5f;
        }

        public string GetHoverName()
        {
            return "Altar of Blessings";
        }

        public string GetHoverText()
        {
            string selected = Plugin.GetSelectedClass(Player.m_localPlayer);
            string advancement = Plugin.GetSelectedAdvancement(Player.m_localPlayer);

            if (string.IsNullOrEmpty(selected))
                return "Altar of Blessings\n[<color=yellow><b>$KEY_Use</b></color>] Choose class";

            string path = selected;
            if (!string.IsNullOrEmpty(advancement))
                path += " > " + advancement;

            return "Altar of Blessings\nCurrent path: " + path +
                   "\n[<color=yellow><b>$KEY_Use</b></color>] Open";
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold)
                return false;

            Player player = user as Player;
            if (player == null || player != Player.m_localPlayer)
                return false;

            if (Plugin.Instance != null)
            {
                Plugin.Instance.OpenClassPanel();
                return true;
            }

            return false;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }
    }
}
