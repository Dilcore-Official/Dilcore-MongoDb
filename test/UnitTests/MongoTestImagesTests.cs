namespace Dilcore.MongoDB.UnitTests;

[NonParallelizable]
public class MongoTestImagesTests
{
    [Test]
    public void Mongo_UsesDefaultTag_WhenEnvironmentVariableIsMissing()
    {
        var previous = Environment.GetEnvironmentVariable(MongoTestImages.ImageTagEnvironmentVariable);

        try
        {
            Environment.SetEnvironmentVariable(MongoTestImages.ImageTagEnvironmentVariable, null);

            MongoTestImages.Mongo.ShouldBe($"mongo:{MongoTestImages.DefaultTag}");
        }
        finally
        {
            Environment.SetEnvironmentVariable(MongoTestImages.ImageTagEnvironmentVariable, previous);
        }
    }

    [Test]
    public void Mongo_UsesEnvironmentTag_WhenSet()
    {
        var previous = Environment.GetEnvironmentVariable(MongoTestImages.ImageTagEnvironmentVariable);

        try
        {
            Environment.SetEnvironmentVariable(MongoTestImages.ImageTagEnvironmentVariable, "8.0");

            MongoTestImages.Mongo.ShouldBe("mongo:8.0");
        }
        finally
        {
            Environment.SetEnvironmentVariable(MongoTestImages.ImageTagEnvironmentVariable, previous);
        }
    }

    [Test]
    public void Mongo_TrimsEnvironmentTag_AndTreatsWhitespaceAsDefault()
    {
        var previous = Environment.GetEnvironmentVariable(MongoTestImages.ImageTagEnvironmentVariable);

        try
        {
            Environment.SetEnvironmentVariable(MongoTestImages.ImageTagEnvironmentVariable, "  7.0  ");
            var trimmed = MongoTestImages.Mongo;

            Environment.SetEnvironmentVariable(MongoTestImages.ImageTagEnvironmentVariable, "   ");
            var blank = MongoTestImages.Mongo;

            trimmed.ShouldBe("mongo:7.0");
            blank.ShouldBe($"mongo:{MongoTestImages.DefaultTag}");
        }
        finally
        {
            Environment.SetEnvironmentVariable(MongoTestImages.ImageTagEnvironmentVariable, previous);
        }
    }
}