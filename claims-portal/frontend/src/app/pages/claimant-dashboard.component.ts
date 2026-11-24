import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ClaimsApiService } from '../core/claims-api.service';
import { readError } from '../core/api-error';
import { money, shortDate, statusClass, statusLabel } from '../core/labels';
import { ClaimSummary } from '../core/models';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-claimant-dashboard',
  styleUrls: ['./claimant-dashboard.component.css'],
  imports: [RouterLink],
  templateUrl: './claimant-dashboard.component.html'
})
export class ClaimantDashboardComponent {
  private readonly api = inject(ClaimsApiService);
  readonly auth = inject(AuthService);
  readonly money = money;
  readonly shortDate = shortDate;
  readonly statusClass = statusClass;
  readonly statusLabel = statusLabel;

  readonly claims = signal<ClaimSummary[]>([]);
  readonly error = signal('');
  readonly loading = signal(true);

  constructor() {
    this.api.listClaims({ page: 1, pageSize: 100 }).subscribe({
      next: page => {
        this.claims.set(page.items);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.error.set(readError(error));
        this.loading.set(false);
      }
    });
  }

  count(status: string): number {
    return this.claims().filter(claim => claim.status === status).length;
  }
}
