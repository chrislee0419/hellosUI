using HUI.Interfaces;

namespace HUI.Utilities
{
    internal static class SortModeUtilities
    {
        public static string GetIdentifier(this ISortMode sortMode) => sortMode.GetType().FullName;
    }
}
