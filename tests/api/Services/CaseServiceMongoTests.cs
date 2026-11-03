using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LazyCache;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;
using Scv.Api.Infrastructure.Mappings;
using Scv.Api.Services;
using Scv.Db.Contexts;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Scv.Models;
using Xunit;

namespace tests.api.Services;

// Opt in with an isolated replica set; never use the application's configured database.
public class CaseServiceMongoTests : IAsyncLifetime
{
    private readonly IMongoDatabase _database;
    private readonly string _databaseName = "jasper867_test_" + Guid.NewGuid().ToString("N");
    private readonly IMapper _mapper;

    public CaseServiceMongoTests()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("JASPER_CASE_TEST_MONGO_URI"));
        _database = client.GetDatabase(_databaseName);
        var config = new TypeAdapterConfig();
        config.Apply(new CaseMapping());
        _mapper = new Mapper(config);
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => _database.Client.DropDatabaseAsync(_databaseName);

    private JasperDbContext Context(SaveChangesInterceptor interceptor = null)
    {
        var options = new DbContextOptionsBuilder<JasperDbContext>()
            .UseMongoDB(_database.Client, _databaseName);
        if (interceptor != null) options.AddInterceptors(interceptor);
        return new JasperDbContext(options.Options);
    }

    private CaseService Service(JasperDbContext context, IAppCache cache = null, string retention = "30") => new(
        cache ?? new CachingService(), _mapper, NullLogger<CaseService>.Instance,
        new RepositoryBase<Case>(context, _database), context,
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            ["JOBS:SyncAssignedCases:RetentionDays"] = retention
        }).Build());

    private async Task Seed(params Case[] cases)
    {
        await using var context = Context();
        context.Cases.AddRange(cases);
        await context.SaveChangesAsync();
    }

    private async Task<List<Case>> Read()
    {
        await using var context = Context();
        return await context.Cases.AsNoTracking().ToListAsync();
    }

    private static Case Row(string appearance = "appearance", string date = null) => new()
    {
        JudgeId = 1,
        PhysicalFileId = "file",
        AppearanceId = appearance,
        RestrictionCode = "S",
        OverrideDueDate = date,
        DueDate = "25-Nov-2026"
    };

    private static CaseDto Import(string appearance = "appearance", string style = null) => new()
    {
        JudgeId = 1,
        PhysicalFileId = "file",
        AppearanceId = appearance,
        RestrictionCode = "S",
        StyleOfCause = style,
        DueDate = "26-Nov-2026"
    };

    [MongoCaseFact]
    public async Task Sync_RetainsIdsOverridesRestoresAndRefreshesAcrossIndependentCaches()
    {
        var original = Row(date: "2026-12-01");
        var removed = Row("removed");
        await Seed(original, removed);
        var cache = new CachingService();
        var otherInstanceCache = new CachingService();
        await using (var before = Context())
            Assert.Equal(2, (await Service(before, cache).GetAssignedCasesAsync(1)).Payload.ReservedJudgments.Count);
        await using (var beforeOther = Context())
            Assert.Equal(2, (await Service(beforeOther, otherInstanceCache).GetAssignedCasesAsync(1)).Payload.ReservedJudgments.Count);
        await using (var genericRead = Context())
            Assert.Equal(2, (await Service(genericRead, cache).GetAllAsync()).Count);
        await using (var sync = Context())
            Assert.True((await Service(sync, cache).SyncAllAssignedCasesAsync([])).Succeeded);
        Assert.NotNull(Assert.Single(await Read()).DeletedAt);
        await using (var after = Context())
            Assert.Empty((await Service(after, otherInstanceCache).GetAssignedCasesAsync(1)).Payload.ReservedJudgments);
        await using (var genericRead = Context())
            Assert.Single(await Service(genericRead, cache).GetAllAsync());
        await using (var sync = Context())
            Assert.True((await Service(sync, cache).SyncAllAssignedCasesAsync([Import(style: "Updated")])).Succeeded);
        var restored = Assert.Single(await Read());
        Assert.Equal(original.Id, restored.Id);
        Assert.Equal("2026-12-01", restored.OverrideDueDate);
        Assert.Equal("26-Nov-2026", restored.DueDate);
        Assert.Equal("Updated", restored.StyleOfCause);
        Assert.Null(restored.DeletedAt);
    }

    [MongoCaseFact]
    public async Task Sync_DuplicatesPropagateConflictsSurviveAndOtherCategoriesStaySeparate()
    {
        var first = Row(date: "2026-12-01");
        var continuation = Row();
        continuation.Reason = "CNT";
        var conflict1 = Row("conflict", "2026-12-02");
        var conflict2 = Row("conflict", "2026-12-03");
        await Seed(first, continuation, conflict1, conflict2);
        var continuationImport = Import();
        continuationImport.Reason = "CNT";
        await using var sync = Context();
        var result = await Service(sync).SyncAllAssignedCasesAsync(
            [Import(), Import(), continuationImport, Import("conflict"), Import("conflict"), Import("conflict")]);
        Assert.True(result.Succeeded);
        var rows = await Read();
        Assert.Equal(6, rows.Count);
        Assert.All(rows.Where(c => c.AppearanceId == "appearance" && c.Reason == null),
            c => Assert.Equal("2026-12-01", c.OverrideDueDate));
        Assert.Null(rows.Single(c => c.Reason == "CNT").OverrideDueDate);
        Assert.Equal("2026-12-02", rows.Single(c => c.Id == conflict1.Id).OverrideDueDate);
        Assert.Equal("2026-12-03", rows.Single(c => c.Id == conflict2.Id).OverrideDueDate);
        Assert.Null(rows.Single(c => c.AppearanceId == "conflict" && c.Id != conflict1.Id && c.Id != conflict2.Id).OverrideDueDate);
        Assert.Equal(2, result.Payload.Count);
        Assert.Equal(2, result.Payload.Single(w => w.AppearanceId == "conflict").ConflictingOverrides.Length);
    }

    [MongoCaseFact]
    public async Task Sync_UnrelatedDuplicateCategoriesSucceedWithoutOverrideWarnings()
    {
        var original = Row();
        original.Reason = "CNT";
        await Seed(original);
        var first = Import();
        first.Reason = "CNT";
        var second = Import();
        second.Reason = "CNT";
        await using var sync = Context();
        var result = await Service(sync).SyncAllAssignedCasesAsync([first, second]);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Payload);
        var rows = await Read();
        Assert.Equal(2, rows.Count);
        Assert.Contains(rows, c => c.Id == original.Id);
        Assert.All(rows, c => Assert.Null(c.OverrideDueDate));
    }

    [MongoCaseFact]
    public async Task Override_SavesEditsClearsDuplicatesAndReturnsPersistedRevisions()
    {
        var first = Row();
        var second = Row();
        var tombstone = Row(date: "2026-12-01");
        tombstone.DeletedAt = DateTime.UtcNow.AddDays(-1);
        var unrelated = Row();
        unrelated.Reason = "CNT";
        await Seed(first, second, tombstone, unrelated);
        var cache = new CachingService();
        foreach (var date in new[] { "2026-12-01", "2026-12-02", null })
        {
            var before = (await Read()).Single(c => c.Id == first.Id);
            await using var edit = Context();
            var result = await Service(edit, cache).SetDueDateOverrideAsync(first.Id, 1,
                new CaseDueDateOverrideRequest { OverrideDueDate = date, UpdatedDate = before.Upd_Dtm });
            Assert.Equal(200, result.Status);
            Assert.Equal(2, result.Result.Payload.Count);
            var persisted = await Read();
            Assert.All(persisted.Where(c => c.Reason == null), c => Assert.Equal(date, c.OverrideDueDate));
            Assert.Null(persisted.Single(c => c.Reason == "CNT").OverrideDueDate);
            Assert.Equal(persisted.Single(c => c.Id == first.Id).Upd_Dtm,
                result.Result.Payload.Single(c => c.Id == first.Id).UpdatedDate);
            Assert.All(result.Result.Payload, c => Assert.Equal(date ?? c.DueDate, c.EffectiveDueDate));
            Assert.All(result.Result.Payload, c => Assert.Equal("25-Nov-2026", c.DueDate));
        }
    }

    [MongoCaseFact]
    public async Task Override_RejectsUnauthorizedDeletedStaleIneligibleMissingAndConflictingRecords()
    {
        var active = Row();
        var deleted = Row("deleted", "2026-12-01");
        deleted.DeletedAt = DateTime.UtcNow;
        var ineligible = Row("continuation");
        ineligible.Reason = "CNT";
        var conflict = Row("conflict", "2026-12-01");
        await Seed(active, deleted, ineligible, conflict, Row("conflict", "2026-12-02"));
        foreach (var (row, judge, status) in new[]
        {
            (active, 2, 403), (deleted, 1, 409), (ineligible, 1, 409),
            (conflict, 1, 409), (Row("missing"), 1, 404)
        })
        {
            await using var edit = Context();
            var result = await Service(edit).SetDueDateOverrideAsync(row.Id, judge,
                new CaseDueDateOverrideRequest { OverrideDueDate = "2026-12-10", UpdatedDate = row.Upd_Dtm });
            Assert.Equal(status, result.Status);
        }
        await using var staleEdit = Context();
        Assert.Equal(409, (await Service(staleEdit).SetDueDateOverrideAsync(active.Id, 1,
            new CaseDueDateOverrideRequest { UpdatedDate = active.Upd_Dtm.AddMilliseconds(-1) })).Status);
    }

    [MongoCaseFact]
    public async Task Sync_FailedBulkWriteRollsBackUpdatesDeletesAndInserts()
    {
        var original = Row(date: "2026-12-01");
        var removed = Row("removed");
        await Seed(original, removed);
        await _database.RunCommandAsync<BsonDocument>(new BsonDocument
        {
            { "collMod", "cases" },
            { "validator", new BsonDocument("StyleOfCause", new BsonDocument("$ne", "reject")) }
        });
        await using var sync = Context();
        Assert.False((await Service(sync).SyncAllAssignedCasesAsync([Import(style: "changed"), Import("new", "reject")])).Succeeded);
        var rows = await Read();
        Assert.Equal(2, rows.Count);
        Assert.Contains(rows, c => c.Id == removed.Id);
        Assert.Null(rows.Single(c => c.Id == original.Id).StyleOfCause);
        Assert.All(rows, c => Assert.Null(c.DeletedAt));
    }

    [MongoCaseFact]
    public async Task Sync_ConcurrentOverrideSurvivesAndStaleSyncFailsAtomically()
    {
        var original = Row();
        await Seed(original, Row("removed"));
        var reachedSave = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var syncContext = Context(new BeforeSave(async () =>
        {
            reachedSave.SetResult();
            await resume.Task.WaitAsync(TimeSpan.FromSeconds(20));
        }));
        var sync = Service(syncContext).SyncAllAssignedCasesAsync([Import(style: "stale sync"), Import()]);
        try
        {
            await reachedSave.Task.WaitAsync(TimeSpan.FromSeconds(20));
            await using var edit = Context();
            var saved = await Service(edit).SetDueDateOverrideAsync(original.Id, 1,
                new CaseDueDateOverrideRequest { OverrideDueDate = "2026-12-01", UpdatedDate = original.Upd_Dtm });
            Assert.Equal(200, saved.Status);
        }
        finally { resume.TrySetResult(); }
        Assert.False((await sync).Succeeded);
        var rows = await Read();
        Assert.Equal(2, rows.Count);
        Assert.Equal("2026-12-01", rows.Single(c => c.Id == original.Id).OverrideDueDate);
        Assert.Null(rows.Single(c => c.Id == original.Id).StyleOfCause);
        await using var retry = Context();
        Assert.True((await Service(retry).SyncAllAssignedCasesAsync([Import(), Import()])).Succeeded);
        Assert.All(await Read(), c => Assert.Equal("2026-12-01", c.OverrideDueDate));
    }

    [MongoCaseFact]
    public async Task Override_ConcurrentSyncRejectsStaleGroupEditWithoutMissingNewDuplicate()
    {
        var original = Row();
        await Seed(original);
        var reachedSave = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var editContext = Context(new BeforeSave(async () =>
        {
            reachedSave.SetResult();
            await resume.Task.WaitAsync(TimeSpan.FromSeconds(20));
        }));
        var edit = Service(editContext).SetDueDateOverrideAsync(original.Id, 1,
            new CaseDueDateOverrideRequest { OverrideDueDate = "2026-12-01", UpdatedDate = original.Upd_Dtm });
        try
        {
            await reachedSave.Task.WaitAsync(TimeSpan.FromSeconds(20));
            await using var sync = Context();
            Assert.True((await Service(sync).SyncAllAssignedCasesAsync([Import(), Import()])).Succeeded);
        }
        finally { resume.TrySetResult(); }
        Assert.Equal(409, (await edit).Status);
        var rows = await Read();
        Assert.Equal(2, rows.Count);
        Assert.All(rows, c => Assert.Null(c.OverrideDueDate));
    }

    [MongoCaseFact]
    public async Task Override_LegacyDocumentsWithoutNewFieldsCanBeReadAndEdited()
    {
        var original = Row();
        await Seed(original);
        var collection = _database.GetCollection<Case>("cases");
        await collection.UpdateOneAsync(c => c.Id == original.Id,
            Builders<Case>.Update.Unset(c => c.OverrideDueDate).Unset(c => c.DeletedAt));
        await using var context = Context();
        var service = Service(context);
        var displayed = Assert.Single((await service.GetAssignedCasesAsync(1)).Payload.ReservedJudgments);
        var result = await service.SetDueDateOverrideAsync(displayed.Id, 1,
            new CaseDueDateOverrideRequest { OverrideDueDate = "2026-12-01", UpdatedDate = displayed.UpdatedDate });
        Assert.Equal(200, result.Status);
        Assert.Equal("2026-12-01", Assert.Single(await Read()).OverrideDueDate);
    }

    [MongoCaseFact]
    public async Task Sync_RetentionDoesNotResetAndInvalidConfigurationUsesThirtyDays()
    {
        var old = Row("old", "2026-12-01");
        old.DeletedAt = DateTime.UtcNow.AddDays(-31);
        var retained = Row("retained", "2026-12-01");
        retained.DeletedAt = DateTime.UtcNow.AddDays(-29);
        await Seed(old, retained);
        foreach (var config in new[] { "0", "invalid", "-1", "36501" })
        {
            await using var sync = Context();
            Assert.True((await Service(sync, retention: config).SyncAllAssignedCasesAsync([])).Succeeded);
            var row = Assert.Single(await Read());
            Assert.Equal(retained.Id, row.Id);
            Assert.Equal(retained.DeletedAt.Value.Ticks / TimeSpan.TicksPerMillisecond,
                row.DeletedAt.Value.Ticks / TimeSpan.TicksPerMillisecond);
        }
    }

    [MongoCaseFact]
    public async Task Sync_AbsentDuplicatesStopWarningAndRestoreOriginalOverridesAndIds()
    {
        var first = Row(date: "2026-12-01");
        var second = Row(date: "2026-12-01");
        await Seed(first, second);
        DateTime? deletedAt = null;
        for (var run = 0; run < 2; run++)
        {
            await using var sync = Context();
            var result = await Service(sync).SyncAllAssignedCasesAsync([]);
            Assert.True(result.Succeeded);
            Assert.Empty(result.Payload);
            var missing = await Read();
            Assert.Equal(2, missing.Count);
            deletedAt ??= missing[0].DeletedAt;
            Assert.All(missing, c => Assert.Equal(deletedAt, c.DeletedAt));
        }
        await using (var sync = Context())
            Assert.Empty((await Service(sync).SyncAllAssignedCasesAsync([Import()])).Payload);
        await using (var sync = Context())
            Assert.Single((await Service(sync).SyncAllAssignedCasesAsync([Import(), Import()])).Payload);
        var restored = await Read();
        Assert.Equal(new[] { first.Id, second.Id }.Order(), restored.Select(c => c.Id).Order());
        Assert.All(restored, c =>
        {
            Assert.Null(c.DeletedAt);
            Assert.Equal("2026-12-01", c.OverrideDueDate);
        });
    }

    private sealed class BeforeSave(Func<Task> action) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            await action();
            return result;
        }
    }
}

public sealed class MongoCaseFactAttribute : FactAttribute
{
    public MongoCaseFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("JASPER_CASE_TEST_MONGO_URI")))
            Skip = "Set JASPER_CASE_TEST_MONGO_URI to an isolated transaction-enabled MongoDB instance.";
    }
}
