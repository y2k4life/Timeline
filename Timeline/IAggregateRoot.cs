using System;
using System.Collections.Generic;
using Timeline.Events;

namespace Timeline
{
    public interface IAggregateRoot
    {
        public Guid AggregateIdentifier { get; set; }

        public int AggregateVersion { get; set; }

        public AggregateState State { get; set;  }

        public IEvent[] FlushUncommittedChanges();

        public IEvent[] GetUncommittedChanges();

        public void Rehydrate(IEnumerable<IEvent> history);
    }
}
