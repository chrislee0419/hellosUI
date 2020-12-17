using System;
using System.Collections.Generic;

namespace HUI.Interfaces
{
    public interface ILevelCollectionModifier
    {
        /// <summary>
        /// Used to request a level collection refresh when the settings of this modifier have changed.
        /// </summary>
        event Action LevelCollectionRefreshRequested;

        /// <summary>
        /// Perform some action when the user selects a level collection.
        /// </summary>
        /// <param name="annotatedBeatmapLevelCollection">The beatmap level collection selected by the user.</param>
        void OnLevelCollectionSelected(IAnnotatedBeatmapLevelCollection annotatedBeatmapLevelCollection);

        /// <summary>
        /// Makes changes to a level collection when the user selects a level collection or 
        /// when a refresh is requested in code.
        /// 
        /// <para>
        /// NOTE: levels will be sorted last by the <see cref="Sort.SongSortManager"/>, so any changes to the
        /// ordering of levels will be lost unless sort mode is set to Default.
        /// </para>
        /// 
        /// <para>
        /// If you need to have some control over the ordering of levels, it is highly recommended that you implement
        /// <see cref="Sort.ISortMode"/> instead of creating a custom solution.
        /// </para>
        /// </summary>
        /// <param name="levelCollection">A collection of levels.</param>
        /// <param name="modifiedLevelCollection">A collection of levels</param>
        /// <returns>True if this modifier has been applied. False indicates that this modifier has no applied any modifications.</returns>
        bool ApplyModifications(IEnumerable<IPreviewBeatmapLevel> levelCollection, out IEnumerable<IPreviewBeatmapLevel> modifiedLevelCollection);
    }
}
