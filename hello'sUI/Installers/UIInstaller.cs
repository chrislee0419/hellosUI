using Zenject;
using HUI.UI;
using HUI.UI.Screens;

namespace HUI.Installers
{
    public class UIInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<DeleteButtonManager>().AsSingle().NonLazy();

            Container.BindInterfacesAndSelfTo<SortScreenManager>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<SearchScreenManager>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<ScrollerScreenManager>().AsSingle().NonLazy();
        }
    }
}
