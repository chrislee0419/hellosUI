using System.Collections.Generic;
using System.Linq;
using Zenject;
using HUI.Interfaces;
using HUI.Utilities;

namespace HUI.Sort.BuiltIn
{
    public class PlayCountSortMode : ISortMode
    {
        public string Name => "Play Count";
        public bool IsAvailable => true;
        public bool DefaultSortByAscending => false;

        private PlayerDataModel _playerDataModel;

        [Inject]
        public PlayCountSortMode(PlayerDataModel playerDataModel)
        {
            _playerDataModel = playerDataModel;
        }

        public IEnumerable<IPreviewBeatmapLevel> SortSongs(IEnumerable<IPreviewBeatmapLevel> levels, bool ascending)
        {
            if (_playerDataModel == null)
                return levels;

            var levelsWithPlays = levels.AsParallel().Select(level => GetPlayCountForLevel(level));

            if (ascending)
                return levelsWithPlays.OrderBy(x => x.playCount).Select(x => x.level);
            else
                return levelsWithPlays.OrderByDescending(x => x.playCount).Select(x => x.level);
        }

        private (IPreviewBeatmapLevel level, int playCount) GetPlayCountForLevel(IPreviewBeatmapLevel level)
        {
            string levelID = BeatmapUtilities.GetSimplifiedLevelID(level);
            return (level, _playerDataModel.playerData.levelsStatsData.Where(x => x.levelID.StartsWith(levelID)).Sum(x => x.playCount));
        }
    }
}
