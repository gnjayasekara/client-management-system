
using ClientManagement.Api.Infrastructure.Data;
using ClientManagement.Api.Infrastructure.Realtime;
using ClientManagement.Api.Features.Notes.Events;
using ClientManagement.Api.Infrastructure.Messaging.Notes;
using Microsoft.EntityFrameworkCore;

namespace ClientManagement.Api.Features.Notes.Commands;

public static class DeleteNoteHandler
{
    public static async Task<IResult> HandleAsync(
        int id,
        AppDbContext db,
        IRealtimeNotifier realtimeNotifier,
        INoteEventPublisher eventPublisher,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger(
            "DeleteNoteHandler");

        var note = await db.ClientNotes
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (note is null)
            return Results.NotFound();

        
        var noteId = note.Id;
        var clientId = note.ClientId;

        db.ClientNotes.Remove(note);

        await db.SaveChangesAsync(ct);

        // SignalR notification
        try
        {
            await realtimeNotifier.NoteChangedAsync(
                clientId,
                "Deleted",
                ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "SignalR notification failed for Note {NoteId}",
                noteId);
        }

        // RabbitMQ event
        try
        {
            var noteDeletedEvent = new NoteDeletedEvent(
                Guid.NewGuid(),
                noteId,
                clientId,
                DateTime.UtcNow);

            await eventPublisher.PublishDeletedAsync(
                noteDeletedEvent,
                ct);

            logger.LogInformation(
                "Published NoteDeleted event for Note {NoteId}",
                noteId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "RabbitMQ publishing failed for Note {NoteId}",
                noteId);
        }

        return Results.NoContent();
    }
}
