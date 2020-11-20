using System;
using System.Linq;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
using HMUI;
using IPA.Utilities;
using BS_Utils.Utilities;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.Attributes;
using SongCore;
using HUI.UI.Components;
using HUI.Utilities;
using Object = UnityEngine.Object;
using BSMLUtilities = BeatSaberMarkupLanguage.Utilities;

namespace HUI.UI
{
    public class DeleteButtonManager : SinglePlayerManagerBase, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _deleteConfirmationText;
        [UIValue("delete-confirmation-text")]
        public string DeleteConfirmationText
        {
            get => _deleteConfirmationText;
            set
            {
                if (_deleteConfirmationText == value)
                    return;

                _deleteConfirmationText = value;
                NotifyPropertyChanged();
            }
        }

#pragma warning disable CS0649
        [UIObject("confirm-delete-button")]
        private GameObject _confirmDeleteButton;
#pragma warning restore CS0649

        private Button _deleteButton;
        private bool _deleteButtonInitialized = false;
        private BSMLParserParams _parserParams;

        private LevelCollectionNavigationController _levelCollectionNavigationController;
        private LevelCollectionViewController _levelCollectionViewController;
        private TableView _levelsTableView;
        private CustomBeatmapLevel _levelToDelete;

        private static readonly FieldAccessor<LevelCollectionNavigationController, IBeatmapLevelPack>.Accessor LevelPackAccessor = FieldAccessor<LevelCollectionNavigationController, IBeatmapLevelPack>.GetAccessor("_levelPack");
        private static readonly FieldAccessor<LevelCollectionViewController, LevelCollectionTableView>.Accessor LevelCollectionTableViewAccessor = FieldAccessor<LevelCollectionViewController, LevelCollectionTableView>.GetAccessor("_levelCollectionTableView");
        private static readonly FieldAccessor<LevelCollectionTableView, IPreviewBeatmapLevel[]>.Accessor TableViewLevelsAccessor = FieldAccessor<LevelCollectionTableView, IPreviewBeatmapLevel[]>.GetAccessor("_previewBeatmapLevels");
        private static readonly FieldAccessor<LevelCollectionNavigationController, bool>.Accessor ShowPlayerStatsAccessor = FieldAccessor<LevelCollectionNavigationController, bool>.GetAccessor("_showPlayerStatsInDetailView");
        private static readonly FieldAccessor<LevelCollectionNavigationController, bool>.Accessor ShowPracticeButtonAccessor = FieldAccessor<LevelCollectionNavigationController, bool>.GetAccessor("_showPracticeButtonInDetailView");
        private static readonly FieldAccessor<LevelCollectionNavigationController, string>.Accessor ActionButtonTextAccessor = FieldAccessor<LevelCollectionNavigationController, string>.GetAccessor("_actionButtonTextInDetailView");
        private static readonly FieldAccessor<StandardLevelDetailView, IBeatmapLevel>.Accessor LevelAccessor = FieldAccessor<StandardLevelDetailView, IBeatmapLevel>.GetAccessor("_level");

        public DeleteButtonManager(
            MainMenuViewController mainMenuVC,
            SoloFreePlayFlowCoordinator soloFC,
            PartyFreePlayFlowCoordinator partyFC,
            LevelCollectionNavigationController levelCollectionNavigationController,
            LevelCollectionViewController levelCollectionViewController,
            StandardLevelDetailViewController levelVC) : base(mainMenuVC, soloFC, partyFC)
        {
            _levelCollectionNavigationController = levelCollectionNavigationController;
            _levelCollectionViewController = levelCollectionViewController;
            var levelCollectionTableView = FieldAccessor<LevelCollectionViewController, LevelCollectionTableView>.Get(ref levelCollectionViewController, "_levelCollectionTableView");
            _levelsTableView = FieldAccessor<LevelCollectionTableView, TableView>.Get(ref levelCollectionTableView, "_tableView");

            var detailView = FieldAccessor<StandardLevelDetailViewController, StandardLevelDetailView>.Get(ref levelVC, "_standardLevelDetailView");
            _deleteButton = Object.Instantiate(detailView.practiceButton, detailView.practiceButton.transform.parent, false);
            _deleteButton.name = "HUIDeleteButton";

            // pretty much yoinked from BSML
            // https://github.com/monkeymanboy/BeatSaberMarkupLanguage/blob/master/BeatSaberMarkupLanguage/Tags/ButtonWithIconTag.cs
            Transform contentTransform = _deleteButton.transform.Find("Content");
            var slg = contentTransform.GetComponent<StackLayoutGroup>();
            slg.padding = new RectOffset(3, 3, 2, 2);

            Object.Destroy(contentTransform.Find("Text").gameObject);
            Image iconImage = new GameObject("Icon").AddComponent<ImageView>();
            iconImage.material = BSMLUtilities.ImageResources.NoGlowMat;
            iconImage.rectTransform.SetParent(contentTransform, false);
            iconImage.sprite = UIUtilities.LoadSpriteFromResources("HUI.Assets.garbagecan.png");
            iconImage.preserveAspect = true;

            Object.Destroy(contentTransform.GetComponent<LayoutElement>());

            // update and show modal on click
            _deleteButton.onClick.RemoveAllListeners();
            _deleteButton.onClick.AddListener(delegate ()
            {
                // initialize modal on first click
                // the modal doesn't like to be created during DI
                if (_parserParams == null)
                {
                    _parserParams = BSMLParser.instance.Parse(BSMLUtilities.GetResourceContent(Assembly.GetExecutingAssembly(), "HUI.UI.Views.DeleteButtonModalView.bsml"), _levelCollectionNavigationController.gameObject, this);

                    Object.Destroy(_confirmDeleteButton.GetComponent<ButtonStaticAnimations>());

                    var btnAnims = _confirmDeleteButton.AddComponent<CustomButtonAnimations>();
                    btnAnims.NormalSelectionUseTranslucentColour = true;
                    btnAnims.NormalBGColour = Color.red;
                    btnAnims.HighlightedBGColour = new Color(1f, 0.375f, 0f);
                    btnAnims.PressedBGColour = new Color(1f, 0.375f, 0f);
                }

                var level = LevelAccessor.Invoke(ref detailView);

                if (level is CustomBeatmapLevel customLevel)
                {
                    _levelToDelete = customLevel;
                    DeleteConfirmationText = $"Are you sure you would like to delete '<color=#FFFFCC>{customLevel.songName.EscapeTextMeshProTags()}</color>' by {customLevel.levelAuthorName.EscapeTextMeshProTags()}?";

                    _parserParams.EmitEvent("show-delete-confirmation-modal");
                }
                else
                {
                    Plugin.Log.Debug("Unable to delete level (not a custom level)");
                    _deleteButton.interactable = false;
                }
            });
        }

        protected override void OnSinglePlayerLevelSelectionStarting()
        {
            // not sure why, but this is throwing a null ref exception when setting the highlighted colour in the constructor
            // seems like it somehow can't find the necessary components, even though i do the exact same thing in ScrollerScreenManager
            if (!_deleteButtonInitialized)
            {
                // add custom animations
                Object.Destroy(_deleteButton.GetComponent<ButtonStaticAnimations>());

                var btnAnims = _deleteButton.gameObject.AddComponent<CustomIconButtonAnimations>();
                btnAnims.HighlightedBGColour = Color.red;
                btnAnims.PressedBGColour = Color.red;

                _deleteButtonInitialized = true;
            }

            _levelCollectionViewController.didSelectLevelEvent += OnLevelSelected;

            _deleteButton.interactable = false;
        }

        protected override void OnSinglePlayerLevelSelectionFinished()
        {
            _levelCollectionViewController.didSelectLevelEvent -= OnLevelSelected;

            // NOTE: modal view is lazy init'd, so this could be null
            _parserParams?.EmitEvent("hide-delete-confirmation-modal");
        }

        private void OnLevelSelected(LevelCollectionViewController _, IPreviewBeatmapLevel level) => _deleteButton.interactable = level is CustomPreviewBeatmapLevel;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = null) => this.CallAndHandleAction(PropertyChanged, propertyName);

        [UIAction("confirm-delete-button-clicked")]
        private void OnConfirmDeleteButtonClicked()
        {
            if (_levelToDelete != null)
            {
                Plugin.Log.Info($"Deleting beatmap '{_levelToDelete.songName}' by {_levelToDelete.levelAuthorName}");

                int indexOfTopCell = 0;
                if (_levelsTableView.visibleCells.Count() > 0)
                    indexOfTopCell = _levelsTableView.visibleCells.Min(x => x.idx);

                // delete the level and reload the levels
                IBeatmapLevelPack currentPack = LevelPackAccessor.Invoke(ref _levelCollectionNavigationController);

                if (currentPack == null)
                {
                    var tableView = LevelCollectionTableViewAccessor.Invoke(ref _levelCollectionViewController);
                    IPreviewBeatmapLevel[] levels = TableViewLevelsAccessor.Invoke(ref tableView);
                    BeatmapLevelCollection replacementLevels = new BeatmapLevelCollection(levels.Where(x => x.levelID != _levelToDelete.levelID).ToArray());

                    Loader.Instance.DeleteSong(_levelToDelete.customLevelPath);

                    _levelCollectionNavigationController.SetDataForLevelCollection(
                        replacementLevels,
                        ShowPlayerStatsAccessor.Invoke(ref _levelCollectionNavigationController),
                        ShowPracticeButtonAccessor.Invoke(ref _levelCollectionNavigationController),
                        ActionButtonTextAccessor.Invoke(ref _levelCollectionNavigationController),
                        null);
                }
                else
                {
                    // SongCore should automatically reload the pack
                    Loader.Instance.DeleteSong(_levelToDelete.customLevelPath);
                }

                _levelToDelete = null;

                // scroll back to where we were
                _levelsTableView.ScrollToCellWithIdx(indexOfTopCell, TableViewScroller.ScrollPositionType.Beginning, false);
            }

            _parserParams.EmitEvent("hide-delete-confirmation-modal");
        }
    }
}
