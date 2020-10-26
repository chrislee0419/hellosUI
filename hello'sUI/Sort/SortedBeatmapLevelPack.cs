using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using IPA.Utilities;
using SongCore;

namespace HUI.Sort
{
    internal class SortedBeatmapLevelPack : IBeatmapLevelPack
    {
        public string packID { get; private set; }
        public string packName { get; private set; }
        public string shortPackName { get; private set; }
        public string collectionName => shortPackName;

        public Sprite coverImage { get; private set; }

        private BeatmapLevelCollection _beatmapLevelCollection = new BeatmapLevelCollection(Array.Empty<IPreviewBeatmapLevel>());
        public IBeatmapLevelCollection beatmapLevelCollection => _beatmapLevelCollection;

        private const string SortedLevelPackID = CustomLevelLoader.kCustomLevelPackPrefixId + "HUISortedLevelPack";

        private static readonly FieldAccessor<BeatmapLevelCollection, IPreviewBeatmapLevel[]>.Accessor LevelsAccessor = FieldAccessor<BeatmapLevelCollection, IPreviewBeatmapLevel[]>.GetAccessor("_levels");

        public IBeatmapLevelPack SetupFromLevels(IAnnotatedBeatmapLevelCollection originalLevelCollection, IEnumerable<IPreviewBeatmapLevel> levels)
        {
            packID = SortedLevelPackID;
            packName = originalLevelCollection.collectionName;
            shortPackName = SortedLevelPackID;
            coverImage = originalLevelCollection.coverImage ?? Loader.defaultCoverImage;

            LevelsAccessor(ref _beatmapLevelCollection) = levels is IPreviewBeatmapLevel[] array ? array : levels.ToArray();

            return this;
        }
    }
}
