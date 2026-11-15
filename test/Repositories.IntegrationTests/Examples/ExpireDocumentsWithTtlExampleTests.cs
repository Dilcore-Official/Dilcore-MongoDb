using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Policies;
using Dilcore.MongoDB.Abstractions.Provisioning;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.Extensions;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Repositories.IntegrationTests.Examples;

/// <summary>Proves docs/guides/expire-documents-with-ttl.md.</summary>
public class ExpireDocumentsWithTtlExampleTests : ExampleIntegrationTests
{
    public sealed class UserSession : IDocumentEntity<Guid>
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = "";
        public DateTime? ExpiresAt { get; set; }
    }

    public sealed class AuditEntry : IDocumentEntity<Guid>, IAuditableDocument
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Action { get; set; } = "";
    }

    public static class Provisioning
    {
        public static async Task ApplyAsync(IServiceProvider services, CancellationToken cancellationToken = default)
        {
            using var scope = services.CreateScope();
            var provisioner = scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();

            var report = await provisioner.ApplyAsync(cancellationToken);
            if (report.IsFailed)
            {
                throw new InvalidOperationException("MongoDB provisioning failed.");
            }
        }
    }

    public sealed class SessionService(IGenericRepository<UserSession> sessions)
    {
        public Task<Result<UserSession>> CreateAsync(
            string userName,
            DateTime? expiresAt,
            CancellationToken cancellationToken = default)
        {
            var session = new UserSession { UserName = userName, ExpiresAt = expiresAt };

            return sessions.StoreAsync(session, cancellationToken);
        }

        public Task<Result<UserSession>> FindAsync(Guid id, CancellationToken cancellationToken = default)
            => sessions.GetAsync(id, cancellationToken);

        public Task<Result<UserSession>> ExtendAsync(
            Guid id,
            DateTime expiresAt,
            CancellationToken cancellationToken = default)
        {
            var filter = Builders<UserSession>.Filter.Eq(x => x.Id, id);
            var update = Builders<UserSession>.Update.Set(x => x.ExpiresAt, expiresAt);

            return sessions.PatchAsync(filter, update, cancellationToken);
        }

        public Task<Result<UserSession>> PinAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var filter = Builders<UserSession>.Filter.Eq(x => x.Id, id);
            var update = Builders<UserSession>.Update.Unset(x => x.ExpiresAt);

            return sessions.PatchAsync(filter, update, cancellationToken);
        }

        public Task<Result<IReadOnlyList<UserSession>>> ListActiveAsync(
            DateTime utcNow,
            CancellationToken cancellationToken = default)
            => sessions.GetListAsync(x => x.ExpiresAt == null || x.ExpiresAt > utcNow, cancellationToken);
    }

    [OneTimeSetUp]
    public async Task SpeedUpTtlMonitorAsync()
    {
        await SetTtlMonitorSleepSecondsAsync(1);
    }

    [OneTimeTearDown]
    public async Task RestoreTtlMonitorAsync()
    {
        await SetTtlMonitorSleepSecondsAsync(60);
    }

    [Test]
    public async Task ApplyAsync_CreatesZeroOffsetAndFixedWindowTtlIndexes()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var audits = scope.ServiceProvider.GetRequiredService<IGenericRepository<AuditEntry>>();
        var cancellationToken = CancellationToken.None;

        // Act
        await Provisioning.ApplyAsync(provider, cancellationToken);
        var audit = await audits.StoreAsync(new AuditEntry { Action = "login" }, cancellationToken);
        var sessionIndexes = await ServerIndexesAsync("sessions");
        var auditIndexes = await ServerIndexesAsync("audit_entries");

        // Assert
        var sessionsTtl = TtlIndex(sessionIndexes, "sessions_ttl");
        sessionsTtl["expireAfterSeconds"].ToInt64().ShouldBe(0);
        sessionsTtl["key"]["expiresAt"].ToInt32().ShouldBe(1);
        var auditTtl = TtlIndex(auditIndexes, "audit_entries_ttl");
        auditTtl["expireAfterSeconds"].ToInt64().ShouldBe(2_592_000);
        auditTtl["key"]["createdAt"].ToInt32().ShouldBe(1);
        audit.ShouldBeSuccess();
        audit.Value.CreatedAt.Kind.ShouldBe(DateTimeKind.Utc);
        audit.Value.CreatedAt.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-1));
    }

    [Test]
    public async Task ExpiredSession_IsRemovedByServer_WhileFutureAndPinnedSessionsRemain()
    {
        // Arrange
        await using var provider = BuildProvider();
        await Provisioning.ApplyAsync(provider, CancellationToken.None);
        using var scope = provider.CreateScope();
        var sessions = scope.ServiceProvider.GetRequiredService<IGenericRepository<UserSession>>();
        var service = ActivatorUtilities.CreateInstance<SessionService>(scope.ServiceProvider);
        var cancellationToken = CancellationToken.None;
        var expiredAt = DateTime.UtcNow.AddSeconds(-5);
        var futureAt = DateTime.UtcNow.AddHours(1);
        var expired = await service.CreateAsync("expired", expiredAt, cancellationToken);
        var future = await service.CreateAsync("future", futureAt, cancellationToken);
        var pinned = await service.CreateAsync("pinned", null, cancellationToken);

        // Act
        var gone = await WaitUntilExpiredAsync(sessions, expired.Value.Id, cancellationToken);
        var extendExpired = await service.ExtendAsync(expired.Value.Id, futureAt, cancellationToken);
        var futureStillThere = await service.FindAsync(future.Value.Id, cancellationToken);
        var pinnedStillThere = await service.FindAsync(pinned.Value.Id, cancellationToken);

        // Assert
        gone.HasError<DocumentNotFoundError>().ShouldBeTrue();
        extendExpired.HasError<DocumentNotFoundError>().ShouldBeTrue();
        futureStillThere.ShouldBeSuccess();
        futureStillThere.Value.UserName.ShouldBe("future");
        pinnedStillThere.ShouldBeSuccess();
        pinnedStillThere.Value.ExpiresAt.ShouldBeNull();
    }

    [Test]
    public async Task ExtendAndPinSession_UpdateExpirationLifecycle()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var service = ActivatorUtilities.CreateInstance<SessionService>(scope.ServiceProvider);
        var cancellationToken = CancellationToken.None;
        var originalDeadline = DateTime.UtcNow.AddMinutes(10);
        var created = await service.CreateAsync("slide", originalDeadline, cancellationToken);
        var extendedDeadline = DateTime.UtcNow.AddHours(2);

        // Act
        var extended = await service.ExtendAsync(created.Value.Id, extendedDeadline, cancellationToken);
        var pinned = await service.PinAsync(created.Value.Id, cancellationToken);

        // Assert
        extended.ShouldBeSuccess();
        extended.Value.ExpiresAt.ShouldNotBeNull();
        extended.Value.ExpiresAt!.Value.ShouldBeGreaterThan(originalDeadline);
        pinned.ShouldBeSuccess();
        pinned.Value.ExpiresAt.ShouldBeNull();
    }

    [Test]
    public async Task ChangingTtlDuration_ReportsWouldRecreateAndUpdatesExpireAfterSeconds()
    {
        // Arrange
        await using var first = BuildProvider();
        await Provisioning.ApplyAsync(first, CancellationToken.None);
        using var firstScope = first.CreateScope();
        var firstService = ActivatorUtilities.CreateInstance<SessionService>(firstScope.ServiceProvider);
        var cancellationToken = CancellationToken.None;
        var deadline = DateTime.UtcNow.AddHours(1);
        var created = await firstService.CreateAsync("keep", deadline, cancellationToken);

        // Act
        await using var second = BuildProvider(TimeSpan.FromMinutes(30));
        using var secondScope = second.CreateScope();
        var provisioner = secondScope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();
        var preview = await provisioner.DryRunAsync(cancellationToken);
        var applied = await provisioner.ApplyAsync(cancellationToken);
        var indexes = await ServerIndexesAsync("sessions");
        var secondService = ActivatorUtilities.CreateInstance<SessionService>(secondScope.ServiceProvider);
        var stillThere = await secondService.FindAsync(created.Value.Id, cancellationToken);

        // Assert
        preview.ShouldBeSuccess();
        preview.Value.Steps.ShouldContain(step =>
            step.Name == "sessions:index:sessions_ttl" && step.Action == "would-recreate");
        applied.ShouldBeSuccess();
        applied.Value.Steps.ShouldContain(step =>
            step.Name == "sessions:index:sessions_ttl" && step.Action == "recreate");
        TtlIndex(indexes, "sessions_ttl")["expireAfterSeconds"].ToInt64().ShouldBe(1_800);
        stillThere.ShouldBeSuccess();
        stillThere.Value.UserName.ShouldBe("keep");
    }

    [Test]
    public async Task ReadTimeFilter_HidesPastDueSessionBeforeMonitorOrProvisioningRuns()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var service = ActivatorUtilities.CreateInstance<SessionService>(scope.ServiceProvider);
        var cancellationToken = CancellationToken.None;
        var utcNow = DateTime.UtcNow;
        var past = await service.CreateAsync("past", utcNow.AddMinutes(-1), cancellationToken);
        await service.CreateAsync("future", utcNow.AddHours(1), cancellationToken);
        await service.CreateAsync("pinned", null, cancellationToken);

        // Act
        var raw = await service.FindAsync(past.Value.Id, cancellationToken);
        var active = await service.ListActiveAsync(utcNow, cancellationToken);

        // Assert
        raw.ShouldBeSuccess();
        raw.Value.UserName.ShouldBe("past");
        active.ShouldBeSuccess();
        active.Value.Select(session => session.UserName).ShouldBe(["future", "pinned"], ignoreOrder: true);
    }

    private ServiceProvider BuildProvider()
        => BuildProvider(TimeSpan.Zero);

    private ServiceProvider BuildProvider(TimeSpan sessionTimeToLive)
    {
        var services = new ServiceCollection();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("app", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<UserSession>("sessions", d => d
                    .WithCollectionName("sessions")
                    .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7)
                    .WithCollectionItemsTimeToLive(sessionTimeToLive, x => x.ExpiresAt!));
                db.AddDocumentBinding<AuditEntry>("audit-entries", d => d
                    .WithCollectionName("audit_entries")
                    .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7)
                    .WithCollectionItemsTimeToLive(TimeSpan.FromDays(30), x => x.CreatedAt));
            }));

        return AcceptanceServiceProviderFactory.Create(services);
    }

    private async Task SetTtlMonitorSleepSecondsAsync(int seconds)
    {
        using var client = new MongoClient(connectionString);
        var admin = client.GetDatabase("admin");
        var command = new BsonDocument
        {
            { "setParameter", 1 },
            { "ttlMonitorSleepSecs", seconds }
        };

        await admin.RunCommandAsync<BsonDocument>(command);
    }

    private async Task<List<BsonDocument>> ServerIndexesAsync(string collectionName)
    {
        using var client = new MongoClient(connectionString);
        var database = client.GetDatabase("app");
        var collection = database.GetCollection<BsonDocument>(collectionName);
        var cursor = await collection.Indexes.ListAsync();

        return await cursor.ToListAsync();
    }

    private static BsonDocument TtlIndex(IReadOnlyList<BsonDocument> indexes, string name)
        => indexes.Single(index => index["name"].AsString == name);

    private static async Task<Result<UserSession>> WaitUntilExpiredAsync(
        IGenericRepository<UserSession> sessions,
        Guid id,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);
        var found = await sessions.GetAsync(id, cancellationToken);
        while (found.IsSuccess && DateTime.UtcNow < deadline)
        {
            var pause = TimeSpan.FromMilliseconds(250);
            await Task.Delay(pause, cancellationToken);
            found = await sessions.GetAsync(id, cancellationToken);
        }

        return found;
    }
}