
import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { ClientService } from '../../../core/services/client';
import { Client } from '../../../core/models/client.model';

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

  readonly client = signal<Client | null>(null);
  readonly loading = signal(true);
  readonly deleting = signal(false);
  readonly showDeleteConfirmation = signal(false);
  readonly errorMessage = signal('');

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    const id = Number(idParam);

    if (!idParam || !Number.isInteger(id) || id <= 0) {
      this.loading.set(false);
      this.errorMessage.set('Invalid client ID.');
      return;
    }

    this.loadClient(id);
  }

  loadClient(id: number): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.clientService.getClientById(id).subscribe({
      next: (response) => {
        this.client.set(response);
        this.loading.set(false);
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
