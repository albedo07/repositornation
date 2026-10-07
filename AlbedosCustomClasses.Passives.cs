using BepInEx;

namespace AlbedosCustomClassesPassives
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInDependency("albedo.customclasses", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("albedo.customclasses.combatruntime", BepInDependency.DependencyFlags.HardDependency)]
    public class PassivesPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "albedo.customclasses.passives";
        public const string ModName = "Dragon's Altar - Base Blessings Compatibility";
        public const string ModVersion = "0.25.57";

        private void Awake()
        {
            Logger.LogInfo(
                ModName + " v" + ModVersion +
                " loaded. Legacy Warrior's Might and Cleric health-regeneration passives are disabled; " +
                "the Dragon's Altar Combat Runtime now owns framework-defined Blessing behavior."
            );
        }
    }
}
