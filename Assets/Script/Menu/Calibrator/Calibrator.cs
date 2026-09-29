using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using YARG.Audio;
using YARG.Core.Audio;
using YARG.Core.Input;
using YARG.Input;
using YARG.Localization;
using YARG.Menu.Navigation;
using YARG.Menu.Persistent;
using YARG.Player;
using YARG.Settings;

namespace YARG.Menu.Calibrator
{
    // TODO: Redo this

    public class Calibrator : MonoBehaviour
    {
        private const float SECONDS_PER_BEAT = 1f / 80f * 60f;
        private const float DROP_THRESH = 0.05f;

        private enum State
        {
            Starting,
            AudioWaiting,
            Audio,
            AudioDone,
            Saved, // [pessoal]
        }

        // [pessoal] Guided calibration: the result can be saved to the profile that tapped or globally
        private const float RESULT_INPUT_DELAY = 1f;
        // [pessoal] The calibration music (15 s, 20 beats) plays twice: ~40 taps instead of ~20 cut the
        // statistical error of the result by ~30% (see CalibrationMathTests in yarg-autochart)
        private const int PASSES = 2;
        private int _pass;
        private int _resultAudioCalibration;
        private long _resultInputCalibration;
        // [pessoal] The info text is sized for one line (64 pt, no auto-size); longer texts use a smaller size
        private const float LONG_TEXT_SCALE = 0.55f;
        private const float MEDIUM_TEXT_SCALE = 0.7f;
        private float _baseFontSize;

        [SerializeField]
        private GameObject _startingStateContainer;

        [SerializeField]
        private GameObject _audioCalibrateContainer;

        [Space]
        [SerializeField]
        private TextMeshProUGUI _audioCalibrateText;

        private State _state = State.Starting;
        private readonly List<double> _calibrationTimes = new();

        private YargPlayer _player;
#nullable enable
        private StemMixer? _mixer;
#nullable disable

        private bool _hasNavigationScheme;

        private void Start()
        {
            // [pessoal] The start button text is fixed in the scene ("Calibrate Audio"); localize it here
            var buttonText = _startingStateContainer.GetComponentInChildren<TextMeshProUGUI>(true);
            if (buttonText != null)
            {
                buttonText.text = Localize.Key("Menu.Calibrator.StartButton");
            }

            _baseFontSize = _audioCalibrateText.fontSize; // [pessoal]
            UpdateForState();
        }

        // [pessoal]
        private void SetInfoText(string text, Color color, float scale)
        {
            _audioCalibrateText.fontSize = _baseFontSize * scale;
            _audioCalibrateText.color = color;
            _audioCalibrateText.text = text;
        }

        private void OnDestroy()
        {
            InputManager.MenuInput -= OnMenuInput;
            ClearNavigation();
            _mixer?.Dispose();
        }

        private void OnMenuInput(YargPlayer player, ref GameInput input)
        {
            // Only detect button downs
            if (!input.Button) return;

            // Only allow inputs from one player at a time
            if (_player is null)
            {
                _player = player;
            }
            else if (_player != player)
            {
                return;
            }

            switch (_state)
            {
                case State.AudioWaiting:
                    _state = State.Audio;
                    UpdateForState();
                    break;
                case State.Audio:
                    double inputAge = InputManager.CurrentInputTime - input.Time;
                    _calibrationTimes.Add(_mixer.GetPosition() - inputAge);

                    // [pessoal] Show how many taps were registered (upstream: "Detected")
                    _audioCalibrateText.color = Color.green;
                    _audioCalibrateText.text = Localize.KeyFormat("Menu.Calibrator.Tap", _calibrationTimes.Count);
                    break;
            }
        }

        private void Update()
        {
            switch (_state)
            {
                case State.Audio:
                    // Fade out text
                    var color = _audioCalibrateText.color;
                    color.a -= Time.deltaTime * 3f;
                    _audioCalibrateText.color = color;
                    break;
            }
        }

        private void UpdateForState()
        {
            _mixer?.Dispose();
            _mixer = null;

            StopAllCoroutines();

            _startingStateContainer.SetActive(false);
            _audioCalibrateContainer.SetActive(false);

            switch (_state)
            {
                case State.Starting:
                    _startingStateContainer.SetActive(true);
                    SetConfirmNavigation();
                    break;
                case State.AudioWaiting:
                    _audioCalibrateContainer.SetActive(true);
                    _player = null;

                    // [pessoal] Full instructions (upstream: two hardcoded English lines)
                    SetInfoText(Localize.Key("Menu.Calibrator.Instructions"), Color.white, LONG_TEXT_SCALE);
                    SetEmptyNavigation();
                    StartCoroutine(EnableInputAfterDelay());
                    break;
                case State.Audio:
                    _audioCalibrateContainer.SetActive(true);
                    _calibrationTimes.Clear();

                    _pass = 1; // [pessoal]
                    _audioCalibrateText.fontSize = _baseFontSize; // [pessoal] count-in and taps at full size
                    StartCalibrationMusic();
                    StartCoroutine(AudioCalibrateCoroutine());
                    break;
                case State.AudioDone:
                    _audioCalibrateContainer.SetActive(true);
                    InputManager.MenuInput -= OnMenuInput;
                    // [pessoal] Result with save options; navigation is enabled after a short delay so
                    // a tap still in flight when the music ends doesn't pick an option
                    bool canSave = CalculateAudioLatency();
                    SetEmptyNavigation();
                    StartCoroutine(EnableResultNavigationAfterDelay(canSave));
                    break;
                case State.Saved: // [pessoal]
                    _audioCalibrateContainer.SetActive(true);
                    SetBackNavigation();
                    break;
            }
        }

        // [pessoal]
        private IEnumerator EnableResultNavigationAfterDelay(bool canSave)
        {
            yield return new WaitForSeconds(RESULT_INPUT_DELAY);

            var entries = new List<NavigationScheme.Entry>();
            if (canSave)
            {
                if (_player != null)
                {
                    entries.Add(new NavigationScheme.Entry(MenuAction.Green, "Menu.Calibrator.SaveProfile",
                        () => SaveToProfile()));
                }

                entries.Add(new NavigationScheme.Entry(MenuAction.Yellow, "Menu.Calibrator.SaveGlobal",
                    () => SaveGlobal()));
            }

            entries.Add(new NavigationScheme.Entry(MenuAction.Blue, "Menu.Calibrator.Repeat", () => StartAudioMode()));
            entries.Add(new NavigationScheme.Entry(MenuAction.Red, "Menu.Common.Back", () => BackButton()));
            SetNavigation(new NavigationScheme(entries, true));
        }

        // [pessoal]
        private void SaveToProfile()
        {
            var profile = _player.Profile;
            profile.InputCalibrationMilliseconds = _resultInputCalibration;
            PlayerContainer.SaveProfiles(false);

            SetInfoText(Localize.KeyFormat("Menu.Calibrator.SavedProfile", profile.Name, _resultInputCalibration),
                Color.green, MEDIUM_TEXT_SCALE);
            _state = State.Saved;
            UpdateForState();
        }

        // [pessoal] Same as the upstream calibrator
        private void SaveGlobal()
        {
            SettingsManager.Settings.AudioCalibration.Value = _resultAudioCalibration;
            SettingsManager.SaveSettings();

            SetInfoText(Localize.KeyFormat("Menu.Calibrator.SavedGlobal", _resultAudioCalibration),
                Color.green, MEDIUM_TEXT_SCALE);
            _state = State.Saved;
            UpdateForState();
        }

        // [pessoal] Moved out of UpdateForState so the music can be restarted for the second pass
        private void StartCalibrationMusic()
        {
            const float SPEED = 1f;
            const double VOLUME = 1.0;
            var file = Path.Combine(Application.streamingAssetsPath, "calibration_music.ogg");

            _mixer = GlobalAudioHandler.LoadCustomFile(file, SPEED, VOLUME);
            _mixer.SongEnd += OnAudioEnd;
            _mixer.Play();
        }

        private IEnumerator EnableInputAfterDelay()
        {
            yield return new WaitForSeconds(0.5f);
            // [pessoal] Never subscribe twice (going back and starting again would count every tap twice)
            InputManager.MenuInput -= OnMenuInput;
            InputManager.MenuInput += OnMenuInput;
        }

        private void SetConfirmNavigation()
        {
            SetNavigation(new NavigationScheme(new()
            {
                new NavigationScheme.Entry(MenuAction.Green, "Menu.Common.Confirm", () => StartAudioMode()),
                new NavigationScheme.Entry(MenuAction.Red, "Menu.Common.Back", () => BackButton()),
            }, true));
        }

        private void SetBackNavigation()
        {
            SetNavigation(new NavigationScheme(new()
            {
                new NavigationScheme.Entry(MenuAction.Red, "Menu.Common.Back", () => BackButton()),
            }, true));
        }

        private void SetEmptyNavigation()
        {
            SetNavigation(NavigationScheme.Empty);
        }

        private void SetNavigation(NavigationScheme scheme)
        {
            ClearNavigation();
            _ = Navigator.Instance.PushScheme(scheme);
            _hasNavigationScheme = true;
        }

        private void ClearNavigation()
        {
            if (!_hasNavigationScheme || Navigator.Instance == null)
            {
                return;
            }

            Navigator.Instance.PopScheme();
            _hasNavigationScheme = false;
        }

        // [pessoal] Upstream dropped each tap that wasn't one beat after the previous one (a missed beat also
        // dropped the next good tap) and set the global audio calibration right away. Now the outliers are
        // discarded individually (CalibrationMath), the result is explained and the player chooses where to save.
        private bool CalculateAudioLatency()
        {
            if (!CalibrationMath.TryCalculate(_calibrationTimes, SECONDS_PER_BEAT, out var result))
            {
                SetInfoText(Localize.Key("Menu.Calibrator.NotEnoughData"), Color.red, MEDIUM_TEXT_SCALE);
                return false;
            }

            // Same global value the upstream calibrator sets
            int delay = (int) Math.Round(result.Delay * 1000);
            int calibration = delay;
            if (SettingsManager.Settings.AccountForHardwareLatency.Value)
                calibration -= GlobalAudioHandler.PlaybackLatency;

            // Profile alternative: keep the audio calibration and move the difference to the player's input
            int currentAudio = SettingsManager.Settings.AudioCalibration.Value;
            _resultAudioCalibration = calibration;
            _resultInputCalibration = CalibrationMath.ProfileInputCalibration(calibration, currentAudio);

            string consistency = Localize.Key("Menu.Calibrator.Consistency", result.Consistency.ToString());
            string profileName = _player?.Profile.Name ?? "-";
            long currentInput = _player?.Profile.InputCalibrationMilliseconds ?? 0;

            SetInfoText(
                Localize.KeyFormat("Menu.Calibrator.Result", delay, (int) Math.Round(result.Spread * 1000),
                    consistency, result.Used, result.Discarded) + "\n\n" +
                Localize.KeyFormat("Menu.Calibrator.Current", currentAudio, profileName, currentInput) + "\n\n" +
                Localize.KeyFormat("Menu.Calibrator.Choose", profileName, _resultInputCalibration, calibration),
                result.Consistency == CalibrationMath.Consistency.Poor ? Color.yellow : Color.white,
                LONG_TEXT_SCALE);
            return true;
        }

        private IEnumerator AudioCalibrateCoroutine()
        {
            _audioCalibrateText.color = Color.white;
            _audioCalibrateText.text = "1";

            yield return new WaitUntil(() => _mixer.GetPosition() >= SECONDS_PER_BEAT * 1f);
            _audioCalibrateText.color = Color.white;
            _audioCalibrateText.text = "2";

            yield return new WaitUntil(() => _mixer.GetPosition() >= SECONDS_PER_BEAT * 2f);
            _audioCalibrateText.color = Color.white;
            _audioCalibrateText.text = "3";

            yield return new WaitUntil(() => _mixer.GetPosition() >= SECONDS_PER_BEAT * 3f);
            _audioCalibrateText.color = Color.white;
            _audioCalibrateText.text = "4";
        }

        public void StartAudioMode()
        {
            _state = State.AudioWaiting;
            UpdateForState();
        }

        public void BackButton()
        {
            if (_state == State.Starting)
            {
                GlobalVariables.Instance.LoadScene(SceneIndex.Menu);
            }
            else
            {
                _state = State.Starting;
                UpdateForState();
            }
        }

        private void OnAudioEnd()
        {
            // [pessoal] Second pass (SongEnd is queued to the main thread by the audio backend).
            // Beats are measured from the start of the file, so the restart keeps the same grid.
            if (_state == State.Audio && _pass < PASSES)
            {
                _pass++;
                _mixer?.Dispose();
                StartCalibrationMusic();
                return;
            }

            _state = State.AudioDone;
            UpdateForState();
        }
    }
}
