using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using IPA.Config.Stores;
using IPA.Config.Stores.Attributes;
using System;
using IPA.Config.Stores.Converters;
using SiraUtil.Converters;
using HUI.Converters;
using HUI.UI.Settings;
using HUI.Utilities;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace HUI
{
    internal class PluginConfig
    {
        public event Action ConfigReloaded;

        public static PluginConfig Instance { get; set; }

        public virtual int FastScrollSpeed { get; set; } = 5;

        [UseConverter(typeof(EnumConverter<SelectLevelCategoryViewController.LevelCategory>))]
        public virtual SelectLevelCategoryViewController.LevelCategory LastLevelCategory { get; set; } = SelectLevelCategoryViewController.LevelCategory.None;

        public virtual string LastLevelPackID { get; set; }

        public virtual string LastLevelID { get; set; }
        public const string LastLevelIDSeparator = "|||;;;";
        public static readonly string[] LastLevelIDSeparatorArray = new string[] { LastLevelIDSeparator };

        public virtual ScreensSettings Screens { get; set; } = new ScreensSettings();

        public class ScreensSettings
        {
            [UseConverter(typeof(HexColorConverter))]
            public virtual Color ScreenBackgroundColour { get; set; } = ScreenBackgroundColourDefaultValue;
            public static readonly Color ScreenBackgroundColourDefaultValue = new Color(0.5f, 0.5f, 0.5f);

            [NonNullable]
            [UseConverter(typeof(DictionaryConverter<ScreensSettingsTab.BackgroundOpacity, EnumConverter<ScreensSettingsTab.BackgroundOpacity>>))]
            public virtual Dictionary<string, ScreensSettingsTab.BackgroundOpacity> ScreenOpacities { get; set; } = new Dictionary<string, ScreensSettingsTab.BackgroundOpacity>();

            [NonNullable]
            [UseConverter(typeof(DictionaryConverter<Vector3, Vector3Converter>))]
            public virtual Dictionary<string, Vector3> ScreenPositions { get; set; } = new Dictionary<string, Vector3>();

            [NonNullable]
            [UseConverter(typeof(DictionaryConverter<Quaternion, QuaternionConverter>))]
            public virtual Dictionary<string, Quaternion> ScreenRotations { get; set; } = new Dictionary<string, Quaternion>();
        }

        [NonNullable]
        public virtual SortSettings Sort { get; set; } = new SortSettings();

        public class SortSettings
        {
            public virtual string LastSortModeID { get; set; }

            public virtual bool LastSortModeIsAscending { get; set; }

            public virtual bool HideUnavailable { get; set; } = false;

            [NonNullable]
            [UseConverter(typeof(ISetConverter<string>))]
            public virtual ISet<string> HiddenSortModes { get; set; } = new HashSet<string>();

            [NonNullable]
            [UseConverter(typeof(ListConverter<string>))]
            public virtual List<string> SortModeOrdering { get; set; } = new List<string>();
        }

        /// <summary>
        /// This is called whenever BSIPA reads the config from disk (including when file changes are detected).
        /// </summary>
        public virtual void OnReload() => this.CallAndHandleAction(ConfigReloaded, nameof(ConfigReloaded));

        ///// <summary>
        ///// Call this to force BSIPA to update the config file. This is also called by BSIPA if it detects the file was modified.
        ///// </summary>
        //public virtual void Changed()
        //{
        //    // Do stuff when the config is changed.
        //}

        ///// <summary>
        ///// Call this to have BSIPA copy the values from <paramref name="other"/> into this config.
        ///// </summary>
        //public virtual void CopyFrom(PluginConfig other)
        //{
        //    // This instance's members populated from other
        //}
    }
}