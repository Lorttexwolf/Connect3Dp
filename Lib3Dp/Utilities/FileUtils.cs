using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib3Dp.Utilities
{
	internal class FileUtils
	{
		public static FileStream CreateTempFileStream()
		{
			return new FileStream(
				Path.GetTempFileName(),
				FileMode.Create,
				FileAccess.ReadWrite,
				FileShare.None,
				bufferSize: 81920,
				FileOptions.DeleteOnClose | FileOptions.SequentialScan);
		}
	}
}
