using System;

namespace ThirtyFiveG.DbEntity.Logging
{
    public class DbEntityActionCommandLogEntry : DbEntityLogEntry
    {
        public DbEntityActionCommandLogEntry(string guid, Type commandParent, string commandName, int userId, TimeSpan executionTime, Tuple<string, object>[] entityPrimaryKeys, Type entityType) : this(guid, commandParent, commandName, userId, executionTime.TotalMilliseconds, entityPrimaryKeys, entityType) { }

        public DbEntityActionCommandLogEntry(string guid, Type commandParent, string commandName, int userId, Tuple<string, object>[] entityPrimaryKeys, Type entityType) : this(guid, commandParent, commandName, userId, 0, entityPrimaryKeys, entityType) { }

        private DbEntityActionCommandLogEntry(string guid, Type commandParent, string commandName, int userId, double executionTime, Tuple<string, object>[] entityPrimaryKeys, Type entityType) : base(userId, entityPrimaryKeys, entityType)
        {
            CommandExecutionGuid = guid;
            CommandParent = commandParent.FullName;
            CommandName = commandName;
            ExecutionTime = executionTime;
        }

        public string CommandExecutionGuid { get; private set; }
        public string CommandParent { get; private set; }
        public string CommandName { get; private set; }
        public double ExecutionTime { get; private set; }
    }
}
