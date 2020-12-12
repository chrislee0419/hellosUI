using HUI.Interfaces;

namespace HUI.Utilities
{
    internal static class InterfaceUtilities
    {
        public static string GetIdentifier(this ISortMode sortMode) => sortMode.GetType().FullName;
        public static string GetIdentifier(this IModifiableScreen modScreen) => modScreen.GetType().FullName;
    }
}
