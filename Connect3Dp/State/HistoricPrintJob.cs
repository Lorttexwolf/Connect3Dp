using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Connect3Dp.State
{
    public record HistoricPrintJob(string Name, bool IsSuccess, DateTime EndedAt, TimeSpan Elapsed, MachineFile? Thumbnail, MachineFile? File);
}
