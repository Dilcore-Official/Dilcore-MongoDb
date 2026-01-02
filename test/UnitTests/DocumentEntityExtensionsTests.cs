using AutoFixture.NUnit4;
using Dilcore.DocumentDb.Abstractions.Exceptions;
using Dilcore.DocumentDb.Abstractions.Extensions;
using Dilcore.DocumentDb.Abstractions.Helpers;

namespace Dilcore.DocumentDb.Abstractions.UnitTests;

public class DocumentEntityExtensionsTests
{
    [Test]
    public void DocumentEntity_WithUpdateAt()
    {
        var entity = new TestEntity();
        entity.UpdatedNow();

        entity.UpdatedAt.ShouldBe(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Test]
    public void DocumentEntity_WithETag()
    {
        var entity = new TestEntity();
        entity.GenerateETag();

        var expected = DocumentDbHelper.GenerateEtag();
        entity.ETag.ShouldBeInRange(expected - 100, expected + 100);
    }

    [Test]
    public void DocumentEntity_WithNewId()
    {
        var entity = new TestEntity();
        entity.NewId();

        entity.Id.ShouldNotBe(Guid.Empty);
    }

    [Test]
    [InlineAutoData]
    [InlineAutoData(Constants.EmptyETag)]
    public void DocumentEntity_WithIsNew(long etag)
    {
        var entity = new TestEntity { ETag = etag };

        entity.IsNew().ShouldBe(etag == Constants.EmptyETag);
    }

    [Test]
    public void DocumentEntity_CheckId_ShouldThrowException_WhenEmpty()
    {
        var entity = new TestEntity();
        Should.Throw<DocumentIdentifierIsEmptyException>(() => entity.CheckId());
    }

    [Test]
    public void DocumentEntity_CheckId_ShouldNotThrowException_WhenNotEmpty()
    {
        var entity = new TestEntity();
        entity.NewId();

        Should.NotThrow(() => entity.CheckId());
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void DocumentEntity_IsIdEmpty(bool withId, bool isEmpty)
    {
        var entity = new TestEntity();
        if (withId)
        {
            entity.NewId();
        }

        entity.IsIdEmpty().ShouldBe(isEmpty);
    }

    private class TestEntity : IDocumentEntity
    {
        public Guid Id { get; set; }
        public long ETag { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public string? Value { get; set; }
    }
}