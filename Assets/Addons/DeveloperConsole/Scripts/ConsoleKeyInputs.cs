using UnityEngine;
using UnityEngine.InputSystem;

namespace Anarkila.DeveloperConsole {

    /// <summary>
    /// This script handles listening key inputs (old Unity input system)
    /// - enable/disable Developer Console  (default: § or ½ key below ESC)
    /// - submit current inputfield text    (default: enter)
    /// - fill from prediction              (default: tab and down arrow)
    /// - fill inputfield with previous     (default: up arrow)
    /// </summary>
    public class ConsoleKeyInputs : MonoBehaviour {

        private InputAction consoleToggleAction;
        private InputAction submitAction;
        private InputAction searchPreviousAction;
        private InputAction nextSuggestedCommandAction;
        public InputActionAsset Inputs;
        private bool listenActivateKey = true;
        private bool consoleIsOpen = false;
       
        private void Start() {
            GetSettings();
            ConsoleEvents.RegisterListenActivatStateEvent += ActivatorStateChangeEvent;
            ConsoleEvents.RegisterConsoleStateChangeEvent += ConsoleStateChanged;
            ConsoleEvents.RegisterSettingsChangedEvent += GetSettings;

            consoleToggleAction = Inputs.FindAction("ConsoleToggle", true);
            submitAction = Inputs.FindAction("ConsoleSubmit", true);
            searchPreviousAction = Inputs.FindAction("ConsolePrevious", true);
            nextSuggestedCommandAction = Inputs.FindAction("ConsoleSuggest", true);
        }

        private void OnDestroy() {
            ConsoleEvents.RegisterListenActivatStateEvent -= ActivatorStateChangeEvent;
            ConsoleEvents.RegisterConsoleStateChangeEvent -= ConsoleStateChanged;
            ConsoleEvents.RegisterSettingsChangedEvent -= GetSettings;
        }

        private void Update() {
            ListenPlayerInputs();
        }

        private void ListenPlayerInputs() {
            // If you wish to move into the new Unity Input system, modify this.
            if (consoleToggleAction.WasPressedThisFrame() && listenActivateKey) {
                ConsoleEvents.SetConsoleState(!consoleIsOpen);
            }

            if (!listenActivateKey) {
                consoleIsOpen = ConsoleManager.IsConsoleOpen();
            }

            // If console is not open then don't check other input keys
            if (!consoleIsOpen) return;

            if (submitAction.WasPressedThisFrame()) {
                ConsoleEvents.InputFieldSubmit();
            }

            if (searchPreviousAction.WasPressedThisFrame()) {
                ConsoleEvents.SearchPreviousCommand();
            }

            if (nextSuggestedCommandAction.WasPressedThisFrame()) {
                ConsoleEvents.FillCommand();
            }
        }

        private void ActivatorStateChangeEvent(bool enabled) {
            listenActivateKey = enabled;
        }

        private void ConsoleStateChanged(bool state) {
            consoleIsOpen = state;
        }

        private void GetSettings() {
            var settings = ConsoleManager.GetSettings();
        }
    }
}