using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace uNet2.SocketOperation
{
    internal class SocketOperationContext
    {
        public Guid OperationGuid { get; set; }

        public SocketOperationContext(Guid operationGuid)
        {
            OperationGuid = operationGuid;
        }
    }
}
