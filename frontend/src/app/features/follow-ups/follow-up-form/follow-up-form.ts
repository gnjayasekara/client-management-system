
import {
  Component,
  OnInit,
  inject,
  signal
} from '@angular/core';

import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable, forkJoin } from 'rxjs';

import { ClientService } from '../../../core/services/client';
import { FollowUpService } from '../../../core/services/follow-up';

import { Client } from '../../../core/models/client.model';
import {
  CreateFollowUpRequest,
  UpdateFollowUpRequest
} from '../../../core/models/follow-up.model';

@Component({
  selector: 'app-follow-up-form',
  imports: [FormsModule, RouterLink],
  templateUrl: './follow-up-form.html',
  styleUrl: './follow-up-form.css'
})
export class FollowUpForm implements OnInit {
  private readonly clientService = inject(ClientService);
  private readonly followUpService = inject(FollowUpService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly clients = signal<Client[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly errorMessage = signal('');
  readonly isEditMode = signal(false);

  private followUpId: number | null = null;

  form = {
    clientId: null as number | null,
    title: '',
    dueDate: '',
    isCompleted: false
  };

  
  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');

    // Edit mode
    if (idParam !== null) {
      const id = Number(idParam);

      if (!Number.isInteger(id) || id <= 0) {
        this.loading.set(false);
        this.errorMessage.set('Invalid follow-up ID.');
        return;
      }

      this.followUpId = id;
      this.isEditMode.set(true);
      this.loadEditData(id);
      return;
    }

    // Create mode
    const clientIdParam =
      this.route.snapshot.queryParamMap.get('clientId');

    if (clientIdParam !== null) {
      const clientId = Number(clientIdParam);

      if (Number.isInteger(clientId) && clientId > 0) {
        this.form.clientId = clientId;
      }
    }

    this.loadClients();
  }


  private loadClients(): void {
    this.loading.set(true);

    this.clientService.getClients().subscribe({
      next: clients => {
        this.clients.set(clients);
        this.loading.set(false);

        if (clients.length === 0) {
          this.errorMessage.set(
            'Create a client before adding a follow-up.'
          );
        }
      },
      error: () => {
        this.loading.set(false);
        this.errorMessage.set('Unable to load clients.');
      }
    });
  }

  

  private loadEditData(id: number): void {
    this.loading.set(true);

    forkJoin({
      clients: this.clientService.getClients(),
      followUp: this.followUpService.getFollowUpById(id)
    }).subscribe({
      next: ({ clients, followUp }) => {
        this.clients.set(clients);

        this.form = {
          clientId: followUp.clientId,
          title: followUp.title,
          dueDate: followUp.dueDate.substring(0, 10),
          isCompleted: followUp.isCompleted
        };

        this.loading.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.loading.set(false);
        this.errorMessage.set(
          error.status === 404
            ? 'Follow-up not found.'
            : 'Failed to load follow-up information.'
        );
      }
    });
  }

  onSubmit(): void {
    if (this.saving() || this.loading()) {
      return;
    }

    const clientId = this.form.clientId;

    if (
      clientId === null ||
      !Number.isInteger(clientId) ||
      clientId <= 0 ||
      !this.form.title.trim() ||
      !this.form.dueDate ||
      (this.isEditMode() && this.followUpId === null)
    ) {
      this.errorMessage.set('Please complete all required fields.');
      return;
    }

    this.saving.set(true);
    this.errorMessage.set('');

    let operation: Observable<unknown>;

    if (this.isEditMode()) {
      const request: UpdateFollowUpRequest = {
        clientId,
        title: this.form.title.trim(),
        dueDate: this.form.dueDate,
        isCompleted: this.form.isCompleted
      };

      operation = this.followUpService.updateFollowUp(
        this.followUpId!,
        request
      );
    } else {
      const request: CreateFollowUpRequest = {
        clientId,
        title: this.form.title.trim(),
        dueDate: this.form.dueDate
      };

      operation = this.followUpService.createFollowUp(request);
    }

    operation.subscribe({
      next: () => {
        this.saving.set(false);
        this.router.navigateByUrl('/follow-ups');
      },
      error: (error: HttpErrorResponse) => {
        this.saving.set(false);

        if (error.status === 400) {
          this.errorMessage.set(
            'Please check the form fields and try again.'
          );
        } else if (error.status === 404) {
          this.errorMessage.set(
            'The selected client or follow-up was not found.'
          );
        } else if (error.status === 0) {
          this.errorMessage.set(
            'Unable to connect to the backend server.'
          );
        } else {
          this.errorMessage.set(
            'Unable to save follow-up. Please try again.'
          );
        }
      }
    });
  }
}
