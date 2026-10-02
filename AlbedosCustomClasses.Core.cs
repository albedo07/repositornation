using System;
using System.Collections;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using Jotunn.Managers;
using Jotunn.Configs;
using Jotunn.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace AlbedosCustomClasses
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInDependency("com.jotunn.jotunn", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGuid = "albedo.customclasses";
        public const string ModName = "Dragon's Altar";
        public const string ModVersion = "0.17.1";

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
        private Text _baseDetailSkills;
        private Text _advancementPageTitle;
        private Text _advancementBaseSummaryTitle;
        private Text _advancementBaseSummaryBody;
        private Text _advDetailTitle;
        private Text _advDetailRole;
        private Text _advDetailBody;
        private Text _advDetailSkills;
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
            PrefabManager.OnVanillaPrefabsAvailable -= RegisterShrine;

            if (!GUIManager.IsHeadless())
            {
                GUIManager.OnCustomGUIAvailable -= BuildClassPanel;
                GUIManager.BlockInput(false);
            }
        }

        private void Update()
        {
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
                    Name = "Dragon's Altar",
                    Description = "An ancient dragon altar where adventurers choose their class path.",
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

        private void BuildClassPanel()
        {
            if (GUIManager.CustomGUIFront == null)
                return;

            if (_classPanel != null)
            {
                Destroy(_classPanel);
                _classPanel = null;
            }

            _classPanel = GUIManager.Instance.CreateWoodpanel(
                parent: GUIManager.CustomGUIFront.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: Vector2.zero,
                width: 1040f,
                height: 900f,
                draggable: false
            );

            GUIManager.Instance.CreateText(
                text: "DRAGON'S ALTAR",
                parent: _classPanel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0f, 407f),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 31,
                color: GUIManager.Instance.ValheimOrange,
                outline: true,
                outlineColor: Color.black,
                width: 700f,
                height: 42f,
                addContentSizeFitter: false
            );

            GameObject resetObject = GUIManager.Instance.CreateButton(
                text: "Reset",
                parent: _classPanel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(-455f, 407f),
                width: 92f,
                height: 34f
            );
            resetObject.GetComponent<Button>().onClick.AddListener(ResetClassSelection);

            GameObject closeObject = GUIManager.Instance.CreateButton(
                text: "Close",
                parent: _classPanel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(455f, 407f),
                width: 92f,
                height: 34f
            );
            closeObject.GetComponent<Button>().onClick.AddListener(CloseClassPanel);

            GameObject currentClassObject = GUIManager.Instance.CreateText(
                text: "Base class: None",
                parent: _classPanel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(-205f, 362f),
                font: GUIManager.Instance.AveriaSerif,
                fontSize: 16,
                color: Color.white,
                outline: true,
                outlineColor: Color.black,
                width: 370f,
                height: 28f,
                addContentSizeFitter: false
            );
            _currentClassText = currentClassObject.GetComponent<Text>();

            GameObject currentAdvancementObject = GUIManager.Instance.CreateText(
                text: "Advancement: None",
                parent: _classPanel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(205f, 362f),
                font: GUIManager.Instance.AveriaSerif,
                fontSize: 16,
                color: Color.white,
                outline: true,
                outlineColor: Color.black,
                width: 370f,
                height: 28f,
                addContentSizeFitter: false
            );
            _currentAdvancementText = currentAdvancementObject.GetComponent<Text>();

            BuildBaseClassPage();
            BuildAdvancementPage();
            BuildConfirmationPanel();

            ShowBaseClassPage();
            _classPanel.SetActive(false);
        }

        private GameObject CreateUiGroup(string name)
        {
            GameObject group = new GameObject(name);
            group.transform.SetParent(_classPanel.transform, false);

            RectTransform rect = group.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -18f);
            rect.sizeDelta = new Vector2(980f, 710f);
            return group;
        }

        private GameObject CreateDarkPanel(Transform parent, Vector2 position, Vector2 size, Color accent)
        {
            GameObject panel = new GameObject("DragonAltarDetailPanel");
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = panel.AddComponent<Image>();
            image.color = new Color(0.025f, 0.025f, 0.035f, 0.70f);

            Outline outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color(accent.r, accent.g, accent.b, 0.55f);
            outline.effectDistance = new Vector2(1f, -1f);
            return panel;
        }

        private Text CreateWrappedText(Transform parent, string text, Vector2 position, float width, float height, int fontSize, Color color, bool bold, TextAnchor alignment)
        {
            GameObject obj = GUIManager.Instance.CreateText(
                text: text,
                parent: parent,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: position,
                font: bold ? GUIManager.Instance.AveriaSerifBold : GUIManager.Instance.AveriaSerif,
                fontSize: fontSize,
                color: color,
                outline: true,
                outlineColor: Color.black,
                width: width,
                height: height,
                addContentSizeFitter: false
            );

            Text label = obj.GetComponent<Text>();
            if (label != null)
            {
                label.alignment = alignment;
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Truncate;
                label.supportRichText = true;
            }
            return label;
        }

        private void BuildBaseClassPage()
        {
            _baseClassPage = CreateUiGroup("DragonAltarBaseClassPage");

            CreateWrappedText(
                _baseClassPage.transform,
                "BASE CLASSES",
                new Vector2(-285f, 302f),
                330f,
                34f,
                22,
                GUIManager.Instance.ValheimOrange,
                true,
                TextAnchor.MiddleCenter
            );

            CreateBaseClassNavRow("Warrior", "Front-line physical bruiser", 170f, new Color(1f, 0.63f, 0.28f, 1f));
            CreateBaseClassNavRow("Cleric", "Holy hybrid support", 25f, new Color(0.55f, 0.86f, 1f, 1f));
            CreateBaseClassNavRow("Sorcerer", "Eitr-first magic specialist", -120f, new Color(0.78f, 0.52f, 1f, 1f));

            CreateDarkPanel(_baseClassPage.transform, new Vector2(190f, -5f), new Vector2(535f, 585f), new Color(1f, 0.60f, 0.24f, 1f));
            _baseDetailTitle = CreateWrappedText(_baseClassPage.transform, "WARRIOR", new Vector2(190f, 250f), 390f, 36f, 23, Color.white, true, TextAnchor.MiddleCenter);
            _baseDetailRole = CreateWrappedText(_baseClassPage.transform, "FRONT-LINE PHYSICAL BRUISER", new Vector2(190f, 214f), 390f, 28f, 14, new Color(0.90f, 0.78f, 0.58f, 1f), true, TextAnchor.MiddleCenter);
            _baseDetailBody = CreateWrappedText(_baseClassPage.transform, "", new Vector2(190f, 45f), 475f, 285f, 13, new Color(0.94f, 0.94f, 0.94f, 1f), false, TextAnchor.UpperLeft);
            _baseDetailSkills = CreateWrappedText(_baseClassPage.transform, "", new Vector2(190f, -152f), 475f, 95f, 13, new Color(0.84f, 0.90f, 1f, 1f), false, TextAnchor.UpperLeft);

            GameObject choose = GUIManager.Instance.CreateButton(
                text: "Choose Warrior",
                parent: _baseClassPage.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(85f, -265f),
                width: 210f,
                height: 42f
            );
            _chooseBaseButtonText = choose.GetComponentInChildren<Text>();
            choose.GetComponent<Button>().onClick.AddListener(RequestBaseClassConfirmation);

            GameObject view = GUIManager.Instance.CreateButton(
                text: "View Advancements",
                parent: _baseClassPage.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(300f, -265f),
                width: 210f,
                height: 42f
            );
            view.GetComponent<Button>().onClick.AddListener(OpenFocusedAdvancements);
        }

        private void CreateBaseClassNavRow(string className, string role, float y, Color accent)
        {
            CreateIconPlaceholder(_baseClassPage.transform, new Vector2(-430f, y + 10f), 58f, accent);

            GameObject button = GUIManager.Instance.CreateButton(
                text: className.ToUpper(),
                parent: _baseClassPage.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(-295f, y + 18f),
                width: 225f,
                height: 46f
            );
            button.GetComponent<Button>().onClick.AddListener(delegate { FocusBaseClass(className); });

            CreateWrappedText(
                _baseClassPage.transform,
                role,
                new Vector2(-295f, y - 23f),
                245f,
                28f,
                13,
                accent,
                true,
                TextAnchor.MiddleCenter
            );
        }

        private void BuildAdvancementPage()
        {
            _advancementPage = CreateUiGroup("DragonAltarAdvancementPage");

            GameObject back = GUIManager.Instance.CreateButton(
                text: "< Base Classes",
                parent: _advancementPage.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(-385f, 302f),
                width: 180f,
                height: 36f
            );
            back.GetComponent<Button>().onClick.AddListener(ShowBaseClassPage);

            _advancementBaseSummaryTitle = CreateWrappedText(
                _advancementPage.transform,
                "WARRIOR - FRONT-LINE PHYSICAL BRUISER",
                new Vector2(120f, 306f),
                650f,
                28f,
                16,
                GUIManager.Instance.ValheimOrange,
                true,
                TextAnchor.MiddleCenter
            );

            _advancementBaseSummaryBody = CreateWrappedText(
                _advancementPage.transform,
                "Heavy melee foundation built around Hyper Armor, Broken Bones and direct frontline pressure.",
                new Vector2(120f, 278f),
                650f,
                34f,
                12,
                new Color(0.88f, 0.88f, 0.88f, 1f),
                false,
                TextAnchor.MiddleCenter
            );

            _advancementPageTitle = CreateWrappedText(
                _advancementPage.transform,
                "CHOOSE AN ADVANCEMENT",
                new Vector2(-290f, 220f),
                330f,
                38f,
                20,
                GUIManager.Instance.ValheimOrange,
                true,
                TextAnchor.MiddleCenter
            );

            CreateDarkPanel(_advancementPage.transform, new Vector2(190f, -5f), new Vector2(535f, 585f), new Color(0.86f, 0.62f, 0.30f, 1f));

            _advDetailTitle = CreateWrappedText(_advancementPage.transform, "SWORD MASTER", new Vector2(190f, 250f), 420f, 36f, 23, Color.white, true, TextAnchor.MiddleCenter);
            _advDetailRole = CreateWrappedText(_advancementPage.transform, "FAST SWORD SPECIALIST", new Vector2(190f, 214f), 420f, 28f, 14, new Color(0.84f, 0.90f, 1f, 1f), true, TextAnchor.MiddleCenter);
            _advDetailBody = CreateWrappedText(_advancementPage.transform, "", new Vector2(190f, 45f), 475f, 285f, 13, new Color(0.94f, 0.94f, 0.94f, 1f), false, TextAnchor.UpperLeft);
            _advDetailSkills = CreateWrappedText(_advancementPage.transform, "", new Vector2(190f, -152f), 475f, 95f, 13, new Color(0.84f, 0.90f, 1f, 1f), false, TextAnchor.UpperLeft);

            GameObject choose = GUIManager.Instance.CreateButton(
                text: "Choose Sword Master",
                parent: _advancementPage.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(190f, -265f),
                width: 270f,
                height: 42f
            );
            _chooseAdvButton = choose.GetComponent<Button>();
            _chooseAdvButtonText = choose.GetComponentInChildren<Text>();
            _chooseAdvButton.onClick.AddListener(RequestAdvancementConfirmation);
        }

        private void CreateAdvancementNavRow(Transform parent, string advancementName, string role, float y, Color accent)
        {
            CreateIconPlaceholder(parent, new Vector2(-430f, y + 10f), 58f, accent);

            GameObject button = GUIManager.Instance.CreateButton(
                text: advancementName.ToUpper(),
                parent: parent,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(-295f, y + 18f),
                width: 225f,
                height: 46f
            );
            button.GetComponent<Button>().onClick.AddListener(delegate { FocusAdvancement(advancementName); });

            CreateWrappedText(
                parent,
                role,
                new Vector2(-295f, y - 23f),
                245f,
                28f,
                13,
                accent,
                true,
                TextAnchor.MiddleCenter
            );
        }

        private void RebuildAdvancementNavigation()
        {
            Transform old = _advancementPage.transform.Find("AdvancementNavigation");
            if (old != null)
                Destroy(old.gameObject);

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
                CreateAdvancementNavRow(nav.transform, "Sword Master", "Speed + spirit swordplay", 80f, new Color(0.72f, 0.86f, 1f, 1f));
                CreateAdvancementNavRow(nav.transform, "Mercenary", "Control + brutal AoE", -75f, new Color(1f, 0.72f, 0.35f, 1f));
            }
            else if (_advancementParent == "Cleric")
            {
                CreateAdvancementNavRow(nav.transform, "Paladin", "Holy impact + resilience", 80f, new Color(1f, 0.87f, 0.42f, 1f));
                CreateAdvancementNavRow(nav.transform, "Priest", "Relics + team support", -75f, new Color(0.60f, 0.91f, 1f, 1f));
            }
            else
            {
                CreateAdvancementNavRow(nav.transform, "Wizard", "Charged large-scale magic", 80f, new Color(0.78f, 0.42f, 1f, 1f));
                CreateAdvancementNavRow(nav.transform, "Spellcaster", "Rapid mobile spatial magic", -75f, new Color(0.92f, 0.50f, 1f, 1f));
            }
        }

        private void BuildConfirmationPanel()
        {
            _confirmationPanel = CreateDarkPanel(_classPanel.transform, Vector2.zero, new Vector2(570f, 300f), new Color(1f, 0.64f, 0.28f, 1f));
            _confirmationPanel.name = "DragonAltarConfirmation";

            CreateWrappedText(
                _confirmationPanel.transform,
                "CONFIRM YOUR CHOICE",
                new Vector2(0f, 105f),
                500f,
                38f,
                22,
                GUIManager.Instance.ValheimOrange,
                true,
                TextAnchor.MiddleCenter
            );

            _confirmationText = CreateWrappedText(
                _confirmationPanel.transform,
                "",
                new Vector2(0f, 20f),
                490f,
                125f,
                15,
                Color.white,
                false,
                TextAnchor.MiddleCenter
            );

            GameObject confirm = GUIManager.Instance.CreateButton(
                text: "Yes, Confirm",
                parent: _confirmationPanel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(-115f, -100f),
                width: 205f,
                height: 42f
            );
            confirm.GetComponent<Button>().onClick.AddListener(ConfirmPendingSelection);

            GameObject cancel = GUIManager.Instance.CreateButton(
                text: "Cancel",
                parent: _confirmationPanel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(115f, -100f),
                width: 205f,
                height: 42f
            );
            cancel.GetComponent<Button>().onClick.AddListener(CancelConfirmation);

            _confirmationPanel.SetActive(false);
        }

        private void CreateIconPlaceholder(Transform parent, Vector2 position, float size, Color accent)
        {
            GameObject icon = new GameObject("DragonAltarIconPlaceholder");
            icon.transform.SetParent(parent, false);

            RectTransform rect = icon.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(size, size);

            Image image = icon.AddComponent<Image>();
            image.color = new Color(0.025f, 0.025f, 0.035f, 0.82f);

            Outline outline = icon.AddComponent<Outline>();
            outline.effectColor = new Color(accent.r, accent.g, accent.b, 0.92f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        private void FocusBaseClass(string className)
        {
            _focusedBaseClass = className;
            UpdateBaseClassDetail();
        }

        private void UpdateBaseClassDetail()
        {
            string role = GetBaseClassRole(_focusedBaseClass);
            Color accent = GetBaseClassAccent(_focusedBaseClass);

            if (_baseDetailTitle != null)
            {
                _baseDetailTitle.text = _focusedBaseClass.ToUpper();
                _baseDetailTitle.color = accent;
            }
            if (_baseDetailRole != null)
            {
                _baseDetailRole.text = role.ToUpper();
                _baseDetailRole.color = accent;
            }
            if (_baseDetailBody != null)
                _baseDetailBody.text = GetBaseClassDescription(_focusedBaseClass);
            if (_baseDetailSkills != null)
                _baseDetailSkills.text = GetBaseClassSkills(_focusedBaseClass);
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
            if (_confirmationPanel != null)
                _confirmationPanel.SetActive(false);
            if (_baseClassPage != null)
                _baseClassPage.SetActive(false);
            if (_advancementPage != null)
                _advancementPage.SetActive(true);

            if (_advancementPageTitle != null)
                _advancementPageTitle.text = "CHOOSE AN ADVANCEMENT";
            if (_advancementBaseSummaryTitle != null)
                _advancementBaseSummaryTitle.text = _advancementParent.ToUpper() + "  -  " + GetBaseClassRole(_advancementParent).ToUpper();
            if (_advancementBaseSummaryBody != null)
                _advancementBaseSummaryBody.text = GetBaseClassSummary(_advancementParent);

            RebuildAdvancementNavigation();
            FocusAdvancement(_focusedAdvancement);
        }

        private void FocusAdvancement(string advancementName)
        {
            _focusedAdvancement = advancementName;
            Color accent = GetAdvancementAccent(advancementName);

            if (_advDetailTitle != null)
            {
                _advDetailTitle.text = advancementName.ToUpper();
                _advDetailTitle.color = accent;
            }
            if (_advDetailRole != null)
            {
                _advDetailRole.text = GetAdvancementRole(advancementName).ToUpper();
                _advDetailRole.color = accent;
            }
            if (_advDetailBody != null)
                _advDetailBody.text = GetAdvancementDescription(advancementName);
            if (_advDetailSkills != null)
                _advDetailSkills.text = GetAdvancementSkills(advancementName);

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
                    "Advance from <b>" + _advancementParent + "</b> to <b>" + _focusedAdvancement + "</b>?\n\n" +
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
            return "Eitr-first magic specialist";
        }

        private Color GetBaseClassAccent(string className)
        {
            if (className == "Warrior") return new Color(1f, 0.63f, 0.28f, 1f);
            if (className == "Cleric") return new Color(0.55f, 0.86f, 1f, 1f);
            return new Color(0.78f, 0.52f, 1f, 1f);
        }

        private string GetBaseClassSummary(string className)
        {
            if (className == "Warrior")
                return "Heavy melee foundation built around Hyper Armor, Broken Bones and direct frontline pressure.";
            if (className == "Cleric")
                return "Holy battlemage foundation mixing Lightning pressure, healing and flexible divine equipment.";
            return "Magic-first foundation built around Eitr management, elemental pressure and Staff/Wand combat.";
        }

        private string GetFirstAdvancement(string className)
        {
            if (className == "Warrior") return "Sword Master";
            if (className == "Cleric") return "Paladin";
            return "Wizard";
        }

        private Color GetAdvancementAccent(string advancementName)
        {
            if (advancementName == "Sword Master") return new Color(0.72f, 0.86f, 1f, 1f);
            if (advancementName == "Mercenary") return new Color(1f, 0.72f, 0.35f, 1f);
            if (advancementName == "Paladin") return new Color(1f, 0.87f, 0.42f, 1f);
            if (advancementName == "Priest") return new Color(0.60f, 0.91f, 1f, 1f);
            if (advancementName == "Wizard") return new Color(0.78f, 0.42f, 1f, 1f);
            return new Color(0.92f, 0.50f, 1f, 1f);
        }

        private string GetAdvancementRole(string advancementName)
        {
            if (advancementName == "Sword Master") return "Fast sword specialist";
            if (advancementName == "Mercenary") return "Control and brutal AoE pressure";
            if (advancementName == "Paladin") return "Holy impact and resilient offense";
            if (advancementName == "Priest") return "Relics, healing and team support";
            if (advancementName == "Wizard") return "Charged large-scale magic";
            return "Rapid mobile spatial magic";
        }

        private string GetBaseClassDescription(string className)
        {
            if (className == "Warrior")
            {
                return "<b>IDENTITY</b>\nWarrior is the direct melee front-liner: simple to understand, difficult to bully, and built to stay close while forcing enemies to respect physical pressure.\n\n<b>CORE MECHANICS</b>\nHyper Armor protects the Warrior from interruption by ordinary hits below the class threshold. Heavy Slash punishes with Broken Bones, Impact Wave controls a line in front of you, and Impact Punch gives fast close-range Blunt pressure plus Small-enemy Stun.\n\n<b>PLAYSTYLE</b>\nBest for players who want to commit to melee, trade confidently, control space with physical attacks, and later specialize into speed or overwhelming AoE.";
            }
            if (className == "Cleric")
            {
                return "<b>IDENTITY</b>\nCleric mixes holy offense with healing and setup. It can contribute meaningful damage without giving up the ability to rescue itself or a party.\n\n<b>CORE MECHANICS</b>\nShield Weapon Mastery multiplies Block Force and Block Power of every Shield by 1.5x. Divine Duality allows a Staff and Shield to be equipped together. Lightning Zap gives immediate cone pressure, Righteous Strike calls lightning onto a physical target and applies Expose, and Holy Wave restores health around the caster.\n\n<b>PLAYSTYLE</b>\nBest for players who like flexible roles, strong defense, Staff + Shield magic, lightning and holy effects, and having answers for both damage and recovery.";
            }
            return "<b>IDENTITY</b>\nSorcerer is the magic-first base class. Its power comes from Eitr management, magical damage and large spell effects rather than conventional weapon damage.\n\n<b>CORE MECHANICS</b>\nArcane Blood grants +30% Eitr Regen, +40 flat Max Eitr and +30% Magic Damage to Eitr-based attacks and skills. Flame Burst supplies fast Fire pressure, Glacial Descent gives a large Frost impact, and Stonefang Eruption controls the ground with Blunt/Pierce damage, Stun and Cripple. The planned emergency Arcane Blood teleport remains intentionally unimplemented until its safe-destination rules are designed.\n\n<b>PLAYSTYLE</b>\nBest for players who want spell rotations, resource management, ranged control and spectacular magic, then specialize into deliberate charged casting or extremely mobile rapid casting.";
        }

        private string GetBaseClassSkills(string className)
        {
            if (className == "Warrior") return "<b>STARTER KIT</b>  Heavy Slash  |  Impact Wave  |  Impact Punch\n<b>ADVANCEMENTS</b>  Sword Master  |  Mercenary";
            if (className == "Cleric") return "<b>STARTER KIT</b>  Lightning Zap  |  Righteous Strike  |  Holy Wave\n<b>ADVANCEMENTS</b>  Paladin  |  Priest";
            return "<b>STARTER KIT</b>  Flame Burst  |  Glacial Descent  |  Stonefang Eruption\n<b>ADVANCEMENTS</b>  Wizard  |  Spellcaster";
        }

        private string GetAdvancementDescription(string advancementName)
        {
            if (advancementName == "Sword Master")
            {
                return "<b>IDENTITY</b>\nSword Master turns Warrior into a high-tempo sword specialist built around fast attacks, broad spirit slashes and precise burst windows.\n\n<b>MECHANICS</b>\nMoonlight Splitter fires three extra-wide Ghost slashes across the full 50m. Crescent Cleave spreads five large ground cleaves across a very wide 120° cone for 20m. Judgement Cut remains the four-charge spatial slash sphere for now. The Way of the Sword grants +100% Attack Speed only while exactly one Sword is equipped with an empty off-hand; the old activatable Sword Mastery burst is retired.\n\n<b>BEST FOR</b>\nAggressive players who want flashy sword pressure, ranged melee, charge management and a fast combo rhythm rather than tanking through everything.";
            }
            if (advancementName == "Mercenary")
            {
                return "<b>IDENTITY</b>\nMercenary is the brutal battlefield controller: heavy physical attacks, violent displacement and huge close-range pressure.\n\n<b>MECHANICS</b>\nWarfreak allows any two one-handed weapons to be equipped together and grants +125% Attack Speed with two-handed weapons; dedicated weapon-combination animations come later. Stomp, Bonecrusher, Circle Swing, Seismic Guillotine, Reaver's Orbit and Unchained Fury keep their current crowd-control behavior.\n\n<b>BEST FOR</b>\nPlayers who enjoy crowd control, displacement, huge AoEs, mixed-weapon brutality and chaotic close-range pressure.";
            }
            if (advancementName == "Paladin")
            {
                return "<b>IDENTITY</b>\nPaladin is Cleric's durable battle branch: a holy bruiser that can specialize toward magic or weapon-and-shield pressure.\n\n<b>MECHANICS</b>\nChoose one locked passive. Elemental Savant grants +25% elemental damage, +30 flat Eitr and +30% Eitr Regen. Holy Knight grants +25% Movement Speed, +35 HP, +35 Stamina, +30% HP/Stamina Regen and +75% Attack Speed while any weapon is paired with any Shield. Existing Paladin skills remain unchanged in this foundation pass.\n\n<b>BEST FOR</b>\nPlayers who want either durable elemental casting or aggressive weapon-and-shield combat.";
            }
            if (advancementName == "Priest")
            {
                return "<b>IDENTITY</b>\nPriest is the tactical Cross-field support branch. Lightning Relic and Holy Relic establish battlefield anchors. Divine Intervention and Heaven's Judgement can self-cast or Cross Cast from an aimed active Cross; Grand Cross is deliberately self-cast only.\n\n<b>MECHANICS</b>\nGrand Sigil now adds +30% of current Armor. Its Priest death-save retains the 1 HP + 50% recovery behavior on a 20-minute cooldown; allies within 20m of a living Priest can receive the same save on their own 35-minute cooldown. The activatable barrier remains.\n\n<b>BEST FOR</b>\nPlayers who want battlefield anchors, ranged support placement and a support identity built around staying alive long enough to keep everyone else alive.";
            }
            if (advancementName == "Wizard")
            {
                return "<b>IDENTITY</b>\nWizard is the deliberate heavy-artillery caster: huge committed spells, Staff charge windows and overwhelming single releases.\n\n<b>MECHANICS</b>\nWizard Staff Weapon Mastery grants +20% additional Eitr Regen and +30 flat Max Eitr only while a normal Staff is actually held. The existing 3-stack Charged Staff behavior stays unchanged. Overcharge remains the Wizard passive.\n\n<b>BEST FOR</b>\nPlayers who want magical artillery, giant telegraphed attacks and the satisfaction of charging one disgusting hit instead of spraying dozens of smaller ones.";
            }
            return "<b>IDENTITY</b>\nSpellcaster is the Rambo-style mobile gunmage: sprint-casting, rapid Staff pressure, dual Gun Staves and spatial tricks everywhere.\n\n<b>MECHANICS</b>\nSingle Staff/Wand use has +50% Attack Speed; dual Gun Staves have +100% Attack Speed, so a 0.50s baseline cadence targets 0.25s when dual-wielded. The artificial firing movement bonus is removed: firing simply preserves the player's real current walk/run/sprint speed. -50% Eitr use, +20% Eitr Regen, 50% reduced normal magic-weapon damage, zero Gun Staff spread and warm-up removal remain.\n\n<b>BEST FOR</b>\nPlayers who want constant motion, absurd magical gunfire, teleport pressure and a deliberately lower per-shot damage ceiling in exchange for attacking from everywhere at once.";
        }

        private string GetAdvancementSkills(string advancementName)
        {
            if (advancementName == "Sword Master") return "<b>SKILLS</b>  Moonlight Splitter  |  Crescent Cleave  |  Judgement Cut  |  Halfmoon Slash (Ultimate)\n<b>PASSIVE</b>  The Way of the Sword";
            if (advancementName == "Mercenary") return "<b>SKILLS</b>  Stomp  |  Bonecrusher  |  Circle Swing  |  Seismic Guillotine  |  Reaver's Orbit  |  Whirlwind (Ultimate)\n<b>PASSIVE</b>  Barbaric / Unchained Fury  |  <b>MASTERY</b> Warfreak";
            if (advancementName == "Paladin") return "<b>SKILLS</b>  Goddess Relic  |  Ray of Hope  |  Shield Charge  |  Electric Smite (Ultimate)\n<b>PASSIVE CHOICE</b>  Elemental Savant  |  Holy Knight";
            if (advancementName == "Priest") return "<b>SKILLS</b>  Lightning Relic  |  Holy Relic  |  Divine Intervention  |  Grand Cross  |  Heaven's Judgement  |  Lightning Tempest (Ultimate)\n<b>PASSIVE</b>  Grand Sigil";
            if (advancementName == "Wizard") return "<b>SKILLS</b>  Gravity Dominion  |  Astral Greatblade  |  Frost Nova  |  Meteor Fall  |  Astral Railcannon  |  Elemental Cataclysm (Ultimate)\n<b>PASSIVE</b>  Overcharge + 3-stack Staff Charge";
            return "<b>SKILLS</b>  Rift Echo  |  Void Step  |  Arcane Phalanx  |  Afterimage Arsenal  |  Arcane Rupture (Ultimate)\n<b>PASSIVE</b>  Riftwalker / Phase Flow + dual Gun Staff mastery";
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
                _currentAdvancementText.text = "Advancement: " + (string.IsNullOrEmpty(selectedAdvancement) ? "None" : selectedAdvancement);

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
                _currentAdvancementText.text = "Advancement: " + advancementName;

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
            return "Dragon's Altar";
        }

        public string GetHoverText()
        {
            string selected = Plugin.GetSelectedClass(Player.m_localPlayer);
            string advancement = Plugin.GetSelectedAdvancement(Player.m_localPlayer);

            if (string.IsNullOrEmpty(selected))
                return "Dragon's Altar\n[<color=yellow><b>$KEY_Use</b></color>] Choose class";

            string path = selected;
            if (!string.IsNullOrEmpty(advancement))
                path += " > " + advancement;

            return "Dragon's Altar\nCurrent path: " + path +
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
