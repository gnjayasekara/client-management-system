
import { Component, OnInit, inject } from '@angular/core';
import { ApiHealthService } from '../../core/services/api-health';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-dashboard',
  imports: [],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class Dashboard implements OnInit {
  private readonly apiHealth = inject(ApiHealthService);

  private readonly authService = inject(AuthService);

    logout(): void {
      this.authService.logout();
    }

  apiStatus = 'Checking backend connection...';

  ngOnInit(): void {
    this.apiHealth.checkHealth().subscribe({
      next: (response) => {
        this.apiStatus = response.status;
      },
      error: () => {
        this.apiStatus = 'Connection failed';
      }
    });
  }
}
