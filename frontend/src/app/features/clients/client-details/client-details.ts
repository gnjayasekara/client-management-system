
import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { ClientService } from '../../../core/services/client';
import { Client } from '../../../core/models/client.model';
import { AuthService } from '../../../core/services/auth.service';


import { FollowUpService } from '../../../core/services/follow-up';
import { FollowUp } from '../../../core/models/follow-up.model';


import { ClientNoteService } from '../../../core/services/client-note';
import { ClientNote } from '../../../core/models/client-note.model';

import { RealtimeService } from '../../../core/services/realtime.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DestroyRef } from '@angular/core';



@Component({
  selector: 'app-client-details',
  imports: [DatePipe, RouterLink],
  templateUrl: './client-details.html',
  styleUrl: './client-details.css'
})
export class ClientDetails implements OnInit {
  private readonly clientService = inject(ClientService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly authService = inject(AuthService);

  readonly client = signal<Client | null>(null);
  readonly loading = signal(true);
  readonly deleting = signal(false);
  readonly showDeleteConfirmation = signal(false);
  readonly errorMessage = signal('');

  
  private readonly followUpService = inject(FollowUpService);

  readonly clientFollowUps = signal<FollowUp[]>([]);
  readonly followUpsLoading = signal(false);
  readonly followUpsError = signal('');

  
  private readonly clientNoteService = inject(ClientNoteService);

  readonly clientNotes = signal<ClientNote[]>([]);
  readonly notesLoading = signal(false);
  readonly notesSaving = signal(false);
  readonly notesDeletingId = signal<number | null>(null);
  readonly notesError = signal('');
  readonly noteContent = signal('');

  readonly selectedNote = signal<ClientNote | null>(null);

  private readonly realtimeService = inject(RealtimeService);
  private readonly destroyRef = inject(DestroyRef);



  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    const id = Number(idParam);

    if (!idParam || !Number.isInteger(id) || id <= 0) {
      this.loading.set(false);
      this.errorMessage.set('Invalid client ID.');
      return;
    }

    this.loadClient(id);
    this.loadClientFollowUps(id);
    this.loadClientNotes(id);

    
    this.realtimeService.noteChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(event => {
        if (event.entityId === id) {
          this.loadClientNotes(id);
        }
      });

    this.realtimeService.clientChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(event => {
        if (event.entityId === id) {
          this.loadClientNotes(id);
        }
      });

    this.realtimeService.followUpChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.loadClientFollowUps(id);
      });

    this.realtimeService.reconnected$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.loadClient(id);
        this.loadClientNotes(id);
        this.loadClientFollowUps(id);
      });

  }

  loadClient(id: number): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.clientService.getClientById(id).subscribe({
      
      next: (response) => {
        this.client.set(response);
        this.loading.set(false);

        this.loadClientFollowUps(response.id);
        this.loadClientNotes(response.id);
      },
      error: (error: HttpErrorResponse) => {
        this.loading.set(false);

        this.errorMessage.set(
          error.status === 404
            ? 'Client not found.'
            : 'Failed to load client information.'
        );
      }
    });
  }

  
  loadClientFollowUps(clientId: number): void {
    this.followUpsLoading.set(true);
    this.followUpsError.set('');

    this.followUpService.getFollowUps(clientId).subscribe({
      next: (response) => {
        this.clientFollowUps.set(response);
        this.followUpsLoading.set(false);
      },
      error: () => {
        this.followUpsLoading.set(false);
        this.followUpsError.set(
          'Unable to load client follow-ups.'
        );
      }
    });
  }

  
  loadClientNotes(clientId: number): void {
    this.notesLoading.set(true);
    this.notesError.set('');

    this.clientNoteService.getNotes(clientId).subscribe({
      next: (response) => {
        this.clientNotes.set(response);
        this.notesLoading.set(false);
      },
      error: () => {
        this.notesLoading.set(false);
        this.notesError.set('Unable to load client notes.');
      }
    });
  }

  
  addNote(): void {
    const currentClient = this.client();
    const content = this.noteContent().trim();

    if (
      !currentClient ||
      !content ||
      this.notesSaving()
    ) {
      return;
    }

    this.notesSaving.set(true);
    this.notesError.set('');

    this.clientNoteService
      .addNote(currentClient.id, { content })
      .subscribe({
        next: () => {
          this.notesSaving.set(false);
          this.noteContent.set('');

          this.loadClientNotes(currentClient.id);
        },
        error: () => {
          this.notesSaving.set(false);
          this.notesError.set(
            'Unable to add note. Please try again.'
          );
        }
      });
  }

  
  confirmDeleteNote(note: ClientNote): void {
    if (this.notesDeletingId() !== null) {
      return;
    }

    this.selectedNote.set(note);
  }

  cancelDeleteNote(): void {
    if (this.notesDeletingId() === null) {
      this.selectedNote.set(null);
    }
  }

  deleteNote(): void {
    const note = this.selectedNote();
    const currentClient = this.client();

    if (
      !note ||
      !currentClient ||
      note.clientId !== currentClient.id ||
      this.notesDeletingId() !== null
    ) {
      return;
    }

    this.notesDeletingId.set(note.id);
    this.notesError.set('');

    this.clientNoteService.deleteNote(note.id).subscribe({
      next: () => {
        this.notesDeletingId.set(null);
        this.selectedNote.set(null);

        this.loadClientNotes(currentClient.id);
      },
      error: () => {
        this.notesDeletingId.set(null);
        this.selectedNote.set(null);

        this.notesError.set(
          'Unable to delete note. Please try again.'
        );
      }
    });
  }





  openDeleteConfirmation(): void {
    this.showDeleteConfirmation.set(true);
  }

  closeDeleteConfirmation(): void {
    if (!this.deleting()) {
      this.showDeleteConfirmation.set(false);
    }
  }

  deleteClient(): void {
    const selectedClient = this.client();

    if (!selectedClient || this.deleting()) {
      return;
    }

    this.deleting.set(true);
    this.errorMessage.set('');

    this.clientService.deleteClient(selectedClient.id).subscribe({
      next: () => {
        this.deleting.set(false);
        this.showDeleteConfirmation.set(false);
        this.router.navigateByUrl('/clients');
      },
      error: (error: HttpErrorResponse) => {
        this.deleting.set(false);
        this.showDeleteConfirmation.set(false);

        this.errorMessage.set(
          error.status === 404
            ? 'This client no longer exists.'
            : 'Failed to delete the client. Please try again.'
        );
      }
    });
  }
}
