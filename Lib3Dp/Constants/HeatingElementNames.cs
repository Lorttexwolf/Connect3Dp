using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib3Dp.Constants
{
	public static class HeatingElementNames
	{
		public const string Bed = "Bed";
		public const string Nozzle = "Nozzle";
		public const string Chamber = "Chamber";

		public static string Nozzles(int n)
		{
			return $"{Nozzle}-{n}";
		}
	}
}
