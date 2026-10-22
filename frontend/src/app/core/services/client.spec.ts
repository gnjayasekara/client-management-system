import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { ClientService } from './client';
import { environment } from '../../../environments/environment';
import { Client, CreateClientRequest, UpdateClientRequest } from '../models/client.model';

describe('ClientService', () => {
  let service: ClientService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/clients`;

  const mockClients: Client[] = [
    {
      id: 1,
      name: 'Acme Corp',
      company: 'Acme',
      email: 'contact@acme.com',
      phone: '1234567890',
      address: '123 Street',
      isActive: true,
      createdAt: '2026-01-01T00:00:00Z'
    }
  ];

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(ClientService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should fetch clients with query parameters', () => {
    service.getClients('Acme', true).subscribe((clients) => {
      expect(clients).toEqual(mockClients);
    });

    const req = httpMock.expectOne((r) =>
      r.url === apiUrl &&
      r.params.get('search') === 'Acme' &&
      r.params.get('isActive') === 'true'
    );
    expect(req.request.method).toBe('GET');
    req.flush(mockClients);
  });

  it('should fetch client by ID', () => {
    service.getClientById(1).subscribe((client) => {
      expect(client).toEqual(mockClients[0]);
    });

    const req = httpMock.expectOne(`${apiUrl}/1`);
    expect(req.request.method).toBe('GET');
    req.flush(mockClients[0]);
  });

  it('should create client', () => {
    const newClientReq: CreateClientRequest = {
      name: 'Beta LLC',
      company: 'Beta',
      email: 'beta@example.com',
      phone: null,
      address: null,
      isActive: true
    };

    service.createClient(newClientReq).subscribe((client) => {
      expect(client.name).toBe('Beta LLC');
    });

    const req = httpMock.expectOne(apiUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(newClientReq);
    req.flush({ id: 2, ...newClientReq, createdAt: '2026-01-01' });
  });

  it('should update client', () => {
    const updateReq: UpdateClientRequest = {
      name: 'Beta Updated',
      company: null,
      email: null,
      phone: null,
      address: null,
      isActive: false
    };

    service.updateClient(2, updateReq).subscribe();

    const req = httpMock.expectOne(`${apiUrl}/2`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(updateReq);
    req.flush(null);
  });

  it('should delete client', () => {
    service.deleteClient(2).subscribe();

    const req = httpMock.expectOne(`${apiUrl}/2`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
