import { TestBed, ComponentFixture } from '@angular/core/testing';
import { provideRouter, Router, ActivatedRoute, convertToParamMap } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { ClientForm } from './client-form';
import { ClientService } from '../../../core/services/client';
import { Client } from '../../../core/models/client.model';

describe('ClientForm Component', () => {
  let component: ClientForm;
  let fixture: ComponentFixture<ClientForm>;
  let clientServiceSpy: {
    getClientById: ReturnType<typeof vi.fn>;
    createClient: ReturnType<typeof vi.fn>;
    updateClient: ReturnType<typeof vi.fn>;
  };
  let router: Router;

  const mockClient: Client = {
    id: 10,
    name: 'Tech Innovators',
    company: 'Tech Corp',
    email: 'info@techcorp.com',
    phone: '555-1234',
    address: '100 Innovation Way',
    isActive: true,
    createdAt: '2026-01-01T00:00:00Z'
  };

  const setupComponent = async (routeParams: { [key: string]: string } = {}) => {
    clientServiceSpy = {
      getClientById: vi.fn(),
      createClient: vi.fn(),
      updateClient: vi.fn()
    };

    await TestBed.configureTestingModule({
      imports: [ClientForm],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: convertToParamMap(routeParams)
            }
          }
        },
        { provide: ClientService, useValue: clientServiceSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ClientForm);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
  };

  describe('Create Mode', () => {
    beforeEach(async () => {
      await setupComponent();
      fixture.detectChanges();
    });

    it('should initialize in create mode with empty form', () => {
      expect(component).toBeTruthy();
      expect(component.isEditMode()).toBe(false);
      expect(component.client.name).toBe('');
      expect(component.client.isActive).toBe(true);
    });

    it('should not submit if name is empty', () => {
      component.client.name = '   ';
      component.onSubmit();

      expect(clientServiceSpy.createClient).not.toHaveBeenCalled();
    });

    it('should create client and navigate to /clients on valid submission', () => {
      const navigateSpy = vi.spyOn(router, 'navigateByUrl');
      clientServiceSpy.createClient.mockReturnValue(of(mockClient));

      component.client = {
        name: 'New Client Inc',
        company: 'New Corp',
        email: 'hello@newclient.com',
        phone: '123456789',
        address: '456 Avenue',
        isActive: true
      };

      component.onSubmit();

      expect(clientServiceSpy.createClient).toHaveBeenCalledWith({
        name: 'New Client Inc',
        company: 'New Corp',
        email: 'hello@newclient.com',
        phone: '123456789',
        address: '456 Avenue',
        isActive: true
      });
      expect(component.saving()).toBe(false);
      expect(navigateSpy).toHaveBeenCalledWith('/clients');
    });

    it('should set appropriate error message when server returns 400', () => {
      const error = new HttpErrorResponse({ status: 400 });
      clientServiceSpy.createClient.mockReturnValue(throwError(() => error));

      component.client.name = 'Valid Name';
      component.onSubmit();

      expect(component.saving()).toBe(false);
      expect(component.errorMessage()).toBe('Please check the form fields and try again.');
    });
  });

  describe('Edit Mode', () => {
    beforeEach(async () => {
      await setupComponent({ id: '10' });
    });

    it('should fetch client data on init when id parameter is present', () => {
      clientServiceSpy.getClientById.mockReturnValue(of(mockClient));

      fixture.detectChanges();

      expect(component.isEditMode()).toBe(true);
      expect(clientServiceSpy.getClientById).toHaveBeenCalledWith(10);
      expect(component.client.name).toBe('Tech Innovators');
      expect(component.client.company).toBe('Tech Corp');
    });

    it('should update client and navigate to /clients on submit in edit mode', () => {
      const navigateSpy = vi.spyOn(router, 'navigateByUrl');
      clientServiceSpy.getClientById.mockReturnValue(of(mockClient));
      clientServiceSpy.updateClient.mockReturnValue(of(undefined));

      fixture.detectChanges();

      component.client.name = 'Updated Tech Innovators';
      component.onSubmit();

      expect(clientServiceSpy.updateClient).toHaveBeenCalledWith(
        10,
        expect.objectContaining({
          name: 'Updated Tech Innovators'
        })
      );
      expect(navigateSpy).toHaveBeenCalledWith('/clients');
    });

    it('should show error when client fails to load', () => {
      const error = new HttpErrorResponse({ status: 404 });
      clientServiceSpy.getClientById.mockReturnValue(throwError(() => error));

      fixture.detectChanges();

      expect(component.loading()).toBe(false);
      expect(component.errorMessage()).toBe('Client not found.');
    });
  });
});
