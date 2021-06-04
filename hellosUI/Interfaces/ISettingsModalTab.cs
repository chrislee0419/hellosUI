using System.ComponentModel;
using System.Runtime.CompilerServices;
using UnityEngine;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.Attributes;
using HUI.Utilities;
using BSMLUtilities = BeatSaberMarkupLanguage.Utilities;

namespace HUI.Interfaces
{
    public interface ISettingsModalTab
    {
        string TabName { get; }

        void SetupView();
        void OnTabHidden();
    }

    public abstract class SettingsModalTabBase : ISettingsModalTab, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [UIValue("tab-name")]
        public abstract string TabName { get; }

        [UIObject("settings-container")]
        protected GameObject _container;

        protected virtual string AssociatedBSMLResource => null;

        protected BSMLParserParams _parserParams;

        public virtual void SetupView()
        {
            if (!string.IsNullOrEmpty(AssociatedBSMLResource))
                _parserParams = BSMLParser.instance.Parse(BSMLUtilities.GetResourceContent(this.GetType().Assembly, AssociatedBSMLResource), _container, this);

            _container.name = TabName + "Tab";

            PluginConfig.Instance.ConfigReloaded += OnPluginConfigReloaded;
        }

        public virtual void Dispose()
        {
            if (PluginConfig.Instance != null)
                PluginConfig.Instance.ConfigReloaded -= OnPluginConfigReloaded;
        }

        public virtual void OnTabHidden()
        { }

        protected virtual void OnPluginConfigReloaded()
        {
            _parserParams?.EmitEvent("refresh-all-values");
        }

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = null) => this.CallAndHandleAction(PropertyChanged, propertyName);
    }
}
