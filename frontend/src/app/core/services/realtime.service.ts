
import { Injectable, inject, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import { RealtimeEvent } from '../models/real-time-event.model';

@Injectable({
  providedIn: 'root'
})
export class RealtimeService {
  private readonly authService = inject(AuthService);

  private connection: signalR.HubConnection | null = null;
  private startPromise: Promise<void> | null = null;

  readonly connected = signal(false);

  private readonly clientChangedSubject =
    new Subject<RealtimeEvent>();

  private readonly followUpChangedSubject =
    new Subject<RealtimeEvent>();

  private readonly noteChangedSubject =
    new Subject<RealtimeEvent>();

  private readonly reconnectedSubject =
    new Subject<void>();

  readonly clientChanged$ =
    this.clientChangedSubject.asObservable();

  readonly followUpChanged$ =
    this.followUpChangedSubject.asObservable();

  readonly noteChanged$ =
    this.noteChangedSubject.asObservable();

  readonly reconnected$ =
    this.reconnectedSubject.asObservable();

  async start(): Promise<void> {
    if (
      !this.authService.isAuthenticated() ||
      this.authService.mustChangePassword()
    ) {
      return;
    }

    if (this.startPromise) {
      return this.startPromise;
    }

    if (this.connection) {
      return;
    }

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(environment.hubUrl, {
        accessTokenFactory: () =>
          this.authService.getAccessToken() ?? ''
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.connection = connection;

    // Client events
    connection.on('ClientChanged', (event: RealtimeEvent) => {
      this.clientChangedSubject.next(event);
    });

    // Follow-up events
    connection.on('FollowUpChanged', (event: RealtimeEvent) => {
      this.followUpChangedSubject.next(event);
    });

    // Note events
    connection.on('NoteChanged', (event: RealtimeEvent) => {
      this.noteChangedSubject.next(event);
    });

    connection.onreconnecting(() => {
      if (this.connection === connection) {
        this.connected.set(false);
      }
    });

    connection.onreconnected(() => {
      if (this.connection === connection) {
        this.connected.set(true);

        // Components can reload missed changes
        this.reconnectedSubject.next();
      }
    });

    connection.onclose(() => {
      if (this.connection === connection) {
        this.connected.set(false);
      }
    });

    const attempt = connection.start()
      .then(() => {
        if (this.connection === connection) {
          this.connected.set(true);
        }
      })
      .catch(error => {
        if (this.connection === connection) {
          this.connection = null;
          this.connected.set(false);
        }

        console.error('SignalR connection failed:', error);
      })
      .finally(() => {
        if (this.startPromise === attempt) {
          this.startPromise = null;
        }
      });

    this.startPromise = attempt;

    return attempt;
  }

  async stop(): Promise<void> {
    const connection = this.connection;

    this.connection = null;
    this.connected.set(false);

    if (connection) {
      await connection.stop();
    }
  }
}
