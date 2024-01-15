namespace Dilcore.DocumentDb.MongoDb.Repositories;

public class RegisterRepositoryOptions
{
    internal bool RegisterBulkRepository { get; private set; }

    private RegisterRepositoryOptions()
    { }
        
    public RegisterRepositoryOptions WithBulkRepository()
    {
        RegisterBulkRepository = true;
        return this;
    }
        
    internal static RegisterRepositoryOptions Create() => new();
}