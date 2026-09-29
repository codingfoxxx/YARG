using System.Globalization;

namespace YARG.Input
{
    // [pessoal] Hysteresis for analog buttons (e.g. gamepad triggers bound to frets).
    // Kept free of Unity dependencies so it can be unit-tested outside the editor.
    public static class AnalogButtonHysteresis
    {
        /// <summary>
        /// Release threshold that disables hysteresis (upstream behavior):
        /// the button is released as soon as the value drops below the press point.
        /// </summary>
        public const float NO_HYSTERESIS = 1f;

        /// <summary>
        /// Release threshold used by the Unity Input System for its own buttons
        /// (<c>InputSettings.buttonReleaseThreshold</c> in this project): release at 75% of the press point.
        /// </summary>
        public const float INPUT_SYSTEM_DEFAULT = 0.75f;

        /// <summary>
        /// Calculates the pressed state of an analog button.
        /// </summary>
        /// <remarks>
        /// A released button becomes pressed once the value reaches <paramref name="pressPoint"/>.
        /// A pressed button stays pressed until the value drops below
        /// <paramref name="pressPoint"/> * <paramref name="releaseThreshold"/>,
        /// so a trigger held near the press point doesn't chatter between pressed and released.
        /// With a release threshold of 1 this is exactly <c>value >= pressPoint</c>.
        /// </remarks>
        public static bool IsPressed(bool wasPressed, float value, float pressPoint, float releaseThreshold)
        {
            if (!wasPressed || pressPoint <= 0f)
                return value >= pressPoint;

            return value >= pressPoint * SanitizeReleaseThreshold(releaseThreshold);
        }

        /// <summary>
        /// Clamps a release threshold to (0, 1]; anything invalid disables hysteresis.
        /// </summary>
        public static float SanitizeReleaseThreshold(float releaseThreshold)
        {
            if (float.IsNaN(releaseThreshold) || releaseThreshold <= 0f || releaseThreshold > 1f)
                return NO_HYSTERESIS;

            return releaseThreshold;
        }

        /// <summary>
        /// Formats a release threshold for the bindings file, independent of the system language.
        /// </summary>
        public static string Format(float releaseThreshold)
            => releaseThreshold.ToString("0.###", CultureInfo.InvariantCulture);

        /// <summary>
        /// Parses a release threshold written by <see cref="Format"/>.
        /// </summary>
        public static bool TryParse(string text, out float releaseThreshold)
        {
            if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            {
                releaseThreshold = SanitizeReleaseThreshold(value);
                return true;
            }

            releaseThreshold = NO_HYSTERESIS;
            return false;
        }
    }
}