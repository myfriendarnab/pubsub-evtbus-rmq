using System;

namespace Maersk.Integrations.Events.Entities
{
    public class Data
    {
        public DataObject MessageType { get; private set; }

        public TransactionType TransactionType { get; private set; }

        public Guid Id { get; private set; }

        public Guid SnapshotId { get; private set; }

        public Data(DataObject messageType, TransactionType transactionType = default, Guid id = default,
            Guid snapshotId = default)
        {
            MessageType = messageType;
            TransactionType = transactionType;
            Id = id;
            SnapshotId = snapshotId;
        }
    }
}