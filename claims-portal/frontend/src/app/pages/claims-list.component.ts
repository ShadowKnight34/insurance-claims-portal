import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ClaimsApiService } from '../core/claims-api.service';
import { AuthService } from '../core/auth.service';
import { readError } from '../core/api-error';
import { money, shortDate, statusClass, statusLabel } from '../core/labels';
import { ClaimSummary } from '../core/models';

@Component({
  selector: 'app-claims-list',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './claims-list.component.html'
})
export class ClaimsListComponent {
  private readonly api = inject(ClaimsApiService);
  private readonly formBuilder = inject(FormBuilder);
  readonly auth = inject(AuthService);
  readonly money = money;
  readonly shortDate = shortDate;
  readonly statusClass = statusClass;
  readonly statusLabel = statusLabel;

  readonly items = signal<ClaimSummary[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly pageSize = 8;
  readonly error = signal('');
  readonly loading = signal(false);

  readonly filters = this.formBuilder.nonNullable.group({
    search: '',
    status: '',
    claimType: '',
    queue: ''
  });

  constructor() {
    this.load();
  }

  detailLink(id: string): string[] {
    return this.auth.isOfficer() ? ['/officer/claims', id] : ['/claimant/claims', id];
  }

  apply(): void {
    this.page.set(1);
    this.load();
  }

  clear(): void {
    this.filters.reset({ search: '', status: '', claimType: '', queue: '' });
    this.apply();
  }

  previous(): void {
    if (this.page() > 1) {
      this.page.update(value => value - 1);
      this.load();
    }
  }

  next(): void {
    if (this.page() * this.pageSize < this.total()) {
      this.page.update(value => value + 1);
      this.load();
    }
  }

  private load(): void {
    const value = this.filters.getRawValue();
    this.loading.set(true);
    this.error.set('');
    this.api.listClaims({
      page: this.page(),
      pageSize: this.pageSize,
      search: value.search.trim() || undefined,
      status: value.status || undefined,
      claimType: value.claimType || undefined,
      assignedToMe: value.queue === 'mine',
      unassignedOnly: value.queue === 'unassigned'
    }).subscribe({
      next: result => {
        this.items.set(result.items);
        this.total.set(result.totalCount);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.error.set(readError(error));
        this.loading.set(false);
      }
    });
  }
}
