// Angular route definitions for the Claims Portal Claims Management UI
import { Routes } from '@angular/router';
import { LoginComponent } from './pages/login.component';
import { HomeRedirectComponent } from './pages/home-redirect.component';
import { ClaimantDashboardComponent } from './pages/claimant-dashboard.component';
import { OfficerDashboardComponent } from './pages/officer-dashboard.component';
import { ClaimsListComponent } from './pages/claims-list.component';
import { SubmitClaimComponent } from './pages/submit-claim.component';
import { authGuard } from './core/auth.guard';

export const routes: Routes = [
  // default entry – decides where to go based on authentication/role
  { path: '', component: HomeRedirectComponent },
  // explicit login page (public)
  { path: 'login', component: LoginComponent },
  // dashboards (protected)
  { path: 'claimant/dashboard', component: ClaimantDashboardComponent, canActivate: [authGuard], data: { role: 'Claimant' } },
  { path: 'officer/dashboard', component: OfficerDashboardComponent, canActivate: [authGuard], data: { role: 'ClaimsOfficer' } },
  // claim management pages (protected – both roles can view)
  { path: 'claims', component: ClaimsListComponent, canActivate: [authGuard] },
  { path: 'claims/submit', component: SubmitClaimComponent, canActivate: [authGuard] },
  // fallback – any unknown URL goes to login
  { path: '**', redirectTo: 'login' },
];
