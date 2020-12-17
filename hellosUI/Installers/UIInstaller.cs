using Zenject;
using HUI.UI;
using HUI.UI.Screens;
using HUI.UI.Settings;
using HUI.Interfaces;
using HUI.Utilities;

namespace HUI.Installers
{
    public class UIInstaller : Installer
    {
        /// <summary>
        /// <para>
        /// Screen settings are loaded and applied by <see cref="ScreensSettingsTab"/>. As a result,
        /// all <see cref="IModifiableScreen"/> objects should populate <see cref="IModifiableScreen.Screen"/>
        /// and <see cref="IModifiableScreen.Background"/> before or during the initialization phase of
        /// <see cref="IInitializable"/>.
        /// </para>
        /// <para>
        /// For <see cref="IModifiableScreen"/> objects that are order dependent, the execution order of the
        /// screen should be greater than this number.
        /// </para>
        /// </summary>
        public const int ScreensSettingsInitializationOrder = -50;

        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<DeleteButtonManager>().AsSingle();

            Container.BindInterfacesAndSelfTo<SortScreenManager>().AsSingle();
            Container.BindInterfacesAndSelfTo<SortListScreenManager>().AsSingle();
            Container.BindInterfacesAndSelfTo<SearchScreenManager>().AsSingle();
            Container.BindInterfacesAndSelfTo<SearchKeyboardScreenManager>().AsSingle();
            Container.BindInterfacesAndSelfTo<ScrollerScreenManager>().AsSingle();

            Container.BindInterfacesAndSelfTo<SettingsModalManager>().AsSingle();
            Container.Bind<SettingsModalDispatcher>().AsSingle();

            Container.BindInterfacesAndSelfTo<SortSettingsTab>().AsSingle();
            Container.BindInterfacesAndSelfTo<SearchSettingsTab>().AsSingle();
            Container.BindInterfacesAndSelfTo<ScreensSettingsTab>().AsSingle();
            Container.BindExecutionOrder<ScreensSettingsTab>(ScreensSettingsInitializationOrder);

            var externalModifiableScreens = InstallerUtilities.GetDerivativeTypesFromAllAssemblies(typeof(IModifiableScreen));
            foreach (var externalModifiableScreen in externalModifiableScreens)
                Container.BindInterfacesAndSelfTo(externalModifiableScreen).AsSingle();
        }
    }
}
