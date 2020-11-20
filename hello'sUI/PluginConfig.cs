using System.Runtime.CompilerServices;
using UnityEngine;
using IPA.Config.Stores;
using IPA.Config.Stores.Attributes;
using System;
using IPA.Config.Stores.Converters;
using SiraUtil.Converters;
using HUI.Converters;
using HUI.UI.Screens;
using HUI.Utilities;
using static HUI.Search.WordSearchEngine;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace HUI
{
    internal class PluginConfig
    {
        public event Action ConfigReloaded;

        public static PluginConfig Instance { get; set; }

        public virtual int FastScrollSpeed { get; set; } = 5;

        [NonNullable]
        public virtual SearchSettings Search { get; set; } = new SearchSettings();

        public class SearchSettings
        {
            [UseConverter(typeof(Vector3Converter))]
            public virtual Vector3 KeyboardPosition { get; set; } = SearchKeyboardScreenManager.DefaultKeyboardPosition;

            [UseConverter(typeof(QuaternionConverter))]
            public virtual Quaternion KeyboardRotation { get; set; } = SearchKeyboardScreenManager.DefaultKeyboardRotation;

            public virtual bool CloseScreenOnSelectLevel { get; set; } = CloseScreenOnSelectLevelDefaultValue;
            public const bool CloseScreenOnSelectLevelDefaultValue = true;

            public virtual bool CloseScreenOnSelectLevelCollection { get; set; } = CloseScreenOnSelectLevelCollectionDefaultValue;
            public const bool CloseScreenOnSelectLevelCollectionDefaultValue = true;

            public virtual bool ClearQueryOnSelectLevelCollection { get; set; } = ClearQueryOnSelectLevelCollectionDefaultValue;
            public const bool ClearQueryOnSelectLevelCollectionDefaultValue = false;

            public virtual bool UseOffHandLaserPointer { get; set; } = UseOffHandLaserPointerDefaultValue;
            public const bool UseOffHandLaserPointerDefaultValue = true;

            public virtual bool StripSymbols { get; set; } = StripSymbolsDefaultValue;
            public const bool StripSymbolsDefaultValue = false;

            public virtual bool SplitQueryByWords { get; set; } = SplitQueryByWordsDefaultValue;
            public const bool SplitQueryByWordsDefaultValue = true;

            [UseConverter(typeof(EnumConverter<SearchableSongFields>))]
            public virtual SearchableSongFields SongFieldsToSearch { get; set; } = SongFieldsToSearchDefaultValue;
            public const SearchableSongFields SongFieldsToSearchDefaultValue =
                SearchableSongFields.SongName | SearchableSongFields.SongAuthor | SearchableSongFields.LevelAuthor | SearchableSongFields.Contributors;
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