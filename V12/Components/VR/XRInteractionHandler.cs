using Godot;
using System;
using System.Collections.Generic;

namespace V12.Components.VR
{
    [Obsolete("XRInteractionHandler shouldn't be used, use VRInteractionController instead (I would delete this but idk if charlie still wants it)")]
    public partial class XRInteractionHandler : Node
    {
        // Events for button presses
        public event Action<string, string>? ButtonPressed; // (hand, button)
        public event Action<string, float>? TriggerChanged; // (hand, value)
        public event Action<string, Vector2>? StickChanged; // (hand, value)

        // Call this from your controller nodes' signals
        public void OnButtonPressed(string hand, string button)
        {
            GD.Print($"[XRInteractionHandler] {hand} button pressed: {button}");
            ButtonPressed?.Invoke(hand, button);
        }

        // Convenience methods to connect XRController3D signals directly
        public void OnLeftNodeButtonPressed(string button)
        {
            OnButtonPressed("left", button);
            GD.Print($"[XRInteractionHandler] Left node button pressed: {button}");
        }
        public void OnRightNodeButtonPressed(string button)
        {
            OnButtonPressed("right", button);
        }

        public void OnTriggerChanged(string hand, float value)
        {
            GD.Print($"[XRInteractionHandler] {hand} trigger value: {value}");
            TriggerChanged?.Invoke(hand, value);
        }

        public void OnStickChanged(string hand, Vector2 value)
        {
            GD.Print($"[XRInteractionHandler] {hand} stick value: {value}");
            StickChanged?.Invoke(hand, value);
        }

        public void OnLeftNodeFloatChanged(string action, float value)
        {
            OnTriggerChanged("left", value);
        }
        public void OnRightNodeFloatChanged(string action, float value)
        {
            OnTriggerChanged("right", value);
        }

        public void OnLeftNodeVector2Changed(string action, Vector2 value)
        {
            OnStickChanged("left", value);
        }
        public void OnRightNodeVector2Changed(string action, Vector2 value)
        {
            OnStickChanged("right", value);
        }

        // Poll OpenXR actions for left/right hand-specific actions only
        public void PollActions()
        {
            // Button clicks (trigger, grip, menu, select, ax/by, primary/secondary)
            PollButton("trigger_click_left", "left", "trigger_click");
            PollButton("trigger_click_right", "right", "trigger_click");
            PollButton("grip_click_left", "left", "grip_click");
            PollButton("grip_click_right", "right", "grip_click");
            PollButton("menu_button_left", "left", "menu_button");
            PollButton("menu_button_right", "right", "menu_button");
            PollButton("select_button_left", "left", "select_button");
            PollButton("select_button_right", "right", "select_button");
            PollButton("ax_button_left", "left", "ax_button");
            PollButton("ax_button_right", "right", "ax_button");
            PollButton("by_button_left", "left", "by_button");
            PollButton("by_button_right", "right", "by_button");
            PollButton("primary_click_left", "left", "primary_click");
            PollButton("primary_click_right", "right", "primary_click");
            PollButton("secondary_click_left", "left", "secondary_click");
            PollButton("secondary_click_right", "right", "secondary_click");

            // Analog triggers
            PollAnalog("trigger_left", "left");
            PollAnalog("trigger_right", "right");
            PollAnalog("grip_left", "left_grip");
            PollAnalog("grip_right", "right_grip");

            // Thumbstick/trackpad (primary/secondary)
            PollStick("primary_left", "left");
            PollStick("primary_right", "right");
            PollStick("secondary_left", "left_secondary");
            PollStick("secondary_right", "right_secondary");
        }

        private void PollButton(string action, string hand, string buttonName)
        {
            if (Input.IsActionJustPressed(action))
                OnButtonPressed(hand, buttonName);
        }

        private void PollAnalog(string action, string handName)
        {
            float value = Input.GetActionStrength(action);
            OnTriggerChanged(handName, value);
        }

        private void PollStick(string actionPrefix, string handName)
        {
            // e.g. primary_left_left, primary_left_right, primary_left_up, primary_left_down
            Vector2 stick = Input.GetVector(
                actionPrefix + "_left",
                actionPrefix + "_right",
                actionPrefix + "_up",
                actionPrefix + "_down");
            OnStickChanged(handName, stick);
        }
    }
}
