using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using TEDF.Persistence.MongoDB.Documents;
using TEDF.Persistence.MongoDB.Repositories.Interfaces;

namespace TEDF.Persistence.MongoDB.Repositories.Implementation
{
    public class UserActivityLogRepository : IUserActivityLogRepository
    {
        private readonly IMongoCollection<UserActivityLogDocument> _collection;

        public UserActivityLogRepository(MongoDbContext context)
        {
            _collection = context.GetCollection<UserActivityLogDocument>(MongoDbContext.Collections.UserActivityLogs);
        }

        public async Task AddAsync(UserActivityLogDocument log, CancellationToken ct = default)
            => await _collection.InsertOneAsync(log, cancellationToken: ct);

        public async Task<IEnumerable<UserActivityLogDocument>> GetByUserIdAsync(Guid userId, int limit = 100, CancellationToken ct = default)
            => await _collection.Find(l => l.UserId == userId).SortByDescending(l => l.Timestamp).Limit(limit).ToListAsync(ct);

        public async Task<(IEnumerable<UserActivityLogDocument> Items, long TotalCount)> GetPagedAsync(
            string? role = null,
            string? category = null,
            string? severity = null,
            string? searchTerm = null,
            DateTime? from = null,
            DateTime? to = null,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default)
        {
            var filter = BuildFilter(role, category, severity, searchTerm, from, to);

            var totalCount = await _collection.CountDocumentsAsync(filter, cancellationToken: ct);

            var items = await _collection
                .Find(filter)
                .SortByDescending(x => x.Timestamp)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync(ct);

            return (items, totalCount);
        }

        public async Task<(IEnumerable<GroupedActivityLogResult> Items, long TotalGroups, Dictionary<string, long> RoleCounts)>
            GetGroupedAsync(
                string? role = null,
                string? severity = null,
                string? searchTerm = null,
                DateTime? from = null,
                DateTime? to = null,
                int page = 1,
                int pageSize = 20,
                CancellationToken ct = default)
        {
            // Build match filter (same logic as GetPagedAsync, minus category)
            var matchDoc = BuildMatchBsonDocument(role, severity, searchTerm, from, to);

            // ── Pipeline: grouped items with severity counts ──
            var pipeline = new[]
            {
                new BsonDocument("$match", matchDoc),
                new BsonDocument("$group", new BsonDocument
                {
                    ["_id"] = new BsonDocument
                    {
                        ["userId"] = "$UserId",
                        ["userRole"] = new BsonDocument("$ifNull", new BsonArray { "$ActiveRole", "$UserRole" }),
                        ["action"] = "$Action",
                        ["category"] = "$Category"
                    },
                    ["userName"] = new BsonDocument("$last", "$UserName"),
                    ["userEmail"] = new BsonDocument("$last", "$UserEmail"),
                    ["totalCount"] = new BsonDocument("$sum", 1),
                    ["latestTimestamp"] = new BsonDocument("$max", "$Timestamp"),
                    ["infoCount"] = new BsonDocument("$sum",
                        new BsonDocument("$cond", new BsonArray
                        {
                            new BsonDocument("$eq", new BsonArray { "$Severity", "info" }), 1, 0
                        })),
                    ["warningCount"] = new BsonDocument("$sum",
                        new BsonDocument("$cond", new BsonArray
                        {
                            new BsonDocument("$eq", new BsonArray { "$Severity", "warning" }), 1, 0
                        })),
                    ["errorCount"] = new BsonDocument("$sum",
                        new BsonDocument("$cond", new BsonArray
                        {
                            new BsonDocument("$eq", new BsonArray { "$Severity", "error" }), 1, 0
                        })),
                    ["criticalCount"] = new BsonDocument("$sum",
                        new BsonDocument("$cond", new BsonArray
                        {
                            new BsonDocument("$eq", new BsonArray { "$Severity", "critical" }), 1, 0
                        })),
                }),
                new BsonDocument("$sort", new BsonDocument("latestTimestamp", -1)),
                new BsonDocument("$facet", new BsonDocument
                {
                    ["items"] = new BsonArray
                    {
                        new BsonDocument("$skip", (page - 1) * pageSize),
                        new BsonDocument("$limit", pageSize)
                    },
                    ["total"] = new BsonArray
                    {
                        new BsonDocument("$count", "count")
                    }
                })
            };

            var cursor = await _collection.AggregateAsync<BsonDocument>(
                PipelineDefinition<UserActivityLogDocument, BsonDocument>.Create(pipeline),
                cancellationToken: ct);

            var facetResult = await cursor.FirstOrDefaultAsync(ct);

            var items = new List<GroupedActivityLogResult>();
            long totalGroups = 0;

            if (facetResult != null)
            {
                totalGroups = facetResult["total"].AsBsonArray.Count > 0
                    ? facetResult["total"].AsBsonArray[0]["count"].AsInt32
                    : 0;

                foreach (var doc in facetResult["items"].AsBsonArray)
                {
                    var id = doc["_id"].AsBsonDocument;
                    items.Add(new GroupedActivityLogResult
                    {
                        UserId = SafeParseGuid(id["userId"]),
                        ActiveRole = id.GetValue("userRole", BsonNull.Value) is BsonString s ? s.AsString : "unknown",
                        Action = id["action"].AsString,
                        Category = id["category"].IsBsonNull ? null : id["category"].AsString,
                        UserName = doc["userName"].AsString,
                        UserEmail = doc["userEmail"].IsBsonNull ? null : doc["userEmail"].AsString,
                        TotalCount = doc["totalCount"].AsInt32,
                        LatestTimestamp = doc["latestTimestamp"].ToUniversalTime(),
                        SeverityCounts = new SeverityCountsResult
                        {
                            Info = doc["infoCount"].AsInt32,
                            Warning = doc["warningCount"].AsInt32,
                            Error = doc["errorCount"].AsInt32,
                            Critical = doc["criticalCount"].AsInt32,
                        }
                    });
                }
            }

            // ── Pipeline 2: role counts (for stat cards) ──
            // Uses the same base filters BUT without severity filter so cards show total per role
            var roleMatchDoc = BuildMatchBsonDocument(role: null, severity: null, searchTerm, from, to);

            var rolePipeline = new[]
            {
                new BsonDocument("$match", roleMatchDoc),
                new BsonDocument("$group", new BsonDocument
                {
                    ["_id"] = new BsonDocument("$ifNull", new BsonArray { "$ActiveRole", "$UserRole" }),
                    ["count"] = new BsonDocument("$sum", 1)
                })
            };

            var roleCursor = await _collection.AggregateAsync<BsonDocument>(
                PipelineDefinition<UserActivityLogDocument, BsonDocument>.Create(rolePipeline),
                cancellationToken: ct);

            var roleResults = await roleCursor.ToListAsync(ct);
            var roleCounts = new Dictionary<string, long>();
            foreach (var doc in roleResults)
            {
                var roleKey = doc["_id"].IsBsonNull ? "unknown" : doc["_id"].AsString;
                roleCounts[roleKey.ToLowerInvariant()] = doc["count"].AsInt32;
            }

            return (items, totalGroups, roleCounts);
        }

        public async Task<IEnumerable<ErrorDetailResult>> GetErrorDetailsAsync(
            Guid userId,
            string action,
            DateTime? from = null,
            DateTime? to = null,
            CancellationToken ct = default)
        {
            var matchConditions = new BsonDocument
            {
                ["UserId"] = new BsonBinaryData(userId, GuidRepresentation.Standard),
                ["Action"] = action,
                ["Severity"] = new BsonDocument("$in", new BsonArray { "error", "critical" })
            };

            if (from.HasValue)
                matchConditions["Timestamp"] = new BsonDocument("$gte", new BsonDateTime(from.Value));
            if (to.HasValue)
            {
                if (matchConditions.Contains("Timestamp"))
                    matchConditions["Timestamp"].AsBsonDocument.Add("$lte", new BsonDateTime(to.Value));
                else
                    matchConditions["Timestamp"] = new BsonDocument("$lte", new BsonDateTime(to.Value));
            }

            var pipeline = new[]
            {
                new BsonDocument("$match", matchConditions),
                new BsonDocument("$group", new BsonDocument
                {
                    ["_id"] = new BsonDocument
                    {
                        ["message"] = "$Details.ErrorMessage",
                        ["type"] = "$Details.ErrorType"
                    },
                    ["count"] = new BsonDocument("$sum", 1),
                    ["latestAt"] = new BsonDocument("$max", "$Timestamp")
                }),
                new BsonDocument("$sort", new BsonDocument("count", -1))
            };

            var cursor = await _collection.AggregateAsync<BsonDocument>(
                PipelineDefinition<UserActivityLogDocument, BsonDocument>.Create(pipeline),
                cancellationToken: ct);

            var results = await cursor.ToListAsync(ct);

            return results.Select(doc =>
            {
                var id = doc["_id"].AsBsonDocument;
                return new ErrorDetailResult
                {
                    Message = id["message"].IsBsonNull ? "Unknown error" : id["message"].AsString,
                    ErrorType = id["type"].IsBsonNull ? null : id["type"].AsString,
                    Count = doc["count"].AsInt32,
                    LatestAt = doc["latestAt"].ToUniversalTime()
                };
            }).ToList();
        }

        public async Task<SeverityCountsResult> GetSeveritySummaryAsync(
            string? role = null,
            DateTime? from = null,
            DateTime? to = null,
            CancellationToken ct = default)
        {
            var matchDoc = BuildMatchBsonDocument(role, severity: null, searchTerm: null, from, to);

            var pipeline = new[]
            {
                new BsonDocument("$match", matchDoc),
                new BsonDocument("$group", new BsonDocument
                {
                    ["_id"] = "$Severity",
                    ["count"] = new BsonDocument("$sum", 1)
                })
            };

            var cursor = await _collection.AggregateAsync<BsonDocument>(
                PipelineDefinition<UserActivityLogDocument, BsonDocument>.Create(pipeline),
                cancellationToken: ct);

            var results = await cursor.ToListAsync(ct);

            var summary = new SeverityCountsResult();
            foreach (var doc in results)
            {
                var key = doc["_id"].IsBsonNull ? "info" : doc["_id"].AsString;
                var count = doc["count"].AsInt32;
                switch (key.ToLowerInvariant())
                {
                    case "info": summary.Info = count; break;
                    case "warning": summary.Warning = count; break;
                    case "error": summary.Error = count; break;
                    case "critical": summary.Critical = count; break;
                }
            }

            return summary;
        }

        // ── Shared Helpers ──────────────────────────────────────────

        private static FilterDefinition<UserActivityLogDocument> BuildFilter(
            string? role, string? category, string? severity,
            string? searchTerm, DateTime? from, DateTime? to)
        {
            var builder = Builders<UserActivityLogDocument>.Filter;
            var filters = new List<FilterDefinition<UserActivityLogDocument>>();

            if (!string.IsNullOrWhiteSpace(role))
            {
                var roleRegex = new BsonRegularExpression($"^{Regex.Escape(role)}$", "i");
                filters.Add(builder.Regex(x => x.ActiveRole, roleRegex));
            }

            if (!string.IsNullOrWhiteSpace(category))
                filters.Add(builder.Eq(x => x.Category, category));

            if (!string.IsNullOrWhiteSpace(severity))
                filters.Add(builder.Eq(x => x.Severity, severity));

            if (from.HasValue)
                filters.Add(builder.Gte(x => x.Timestamp, from.Value));

            if (to.HasValue)
                filters.Add(builder.Lte(x => x.Timestamp, to.Value));

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var regex = new BsonRegularExpression(searchTerm, "i");
                filters.Add(builder.Or(
                    builder.Regex(x => x.UserName, regex),
                    builder.Regex(x => x.Action, regex),
                    builder.Regex(x => x.EntityType, regex),
                    builder.Regex(x => x.UserEmail, regex)));
            }

            return filters.Count > 0 ? builder.And(filters) : builder.Empty;
        }

        private static Guid SafeParseGuid(BsonValue value) => value.BsonType switch
        {
            BsonType.Binary => value.AsGuid,
            BsonType.String => Guid.Parse(value.AsString),
            _ => Guid.Empty,
        };

        /// <summary>
        /// Builds a BsonDocument $match stage for use in aggregation pipelines.
        /// </summary>
        private static BsonDocument BuildMatchBsonDocument(
            string? role, string? severity, string? searchTerm,
            DateTime? from, DateTime? to)
        {
            var match = new BsonDocument();

            if (!string.IsNullOrWhiteSpace(role))
                match["ActiveRole"] = new BsonRegularExpression($"^{Regex.Escape(role)}$", "i");

            if (!string.IsNullOrWhiteSpace(severity))
                match["Severity"] = severity;

            if (from.HasValue || to.HasValue)
            {
                var timeRange = new BsonDocument();
                if (from.HasValue) timeRange["$gte"] = new BsonDateTime(from.Value);
                if (to.HasValue) timeRange["$lte"] = new BsonDateTime(to.Value);
                match["Timestamp"] = timeRange;
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var regex = new BsonRegularExpression(searchTerm, "i");
                match["$or"] = new BsonArray
                {
                    new BsonDocument("UserName", regex),
                    new BsonDocument("Action", regex),
                    new BsonDocument("EntityType", regex),
                    new BsonDocument("UserEmail", regex)
                };
            }

            return match;
        }
    }
}
