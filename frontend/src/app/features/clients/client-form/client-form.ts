
import {
  Component,
  OnInit,
  inject,
  signal
} from '@angular/core';

import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { ClientService } from '../../../core/services/client';
import { CreateClientRequest } from '../../../core/models/client.model';

import { Observable } from 'rxjs';


@Component({
  selector: 'app-client-form',
  imports: [FormsModule, RouterLink],
  templateUrl: './client-form.html',
  styleUrl: './client-form.css'
})
export class ClientForm implements OnInit {
  private readonly clientService = inject(ClientService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly errorMessage = signal('');
  readonly isEditMode = signal(false);

  private clientId: number | null = null;

  client: CreateClientRequest = {
    name: '',
    company: null,
    email: null,
    phone: null,
    address: null,
    isActive: true
  };

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');

    if (idParam !== null) {
      const id = Number(idParam);

      if (!Number.isInteger(id) || id <= 0) {
        this.errorMessage.set('Invalid client ID.');
        return;
      }

      this.clientId = id;
      this.isEditMode.set(true);
      this.loadClient(id);
    }
  }

  private loadClient(id: number): void {
    this.loading.set(true);

    this.clientService.getClientById(id).subscribe({
      next: (response) => {
        this.client = {
          name: response.name,
          company: response.company,
          email: response.email,
          phone: response.phone,
          address: response.address,
          isActive: response.isActive
        };

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

  onSubmit(): void {
    if (this.saving() || this.loading() ||
        !this.client.name.trim() ||
        (this.isEditMode() && this.clientId === null)) {
      return;
    }

    this.saving.set(true);
    this.errorMessage.set('');

    const request: CreateClientRequest = {
      name: this.client.name.trim(),
      company: this.client.company?.trim() || null,
      email: this.client.email?.trim() || null,
      phone: this.client.phone?.trim() || null,
      address: this.client.address?.trim() || null,
      isActive: this.client.isActive
    };

    const operation: Observable<unknown> = this.isEditMode()
      ? this.clientService.updateClient(this.clientId!, request)
      : this.clientService.createClient(request);

    operation.subscribe({
      next: () => {
        this.saving.set(false);
        this.router.navigateByUrl('/clients');
      },
      error: (error: HttpErrorResponse) => {
        this.saving.set(false);

        if (error.status === 400) {
          this.errorMessage.set(
            'Please check the form fields and try again.'
          );
        } else if (error.status === 404) {
          this.errorMessage.set('Client not found.');
        } else if (error.status === 0) {
          this.errorMessage.set(
            'Unable to connect to the backend server.'
          );
        } else {
          this.errorMessage.set(
            'Unable to save client. Please try again.'
          );
        }
      }
    });

  }
}
