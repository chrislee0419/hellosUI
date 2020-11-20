using System;
using System.ComponentModel;

namespace HUI.Utilities
{
    internal static class GeneralUtilities
    {
        public static void CallAndHandleAction(this object obj, Action action, string nameOfAction)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                Plugin.Log.Warn($"Unexpected exception occurred in {obj.GetType().Name}:{nameOfAction}");
                Plugin.Log.Debug(e);
            }
        }

        public static void CallAndHandleAction<T>(this object obj, Action<T> action, string nameOfAction, T param)
        {
            try
            {
                action?.Invoke(param);
            }
            catch (Exception e)
            {
                Plugin.Log.Warn($"Unexpected exception occurred in {obj.GetType().Name}:{nameOfAction}");
                Plugin.Log.Debug(e);
            }
        }

        public static void CallAndHandleAction(this object obj, PropertyChangedEventHandler action, string propertyName)
        {
            try
            {
                action?.Invoke(obj, new PropertyChangedEventArgs(propertyName));
            }
            catch (Exception e)
            {
                Plugin.Log.Warn($"Unexpected exception occurred in {obj.GetType().Name}:PropertyChanged");
                Plugin.Log.Debug(e);
            }
        }
    }
}
