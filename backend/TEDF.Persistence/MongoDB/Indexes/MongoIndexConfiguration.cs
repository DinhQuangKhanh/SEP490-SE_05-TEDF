using MongoDB.Driver;
using TEDF.Persistence.MongoDB.Documents;

namespace TEDF.Persistence.MongoDB.Indexes;

public static class MongoIndexConfiguration
{
    public static async Task CreateIndexesAsync(MongoDbContext context)
    {
        await CreateActivityLogIndexesAsync(context);
        await CreateErrorLogIndexesAsync(context);
        await CreateNotificationIndexesAsync(context);
        await CreateConversationIndexesAsync(context);
        await CreateMessageIndexesAsync(context);
    }

    private static async Task CreateActivityLogIndexesAsync(MongoDbContext context)
    {
        var collection = context.GetCollection<ActivityLogDocument>(MongoDbContext.Collections.ActivityLogs);
        var idx = Builders<ActivityLogDocument>.IndexKeys;

        await collection.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<ActivityLogDocument>(
                idx.Descending(x => x.Timestamp),
                new CreateIndexOptions { ExpireAfter = TimeSpan.FromDays(365) }),
            new CreateIndexModel<ActivityLogDocument>(
                idx.Combine(idx.Ascending(x => x.Role), idx.Descending(x => x.Timestamp))),
            new CreateIndexModel<ActivityLogDocument>(idx.Ascending(x => x.UserId)),
            new CreateIndexModel<ActivityLogDocument>(idx.Ascending(x => x.FeatureCategory)),
            new CreateIndexModel<ActivityLogDocument>(idx.Ascending(x => x.Status)),
            new CreateIndexModel<ActivityLogDocument>(idx.Ascending(x => x.ActionCode)),
            new CreateIndexModel<ActivityLogDocument>(idx.Ascending(x => x.CorrelationId)),
        ]);
    }

    private static async Task CreateErrorLogIndexesAsync(MongoDbContext context)
    {
        var collection = context.GetCollection<ErrorLogDocument>(MongoDbContext.Collections.ErrorLogs);
        var idx = Builders<ErrorLogDocument>.IndexKeys;

        await collection.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<ErrorLogDocument>(
                idx.Descending(x => x.Timestamp),
                new CreateIndexOptions { ExpireAfter = TimeSpan.FromDays(365) }),
            new CreateIndexModel<ErrorLogDocument>(
                idx.Combine(idx.Ascending(x => x.Severity), idx.Descending(x => x.Timestamp))),
            new CreateIndexModel<ErrorLogDocument>(
                idx.Combine(idx.Ascending(x => x.ActionCode), idx.Descending(x => x.Timestamp))),
            new CreateIndexModel<ErrorLogDocument>(idx.Ascending(x => x.CorrelationId)),
        ]);
    }

    private static async Task CreateNotificationIndexesAsync(MongoDbContext context)
    {
        var collection = context.GetCollection<NotificationDocument>(MongoDbContext.Collections.Notifications);
        var idx = Builders<NotificationDocument>.IndexKeys;

        await collection.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<NotificationDocument>(idx.Ascending(x => x.UserId)),
            new CreateIndexModel<NotificationDocument>(
                idx.Combine(idx.Ascending(x => x.UserId), idx.Ascending(x => x.IsRead))),
            new CreateIndexModel<NotificationDocument>(
                idx.Combine(idx.Ascending(x => x.UserId), idx.Descending(x => x.CreatedAt))),
            new CreateIndexModel<NotificationDocument>(
                idx.Descending(x => x.CreatedAt),
                new CreateIndexOptions { ExpireAfter = TimeSpan.FromDays(90) }),
        ]);
    }

    private static async Task CreateConversationIndexesAsync(MongoDbContext context)
    {
        var collection = context.GetCollection<ConversationDocument>(MongoDbContext.Collections.Conversations);
        var idx = Builders<ConversationDocument>.IndexKeys;

        await collection.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<ConversationDocument>(idx.Ascending(x => x.ParticipantIds)),
            new CreateIndexModel<ConversationDocument>(idx.Ascending(x => x.GroupId)),
            new CreateIndexModel<ConversationDocument>(idx.Descending(x => x.LastMessageAt)),
        ]);
    }

    private static async Task CreateMessageIndexesAsync(MongoDbContext context)
    {
        var collection = context.GetCollection<MessageDocument>(MongoDbContext.Collections.Messages);
        var idx = Builders<MessageDocument>.IndexKeys;

        await collection.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<MessageDocument>(idx.Ascending(x => x.ConversationId)),
            new CreateIndexModel<MessageDocument>(
                idx.Combine(idx.Ascending(x => x.ConversationId), idx.Descending(x => x.CreatedAt))),
            new CreateIndexModel<MessageDocument>(idx.Ascending(x => x.SenderId)),
        ]);
    }
}
