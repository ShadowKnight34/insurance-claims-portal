import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ClaimsApiService } from '../core/claims-api.service';
import { readError } from '../core/api-error';
import { OfficerWorkload } from '../core/models';

@Component({
  selector: 'app-workload',
  imports: [RouterLink],
  templateUrl: './workload.component.html'
})
export class WorkloadComponent {
  private readonly api = inject(ClaimsApiService);
  readonly rows = signal<OfficerWorkload[]>([]);
  readonly error = signal('');

  constructor() {
    this.api.dashboard().subscribe({
      next: data => this.rows.set(data.officerWorkload),
      error: (error: unknown) => this.error.set(readError(error))
    });
  }

  maxOpen(): number {
    return Math.max(1, ...this.rows().map(row => row.openClaims));
  }
}
