using Zenject;
using HUI.UI;
using HUI.UI.Screens;
using HUI.UI.Settings;

namespace HUI.Installers
{
    public class UIInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<DeleteButtonManager>().AsSingle();

            Container.BindInterfacesAndSelfTo<SortScreenManager>().AsSingle();
            Container.BindInterfacesAndSelfTo<SearchScreenManager>().AsSingle();
            Container.BindInterfacesAndSelfTo<SearchKeyboardScreenManager>().AsSingle();
            Container.BindInterfacesAndSelfTo<ScrollerScreenManager>().AsSingle();

            Container.BindInterfacesAndSelfTo<SettingsModalManager>().AsSingle();

            Container.BindInterfacesAndSelfTo<SearchSettingsTab>().AsSingle();
        }
    }
}
