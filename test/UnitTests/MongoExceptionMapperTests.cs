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

    [Test]
    public void ToError_FormatException_Unrelated_IsPlainError()
    {
        var error = MongoExceptionMapper.ToError(new FormatException("invalid date format"));
        error.ShouldNotBeOfType<DocumentTooLargeError>();
        error.Message.ShouldBe("invalid date format");
    }

    [Test]
    public void ToError_MongoCommandException_Code10334_IsDocumentTooLarge()
    {
        var error = MongoExceptionMapper.ToError(CreateCommandException(10334, "document too large"));
        error.ShouldBeOfType<DocumentTooLargeError>();
    }

    [Test]
    public void ToError_MongoCommandException_Generic_IsPlainError()
    {
        var error = MongoExceptionMapper.ToError(CreateCommandException(11000, "duplicate key"));
        error.ShouldNotBeOfType<DocumentTooLargeError>();
        error.Message.ShouldContain("duplicate key");
    }

    [Test]
    public void ToError_MongoWriteException_Code10334_IsDocumentTooLarge()
    {
        var error = MongoExceptionMapper.ToError(CreateWriteException(10334, "object to insert too large"));
        error.ShouldBeOfType<DocumentTooLargeError>();
    }

    [Test]
    public void ToError_MongoWriteException_GenericWriteError_IsPlainError()
    {
        var error = MongoExceptionMapper.ToError(CreateWriteException(11000, "E11000 duplicate key"));
        error.ShouldNotBeOfType<DocumentTooLargeError>();
        error.Message.ShouldContain("E11000 duplicate key");
    }

    [Test]
    public void ToError_MongoBulkWriteException_WithWriteErrors_IsBulkWritePartialFailure()
    {
        var error = MongoExceptionMapper.ToError(CreateBulkWriteException(
        [
            CreateBulkWriteError(0, 11000, "dup at 0"),
            CreateBulkWriteError(2, 11000, "dup at 2")
        ]));

        var partial = error.ShouldBeOfType<BulkWritePartialFailureError>();
        partial.Items.Count.ShouldBe(2);
        partial.Items.ShouldContain(item => item.Index == 0 && !item.Succeeded && item.ErrorMessage == "dup at 0");
        partial.Items.ShouldContain(item => item.Index == 2 && !item.Succeeded && item.ErrorMessage == "dup at 2");
    }

    [Test]
    public void ToError_MongoBulkWriteException_EmptyWriteErrors_IsPlainError()
    {
        var error = MongoExceptionMapper.ToError(CreateBulkWriteException([]));
        error.ShouldNotBeOfType<BulkWritePartialFailureError>();
        error.ShouldNotBeOfType<DocumentTooLargeError>();
    }

    [Test]
    public void Fail_And_FailT_WrapSameErrorType()
    {
        var exception = CreateCommandException(10334, "too large");

        var fail = MongoExceptionMapper.Fail(exception);
        fail.IsFailed.ShouldBeTrue();
        fail.Errors[0].ShouldBeOfType<DocumentTooLargeError>();

        var failT = MongoExceptionMapper.Fail<int>(exception);
        failT.IsFailed.ShouldBeTrue();
        failT.Errors[0].ShouldBeOfType<DocumentTooLargeError>();
    }

    [Test]
    public void ToError_RetryableWriteErrorLabel_IsTransientWriteError()
    {
        var exception = new MongoException("retryable write failed");
        exception.AddErrorLabel("RetryableWriteError");

        var error = MongoExceptionMapper.ToError(exception);
        error.ShouldBeOfType<TransientWriteError>();
    }

    [Test]
    public void ToError_CommandCode121_WithDuplicateKeyMessage_IsNotDuplicateKey()
    {
        var error = MongoExceptionMapper.ToError(
            CreateCommandException(121, "Document failed validation: duplicate key mentioned"));

        error.ShouldNotBeOfType<DuplicateKeyError>();
        error.Message.ShouldContain("duplicate key");
    }

    [Test]
    public void ToError_CommandCode11000_IsDuplicateKey()
    {
        var error = MongoExceptionMapper.ToError(CreateCommandException(11000, "E11000 duplicate key"));
        error.ShouldBeOfType<DuplicateKeyError>();
    }

    [Test]
    public void ToError_MongoWriteException_WriteConcernOnly_IsWriteConcernFailure()
    {
        var exception = new MongoWriteException(
            CreateConnectionId(),
            writeError: null,
            CreateWriteConcernError(64, "wtimeout", "waiting for replication timed out"),
            innerException: null);

        var error = MongoExceptionMapper.ToError(exception);
        error.ShouldBeOfType<WriteConcernFailureError>();
    }

    [Test]
    public void ToError_MongoWriteConcernException_IsWriteConcernFailure()
    {
        var exception = new MongoWriteConcernException(
            CreateConnectionId(),
            "waiting for replication timed out",
            new WriteConcernResult(new BsonDocument("ok", 1)));

        var error = MongoExceptionMapper.ToError(exception);
        error.ShouldBeOfType<WriteConcernFailureError>();
    }

    [Test]
    public void ToError_MongoWriteException_GenericWriteError_FallsThroughToPlainError()
    {
        var error = MongoExceptionMapper.ToError(CreateWriteException(2, "BadValue"));

        error.ShouldNotBeOfType<DuplicateKeyError>();
        error.ShouldNotBeOfType<DocumentTooLargeError>();
        error.ShouldNotBeOfType<WriteConcernFailureError>();
        error.ShouldNotBeOfType<TransientWriteError>();
        error.Message.ShouldContain("BadValue");
    }

    [Test]
    public void ToError_MongoBulkWriteException_SingleDuplicate_IsDuplicateKey()
    {
        var error = MongoExceptionMapper.ToError(CreateBulkWriteException(
        [
            CreateBulkWriteError(0, 11000, "E11000 duplicate key")
        ]));

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