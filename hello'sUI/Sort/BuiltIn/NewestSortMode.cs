using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using HUI.Interfaces;

namespace HUI.Sort.BuiltIn
{
    public class NewestSortMode : ISortMode
    {
#pragma warning disable CS0067
        public event Action AvailabilityChanged;
#pragma warning restore CS0067

        public string Name => "Newest";
        public bool IsAvailable => true;
        public bool DefaultSortByAscending => false;

        public IEnumerable<IPreviewBeatmapLevel> SortSongs(IEnumerable<IPreviewBeatmapLevel> levels, bool ascending)
        {
            Dictionary<string, long> directoriesWithCreationTime = levels
                .Select(delegate (IPreviewBeatmapLevel level)
                {
                    if (level is CustomPreviewBeatmapLevel customLevel)
                        return Directory.GetParent(customLevel.customLevelPath).FullName;
                    else
                        return null;
                })
                .Distinct()
                .Where(dirName => dirName != null)
                .Select(dir => new DirectoryInfo(dir))
                .SelectMany(dir => dir.GetDirectories())
                .ToDictionary(x => x.FullName, x => x.CreationTime.Ticks);

            Func<IPreviewBeatmapLevel, long> getCreationTime = delegate (IPreviewBeatmapLevel level)
            {
                if (level is CustomPreviewBeatmapLevel customLevel)
                {
                    if (directoriesWithCreationTime.TryGetValue(Path.GetFullPath(customLevel.customLevelPath), out long creationTime))
                        return creationTime;
                }

                return DateTime.MinValue.Ticks;
            };

            if (ascending)
                return levels.OrderBy(getCreationTime);
            else
                return levels.OrderByDescending(getCreationTime);
        }
    }
}
