
import { Component, OnInit, inject, signal, DestroyRef } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';

import { DashboardService } from '../../core/services/dashboard';
import { DashboardStats } from '../../core/models/dashboard.model';

import { RealtimeService } from '../../core/services/realtime.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-dashboard',
  imports: [DatePipe, RouterLink],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class Dashboard implements OnInit {
  private readonly dashboardService = inject(DashboardService);

  readonly stats = signal<DashboardStats | null>(null);
  readonly loading = signal(true);
  readonly errorMessage = signal('');
  private readonly realtimeService = inject(RealtimeService);
  private readonly destroyRef = inject(DestroyRef);

  ngOnInit(): void {
    this.loadDashboard();

  this.realtimeService.clientChanged$
    .pipe(takeUntilDestroyed(this.destroyRef))
    .subscribe(() => this.loadDashboard());

  this.realtimeService.followUpChanged$
    .pipe(takeUntilDestroyed(this.destroyRef))
    .subscribe(() => this.loadDashboard());

  this.realtimeService.reconnected$
    .pipe(takeUntilDestroyed(this.destroyRef))
    .subscribe(() => this.loadDashboard());

  }

  loadDashboard(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.dashboardService.getStats().subscribe({
      next: (response) => {
        this.stats.set(response);
        this.loading.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.loading.set(false);

        if (error.status === 0) {
          this.errorMessage.set(
            'Unable to connect to the server.'
          );
        } else {
          this.errorMessage.set(
            'Failed to load dashboard statistics.'
          );
        }
      }
    });
  }
}
