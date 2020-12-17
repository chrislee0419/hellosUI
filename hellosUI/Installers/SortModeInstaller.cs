using System;
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

            // don't bind ISortMode to PPSortMode, otherwise it will be grouped with the external sort groups
            Container.Bind(typeof(PPSortMode), typeof(IInitializable), typeof(IDisposable)).To<PPSortMode>().AsSingle();

            // get external sort modes
            var externalSortModes = InstallerUtilities.GetDerivativeTypesFromAllAssemblies(typeof(ISortMode));
            foreach (var externalSortMode in externalSortModes)
                Container.BindInterfacesAndSelfTo(externalSortMode).AsSingle();
        }
    }
}
