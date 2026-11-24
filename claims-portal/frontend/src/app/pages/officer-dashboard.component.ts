import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ClaimsApiService } from '../core/claims-api.service';
import { readError } from '../core/api-error';
import { money, statusClass, statusLabel } from '../core/labels';
import { Dashboard } from '../core/models';

@Component({
  selector: 'app-officer-dashboard',
  styleUrls: ['./officer-dashboard.component.css'],
  templateUrl: './officer-dashboard.component.html',
  imports: [RouterLink]
})
export class OfficerDashboardComponent {
  private readonly api = inject(ClaimsApiService);
  readonly money = money;
  readonly statusClass = statusClass;
  readonly statusLabel = statusLabel;

  readonly dashboard = signal<Dashboard | null>(null);
  readonly error = signal('');

  constructor() {
    this.api.dashboard().subscribe({
      next: data => this.dashboard.set(data),
      error: (error: unknown) => this.error.set(readError(error))
    });
  }
}
