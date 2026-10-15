
namespace ClientManagement.Api.Features.Notes.DTOs;

public record NoteResponse(
    int Id,
    int ClientId,
    string Content,
    DateTime CreatedAt
);
