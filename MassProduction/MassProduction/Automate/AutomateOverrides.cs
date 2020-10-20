using Pathoschild.Stardew.Automate;
using ProducerFrameworkMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SObject = StardewValley.Object;

namespace MassProduction.Automate
{
    public class AutomateOverrides
    {
        public static readonly HashSet<string> SupportedVanillaMachines = new HashSet<string>()
        {
            "Pathoschild.Stardew.Automate.Framework.Machines.Objects.SeedMakerMachine"
        };
        
        public static void GetFor(ref object __result, SObject obj)
        {
            if (__result != null && __result is IMachine machine)
            {
                string machineFullName = __result.GetType().FullName;

                if (SupportedVanillaMachines.Contains(machineFullName) && (ProducerController.HasProducerRule(obj.Name) || ProducerController.GetProducerConfig(obj.Name) != null))
                {
                    __result = new VanillaAutomatedOverride((IMachine)__result);
                }
            }
        }
    }
}
