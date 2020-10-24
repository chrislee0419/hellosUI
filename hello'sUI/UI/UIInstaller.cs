using Zenject;
using HUI.UI.Screens;

namespace HUI.UI
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
