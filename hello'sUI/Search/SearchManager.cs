using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zenject;
using HUI.Interfaces;
using HUI.UI.Screens;
using HUI.Utilities;
using static HUI.Search.WordPredictionEngine;

namespace HUI.Search
{
    public class SearchManager : IInitializable, IDisposable, ILevelCollectionModifier
    {
        public event Action LevelCollectionRefreshRequested;

        private WordSearchEngine _wordSearchEngine;
        private WordPredictionEngine _wordPredictionEngine;
        private SearchScreenManager _searchScreenManager;
        private SearchKeyboardScreenManager _searchKeyboardScreenManager;

        private IAnnotatedBeatmapLevelCollection _originalLevelCollection;

        private StringBuilder _searchText;

        [Inject]
        public SearchManager(
            WordSearchEngine wordSearchEngine,
            WordPredictionEngine wordPredictionEngine,
            SearchScreenManager searchScreenManager,
            SearchKeyboardScreenManager searchKeyboardScreenManager)
        {
            _wordSearchEngine = wordSearchEngine;
            _wordPredictionEngine = wordPredictionEngine;
            _searchScreenManager = searchScreenManager;
            _searchKeyboardScreenManager = searchKeyboardScreenManager;

            _searchText = new StringBuilder();
        }

        public void Initialize()
        {
            _searchScreenManager.SearchButtonPressed += OnSearchScreenSearchButtonPressed;
            _searchScreenManager.CancelButtonPressed += OnSearchScreenCancelButtonPressed;

            _searchKeyboardScreenManager.KeyPressed += OnSearchKeyboardScreenKeyPressed;
            _searchKeyboardScreenManager.DeleteButtonPressed += OnSearchKeyboardScreenDeleteButtonPressed;
            _searchKeyboardScreenManager.ClearButtonPressed += ClearSearchText;
            _searchKeyboardScreenManager.PredictionButtonPressed += OnSearchKeyboardScreenPredictionButtonPressed;
        }

        public void Dispose()
        {
            if (_searchScreenManager != null)
            {
                _searchScreenManager.SearchButtonPressed -= OnSearchScreenSearchButtonPressed;
                _searchScreenManager.CancelButtonPressed -= OnSearchScreenCancelButtonPressed;
            }

            if (_searchKeyboardScreenManager != null)
            {
                _searchKeyboardScreenManager.KeyPressed -= OnSearchKeyboardScreenKeyPressed;
                _searchKeyboardScreenManager.DeleteButtonPressed -= OnSearchKeyboardScreenDeleteButtonPressed;
                _searchKeyboardScreenManager.ClearButtonPressed -= ClearSearchText;
                _searchKeyboardScreenManager.PredictionButtonPressed -= OnSearchKeyboardScreenPredictionButtonPressed;
            }
        }

        public void OnLevelCollectionSelected(IAnnotatedBeatmapLevelCollection annotatedBeatmaplevelCollection)
        {
            _originalLevelCollection = annotatedBeatmaplevelCollection;
            _wordPredictionEngine.SetActiveWordStorageFromLevelCollection(annotatedBeatmaplevelCollection);

            // TODO: maybe have a toggle on whether to clear search query on level collection switch
            _searchText.Clear();
            UpdateView();
        }

        public bool ApplyModifications(IEnumerable<IPreviewBeatmapLevel> levelCollection, out IEnumerable<IPreviewBeatmapLevel> modifiedLevelCollection)
        {
            if (_searchText.Length == 0)
            {
                Plugin.Log.Notice("search: no changes");
                modifiedLevelCollection = levelCollection;
                return false;
            }
            else if (_wordSearchEngine.SearchFinished)
            {
                modifiedLevelCollection = levelCollection.Where(x => _wordSearchEngine.CachedResult.Contains(x));
                Plugin.Log.Notice($"search: search finished, resultCount={_wordSearchEngine.CachedResult.Count()}, levelCollectionCount={modifiedLevelCollection.Count()}");
                return true;
            }
            else
            {
                // search results are not ready yet
                // show an empty list first and show the results later
                Plugin.Log.Notice("search: no results, show empty");
                modifiedLevelCollection = Array.Empty<IPreviewBeatmapLevel>();
                return true;
            }
        }

        private void UpdateView()
        {
            string searchText = _searchText.ToString();

            _searchScreenManager.SearchText = searchText;
            _searchKeyboardScreenManager.SearchText = searchText;
            _searchKeyboardScreenManager.SetSuggestedWords(_wordPredictionEngine.GetSuggestedWords(searchText));
        }

        private void UpdateSearchResults(bool startNewSearch = true)
        {
            if (startNewSearch)
                _wordSearchEngine.StartNewSearch(_originalLevelCollection.beatmapLevelCollection.beatmapLevels, _searchText.ToString(), OnSearchFinished);
            else
                _wordSearchEngine.StartSearchOnExistingList(_searchText.ToString(), OnSearchFinished);
        }

        private void OnSearchFinished(IEnumerable<IPreviewBeatmapLevel> levels)
        {
            RequestLevelCollectionRefresh();
        }

        private void ClearSearchText()
        {
            _searchText.Clear();

            UpdateSearchResults();
            UpdateView();
            RequestLevelCollectionRefresh();
        }

        private void OnSearchScreenSearchButtonPressed()
        {
            if (_searchKeyboardScreenManager.IsVisible)
                _searchKeyboardScreenManager.HideScreen();
            else
                _searchKeyboardScreenManager.ShowScreen();
        }

        private void OnSearchScreenCancelButtonPressed()
        {
            if (_searchKeyboardScreenManager.IsVisible)
                _searchKeyboardScreenManager.HideScreen();

            ClearSearchText();
        }

        private void OnSearchKeyboardScreenKeyPressed(char key)
        {
            _searchText.Append(key);

            UpdateSearchResults(_searchText.Length <= 1);
            UpdateView();
            RequestLevelCollectionRefresh();
        }

        private void OnSearchKeyboardScreenDeleteButtonPressed()
        {
            if (_searchText.Length > 0)
                _searchText.Remove(_searchText.Length - 1, 1);

            UpdateSearchResults();
            UpdateView();
            RequestLevelCollectionRefresh();
        }

        private void OnSearchKeyboardScreenPredictionButtonPressed(SuggestedWord suggestedWord)
        {
            string searchText = _searchText.ToString();
            string[] searchTextWords = StringUtilities.RemoveSymbolsRegex.Replace(searchText, " ").Split(StringUtilities.SpaceCharArray);

            if (searchTextWords.Length == 0)
            {
                // this should never be able to happen
                // implies we got a suggested word from an empty search query
                _searchText.Clear();
                _searchText.Append(suggestedWord.Word);
            }
            else
            {
                char lastChar = searchText[searchText.Length - 1];
                string lastSearchWord = searchTextWords[searchTextWords.Length - 1];

                if (suggestedWord.Type == SuggestionType.Prefixed || suggestedWord.Type == SuggestionType.FuzzyMatch)
                {
                    int index = searchText.LastIndexOf(lastSearchWord);
                    _searchText.Remove(index, _searchText.Length - index);
                }
                else if (suggestedWord.Type == SuggestionType.FollowUp)
                {
                    _searchText.Append(lastChar == ' ' ? "" : " ");
                }
                else
                {
                    _searchText.Clear();
                }

                _searchText.Append(suggestedWord.Word);
            }

            UpdateSearchResults(false);
            UpdateView();
            RequestLevelCollectionRefresh();
        }

        private void RequestLevelCollectionRefresh()
        {
            try
            {
                LevelCollectionRefreshRequested?.Invoke();
            }
            catch (Exception e)
            {
                Plugin.Log.Warn($"Unexpected error occurred in {nameof(SearchManager)}:{nameof(LevelCollectionRefreshRequested)} event");
                Plugin.Log.Warn(e);
            }
        }
    }
}
