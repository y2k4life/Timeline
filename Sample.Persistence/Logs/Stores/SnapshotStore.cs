using Newtonsoft.Json;
using Raven.Client.Documents;
using Raven.Client.Documents.Commands.Batches;
using System;
using System.IO;
using System.Linq;
using System.Text;
using Timeline;
using Timeline.Events;
using Timeline.Snapshots;

namespace Sample.Persistence.Logs.Stores
{
    public class SnapshotStore : ISnapshotStore
    {
        readonly IDocumentStore _documentStore;

        public string OfflineStorageFolder { get; set; }

        public SnapshotStore(IDocumentStore documentStore, string offlineStorageFolder)
        {
            _documentStore = documentStore;

            OfflineStorageFolder = offlineStorageFolder;
        }

        public Snapshot Get(Guid id)
        {
            using var session = _documentStore.OpenSession();
            return session.Query<Snapshot>().FirstOrDefault(s => s.AggregateIdentifier == id);
        }

        public void Save(Snapshot snapshot)
        {
            using var session = _documentStore.OpenSession();
            session.Store(snapshot);
            session.SaveChanges();
        }

        public void Box(Guid aggregateId)
        {
            // Create a new directory using the aggregate identifier as the folder name.
            var path = Path.Combine(OfflineStorageFolder, aggregateId.ToString());
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            // Serialize the event stream and write it to an external file.
            var aggregateState = Get(aggregateId).AggregateState;
            var json = JsonConvert.SerializeObject(aggregateState);
            var file = Path.Combine(path, "Snapshot.json");
            File.WriteAllText(file, json, Encoding.Unicode);

            // Delete the aggregate and the events from the online logs.
            Delete(aggregateId);
        }

        public string Unbox(Guid aggregateIdentifier)
        {
            // The snapshot must exist!
            var file = Path.Combine(OfflineStorageFolder, aggregateIdentifier.ToString(), "Snapshot.json");
            if (!File.Exists(file))
                throw new SnapshotNotFoundException(file);

            // Read the serialized JSON into a new snapshot and return it.
            return File.ReadAllText(file);
        }

        private void Delete(Guid aggregateId)
        {
            using var session = _documentStore.OpenSession();
            var aggregate = session.Query<Snapshot>().FirstOrDefault(s => s.AggregateIdentifier == aggregateId);
            session.Delete(aggregate);
            session.SaveChanges();
        }
    }
}
