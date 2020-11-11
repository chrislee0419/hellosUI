using Zenject;
using HUI.Interfaces;
using HUI.Sort;
using HUI.Sort.BuiltIn;
using HUI.Utilities;

namespace HUI.Installers
{
    public class SortModeInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<SongSortManager>().AsSingle();
            Container.Bind<PlayCountSortMode>().AsSingle();

            // get external sort modes
            var externalSortModes = InstallerUtilities.GetDerivativeTypesFromAllAssemblies(typeof(ISortMode));
            Container.Bind<ISortMode>().To(externalSortModes).AsCached();
        }
    }
}
