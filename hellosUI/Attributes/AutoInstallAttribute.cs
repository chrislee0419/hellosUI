using System;

namespace HUI.Attributes
{
    /// <summary>
    /// This attribute is used on classes that implement an HUI interface and
    /// indicates that HUI should handle installing the class. This may be
    /// useful if you don't need to use (and therefore reference) SiraUtil in
    /// your mod, but still want to create classes that implement an HUI interface.
    ///
    /// <para>
    /// HUI will install the binding for your class like so:
    /// <code>
    /// Container.BindInterfacesAndSelfTo(ClassType).AsSingle();
    /// </code>
    /// As such, do not use this attribute if you would like to configure your
    /// own bindings. For example, you will need to configure your own bindings if
    /// your class inherits <see cref="UnityEngine.MonoBehaviour"/> to make sure it is
    /// properly instantiated.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class AutoInstallAttribute : Attribute
    {
    }
}
