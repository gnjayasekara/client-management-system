
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  FollowUp,
  CreateFollowUpRequest,
  UpdateFollowUpRequest
} from '../models/follow-up.model';

@Injectable({
  providedIn: 'root'
})
export class FollowUpService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/followups`;

  getFollowUps(
    clientId?: number | null,
    isCompleted?: boolean | null
  ): Observable<FollowUp[]> {
    let params = new HttpParams();

    if (clientId !== null && clientId !== undefined) {
      params = params.set('clientId', String(clientId));
    }

    if (isCompleted !== null && isCompleted !== undefined) {
      params = params.set('isCompleted', String(isCompleted));
    }

    return this.http.get<FollowUp[]>(this.apiUrl, { params });
  }

  getFollowUpById(id: number): Observable<FollowUp> {
    return this.http.get<FollowUp>(`${this.apiUrl}/${id}`);
  }

  createFollowUp(
    request: CreateFollowUpRequest
  ): Observable<FollowUp> {
    return this.http.post<FollowUp>(this.apiUrl, request);
  }

  updateFollowUp(
    id: number,
    request: UpdateFollowUpRequest
  ): Observable<void> {
    return this.http.put<void>(
      `${this.apiUrl}/${id}`,
      request
    );
  }

  completeFollowUp(id: number): Observable<void> {
    return this.http.patch<void>(
      `${this.apiUrl}/${id}/complete`,
      {}
    );
  }

  deleteFollowUp(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
