using System;
using System.Collections.Generic;

namespace YARG.Menu.Calibrator
{
    // [pessoal] Math for the guided calibration. Kept free of Unity dependencies so it can be
    // unit-tested outside the editor.
    public static class CalibrationMath
    {
        /// <summary>
        /// Minimum number of accepted taps (upstream required more than 8).
        /// </summary>
        public const int MIN_TAPS = 9;

        /// <summary>
        /// Taps within max(this, 3 robust standard deviations) of the median are accepted
        /// (50 ms is the upstream drop threshold).
        /// </summary>
        public const double OUTLIER_FLOOR = 0.050;

        /// <summary>
        /// Largest delay accepted: beats are 0.75 s apart, so near 0.375 s an early and a late tap look alike.
        /// </summary>
        public const double MAX_DELAY = 0.300;

        public const double GOOD_SPREAD = 0.012;
        public const double FAIR_SPREAD = 0.025;

        public enum Consistency
        {
            Good,
            Fair,
            Poor,
        }

        public readonly struct Result
        {
            /// <summary>Median delay of the taps after the beat, in seconds (positive = late).</summary>
            public readonly double Delay;

            /// <summary>Robust standard deviation of the accepted taps (1.4826 * MAD), in seconds.</summary>
            public readonly double Spread;

            public readonly int Used;
            public readonly int Discarded;

            public Result(double delay, double spread, int used, int discarded)
            {
                Delay = delay;
                Spread = spread;
                Used = used;
                Discarded = discarded;
            }

            public Consistency Consistency =>
                Spread <= GOOD_SPREAD ? Consistency.Good
                : Spread <= FAIR_SPREAD ? Consistency.Fair
                : Consistency.Poor;

            /// <summary>
            /// False when the median can't be trusted: more than a quarter of the taps discarded (erratic tapping,
            /// or two clusters because a delay near half a beat folds around the nearest-beat measurement), or a
            /// delay so close to half a beat that early and late can't be told apart.
            /// </summary>
            public bool IsReliable =>
                Discarded * 4 <= Used + Discarded && Math.Abs(Delay) <= MAX_DELAY;
        }

        /// <summary>
        /// Calculates the tap delay from tap times on the audio timeline.
        /// </summary>
        /// <remarks>
        /// Each tap is measured against the nearest beat (beats at multiples of <paramref name="secondsPerBeat"/>).
        /// Taps far from the median (double taps, stray presses) are discarded individually; unlike the
        /// upstream filter, which compared each tap with the previous one, a missed beat doesn't discard
        /// the following good taps.
        /// </remarks>
        public static bool TryCalculate(IReadOnlyList<double> tapTimes, double secondsPerBeat, out Result result)
        {
            result = default;
            if (tapTimes == null || tapTimes.Count < MIN_TAPS || !(secondsPerBeat > 0))
                return false;

            var deviations = new List<double>(tapTimes.Count);
            foreach (double time in tapTimes)
            {
                double nearestBeat = Math.Round(time / secondsPerBeat) * secondsPerBeat;
                deviations.Add(time - nearestBeat);
            }

            double median = Median(deviations);
            double spread = RobustSpread(deviations, median);
            double cut = Math.Max(OUTLIER_FLOOR, 3 * spread);

            var accepted = deviations.FindAll(d => Math.Abs(d - median) <= cut);
            if (accepted.Count < MIN_TAPS)
                return false;

            double finalMedian = Median(accepted);
            result = new Result(finalMedian, RobustSpread(accepted, finalMedian), accepted.Count,
                deviations.Count - accepted.Count);
            return true;
        }

        /// <summary>
        /// Input calibration for a profile that gives the same total compensation as setting the global
        /// audio calibration to <paramref name="globalAudioCalibration"/>, keeping the current audio calibration.
        /// </summary>
        public static long ProfileInputCalibration(int globalAudioCalibration, int currentAudioCalibration)
            => (long) globalAudioCalibration - currentAudioCalibration;

        private static double Median(List<double> values)
        {
            var sorted = new List<double>(values);
            sorted.Sort();
            int mid = sorted.Count / 2;
            return sorted.Count % 2 != 0 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
        }

        private static double RobustSpread(List<double> values, double median)
        {
            var absolute = new List<double>(values.Count);
            foreach (double v in values)
                absolute.Add(Math.Abs(v - median));
            return 1.4826 * Median(absolute);
        }
    }
}