using System.Collections.Generic;

namespace HUI.Sort
{
    public interface ISortMode
    {
        /// <summary>
        /// The name of the sort mode that will be displayed to the user.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// A <see cref="bool"/> representing whether this sort mode can be used.
        /// <para>
        /// This is typically used to prevent the sort mode from being used
        /// when another mod that it is dependent on is not available.
        /// </para>
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Indicates the default sort direction to use when this sort mode is first selected.
        /// </summary>
        bool DefaultSortByAscending { get; }

        /// <summary>
        /// Sort the songs.
        /// </summary>
        /// <param name="levels">Unsorted collection of levels.</param>
        /// <param name="ascending">Sort the levels in ascending order.</param>
        /// <returns>A sorted collection of levels.</returns>
        IEnumerable<IPreviewBeatmapLevel> SortSongs(IEnumerable<IPreviewBeatmapLevel> levels, bool ascending);
    }
}
