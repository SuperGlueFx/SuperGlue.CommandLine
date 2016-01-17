using System;
using System.Collections.Generic;

namespace SuperGlue
{
    internal static class EnvironmentCommandExtensions
    {
        public static class CommandConstants
        {
            public const string CurrentCommand = "superglue.CurrentCommand";
        }

        public static Command GetCurrentCommand(this IDictionary<string, object> environment)
        {
            return environment.Get<Command>(CommandConstants.CurrentCommand);
        }

        public static IDisposable OpenCommandContext(this IDictionary<string, object> environment, object command, Guid id)
        {
            return new CommandDisposable(environment, command, id);
        }

        public class Command
        {
            public Command(object commandObject, Guid commandId, string causedBy)
            {
                CommandObject = commandObject;
                CommandId = commandId;
                CausedBy = causedBy;
            }

            public object CommandObject { get; }
            public Guid CommandId { get; }
            public string CausedBy { get; }
        }

        private class CommandDisposable : IDisposable
        {
            private readonly IDictionary<string, object> _environment;
            private readonly Command _previousCommand;

            public CommandDisposable(IDictionary<string, object> environment, object newCommand, Guid newCommandId)
            {
                _environment = environment;

                _previousCommand = environment.GetCurrentCommand();
                _environment[CommandConstants.CurrentCommand] = new Command(newCommand, newCommandId, environment.GetCausationId());
            }

            public void Dispose()
            {
                _environment[CommandConstants.CurrentCommand] = _previousCommand;
            }
        }
    }
}