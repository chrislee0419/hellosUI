using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Zenject;
using IPA.Loader;
using HUI.Interfaces;
using HUI.Sort;
using HUI.Sort.BuiltIn;

namespace HUI.Installers
{
    public class SortModeInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<SongSortManager>().AsSingle();
            Container.Bind<PlayCountSortMode>().AsSingle();

            // get external sort modes
            Assembly currentAssembly = Assembly.GetExecutingAssembly();
            var externalSortModes = PluginManager.EnabledPlugins
                .Select(x => x.Assembly)
                .Where(x => x != currentAssembly && x != null)
                .SelectMany(GetTypesFromAssembly)
                .Where(x => typeof(ISortMode).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract);
            Container.Bind<ISortMode>().To(externalSortModes).AsSingle();
        }

        private IEnumerable<Type> GetTypesFromAssembly(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(x => x != null);
            }
        }
    }
}
