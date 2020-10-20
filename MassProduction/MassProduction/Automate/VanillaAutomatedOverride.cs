using MassProduction.VanillaOverrides;
using Microsoft.Xna.Framework;
using Pathoschild.Stardew.Automate;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SObject = StardewValley.Object;

namespace MassProduction.Automate
{
    /// <summary>
    /// Overrides the vanilla machine implementations used in the base Automate.
    /// </summary>
    public class VanillaAutomatedOverride : IMachine
    {
        public string MachineTypeID { get { return "MassProduction.Override." + OriginalMachine.MachineTypeID; } }
        public GameLocation Location { get { return OriginalMachine.Location; } }
        public Rectangle TileArea { get { return OriginalMachine.TileArea; } }

        public IMachine OriginalMachine;
        public SObject OriginalMachineObject;
        public MPMAutomated OverridingMachine;

        public VanillaAutomatedOverride(IMachine original)
        {
            OriginalMachine = original;
            OriginalMachineObject = ModEntry.Instance.Helper.Reflection.GetProperty<SObject>(original, "Machine").GetValue();
            OverridingMachine = new MPMAutomated(OriginalMachineObject, Location, OriginalMachineObject.TileLocation);
        }

        public ITrackedStack GetOutput()
        {
            return OverridingMachine.GetOutput();
        }

        public MachineState GetState()
        {
            return OverridingMachine.GetState();
        }

        public bool SetInput(IStorage input)
        {
            if (OverridingMachine.SetInput(input))
            {
                return true;
            }
            else
            {
                //Check for special overriding logic, if any is required
                string originalClassName = OriginalMachine.GetType().FullName;

                if (VanillaOverrideList.GetFor(originalClassName) != null)
                {
                    IVanillaOverride vanillaOverride = VanillaOverrideList.GetFor(originalClassName);
                    MassProductionMachineDefinition mpm = ModEntry.GetMPMMachine(OriginalMachineObject.name, OriginalMachineObject.GetMassProducerKey());

                    return vanillaOverride.Automate_SetInput(input, mpm, OriginalMachine, OriginalMachineObject);
                }

                return OriginalMachine.SetInput(input);
            }
        }
    }
}
