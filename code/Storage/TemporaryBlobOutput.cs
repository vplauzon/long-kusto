using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storage
{
    public record TemporaryBlobOutput(
        IAppendStorage AppendStorage,
        Func<CancellationToken, Task> MoveToPermanentAsync);
}