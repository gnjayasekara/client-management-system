
import { Routes } from '@angular/router';

import { Login } from './features/auth/login/login';
import { Dashboard } from './features/dashboard/dashboard';
import { ClientList } from './features/clients/client-list/client-list';
import { FollowUpList } from './features/follow-ups/follow-up-list/follow-up-list';
import { MainLayout } from './layout/main-layout/main-layout';
import { authGuard } from './core/guards/auth.guard';

import { ClientForm } from './features/clients/client-form/client-form';
import { ClientDetails } from './features/clients/client-details/client-details';

import { FollowUpForm } from './features/follow-ups/follow-up-form/follow-up-form';


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
        path: 'clients/:id',
        component: ClientDetails
      },
      {
        path: 'follow-ups',
        component: FollowUpList
      },
      {
        path: 'follow-ups/new',
        component: FollowUpForm
      },
      {
        path: 'follow-ups/:id/edit',
        component: FollowUpForm
      }
    ]
  },
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];
