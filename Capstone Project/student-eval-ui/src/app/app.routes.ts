import { DashboardComponent } from './_library/components/dashboard-component/dashboard-component';

export const routes = [
  { path: 'upload', loadComponent: () => import('./_library/components/upload-component/upload-component').then(m => m.UploadComponent) },
  { path: 'dashboard', component: DashboardComponent },
  { path: '', redirectTo: 'upload', pathMatch: 'full' }
];
