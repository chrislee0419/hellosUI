using Zenject;
using HUI.DataFlow;
using HUI.Interfaces;
using HUI.Utilities;

namespace HUI.Installers
{
    public class DataFlowInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<LevelCollectionDataFlowManager>().AsSingle();

            var externalLevelCollectionModifiers = InstallerUtilities.GetAutoInstallDerivativeTypesFromAllAssemblies(typeof(ILevelCollectionModifier));
            foreach (var externalLevelCollectionModifier in externalLevelCollectionModifiers)
                Container.BindInterfacesAndSelfTo(externalLevelCollectionModifier).AsSingle();
        }
    }
}
