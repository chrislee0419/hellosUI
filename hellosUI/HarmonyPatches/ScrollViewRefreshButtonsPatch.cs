using System;
using System.Collections.Generic;
using HarmonyLib;
using HMUI;
using IPA.Utilities;

namespace HUI.HarmonyPatches
{
    [HarmonyPatch(typeof(ScrollView), "RefreshButtons")]
    internal class ScrollViewRefreshButtonsPatch
    {
        private static Dictionary<ScrollView, Action<bool, bool>> _hooks = new Dictionary<ScrollView, Action<bool, bool>>();

        private static readonly PropertyAccessor<ScrollView, float>.Getter ContentSizeGetter = PropertyAccessor<ScrollView, float>.GetGetter("contentSize");
        private static readonly PropertyAccessor<ScrollView, float>.Getter ScrollPageSizeGetter = PropertyAccessor<ScrollView, float>.GetGetter("scrollPageSize");

        private static void Postfix(ScrollView __instance, float ____destinationPos)
        {
            if (_hooks.TryGetValue(__instance, out var action))
            {
                if (action == null)
                {
                    _hooks.Remove(__instance);
                }
                else
                {
                    action.Invoke(
                        ____destinationPos > 0.001f,
                        ____destinationPos < ContentSizeGetter(ref __instance) - ScrollPageSizeGetter(ref __instance) - 0.001f);
                }
            }
        }

        /// <summary>
        /// Install a listener for when the <see cref="ScrollView.RefreshButtons"/> method is called on a specific <see cref="ScrollView" />.
        /// </summary>
        /// <param name="scrollView">The <see cref="ScrollView"/> to install the listener to.</param>
        /// <param name="hook">The delegate to fire when <see cref="ScrollView.RefreshButtons"/> is called.
        /// <para>The delegate takes two <see cref="bool"/> parameters: 
        /// The first parameter represents the page up button's interactable state. 
        /// The second parameter represents the page down button's interactable state.</para></param>
        public static void InstallHook(ScrollView scrollView, Action<bool, bool> hook)
        {
            if (_hooks.ContainsKey(scrollView))
                _hooks[scrollView] += hook;
            else
                _hooks.Add(scrollView, hook);
        }

        /// <summary>
        /// Remove a previously installed listener.
        /// </summary>
        /// <param name="scrollView">The <see cref="ScrollView"/> to remove the listener from.</param>
        /// <param name="hook">The delegate to remove.</param>
        public static void RemoveHook(ScrollView scrollView, Action<bool, bool> hook)
        {
            if (_hooks.ContainsKey(scrollView))
            {
                _hooks[scrollView] -= hook;

                if (_hooks[scrollView] == null)
                    _hooks.Remove(scrollView);
            }
        }
    }
}
