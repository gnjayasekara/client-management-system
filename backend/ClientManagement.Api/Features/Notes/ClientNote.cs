
namespace ClientManagement.Api.Features.Notes;

public class ClientNote
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Clients.Client Client { get; set; } = null!;
}
