using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SObject = StardewValley.Object;

namespace MassProduction
{
    /// <summary>
    /// Keeps track of where machines have been upgraded to mass production versions.
    /// </summary>
    public class MPMManager
    {
        public const string SAVE_KEY = "UpgradedMachineLocations";
        private Dictionary<string, string> UpgradedMachineLocations;

        /// <summary>
        /// Constructor.
        /// </summary>
        public MPMManager()
        {
            UpgradedMachineLocations = new Dictionary<string, string>();

            try
            {
                if (!Context.IsMultiplayer || Context.IsMainPlayer)
                {
                    SavedMPMInfo[] SavedData = ModEntry.Instance.Helper.Data.ReadSaveData<SavedMPMInfo[]>(SAVE_KEY);

                    if (SavedData != null)
                    {
                        foreach (SavedMPMInfo info in SavedData)
                        {
                            UpgradedMachineLocations.Add(info.GetIDString(), info.UpgradeKey);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ModEntry.Instance.Monitor.Log($"Error while loading:\n{e}", LogLevel.Error);
            }
        }

        /// <summary>
        /// To be called whenever an object's machine upgrade key is changed. Keeps record of the upgrades at every location.
        /// </summary>
        /// <param name="o"></param>
        public void OnMachineUpgradeKeyChanged(SObject o, string newUpgradeKey)
        {
            foreach (GameLocation location in Game1.locations)
            {
                if (location.objects.Values.Contains(o))
                {
                    string idString = GetIdString(location, o);

                    if (UpgradedMachineLocations.ContainsKey(idString))
                    {
                        UpgradedMachineLocations[idString] = newUpgradeKey;
                    }
                    else
                    {
                        UpgradedMachineLocations.Add(idString, newUpgradeKey);
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// Removes a machine from tracking by location and coordinates.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="o"></param>
        /// <returns>The upgrade key of the removed machine. Empty string or null if none found.</returns>
        public string Remove(GameLocation location, SObject o)
        {
            string idString = GetIdString(location, o);
            string upgradeKey = "";

            if (UpgradedMachineLocations.ContainsKey(idString))
            {
                upgradeKey = UpgradedMachineLocations[idString];
                UpgradedMachineLocations.Remove(idString);
            }

            return upgradeKey;
        }

        /// <summary>
        /// Gets the upgrade key for a given object.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public string GetUpgradeKey(SObject o)
        {
            foreach (GameLocation location in Game1.locations)
            {
                if (location.objects.Values.Contains(o))
                {
                    string idString = GetIdString(location, o);

                    if (UpgradedMachineLocations.ContainsKey(idString))
                    {
                        return UpgradedMachineLocations[idString];
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Saves the data to file.
        /// </summary>
        public void Save()
        {
            List<SavedMPMInfo> saveData = new List<SavedMPMInfo>();

            foreach (string idString in UpgradedMachineLocations.Keys)
            {
                string[] idParts = idString.Split('_');
                int yIndex = idParts.Length - 1;
                int xIndex = yIndex - 1;
                string locationName = string.Join("_", idParts.Take(idParts.Length - 2));

                SavedMPMInfo info = new SavedMPMInfo()
                {
                    LocationName = locationName,
                    CoordinateX = int.Parse(idParts[xIndex]),
                    CoordinateY = int.Parse(idParts[yIndex]),
                    UpgradeKey = UpgradedMachineLocations[idString]
                };

                saveData.Add(info);
            }

            ModEntry.Instance.Helper.Data.WriteSaveData(SAVE_KEY, saveData.ToArray());
        }

        /// <summary>
        /// Empties the tracking list.
        /// </summary>
        public void Clear()
        {
            UpgradedMachineLocations.Clear();
        }

        /// <summary>
        /// Converts an object and it's location into an identifier string to save it's upgrade key data.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="o"></param>
        /// <returns></returns>
        public static string GetIdString(GameLocation location, SObject o)
        {
            return $"{location.name}_{o.TileLocation.X}_{o.TileLocation.Y}";
        }
    }
}
