
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

interface HealthResponse {
  status: string;
  application?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ApiHealthService {
  private readonly http = inject(HttpClient);

  checkHealth() {
    return this.http.get<HealthResponse>(
      `${environment.apiUrl}/health`
    );
  }
}
