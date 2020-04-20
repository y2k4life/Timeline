using Raven.Client.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using Timeline.Commands;
using Timeline.Utilities;

namespace Sample.Persistence.Logs.Stores
{
    public class CommandStore : ICommandStore
    {
        readonly IDocumentStore _documentStore;

        public CommandStore(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        public ISerializer Serializer => throw new NotImplementedException();

        public bool Exists(Guid commandIdentifier)
        {
            using var session = _documentStore.OpenSession();
            return session.Query<Command>().Any(c => c.CommandIdentifier == commandIdentifier);
        }

        public CommandSchedule Get(Guid commandIdentifier)
        {
            using var session = _documentStore.OpenSession();
            var commandSchedule = session.Query<CommandSchedule>().
                FirstOrDefault(c => c.CommandIdentifier == commandIdentifier);

            return commandSchedule ?? throw new CommandNotFoundException($"Command not found: {commandIdentifier}");
        }

        public IEnumerable<CommandSchedule> GetExpired(DateTimeOffset at)
        {
            using var session = _documentStore.OpenSession();
            return session.Query<CommandSchedule>()
                .Where(s => s.SendScheduled <= at && s.SendStatus == "Scheduled")
                .ToArray();
        }

        public void Save(CommandSchedule command, bool isNew)
        {
            using var session = _documentStore.OpenSession();

            session.Store(command);
            session.SaveChanges();
        }
    }
}
