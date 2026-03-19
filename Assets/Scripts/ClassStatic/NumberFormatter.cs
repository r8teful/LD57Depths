using System;
using UnityEngine;

public static class NumberFormatter {
    private static readonly (double threshold, string suffix)[] Tiers =
    {
        (1_000_000_000_000_000_000.0, "Qi"),   // Quintillion
        (1_000_000_000_000_000.0,     "Qa"),   // Quadrillion
        (1_000_000_000_000.0,         "T"),    // Trillion
        (1_000_000_000.0,             "B"),    // Billion
        (1_000_000.0,                 "M"),    // Million
        (1_000.0,                     "K"),    // Thousand
    };


    /// <summary>Specifies how decimals are handled when formatting a number.</summary>
    public enum DecimalMode {
        /// <summary>No decimals ever — e.g. 1040 → "1K"</summary>
        None,

        /// <summary>Always show exactly one decimal — e.g. 1040 → "1.0K"</summary>
        One,

        /// <summary>Always show exactly two decimals — e.g. 1040 → "1.04K"</summary>
        Two,

        /// <summary>
        /// Smart: use the fewest decimals needed to show the most significant
        /// fractional digit, up to two. Trailing zeros are stripped.
        /// e.g. 1000 → "1K", 1040 → "1.04K", 1400 → "1.4K"
        /// </summary>
        Smart,
    }


    /// <inheritdoc cref="Format(double, DecimalMode)"/>
    public static string Format(int value, DecimalMode decimals = DecimalMode.Smart)
        => Format((double)value, decimals);

    /// <inheritdoc cref="Format(double, DecimalMode)"/>
    public static string Format(long value, DecimalMode decimals = DecimalMode.Smart)
        => Format((double)value, decimals);

    /// <inheritdoc cref="Format(double, DecimalMode)"/>
    public static string Format(float value, DecimalMode decimals = DecimalMode.Smart)
        => Format((double)value, decimals);

    /// <summary>
    /// Formats <paramref name="value"/> into a human-readable shorthand string.
    /// </summary>
    /// <param name="value">The number to format.</param>
    /// <param name="decimals">Controls how decimal places are shown.</param>
    /// <returns>A formatted string such as "1.4K", "23M", or "999".</returns>
    public static string Format(double value, DecimalMode decimals = DecimalMode.Smart) {
        // Handle negative numbers by formatting the absolute value and prepending "-".
        if (value < 0)
            return "-" + Format(-value, decimals);

        foreach (var (threshold, suffix) in Tiers) {
            if (value >= threshold) {
                double scaled = value / threshold;
                return FormatScaled(scaled, suffix, decimals);
            }
        }

        // Below 1 000 — format as a plain integer (no suffix).
        return decimals == DecimalMode.None
            ? Mathf.FloorToInt((float)value).ToString()
            : value.ToString("0.##");
    }


    private static string FormatScaled(double scaled, string suffix, DecimalMode mode) {
        string number = mode switch {
            DecimalMode.None => Mathf.FloorToInt((float)scaled).ToString(),
            DecimalMode.One => scaled.ToString("0.0"),
            DecimalMode.Two => scaled.ToString("0.00"),
            DecimalMode.Smart => SmartFormat(scaled),
            _ => scaled.ToString("0.##"),
        };

        return number + suffix;
    }

    /// <summary>
    /// Returns the shortest representation of <paramref name="scaled"/> that
    /// preserves its two most significant fractional digits (trailing zeros stripped).
    /// </summary>
    private static string SmartFormat(double scaled) {
        // Try zero decimals first.
        double floored = Math.Floor(scaled);

        // One decimal captures .1 increments.
        double oneDecimal = Math.Round(scaled, 1);
        // Two decimals captures .01 increments.
        double twoDecimals = Math.Round(scaled, 2);

        // Use the fewest decimal places whose rounded value equals the two-decimal value.
        if (Math.Abs(floored - twoDecimals) < 0.001)
            return ((long)floored).ToString();

        if (Math.Abs(oneDecimal - twoDecimals) < 0.0001)
            return oneDecimal.ToString("0.0");

        return twoDecimals.ToString("0.00");
    }
}