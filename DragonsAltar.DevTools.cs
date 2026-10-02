using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace DragonsAltarDevTools
{
    internal class DevSetting
    {
        public string Section;
        public string Key;
        public string Id;
        public string Tab;
        public string Description;
        public Type SettingType;
        public float SliderMin;
        public float SliderMax;
        // The same Section/Key can exist in several Immortal Heroes modules (e.g. [Testing]).
        // Every copy is edited together so the modules never disagree.
        public readonly List<ConfigEntryBase> Entries = new List<ConfigEntryBase>();
        public readonly List<ConfigFile> Files = new List<ConfigFile>();

        public ConfigEntryBase Main { get { return Entries[0]; } }
        public bool IsNumber { get { return SettingType == typeof(float) || SettingType == typeof(int); } }
    }

    // v0.18.0: the in-game Config window. Every setting of every Immortal Heroes module
    // (numbers, toggles, text, keys, choices) is editable here and saved to the .cfg files,
    // so the cfg never has to be opened by hand.
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInDependency("albedo.customclasses", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("albedo.customclasses.combatruntime", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("albedo.customclasses.skills", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("albedo.customclasses.advanced", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("albedo.customclasses.sorcerer", BepInDependency.DependencyFlags.HardDependency)]
    public class DeveloperToolsPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "albedo.customclasses.devtools";
        public const string ModName = "Dragon's Altar - Developer Tools";
        public const string ModVersion = "0.18.1";

        public static DeveloperToolsPlugin Instance;

        public static bool IsDeveloperPanelOpen
        {
            get { return Instance != null && Instance._open; }
        }

        private static readonly string[] Tabs = { "Paladin & Cleric", "Other Skills", "Testing", "Keys & Hotbar", "General" };
        private static readonly string[] AscendableSkills =
            { "righteous_strike", "goddess_relic", "judgement_hammer", "shield_charge", "fallen_angel", "ray_of_hope", "electric_smite" };

        private ConfigEntry<KeyCode> _toggleKey;
        private ConfigEntry<bool> _enableWorldPreview;
        private ConfigEntry<bool> _autoSave;

        private readonly List<DevSetting> _settings = new List<DevSetting>();
        private readonly List<string> _sections = new List<string>();
        private readonly Dictionary<string, string> _inputBuffers = new Dictionary<string, string>();
        private readonly List<GameObject> _previewObjects = new List<GameObject>();
        private readonly HashSet<ConfigFile> _dirtyFiles = new HashSet<ConfigFile>();
        private static KeyCode[] _allKeys;

        private Rect _windowRect = new Rect(55f, 55f, 1120f, 700f);
        private Vector2 _sectionScroll;
        private Vector2 _settingScroll;
        private string _selectedTab = "Paladin & Cleric";
        private string _selectedSection = string.Empty;
        private string _selectedSettingId = string.Empty;
        private string _search = string.Empty;
        private string _status = string.Empty;
        private float _statusUntil;
        private float _lastChangeTime;
        private DevSetting _captureSetting;
        private int _captureFrame;
        private bool _open;
        private bool _preview;
        private bool _savedCursorVisible;
        private CursorLockMode _savedCursorLock;
        private GUIStyle _titleStyle;
        private GUIStyle _smallStyle;
        private GUIStyle _descStyle;
        private GUIStyle _selectedButtonStyle;
        private GUIStyle _sectionButtonStyle;
        private GUIStyle _tabStyle;
        private GUIStyle _tabSelectedStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _onStyle;
        private GUIStyle _offStyle;
        private Texture2D _panelTexture;
        private Texture2D _selectedTexture;
        private Texture2D _buttonTexture;
        private Texture2D _onTexture;

        private void Awake()
        {
            Instance = this;
            _toggleKey = Config.Bind("Developer UI", "ToggleKey", KeyCode.F8, "Open or close the Immortal Heroes Config window.");
            _enableWorldPreview = Config.Bind("Developer UI", "EnableWorldPreview", true, "Allow the selected range/radius setting to draw an in-world ruler preview.");
            _autoSave = Config.Bind("Developer UI", "AutoSave", true, "Save changes made in the Config window to the .cfg files automatically.");
            _preview = _enableWorldPreview.Value;
            Logger.LogInfo(ModName + " v" + ModVersion + " loaded. Press " + _toggleKey.Value.ToString() + " for the Config window.");
        }

        private void OnDestroy()
        {
            SaveDirty();
            ClosePanel();
            ClearPreview();
            Instance = null;
        }

        private void Update()
        {
            if (_captureSetting != null)
            {
                UpdateKeyCapture();
                return;
            }

            if (Input.GetKeyDown(_toggleKey.Value))
            {
                if (_open)
                    ClosePanel();
                else
                    OpenPanel();
            }

            if (_open && Input.GetKeyDown(KeyCode.Escape))
            {
                ClosePanel();
                return;
            }

            if (_dirtyFiles.Count > 0 && _autoSave.Value && Time.unscaledTime - _lastChangeTime > 0.75f)
                SaveDirty();

            if (_open && _preview && _enableWorldPreview.Value)
                UpdateWorldPreview();
            else
                ClearPreview();
        }

        private void OpenPanel()
        {
            RefreshSettings();
            _open = true;
            _savedCursorVisible = Cursor.visible;
            _savedCursorLock = Cursor.lockState;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            SetJotunnInputBlock(true);

            if (_windowRect.x < 0f || _windowRect.x > Screen.width - 150f)
                _windowRect.x = 40f;
            if (_windowRect.y < 0f || _windowRect.y > Screen.height - 100f)
                _windowRect.y = 40f;
        }

        private void ClosePanel()
        {
            if (!_open)
                return;

            _open = false;
            _captureSetting = null;
            if (_autoSave.Value)
                SaveDirty();
            SetJotunnInputBlock(false);
            Cursor.visible = _savedCursorVisible;
            Cursor.lockState = _savedCursorLock;
        }

        private void SetJotunnInputBlock(bool blocked)
        {
            try
            {
                Type type = FindLoadedType("Jotunn.Managers.GUIManager");
                if (type == null)
                    return;

                System.Reflection.MethodInfo method = type.GetMethod(
                    "BlockInput",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic
                );

                if (method != null)
                    method.Invoke(null, new object[] { blocked });
            }
            catch
            {
            }
        }

        private Type FindLoadedType(string fullName)
        {
            System.Reflection.Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                try
                {
                    Type type = assemblies[i].GetType(fullName, false);
                    if (type != null)
                        return type;
                }
                catch
                {
                }
            }
            return null;
        }

        // ------------------------------------------------------------------ settings model
        private static bool IsSupportedType(Type type)
        {
            return type == typeof(float) || type == typeof(int) || type == typeof(bool) || type == typeof(string) || (type != null && type.IsEnum);
        }

        private void RefreshSettings()
        {
            _settings.Clear();
            _inputBuffers.Clear();
            Dictionary<string, DevSetting> byKey = new Dictionary<string, DevSetting>();

            UnityEngine.Object[] plugins = UnityEngine.Object.FindObjectsOfType(typeof(BaseUnityPlugin));
            for (int i = 0; i < plugins.Length; i++)
            {
                BaseUnityPlugin plugin = plugins[i] as BaseUnityPlugin;
                if (plugin == null)
                    continue;

                string guid = string.Empty;
                try
                {
                    if (plugin.Info != null && plugin.Info.Metadata != null)
                        guid = plugin.Info.Metadata.GUID;
                }
                catch
                {
                }
                if (plugin == this)
                    guid = ModGuid;

                if (string.IsNullOrEmpty(guid) || !guid.StartsWith("albedo.customclasses", StringComparison.OrdinalIgnoreCase))
                    continue;

                IDictionary<ConfigDefinition, ConfigEntryBase> entries = plugin.Config as IDictionary<ConfigDefinition, ConfigEntryBase>;
                if (entries == null)
                    continue;

                foreach (KeyValuePair<ConfigDefinition, ConfigEntryBase> pair in entries)
                {
                    ConfigEntryBase entry = pair.Value;
                    if (entry == null || !IsSupportedType(entry.SettingType))
                        continue;

                    string section = entry.Definition.Section;
                    string key = entry.Definition.Key;
                    string unique = section + "|" + key;
                    DevSetting setting;
                    if (!byKey.TryGetValue(unique, out setting))
                    {
                        setting = new DevSetting();
                        setting.Section = section;
                        setting.Key = key;
                        setting.Id = unique;
                        setting.SettingType = entry.SettingType;
                        setting.Tab = TabFor(section, entry.SettingType);
                        try
                        {
                            setting.Description = entry.Description == null ? "" : entry.Description.Description;
                        }
                        catch
                        {
                            setting.Description = "";
                        }
                        GetSliderBounds(entry, key, out setting.SliderMin, out setting.SliderMax);
                        byKey.Add(unique, setting);
                        _settings.Add(setting);
                    }
                    if (entry.SettingType == setting.SettingType)
                    {
                        setting.Entries.Add(entry);
                        setting.Files.Add(plugin.Config);
                    }
                }
            }

            _settings.Sort(delegate(DevSetting a, DevSetting b)
            {
                int sectionCompare = string.Compare(a.Section, b.Section, StringComparison.OrdinalIgnoreCase);
                if (sectionCompare != 0)
                    return sectionCompare;
                return string.Compare(a.Key, b.Key, StringComparison.OrdinalIgnoreCase);
            });
            RebuildSections();
        }

        private void RebuildSections()
        {
            _sections.Clear();
            string search = (_search ?? "").Trim().ToLowerInvariant();
            for (int i = 0; i < _settings.Count; i++)
            {
                DevSetting s = _settings[i];
                if (!MatchesFilter(s, search))
                    continue;
                if (!_sections.Contains(s.Section))
                    _sections.Add(s.Section);
            }
            if (_sections.Count > 0 && !_sections.Contains(_selectedSection))
                _selectedSection = _sections[0];
            EnsureSelectedSetting();
        }

        private bool MatchesFilter(DevSetting s, string search)
        {
            if (search.Length > 0)
                return s.Section.ToLowerInvariant().Contains(search) || s.Key.ToLowerInvariant().Contains(search);
            return s.Tab == _selectedTab;
        }

        private static string TabFor(string section, Type type)
        {
            if (section == "Testing" || section == "Progression" || section == "Developer UI")
                return "Testing";
            if (section == "Hotkeys" || section == "Hotbar" || type == typeof(KeyCode))
                return "Keys & Hotbar";
            if (section.StartsWith("Paladin", StringComparison.OrdinalIgnoreCase) || section.StartsWith("Cleric", StringComparison.OrdinalIgnoreCase))
                return "Paladin & Cleric";
            if (section.StartsWith("Warrior", StringComparison.OrdinalIgnoreCase) ||
                section.StartsWith("Sword Master", StringComparison.OrdinalIgnoreCase) ||
                section.StartsWith("Mercenary", StringComparison.OrdinalIgnoreCase) ||
                section.StartsWith("Priest", StringComparison.OrdinalIgnoreCase) ||
                section.StartsWith("Sorcerer", StringComparison.OrdinalIgnoreCase) ||
                section.StartsWith("Wizard", StringComparison.OrdinalIgnoreCase) ||
                section.StartsWith("Spellcaster", StringComparison.OrdinalIgnoreCase) ||
                section == "Grand Sigil Survival" ||
                section == "Acrobatic Jump Skills")
                return "Other Skills";
            return "General";
        }

        private static bool IsDamageKey(string lower)
        {
            return lower == "blunt" || lower == "slash" || lower == "pierce" || lower == "fire" || lower == "frost" ||
                   lower == "lightning" || lower == "poison" || lower == "spirit" || lower.Contains("damage") || lower.Contains("dotpersecond");
        }

        private void GetSliderBounds(ConfigEntryBase entry, string key, out float min, out float max)
        {
            string lower = key.ToLowerInvariant();
            min = 0f;
            max = 100f;

            try
            {
                AcceptableValueRange<float> fr = entry.Description == null ? null : entry.Description.AcceptableValues as AcceptableValueRange<float>;
                if (fr != null) { min = fr.MinValue; max = fr.MaxValue; return; }
                AcceptableValueRange<int> ir = entry.Description == null ? null : entry.Description.AcceptableValues as AcceptableValueRange<int>;
                if (ir != null) { min = ir.MinValue; max = ir.MaxValue; return; }
            }
            catch
            {
            }

            if (lower.Contains("cooldown") || lower.Contains("recharge")) max = 300f;
            else if (IsDamageKey(lower)) max = 300f;
            else if (lower.Contains("percent")) max = 200f;
            else if (lower.Contains("degrees") || lower.Contains("angle") || lower.Contains("cone")) max = 360f;
            else if (lower.Contains("radius") || lower.Contains("width") || lower.Contains("height")) max = 50f;
            else if (lower.Contains("range") || lower.Contains("distance") || lower.Contains("length")) max = 100f;
            else if (lower.Contains("duration")) max = 120f;
            else if (lower.Contains("windup") || lower.Contains("channel") || lower.Contains("interval") || lower.Contains("delay") || lower.Contains("traveltime") || lower.Contains("droptime") || lower.Contains("time")) max = 20f;
            else if (lower.Contains("speed")) max = 100f;
            else if (lower.Contains("cost") || lower.Contains("health") || lower.Contains("barrier") || lower.Contains("hp")) max = 500f;
            else if (lower.Contains("charges") || lower.Contains("count") || lower.Contains("strikes")) { min = 1f; max = 50f; }
            else if (lower.Contains("multiplier")) max = 10f;
        }

        private void EnsureSelectedSetting()
        {
            DevSetting selected = FindSetting(_selectedSettingId);
            if (selected != null && selected.Section == _selectedSection)
                return;

            _selectedSettingId = string.Empty;
            for (int i = 0; i < _settings.Count; i++)
            {
                if (_settings[i].Section == _selectedSection)
                {
                    _selectedSettingId = _settings[i].Id;
                    break;
                }
            }
        }

        private DevSetting FindSetting(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            for (int i = 0; i < _settings.Count; i++)
            {
                if (_settings[i].Id == id)
                    return _settings[i];
            }
            return null;
        }

        private DevSetting FindSetting(string section, string key)
        {
            return FindSetting(section + "|" + key);
        }

        private float GetNumericValue(DevSetting setting)
        {
            try
            {
                return Convert.ToSingle(setting.Main.BoxedValue, CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0f;
            }
        }

        private float GetDefaultNumericValue(DevSetting setting)
        {
            try
            {
                return Convert.ToSingle(setting.Main.DefaultValue, CultureInfo.InvariantCulture);
            }
            catch
            {
                return GetNumericValue(setting);
            }
        }

        // Writes the value into every copy of the setting and marks the files for saving.
        private void SetValue(DevSetting setting, object value)
        {
            if (setting == null)
                return;
            for (int i = 0; i < setting.Entries.Count; i++)
            {
                ConfigFile file = setting.Files[i];
                bool oldSave = file.SaveOnConfigSet;
                try
                {
                    file.SaveOnConfigSet = false;
                    setting.Entries[i].BoxedValue = value;
                    _dirtyFiles.Add(file);
                }
                catch
                {
                }
                finally
                {
                    file.SaveOnConfigSet = oldSave;
                }
            }
            _lastChangeTime = Time.unscaledTime;
        }

        private void SetNumericValue(DevSetting setting, float value)
        {
            if (setting == null)
                return;
            if (setting.SettingType == typeof(int))
                SetValue(setting, Mathf.RoundToInt(value));
            else
                SetValue(setting, value);
            _inputBuffers[setting.Id] = FormatValue(GetNumericValue(setting));

            // Editing a real cooldown only matters while Test Cooldowns is off.
            if (IsCooldownKey(setting.Key) && setting.Section != "Testing" && GetTestCooldowns())
            {
                SetTestCooldowns(false);
                ShowStatus("Test Cooldowns turned OFF so your cooldown values apply.");
            }
        }

        private void SetToDefault(DevSetting setting)
        {
            if (setting == null)
                return;
            SetValue(setting, setting.Main.DefaultValue);
            _inputBuffers.Remove(setting.Id);
        }

        private void SaveDirty()
        {
            if (_dirtyFiles.Count == 0)
                return;
            foreach (ConfigFile file in _dirtyFiles)
            {
                try
                {
                    file.Save();
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("Config save failed: " + ex.Message);
                }
            }
            _dirtyFiles.Clear();
            ShowStatus("Saved to .cfg");
        }

        private void ReloadAll()
        {
            UnityEngine.Object[] plugins = UnityEngine.Object.FindObjectsOfType(typeof(BaseUnityPlugin));
            for (int i = 0; i < plugins.Length; i++)
            {
                BaseUnityPlugin plugin = plugins[i] as BaseUnityPlugin;
                if (plugin == null)
                    continue;
                string guid = string.Empty;
                try
                {
                    if (plugin.Info != null && plugin.Info.Metadata != null)
                        guid = plugin.Info.Metadata.GUID;
                }
                catch
                {
                }
                if (plugin == this || guid.StartsWith("albedo.customclasses", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        plugin.Config.Reload();
                    }
                    catch
                    {
                    }
                }
            }
            _dirtyFiles.Clear();
            RefreshSettings();
            ShowStatus("Reloaded from .cfg");
        }

        private void ShowStatus(string text)
        {
            _status = text;
            _statusUntil = Time.unscaledTime + 3f;
        }

        private static bool IsCooldownKey(string key)
        {
            string lower = key.ToLowerInvariant();
            return lower.Contains("cooldown") || lower.Contains("rechargeseconds");
        }

        private bool GetTestCooldowns()
        {
            DevSetting force = FindSetting("Testing", "ForceCooldowns");
            if (force == null)
                return false;
            try
            {
                return (bool)force.Main.BoxedValue;
            }
            catch
            {
                return false;
            }
        }

        private void SetTestCooldowns(bool on)
        {
            DevSetting force = FindSetting("Testing", "ForceCooldowns");
            if (force != null)
                SetValue(force, on);
        }

        private string FormatValue(float value)
        {
            if (Mathf.Abs(value - Mathf.Round(value)) < 0.0001f)
                return Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture);
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        // ------------------------------------------------------------------ key capture
        private void UpdateKeyCapture()
        {
            if (Time.frameCount <= _captureFrame + 1)
                return;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _captureSetting = null;
                ShowStatus("Key change cancelled");
                return;
            }
            if (_allKeys == null)
                _allKeys = (KeyCode[])Enum.GetValues(typeof(KeyCode));
            for (int i = 0; i < _allKeys.Length; i++)
            {
                KeyCode key = _allKeys[i];
                if (key == KeyCode.None || key == KeyCode.Mouse0 || key == KeyCode.Mouse1)
                    continue;
                if (Input.GetKeyDown(key))
                {
                    SetValue(_captureSetting, key);
                    ShowStatus(FriendlyKey(_captureSetting.Key) + " = " + key.ToString());
                    _captureSetting = null;
                    return;
                }
            }
        }

        // ------------------------------------------------------------------ drawing
        private void EnsureStyles()
        {
            if (_titleStyle != null)
                return;

            _panelTexture = MakeTexture(new Color(0.035f, 0.04f, 0.055f, 0.97f));
            _selectedTexture = MakeTexture(new Color(0.20f, 0.42f, 0.52f, 0.95f));
            _buttonTexture = MakeTexture(new Color(0.10f, 0.12f, 0.16f, 0.94f));
            _onTexture = MakeTexture(new Color(0.18f, 0.48f, 0.26f, 0.96f));

            _titleStyle = new GUIStyle(GUI.skin.label);
            _titleStyle.fontSize = 21;
            _titleStyle.fontStyle = FontStyle.Bold;
            _titleStyle.normal.textColor = Color.white;

            _smallStyle = new GUIStyle(GUI.skin.label);
            _smallStyle.fontSize = 12;
            _smallStyle.normal.textColor = new Color(0.78f, 0.83f, 0.89f, 1f);

            _descStyle = new GUIStyle(_smallStyle);
            _descStyle.fontSize = 11;
            _descStyle.wordWrap = false;
            _descStyle.clipping = TextClipping.Clip;
            _descStyle.normal.textColor = new Color(0.62f, 0.66f, 0.72f, 1f);

            _valueStyle = new GUIStyle(GUI.skin.textField);
            _valueStyle.alignment = TextAnchor.MiddleCenter;

            _sectionButtonStyle = new GUIStyle(GUI.skin.button);
            _sectionButtonStyle.alignment = TextAnchor.MiddleLeft;
            _sectionButtonStyle.fontSize = 12;
            _sectionButtonStyle.normal.background = _buttonTexture;
            _sectionButtonStyle.hover.background = _selectedTexture;

            _selectedButtonStyle = new GUIStyle(_sectionButtonStyle);
            _selectedButtonStyle.normal.background = _selectedTexture;

            _tabStyle = new GUIStyle(_sectionButtonStyle);
            _tabStyle.alignment = TextAnchor.MiddleCenter;
            _tabStyle.fontSize = 13;
            _tabStyle.fontStyle = FontStyle.Bold;

            _tabSelectedStyle = new GUIStyle(_tabStyle);
            _tabSelectedStyle.normal.background = _selectedTexture;

            _offStyle = new GUIStyle(_tabStyle);
            _onStyle = new GUIStyle(_tabStyle);
            _onStyle.normal.background = _onTexture;
            _onStyle.hover.background = _onTexture;
        }

        private Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void OnGUI()
        {
            if (!_open)
                return;

            EnsureStyles();
            Color old = GUI.color;
            GUI.color = Color.white;
            _windowRect = GUI.Window(990011, _windowRect, DrawWindow, "IMMORTAL HEROES - CONFIG");
            GUI.color = old;
        }

        private void DrawWindow(int id)
        {
            GUI.DrawTexture(new Rect(0f, 22f, _windowRect.width, _windowRect.height - 22f), _panelTexture);

            GUI.Label(new Rect(22f, 30f, 420f, 28f), "Immortal Heroes Config", _titleStyle);
            string saveState = _dirtyFiles.Count > 0 ? (_autoSave.Value ? "Saving..." : "Unsaved changes") : "All changes saved";
            if (Time.unscaledTime < _statusUntil)
                saveState = _status;
            GUI.Label(new Rect(22f, 58f, 420f, 20f), saveState + "   |   " + _toggleKey.Value.ToString() + " / Esc closes", _smallStyle);

            bool test = GetTestCooldowns();
            if (GUI.Button(new Rect(440f, 34f, 190f, 34f), test ? "TEST COOLDOWNS: ON" : "TEST COOLDOWNS: OFF", test ? _onStyle : _offStyle))
            {
                SetTestCooldowns(!test);
                ShowStatus(!test ? "Every cooldown uses Testing CooldownSeconds" : "Real cooldowns restored");
            }
            bool nextAuto = GUI.Toggle(new Rect(640f, 40f, 110f, 24f), _autoSave.Value, " Auto-save");
            if (nextAuto != _autoSave.Value)
            {
                _autoSave.Value = nextAuto;
                Config.Save();
            }
            if (GUI.Button(new Rect(755f, 34f, 100f, 34f), "SAVE"))
                SaveDirty();
            if (GUI.Button(new Rect(862f, 34f, 120f, 34f), "RELOAD .CFG"))
                ReloadAll();
            if (GUI.Button(new Rect(990f, 34f, 108f, 34f), "CLOSE"))
                ClosePanel();

            // Tabs + search
            float tx = 22f;
            for (int i = 0; i < Tabs.Length; i++)
            {
                bool selectedTab = Tabs[i] == _selectedTab && string.IsNullOrEmpty(_search);
                if (GUI.Button(new Rect(tx, 84f, 150f, 28f), Tabs[i], selectedTab ? _tabSelectedStyle : _tabStyle))
                {
                    _selectedTab = Tabs[i];
                    _search = string.Empty;
                    _sectionScroll = Vector2.zero;
                    _settingScroll = Vector2.zero;
                    RebuildSections();
                }
                tx += 156f;
            }
            GUI.Label(new Rect(812f, 88f, 60f, 22f), "Search", _smallStyle);
            string nextSearch = GUI.TextField(new Rect(868f, 85f, 230f, 26f), _search ?? "");
            if (nextSearch != _search)
            {
                _search = nextSearch;
                _sectionScroll = Vector2.zero;
                RebuildSections();
            }

            GUI.Box(new Rect(18f, 122f, 300f, 562f), "SECTIONS");
            GUI.Box(new Rect(328f, 122f, 774f, 562f), "");

            Rect sectionView = new Rect(28f, 148f, 282f, 528f);
            Rect sectionContent = new Rect(0f, 0f, 260f, Mathf.Max(520f, _sections.Count * 32f + 8f));
            _sectionScroll = GUI.BeginScrollView(sectionView, _sectionScroll, sectionContent);
            float sy = 4f;
            for (int i = 0; i < _sections.Count; i++)
            {
                string section = _sections[i];
                GUIStyle style = section == _selectedSection ? _selectedButtonStyle : _sectionButtonStyle;
                if (GUI.Button(new Rect(4f, sy, 252f, 28f), FriendlySectionName(section), style))
                {
                    _selectedSection = section;
                    EnsureSelectedSetting();
                    _settingScroll = Vector2.zero;
                }
                sy += 32f;
            }
            GUI.EndScrollView();

            DrawSettingsPanel();
            GUI.DragWindow(new Rect(0f, 0f, _windowRect.width, 24f));
        }

        private string FriendlySectionName(string section)
        {
            return section.Replace(".", " - ");
        }

        private void DrawSettingsPanel()
        {
            List<DevSetting> visible = new List<DevSetting>();
            string search = (_search ?? "").Trim().ToLowerInvariant();
            for (int i = 0; i < _settings.Count; i++)
            {
                DevSetting s = _settings[i];
                if (s.Section != _selectedSection)
                    continue;
                if (search.Length > 0 && !s.Section.ToLowerInvariant().Contains(search) && !s.Key.ToLowerInvariant().Contains(search))
                    continue;
                visible.Add(s);
            }

            GUI.Label(new Rect(344f, 128f, 520f, 26f), FriendlySectionName(_selectedSection), _titleStyle);
            if (visible.Count > 0 && GUI.Button(new Rect(918f, 130f, 170f, 26f), "SECTION DEFAULTS"))
            {
                for (int i = 0; i < visible.Count; i++)
                    SetToDefault(visible[i]);
                ShowStatus(FriendlySectionName(_selectedSection) + " reset to defaults");
            }
            bool nextPreview = GUI.Toggle(new Rect(344f, 156f, 330f, 20f), _preview, " Show range / radius preview in the world");
            if (nextPreview != _preview)
            {
                _preview = nextPreview;
                if (!_preview)
                    ClearPreview();
            }

            Rect view = new Rect(338f, 182f, 756f, 494f);
            float rowHeight = 58f;
            float contentHeight = 8f;
            for (int i = 0; i < visible.Count; i++)
                contentHeight += visible[i].Key == "AscendedSkills" ? 118f : rowHeight;
            Rect content = new Rect(0f, 0f, 730f, Mathf.Max(490f, contentHeight));
            _settingScroll = GUI.BeginScrollView(view, _settingScroll, content);

            float y = 4f;
            for (int i = 0; i < visible.Count; i++)
            {
                DevSetting setting = visible[i];
                bool selectedRow = setting.Id == _selectedSettingId;
                float h = setting.Key == "AscendedSkills" ? 114f : rowHeight - 4f;
                if (selectedRow)
                    GUI.Box(new Rect(0f, y - 2f, 724f, h), "");

                if (GUI.Button(new Rect(4f, y + 3f, 210f, 28f), FriendlyKey(setting.Key), selectedRow ? _selectedButtonStyle : _sectionButtonStyle))
                    _selectedSettingId = setting.Id;

                if (setting.IsNumber)
                    DrawNumberRow(setting, y);
                else if (setting.SettingType == typeof(bool))
                    DrawBoolRow(setting, y);
                else if (setting.SettingType == typeof(KeyCode))
                    DrawKeyRow(setting, y);
                else if (setting.SettingType.IsEnum)
                    DrawEnumRow(setting, y);
                else if (setting.Key == "AscendedSkills")
                    DrawAscendedRow(setting, y);
                else
                    DrawStringRow(setting, y);

                if (GUI.Button(new Rect(640f, y + 3f, 78f, 28f), "Default"))
                    SetToDefault(setting);

                if (setting.Key != "AscendedSkills")
                    GUI.Label(new Rect(8f, y + 33f, 710f, 18f), DescriptionLine(setting), _descStyle);
                y += setting.Key == "AscendedSkills" ? 118f : rowHeight;
            }

            GUI.EndScrollView();
        }

        private string DescriptionLine(DevSetting setting)
        {
            string text = string.IsNullOrEmpty(setting.Description) ? "" : setting.Description;
            string def;
            try
            {
                def = setting.IsNumber ? FormatValue(GetDefaultNumericValue(setting)) : Convert.ToString(setting.Main.DefaultValue, CultureInfo.InvariantCulture);
            }
            catch
            {
                def = "?";
            }
            return "Default " + def + (text.Length > 0 ? "   |   " + text : "");
        }

        private void DrawNumberRow(DevSetting setting, float y)
        {
            float current = GetNumericValue(setting);
            float slider = GUI.HorizontalSlider(new Rect(226f, y + 12f, 300f, 20f), Mathf.Clamp(current, setting.SliderMin, setting.SliderMax), setting.SliderMin, setting.SliderMax);
            if (Mathf.Abs(slider - Mathf.Clamp(current, setting.SliderMin, setting.SliderMax)) > 0.01f)
            {
                float step = setting.SliderMax - setting.SliderMin <= 20f ? 0.1f : 1f;
                SetNumericValue(setting, Mathf.Round(slider / step) * step);
                current = GetNumericValue(setting);
            }

            string buffer;
            if (!_inputBuffers.TryGetValue(setting.Id, out buffer))
            {
                buffer = FormatValue(current);
                _inputBuffers[setting.Id] = buffer;
            }

            string next = GUI.TextField(new Rect(540f, y + 3f, 90f, 28f), buffer, _valueStyle);
            if (next != buffer)
            {
                _inputBuffers[setting.Id] = next;
                float parsed;
                if (float.TryParse(next, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                {
                    if (setting.SettingType == typeof(int))
                        SetValue(setting, Mathf.RoundToInt(parsed));
                    else
                        SetValue(setting, parsed);
                    if (IsCooldownKey(setting.Key) && setting.Section != "Testing" && GetTestCooldowns())
                    {
                        SetTestCooldowns(false);
                        ShowStatus("Test Cooldowns turned OFF so your cooldown values apply.");
                    }
                }
            }
        }

        private void DrawBoolRow(DevSetting setting, float y)
        {
            bool value = false;
            try
            {
                value = (bool)setting.Main.BoxedValue;
            }
            catch
            {
            }
            if (GUI.Button(new Rect(226f, y + 3f, 120f, 28f), value ? "ON" : "OFF", value ? _onStyle : _offStyle))
                SetValue(setting, !value);
        }

        private void DrawKeyRow(DevSetting setting, float y)
        {
            bool capturing = _captureSetting == setting;
            string label = capturing ? "Press a key...  (Esc cancels)" : Convert.ToString(setting.Main.BoxedValue, CultureInfo.InvariantCulture);
            if (GUI.Button(new Rect(226f, y + 3f, 300f, 28f), label, capturing ? _tabSelectedStyle : _tabStyle))
            {
                _captureSetting = setting;
                _captureFrame = Time.frameCount;
            }
        }

        private void DrawEnumRow(DevSetting setting, float y)
        {
            Array values = Enum.GetValues(setting.SettingType);
            object current = setting.Main.BoxedValue;
            int index = Array.IndexOf(values, current);
            if (GUI.Button(new Rect(226f, y + 3f, 34f, 28f), "<"))
                SetValue(setting, values.GetValue((index - 1 + values.Length) % values.Length));
            GUI.Label(new Rect(266f, y + 7f, 220f, 22f), Convert.ToString(current, CultureInfo.InvariantCulture), _smallStyle);
            if (GUI.Button(new Rect(492f, y + 3f, 34f, 28f), ">"))
                SetValue(setting, values.GetValue((index + 1) % values.Length));
        }

        private void DrawStringRow(DevSetting setting, float y)
        {
            string current = Convert.ToString(setting.Main.BoxedValue, CultureInfo.InvariantCulture) ?? "";
            string next = GUI.TextField(new Rect(226f, y + 3f, 404f, 28f), current);
            if (next != current)
                SetValue(setting, next);
        }

        // [Testing] AscendedSkills: one checkbox per Paladin skill instead of a comma list.
        private void DrawAscendedRow(DevSetting setting, float y)
        {
            string current = Convert.ToString(setting.Main.BoxedValue, CultureInfo.InvariantCulture) ?? "";
            List<string> list = new List<string>();
            string[] parts = current.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i].Trim();
                if (part.Length > 0 && !list.Contains(part))
                    list.Add(part);
            }
            GUI.Label(new Rect(226f, y + 7f, 400f, 22f), "Force these skills Ascended (testing):", _smallStyle);
            bool changed = false;
            for (int i = 0; i < AscendableSkills.Length; i++)
            {
                string skill = AscendableSkills[i];
                float x = 8f + (i % 4) * 178f;
                float row = y + 36f + (i / 4) * 30f;
                bool on = list.Contains(skill);
                bool next = GUI.Toggle(new Rect(x, row, 172f, 24f), on, " " + PrettySkill(skill));
                if (next != on)
                {
                    if (next) list.Add(skill);
                    else list.Remove(skill);
                    changed = true;
                }
            }
            if (changed)
                SetValue(setting, string.Join(", ", list.ToArray()));
        }

        private static string PrettySkill(string id)
        {
            string[] words = id.Split('_');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                    words[i] = char.ToUpperInvariant(words[i][0]) + words[i].Substring(1);
            }
            return string.Join(" ", words);
        }

        private string FriendlyKey(string key)
        {
            string text = key;
            int v = text.LastIndexOf("_v0", StringComparison.Ordinal);
            if (v > 0)
                text = text.Substring(0, v);
            text = text.Replace("Meters", "").Replace("Seconds", "");
            return text;
        }

        // ------------------------------------------------------------------ world preview
        private void UpdateWorldPreview()
        {
            Player player = Player.m_localPlayer;
            DevSetting setting = FindSetting(_selectedSettingId);
            if (player == null || setting == null || !setting.IsNumber)
            {
                ClearPreview();
                return;
            }

            float value = Mathf.Max(0f, GetNumericValue(setting));
            string lower = setting.Key.ToLowerInvariant();
            Vector3 origin = GroundPoint(player.transform.position + Vector3.up * 0.2f);
            Vector3 forward = player.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;
            forward.Normalize();

            if (lower.Contains("radius"))
            {
                DrawCircle(0, origin, value);
                TrimPreview(1);
                return;
            }

            if (lower.Contains("cone") || lower.Contains("degrees") || lower.Contains("angle"))
            {
                float range = FindCompanionValue(setting.Section, new string[] { "Range", "RangeMeters", "CastRange", "GroundPACRange", "GroundPacRange", "Distance", "Length" }, 10f);
                DrawCone(0, origin, forward, range, value);
                TrimPreview(1);
                return;
            }

            if (lower.Contains("width"))
            {
                float range = FindCompanionValue(setting.Section, new string[] { "Range", "RangeMeters", "CastRange", "GroundPACRange", "GroundPacRange", "Distance", "Length" }, 10f);
                DrawLane(0, origin, forward, range, value);
                TrimPreview(1);
                return;
            }

            if (lower.Contains("range") || lower.Contains("distance") || lower.Contains("length"))
            {
                DrawRangeLine(0, origin, forward, value);
                DrawCircle(1, GroundPoint(origin + forward * value), 0.5f);
                TrimPreview(2);
                return;
            }

            ClearPreview();
        }

        private float FindCompanionValue(string section, string[] keyStarts, float fallback)
        {
            for (int i = 0; i < keyStarts.Length; i++)
            {
                string wanted = keyStarts[i].ToLowerInvariant();
                for (int j = 0; j < _settings.Count; j++)
                {
                    DevSetting setting = _settings[j];
                    if (setting.Section != section || !setting.IsNumber)
                        continue;
                    string key = setting.Key.ToLowerInvariant();
                    if (key == wanted || key.StartsWith(wanted, StringComparison.OrdinalIgnoreCase))
                        return Mathf.Max(0f, GetNumericValue(setting));
                }
            }
            return fallback;
        }

        private Vector3 GroundPoint(Vector3 point)
        {
            RaycastHit hit;
            Vector3 start = point + Vector3.up * 4f;
            int mask = LayerMask.GetMask(
                "Default",
                "static_solid",
                "Default_small",
                "piece_nonsolid",
                "terrain",
                "vehicle",
                "piece",
                "viewblock"
            );
            bool found = mask == 0
                ? Physics.Raycast(start, Vector3.down, out hit, 20f)
                : Physics.Raycast(start, Vector3.down, out hit, 20f, mask);
            if (found)
                return hit.point + Vector3.up * 0.05f;
            point.y += 0.05f;
            return point;
        }

        private LineRenderer GetPreviewLine(int index)
        {
            while (_previewObjects.Count <= index)
            {
                GameObject obj = new GameObject("DragonsAltar_DeveloperRangePreview");
                LineRenderer line = obj.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.loop = false;
                line.startWidth = 0.08f;
                line.endWidth = 0.08f;
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null)
                    line.material = new Material(shader);
                line.startColor = new Color(0.25f, 0.95f, 1f, 0.95f);
                line.endColor = new Color(1f, 0.88f, 0.25f, 0.95f);
                _previewObjects.Add(obj);
            }
            return _previewObjects[index].GetComponent<LineRenderer>();
        }

        private void DrawCircle(int index, Vector3 center, float radius)
        {
            LineRenderer line = GetPreviewLine(index);
            int count = 65;
            line.loop = false;
            line.positionCount = count;
            for (int i = 0; i < count; i++)
            {
                float angle = ((float)i / 64f) * Mathf.PI * 2f;
                Vector3 point = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                line.SetPosition(i, GroundPoint(point));
            }
        }

        private void DrawRangeLine(int index, Vector3 origin, Vector3 forward, float range)
        {
            LineRenderer line = GetPreviewLine(index);
            line.loop = false;
            line.positionCount = 2;
            line.SetPosition(0, GroundPoint(origin));
            line.SetPosition(1, GroundPoint(origin + forward * range));
        }

        private void DrawCone(int index, Vector3 origin, Vector3 forward, float range, float degrees)
        {
            LineRenderer line = GetPreviewLine(index);
            int arcPoints = 25;
            line.loop = false;
            line.positionCount = arcPoints + 3;
            float half = Mathf.Clamp(degrees * 0.5f, 0f, 179.5f);
            Vector3 left = Quaternion.Euler(0f, -half, 0f) * forward;
            line.SetPosition(0, GroundPoint(origin));
            line.SetPosition(1, GroundPoint(origin + left * range));
            for (int i = 0; i < arcPoints; i++)
            {
                float t = arcPoints <= 1 ? 0f : (float)i / (float)(arcPoints - 1);
                float angle = Mathf.Lerp(-half, half, t);
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * forward;
                line.SetPosition(i + 2, GroundPoint(origin + dir * range));
            }
            line.SetPosition(arcPoints + 2, GroundPoint(origin));
        }

        private void DrawLane(int index, Vector3 origin, Vector3 forward, float range, float width)
        {
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            float half = width * 0.5f;
            LineRenderer line = GetPreviewLine(index);
            line.loop = false;
            line.positionCount = 5;
            line.SetPosition(0, GroundPoint(origin - right * half));
            line.SetPosition(1, GroundPoint(origin + right * half));
            line.SetPosition(2, GroundPoint(origin + forward * range + right * half));
            line.SetPosition(3, GroundPoint(origin + forward * range - right * half));
            line.SetPosition(4, GroundPoint(origin - right * half));
        }

        private void TrimPreview(int count)
        {
            for (int i = 0; i < _previewObjects.Count; i++)
            {
                if (_previewObjects[i] != null)
                    _previewObjects[i].SetActive(i < count);
            }
        }

        private void ClearPreview()
        {
            for (int i = 0; i < _previewObjects.Count; i++)
            {
                if (_previewObjects[i] != null)
                    Destroy(_previewObjects[i]);
            }
            _previewObjects.Clear();
        }
    }
}
