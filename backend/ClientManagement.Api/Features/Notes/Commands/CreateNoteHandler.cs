
using ClientManagement.Api.Features.Notes.DTOs;
using ClientManagement.Api.Infrastructure.Data;
using ClientManagement.Api.Infrastructure.Realtime;
using Microsoft.EntityFrameworkCore;

using ClientManagement.Api.Features.Notes.Events;
using ClientManagement.Api.Infrastructure.Messaging.Notes;

namespace ClientManagement.Api.Features.Notes.Commands;

public static class CreateNoteHandler
{
    public static async Task<IResult> HandleAsync(
        int clientId,
        CreateNoteRequest request,
        AppDbContext db,
        IRealtimeNotifier realtimeNotifier,
        INoteEventPublisher eventPublisher,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("CreateNoteHandler");

        // Check whether the client exists
        var clientExists = await db.Clients
            .AnyAsync(x => x.Id == clientId, ct);

        if (!clientExists)
        {
            return Results.NotFound(new
            {
                message = "Client not found."
            });
        }

        // Create a new note
        var note = new ClientNote
        {
            ClientId = clientId,
            Content = request.Content.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        // Save to SQL Server
        db.ClientNotes.Add(note);
        await db.SaveChangesAsync(ct);



        // Notify other connected users
        try
        {
            await realtimeNotifier.NoteChangedAsync(
                note.ClientId,
                "Created",
                ct
            );
        }
        catch (Exception ex)
        {
            

            logger.LogWarning(
                ex,
                "Failed to broadcast NoteChanged for Note {NoteId}",
                note.Id
            );
        }

        

        try
        {
            // RabbitMQ event publishing
            var noteCreatedEvent = new NoteCreatedEvent(
                Guid.NewGuid(),
                note.Id,
                note.ClientId,
                DateTime.UtcNow
            );

            await eventPublisher.PublishCreatedAsync(
                noteCreatedEvent,
                ct
            );

            logger.LogInformation(
                "Published NoteCreated event {EventId} for Note {NoteId}",
                noteCreatedEvent.EventId,
                note.Id
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "RabbitMQ publishing failed for Note {NoteId}",
                note.Id
            );
        }

        // Create response
        var response = new NoteResponse(
            note.Id,
            note.ClientId,
            note.Content,
            note.CreatedAt
        );

        return Results.Created(
            $"/api/clients/{clientId}/notes",
            response
        );
    }
}
