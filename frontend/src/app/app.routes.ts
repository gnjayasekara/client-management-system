
import { Routes } from '@angular/router';

import { Login } from './features/auth/login/login';
import { Dashboard } from './features/dashboard/dashboard';
import { ClientList } from './features/clients/client-list/client-list';
import { FollowUpList } from './features/follow-ups/follow-up-list/follow-up-list';
import { MainLayout } from './layout/main-layout/main-layout';
import { authGuard } from './core/guards/auth.guard';

import { ClientForm } from './features/clients/client-form/client-form';

export const routes: Routes = [
  {
    path: 'login',
    component: Login
  },
  {
    path: '',
    component: MainLayout,
    canActivate: [authGuard],
    children: [
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full'
      },
      {
        path: 'dashboard',
        component: Dashboard
      },
      {
        path: 'clients',
        component: ClientList
      },
      {
        path: 'clients/new',
        component: ClientForm
      },
      {
        path: 'clients/:id/edit',
        component: ClientForm
      },
      {
        path: 'follow-ups',
        component: FollowUpList
      }
    ]
  },
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];
