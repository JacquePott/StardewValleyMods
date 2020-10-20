using Pathoschild.Stardew.Automate;
using StardewValley;
using System.Collections.Generic;
using SObject = StardewValley.Object;

namespace MassProduction.VanillaOverrides
{
    /// <summary>
    /// Defines methods for overriding a vanilla machine's functionality to implement the effects of mass production upgrades.
    /// </summary>
    public interface IVanillaOverride
    {
        /// <summary>
        /// Replacement for Automate's IMachine.SetInput(). 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="mpm"></param>
        /// <param name="originalMachine"></param>
        /// <param name="originalMachineObject"></param>
        /// <returns></returns>
        bool Automate_SetInput(IStorage input, MassProductionMachineDefinition mpm, IMachine originalMachine, SObject originalMachineObject);

        /// <summary>
        /// Replacement for the object drop in action.
        /// </summary>
        /// <param name="machine"></param>
        /// <param name="input"></param>
        /// <param name="probe"></param>
        /// <param name="who"></param>
        /// <param name="mpm"></param>
        /// <returns></returns>
        bool Manual_PerformObjectDropInAction(SObject machine, SObject input, bool probe, Farmer who, MassProductionMachineDefinition mpm);
    }

    public class VanillaOverrideList
    {
        public static readonly Dictionary<string, IVanillaOverride> Overrides = new Dictionary<string, IVanillaOverride>()
        {
            { "Seed Maker", new SeedMakerOverride() }
        };

        public static readonly Dictionary<string, string> AUTOMATE_OVERRIDES = new Dictionary<string, string>()
        {
            { "Pathoschild.Stardew.Automate.Framework.Machines.Objects.SeedMakerMachine", "Seed Maker" }
        };

        /// <summary>
        /// Gets the appropriate override for the given machine.
        /// </summary>
        /// <param name="machineName">Name of the base machine as appears in-game or the full namespace qualified class name of an Automate.IMachine</param>
        /// <returns>Null if no override exists.</returns>
        public static IVanillaOverride GetFor(string machineName)
        {
            if (Overrides.ContainsKey(machineName))
            {
                return Overrides[machineName];
            }
            else if (AUTOMATE_OVERRIDES.ContainsKey(machineName) && Overrides.ContainsKey(AUTOMATE_OVERRIDES[machineName]))
            {
                return Overrides[AUTOMATE_OVERRIDES[machineName]];
            }

            return null;
        }
    }
}
