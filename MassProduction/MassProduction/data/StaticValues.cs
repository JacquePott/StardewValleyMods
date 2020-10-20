using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MassProduction
{
    public class StaticValues
    {
        /// <summary>
        /// All machines that need support outside of PFM-enabled ones.
        /// </summary>
        public static readonly List<string> SUPPORTED_VANILLA_MACHINES = new List<string>()
        {
            "Seed Maker"
        };
    }
}
