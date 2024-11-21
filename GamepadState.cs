// using System;
// using Dalamud.Game;
// using Dalamud.Game.ClientState.GamePad;
// using Dalamud.Hooking;
// using Dalamud.Logging;
// using ImGuiNET;
//
// namespace Gamepad2
// {
//     /// <summary>
//     /// Exposes the game gamepad state to dalamud.
//     ///
//     /// Will block game's gamepad input if <see cref="ImGuiConfigFlags.NavEnableGamepad"/> is set.
//     /// </summary>
//     public unsafe class GamepadState : IDisposable, IServiceType
//     {
//         private readonly Hook<ControllerPoll> gamepadPoll;
//
//         private bool isDisposed;
//
//         private int leftStickX;
//         private int leftStickY;
//         private int rightStickX;
//         private int rightStickY;
//         private int dataOrigin;
//
//         public GamepadState(SigScanner sigScanner)
//         {
//             gamepadPoll = Hook<ControllerPoll>.FromAddress(
//                 sigScanner.ScanText("40 ?? 57 41 ?? 48 81 EC ?? ?? ?? ?? 44 0F ?? ?? ?? ?? ?? ?? ?? 48 8B"),
//                 GamepadPollDetour);
//
//             gamepadPoll.Enable();
//         }
//
//
//         private delegate int ControllerPoll(IntPtr controllerInput);
//
//         /// <summary>
//         /// Gets the pointer to the current instance of the GamepadInput struct.
//         /// </summary>
//         public IntPtr GamepadInputAddress { get; private set; }
//
//         /// <summary>
//         ///     Gets the state of the left analogue stick in the left direction between 0 (not tilted) and 1 (max tilt).
//         /// </summary>
//         public float LeftStickLeft => this.leftStickX < 0 ? -this.leftStickX / 100f : 0;
//
//         /// <summary>
//         ///     Gets the state of the left analogue stick in the right direction between 0 (not tilted) and 1 (max tilt).
//         /// </summary>
//         public float LeftStickRight => this.leftStickX > 0 ? this.leftStickX / 100f : 0;
//
//         /// <summary>
//         ///     Gets the state of the left analogue stick in the up direction between 0 (not tilted) and 1 (max tilt).
//         /// </summary>
//         public float LeftStickUp => this.leftStickY > 0 ? this.leftStickY / 100f : 0;
//
//         /// <summary>
//         ///     Gets the state of the left analogue stick in the down direction between 0 (not tilted) and 1 (max tilt).
//         /// </summary>
//         public float LeftStickDown => this.leftStickY < 0 ? -this.leftStickY / 100f : 0;
//
//         /// <summary>
//         ///     Gets the state of the right analogue stick in the left direction between 0 (not tilted) and 1 (max tilt).
//         /// </summary>
//         public float RightStickLeft => this.rightStickX < 0 ? -this.rightStickX / 100f : 0;
//
//         /// <summary>
//         ///     Gets the state of the right analogue stick in the right direction between 0 (not tilted) and 1 (max tilt).
//         /// </summary>
//         public float RightStickRight => this.rightStickX > 0 ? this.rightStickX / 100f : 0;
//
//         /// <summary>
//         ///     Gets the state of the right analogue stick in the up direction between 0 (not tilted) and 1 (max tilt).
//         /// </summary>
//         public float RightStickUp => this.rightStickY > 0 ? this.rightStickY / 100f : 0;
//
//         /// <summary>
//         ///     Gets the state of the right analogue stick in the down direction between 0 (not tilted) and 1 (max tilt).
//         /// </summary>
//         public float RightStickDown => this.rightStickY < 0 ? -this.rightStickY / 100f : 0;
//
//         /// <summary>
//         /// Gets buttons pressed bitmask, set once when the button is pressed. See <see cref="GamepadButtons"/> for the mapping.
//         ///
//         /// Exposed internally for Debug Data window.
//         /// </summary>
//         internal ushort ButtonsPressed { get; private set; }
//
//         /// <summary>
//         /// Gets raw button bitmask, set the whole time while a button is held. See <see cref="GamepadButtons"/> for the mapping.
//         ///
//         /// Exposed internally for Debug Data window.
//         /// </summary>
//         internal ushort ButtonsRaw { get; private set; }
//
//         /// <summary>
//         /// Gets button released bitmask, set once right after the button is not hold anymore. See <see cref="GamepadButtons"/> for the mapping.
//         ///
//         /// Exposed internally for Debug Data window.
//         /// </summary>
//         internal ushort ButtonsReleased { get; private set; }
//
//         /// <summary>
//         /// Gets button repeat bitmask, emits the held button input in fixed intervals. See <see cref="GamepadButtons"/> for the mapping.
//         ///
//         /// Exposed internally for Debug Data window.
//         /// </summary>
//         internal ushort ButtonsRepeat { get; private set; }
//
//         /// <summary>
//         /// Gets whether <paramref name="button"/> has been pressed.
//         ///
//         /// Only true on first frame of the press.
//         /// If ImGuiConfigFlags.NavEnableGamepad is set, this is unreliable.
//         /// </summary>
//         /// <param name="button">The button to check for.</param>
//         /// <returns>1 if pressed, 0 otherwise.</returns>
//         public float Pressed(GamepadButtons button) => (this.ButtonsPressed & (ushort)button) > 0 ? 1 : 0;
//
//         /// <summary>
//         /// Gets whether <paramref name="button"/> is being pressed.
//         ///
//         /// True in intervals if button is held down.
//         /// If ImGuiConfigFlags.NavEnableGamepad is set, this is unreliable.
//         /// </summary>
//         /// <param name="button">The button to check for.</param>
//         /// <returns>1 if still pressed during interval, 0 otherwise or in between intervals.</returns>
//         public float Repeat(GamepadButtons button) => (this.ButtonsRepeat & (ushort)button) > 0 ? 1 : 0;
//
//         /// <summary>
//         /// Gets whether <paramref name="button"/> has been released.
//         ///
//         /// Only true the frame after release.
//         /// If ImGuiConfigFlags.NavEnableGamepad is set, this is unreliable.
//         /// </summary>
//         /// <param name="button">The button to check for.</param>
//         /// <returns>1 if released, 0 otherwise.</returns>
//         public float Released(GamepadButtons button) => (this.ButtonsReleased & (ushort)button) > 0 ? 1 : 0;
//
//         /// <summary>
//         /// Gets the raw state of <paramref name="button"/>.
//         ///
//         /// Is set the entire time a button is pressed down.
//         /// </summary>
//         /// <param name="button">The button to check for.</param>
//         /// <returns>1 the whole time button is pressed, 0 otherwise.</returns>
//         public float Raw(GamepadButtons button) => (this.ButtonsRaw & (ushort)button) > 0 ? 1 : 0;
//
//         /// <summary>
//         /// Disposes this instance, alongside its hooks.
//         /// </summary>
//         void IDisposable.Dispose()
//         {
//             DisposeInternal(true);
//             GC.SuppressFinalize(this);
//         }
//
//         public void Block(GamepadButtons button)
//         {
//             ButtonsRaw &= (ushort)~button;
//             ButtonsPressed &= (ushort)~button;
//             ButtonsRepeat &= (ushort)~button;
//             ButtonsReleased &= (ushort)~button;
//             //dataOrigin &= (ushort)~button;
//         }
//
//         private int GamepadPollDetour(IntPtr gamepadInput)
//         {
//             var original = gamepadPoll.Original(gamepadInput);
//             try
//             {
//                 GamepadInputAddress = gamepadInput;
//                 var input = (GamepadInput*)gamepadInput;
//                 //
//                 // var originalTemp = dataOrigin;
//                 // var leftStickXTemp = leftStickX;
//                 // var leftStickYTemp = leftStickY;
//                 // var rightStickXTemp = rightStickX;
//                 // var rightStickYTemp = rightStickY;
//                 // var ButtonsRawTemp = ButtonsRaw;
//                 // var ButtonsPressedTemp = ButtonsPressed;
//                 // var ButtonsReleasedTemp = ButtonsReleased;
//                 // var ButtonsRepeatTemp = ButtonsRepeat;
//                 //
//                 dataOrigin = original;
//                 leftStickX = input->LeftStickX;
//                 leftStickY = input->LeftStickY;
//                 rightStickX = input->RightStickX;
//                 rightStickY = input->RightStickY;
//                 ButtonsRaw = input->ButtonsRaw;
//                 ButtonsPressed = input->ButtonsPressed;
//                 ButtonsReleased = input->ButtonsReleased;
//                 ButtonsRepeat = input->ButtonsRepeat;
//                 //
//                 // input->LeftStickX = leftStickXTemp;
//                 // input->LeftStickY = leftStickYTemp;
//                 // input->RightStickX = rightStickXTemp;
//                 // input->RightStickY = rightStickYTemp;
//                 //
//                 // input->ButtonsRaw = ButtonsRawTemp;
//                 // input->ButtonsPressed = ButtonsPressedTemp;
//                 // input->ButtonsReleased = ButtonsReleasedTemp;
//                 // input->ButtonsRepeat = ButtonsRepeatTemp;
//
//                 // NOTE (Chiv) Not so sure about the return value, does not seem to matter if we return the
//                 // original, zero or do the work adjusting the bits.
//                 return original;
//             }
//             catch (Exception e)
//             {
//                 PluginLog.Error(e, $"Gamepad Poll detour critical error! Gamepad navigation will not work!");
//
//                 return original;
//             }
//         }
//
//         private void DisposeInternal(bool disposing)
//         {
//             if (isDisposed) return;
//             if (disposing)
//             {
//                 gamepadPoll?.Disable();
//                 gamepadPoll?.Dispose();
//             }
//
//             isDisposed = true;
//         }
//     }
//
// }