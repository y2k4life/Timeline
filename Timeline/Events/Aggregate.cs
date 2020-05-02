using System;
using System.Collections.Generic;
using System.Text;

namespace Timeline.Events
{
    public class Aggregate
    {
        public Guid TenantIdentifier { get; set; }

        public Guid AggregateIdentifier { get; set; }

        public Guid AggregateVersion { get; set; }

        public DateTime? AggregateExpires { get; set; }

        public string AggregateType { get; set; }

        public string AggregateClass { get; set; }
    }
}
