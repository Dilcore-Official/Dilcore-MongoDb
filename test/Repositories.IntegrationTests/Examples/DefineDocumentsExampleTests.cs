using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Policies;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.Extensions;
using FluentResults;
using MongoDB.Bson;

namespace Dilcore.MongoDB.Repositories.IntegrationTests.Examples;

/// <summary>Proves docs/guides/define-documents.md.</summary>
public class DefineDocumentsExampleTests : ExampleIntegrationTests
{
    public sealed class Note : IDocumentEntity<Guid>, IAuditableDocument
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Text { get; set; } = "";
    }

    public sealed class LegacyDoc : IDocumentEntity<ObjectId>
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = "";
    }

    public sealed class NoteService(IGenericRepository<Note> notes)
    {
        public async Task<Result<Note>> AddAsync(string text, CancellationToken cancellationToken = default)
        {
            var note = new Note { Text = text };

            return await notes.StoreAsync(note, cancellationToken);
        }
    }

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("app", db =>
            {
                db.OnCluster("primary");

                db.AddDocumentBinding<Note>("notes", d => d
                    .WithCollectionName("notes")
                    .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7));

                db.AddDocumentBinding<LegacyDoc>("legacy", d => d.WithCollectionName("legacy"));
            }));

        return AcceptanceServiceProviderFactory.Create(services);
    }

    [Test]
    public async Task GuidDocument_GetsV7IdAndAuditStamps()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var service = ActivatorUtilities.CreateInstance<NoteService>(scope.ServiceProvider);

        // Act
        var result = await service.AddAsync("hello", CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();
        result.Value.Id.ShouldNotBe(Guid.Empty);
        result.Value.CreatedAt.ShouldNotBe(default);
        result.Value.UpdatedAt.ShouldNotBe(default);
    }

    [Test]
    public async Task ObjectIdDocument_GetsDriverAssignedId()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var legacy = scope.ServiceProvider.GetRequiredService<IGenericRepository<LegacyDoc>>();

        // Act
        var result = await legacy.StoreAsync(new LegacyDoc { Name = "a" }, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();
        result.Value.Id.ShouldNotBe(ObjectId.Empty);
    }

    [Test]
    public async Task OversizedDocument_ReturnsDocumentTooLargeError()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var service = ActivatorUtilities.CreateInstance<NoteService>(scope.ServiceProvider);

        // Act
        var result = await service.AddAsync(new string('x', 17 * 1024 * 1024), CancellationToken.None);

        // Assert
        result.ShouldBeFailure();
        result.HasError<DocumentTooLargeError>().ShouldBeTrue();
    }

    [Test]
    public void GuidGenerationOnObjectId_ThrowsAtRegistration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("app", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<LegacyDoc>("legacy", d => d
                    .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7));
            }));

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }
}