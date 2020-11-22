using System;
using System.Reflection;
using UnityEngine;
using Zenject;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;

namespace SortModeExamplePlugin
{
    public class ToggleManagerInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<ToggleManager>().AsSingle();

            // HUI.Installers.SortModeInstaller will do the binding for the ExampleSortMode object
        }
    }

    public class ToggleManager : IInitializable, IDisposable
    {
#pragma warning disable CS0649
        [UIObject("root")]
        private GameObject _rootContainer;
#pragma warning restore CS0649

        private ExampleSortMode _sortMode;

        private LevelCollectionNavigationController _levelCollectionNavigationController;

        [Inject]
        public ToggleManager(LevelCollectionNavigationController levelCollectionNavigationController, ExampleSortMode exampleSortMode)
        {
            _levelCollectionNavigationController = levelCollectionNavigationController;
            _sortMode = exampleSortMode;
        }

        public void Initialize()
        {
            Plugin.Log.Info("Initializing example sort mode availability toggle manager");

            BSMLParser.instance.Parse(Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "SortModeExamplePlugin.ToggleView.bsml"), _levelCollectionNavigationController.gameObject, this);
        }

        public void Dispose()
        {
            if (_rootContainer != null)
                GameObject.Destroy(_rootContainer);
        }

        [UIAction("button-clicked")]
        private void OnButtonClicked()
        {
            _sortMode.IsAvailable = !_sortMode.IsAvailable;
        }
    }
}
