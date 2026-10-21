
import { Component, OnInit, inject, signal, DestroyRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize, Subscription } from 'rxjs';
import { RouterLink } from '@angular/router';

import { ClientService } from '../../../core/services/client';
import { Client } from '../../../core/models/client.model';

import { AuthService } from '../../../core/services/auth.service';


import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RealtimeService } from '../../../core/services/realtime.service';

@Component({
  selector: 'app-client-list',
  imports: [FormsModule, DatePipe, RouterLink],
  templateUrl: './client-list.html',
  styleUrl: './client-list.css'
})
export class ClientList implements OnInit {
  private readonly clientService = inject(ClientService);
  private request?: Subscription;

  readonly clients = signal<Client[]>([]);
  readonly loading = signal(false);
  readonly errorMessage = signal('');

  readonly authService = inject(AuthService);

  
  private readonly realtimeService = inject(RealtimeService);
  private readonly destroyRef = inject(DestroyRef);


  search = '';
  statusFilter = 'all';

  ngOnInit(): void {
    this.loadClients();
    
  // Refresh clients when another user changes client data
  this.realtimeService.clientChanged$
    .pipe(takeUntilDestroyed(this.destroyRef))
    .subscribe(() => {
      this.loadClients();
    });

  // Reload after reconnection to catch missed changes
  this.realtimeService.reconnected$
    .pipe(takeUntilDestroyed(this.destroyRef))
    .subscribe(() => {
      this.loadClients();
    });

  }

  loadClients(): void {
    // Cancel an earlier request when filters change.
    this.request?.unsubscribe();

    this.loading.set(true);
    this.errorMessage.set('');

    let isActive: boolean | null = null;

    if (this.statusFilter === 'active') {
      isActive = true;
    } else if (this.statusFilter === 'inactive') {
      isActive = false;
    }

    this.request = this.clientService
      .getClients(this.search, isActive)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          this.clients.set(response);
        },
        error: (error: HttpErrorResponse) => {
          this.clients.set([]);

          if (error.status === 0) {
            this.errorMessage.set(
              'Unable to connect to the backend server.'
            );
          } else {
            this.errorMessage.set(
              'Failed to load clients. Please try again.'
            );
          }
        }
      });
  }

  clearFilters(): void {
    this.search = '';
    this.statusFilter = 'all';
    this.loadClients();
  }
}
