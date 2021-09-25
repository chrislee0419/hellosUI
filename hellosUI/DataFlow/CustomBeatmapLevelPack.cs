using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using IPA.Utilities;
using SongCore;

namespace HUI.DataFlow
{
    internal class CustomBeatmapLevelPack : IBeatmapLevelPack
    {
        public string packID { get; private set; }
        public string packName { get; private set; }
        public string shortPackName { get; private set; }
        public string collectionName => shortPackName;

        public Sprite coverImage { get; private set; }
        public Sprite smallCoverImage => coverImage;

        private BeatmapLevelCollection _beatmapLevelCollection = new BeatmapLevelCollection(Array.Empty<IPreviewBeatmapLevel>());
        public IBeatmapLevelCollection beatmapLevelCollection => _beatmapLevelCollection;

        public const string LevelPackID = CustomLevelLoader.kCustomLevelPackPrefixId + "HUICustomLevelPack";

        private static readonly FieldAccessor<BeatmapLevelCollection, IPreviewBeatmapLevel[]>.Accessor LevelsAccessor = FieldAccessor<BeatmapLevelCollection, IPreviewBeatmapLevel[]>.GetAccessor("_levels");

        public IBeatmapLevelPack SetupFromLevels(IAnnotatedBeatmapLevelCollection originalLevelCollection, IEnumerable<IPreviewBeatmapLevel> levels)
        {
            packID = LevelPackID;
            packName = originalLevelCollection.collectionName;
            shortPackName = LevelPackID;
            coverImage = originalLevelCollection.coverImage ?? Loader.defaultCoverImage;

            LevelsAccessor(ref _beatmapLevelCollection) = levels is IPreviewBeatmapLevel[] array ? array : levels.ToArray();

            return this;
        }
    }
}
