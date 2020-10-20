﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using MassProduction.Automate;
using MassProduction.VanillaOverrides;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathoschild.Stardew.Automate;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using SObject = StardewValley.Object;

namespace MassProduction
{
    public class ModEntry : Mod
    {
        public static Dictionary<string, MPMSettings> MPMSettings { get; private set; }
        public static List<MassProductionMachineDefinition> MPMDefinitionSet { get; private set; }
        public static MPMManager MPMManager { get; private set; }
        public static Dictionary<SObject, string> MassProducerRecord { get; private set; }
        public static ModEntry Instance;

        /// <summary>
        /// Runs when the mod is loaded.
        /// </summary>
        /// <param name="helper"></param>
        public override void Entry(IModHelper helper)
        {
            Instance = this;
            MPMSettings = new Dictionary<string, MPMSettings>();
            MPMDefinitionSet = new List<MassProductionMachineDefinition>();
            MassProducerRecord = new Dictionary<SObject, string>();

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.World.ObjectListChanged += World_ObjectListChanged;
        }

        /// <summary>
        /// Patches game through Harmony and sets up mod integration.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnGameLaunched(object sender, GameLaunchedEventArgs args)
        {
            HarmonyInstance harmony = HarmonyInstance.Create("JacquePott.MassProduction");

            harmony.Patch(
                original: AccessTools.Method(typeof(SObject), nameof(SObject.performObjectDropInAction)),
                prefix: new HarmonyMethod(typeof(ObjectOverrides), nameof(ObjectOverrides.PerformObjectDropInAction))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(SObject), nameof(SObject.draw), new Type[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) }),
                postfix: new HarmonyMethod(typeof(ObjectOverrides), nameof(ObjectOverrides.Draw_Postfix))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(SObject), nameof(SObject.isPlaceable)),
                postfix: new HarmonyMethod(typeof(ObjectOverrides), nameof(ObjectOverrides.isPlaceable_Postfix))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(SObject), nameof(SObject.performDropDownAction)),
                prefix: new HarmonyMethod(typeof(ObjectOverrides), nameof(ObjectOverrides.performDropDownAction_Prefix))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(SObject), nameof(SObject.DayUpdate)),
                prefix: new HarmonyMethod(typeof(ObjectOverrides), nameof(ObjectOverrides.DayUpdate_Prefix))
            );

            //Automate integration
            if (Helper.ModRegistry.IsLoaded("Pathoschild.Automate"))
            {
                IAutomateAPI automate = Helper.ModRegistry.GetApi<IAutomateAPI>("Pathoschild.Automate");
                automate.AddFactory(new MPMAutomationFactory());

                Assembly automateAssembly = AppDomain.CurrentDomain.GetAssemblies().First(a => a.FullName.StartsWith("Automate,"));
                Type automationFactoryType = automateAssembly.GetType("Pathoschild.Stardew.Automate.Framework.AutomationFactory");
                MethodInfo methodInfo = AccessTools.GetDeclaredMethods(automationFactoryType).Find(m => m.GetParameters().Any(p => p.ParameterType == typeof(SObject)));
                harmony.Patch(
                    original: methodInfo,
                    postfix: new HarmonyMethod(typeof(AutomateOverrides), nameof(AutomateOverrides.GetFor))
                );

                if (Helper.ModRegistry.IsLoaded("Digus.PFMAutomate"))
                {
                    Monitor.Log("Automate integration for Mass Production clashes with PFMAutomate. Please remove PFMAutomate " +
                        "to have this functionality work correctly.", LogLevel.Warn);
                }
            }
        }

        /// <summary>
        /// Sets up the mod.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs args)
        {
            //Clear out old data if any exists
            MPMSettings.Clear();
            MPMDefinitionSet.Clear();
            MassProducerRecord.Clear();
            if (MPMManager != null)
            {
                MPMManager.Clear();
            }

            //Load content packs
            Monitor.Log("Loading content packs...", LogLevel.Info);
            string filepath = "upgrades.json";
            foreach (IContentPack contentPack in Helper.ContentPacks.GetOwned())
            {
                if (contentPack.HasFile(filepath))
                {
                    List<MPMSettings> loaded = contentPack.ReadJsonFile<List<MPMSettings>>(filepath);

                    foreach (MPMSettings setting in loaded)
                    {
                        if (MPMSettings.ContainsKey(setting.Key))
                        {
                            Monitor.Log($"Content pack {contentPack.Manifest.Name} {contentPack.Manifest.Version} tried to add upgrade settings for existing key '{setting.Key}'! " +
                                "Settings left unchanged.", LogLevel.Warn);
                        }
                        else
                        {
                            MPMSettings.Add(setting.Key, setting);
                        }
                    }

                    Monitor.Log($"Loaded content pack {contentPack.Manifest.Name} {contentPack.Manifest.Version} by {contentPack.Manifest.Author}.",
                                LogLevel.Info);
                }
                else
                {
                    Monitor.Log($"Failed reading content pack {contentPack.Manifest.Name} {contentPack.Manifest.Version}: missing file {filepath}.", LogLevel.Warn);
                }
            }

            //Create mail to send recipes
            MailManager.SetupMail();

            //Set up machines to work with PFM
            Monitor.Log("Defining machines...", LogLevel.Info);
            MPMDefinitionSet = MassProductionMachineDefinition.Setup(MPMSettings);
            SeedMakerOverride.Initialize();

            //Start manager, loading saved data
            MPMManager = new MPMManager();
            
            Helper.Events.GameLoop.Saving += OnSave;
        }

        /// <summary>
        /// Gets all mass production machines based on another machine by name.
        /// </summary>
        /// <param name="baseMachineName"></param>
        /// <returns></returns>
        public static IEnumerable<MassProductionMachineDefinition> GetMPMachinesBasedOn(string baseMachineName)
        {
            return from mpm in MPMDefinitionSet
                   where mpm.BaseProducerName.Equals(baseMachineName)
                   select mpm;
        }

        /// <summary>
        /// Gets the mass production machine based on a machine and with this settings key. Returns null if none found.
        /// </summary>
        /// <param name="baseMachineName"></param>
        /// <param name="settingsKey"></param>
        /// <returns></returns>
        public static MassProductionMachineDefinition GetMPMMachine(string baseMachineName, string settingsKey)
        {
            if (string.IsNullOrEmpty(baseMachineName) || string.IsNullOrEmpty(settingsKey))
            {
                return null;
            }

            return (from mpm in GetMPMachinesBasedOn(baseMachineName)
                    where mpm.Settings.Key.Equals(settingsKey)
                    select mpm).FirstOrDefault();
        }

        /// <summary>
        /// Save this mod's extra data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSave(object sender, SavingEventArgs e)
        {
            MPMManager.Save();
        }

        /// <summary>
        /// When an object with a mass production upgrade is destroyed, drop that upgrade.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void World_ObjectListChanged(object sender, ObjectListChangedEventArgs e)
        {
            if (e.Removed.Count() > 0)
            {
                Dictionary<string, int> toReturn = new Dictionary<string, int>();
                Dictionary<string, List<Vector2>> toReturnCoords = new Dictionary<string, List<Vector2>>();

                foreach (var pair in e.Removed)
                {
                    if (!string.IsNullOrEmpty(pair.Value.GetMassProducerKey()))
                    {
                        if (toReturn.ContainsKey(pair.Value.GetMassProducerKey()))
                        {
                            toReturn[pair.Value.GetMassProducerKey()]++;
                            toReturnCoords[pair.Value.GetMassProducerKey()].Add(pair.Key);
                        }
                        else
                        {
                            toReturn.Add(pair.Value.GetMassProducerKey(), 1);
                            toReturnCoords.Add(pair.Value.GetMassProducerKey(), new List<Vector2>() { pair.Key });
                        }
                    }
                }

                foreach (string upgradeKey in toReturn.Keys)
                {
                    int itemId = MPMSettings[upgradeKey].UpgradeObjectID;

                    foreach (Vector2 coord in toReturnCoords[upgradeKey])
                    {
                        Game1.createItemDebris(new SObject(itemId, toReturn[upgradeKey]), coord * Game1.tileSize, 0, e.Location);
                        MPMManager.Remove(e.Location.name, coord);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the mass production machine settings based on the name of the item that provides that upgrade. Returns null if not found.
        /// </summary>
        /// <param name="itemName"></param>
        /// <returns></returns>
        public static MPMSettings GetSettingsFromItem(string itemName)
        {
            return (from setting in MPMSettings.Values
                    where setting.UpgradeObject.Equals(itemName)
                    select setting).FirstOrDefault();
        }
    }
}
