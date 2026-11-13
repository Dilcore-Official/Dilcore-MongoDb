using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Exceptions;
using Dilcore.MongoDB.Abstractions.Extensions;
using Dilcore.MongoDB.Abstractions.Helpers;
using Dilcore.MongoDB.Abstractions.Policies;
using AutoFixture.NUnit4;
using MongoDB.Bson;
using AbstractionsConstants = Dilcore.MongoDB.Abstractions.Constants;

namespace Dilcore.MongoDB.UnitTests;

public class DocumentEntityExtensionsTests
{
    [Test]
    public void DocumentEntity_WithUpdateAt()
    {
        var entity = new FullPolicyEntity();
        entity.UpdatedNow();

        entity.UpdatedAt.ShouldBe(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Test]
    public void DocumentEntity_WithETag()
    {
        var entity = new FullPolicyEntity();
        entity.GenerateETag();

        entity.ETag.ShouldNotBe(AbstractionsConstants.EmptyETag);
    }

    [Test]
    public void MongoDbHelper_GenerateEtag_IsCollisionResistant()
    {
        var values = Enumerable.Range(0, 10_000).Select(_ => MongoDbHelper.GenerateEtag()).ToHashSet();
        values.Count.ShouldBe(10_000);
        values.ShouldNotContain(AbstractionsConstants.EmptyETag);
    }

    [Test]
    public void DocumentEntity_WithNewId()
    {
        var entity = new FullPolicyEntity();
        entity.NewId();

        entity.Id.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public void DocumentEntity_WithNewId_SequentialVersion7_SetsUuidVersion7()
    {
        var entity = new FullPolicyEntity();
        entity.NewId(GuidIdGenerationStrategy.SequentialVersion7);

        entity.Id.ShouldNotBe(Guid.Empty);
        entity.Id.Version.ShouldBe(7);
    }

    [Test]
    [InlineAutoData]
    [InlineAutoData(AbstractionsConstants.EmptyETag)]
    public void DocumentEntity_WithIsNew(long etag)
    {
        var entity = new FullPolicyEntity { ETag = etag };

        entity.IsNew().ShouldBe(etag == AbstractionsConstants.EmptyETag);
    }

    [Test]
    public void DocumentEntity_CheckId_ShouldThrowException_WhenEmpty()
    {
        var entity = new FullPolicyEntity();
        Should.Throw<DocumentIdentifierIsEmptyException>(() => entity.CheckId());
    }

    [Test]
    public void DocumentEntity_CheckId_ShouldNotThrowException_WhenNotEmpty()
    {
        var entity = new FullPolicyEntity();
        entity.NewId();

        Should.NotThrow(() => entity.CheckId());
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void DocumentEntity_IsIdEmpty(bool withId, bool isEmpty)
    {
        var entity = new FullPolicyEntity();
        if (withId)
        {
            entity.NewId();
        }

        entity.IsIdEmpty().ShouldBe(isEmpty);
    }

    [Test]
    public void DocumentEntity_ToBsonUpdateDocument_ExcludesId()
    {
        var entity = new FullPolicyEntity { Id = Guid.NewGuid(), Value = "x" };
        var bson = entity.ToBsonSnapshotUpdateDocument();
        bson.Contains("$set").ShouldBeTrue();
        bson["$set"].AsBsonDocument.Contains("_id").ShouldBeFalse();
    }

    [Test]
    public void PolicyAbsent_GenerateETag_IsNoOp()
    {
        var entity = new MinimalEntity();
        Should.NotThrow(() => entity.GenerateETag());
    }

    [Test]
    public void PolicyAbsent_CreatedNowAndUpdatedNow_AreNoOps()
    {
        var entity = new MinimalEntity();
        Should.NotThrow(() =>
        {
            entity.CreatedNow();
            entity.UpdatedNow();
        });
    }

    [Test]
    public void PolicyAbsent_IsNew_ReturnsTrue()
    {
        var entity = new MinimalEntity { Id = Guid.NewGuid() };
        entity.IsNew().ShouldBeTrue();
    }

    [Test]
    public void PolicyAbsent_NewId_And_IsIdEmpty_Work()
    {
        var entity = new MinimalEntity();
        entity.IsIdEmpty().ShouldBeTrue();
        entity.NewId();
        entity.IsIdEmpty().ShouldBeFalse();
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
