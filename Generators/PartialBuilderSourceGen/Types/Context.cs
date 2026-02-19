using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace PartialBuilderSourceGen.Types
{
	internal class Context(INamedTypeSymbol genAttribSymbol, INamedTypeSymbol dictAttribSymbol)
	{
		public INamedTypeSymbol DictKeyAttribSymbol { get; } = dictAttribSymbol;
		public INamedTypeSymbol GenAttribSymbol { get; } = genAttribSymbol;
	}
}
