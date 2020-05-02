using System;

namespace Timeline.Commands
{
    /// <summary>
    /// Provides a serialization wrapper for commands so that common properties are not embedded inside the command data.
    /// </summary>
    public class CommandSchedule
    {
        public CommandSchedule()
        {
        }

        public CommandSchedule(ICommand command)
        {
            AggregateIdentifier = command.AggregateIdentifier;
            ExpectedVersion = command.ExpectedVersion;

            CommandClass = command.GetType().AssemblyQualifiedName;
            CommandType = command.GetType().Name;
            CommandData = command;

            CommandIdentifier = command.CommandIdentifier;

            IdentityTenant = command.IdentityTenant;
            IdentityUser = command.IdentityUser;
        }

        public Guid AggregateIdentifier { get; set; }
        public int? ExpectedVersion { get; set; }

        public Guid IdentityTenant { get; set; }
        public Guid IdentityUser { get; set; }

        public string CommandClass { get; set; }
        public string CommandType { get; set; }
        public ICommand CommandData { get; set; }

        public Guid CommandIdentifier { get; set; }

        public DateTimeOffset? SendScheduled { get; set; }
        public DateTimeOffset? SendStarted { get; set; }
        public DateTimeOffset? SendCompleted { get; set; }
        public DateTimeOffset? SendCancelled { get; set; }

        public string SendStatus { get; set; }
        public string SendError { get; set; }

        public string Id { get; set; }
    }
}