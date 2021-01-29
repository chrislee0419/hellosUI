using UnityEngine;

namespace HUI.Interfaces
{
    public interface IModifiableScreen
    {
        /// <summary>
        /// The name/identifier for the screen. This will be shown to the player.
        /// </summary>
        string ScreenName { get; }

        /// <summary>
        /// The color of the background for the modifiable screen.
        /// <para>
        /// This property's setter must be ready to use after or during initialization. Look at the summary
        /// of <see cref="Installers.UIInstaller.ScreensSettingsInitializationOrder"/> for more information.
        /// </para>
        /// </summary>
        Color BackgroundColor { get; set; }

        /// <summary>
        /// Allow or disallow the user to reposition the modifiable screen.
        /// <para>
        /// This property's setter must be ready to use after or during initialization. Look at the summary
        /// of <see cref="Installers.UIInstaller.ScreensSettingsInitializationOrder"/> for more information.
        /// </para>
        /// </summary>
        bool AllowMovement { get; set; }

        /// <summary>
        /// Sets the screen's position and rotation to its default values.
        /// </summary>
        void ResetPosition();

        /// <summary>
        /// Save the screen's position and rotation to the user's config file.
        /// </summary>
        void SavePosition();

        /// <summary>
        /// Load the screen's position and rotation to the user's config file.
        /// </summary>
        void LoadPosition();
    }
}
