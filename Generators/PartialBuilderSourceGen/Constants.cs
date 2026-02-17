using Microsoft.CodeAnalysis;
using PartialBuilderSourceGen.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace PartialBuilderSourceGen
{
	internal class Constants
	{
		public const string UpdaterPostfix = "Update";
		public const string ChangesPostfix = "Changes";

		public static string FormatAsUpdater(INamedTypeSymbol namedSymbol)
		{
			return $"{namedSymbol.Name}{UpdaterPostfix}";
		}

		public static string FormatAsChanges(INamedTypeSymbol namedSymbol)
		{
			return $"{namedSymbol.Name}{ChangesPostfix}";
		}
	}
}
