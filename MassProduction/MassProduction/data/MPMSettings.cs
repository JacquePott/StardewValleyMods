using MailFrameworkMod;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MassProduction
{
    /// <summary>
    /// Configurable settings for a MassProductionMachine.
    /// </summary>
    public class MPMSettings
    {
        public static readonly double STANDARD_BASE_MULTIPLIER = 10.0; //Ten times more input and output used over the normal machines

        public string Key { get; set; }
        public string UpgradeObject { get; set; }
        public double BaseMultiplier { get; set; } = STANDARD_BASE_MULTIPLIER;
        public int InputStaticChange { get; set; } = 0;
        public int OutputStaticChange { get; set; } = 0;
        public double InputMultiplier { get; set; } = 0.0;
        public double OutputMultiplier { get; set; } = 0.0;
        public double OutputMultiplierMin { get; set; } = 0.0;
        public double OutputMultiplierMax { get; set; } = 0.0;
        public double TimeMultiplier { get; set; } = 1.0;
        public string InputRequirement { get; set; } = "InputRequired";
        public QualitySetting Quality { get; set; } = QualitySetting.NoStars;
        public Dictionary<string, object> UnlockConditions { get; set; }
        public string[] FuelIgnore { get; set; } = new string[0];

        public int UpgradeObjectID
        {
            get
            {
                if (!string.IsNullOrEmpty(UpgradeObject))
                {
                    JsonAssets.Api jsonAssets = ModEntry.Instance.Helper.ModRegistry.GetApi("spacechase0.JsonAssets") as JsonAssets.Api;
                    int upgradeObjectId = jsonAssets.GetObjectId(UpgradeObject);
                    return upgradeObjectId;
                }
                return -1;
            }
        }
        public InputRequirement InputRequirementEnum
        {
            get
            {
                return (InputRequirement)Enum.Parse(typeof(InputRequirement), InputRequirement);
            }
        }

        /// <summary>
        /// Finds what new amount of input is required.
        /// </summary>
        /// <param name="baseInputStack"></param>
        /// <returns></returns>
        public int CalculateInputRequired(int baseInputStack)
        {
            if (baseInputStack == 0) { return 0; }

            double multiplier = BaseMultiplier + InputMultiplier;
            if (multiplier < 1.0) { multiplier = 1.0; }
            int inputRequired = (int)Math.Ceiling(baseInputStack * multiplier) + InputStaticChange;
            if (inputRequired < 1) { inputRequired = 1; }

            return inputRequired;
        }

        /// <summary>
        /// Find what new input is required. Used for fuel specifically to allow ignoring fuel requirements.
        /// </summary>
        /// <param name="baseInputStack"></param>
        /// <param name="fuelID"></param>
        /// <param name="fuelName"></param>
        /// <returns></returns>
        public int CalculateInputRequired(int baseInputStack, int fuelID)
        {
            Dictionary<int, string> objects = ModEntry.Instance.Helper.Content.Load<Dictionary<int, string>>("Data\\ObjectInformation", ContentSource.GameContent);
            string fuelName = PFMCompatability.GetObjectName(objects[fuelID]);

            if (FuelIgnore.Contains(fuelID.ToString()) || FuelIgnore.Contains(fuelName))
            {
                return 0;
            }
            else
            {
                return CalculateInputRequired(baseInputStack);
            }
        }

        /// <summary>
        /// Finds what new amount of output is produced.
        /// </summary>
        /// <param name="baseOutputStack"></param>
        /// <returns></returns>
        public int CalculateOutputProduced(int baseOutputStack)
        {
            double multiplier = BaseMultiplier + OutputMultiplier + GetRandomDouble(OutputMultiplierMin, OutputMultiplierMax);
            if (multiplier < 1.0) { multiplier = 1.0; }
            int outputProduced = (int)Math.Ceiling(baseOutputStack * multiplier) + OutputStaticChange;
            if (outputProduced < 1) { outputProduced = 1; }

            return outputProduced;
        }

        /// <summary>
        /// Calculates the new time required per operation.
        /// </summary>
        /// <param name="baseTime"></param>
        /// <returns></returns>
        public int CalculateTimeRequired(int baseTime)
        {
            int timeRequired = (int)Math.Round((baseTime / 10.0) * TimeMultiplier) * 10;

            return timeRequired;
        }

        /// <summary>
        /// Gets what quality output will be used.
        /// </summary>
        /// <returns></returns>
        public int GetOutputQuality()
        {
            return (Quality == QualitySetting.KeepInput) ? 0 : (int)Quality;
        }

        /// <summary>
        /// Checked by MailFrameworkMod to see if a recipe can be sent to the player.
        /// </summary>
        /// <param name="letter"></param>
        /// <returns></returns>
        public bool CheckIfRecipeCanBeLearned(Letter letter)
        {
            try
            {
                if (Game1.player.knowsRecipe(letter.Recipe))
                {
                    return false;
                }
                else if (UnlockConditions.Count > 0)
                {
                    if (UnlockConditions.ContainsKey("TotalEarnings") &&
                        Game1.player.totalMoneyEarned < int.Parse(UnlockConditions["TotalEarnings"].ToString()))
                    {
                        return false;
                    }

                    if (UnlockConditions.ContainsKey("UnlockedUpgrade"))
                    {
                        string upgradeKey = UnlockConditions["UnlockedUpgrade"].ToString();
                        string upgradeObjectName = ModEntry.MPMSettings[upgradeKey].UpgradeObject;

                        if (!Game1.player.knowsRecipe(upgradeObjectName))
                        {
                            return false;
                        }
                    }

                    if (UnlockConditions.ContainsKey("IsEndgame") && bool.Parse(UnlockConditions["IsEndgame"].ToString()))
                    {
                        bool jojaComplete = false;

                        if (Game1.MasterPlayer.mailReceived.Contains("JojaMember"))
                        {
                            GameLocation town = Game1.getLocationFromName("Town");
                            jojaComplete = ModEntry.Instance.Helper.Reflection.GetMethod(town, "checkJojaCompletePrerequisite").Invoke<bool>();
                        }

                        bool isEndgame = jojaComplete || Game1.MasterPlayer.mailReceived.Contains("ccIsComplete") || Game1.MasterPlayer.hasCompletedCommunityCenter();

                        if (!isEndgame)
                        {
                            return false;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ModEntry.Instance.Monitor.Log($"Error in checking if recipe could be learned:\n{e}", StardewModdingAPI.LogLevel.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets a random double in a range.
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        private double GetRandomDouble(double min, double max)
        {
            if (max <= min)
            {
                return max;
            }

            return Game1.random.NextDouble() * (max - min) + min;
        }
    }
}
