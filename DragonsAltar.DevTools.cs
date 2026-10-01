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
        public BaseUnityPlugin Owner;
        public ConfigFile File;
        public ConfigEntryBase Entry;
        public string PluginGuid;
        public string Section;
        public string Key;
        public string Id;
        public bool IsInteger;
        public float SliderMin;
        public float SliderMax;
    }

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
        public const string ModVersion = "0.15.0";

        public static DeveloperToolsPlugin Instance;

        public static bool IsDeveloperPanelOpen
        {
            get { return Instance != null && Instance._open; }
        }

        private ConfigEntry<KeyCode> _toggleKey;
        private ConfigEntry<bool> _enableWorldPreview;

        private readonly List<DevSetting> _settings = new List<DevSetting>();
        private readonly List<string> _sections = new List<string>();
        private readonly Dictionary<string, string> _inputBuffers = new Dictionary<string, string>();
        private readonly List<GameObject> _previewObjects = new List<GameObject>();

        private Rect _windowRect = new Rect(55f, 55f, 1120f, 690f);
        private Vector2 _sectionScroll;
        private Vector2 _settingScroll;
        private string _selectedSection = string.Empty;
        private string _selectedSettingId = string.Empty;
        private bool _open;
        private bool _preview;
        private bool _savedCursorVisible;
        private CursorLockMode _savedCursorLock;
        private string _mode = "CUSTOM";
        private GUIStyle _titleStyle;
        private GUIStyle _smallStyle;
        private GUIStyle _selectedButtonStyle;
        private GUIStyle _sectionButtonStyle;
        private GUIStyle _valueStyle;
        private Texture2D _panelTexture;
        private Texture2D _selectedTexture;
        private Texture2D _buttonTexture;

        private void Awake()
        {
            Instance = this;
            _toggleKey = Config.Bind("Developer UI", "ToggleKey", KeyCode.F8, "Open or close the Dragon's Altar developer tuning console.");
            _enableWorldPreview = Config.Bind("Developer UI", "EnableWorldPreview", true, "Allow the selected range/radius setting to draw an in-world ruler preview.");
            _preview = _enableWorldPreview.Value;
            Logger.LogInfo(ModName + " v" + ModVersion + " loaded. Press " + _toggleKey.Value.ToString() + " for Skill Tuning.");
        }

        private void OnDestroy()
        {
            ClosePanel();
            ClearPreview();
            Instance = null;
        }

        private void Update()
        {
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

            if (_preview && _enableWorldPreview.Value)
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

        private void RefreshSettings()
        {
            _settings.Clear();
            _sections.Clear();
            _inputBuffers.Clear();

            UnityEngine.Object[] plugins = UnityEngine.Object.FindObjectsOfType(typeof(BaseUnityPlugin));
            Dictionary<string, DevSetting> dedupe = new Dictionary<string, DevSetting>();

            for (int i = 0; i < plugins.Length; i++)
            {
                BaseUnityPlugin plugin = plugins[i] as BaseUnityPlugin;
                if (plugin == null || plugin == this)
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

                if (string.IsNullOrEmpty(guid) || !guid.StartsWith("albedo.customclasses", StringComparison.OrdinalIgnoreCase))
                    continue;

                IDictionary<ConfigDefinition, ConfigEntryBase> entries = plugin.Config as IDictionary<ConfigDefinition, ConfigEntryBase>;
                if (entries == null)
                    continue;

                foreach (KeyValuePair<ConfigDefinition, ConfigEntryBase> pair in entries)
                {
                    ConfigEntryBase entry = pair.Value;
                    if (entry == null)
                        continue;

                    string section = entry.Definition.Section;
                    string key = entry.Definition.Key;

                    if (!IsSkillSection(section))
                        continue;
                    if (!IsTunableKey(key))
                        continue;
                    if (ShouldSkipDuplicateRuntimeConfig(guid, section))
                        continue;

                    Type settingType = entry.SettingType;
                    bool isInteger = settingType == typeof(int);
                    bool isFloat = settingType == typeof(float);
                    if (!isInteger && !isFloat)
                        continue;

                    string unique = section + "|" + key;
                    if (dedupe.ContainsKey(unique))
                        continue;

                    DevSetting setting = new DevSetting();
                    setting.Owner = plugin;
                    setting.File = plugin.Config;
                    setting.Entry = entry;
                    setting.PluginGuid = guid;
                    setting.Section = section;
                    setting.Key = key;
                    setting.Id = guid + "|" + unique;
                    setting.IsInteger = isInteger;
                    GetSliderBounds(key, out setting.SliderMin, out setting.SliderMax);
                    dedupe.Add(unique, setting);
                    _settings.Add(setting);

                    if (!_sections.Contains(section))
                        _sections.Add(section);
                }
            }

            _sections.Sort(StringComparer.OrdinalIgnoreCase);
            _settings.Sort(delegate(DevSetting a, DevSetting b)
            {
                int sectionCompare = string.Compare(a.Section, b.Section, StringComparison.OrdinalIgnoreCase);
                if (sectionCompare != 0)
                    return sectionCompare;
                return string.Compare(a.Key, b.Key, StringComparison.OrdinalIgnoreCase);
            });

            if (_sections.Count > 0 && (string.IsNullOrEmpty(_selectedSection) || !_sections.Contains(_selectedSection)))
                _selectedSection = _sections[0];

            EnsureSelectedSetting();
        }

        private bool ShouldSkipDuplicateRuntimeConfig(string guid, string section)
        {
            if (guid == "albedo.customclasses.sorcerer" && section.StartsWith("Sorcerer.", StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        private bool IsSkillSection(string section)
        {
            if (string.IsNullOrEmpty(section))
                return false;

            return section.StartsWith("Warrior.", StringComparison.OrdinalIgnoreCase) ||
                   section.StartsWith("Cleric.", StringComparison.OrdinalIgnoreCase) ||
                   section.StartsWith("Sword Master", StringComparison.OrdinalIgnoreCase) ||
                   section.StartsWith("Mercenary", StringComparison.OrdinalIgnoreCase) ||
                   section.StartsWith("Paladin", StringComparison.OrdinalIgnoreCase) ||
                   section.StartsWith("Priest", StringComparison.OrdinalIgnoreCase) ||
                   section.StartsWith("Sorcerer.", StringComparison.OrdinalIgnoreCase) ||
                   section.StartsWith("Wizard", StringComparison.OrdinalIgnoreCase) ||
                   section.StartsWith("Spellcaster", StringComparison.OrdinalIgnoreCase) ||
                   section == "Grand Sigil Survival" ||
                   section == "Acrobatic Jump Skills";
        }

        private bool IsTunableKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            string lower = key.ToLowerInvariant();
            if (lower.Contains("damage") || lower.Contains("dotpersecond") || lower.Contains("healpercent") || lower.Contains("immediateheal"))
                return false;

            return lower.Contains("cooldown") ||
                   lower.Contains("recharge") ||
                   lower.Contains("radius") ||
                   lower.Contains("range") ||
                   lower.Contains("distance") ||
                   lower.Contains("length") ||
                   lower.Contains("width") ||
                   lower.Contains("height") ||
                   lower.Contains("windup") ||
                   lower.Contains("channel") ||
                   lower.Contains("duration") ||
                   lower.Contains("interval") ||
                   lower.Contains("delay") ||
                   lower.Contains("speed") ||
                   lower.Contains("traveltime") ||
                   lower.Contains("cone") ||
                   lower.Contains("degrees") ||
                   lower.Contains("angle") ||
                   lower.Contains("droptime") ||
                   lower.Contains("cost") ||
                   lower.Contains("charges") ||
                   lower.Contains("count") ||
                   lower.Contains("strikes") ||
                   lower.Contains("multiplier");
        }

        private void GetSliderBounds(string key, out float min, out float max)
        {
            string lower = key.ToLowerInvariant();
            min = 0f;
            max = 100f;

            if (lower.Contains("cooldown") || lower.Contains("recharge")) max = 300f;
            else if (lower.Contains("degrees") || lower.Contains("angle") || lower.Contains("cone")) max = 180f;
            else if (lower.Contains("radius") || lower.Contains("width") || lower.Contains("height")) max = 50f;
            else if (lower.Contains("range") || lower.Contains("distance") || lower.Contains("length")) max = 100f;
            else if (lower.Contains("duration")) max = 120f;
            else if (lower.Contains("windup") || lower.Contains("channel") || lower.Contains("interval") || lower.Contains("delay") || lower.Contains("traveltime") || lower.Contains("droptime")) max = 20f;
            else if (lower.Contains("speed")) max = 100f;
            else if (lower.Contains("cost")) max = 500f;
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

        private float GetNumericValue(DevSetting setting)
        {
            try
            {
                object value = setting.Entry.BoxedValue;
                if (setting.IsInteger)
                    return Convert.ToSingle((int)value, CultureInfo.InvariantCulture);
                return Convert.ToSingle((float)value, CultureInfo.InvariantCulture);
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
                object value = setting.Entry.DefaultValue;
                if (setting.IsInteger)
                    return Convert.ToSingle((int)value, CultureInfo.InvariantCulture);
                return Convert.ToSingle((float)value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return GetNumericValue(setting);
            }
        }

        private void SetNumericValue(DevSetting setting, float value)
        {
            if (setting == null || setting.Entry == null || setting.File == null)
                return;

            bool oldSave = setting.File.SaveOnConfigSet;
            try
            {
                if (IsCooldownKey(setting.Key))
                    DisableLegacyCooldownOverrides();

                setting.File.SaveOnConfigSet = false;
                if (setting.IsInteger)
                    setting.Entry.BoxedValue = Mathf.RoundToInt(value);
                else
                    setting.Entry.BoxedValue = value;
                _inputBuffers[setting.Id] = FormatValue(GetNumericValue(setting));
                _mode = "CUSTOM";
            }
            catch
            {
            }
            finally
            {
                setting.File.SaveOnConfigSet = oldSave;
            }
        }

        private void SetEntryWithoutSaving(ConfigFile file, ConfigEntryBase entry, object value)
        {
            if (file == null || entry == null)
                return;

            bool oldSave = file.SaveOnConfigSet;
            try
            {
                file.SaveOnConfigSet = false;
                entry.BoxedValue = value;
            }
            catch
            {
            }
            finally
            {
                file.SaveOnConfigSet = oldSave;
            }
        }

        private void ApplyMode(bool developer)
        {
            for (int i = 0; i < _settings.Count; i++)
            {
                DevSetting setting = _settings[i];
                float value = GetDefaultNumericValue(setting);
                if (developer && IsCooldownKey(setting.Key))
                    value = 5f;

                if (setting.IsInteger)
                    SetEntryWithoutSaving(setting.File, setting.Entry, Mathf.RoundToInt(value));
                else
                    SetEntryWithoutSaving(setting.File, setting.Entry, value);

                _inputBuffers[setting.Id] = FormatValue(GetNumericValue(setting));
            }

            DisableLegacyCooldownOverrides();
            _mode = developer ? "DEVELOPER - 5s COOLDOWNS" : "DEFAULT";
        }

        private bool IsCooldownKey(string key)
        {
            string lower = key.ToLowerInvariant();
            return lower.Contains("cooldown") || lower.Contains("rechargeseconds");
        }

        private void DisableLegacyCooldownOverrides()
        {
            UnityEngine.Object[] plugins = UnityEngine.Object.FindObjectsOfType(typeof(BaseUnityPlugin));
            for (int i = 0; i < plugins.Length; i++)
            {
                BaseUnityPlugin plugin = plugins[i] as BaseUnityPlugin;
                if (plugin == null || plugin == this)
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

                if (string.IsNullOrEmpty(guid) || !guid.StartsWith("albedo.customclasses", StringComparison.OrdinalIgnoreCase))
                    continue;

                IDictionary<ConfigDefinition, ConfigEntryBase> entries = plugin.Config as IDictionary<ConfigDefinition, ConfigEntryBase>;
                if (entries == null)
                    continue;

                ConfigEntryBase forceEntry;
                if (entries.TryGetValue(new ConfigDefinition("Testing", "ForceCooldowns"), out forceEntry))
                    SetEntryWithoutSaving(plugin.Config, forceEntry, false);

                ConfigEntryBase secondsEntry;
                if (entries.TryGetValue(new ConfigDefinition("Testing", "CooldownSeconds"), out secondsEntry))
                    SetEntryWithoutSaving(plugin.Config, secondsEntry, 5f);
            }
        }

        private string FormatValue(float value)
        {
            if (Mathf.Abs(value - Mathf.Round(value)) < 0.0001f)
                return Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture);
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null)
                return;

            _panelTexture = MakeTexture(new Color(0.035f, 0.04f, 0.055f, 0.97f));
            _selectedTexture = MakeTexture(new Color(0.20f, 0.42f, 0.52f, 0.95f));
            _buttonTexture = MakeTexture(new Color(0.10f, 0.12f, 0.16f, 0.94f));

            _titleStyle = new GUIStyle(GUI.skin.label);
            _titleStyle.fontSize = 21;
            _titleStyle.fontStyle = FontStyle.Bold;
            _titleStyle.normal.textColor = Color.white;

            _smallStyle = new GUIStyle(GUI.skin.label);
            _smallStyle.fontSize = 12;
            _smallStyle.normal.textColor = new Color(0.78f, 0.83f, 0.89f, 1f);

            _valueStyle = new GUIStyle(GUI.skin.textField);
            _valueStyle.alignment = TextAnchor.MiddleCenter;

            _sectionButtonStyle = new GUIStyle(GUI.skin.button);
            _sectionButtonStyle.alignment = TextAnchor.MiddleLeft;
            _sectionButtonStyle.fontSize = 12;
            _sectionButtonStyle.normal.background = _buttonTexture;
            _sectionButtonStyle.hover.background = _selectedTexture;

            _selectedButtonStyle = new GUIStyle(_sectionButtonStyle);
            _selectedButtonStyle.normal.background = _selectedTexture;
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
            _windowRect = GUI.Window(990011, _windowRect, DrawWindow, "DRAGON'S ALTAR - SKILL TUNING");
            GUI.color = old;
        }

        private void DrawWindow(int id)
        {
            GUI.DrawTexture(new Rect(0f, 22f, _windowRect.width, _windowRect.height - 22f), _panelTexture);

            GUI.Label(new Rect(22f, 34f, 520f, 28f), "Developer Skill Tuning", _titleStyle);
            GUI.Label(new Rect(22f, 64f, 620f, 20f), "Session-only tuning. Slider steps are 1; type decimals in the value box.", _smallStyle);

            bool defaultPressed = GUI.Button(new Rect(650f, 37f, 132f, 38f), "DEFAULT");
            bool developerPressed = GUI.Button(new Rect(790f, 37f, 190f, 38f), "DEVELOPER - 5s CD");
            if (defaultPressed) ApplyMode(false);
            if (developerPressed) ApplyMode(true);

            GUI.Label(new Rect(987f, 45f, 110f, 24f), _mode, _smallStyle);

            bool nextPreview = GUI.Toggle(new Rect(650f, 82f, 220f, 24f), _preview, " Preview selected value");
            if (nextPreview != _preview)
            {
                _preview = nextPreview;
                if (!_preview)
                    ClearPreview();
            }

            if (GUI.Button(new Rect(880f, 80f, 105f, 28f), "Refresh"))
                RefreshSettings();
            if (GUI.Button(new Rect(993f, 80f, 105f, 28f), "Close"))
                ClosePanel();

            GUI.Box(new Rect(18f, 116f, 320f, 542f), "SKILLS");
            GUI.Box(new Rect(348f, 116f, 754f, 542f), "TUNING");

            Rect sectionView = new Rect(30f, 145f, 296f, 500f);
            Rect sectionContent = new Rect(0f, 0f, 270f, Mathf.Max(500f, _sections.Count * 34f + 8f));
            _sectionScroll = GUI.BeginScrollView(sectionView, _sectionScroll, sectionContent);
            float sy = 4f;
            for (int i = 0; i < _sections.Count; i++)
            {
                string section = _sections[i];
                GUIStyle style = section == _selectedSection ? _selectedButtonStyle : _sectionButtonStyle;
                if (GUI.Button(new Rect(4f, sy, 252f, 29f), FriendlySectionName(section), style))
                {
                    _selectedSection = section;
                    EnsureSelectedSetting();
                    _settingScroll = Vector2.zero;
                }
                sy += 34f;
            }
            GUI.EndScrollView();

            DrawSettingsPanel();
            GUI.DragWindow(new Rect(0f, 0f, _windowRect.width - 120f, 28f));
        }

        private string FriendlySectionName(string section)
        {
            return section.Replace(".", " - ");
        }

        private void DrawSettingsPanel()
        {
            List<DevSetting> visible = new List<DevSetting>();
            for (int i = 0; i < _settings.Count; i++)
            {
                if (_settings[i].Section == _selectedSection)
                    visible.Add(_settings[i]);
            }

            GUI.Label(new Rect(368f, 143f, 700f, 26f), FriendlySectionName(_selectedSection), _titleStyle);

            DevSetting selected = FindSetting(_selectedSettingId);
            string previewText = selected == null ? "No preview value selected" : "Preview: " + FriendlyKey(selected.Key) + " = " + FormatValue(GetNumericValue(selected));
            GUI.Label(new Rect(368f, 171f, 700f, 20f), previewText, _smallStyle);

            Rect view = new Rect(364f, 200f, 722f, 435f);
            Rect content = new Rect(0f, 0f, 694f, Mathf.Max(430f, visible.Count * 62f + 8f));
            _settingScroll = GUI.BeginScrollView(view, _settingScroll, content);

            float y = 5f;
            for (int i = 0; i < visible.Count; i++)
            {
                DevSetting setting = visible[i];
                bool selectedRow = setting.Id == _selectedSettingId;
                if (selectedRow)
                    GUI.Box(new Rect(0f, y - 3f, 676f, 56f), "");

                if (GUI.Button(new Rect(4f, y + 4f, 188f, 28f), FriendlyKey(setting.Key), selectedRow ? _selectedButtonStyle : _sectionButtonStyle))
                    _selectedSettingId = setting.Id;

                float current = GetNumericValue(setting);
                float slider = GUI.HorizontalSlider(new Rect(205f, y + 12f, 285f, 20f), current, setting.SliderMin, setting.SliderMax);
                float stepped = Mathf.Round(slider);
                if (Mathf.Abs(slider - current) > 0.01f)
                {
                    SetNumericValue(setting, stepped);
                    current = GetNumericValue(setting);
                }

                string buffer;
                if (!_inputBuffers.TryGetValue(setting.Id, out buffer))
                {
                    buffer = FormatValue(current);
                    _inputBuffers[setting.Id] = buffer;
                }

                string next = GUI.TextField(new Rect(505f, y + 4f, 74f, 29f), buffer, _valueStyle);
                if (next != buffer)
                {
                    _inputBuffers[setting.Id] = next;
                    float parsed;
                    if (float.TryParse(next, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                        SetNumericValue(setting, parsed);
                }

                if (GUI.Button(new Rect(590f, y + 4f, 78f, 29f), "Default"))
                    SetNumericValue(setting, GetDefaultNumericValue(setting));

                GUI.Label(new Rect(205f, y + 33f, 455f, 18f), "Default " + FormatValue(GetDefaultNumericValue(setting)) + "   |   Slider " + FormatValue(setting.SliderMin) + "-" + FormatValue(setting.SliderMax), _smallStyle);
                y += 62f;
            }

            GUI.EndScrollView();
        }

        private string FriendlyKey(string key)
        {
            string text = key.Replace("_v0109", "");
            text = text.Replace("Meters", "");
            text = text.Replace("Seconds", "");
            return text;
        }

        private void UpdateWorldPreview()
        {
            Player player = Player.m_localPlayer;
            DevSetting setting = FindSetting(_selectedSettingId);
            if (player == null || setting == null)
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
                    if (setting.Section != section)
                        continue;
                    string key = setting.Key.ToLowerInvariant();
                    if (key == wanted.ToLowerInvariant() || key.StartsWith(wanted.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase))
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
            float half = Mathf.Clamp(degrees * 0.5f, 0f, 89.5f);
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
