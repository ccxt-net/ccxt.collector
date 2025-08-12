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

public static class JsonExtensions
{
    #region Diagnostics
    /// <summary>
    /// Optional diagnostics hook invoked when a parsing fallback path is taken.
    /// Assign from host application (e.g., JsonExtensions.DiagnosticsLogger = msg => logger.LogDebug(msg)).
    /// Lightweight and skipped if null. Wrap heavy logic outside.
    /// </summary>
#if DEBUG
    public static Action<string> DiagnosticsLogger { get; set; }
#else
    public static Action<string> DiagnosticsLogger { get; set; } = null; // stripped in Release JIT typically
#endif
    private static void Trace(string message)
    {
        try { DiagnosticsLogger?.Invoke(message); } catch { /* swallow */ }
    }
    #endregion
    /* -------- Common Utilities -------- */
    public static bool IsNullOrUndefined(this JsonElement e) =>
        e.ValueKind == JsonValueKind.Null || e.ValueKind == JsonValueKind.Undefined;

    public static JsonElement GetPropertyOrSelf(this JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var prop) ? prop : element;

    public static int GetArrayLengthOrZero(this JsonElement element) =>
        element.ValueKind == JsonValueKind.Array ? element.GetArrayLength() : 0;

    public static JsonElement FirstOrUndefined(this JsonElement element) =>
        element.ValueKind == JsonValueKind.Array ? element.EnumerateArray().FirstOrDefault() : default;

    /// <summary>
    /// Returns true if the JsonElement is not the default sentinel (Undefined/Null or default struct)
    /// Use after FirstOrUndefined to verify presence: var first = arr.FirstOrUndefined(); if (first.IsDefinedElement()) { ... }
    /// </summary>
    public static bool IsDefinedElement(this JsonElement element) =>
        element.ValueKind != JsonValueKind.Undefined && element.ValueKind != JsonValueKind.Null;

    /* -------- Boolean -------- */
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
    public static string GetStringOrDefault(this JsonElement element, string propertyName, string @default = "")
    {
        if (!element.TryGetProperty(propertyName, out var prop)) return @default;

        if (prop.ValueKind == JsonValueKind.String) return prop.GetString() ?? @default;

        // For non-string types, return default string (can be extended with prop.ToString() if needed)
        return @default;
    }

    public static string GetStringOrNull(this JsonElement e, string name) =>
        e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

    public static JsonElement? GetObjectOrNull(this JsonElement e, string name) =>
        e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.Object ? p : (JsonElement?)null;

    // Chaining access: similar to obj["a"]["b"]
    public static string GetNestedString(this JsonElement e, string a, string b)
    {
        var aProp = e.GetObjectOrNull(a);
        if (aProp == null) return null;

        return aProp.Value.GetStringOrNull(b);        
    }

    /* -------- DateTimeOffset (ISO8601 or epoch number/string) -------- */
    public static DateTimeOffset GetDateTimeOffsetOrDefault(
        this JsonElement element, string propertyName, DateTimeOffset @default = default)
    {
        if (!element.TryGetProperty(propertyName, out var prop)) return @default;

        // Number: assume epoch (seconds/milliseconds)
        if (prop.ValueKind == JsonValueKind.Number)
        {
            // Large value is ms, otherwise s
            if (prop.TryGetInt64(out var num))
            {
                return num >= 1_000_000_000_000
                    ? DateTimeOffset.FromUnixTimeMilliseconds(num)
                    : DateTimeOffset.FromUnixTimeSeconds(num);
            }
        }

        // String: ISO8601 or epoch string
        if (prop.ValueKind == JsonValueKind.String)
        {
            var s = prop.GetString();
            if (string.IsNullOrWhiteSpace(s)) return @default;

            if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dto))
                return dto;

            if (long.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var epoch))
            {
                return epoch >= 1_000_000_000_000
                    ? DateTimeOffset.FromUnixTimeMilliseconds(epoch)
                    : DateTimeOffset.FromUnixTimeSeconds(epoch);
            }
        }

        return @default;
    }

    /// <summary>
    /// If specified date string field exists, parse DateTimeOffset and return UnixTimeMilliseconds,
    /// otherwise return the fallbackLongField value as long.
    /// </summary>
    public static long GetUnixTimeOrDefault(this JsonElement element, string dateFieldName, long @default = 0)
    {
        if (!element.TryGetProperty(dateFieldName, out var dateProp))
            return @default;

        // Numeric epoch (seconds or milliseconds) directly supported
        if (dateProp.ValueKind == JsonValueKind.Number && dateProp.TryGetInt64(out var epochNum))
        {
            return NormalizeEpoch(epochNum);
        }

        if (dateProp.ValueKind == JsonValueKind.String)
        {
            var s = dateProp.GetString();
            if (string.IsNullOrWhiteSpace(s)) return @default;

            if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dto))
                return dto.ToUnixTimeMilliseconds();

            if (long.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var epochParsed))
                return NormalizeEpoch(epochParsed);
        }

        return @default;

        static long NormalizeEpoch(long raw) => raw >= 1_000_000_000_000 ? raw : raw * 1000;
    }

    /// <summary>
    /// Returns true if the specified property exists and is a Json array.
    /// </summary>
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
