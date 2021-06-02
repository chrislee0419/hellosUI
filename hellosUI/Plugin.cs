using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IPA;
using IPA.Config.Stores;
using IPA.Utilities;
using HarmonyLib;
using SiraUtil.Zenject;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Tags;
using BeatSaberMarkupLanguage.TypeHandlers;
using HUI.Installers;
using IPALogger = IPA.Logging.Logger;
using Config = IPA.Config.Config;

namespace HUI
{
    [Plugin(RuntimeOptions.DynamicInit)]
    public class Plugin
    {
        internal static readonly Harmony harmony = new Harmony(HarmonyId);

        internal static Plugin Instance { get; private set; }
        internal static IPALogger Log { get; private set; }

        internal const string HarmonyId = "com.chrislee0419.BeatSaber.HUI";

        [Init]
        public Plugin(IPALogger logger, Config config, Zenjector zenjector)
        {
            Instance = this;
            Plugin.Log = logger;

            PluginConfig.Instance = config.Generated<PluginConfig>();

            zenjector.OnMenu<DataFlowInstaller>();
            zenjector.OnMenu<SortModeInstaller>();
            zenjector.OnMenu<UIInstaller>();
        }

        [OnEnable]
        public void OnEnable()
        {
            ApplyHarmonyPatches();

            // register custom bsml stuff
            foreach (var customTag in GetListOfType<BSMLTag>())
                BSMLParser.instance.RegisterTag(customTag);

            foreach (var customTypeHandler in GetListOfType<TypeHandler>())
                BSMLParser.instance.RegisterTypeHandler(customTypeHandler);
        }

        [OnDisable]
        public void OnDisable()
        {
            RemoveHarmonyPatches();

            // since bsml doesn't expose anything for unregistering stuff,
            // we've got to do it ourselves using reflection
            BSMLParser bsmlParser = BSMLParser.instance;
            var tags = FieldAccessor<BSMLParser, Dictionary<string, BSMLTag>>.Get(ref bsmlParser, "tags");
            var typeHandlers = FieldAccessor<BSMLParser, List<TypeHandler>>.Get(ref bsmlParser, "typeHandlers");

            foreach (var customTag in GetListOfType<BSMLTag>())
            {
                foreach (var alias in customTag.Aliases)
                    tags.Remove(alias);
            }

            foreach (var typeHandler in GetListOfType<TypeHandler>())
                typeHandlers.Remove(typeHandler);
        }

        private List<T> GetListOfType<T>()
        {
            Type type = typeof(T);
            return Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(type))
                .Select(t => (T)Activator.CreateInstance(t))
                .ToList();
        }

        internal static void ApplyHarmonyPatches()
        {
            try
            {
                Plugin.Log?.Debug("Applying Harmony patches.");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error("Error applying Harmony patches: " + ex.Message);
                Plugin.Log?.Debug(ex);
            }
        }

        internal static void RemoveHarmonyPatches()
        {
            try
            {
                // Removes all patches with this HarmonyId
                harmony.UnpatchAll(HarmonyId);
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error("Error removing Harmony patches: " + ex.Message);
                Plugin.Log?.Debug(ex);
            }
        }
    }
}
