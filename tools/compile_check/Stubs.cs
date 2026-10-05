using System;
using UnityEngine;
namespace BepInEx.Logging { public class ManualLogSource { public void LogInfo(object o){} public void LogWarning(object o){} public void LogError(object o){} public void LogDebug(object o){} public void LogMessage(object o){} } }
namespace BepInEx.Configuration {
  public class ConfigDescription { public ConfigDescription(string d, AcceptableValueBase a = null, params object[] t){} public string Description; public AcceptableValueBase AcceptableValues; }
  public class AcceptableValueBase {}
  public class AcceptableValueRange<T> : AcceptableValueBase { public AcceptableValueRange(T a, T b){} public T MinValue; public T MaxValue; }
  public abstract class ConfigEntryBase { public object DefaultValue; public object BoxedValue; public ConfigDefinition Definition; public ConfigDescription Description; public Type SettingType; }
  public class ConfigDefinition { public ConfigDefinition(){} public ConfigDefinition(string s, string k){} public string Section; public string Key; }
  public class ConfigEntry<T> : ConfigEntryBase { public T Value; public event EventHandler SettingChanged; }
  public class ConfigFile { public ConfigEntry<T> Bind<T>(string s, string k, T d, string desc){ return null; } public ConfigEntry<T> Bind<T>(string s, string k, T d, ConfigDescription desc){ return null; } public void Save(){} public void Reload(){} public bool SaveOnConfigSet; }
}
namespace BepInEx {
  public class BaseUnityPlugin : MonoBehaviour { public BepInEx.Configuration.ConfigFile Config; protected BepInEx.Logging.ManualLogSource Logger; }
  [AttributeUsage(AttributeTargets.Class, AllowMultiple=true)] public class BepInPlugin : Attribute { public BepInPlugin(string a,string b,string c){} }
  [AttributeUsage(AttributeTargets.Class, AllowMultiple=true)] public class BepInDependency : Attribute { public BepInDependency(string a){} public BepInDependency(string a, DependencyFlags f){} public enum DependencyFlags { HardDependency=1, SoftDependency=2 } }
  public static class Paths { public static string PluginPath; public static string ConfigPath; }
}
public class Character : MonoBehaviour { public object GetSEMan() { return null; } public bool IsBlocking() { return false; } } public class Humanoid : Character { public void UnequipItem(ItemDrop.ItemData item, bool triggerEquipEffects) {} } public class Player : Humanoid { public static Player m_localPlayer; public string GetPlayerName(){ return ""; } public bool InDodge(){ return false; } public float GetSkillFactor(Skills.SkillType skill){ return 0f; } public float GetStamina(){ return 0f; } public float GetMaxStamina(){ return 0f; } } public class HitData { public enum DamageModifier { Normal } public struct DamageModifiers {} } public class Attack {} public class StatusEffect : ScriptableObject { public string m_name; public string m_tooltip; public Sprite m_icon; public float m_ttl; public virtual string GetIconText() { return ""; } } public class Skills { public enum SkillType { None, Swords, Knives, Clubs, Polearms, Spears, Blocking, Axes, Bows, ElementalMagic, BloodMagic, Unarmed, Pickaxes, WoodCutting, Crossbows, Jump, Run } } public class ItemDrop : MonoBehaviour { public class ItemData { public int m_stack; } public ItemData m_itemData; } public interface Hoverable {} public interface Interactable {}
namespace DragonsAltarCombat { public class _Stub {} }
namespace AlbedosCustomClassesSkills { public class _Stub {} }
namespace AlbedosCustomClasses { public class Plugin { public static bool IsClassPanelOpen; } }
public class Terminal : MonoBehaviour { public void AddString(string s){} public delegate void ConsoleEvent(ConsoleEventArgs args); public class ConsoleEventArgs { public string[] Args; public string FullLine; public Terminal Context; } public class ConsoleCommand { public ConsoleCommand(string command, string description, ConsoleEvent action, bool isCheat = false, bool isNetwork = false, bool onlyServer = false, bool isSecret = false, bool allowInDevBuild = false, object optionsFetcher = null, bool alwaysRefreshTabOptions = false, bool remoteCommand = false, bool onlyAdmin = false){} } }
public class ZNet : MonoBehaviour { public static ZNet instance; public bool IsServer(){ return true; } }
namespace BepInEx { public class PluginInfo { public BaseUnityPlugin Instance; } }
namespace BepInEx.Bootstrap { public static class Chainloader { public static System.Collections.Generic.Dictionary<string, BepInEx.PluginInfo> PluginInfos; } }
