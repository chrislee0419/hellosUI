using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using Zenject;
using HMUI;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using HUI.Utilities;
using BSMLUtilities = BeatSaberMarkupLanguage.Utilities;

namespace HUI.UI
{
    public class SettingsModalManager : IInitializable, IDisposable
    {
        public event Action SettingsModalClosed;

        public bool IsVisible => _modal.isActiveAndEnabled;

        [UIValue("tab-hosts")]
        public List<object> TabHosts => _tabHosts.Select(x => (object)x).ToList();

#pragma warning disable CS0649
        [UIComponent("modal")]
        private ModalView _modal;
#pragma warning restore CS0649

        private LevelCollectionNavigationController _levelCollectionNavigationController;

        private List<ISettingsModalTab> _tabHosts;

        private BSMLParserParams _parserParams;

        [Inject]
        public SettingsModalManager(
            LevelCollectionNavigationController levelCollectionNavigationController,
            List<ISettingsModalTab> settingsTabs)
        {
            _levelCollectionNavigationController = levelCollectionNavigationController;
            _tabHosts = settingsTabs;
        }

        public void Initialize()
        {
            _parserParams = BSMLParser.instance.Parse(BSMLUtilities.GetResourceContent(Assembly.GetExecutingAssembly(), "HUI.UI.Views.SettingsModalView.bsml"), _levelCollectionNavigationController.gameObject, this);

            _modal.name = "HUISettingsModal";

            _parserParams.AddEvent("hide-settings-modal", OnSettingsModalHidden);
            _modal.blockerClickedEvent += OnSettingsModalHidden;

            foreach (ISettingsModalTab tabHost in _tabHosts)
                tabHost.SetupView();

            _levelCollectionNavigationController.didDeactivateEvent += OnLevelCollectionNavigationControllerDeactivated;
        }

        public void Dispose()
        {
            if (_modal?.gameObject != null)
                GameObject.Destroy(_modal.gameObject);

            if (_levelCollectionNavigationController != null)
                _levelCollectionNavigationController.didDeactivateEvent -= OnLevelCollectionNavigationControllerDeactivated;
        }

        public void ShowModal() => _parserParams.EmitEvent("show-settings-modal");

        public void HideModal() => _parserParams.EmitEvent("hide-settings-modal");

        private void OnLevelCollectionNavigationControllerDeactivated(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            HideModal();
        }

        private void OnSettingsModalHidden()
        {
            foreach (ISettingsModalTab tabHost in _tabHosts)
                this.CallAndHandleAction(tabHost.OnModalClosed, nameof(ISettingsModalTab.OnModalClosed));

            this.CallAndHandleAction(SettingsModalClosed, nameof(SettingsModalClosed));
        }

        public interface ISettingsModalTab
        {
            string TabName { get; }

            void SetupView();
            void OnModalClosed();
        }

        public abstract class SettingsModalTabBase : ISettingsModalTab, INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            [UIValue("tab-name")]
            public abstract string TabName { get; }

            [UIObject("settings-container")]
            protected GameObject _container;

            protected virtual string AssociatedBSMLResource => null;

            public virtual void SetupView()
            {
                if (!string.IsNullOrEmpty(AssociatedBSMLResource))
                    BSMLParser.instance.Parse(BSMLUtilities.GetResourceContent(Assembly.GetExecutingAssembly(), AssociatedBSMLResource), _container, this);

                _container.name = TabName + "Tab";
            }

            public abstract void OnModalClosed();

            protected void NotifyPropertyChanged([CallerMemberName] string propertyName = null) => this.CallAndHandleAction(PropertyChanged, propertyName);
        }
    }
}
