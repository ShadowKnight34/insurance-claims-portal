import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ClaimsApiService } from '../core/claims-api.service';
import { readError } from '../core/api-error';

@Component({
  selector: 'app-submit-claim',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './submit-claim.component.html'
})
export class SubmitClaimComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly api = inject(ClaimsApiService);
  private readonly router = inject(Router);

  readonly error = signal('');
  readonly submitting = signal(false);
  readonly today = new Date().toISOString().slice(0, 10);

  readonly form = this.formBuilder.nonNullable.group({
    claimType: ['Motor', Validators.required],
    incidentDate: [this.today, Validators.required],
    incidentLocation: ['', [Validators.required, Validators.maxLength(200)]],
    incidentDescription: ['', [Validators.required, Validators.minLength(20), Validators.maxLength(4000)]],
    estimatedLossAmount: [0, [Validators.required, Validators.min(0.01), Validators.max(50000000)]]
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.error.set('');
    this.api.createClaim(this.form.getRawValue()).subscribe({
      next: claim => {
        this.submitting.set(false);
        void this.router.navigate(['/claimant/claims', claim.id]);
      },
      error: (error: unknown) => {
        this.submitting.set(false);
        this.error.set(readError(error));
      }
    });
  }
}
