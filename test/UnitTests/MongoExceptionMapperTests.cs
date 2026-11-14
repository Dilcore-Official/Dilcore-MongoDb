using System.Net;
using System.Reflection;
using Dilcore.MongoDB.Abstractions.Internal;
using Dilcore.MongoDB.Abstractions.Results;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Dilcore.MongoDB.UnitTests;

public class MongoExceptionMapperTests
{
    [Test]
    public void ToError_ObjectTooLargeMessage_IsDocumentTooLarge()
    {
        // Act
        var error = MongoExceptionMapper.ToError(new MongoException("object to insert too large"));

        // Assert
        error.ShouldBeOfType<DocumentTooLargeError>();
        error.ShouldBeOfType<DocumentTooLargeError>().Code.ShouldBe(DocumentTooLargeError.ErrorCode);
    }

    [Test]
    public void ToError_MaxDocumentSizeFormatException_IsDocumentTooLarge()
    {
        // Act
        var error = MongoExceptionMapper.ToError(
            new FormatException("Size 17825950 is larger than MaxDocumentSize 16777216."));

        // Assert
        error.ShouldBeOfType<DocumentTooLargeError>();
    }

    [Test]
    public void ToError_BsonObjSizeMessage_IsDocumentTooLarge()
    {
        // Act
        var error = MongoExceptionMapper.ToError(new MongoException("BSONObj size: 16793600 is invalid"));

        // Assert
        error.ShouldBeOfType<DocumentTooLargeError>();
    }

    [Test]
    public void ToError_GenericMongoException_IsPlainError()
    {
        // Act
        var error = MongoExceptionMapper.ToError(new MongoException("duplicate key error"));

        // Assert
        error.ShouldNotBeOfType<DocumentTooLargeError>();
        error.Message.ShouldBe("duplicate key error");
    }

    [Test]
    public void ToError_NonMongoException_IsPlainError()
    {
        // Act
        var error = MongoExceptionMapper.ToError(new InvalidOperationException("boom"));

        // Assert
        error.ShouldNotBeOfType<MongoOperationError>();
        error.Message.ShouldBe("boom");
    }

    [Test]
    public void ToError_FormatException_Unrelated_IsPlainError()
    {
        // Act
        var error = MongoExceptionMapper.ToError(new FormatException("invalid date format"));

        // Assert
        error.ShouldNotBeOfType<DocumentTooLargeError>();
        error.Message.ShouldBe("invalid date format");
    }

    [Test]
    public void ToError_MongoCommandException_Code10334_IsDocumentTooLarge()
    {
        // Act
        var error = MongoExceptionMapper.ToError(CreateCommandException(10334, "document too large"));

        // Assert
        error.ShouldBeOfType<DocumentTooLargeError>();
    }

    [Test]
    public void ToError_MongoCommandException_Generic_IsPlainError()
    {
        // Act
        var error = MongoExceptionMapper.ToError(CreateCommandException(11000, "duplicate key"));

        // Assert
        error.ShouldNotBeOfType<DocumentTooLargeError>();
        error.Message.ShouldContain("duplicate key");
    }

    [Test]
    public void ToError_MongoWriteException_Code10334_IsDocumentTooLarge()
    {
        // Act
        var error = MongoExceptionMapper.ToError(CreateWriteException(10334, "object to insert too large"));

        // Assert
        error.ShouldBeOfType<DocumentTooLargeError>();
    }

    [Test]
    public void ToError_MongoWriteException_GenericWriteError_IsPlainError()
    {
        // Act
        var error = MongoExceptionMapper.ToError(CreateWriteException(11000, "E11000 duplicate key"));

        // Assert
        error.ShouldNotBeOfType<DocumentTooLargeError>();
        error.Message.ShouldContain("E11000 duplicate key");
    }

    [Test]
    public void ToError_MongoBulkWriteException_WithWriteErrors_IsBulkWritePartialFailure()
    {
        // Act
        var error = MongoExceptionMapper.ToError(CreateBulkWriteException(
        [
            CreateBulkWriteError(0, 11000, "dup at 0"),
            CreateBulkWriteError(2, 11000, "dup at 2")
        ]));

        // Assert
        var partial = error.ShouldBeOfType<BulkWritePartialFailureError>();
        partial.Items.Count.ShouldBe(2);
        partial.Items.ShouldContain(item => item.Index == 0 && !item.Succeeded && item.ErrorMessage == "dup at 0");
        partial.Items.ShouldContain(item => item.Index == 2 && !item.Succeeded && item.ErrorMessage == "dup at 2");
    }

    [Test]
    public void ToError_MongoBulkWriteException_EmptyWriteErrors_IsPlainError()
    {
        // Act
        var error = MongoExceptionMapper.ToError(CreateBulkWriteException([]));

        // Assert
        error.ShouldNotBeOfType<BulkWritePartialFailureError>();
        error.ShouldNotBeOfType<DocumentTooLargeError>();
    }

    [Test]
    public void Fail_And_FailT_WrapSameErrorType()
    {
        // Arrange
        var exception = CreateCommandException(10334, "too large");

        // Act
        var fail = MongoExceptionMapper.Fail(exception);
        var failT = MongoExceptionMapper.Fail<int>(exception);

        // Assert
        fail.IsFailed.ShouldBeTrue();
        fail.Errors[0].ShouldBeOfType<DocumentTooLargeError>();
        failT.IsFailed.ShouldBeTrue();
        failT.Errors[0].ShouldBeOfType<DocumentTooLargeError>();
    }

    [Test]
    public void ToError_RetryableWriteErrorLabel_IsTransientWriteError()
    {
        // Arrange
        var exception = new MongoException("retryable write failed");
        exception.AddErrorLabel("RetryableWriteError");

        // Act
        var error = MongoExceptionMapper.ToError(exception);

        // Assert
        error.ShouldBeOfType<TransientWriteError>();
    }

    [Test]
    public void ToError_CommandCode121_WithDuplicateKeyMessage_IsNotDuplicateKey()
    {
        // Act
        var error = MongoExceptionMapper.ToError(
            CreateCommandException(121, "Document failed validation: duplicate key mentioned"));

        // Assert
        error.ShouldNotBeOfType<DuplicateKeyError>();
        error.Message.ShouldContain("duplicate key");
    }

    [Test]
    public void ToError_CommandCode11000_IsDuplicateKey()
    {
        // Act
        var error = MongoExceptionMapper.ToError(CreateCommandException(11000, "E11000 duplicate key"));

        // Assert
        error.ShouldBeOfType<DuplicateKeyError>();
    }

    [Test]
    public void ToError_MongoWriteException_WriteConcernOnly_IsWriteConcernFailure()
    {
        // Arrange
        var exception = new MongoWriteException(
            CreateConnectionId(),
            writeError: null,
            CreateWriteConcernError(64, "wtimeout", "waiting for replication timed out"),
            innerException: null);

        // Act
        var error = MongoExceptionMapper.ToError(exception);

        // Assert
        error.ShouldBeOfType<WriteConcernFailureError>();
    }

    [Test]
    public void ToError_MongoWriteConcernException_IsWriteConcernFailure()
    {
        // Arrange
        var exception = new MongoWriteConcernException(
            CreateConnectionId(),
            "waiting for replication timed out",
            new WriteConcernResult(new BsonDocument("ok", 1)));

        // Act
        var error = MongoExceptionMapper.ToError(exception);

        // Assert
        error.ShouldBeOfType<WriteConcernFailureError>();
    }

    [Test]
    public void ToError_MongoWriteException_GenericWriteError_FallsThroughToPlainError()
    {
        // Act
        var error = MongoExceptionMapper.ToError(CreateWriteException(2, "BadValue"));

        // Assert
        error.ShouldNotBeOfType<DuplicateKeyError>();
        error.ShouldNotBeOfType<DocumentTooLargeError>();
        error.ShouldNotBeOfType<WriteConcernFailureError>();
        error.ShouldNotBeOfType<TransientWriteError>();
        error.Message.ShouldContain("BadValue");
    }

    [Test]
    public void ToError_MongoBulkWriteException_SingleDuplicate_IsDuplicateKey()
    {
        // Act
        var error = MongoExceptionMapper.ToError(CreateBulkWriteException(
        [
            CreateBulkWriteError(0, 11000, "E11000 duplicate key")
        ]));

        // Assert
        error.ShouldBeOfType<DuplicateKeyError>();
    }

    private static ConnectionId CreateConnectionId()
        => new(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017)));

    private static MongoCommandException CreateCommandException(int code, string message)
    {
        var result = new BsonDocument
        {
            { "ok", 0 },
            { "code", code },
            { "errmsg", message }
        };
        return new MongoCommandException(CreateConnectionId(), message, new BsonDocument("ping", 1), result);
    }

    private static MongoWriteException CreateWriteException(int code, string message)
    {
        var writeError = CreateWriteError(code, message);
        return new MongoWriteException(CreateConnectionId(), writeError, writeConcernError: null, innerException: null);
    }

    private static MongoBulkWriteException<BsonDocument> CreateBulkWriteException(
        IEnumerable<BulkWriteError> writeErrors)
    {
        var processed = Array.Empty<WriteModel<BsonDocument>>();
        var result = new BulkWriteResult<BsonDocument>.Unacknowledged(0, processed);
        return new MongoBulkWriteException<BsonDocument>(
            CreateConnectionId(),
            result,
            writeErrors,
            writeConcernError: null,
            unprocessedRequests: processed);
    }

    private static WriteError CreateWriteError(int code, string message)
    {
        var ctor = typeof(WriteError).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            [typeof(ServerErrorCategory), typeof(int), typeof(string), typeof(BsonDocument)],
            modifiers: null)
            ?? throw new InvalidOperationException("WriteError constructor not found.");

        return (WriteError)ctor.Invoke(
        [
            ServerErrorCategory.Uncategorized,
            code,
            message,
            null
        ]);
    }

    private static WriteConcernError CreateWriteConcernError(int code, string codeName, string message)
    {
        var ctor = typeof(WriteConcernError).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            [typeof(int), typeof(string), typeof(string), typeof(BsonDocument), typeof(IEnumerable<string>)],
            modifiers: null)
            ?? throw new InvalidOperationException("WriteConcernError constructor not found.");

        return (WriteConcernError)ctor.Invoke(
        [
            code,
            codeName,
            message,
            null,
            Array.Empty<string>()
        ]);
    }

    private static BulkWriteError CreateBulkWriteError(int index, int code, string message)
    {
        var ctor = typeof(BulkWriteError).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            [typeof(int), typeof(ServerErrorCategory), typeof(int), typeof(string), typeof(BsonDocument)],
            modifiers: null)
            ?? throw new InvalidOperationException("BulkWriteError constructor not found.");

        return (BulkWriteError)ctor.Invoke(
        [
            index,
            ServerErrorCategory.Uncategorized,
            code,
            message,
            null
        ]);
    }
}