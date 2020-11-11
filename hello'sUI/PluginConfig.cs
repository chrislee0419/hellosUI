using System.Runtime.CompilerServices;
using IPA.Config.Stores;
using IPA.Config.Stores.Attributes;
using IPA.Config.Stores.Converters;
using static HUI.Search.WordSearchEngine;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace HUI
{
    internal class PluginConfig
    {
        public static PluginConfig Instance { get; set; }

        public virtual int FastScrollSpeed { get; set; } = 5;

        [NonNullable]
        public virtual SearchSettings Search { get; set; } = new SearchSettings();

        ///// <summary>
        ///// This is called whenever BSIPA reads the config from disk (including when file changes are detected).
        ///// </summary>
        //public virtual void OnReload()
        //{
        //    // Do stuff after config is read from disk.
        //}

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

        public class SearchSettings
        {
            public virtual bool StripSymbols { get; set; } = StripSymbolsDefaultValue;
            public const bool StripSymbolsDefaultValue = false;

            public virtual bool SplitQueryByWords { get; set; } = SplitQueryByWordsDefaultValue;
            public const bool SplitQueryByWordsDefaultValue = true;

            [UseConverter(typeof(EnumConverter<SearchableSongFields>))]
            public virtual SearchableSongFields SongFieldsToSearch { get; set; } = SongFieldsToSearchDefaultValue;
            public const SearchableSongFields SongFieldsToSearchDefaultValue =
                SearchableSongFields.SongName | SearchableSongFields.SongAuthor | SearchableSongFields.LevelAuthor | SearchableSongFields.Contributors;
        }
    }
}