using System;
using UnityEngine;
namespace BepInEx.Logging { public class ManualLogSource { public void LogInfo(object o){} public void LogWarning(object o){} public void LogError(object o){} public void LogDebug(object o){} public void LogMessage(object o){} } }
namespace BepInEx.Configuration {
  public class ConfigDescription { public ConfigDescription(string d, AcceptableValueBase a = null, params object[] t){} }
  public class AcceptableValueBase {}
  public class AcceptableValueRange<T> : AcceptableValueBase { public AcceptableValueRange(T a, T b){} }
  public abstract class ConfigEntryBase { public object DefaultValue; public object BoxedValue; public ConfigDefinition Definition; }
  public class ConfigDefinition { public string Section; public string Key; }
  public class ConfigEntry<T> : ConfigEntryBase { public T Value; public event EventHandler SettingChanged; }
  public class ConfigFile { public ConfigEntry<T> Bind<T>(string s, string k, T d, string desc){ return null; } public ConfigEntry<T> Bind<T>(string s, string k, T d, ConfigDescription desc){ return null; } public void Save(){} public bool SaveOnConfigSet; }
}
namespace BepInEx {
  public class BaseUnityPlugin : MonoBehaviour { protected BepInEx.Configuration.ConfigFile Config; protected BepInEx.Logging.ManualLogSource Logger; }
  [AttributeUsage(AttributeTargets.Class, AllowMultiple=true)] public class BepInPlugin : Attribute { public BepInPlugin(string a,string b,string c){} }
  [AttributeUsage(AttributeTargets.Class, AllowMultiple=true)] public class BepInDependency : Attribute { public BepInDependency(string a){} public BepInDependency(string a, DependencyFlags f){} public enum DependencyFlags { HardDependency=1, SoftDependency=2 } }
  public static class Paths { public static string PluginPath; public static string ConfigPath; }
}
public class Character : MonoBehaviour { public object GetSEMan() { return null; } } public class Humanoid : Character {} public class Player : Humanoid { public static Player m_localPlayer; } public class HitData { public enum DamageModifier { Normal } public struct DamageModifiers {} } public class Attack {} public class Skills { public enum SkillType { None, Swords, Knives, Clubs, Polearms, Spears, Blocking, Axes, Bows, ElementalMagic, BloodMagic, Unarmed, Pickaxes, WoodCutting, Crossbows, Jump, Run } } public class ItemDrop : MonoBehaviour { public class ItemData {} }
namespace DragonsAltarCombat { public class _Stub {} }
namespace AlbedosCustomClassesSkills { public class _Stub {} }
namespace AlbedosCustomClasses { public class Plugin { public static bool IsClassPanelOpen; } }
