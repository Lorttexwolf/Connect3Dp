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
	}
}
