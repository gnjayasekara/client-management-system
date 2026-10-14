
namespace ClientManagement.Api.Features.FollowUps;

public class FollowUp
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public required string Title { get; set; }
    public DateTime DueDate { get; set; }
    public bool IsCompleted { get; set; } = false;

    public Clients.Client Client { get; set; } = null!;
}
