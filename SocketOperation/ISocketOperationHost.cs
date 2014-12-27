using System;
using System.Collections.Generic;
using uNet2.Network;

namespace uNet2.SocketOperation
{
    public interface ISocketOperationHost
    {
        T CreateOperation<T>() where T : ISocketOperation;
        void RegisterOperation(ISocketOperation operation);
        void UnregisterOperation(ISocketOperation operation);
        void UnregisterOperation(Guid operationGuid);
        void AttachOperation(ISocketOperation operation, Guid connectionGuid);
        Dictionary<Guid, ISocketOperation> ActiveSocketOperations { get; set; }
    }
}
