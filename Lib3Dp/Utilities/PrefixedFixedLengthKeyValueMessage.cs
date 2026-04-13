using System.Diagnostics.CodeAnalysis;

namespace Lib3Dp.Utilities
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	public class PrefixedFixedLengthKeyValueMessage(string prefix)
	{
		public string Prefix { get; } = prefix;

		public Dictionary<string, string> Values { get; } = new(StringComparer.OrdinalIgnoreCase);

		public void Add(string key, string value)
		{
			Values[key] = value ?? string.Empty;
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.Append(Prefix);

			foreach (var pair in Values)
			{
				sb.Append(EncodeKeyValue(pair.Key, pair.Value));
			}
			return sb.ToString();
		}

		private static string EncodeKeyValue(string key, string value) => $"[{key}={value.Length}:{value}]";

		public static bool TryParse(string input, string expectedPrefix, [NotNullWhen(true)] out PrefixedFixedLengthKeyValueMessage? message)
		{
			message = null;

			if (string.IsNullOrEmpty(input) || !input.StartsWith(expectedPrefix)) return false;

			var result = new PrefixedFixedLengthKeyValueMessage(expectedPrefix);
			int index = expectedPrefix.Length;

			while (index < input.Length)
			{
				if (input[index] != '[') return false;

				int close = input.IndexOf(']', index);
				if (close == -1) return false;

				string block = input.Substring(index + 1, close - index - 1);

				int eq = block.IndexOf('=');
				int colon = block.IndexOf(':');

				if (eq <= 0 || colon <= eq) return false;

				string key = block.Substring(0, eq);

				if (!int.TryParse(block.AsSpan(eq + 1, colon - eq - 1), out int length)) return false;

				string value = block.Substring(colon + 1);

				if (value.Length != length) return false;

				result.Values[key] = value;

				index = close + 1;
			}

			message = result;
			return true;
		}
	}
}
