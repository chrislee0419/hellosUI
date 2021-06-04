using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Zenject;
using HMUI;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using HUI.Interfaces;
using HUI.Utilities;
using BSMLUtilities = BeatSaberMarkupLanguage.Utilities;

namespace HUI.UI.Settings
{
    public class SettingsModalManager : IInitializable, IDisposable
    {
        public event Action SettingsModalClosed;

        public bool IsVisible => _modal?.isActiveAndEnabled ?? false;

        [UIValue("tab-hosts")]
        public List<object> TabHosts => _tabHosts.Select(x => (object)x).ToList();

#pragma warning disable CS0649
        [UIComponent("modal")]
        private ModalView _modal;
#pragma warning restore CS0649

        private LevelCollectionNavigationController _levelCollectionNavigationController;

        private SettingsModalDispatcher _dispatcher;
        private List<ISettingsModalTab> _tabHosts;
        private int _lastTabIndex = 0;

        private BSMLParserParams _parserParams;

        [Inject]
        public SettingsModalManager(
            LevelCollectionNavigationController levelCollectionNavigationController,
            SettingsModalDispatcher settingsModalDispatcher,
            List<ISettingsModalTab> settingsTabs)
        {
            _levelCollectionNavigationController = levelCollectionNavigationController;
            _dispatcher = settingsModalDispatcher;
            _tabHosts = settingsTabs;
        }

        public void Initialize()
        {
            _levelCollectionNavigationController.didDeactivateEvent += OnLevelCollectionNavigationControllerDeactivated;

            _dispatcher.ShowModalRequested += ShowModal;
            _dispatcher.HideModalRequested += HideModal;
            _dispatcher.ToggleModalVisibilityRequested += ToggleModalVisibility;
        }

        public void Dispose()
        {
            if (_modal?.gameObject != null)
                GameObject.Destroy(_modal.gameObject);

            if (_levelCollectionNavigationController != null)
                _levelCollectionNavigationController.didDeactivateEvent -= OnLevelCollectionNavigationControllerDeactivated;

            if (_dispatcher != null)
            {
                _dispatcher.ShowModalRequested -= ShowModal;
                _dispatcher.HideModalRequested -= HideModal;
                _dispatcher.ToggleModalVisibilityRequested -= ToggleModalVisibility;
            }
        }

        private void ShowModal()
        {
            // late initialization of the modal view
            if (_parserParams == null)
            {
                _parserParams = BSMLParser.instance.Parse(BSMLUtilities.GetResourceContent(Assembly.GetExecutingAssembly(), "HUI.UI.Views.SettingsModalView.bsml"), _levelCollectionNavigationController.gameObject, this);

                _modal.name = "HUISettingsModal";

                _parserParams.AddEvent("hide-settings-modal", OnSettingsModalHidden);
                _modal.blockerClickedEvent += OnSettingsModalHidden;

                foreach (ISettingsModalTab tabHost in _tabHosts)
                    tabHost.SetupView();
            }

            _parserParams.EmitEvent("show-settings-modal");
        }

        [UIAction("close-clicked")]
        private void HideModal() => _parserParams?.EmitEvent("hide-settings-modal");

        private void ToggleModalVisibility()
        {
            if (IsVisible)
                HideModal();
            else
                ShowModal();
        }

        private void OnLevelCollectionNavigationControllerDeactivated(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            HideModal();
        }

        private void OnSettingsModalHidden()
        {
            this.CallAndHandleAction(_tabHosts[_lastTabIndex].OnTabHidden, nameof(ISettingsModalTab.OnTabHidden));

            this.CallAndHandleAction(SettingsModalClosed, nameof(SettingsModalClosed));
        }

        [UIAction("tab-selected")]
        private void OnTabSelected(SegmentedControl segmentedControl, int index)
        {
            _tabHosts[_lastTabIndex].OnTabHidden();
            _lastTabIndex = index;
        }
    }
}
