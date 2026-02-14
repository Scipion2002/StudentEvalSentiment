import { Routes } from '@angular/router';
import { DashboardComponent } from './_library/components/dashboard-component/dashboard-component';

export const routes = [
  { path: 'dashboard', component: DashboardComponent },
  {
    path: 'upload',
    loadComponent: () =>
      import('./_library/components/upload-component/upload-component')
        .then(m => m.UploadComponent),
  },
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
] satisfies Routes;