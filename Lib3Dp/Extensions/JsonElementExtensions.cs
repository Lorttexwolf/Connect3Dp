using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Lib3Dp.Extensions
{
	public static class JsonElementExtensions
	{
		public static bool TryGetPropertyChain(this JsonElement element, out JsonElement result, params string[] propertyPath)
		{
			result = element;

			foreach (var property in propertyPath)
			{
				if (!result.TryGetProperty(property, out result))
				{
					result = default;
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Tries to get an int value from a property chain
		/// </summary>
		public static bool TryGetInt32(this JsonElement element, out int value, params string[] propertyPath)
		{
			value = default;

			if (!element.TryGetPropertyChain(out var result, propertyPath))
				return false;

			if (result.ValueKind != JsonValueKind.Number)
				return false;

			return result.TryGetInt32(out value);
		}

		/// <summary>
		/// Tries to get a string value from a property chain
		/// </summary>
		public static bool TryGetString(this JsonElement element, [NotNullWhen(true)] out string? value, params string[] propertyPath)
		{
			value = default;

			if (!element.TryGetPropertyChain(out var result, propertyPath))
				return false;

			value = result.GetString()!.Trim();

			if (value.Length == 0) return false;

			return value != null;
		}

		/// <summary>
		/// Tries to get a bool value from a property chain
		/// </summary>
		public static bool TryGetBoolean(this JsonElement element, out bool value, params string[] propertyPath)
		{
			value = default;

			if (!element.TryGetPropertyChain(out var result, propertyPath))
				return false;

			if (result.ValueKind != JsonValueKind.True && result.ValueKind != JsonValueKind.False)
				return false;

			value = result.GetBoolean();
			return true;
		}

		/// <summary>
		/// Tries to get a double value from a property chain
		/// </summary>
		public static bool TryGetDouble(this JsonElement element, out double value, params string[] propertyPath)
		{
			value = default;

			if (!element.TryGetPropertyChain(out var result, propertyPath))
				return false;

			if (result.ValueKind != JsonValueKind.Number)
				return false;

			return result.TryGetDouble(out value);
		}

        // ── Lenient single-property helpers ──────────────────────
        // These handle firmware quirks where numeric values may arrive as JSON strings
        // (e.g. "123" instead of 123). Used by connectors whose protocol is loosely typed.

        /// <summary>
        /// Gets a non-empty string from a named property.
        /// </summary>
        public static bool TryGetStringValue(this JsonElement obj, string name, [NotNullWhen(true)] out string? value)
        {
            if (!obj.TryGetProperty(name, out var el))
            {
                value = null;
                return false;
            }

            value = el.GetString();
            return !string.IsNullOrEmpty(value);
        }

        /// <summary>
        /// Gets an int from a named property, coercing from a JSON string if necessary.
        /// </summary>
        public static bool TryGetInt32Lenient(this JsonElement obj, string name, out int value)
        {
            if (!obj.TryGetProperty(name, out var el))
            {
                value = 0;
                return false;
            }

            if (el.ValueKind == JsonValueKind.String && int.TryParse(el.GetString(), out var parsed))
            {
                value = parsed;
                return true;
            }

            return el.TryGetInt32(out value);
        }

        /// <summary>
        /// Gets a long from a named property, coercing from a JSON string if necessary.
        /// </summary>
        public static bool TryGetInt64Lenient(this JsonElement obj, string name, out long value)
        {
            if (!obj.TryGetProperty(name, out var el))
            {
                value = 0;
                return false;
            }

            if (el.ValueKind == JsonValueKind.String && long.TryParse(el.GetString(), out var parsed))
            {
                value = parsed;
                return true;
            }

            return el.TryGetInt64(out value);
        }

        /// <summary>
        /// Gets a double from a named property, coercing from a JSON string if necessary.
        /// </summary>
        public static bool TryGetDoubleLenient(this JsonElement obj, string name, out double value)
        {
            if (!obj.TryGetProperty(name, out var el))
            {
                value = 0;
                return false;
            }

            if (el.ValueKind == JsonValueKind.String && double.TryParse(el.GetString(), System.Globalization.NumberStyles.Float, null, out var parsed))
            {
                value = parsed;
                return true;
            }

            return el.TryGetDouble(out value);
        }

        /// <summary>
        /// Gets a <see cref="TimeSpan"/> from a named property. Handles integer seconds, double seconds,
        /// string-encoded seconds, and <see cref="TimeSpan.TryParse"/> formats.
        /// </summary>
        public static bool TryGetDurationLenient(this JsonElement obj, string name, out TimeSpan value)
        {
            value = TimeSpan.Zero;
            if (!obj.TryGetProperty(name, out var el))
                return false;

            if (el.ValueKind == JsonValueKind.String)
            {
                var s = el.GetString();
                if (!string.IsNullOrEmpty(s) && TimeSpan.TryParse(s, out var ts))
                {
                    value = ts;
                    return true;
                }

                if (int.TryParse(s, out var sec))
                {
                    value = TimeSpan.FromSeconds(sec);
                    return true;
                }

                return false;
            }

            if (el.TryGetInt32(out var i))
            {
                value = TimeSpan.FromSeconds(i);
                return true;
            }

            if (el.TryGetDouble(out var d))
            {
                value = TimeSpan.FromSeconds(d);
                return true;
            }

            return false;
        }
    }
}