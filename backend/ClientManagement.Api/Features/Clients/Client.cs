
namespace ClientManagement.Api.Features.Clients;

public class Client
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Company { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<FollowUps.FollowUp> FollowUps { get; set; } = [];
    public List<Notes.ClientNote> Notes { get; set; } = [];
}
