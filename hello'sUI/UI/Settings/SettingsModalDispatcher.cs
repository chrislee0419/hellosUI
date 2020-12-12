using System;
using HUI.Utilities;

namespace HUI.UI.Settings
{
    public class SettingsModalDispatcher
    {
        internal event Action ShowModalRequested;
        internal event Action HideModalRequested;
        internal event Action ToggleModalVisibilityRequested;

        public void ShowModal() => this.CallAndHandleAction(ShowModalRequested, nameof(ShowModalRequested));
        public void HideModal() => this.CallAndHandleAction(HideModalRequested, nameof(HideModalRequested));
        public void ToggleModalVisibility() => this.CallAndHandleAction(ToggleModalVisibilityRequested, nameof(ToggleModalVisibilityRequested));
    }
}
