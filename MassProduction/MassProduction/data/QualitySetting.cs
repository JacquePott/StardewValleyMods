using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MassProduction
{
    /// <summary>
    /// What quality of output does the machine produce? Overrides definitions in base machine's PFM settings.
    /// </summary>
    public enum QualitySetting
    {
        NoStars = 1, Silver = 2, Gold = 3, Iridium = 4, KeepInput = -1
    }
}
