
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  ClientNote,
  CreateClientNoteRequest
} from '../models/client-note.model';

@Injectable({
  providedIn: 'root'
})
export class ClientNoteService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  getNotes(clientId: number): Observable<ClientNote[]> {
    return this.http.get<ClientNote[]>(
      `${this.apiUrl}/clients/${clientId}/notes`
    );
  }

  addNote(
    clientId: number,
    request: CreateClientNoteRequest
  ): Observable<ClientNote> {
    return this.http.post<ClientNote>(
      `${this.apiUrl}/clients/${clientId}/notes`,
      request
    );
  }

  deleteNote(noteId: number): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/notes/${noteId}`
    );
  }
}
