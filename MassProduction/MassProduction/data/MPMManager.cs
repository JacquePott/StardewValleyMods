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
        private List<SavedMPMInfo> UpgradedMachineLocations;

        /// <summary>
        /// Constructor.
        /// </summary>
        public MPMManager()
        {
            UpgradedMachineLocations = new List<SavedMPMInfo>();

            try
            {
                if (!Context.IsMultiplayer || Context.IsMainPlayer)
                {
                    SavedMPMInfo[] SavedData = ModEntry.Instance.Helper.Data.ReadSaveData<SavedMPMInfo[]>(SAVE_KEY);

                    if (SavedData != null)
                    {
                        UpgradedMachineLocations.AddRange(SavedData);
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
                if (location.Objects.ContainsKey(o.TileLocation) && location.Objects[o.TileLocation].Equals(o))
                {
                    IEnumerable<SavedMPMInfo> query = from info in UpgradedMachineLocations
                                                      where info.LocationName.Equals(location.Name) && info.GetCoordinates().Equals(o.TileLocation)
                                                      select info;
                    if (query.Count() > 0)
                    {
                        if (string.IsNullOrEmpty(newUpgradeKey))
                        {
                            UpgradedMachineLocations.Remove(query.First());
                        }
                        else
                        {
                            query.First().UpgradeKey = newUpgradeKey;
                        }
                    }
                    else if (!string.IsNullOrEmpty(newUpgradeKey))
                    {
                        UpgradedMachineLocations.Add(new SavedMPMInfo()
                        {
                            LocationName = location.Name,
                            CoordinateX = (int)o.TileLocation.X,
                            CoordinateY = (int)o.TileLocation.Y,
                            UpgradeKey = newUpgradeKey
                        });
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// Removes a machine from tracking by location and coordinates.
        /// </summary>
        /// <param name="locationName"></param>
        /// <param name="coordinates"></param>
        /// <returns>The upgrade key of the removed machine. Empty string if none found.</returns>
        public string Remove(string locationName, Vector2 coordinates)
        {
            IEnumerable<SavedMPMInfo> query = from info in UpgradedMachineLocations
                                              where info.LocationName.Equals(locationName) && info.GetCoordinates().Equals(coordinates)
                                              select info;
            string upgradeKey = "";

            foreach (SavedMPMInfo info in query.ToArray())
            {
                UpgradedMachineLocations.Remove(info);
                upgradeKey = info.UpgradeKey;
                ModEntry.Instance.Monitor.Log($"Removed machine in {locationName} ({coordinates}) from tracking.", LogLevel.Debug);
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
                if (location.Objects.ContainsKey(o.TileLocation) && location.Objects[o.TileLocation].Equals(o))
                {
                    IEnumerable<SavedMPMInfo> query = from info in UpgradedMachineLocations
                                                      where info.LocationName.Equals(location.Name) && info.GetCoordinates().Equals(o.TileLocation)
                                                      select info;
                    if (query.Count() > 0)
                    {
                        return query.First().UpgradeKey;
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
            ModEntry.Instance.Helper.Data.WriteSaveData(SAVE_KEY, UpgradedMachineLocations.ToArray());
        }

        /// <summary>
        /// Empties the tracking list.
        /// </summary>
        public void Clear()
        {
            UpgradedMachineLocations.Clear();
        }
    }
}
