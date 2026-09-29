using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using YARG.Helpers.Extensions;
using YARG.Input.Serialization;

namespace YARG.Input
{
    public enum DebounceMode
    {
        /// <summary>Activates on press only.</summary>
        Press,

        /// <summary>Activates on release only.</summary>
        Release,

        /// <summary>Activates on both press and release.</summary>
        PressAndRelease,
    }

    public class SingleButtonBinding : SingleBinding<float>
    {
        private const bool INVERT_DEFAULT = false;
        private const DebounceMode DEBOUNCE_MODE_DEFAULT = DebounceMode.Press;
        private const long DEBOUNCE_THRESHOLD_DEFAULT = 5;
        // [pessoal] 1 = no hysteresis, same as upstream
        private const float RELEASE_THRESHOLD_DEFAULT = AnalogButtonHysteresis.NO_HYSTERESIS;

        private DebounceTimer<float> _debounceTimer = new()
        {
            TimeThreshold = DEBOUNCE_THRESHOLD_DEFAULT,
        };

        private float _invertSign = INVERT_DEFAULT ? -1 : 1;
        private float _pressPoint;

        // [pessoal] Pressed state is kept instead of recalculated, since with hysteresis it depends on history
        private float _releaseThreshold = RELEASE_THRESHOLD_DEFAULT;
        private bool _isPressed;
        private bool _wasPreviouslyPressed;

        public bool Inverted
        {
            get => float.IsNegative(_invertSign);
            set
            {
                bool inverted = Inverted;
                _invertSign = value ? -1 : 1;

                // This state change won't be propogated to the main binding, however calibration settings
                // should never be changed outside of the binding menu, so that should be fine
                if (inverted != Inverted)
                {
                    State = CalculateState(Control.value);
                    UpdatePressed(); // [pessoal]
                    InvokeStateChanged(State);
                }
            }
        }

        public float PressPoint
        {
            get => _pressPoint;
            set
            {
                bool pressed = IsPressed;
                _pressPoint = value;
                UpdatePressed(); // [pessoal]

                // (see above)
                if (pressed != IsPressed)
                {
                    State = CalculateState(Control.value);
                    UpdatePressed(); // [pessoal]
                    InvokeStateChanged(State);
                }
            }
        }

        /// <summary>
        /// [pessoal] Fraction of the press point below which a pressed control is released (0-1).
        /// Values below 1 add hysteresis, so analog controls held near the press point don't chatter.
        /// </summary>
        public float ReleaseThreshold
        {
            get => _releaseThreshold;
            set
            {
                bool pressed = IsPressed;
                _releaseThreshold = AnalogButtonHysteresis.SanitizeReleaseThreshold(value);
                UpdatePressed();

                // (see above)
                if (pressed != IsPressed)
                {
                    InvokeStateChanged(State);
                }
            }
        }

        public DebounceMode DebounceMode { get; set; } = DEBOUNCE_MODE_DEFAULT;

        /// <summary>
        /// The debounce time threshold, in milliseconds. Use 0 or less to disable debounce.
        /// </summary>
        public long DebounceThreshold
        {
            get => _debounceTimer.TimeThreshold;
            set => _debounceTimer.TimeThreshold = value;
        }

        public float PreviousState { get; private set; }

        // [pessoal] Without hysteresis these are exactly the upstream expressions
        public bool IsPressed => HasHysteresis ? _isPressed : State >= PressPoint;
        public bool WasPreviouslyPressed => HasHysteresis ? _wasPreviouslyPressed : PreviousState >= PressPoint;

        private bool HasHysteresis => _releaseThreshold < AnalogButtonHysteresis.NO_HYSTERESIS;

        public SingleButtonBinding(InputControl<float> control) : base(control)
        {
        }

        public SingleButtonBinding(InputControl<float> control, ActuationSettings settings)
            : base(control)
        {
            PressPoint = control.GetPressPoint(settings);
            ReleaseThreshold = settings.ButtonReleaseThreshold; // [pessoal]
        }

        public SingleButtonBinding(InputControl<float> control, SerializedInputControl serialized)
            : base(control, serialized)
        {
            if (!serialized.Parameters.TryGetValue(nameof(Inverted), out string invertedText) ||
                !bool.TryParse(invertedText, out bool inverted))
                inverted = INVERT_DEFAULT;

            Inverted = inverted;

            if (!serialized.Parameters.TryGetValue(nameof(PressPoint), out string pressPointText) ||
                !float.TryParse(pressPointText, out float pressPoint))
                pressPoint = control.GetPressPoint();

            PressPoint = pressPoint;

            if (!serialized.Parameters.TryGetValue(nameof(DebounceMode), out string modeText) ||
                !Enum.TryParse<DebounceMode>(modeText, out var debounceMode))
                debounceMode = DEBOUNCE_MODE_DEFAULT;

            DebounceMode = debounceMode;

            if (!serialized.Parameters.TryGetValue(nameof(DebounceThreshold), out string thresholdText) ||
                !long.TryParse(thresholdText, out long debounceThreshold))
                debounceThreshold = DEBOUNCE_THRESHOLD_DEFAULT;

            DebounceThreshold = debounceThreshold;

            // [pessoal] Written by this fork only, always in invariant format
            if (!serialized.Parameters.TryGetValue(nameof(ReleaseThreshold), out string releaseText) ||
                !AnalogButtonHysteresis.TryParse(releaseText, out float releaseThreshold))
                releaseThreshold = RELEASE_THRESHOLD_DEFAULT;

            ReleaseThreshold = releaseThreshold;
        }

        public override SerializedInputControl Serialize()
        {
            var serialized = base.Serialize();
            if (serialized is null)
                return null;

            if (Inverted != INVERT_DEFAULT)
                serialized.Parameters.Add(nameof(Inverted), Inverted.ToString().ToLower());
            if (Math.Abs(PressPoint - Control.GetPressPoint()) >= 0.001)
                serialized.Parameters.Add(nameof(PressPoint), PressPoint.ToString());
            if (DebounceMode != DEBOUNCE_MODE_DEFAULT)
                serialized.Parameters.Add(nameof(DebounceMode), DebounceMode.ToString());
            if (DebounceThreshold != DEBOUNCE_THRESHOLD_DEFAULT)
                serialized.Parameters.Add(nameof(DebounceThreshold), DebounceThreshold.ToString());
            // [pessoal]
            if (Math.Abs(ReleaseThreshold - RELEASE_THRESHOLD_DEFAULT) >= 0.001)
                serialized.Parameters.Add(nameof(ReleaseThreshold), AnalogButtonHysteresis.Format(ReleaseThreshold));

            return serialized;
        }

        public override void UpdateState(double time)
        {
            PreviousState = State;
            _wasPreviouslyPressed = _isPressed; // [pessoal]

            // Read new state
            _debounceTimer.UpdateValue(CalculateState(Control.value));

            // Wait for debounce to end
            if (!_debounceTimer.HasElapsed(time))
                return;

            State = _debounceTimer.Stop();
            UpdatePressed(); // [pessoal]

            if (DebounceMode == DebounceMode.PressAndRelease ||
                (IsPressed && DebounceMode == DebounceMode.Press) ||
                (!IsPressed && DebounceMode == DebounceMode.Release))
            {
                _debounceTimer.Start(time);
            }

            InvokeStateChanged(State);
        }

        public override void ResetState()
        {
            PreviousState = default;
            State = default;
            // [pessoal] Same result as the upstream calculation on the default state
            _wasPreviouslyPressed = AnalogButtonHysteresis.IsPressed(false, PreviousState, PressPoint, ReleaseThreshold);
            _isPressed = AnalogButtonHysteresis.IsPressed(false, State, PressPoint, ReleaseThreshold);
            _debounceTimer.Stop();
            InvokeStateChanged(State);
        }

        private float CalculateState(float rawValue)
        {
            return rawValue * _invertSign;
        }

        // [pessoal]
        private void UpdatePressed()
        {
            _isPressed = AnalogButtonHysteresis.IsPressed(_isPressed, State, PressPoint, ReleaseThreshold);
        }

        public void UpdateDebounce(double time)
        {
            if (!_debounceTimer.IsRunning || !_debounceTimer.HasElapsed(time))
                return;

            State = _debounceTimer.Stop();
            UpdatePressed(); // [pessoal]
            InvokeStateChanged(State);
            return;
        }
    }

    public class ButtonBinding : ControlBinding<float, SingleButtonBinding>
    {
        protected const long DEBOUNCE_DEFAULT = 5;

        protected DebounceTimer<bool> _debounceTimer = new()
        {
            TimeThreshold = DEBOUNCE_DEFAULT,
        };

        public long DebounceThreshold
        {
            get => _debounceTimer.TimeThreshold;
            set => _debounceTimer.TimeThreshold = value;
        }

        public bool State { get; protected set; }

        public ButtonBinding(string name, int action) : base(name, action)
        {
        }

        public ButtonBinding(string name, string nameLefty, int action) : base(name, nameLefty, action)
        {
        }

        protected override Dictionary<string, string> SerializeParameters()
        {
            var parameters = new Dictionary<string, string>();

            if (DebounceThreshold != DEBOUNCE_DEFAULT)
                parameters.Add(nameof(DebounceThreshold), DebounceThreshold.ToString());

            return parameters;
        }

        protected override void DeserializeParameters(Dictionary<string, string> parameters)
        {
            if (!parameters.TryGetValue(nameof(DebounceThreshold), out string debounceText) ||
                !long.TryParse(debounceText, out long debounce))
                debounce = DEBOUNCE_DEFAULT;

            DebounceThreshold = debounce;
        }

        public override bool IsControlActuated(ActuationSettings settings, InputControl<float> control)
        {
            float previousValue = control.ReadValueFromPreviousFrame();
            float value = control.ReadValue();
            bool actuated = Math.Abs(value - previousValue) >= settings.AxisDeltaThreshold;

            if (control is ButtonControl button)
                return actuated && value >= button.pressPointOrDefault;
            else
                return actuated;
        }

        protected override void OnStateChanged(SingleButtonBinding binding, double time)
        {
            bool state = binding.IsPressed;
            foreach (var other in _bindings)
            {
                if (other == binding)
                    continue;

                other.UpdateDebounce(time);
                state |= other.IsPressed;
            }

            // Ignore if state is unchanged
            if (state == State)
                return;

            // Ignore presses/releases within the debounce threshold
            _debounceTimer.UpdateValue(state);
            if (!_debounceTimer.HasElapsed(time))
                return;

            State = _debounceTimer.Stop();
            FireInputEvent(time, State);

            // Already fired in ControlBinding
            // FireStateChanged();

            // Only start collective debounce on button press
            if (State && !_debounceTimer.IsRunning)
                _debounceTimer.Start(time);
        }

        public override void UpdateForFrame(double updateTime)
        {
            UpdateDebounce(updateTime);
        }

        private void UpdateDebounce(double updateTime)
        {
            // Update individual debounces
            bool collectiveState = false;
            foreach (var binding in _bindings)
            {
                binding.UpdateDebounce(updateTime);
                collectiveState |= binding.IsPressed;
            }

            // Ignore presses/releases within the debounce threshold
            _debounceTimer.UpdateValue(collectiveState);
            if (!_debounceTimer.HasElapsed(updateTime))
                return;

            bool state = _debounceTimer.Stop();
            // Ignore if state is unchanged
            if (state == State)
                return;

            State = state;
            FireInputEvent(updateTime, state);
            FireStateChanged();
        }

        protected override SingleButtonBinding CreateBinding(ActuationSettings settings, InputControl<float> control)
        {
            return new(control, settings);
        }

        protected override SingleButtonBinding DeserializeControl(InputControl<float> control, SerializedInputControl serialized)
        {
            return new(control, serialized);
        }
    }
}