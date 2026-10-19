
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  Client,
  CreateClientRequest,
  UpdateClientRequest
} from '../models/client.model';

@Injectable({
  providedIn: 'root'
})
export class ClientService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/clients`;

  getClients(
    search?: string,
    isActive?: boolean | null
  ): Observable<Client[]> {
    let params = new HttpParams();

    if (search?.trim()) {
      params = params.set('search', search.trim());
    }

    if (isActive !== null && isActive !== undefined) {
      params = params.set('isActive', String(isActive));
    }

    return this.http.get<Client[]>(this.apiUrl, { params });
  }

  getClientById(id: number): Observable<Client> {
    return this.http.get<Client>(`${this.apiUrl}/${id}`);
  }

  createClient(
    request: CreateClientRequest
  ): Observable<Client> {
    return this.http.post<Client>(this.apiUrl, request);
  }

  updateClient(
    id: number,
    request: UpdateClientRequest
  ): Observable<void> {
    return this.http.put<void>(
      `${this.apiUrl}/${id}`,
      request
    );
  }

  deleteClient(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
