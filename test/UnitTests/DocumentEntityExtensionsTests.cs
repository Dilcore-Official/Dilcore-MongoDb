using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Exceptions;
using Dilcore.MongoDB.Abstractions.Extensions;
using Dilcore.MongoDB.Abstractions.Helpers;
using Dilcore.MongoDB.Abstractions.Internal;
using Dilcore.MongoDB.Abstractions.Policies;
using AutoFixture.NUnit4;
using AbstractionsConstants = Dilcore.MongoDB.Abstractions.Constants;

namespace Dilcore.MongoDB.UnitTests;

public class DocumentEntityExtensionsTests
{
    [Test]
    public void DocumentEntity_WithUpdateAt()
    {
        // Arrange
        var entity = new FullPolicyEntity();

        // Act
        entity.UpdatedNow();

        // Assert
        entity.UpdatedAt.ShouldBe(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Test]
    public void DocumentEntity_WithETag()
    {
        // Arrange
        var entity = new FullPolicyEntity();

        // Act
        entity.GenerateETag();

        // Assert
        entity.ETag.ShouldNotBe(AbstractionsConstants.EmptyETag);
    }

    [Test]
    public void MongoDbHelper_GenerateEtag_IsCollisionResistant()
    {
        // Act
        var values = Enumerable.Range(0, 10_000).Select(_ => MongoDbHelper.GenerateEtag()).ToHashSet();

        // Assert
        values.Count.ShouldBe(10_000);
        values.ShouldNotContain(AbstractionsConstants.EmptyETag);
    }

    [Test]
    public void DocumentEntity_WithNewId()
    {
        // Arrange
        var entity = new FullPolicyEntity();

        // Act
        entity.NewId();

        // Assert
        entity.Id.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public void DocumentEntity_WithNewId_SequentialVersion7_SetsUuidVersion7()
    {
        // Arrange
        var entity = new FullPolicyEntity();

        // Act
        entity.NewId(GuidIdGenerationStrategy.SequentialVersion7);

        // Assert
        entity.Id.ShouldNotBe(Guid.Empty);
        entity.Id.Version.ShouldBe(7);
    }

    [Test]
    [InlineAutoData]
    [InlineAutoData(AbstractionsConstants.EmptyETag)]
    public void DocumentEntity_WithIsNew(long etag)
    {
        // Arrange
        var entity = new FullPolicyEntity { ETag = etag };

        // Act
        var isNew = entity.IsNew();

        // Assert
        isNew.ShouldBe(etag == AbstractionsConstants.EmptyETag);
    }

    [Test]
    public void DocumentEntity_CheckId_ShouldThrowException_WhenEmpty()
    {
        // Arrange
        var entity = new FullPolicyEntity();

        // Act & Assert
        Should.Throw<DocumentIdentifierIsEmptyException>(() => entity.CheckId());
    }

    [Test]
    public void DocumentEntity_CheckId_ShouldNotThrowException_WhenNotEmpty()
    {
        // Arrange
        var entity = new FullPolicyEntity();
        entity.NewId();

        // Act & Assert
        Should.NotThrow(() => entity.CheckId());
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void DocumentEntity_IsIdEmpty(bool withId, bool isEmpty)
    {
        // Arrange
        var entity = new FullPolicyEntity();
        if (withId)
        {
            entity.NewId();
        }

        // Act
        var empty = entity.IsIdEmpty();

        // Assert
        empty.ShouldBe(isEmpty);
    }

    [Test]
    public void DocumentEntity_ToBsonUpdateDocument_ExcludesId()
    {
        // Arrange
        var entity = new FullPolicyEntity { Id = Guid.NewGuid(), Value = "x" };

        // Act
        var bson = entity.ToBsonSnapshotUpdateDocument();

        // Assert
        bson.Contains("$set").ShouldBeTrue();
        bson["$set"].AsBsonDocument.Contains(DocumentElementNames.Id).ShouldBeFalse();
    }

    [Test]
    public void PolicyAbsent_GenerateETag_IsNoOp()
    {
        // Arrange
        var entity = new MinimalEntity();

        // Act & Assert
        Should.NotThrow(() => entity.GenerateETag());
    }

    [Test]
    public void PolicyAbsent_CreatedNowAndUpdatedNow_AreNoOps()
    {
        // Arrange
        var entity = new MinimalEntity();

        // Act & Assert
        Should.NotThrow(() =>
        {
            entity.CreatedNow();
            entity.UpdatedNow();
        });
    }

    [Test]
    public void PolicyAbsent_IsNew_ReturnsTrue()
    {
        // Arrange
        var entity = new MinimalEntity { Id = Guid.NewGuid() };

        // Act
        var isNew = entity.IsNew();

        // Assert
        isNew.ShouldBeTrue();
    }

    [Test]
    public void PolicyAbsent_NewId_And_IsIdEmpty_Work()
    {
        // Arrange
        var entity = new MinimalEntity();

        // Act
        var before = entity.IsIdEmpty();
        entity.NewId();
        var after = entity.IsIdEmpty();

        // Assert
        before.ShouldBeTrue();
        after.ShouldBeFalse();
        entity.Id.Version.ShouldBe(4);
    }

    private class FullPolicyEntity : IDocumentEntity<Guid>, IHasConcurrencyToken, ISoftDeletable, IAuditableDocument
    {
        public Guid Id { get; set; }
        public long ETag { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? Value { get; set; }
    }

    private class MinimalEntity : IDocumentEntity<Guid>
    {
        public Guid Id { get; set; }
    }
}