namespace ACARSServer.Model;

public class ApiKey
{
    public int Id { get; set; }

    public required string VatsimCid { get; set; }

    public required string HashedKey { get; set; }

    public DateTime CreatedAt { get; set; }
}
