using Newtonsoft.Json;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq.Indexing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Timeline;
using Timeline.Events;
using Timeline.Identities;
using Timeline.Utilities;

namespace Sample.Persistence.Logs.Stores
{
    public class EventStore : IEventStore
    {
        IIdentityService _identityService;
        readonly IDocumentStore _documentStore;

        public ISerializer Serializer { get; set; }

        public string OfflineStorageFolder { get; set; }

        public EventStore(IDocumentStore documentStore, IIdentityService identityService, ISerializer serializer, string offlineStorageFolder)
        {
            _documentStore = documentStore;
            _identityService = identityService;
            Serializer = serializer;
            OfflineStorageFolder = offlineStorageFolder;
        }

        public void Box(Guid aggregateId)
        {
            GetClassAndTenant(aggregateId, out string aggregateClass, out Guid aggregateTenant);

            // Create a new directory using the aggregate identifier as the folder name.
            var path = Path.Combine(OfflineStorageFolder, aggregateId.ToString());
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            // Serialize the event stream and write it to an external file.
            var events = GetSerialzied(aggregateId, -1);
            var json = JsonConvert.SerializeObject(events);
            var file = Path.Combine(path, "Events.json");
            File.WriteAllText(file, json, Encoding.Unicode);
            var info = new FileInfo(file);

            // Delete the aggregate and the events from the online logs.
            var deleted = Delete(aggregateId);

            // Create a metadata file to describe the boxed aggregated.
            var meta = new StringBuilder();
            meta.AppendLine($"Aggregate Identifier : {aggregateId}");
            meta.AppendLine($"     Aggregate Class : {aggregateClass}");
            meta.AppendLine($"    Aggregate Tenant : {aggregateTenant}");
            meta.AppendLine($"  Serialized Events  : {events.Count():n0}");
            meta.AppendLine($"Deleted Log Entries  : {deleted:n0}");
            meta.AppendLine($"    Date/Time Boxed  : {DateTime.Now:dddd, MMMM d, yyyy HH:mm} Local Time");
            meta.AppendLine($"                     : {DateTimeOffset.UtcNow:dddd, MMMM d, yyyy HH:mm} UTC");
            file = Path.Combine(path, "Metadata.txt");
            File.WriteAllText(file, meta.ToString());

            // Write an index entry for the boxed aggregate.
            var index = Path.Combine(OfflineStorageFolder, "Boxes.csv");
            File.AppendAllText(index, $"{DateTime.Now:yyyy/MM/dd-HH:mm},{aggregateId},{aggregateClass},{info.Length / 1024} KB,{aggregateTenant}\n");
        }

        public bool Exists(Guid aggregateIdentifier)
        {
            using var session = _documentStore.OpenSession();
            return session.Query<Aggregate>().Any(a => a.AggregateIdentifier == aggregateIdentifier);
        }

        public bool Exists(Guid aggregateIdentifier, int version)
        {
            //using var session = _documentStore.OpenSession();
            //return session.Query<Aggregate>().Any(a => a.AggregateIdentifier == aggregateIdentifier && a.AggregateVersion == version);
            return false;
        }

        public IEnumerable<IEvent> Get(Guid aggregate, int fromVersion)
        {
            using var session = _documentStore.OpenSession();
            return session.Query<Event>()
                .Where(e => e.AggregateIdentifier == aggregate && e.AggregateVersion > fromVersion)
                .ToList()
                .AsEnumerable();
        }

        public IEnumerable<Guid> GetExpired(DateTimeOffset at)
        {
            using var sessin = _documentStore.OpenSession();
            return sessin.Query<Aggregate>()
                .Where(x => x.AggregateExpires != null && x.AggregateExpires <= at)
                .Select(x => x.AggregateIdentifier)
                .ToList();
        }

        public void Save(IAggregateRoot aggregate, IEnumerable<IEvent> events)
        {
            var current = _identityService.GetCurrent();
            var tenant = current.Tenant.Identifier;
            var user = current.User.Identifier;

            var session = _documentStore.OpenSession();
            foreach (var @event in events)
            {
                EnsureAggregateExists(tenant, aggregate.AggregateIdentifier, aggregate.GetType().Name.Replace("Aggregate", string.Empty), aggregate.GetType().FullName);

                @event.IdentityTenant = tenant;
                @event.IdentityUser = user;

                session.Store(@event);
            }

            session.SaveChanges();
        }

        private void EnsureAggregateExists(Guid tenant, Guid aggregateId, string name, string type)
        {
            using var session = _documentStore.OpenSession();
            if (!session.Query<Aggregate>().Any(a => a.AggregateIdentifier == aggregateId))
            {
                var aggregate = new Aggregate()
                {
                    AggregateIdentifier = aggregateId,
                    AggregateClass = type,
                    AggregateType = name,
                    TenantIdentifier = tenant
                };

                session.Store(aggregate);
                session.SaveChanges();
            }
        }

        private IEnumerable<SerializedEvent> GetSerialzied(Guid aggregateIdentifier, int fromVersion)
        {
            var events = Get(aggregateIdentifier, fromVersion);

            var list = new List<SerializedEvent>();
            foreach (var @event in events)
            {
                var item = @event.Serialize(Serializer, aggregateIdentifier, @event.AggregateVersion, @event.IdentityTenant, @event.IdentityUser);
                list.Add(item);
            }

            return list;
        }

        private int Delete(Guid aggregateIdentifier)
        {
            using var session = _documentStore.OpenSession();

            var aggregate = session.Query<Aggregate>().FirstOrDefault(e => e.AggregateIdentifier == aggregateIdentifier);
            session.Delete(aggregate);

            var events = session.Query<Event>().Where(e => e.AggregateIdentifier == aggregateIdentifier);
            foreach (var @event in events)
            {
                session.Delete(@event);
            }

            session.SaveChanges();

            return events.Count() + 1;
        }

        private void GetClassAndTenant(Guid aggregateIdentifier, out string @class, out Guid tenant)
        {
            using var session = _documentStore.OpenSession();
            var aggregate = session.Query<Aggregate>().FirstOrDefault(a => a.AggregateIdentifier == aggregateIdentifier);
            tenant = aggregate.TenantIdentifier;
            @class = aggregate.AggregateClass;
        }
    }
}
