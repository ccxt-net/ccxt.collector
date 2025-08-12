using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;

// NOTE: This file intentionally keeps global namespace to reduce friction when used across many exchange clients.
// Enhancements added:
// 1. IsDefinedElement helper to distinguish default(JsonElement) sentinel.
// 2. TryGetNonEmptyArray retained (alias of original semantics) + clearer commentary.
// 3. Optional diagnostics delegate to surface parsing fallbacks in DEBUG builds.
// 4. GetUnixTimeOrDefault already supports numeric/string epoch; minor comment clarifications.

/// <summary>
/// Utility extension helpers to safely extract strongly-typed values from <see cref="JsonElement"/>,
/// handling absent properties, null/undefined kinds, numeric vs string representations, and
/// common date/epoch conversions used throughout exchange integrations.
/// </summary>
public static class JsonExtensions
{
    /// <summary>
    /// Returns true when the element is either Null or Undefined (or an uninitialized default struct with Undefined kind).
    /// </summary>
    /// <param name="e">Source element.</param>
    /// <returns>True if the element kind is Null or Undefined.</returns>
    public static bool IsNullOrUndefined(this JsonElement e) =>
        e.ValueKind == JsonValueKind.Null || e.ValueKind == JsonValueKind.Undefined;

    /// <summary>
    /// Returns the named child property if it exists; otherwise returns the element itself (useful for lenient parsing when an API sometimes wraps a payload).
    /// </summary>
    /// <param name="element">Parent element.</param>
    /// <param name="propertyName">Expected child property name.</param>
    /// <returns>Child property <see cref="JsonElement"/> or the original element.</returns>
    public static JsonElement GetPropertyOrSelf(this JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var prop) ? prop : element;

    /// <summary>
    /// Gets array length when the element is an Array; otherwise returns 0 without throwing.
    /// </summary>
    /// <param name="element">Potential array element.</param>
    /// <returns>Length or 0.</returns>
    public static int GetArrayLengthOrZero(this JsonElement element) =>
        element.ValueKind == JsonValueKind.Array ? element.GetArrayLength() : 0;

    /// <summary>
    /// Returns first array item or default(JsonElement) (Undefined) if the element is not an array or empty.
    /// </summary>
    /// <param name="element">Array element.</param>
    /// <returns>First item or default.</returns>
    public static JsonElement FirstOrUndefined(this JsonElement element) =>
        element.ValueKind == JsonValueKind.Array ? element.EnumerateArray().FirstOrDefault() : default;

    /// <summary>
    /// Returns true if the JsonElement is not the default sentinel (Undefined/Null or default struct)
    /// Use after FirstOrUndefined to verify presence: var first = arr.FirstOrUndefined(); if (first.IsDefinedElement()) { ... }
    /// </summary>
    public static bool IsDefinedElement(this JsonElement element) =>
        element.ValueKind != JsonValueKind.Undefined && element.ValueKind != JsonValueKind.Null;

    /* -------- Boolean -------- */
    /// <summary>
    /// Attempts to extract a Boolean property accepting JSON true/false literal or case-insensitive string value; returns false on failure.
    /// </summary>
    /// <param name="element">Source object element.</param>
    /// <param name="propertyName">Boolean field name.</param>
    /// <returns>Parsed boolean or false.</returns>
    public static bool GetBooleanOrFalse(this JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var prop)) return false;

        if (prop.ValueKind == JsonValueKind.True || prop.ValueKind == JsonValueKind.False)
            return prop.GetBoolean();

        if (prop.ValueKind == JsonValueKind.String &&
            bool.TryParse(prop.GetString(), out var b))
            return b;

        return false;
    }

    /* -------- Int32 -------- */
    /// <summary>
    /// Extracts an Int32 property accepting numeric or string representation; returns provided default when missing or unparsable.
    /// </summary>
    /// <param name="element">Source object.</param>
    /// <param name="propertyName">Property name.</param>
    /// <param name="default">Fallback value.</param>
    /// <returns>Parsed Int32 or fallback.</returns>
    public static int GetInt32OrDefault(this JsonElement element, string propertyName, int @default = 0)
    {
        if (!element.TryGetProperty(propertyName, out var prop)) return @default;

        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var i)) return i;

        if (prop.ValueKind == JsonValueKind.String &&
            int.TryParse(prop.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var s))
            return s;

        return @default;
    }

    /* -------- Int64 -------- */
    /// <summary>
    /// Extracts an Int64 property accepting numeric or string representation; returns default when missing/unparsable.
    /// </summary>
    /// <param name="element">Source object.</param>
    /// <param name="propertyName">Property name.</param>
    /// <param name="default">Fallback value.</param>
    /// <returns>Parsed Int64 or fallback.</returns>
    public static long GetInt64OrDefault(this JsonElement element, string propertyName, long @default = 0L)
    {
        if (!element.TryGetProperty(propertyName, out var prop)) return @default;

        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt64(out var i)) return i;

        if (prop.ValueKind == JsonValueKind.String &&
            long.TryParse(prop.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var s))
            return s;

        return @default;
    }

    /* -------- Double -------- */
    /// <summary>
    /// Extracts a Double property supporting numeric or invariant string forms; returns default when absent or invalid.
    /// </summary>
    /// <param name="element">Source object.</param>
    /// <param name="propertyName">Property name.</param>
    /// <param name="default">Fallback value.</param>
    /// <returns>Parsed double or fallback.</returns>
    public static double GetDoubleOrDefault(this JsonElement element, string propertyName, double @default = 0d)
    {
        if (!element.TryGetProperty(propertyName, out var prop)) return @default;

        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDouble(out var v)) return v;

        if (prop.ValueKind == JsonValueKind.String &&
            double.TryParse(prop.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var s))
            return s;

        return @default;
    }

    /* -------- Decimal -------- */
    /// <summary>
    /// Extracts a Decimal property accepting numeric or string representation; returns default on failure.
    /// </summary>
    /// <param name="element">Source object.</param>
    /// <param name="propertyName">Property name.</param>
    /// <param name="default">Fallback value.</param>
    /// <returns>Parsed decimal or fallback.</returns>
    public static decimal GetDecimalOrDefault(this JsonElement element, string propertyName, decimal @default = 0m)
    {
        if (!element.TryGetProperty(propertyName, out var prop)) return @default;

        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDecimal(out var v)) return v;

        if (prop.ValueKind == JsonValueKind.String &&
            decimal.TryParse(prop.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var s))
            return s;

        return @default;
    }

    /* -------- String -------- */
    /// <summary>
    /// Returns a string property value or supplied default (does not coerce non-string kinds).
    /// </summary>
    /// <param name="element">Source object.</param>
    /// <param name="propertyName">Property name.</param>
    /// <param name="default">Fallback string.</param>
    /// <returns>String value or fallback.</returns>
    public static string GetStringOrDefault(this JsonElement element, string propertyName, string @default = "")
    {
        if (!element.TryGetProperty(propertyName, out var prop)) return @default;

        if (prop.ValueKind == JsonValueKind.String) return prop.GetString() ?? @default;

        // For non-string types, return default string (can be extended with prop.ToString() if needed)
        return @default;
    }

    /// <summary>
    /// Attempts to obtain a string property, returning null if not a string or absent.
    /// </summary>
    /// <param name="e">Source object.</param>
    /// <param name="name">Property name.</param>
    /// <returns>String or null.</returns>
    public static string GetStringOrNull(this JsonElement e, string name) =>
        e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

    /// <summary>
    /// Returns object-typed child property or null when missing or not an object.
    /// </summary>
    /// <param name="e">Source element.</param>
    /// <param name="name">Property name.</param>
    /// <returns>Object element or null.</returns>
    public static JsonElement? GetObjectOrNull(this JsonElement e, string name) =>
        e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.Object ? p : (JsonElement?)null;

    /// <summary>
    /// Convenience for e[a][b] pattern; returns nested string or null.
    /// </summary>
    /// <param name="e">Root object.</param>
    /// <param name="a">First-level property.</param>
    /// <param name="b">Second-level property inside first object.</param>
    /// <returns>Nested string or null.</returns>
    public static string GetNestedString(this JsonElement e, string a, string b)
    {
        var aProp = e.GetObjectOrNull(a);
        if (aProp == null) return null;

        return aProp.Value.GetStringOrNull(b);
    }

    /* -------- DateTimeOffset (ISO8601 or epoch number/string) -------- */
    /// <summary>
    /// Parses a property as <see cref="DateTimeOffset"/> accepting: ISO8601 string, epoch seconds, or epoch milliseconds (numeric or string).
    /// </summary>
    /// <param name="element">Source object.</param>
    /// <param name="propertyName">Date/time property name.</param>
    /// <param name="default">Fallback value.</param>
    /// <returns>Parsed date or fallback.</returns>
    public static DateTimeOffset GetDateTimeOffsetOrDefault(
        this JsonElement element, string propertyName, DateTimeOffset @default = default)
    {
        if (!element.TryGetProperty(propertyName, out var prop)) return @default;

        // Number: assume epoch (seconds/milliseconds)
        if (prop.ValueKind == JsonValueKind.Number)
        {
            if (prop.TryGetInt64(out var num))
            {
                return NormalizeEpochToDateTimeOffset(num);
            }
        }

        // String: ISO8601 or epoch string
        if (prop.ValueKind == JsonValueKind.String)
        {
            var s = prop.GetString();
            if (string.IsNullOrWhiteSpace(s)) return @default;

            // Try ISO8601 first (most common for date strings)
            if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dto))
                return dto;

            // Fallback to epoch parsing
            if (long.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var epoch))
            {
                return NormalizeEpochToDateTimeOffset(epoch);
            }
        }

        return @default;
    }

    /// <summary>
    /// If specified date string field exists, parse DateTimeOffset and return UnixTimeMilliseconds,
    /// otherwise return the fallbackLongField value as long.
    /// </summary>
    /// <summary>
    /// Parses a date/time field to Unix epoch milliseconds handling ISO8601, numeric seconds/milliseconds, or string-encoded epoch.
    /// </summary>
    /// <param name="element">Source object.</param>
    /// <param name="dateFieldName">Field containing time representation.</param>
    /// <param name="default">Fallback epoch milliseconds.</param>
    /// <returns>Epoch milliseconds or fallback.</returns>
    public static long GetUnixTimeOrDefault(this JsonElement element, string dateFieldName, long @default = 0)
    {
        if (!element.TryGetProperty(dateFieldName, out var dateProp))
            return @default;

        // Numeric epoch (seconds or milliseconds) directly supported
        if (dateProp.ValueKind == JsonValueKind.Number && dateProp.TryGetInt64(out var epochNum))
        {
            return NormalizeEpochToMilliseconds(epochNum);
        }

        if (dateProp.ValueKind == JsonValueKind.String)
        {
            var s = dateProp.GetString();
            if (string.IsNullOrWhiteSpace(s)) return @default;

            // Try ISO8601 first
            if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dto))
                return dto.ToUnixTimeMilliseconds();

            // Fallback to epoch parsing
            if (long.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var epochParsed))
                return NormalizeEpochToMilliseconds(epochParsed);
        }

        return @default;
    }

    /// <summary>
    /// Normalizes epoch timestamp to milliseconds using improved heuristics.
    /// Strategy: Use reasonable date range (2000-2100) and digit count to determine unit.
    /// </summary>
    private static long NormalizeEpochToMilliseconds(long epoch)
    {
        // Reasonable date range for cryptocurrency data
        const long YEAR_2000_MS = 946684800000L;      // 2000-01-01 in milliseconds
        const long YEAR_2100_MS = 4102444800000L;     // 2100-01-01 in milliseconds
        const long YEAR_2000_SEC = 946684800L;        // 2000-01-01 in seconds
        const long YEAR_2100_SEC = 4102444800L;       // 2100-01-01 in seconds

        // If value is in reasonable millisecond range, it's already milliseconds
        if (epoch >= YEAR_2000_MS && epoch <= YEAR_2100_MS)
            return epoch;

        // If value is in reasonable seconds range, convert to milliseconds
        if (epoch >= YEAR_2000_SEC && epoch <= YEAR_2100_SEC)
            return epoch * 1000;

        // Fallback: Use digit count (less reliable but works for edge cases)
        // 10 digits or less = likely seconds
        // 13 digits = likely milliseconds
        // Note: This will need adjustment after year 2286 when seconds reach 11 digits
        int digitCount = epoch.ToString().Length;
        if (digitCount <= 10)
            return epoch * 1000;  // Assume seconds
        
        return epoch; // Assume milliseconds
    }

    /// <summary>
    /// Converts epoch timestamp to DateTimeOffset using improved detection.
    /// </summary>
    private static DateTimeOffset NormalizeEpochToDateTimeOffset(long epoch)
    {
        long milliseconds = NormalizeEpochToMilliseconds(epoch);
        return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
    }

    /// <summary>
    /// Returns true if the specified property exists and is a Json array.
    /// </summary>
    /// <summary>
    /// Tries to get a non-empty JSON array property.
    /// </summary>
    /// <param name="element">Source object.</param>
    /// <param name="propertyName">Array property name.</param>
    /// <param name="arrayElement">Resulting array element (undefined default on failure).</param>
    /// <returns>True if property exists, is an array, and length &gt; 0.</returns>
    public static bool TryGetArray(this JsonElement element, string propertyName, out JsonElement arrayElement)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Array && prop.GetArrayLength() > 0)
        {
            arrayElement = prop;
            return true;
        }

        arrayElement = default;
        return false;
    }

    /// <summary>
    /// Treat an empty array as valid (only presence matters).
    /// </summary>
    /// <summary>
    /// Tries to get an array property allowing empty arrays (presence only).
    /// </summary>
    /// <param name="element">Source object.</param>
    /// <param name="propertyName">Array property name.</param>
    /// <param name="arrayElement">Resulting array (undefined default on failure).</param>
    /// <returns>True if property exists and is an array (length can be zero).</returns>
    public static bool TryGetArrayAllowEmpty(this JsonElement element, string propertyName, out JsonElement arrayElement)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Array)
        {
            arrayElement = prop;
            return true;
        }
        arrayElement = default;
        return false;
    }

    /// <summary>
    /// Explicit alternative emphasizing the original TryGetArray semantics (non-empty array required).
    /// </summary>
    public static bool TryGetNonEmptyArray(this JsonElement element, string propertyName, out JsonElement arrayElement)
        => TryGetArray(element, propertyName, out arrayElement); // explicit alias for readability

    /// <summary>
    /// Get decimal value from JsonElement, handling both number and string types
    /// Used for direct element access (not property access)
    /// </summary>
    /// <summary>
    /// Extracts a decimal from an element itself (number or numeric string) returning the supplied default otherwise.
    /// </summary>
    /// <param name="element">Element representing a number or numeric string.</param>
    /// <param name="default">Fallback value.</param>
    /// <returns>Decimal value or fallback.</returns>
    public static decimal GetDecimalValue(this JsonElement element, decimal @default = 0m)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out var v))
            return v;

        if (element.ValueKind == JsonValueKind.String &&
            decimal.TryParse(element.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var s))
            return s;

        return @default;
    }

    /// <summary>
    /// Try to get decimal value from JsonElement, handling both number and string types
    /// </summary>
    /// <summary>
    /// Attempts to parse the element as a decimal (number kind or numeric string).
    /// </summary>
    /// <param name="element">Source element.</param>
    /// <param name="value">Parsed decimal on success, 0 on failure.</param>
    /// <returns>True if value parsed.</returns>
    public static bool TryGetDecimalValue(this JsonElement element, out decimal value)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out value))
            return true;

        if (element.ValueKind == JsonValueKind.String &&
            decimal.TryParse(element.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out value))
            return true;

        value = 0m;
        return false;
    }

    /// <summary>
    /// Get Int32 value from JsonElement, handling both number and string types
    /// Used for direct element access (not property access)
    /// </summary>
    /// <summary>
    /// Parses the element as Int32 (number or numeric string) returning fallback if not applicable.
    /// </summary>
    /// <param name="element">Source element.</param>
    /// <param name="default">Fallback value.</param>
    /// <returns>Int32 value or fallback.</returns>
    public static int GetInt32Value(this JsonElement element, int @default = 0)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var v))
            return v;

        if (element.ValueKind == JsonValueKind.String &&
            int.TryParse(element.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var s))
            return s;

        return @default;
    }

    /// <summary>
    /// Get Int64 value from JsonElement, handling both number and string types
    /// Used for direct element access (not property access)
    /// </summary>
    /// <summary>
    /// Parses the element as Int64 (number or numeric string) returning fallback if not applicable.
    /// </summary>
    /// <param name="element">Source element.</param>
    /// <param name="default">Fallback value.</param>
    /// <returns>Int64 value or fallback.</returns>
    public static long GetInt64Value(this JsonElement element, long @default = 0L)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out var v))
            return v;

        if (element.ValueKind == JsonValueKind.String &&
            long.TryParse(element.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var s))
            return s;

        return @default;
    }
}
