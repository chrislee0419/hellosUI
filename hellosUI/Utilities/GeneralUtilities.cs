using System;
using System.ComponentModel;
using IPA.Logging;

namespace HUI.Utilities
{
    public static class GeneralUtilities
    {
        private static double Epsilon = 0.001;
        public static bool RoughlyEquals(this float num2, float num)
        {
            float diff = num2 - num;
            return diff < Epsilon && diff > -Epsilon;
        }

        public static bool RoughlyEquals(this double num2, double num)
        {
            double diff = num2 - num;
            return diff < Epsilon && diff > -Epsilon;
        }

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

        public static void DebugOnly(this Logger logger, string message)
        {
#if DEBUG
            logger.Debug(message);
#endif
        }

        public static void DebugOnly(this Logger logger, Exception exception)
        {
#if DEBUG
            logger.Debug(exception);
#endif
        }
    }
}
