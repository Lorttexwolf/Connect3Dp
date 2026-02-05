using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib3Dp.Connectors.BambuLab
{
	internal static class BitUtils
	{
		public static long GetBitsFromNumb(long value, int start, int count)
		{
			long mask = (1 << count) - 1;
			long flag = (value >> start) & mask;

			return flag;

		}
	}
}
