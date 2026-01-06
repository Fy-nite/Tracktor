using Godot;
using System;
using System.Collections.Generic;

namespace V12.Components.VR
{

    public partial class XRInteractionHandler : Node
    {
        /// <summary>
        /// Represents the method that will handle an event when a button is pressed on an XR controller.   
        /// </summary>
        /// <param name="button">The name of the button that was pressed. This value identifies which button triggered the event.</param>
        /// <param name="controller">The XR controller instance on which the button press occurred. Provides context about the source of the
        /// event.</param>
        public delegate void XRButtonPressedEvent(string button, XRController3D controller);
        /// <summary>
        /// Represents the method that will handle an event when a button is released on an XR controller.
        /// </summary>
        /// <param name="button">The name of the button that was released. This value identifies which button triggered the event.</param>
        /// <param name="controller">The XR controller instance associated with the button release event. Provides context about the source
        /// device.</param>
        public delegate void XRButtonReleasedEvent(string button, XRController3D controller);
        /// <summary>
        /// Represents the method that handles an event when a button is touched on an XR controller.
        /// </summary>
        /// <param name="button">The name of the button that was touched. This value identifies which button triggered the event.</param>
        /// <param name="controller">The XR controller instance on which the button was touched. Provides context about the source of the event.</param>
        public delegate void XRButtonTouchedEvent(string button, XRController3D controller);
        /// <summary>
        /// Represents a method that handles joystick movement events from an XR controller, providing the current axis
        /// position and the source controller.
        /// </summary>
        /// <param name="axis">The current position of the joystick axis, typically ranging from -1 to 1 on each component, where (0, 0)
        /// represents the neutral position.</param>
        /// <param name="controller">The XR controller that generated the joystick movement event.</param>
        public delegate void XRJoystickMovedEvent(Vector2 axis, XRController3D controller);
        /// <summary>
        /// Represents the method that handles an event when the trigger on an XR controller is pulled.
        /// </summary>
        /// <remarks>Use this delegate to respond to trigger actions on XR controllers, such as initiating
        /// interactions or actions in a 3D environment. The value parameter may vary depending on the controller's
        /// hardware and configuration.</remarks>
        /// <param name="value">The analog value of the trigger pull, typically ranging from 0.0 (not pressed) to 1.0 (fully pressed).</param>
        /// <param name="controller">The XR controller instance that generated the trigger pull event.</param>
        public delegate void XRTriggerPulledEvent(double value, XRController3D controller);
        /// <summary>
        /// Represents a method that is called when the grip input is pulled on an XR controller.
        /// </summary>
        /// <param name="value">The normalized grip value, typically ranging from 0.0 (not pressed) to 1.0 (fully pressed).</param>
        /// <param name="controller">The XR controller instance that triggered the grip event.</param>
        public delegate void XRGripPulledEvent(double value, XRController3D controller);
        /// <summary>
        /// Represents the method that will handle an event when a two-step trigger is clicked on an XR controller.
        /// </summary>
        /// <remarks>This delegate is intended for use with XR headsets that feature two-step triggers,
        /// allowing applications to respond to distinct trigger stages. The event is typically raised when the user
        /// actuates the trigger beyond the first stage.</remarks>
        /// <param name="controller">The XR controller instance that raised the event. Typically represents a device with a two-step trigger,
        /// such as the Vive Cosmos or Valve Index.</param>
        public delegate void XRTwoStepTriggerClickedEvent(XRController3D controller); // meant for headsets that have two-step triggers like vive cosmos or Valves Steam frame
        public event XRButtonPressedEvent? OnXRButtonPressed;
        public event XRButtonReleasedEvent? OnXRButtonReleased;
        public event XRButtonTouchedEvent? OnXRButtonTouched;
        public event XRJoystickMovedEvent? OnXRJoystickMoved;
        public event XRTriggerPulledEvent? OnXRTriggerPulled;
        public event XRGripPulledEvent? OnXRGripPulled;
        public event XRTwoStepTriggerClickedEvent? OnXRTwoStepTriggerClicked;

        public void RegisterVRSetup(Node3D Root)
        {
            var Lefthand = Root.GetNodeOrNull<XRController3D>("LeftHand");
            if (Lefthand != null)
            {
                Lefthand.ButtonPressed += (button) =>
                {
                    OnXRButtonPressed?.Invoke(button, Lefthand);
                };
                Lefthand.ButtonReleased += (button) =>
                {
                    OnXRButtonReleased?.Invoke(button, Lefthand);
                };
                Lefthand.InputVector2Changed += (name,axis) =>
                {
                    OnXRJoystickMoved?.Invoke(axis, Lefthand);
                };
                Lefthand.InputFloatChanged += (name, value) =>
                {
                    if (name == "trigger")
                        OnXRTriggerPulled?.Invoke(value, Lefthand);
                    else if (name == "grip")
                        OnXRGripPulled?.Invoke(value, Lefthand);
                };

            }
            else
            {
                GD.PrintErr("[XRInteractionHandler] LeftHand XRController3D not found in VR Player Node3D");
            }
            var Righthand = Root.GetNodeOrNull<XRController3D>("RightHand");
            if (Righthand != null)
            {
                Righthand.ButtonPressed += (button) =>
                {
                    HandleButtonPressed(button, Righthand);
                };
                Righthand.ButtonReleased += (button) =>
                {
                    OnXRButtonReleased?.Invoke(button, Righthand);
                };
                Righthand.InputVector2Changed += (name, axis) =>
                {
                    OnXRJoystickMoved?.Invoke(axis, Righthand);
                };
                Righthand.InputFloatChanged += (name, value) =>
                {
                    if (name == "trigger")
                        OnXRTriggerPulled?.Invoke(value, Righthand);
                    else if (name == "grip")
                        OnXRGripPulled?.Invoke(value, Righthand);
                };
            }
            else
            {
                GD.PrintErr("[XRInteractionHandler] RightHand XRController3D not found in VR Player Node3D");
            }
            GD.Print("[XRInteractionHandler] VR Setup Registered" );
        }
    }
}
