namespace Dilcore.MongoDB.UnitTests;

[NonParallelizable]
public class MongoTestImagesTests
{
    [Test]
    public void Mongo_UsesDefaultTag_WhenEnvironmentVariableIsMissing()
    {
        // Arrange
        var previous = Environment.GetEnvironmentVariable(MongoTestImages.ImageTagEnvironmentVariable);

        try
        {
            Environment.SetEnvironmentVariable(MongoTestImages.ImageTagEnvironmentVariable, null);

            // Act
            var image = MongoTestImages.Mongo;

            // Assert
            image.ShouldBe($"mongo:{MongoTestImages.DefaultTag}");
        }
        finally
        {
            Environment.SetEnvironmentVariable(MongoTestImages.ImageTagEnvironmentVariable, previous);
        }
    }

    [Test]
    public void Mongo_UsesEnvironmentTag_WhenSet()
    {
        // Arrange
        var previous = Environment.GetEnvironmentVariable(MongoTestImages.ImageTagEnvironmentVariable);

        try
        {
            Environment.SetEnvironmentVariable(MongoTestImages.ImageTagEnvironmentVariable, "8.0");

            // Act
            var image = MongoTestImages.Mongo;

            // Assert
            image.ShouldBe("mongo:8.0");
        }
        finally
        {
            Environment.SetEnvironmentVariable(MongoTestImages.ImageTagEnvironmentVariable, previous);
        }
    }

    [Test]
    public void Mongo_TrimsEnvironmentTag_AndTreatsWhitespaceAsDefault()
    {
        // Arrange
        var previous = Environment.GetEnvironmentVariable(MongoTestImages.ImageTagEnvironmentVariable);

        try
        {
            Environment.SetEnvironmentVariable(MongoTestImages.ImageTagEnvironmentVariable, "  7.0  ");

            // Act
            var trimmed = MongoTestImages.Mongo;
            Environment.SetEnvironmentVariable(MongoTestImages.ImageTagEnvironmentVariable, "   ");
            var blank = MongoTestImages.Mongo;

            // Assert
            trimmed.ShouldBe("mongo:7.0");
            blank.ShouldBe($"mongo:{MongoTestImages.DefaultTag}");
        }
        finally
        {
            Environment.SetEnvironmentVariable(MongoTestImages.ImageTagEnvironmentVariable, previous);
        }
    }
}