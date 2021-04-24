using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IPA.Loader;
using HUI.Attributes;

namespace HUI.Utilities
{
    public static class InstallerUtilities
    {
        public static IEnumerable<Type> GetTypesFromAssembly(Assembly assembly)
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

        /// <summary>
        /// Finds all <see cref="Type"/> classes that derive from a provided base <see cref="Type"/> 
        /// in all <see cref="Assembly"/> objects that are loaded and set as enabled in BSIPA's <see cref="PluginManager"/>.
        /// </summary>
        /// <param name="type">The base type of all derived.</param>
        /// <returns>An <see cref="IEnumerable{Type}"/> containing all the classes that derive from <paramref name="type"/>.</returns>
        public static IEnumerable<Type> GetDerivativeTypesFromAllAssemblies(Type type)
        {
            Assembly currentAssembly = Assembly.GetExecutingAssembly();
            return PluginManager.EnabledPlugins
                .Select(x => x.Assembly)
                .Where(x => x != currentAssembly && x != null)
                .SelectMany(GetTypesFromAssembly)
                .Where(x => type.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract);
        }

        public static IEnumerable<Type> GetAutoInstallDerivativeTypesFromAllAssemblies(Type type)
        {
            return GetDerivativeTypesFromAllAssemblies(type).Where(x => x.GetCustomAttribute<AutoInstallAttribute>() != null);
        }
    }
}
