using System;
using Zenject;
using HUI.Utilities;

namespace HUI
{
    public class SinglePlayerManagerBase : IInitializable, IDisposable
    {
        private MainMenuViewController _mainMenuVC;
        private SoloFreePlayFlowCoordinator _soloFC;
        private PartyFreePlayFlowCoordinator _partyFC;

        public SinglePlayerManagerBase(MainMenuViewController mainMenuVC, SoloFreePlayFlowCoordinator soloFC, PartyFreePlayFlowCoordinator partyFC)
        {
            _mainMenuVC = mainMenuVC;
            _soloFC = soloFC;
            _partyFC = partyFC;
        }

        public virtual void Initialize()
        {
            _mainMenuVC.didFinishEvent += OnMainMenuViewControllerDidFinish;
            _soloFC.didFinishEvent += OnSinglePlayerLevelSelectionFlowCoordinatorDidFinish;
            _partyFC.didFinishEvent += OnSinglePlayerLevelSelectionFlowCoordinatorDidFinish;
        }

        public virtual void Dispose()
        {
            if (_mainMenuVC != null)
                _mainMenuVC.didFinishEvent -= OnMainMenuViewControllerDidFinish;
            if (_soloFC != null)
                _soloFC.didFinishEvent -= OnSinglePlayerLevelSelectionFlowCoordinatorDidFinish;
            if (_partyFC != null)
                _partyFC.didFinishEvent -= OnSinglePlayerLevelSelectionFlowCoordinatorDidFinish;
        }

        private void OnMainMenuViewControllerDidFinish(MainMenuViewController _, MainMenuViewController.MenuButton buttonType)
        {
            if (buttonType == MainMenuViewController.MenuButton.SoloFreePlay || buttonType == MainMenuViewController.MenuButton.Party)
                this.CallAndHandleAction(OnSinglePlayerLevelSelectionStarting, nameof(OnSinglePlayerLevelSelectionStarting));
        }

        protected virtual void OnSinglePlayerLevelSelectionStarting()
        {

        }

        private void OnSinglePlayerLevelSelectionFlowCoordinatorDidFinish(SinglePlayerLevelSelectionFlowCoordinator _)
        {
            this.CallAndHandleAction(OnSinglePlayerLevelSelectionFinished, nameof(OnSinglePlayerLevelSelectionFinished));
        }

        protected virtual void OnSinglePlayerLevelSelectionFinished()
        {

        }
    }
}
