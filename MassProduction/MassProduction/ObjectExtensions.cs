using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SObject = StardewValley.Object;

namespace MassProduction
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Gets the mass producer identification string set for this object, or the empty string if none exists.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static string GetMassProducerKey(this SObject o)
        {
            return ModEntry.MassProducerRecord.ContainsKey(o) ? ModEntry.MassProducerRecord[o] : "";
        }

        /// <summary>
        /// Sets the mass producer identification string for this object.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="s"></param>
        public static void SetMassProducerKey(this SObject o, string s)
        {
            try
            {
                if (ModEntry.MassProducerRecord.ContainsKey(o))
                {
                    if (string.IsNullOrEmpty(s))
                    {
                        ModEntry.MassProducerRecord.Remove(o);
                    }
                    else
                    {
                        ModEntry.MassProducerRecord[o] = s;
                    }
                }
                else if (!string.IsNullOrEmpty(s))
                {
                    ModEntry.MassProducerRecord.Add(o, s);
                }

                ModEntry.MPMManager.OnMachineUpgradeKeyChanged(o);
            }
            catch (Exception e)
            {
                ModEntry.Instance.Monitor.Log(e.ToString(), StardewModdingAPI.LogLevel.Error);
            }
        }
    }
}
