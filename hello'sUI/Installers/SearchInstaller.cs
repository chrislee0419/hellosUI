using Zenject;
using HUI.Search;

namespace HUI.Installers
{
    public class SearchInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<SearchManager>().AsSingle();

            Container.Bind<WordSearchEngine>().AsSingle();
            Container.BindInterfacesAndSelfTo<WordPredictionEngine>().AsSingle();
        }
    }
}
