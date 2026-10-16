import { Routes } from '@angular/router';

import { Login } from './features/auth/login/login';
import { Dashboard } from './features/dashboard/dashboard';
// import { ClientList } from './features/clients/client-list/client-list';
// import { FollowUpList } from './features/follow-ups/follow-up-list/follow-up-list';

import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full'
  },
  {
    path: 'login',
    component: Login
  },
  {
    path: 'dashboard',
    component: Dashboard,
    canActivate: [authGuard]
  },
//   {
//     path: 'clients',
//     component: ClientList
//   },
//   {
//     path: 'follow-ups',
//     component: FollowUpList
//   },
  {
    path: '**',
    redirectTo: 'login'
  }
];