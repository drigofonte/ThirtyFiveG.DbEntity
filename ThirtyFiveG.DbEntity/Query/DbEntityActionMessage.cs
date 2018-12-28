using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ThirtyFiveG.DbEntity.Query
{
    public class DbEntityActionMessage
    {
        public DbEntityActionMessage(Type type, List<Tuple<string, object>[]> entitiesReferences, int action, int createdBy, string actionData, int duration, string source, string version, int commentId, ActionLevel actionLevel) : this(Guid.NewGuid().ToString(), type.FullName, DateTime.UtcNow, JsonConvert.SerializeObject(entitiesReferences), action, createdBy, actionData, duration, source, version, commentId, (int)actionLevel) { }

        public DbEntityActionMessage(string id, string type, DateTime timestamp, string entitiesReferences, int actionId, int createdBy, string actionData, int duration, string source, string version, int commentId, int actionLevelId)
        {
            RowKey = id;
            PartitionKey = type + "_" + entitiesReferences;
            Timestamp = timestamp;
            Type = type;
            EntitiesReferences = entitiesReferences;
            ActionID = actionId;
            CreatedByID = createdBy;
            Duration = duration;
            Source = source;
            Version = version;
            ActionData = actionData;
            CommentID = commentId;
            ActionLevelID = actionLevelId;
        }

        public DbEntityActionMessage() { }

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTime Timestamp { get; set; }
        public string Type { get; set; }
        public string EntitiesReferences { get; set; }
        public int ActionID { get; set; }
        public int CreatedByID { get; set; }
        public int Duration { get; set; }
        public string Source { get; set; }
        public string Version { get; set; }
        public string ActionData { get; set; }
        public int CommentID { get; set; }
        public int ActionLevelID { get; set; }

        public enum ActionLevel
        {
            Info        = 1,
            Error       = 2,
            Debug       = 3,
            Warning     = 4
        }
    }
}
