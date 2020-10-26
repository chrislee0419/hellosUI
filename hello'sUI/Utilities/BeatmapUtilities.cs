using System.Collections.Generic;
using System.Linq;
using SongCore;

namespace HUI.Utilities
{
    public static class BeatmapUtilities
    {
        private const int LevelHashLength = 40;

        /// <summary>
        /// Get a <see cref="List{IPreviewBeatmapLevel}"/> containing all custom levels from SongCore.
        /// </summary>
        /// <param name="includeWIP">Include WIP levels in the list.</param>
        /// <returns>A <see cref="List{IPreviewBeatmapLevel}"/> of all custom levels.</returns>
        public static List<IPreviewBeatmapLevel> GetAllCustomLevels(bool includeWIP = false)
        {
            List<IPreviewBeatmapLevel> allCustomLevels = Loader.CustomLevelsCollection.beatmapLevels.ToList();
            foreach (var folder in Loader.SeperateSongFolders)
            {
                if (includeWIP || !folder.SongFolderEntry.WIP)
                    allCustomLevels.AddRange(folder.Levels.Values);
            }

            return allCustomLevels;
        }

        /// <summary>
        /// Gets the hash of a custom level using its level ID.
        /// </summary>
        /// <param name="customLevel">Custom level to get the hash for.</param>
        /// <returns>A <see cref="string"/> containing the level's hash or an empty string if unsuccessful.</returns>
        public static string GetCustomLevelHash(CustomPreviewBeatmapLevel customLevel) => GetCustomLevelHash(customLevel.levelID);

        /// <summary>
        /// Gets the hash portion of a custom level's ID.
        /// </summary>
        /// <param name="levelID">Level ID of a custom song to extract the hash from.</param>
        /// <returns>A <see cref="string"/> containing the level's hash or <see cref="string.Empty"/> if unsuccessful.</returns>
        public static string GetCustomLevelHash(string levelID)
        {
            if (!levelID.StartsWith(CustomLevelLoader.kCustomLevelPrefixId))
                return string.Empty;
            else
                return levelID.Substring(CustomLevelLoader.kCustomLevelPrefixId.Length, LevelHashLength);
        }

        /// <summary>
        /// Get the level ID of an <see cref="IPreviewBeatmapLevel"/>. Removes the directory from a custom level's ID if it exists.
        /// </summary>
        /// <param name="level">A beatmap level.</param>
        /// <returns>The level ID of the provided <see cref="IPreviewBeatmapLevel"/>.</returns>
        public static string GetSimplifiedLevelID(IPreviewBeatmapLevel level)
        {
            // Since custom levels have their IDs formatted like "custom_level_(hash)[_(directory)]", where the "_(directory)" part is optional,
            // we have to remove that part to get a consistent naming. Also, we don't care about duplicate songs;
            // if they have the same hash, we can use the same BeatmapDetails object.
            if (!(level is CustomPreviewBeatmapLevel) && !level.levelID.StartsWith(CustomLevelLoader.kCustomLevelPrefixId))
                return level.levelID;
            else
                return GetCustomLevelIDWithoutDirectory(level.levelID);
        }

        /// <summary>
        /// Get the simplified level ID from a level ID string. Removes the directory from a custom level's ID if it exists.
        /// </summary>
        /// <param name="levelID">A <see cref="string"/> containing some level's ID.</param>
        /// <returns>The level ID, minus the directory when applicable.</returns>
        public static string GetSimplifiedLevelID(string levelID)
        {
            if (levelID.StartsWith(CustomLevelLoader.kCustomLevelPrefixId))
                return GetCustomLevelIDWithoutDirectory(levelID);
            else
                return levelID;
        }

        private static string GetCustomLevelIDWithoutDirectory(string levelID)
        {
            // The hash is always 40 characters long
            return levelID.Substring(0, CustomLevelLoader.kCustomLevelPrefixId.Length + 40);
        }
    }
}
