using UnityEngine;
using BeatSaberMarkupLanguage.FloatingScreen;

namespace HUI.UI.Screens
{
    public class SearchScreenManager
    {
        private FloatingScreen _btnScreen;
        private FloatingScreen _kbScreen;

        public SearchScreenManager()
        {
            //Plugin.Log.Debug("Creating SearchScreenManager");

            //_btnScreen = FloatingScreen.CreateFloatingScreen(new Vector2(40f, 10f), false, new Vector3(0f, 0.35f, 1.4f), Quaternion.Euler(75f, 0f, 0f));
            //_btnScreen.name = "HUISearchButtonScreen";
            //_btnScreen.gameObject.SetActive(false);

            //_kbScreen = FloatingScreen.CreateFloatingScreen(new Vector2(100f, 40f), false, new Vector3(0f, 0.35f, 1.4f), Quaternion.Euler(75f, 0f, 0f));
            //_kbScreen.name = "HUISearchKeyboardScreen";
            //_kbScreen.gameObject.SetActive(false);
        }
    }
}
