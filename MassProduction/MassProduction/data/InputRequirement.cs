using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MassProduction
{
    /// <summary>
    /// Whether or not the machine upgrade supports machines with inputs or machines with no inputs.
    /// </summary>
    public enum InputRequirement
    {
        InputRequired, NoInputsOnly, NoRequirements
    }
}
