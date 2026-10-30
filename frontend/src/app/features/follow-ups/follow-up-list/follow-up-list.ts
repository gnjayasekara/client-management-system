
import {
  Component,
  OnInit,
  OnDestroy,
  inject,
  signal,
  DestroyRef
} from '@angular/core';

import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Subscription, finalize } from 'rxjs';

import { FollowUpService } from '../../../core/services/follow-up';
import { FollowUp } from '../../../core/models/follow-up.model';
import { RouterLink } from '@angular/router';

import { AuthService } from '../../../core/services/auth.service';
import { RealtimeService } from '../../../core/services/realtime.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { ClientService } from '../../../core/services/client';
import { Client } from '../../../core/models/client.model';

@Component({
  selector: 'app-follow-up-list',
  imports: [FormsModule, DatePipe, RouterLink],
  templateUrl: './follow-up-list.html',
  styleUrl: './follow-up-list.css'
})
export class FollowUpList implements OnInit, OnDestroy {
  private readonly followUpService = inject(FollowUpService);
  private request?: Subscription;

  readonly followUps = signal<FollowUp[]>([]);
  readonly loading = signal(false);
  readonly errorMessage = signal('');
  readonly filterError = signal('');

  
  readonly actionLoadingId = signal<number | null>(null);
  readonly showDeleteConfirmation = signal(false);
  readonly selectedFollowUp = signal<FollowUp | null>(null);
  readonly actionError = signal('');

  readonly authService = inject(AuthService);

  
  private readonly realtimeService = inject(RealtimeService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly clientService = inject(ClientService);

  readonly clients = signal<Client[]>([]);
  readonly clientLoading = signal(false);
  readonly clientError = signal('');

  
  readonly allFollowUps = signal<FollowUp[]>([]);

  clientSearch = '';

  get filteredClients(): Client[] {
    const term = this.clientSearch.trim().toLowerCase();

    return this.clients().filter(client =>
      client.name.toLowerCase().includes(term)
    );
  }


  statusFilter = 'all';

  ngOnInit(): void {
    this.loadClients();
    this.loadFollowUps();

    this.realtimeService.followUpChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.loadFollowUps());

    this.realtimeService.clientChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.loadClients();
        this.loadFollowUps();
      });

    this.realtimeService.reconnected$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.loadClients();
        this.loadFollowUps();
      });
  }

  ngOnDestroy(): void {
    this.request?.unsubscribe();
  }

  loadClients(): void {
    this.clientLoading.set(true);
    this.clientError.set('');

    this.clientService.getClients().subscribe({
      next: clients => {
        this.clients.set(clients);
        this.clientLoading.set(false);
      },
      error: () => {
        this.clientLoading.set(false);
        this.clientError.set('Unable to load clients.');
      }
    });
  }

  
  loadFollowUps(): void {
    this.filterError.set('');

    this.request?.unsubscribe();

    this.loading.set(true);
    this.errorMessage.set('');

    // Capture the filters when Apply Filters is clicked.
    const searchTerm = this.clientSearch.trim().toLowerCase();
    const selectedStatus = this.statusFilter;

    // Load all FollowUps so name matching can happen in Angular.
    this.request = this.followUpService
      .getFollowUps(null, null)
      .pipe(
        finalize(() => this.loading.set(false))
      )
      .subscribe({
        next: (response: FollowUp[]) => {
          this.allFollowUps.set(response);

          const filteredResults = response.filter(followUp => {
            // Match full or partial client name.
            const matchesClient =
              !searchTerm ||
              (followUp.clientName ?? '')
                .toLowerCase()
                .includes(searchTerm);

            // Match selected completion status.
            const matchesStatus =
              selectedStatus === 'all' ||
              (selectedStatus === 'completed' &&
                followUp.isCompleted) ||
              (selectedStatus === 'pending' &&
                !followUp.isCompleted);

            return matchesClient && matchesStatus;
          });

          this.followUps.set(filteredResults);
        },
        error: (error: HttpErrorResponse) => {
          this.allFollowUps.set([]);
          this.followUps.set([]);

          this.errorMessage.set(
            error.status === 0
              ? 'Unable to connect to the backend server.'
              : 'Failed to load follow-ups. Please try again.'
          );
        }
      });
  }


  clearFilters(): void {
    this.clientSearch = '';
    this.statusFilter = 'all';

    this.loadFollowUps();
  }

  isOverdue(followUp: FollowUp): boolean {
    if (followUp.isCompleted) {
      return false;
    }

    // Compare calendar dates in the browser's local time.
    const dueDay = followUp.dueDate.substring(0, 10);

    const now = new Date();
    const today = [
      now.getFullYear(),
      String(now.getMonth() + 1).padStart(2, '0'),
      String(now.getDate()).padStart(2, '0')
    ].join('-');

    return dueDay < today;
  }

  
  completeFollowUp(followUp: FollowUp): void {
    if (followUp.isCompleted || this.actionLoadingId() !== null) {
      return;
    }

    this.actionLoadingId.set(followUp.id);
    this.actionError.set('');

    this.followUpService.completeFollowUp(followUp.id).subscribe({
      next: () => {
        this.actionLoadingId.set(null);
        this.loadFollowUps();
      },
      error: (error: HttpErrorResponse) => {
        this.actionLoadingId.set(null);

        this.actionError.set(
          error.status === 404
            ? 'Follow-up not found.'
            : 'Unable to complete follow-up. Please try again.'
        );
      }
    });
  }

  
  openDeleteConfirmation(followUp: FollowUp): void {
    if (
      !this.authService.hasPermission('followups.delete') ||
      this.actionLoadingId() !== null
    ) {
      return;
    }

    this.selectedFollowUp.set(followUp);
    this.showDeleteConfirmation.set(true);
    this.actionError.set('');
  }

  closeDeleteConfirmation(): void {
    if (this.actionLoadingId() !== null) {
      return;
    }

    this.showDeleteConfirmation.set(false);
    this.selectedFollowUp.set(null);
  }

  deleteFollowUp(): void {
    const followUp = this.selectedFollowUp();

    if (!followUp || this.actionLoadingId() !== null) {
      return;
    }

    this.actionLoadingId.set(followUp.id);
    this.actionError.set('');

    this.followUpService.deleteFollowUp(followUp.id).subscribe({
      next: () => {
        this.actionLoadingId.set(null);
        this.closeDeleteConfirmation();
        this.loadFollowUps();
      },
      error: (error: HttpErrorResponse) => {
        this.actionLoadingId.set(null);
        this.closeDeleteConfirmation();

        this.actionError.set(
          error.status === 404
            ? 'Follow-up no longer exists.'
            : 'Unable to delete follow-up. Please try again.'
        );
      }
    });
  }


}
