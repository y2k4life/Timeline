using System;

using Timeline.Events;

namespace Sample.Domain
{
    public class TransferAggregate : AggregateRoot<Transfer>
    {
        public TransferAggregate()
        {
            State = new Transfer();
        }
        
        //public override AggregateState CreateState() => _state;

        public void StartTransfer(Guid fromAccount, Guid toAccount, decimal amount)
        {
            var transferStartedEvent = new TransferStarted(fromAccount, toAccount, amount);
            Apply(transferStartedEvent, State.When);
        }

        public void UpdateTransfer(string activity)
        {
            var e = new TransferUpdated(activity);
            Apply(e, State.When);
        }

        public void CompleteTransfer()
        {
            var e = new TransferCompleted();
            Apply(e, State.When);
        }
    }
}
