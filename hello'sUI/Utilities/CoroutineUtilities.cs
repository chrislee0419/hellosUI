using System;
using System.Collections;
using UnityEngine;

namespace HUI.Utilities
{
    internal static class CoroutineUtilities
    {
        private static WaitForEndOfFrame _wait = new WaitForEndOfFrame();

        /// <summary>
        /// Invoke an action after a short wait.
        /// </summary>
        /// <param name="action">Action to invoke after the wait.</param>
        /// <param name="framesToWait">The number of frames to wait.</param>
        /// <param name="waitForEndOfFrame">True to wait for the end of the frame. False to wait for the next frame</param>
        /// <returns>An <see cref="IEnumerator"/> that should be supplied to <see cref="MonoBehaviour.StartCoroutine"/></returns>
        public static IEnumerator DelayedActionCoroutine(Action action, int framesToWait = 1, bool waitForEndOfFrame = true)
        {
            WaitForEndOfFrame wait = waitForEndOfFrame ? _wait : null;
            while (framesToWait-- > 0)
                yield return wait;

            action.Invoke();
        }
    }
}
