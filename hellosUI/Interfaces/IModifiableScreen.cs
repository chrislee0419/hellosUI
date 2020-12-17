using UnityEngine.UI;
using BeatSaberMarkupLanguage.FloatingScreen;

namespace HUI.Interfaces
{
    public interface IModifiableScreen
    {
        /// <summary>
        /// The name/identifier for the screen. This will be shown to the player.
        /// </summary>
        string ScreenName { get; }

        /// <summary>
        /// The screen to apply settings to.
        /// <para>
        /// This property must be set before or during initialization. Look at the summary of
        /// <see cref="Installers.UIInstaller.ScreensSettingsInitializationOrder"/> for more information.
        /// </para>
        /// </summary>
        FloatingScreen Screen { get; }

        /// <summary>
        /// The background graphic to apply settings to.
        /// <para>
        /// This property must be set before or during initialization. Look at the summary of
        /// <see cref="Installers.UIInstaller.ScreensSettingsInitializationOrder"/> for more information.
        /// </para>
        /// </summary>
        Graphic Background { get; }

        /// <summary>
        /// Sets the screen's position and rotation to its default values.
        /// </summary>
        void ResetPosition();
    }
}
