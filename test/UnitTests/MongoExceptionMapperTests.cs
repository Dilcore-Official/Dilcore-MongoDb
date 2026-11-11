using Dilcore.MongoDB.Abstractions.Internal;
using Dilcore.MongoDB.Abstractions.Results;
using MongoDB.Driver;

namespace Dilcore.MongoDB.UnitTests;

public class MongoExceptionMapperTests
{
    [Test]
    public void ToError_ObjectTooLargeMessage_IsDocumentTooLarge()
    {
        var error = MongoExceptionMapper.ToError(new MongoException("object to insert too large"));
        error.ShouldBeOfType<DocumentTooLargeError>();
        error.ShouldBeOfType<DocumentTooLargeError>().Code.ShouldBe(DocumentTooLargeError.ErrorCode);
    }

    [Test]
    public void ToError_MaxDocumentSizeFormatException_IsDocumentTooLarge()
    {
        var error = MongoExceptionMapper.ToError(
            new FormatException("Size 17825950 is larger than MaxDocumentSize 16777216."));
        error.ShouldBeOfType<DocumentTooLargeError>();
    }

    [Test]
    public void ToError_BsonObjSizeMessage_IsDocumentTooLarge()
    {
        var error = MongoExceptionMapper.ToError(new MongoException("BSONObj size: 16793600 is invalid"));
        error.ShouldBeOfType<DocumentTooLargeError>();
    }

    [Test]
    public void ToError_GenericMongoException_IsPlainError()
    {
        var error = MongoExceptionMapper.ToError(new MongoException("duplicate key error"));
        error.ShouldNotBeOfType<DocumentTooLargeError>();
        error.Message.ShouldBe("duplicate key error");
    }

    [Test]
    public void ToError_NonMongoException_IsPlainError()
    {
        var error = MongoExceptionMapper.ToError(new InvalidOperationException("boom"));
        error.ShouldNotBeOfType<MongoOperationError>();
        error.Message.ShouldBe("boom");
    }
}
