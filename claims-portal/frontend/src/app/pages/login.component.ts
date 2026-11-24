import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { readError } from '../core/api-error';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly error = signal('');
  readonly submitting = signal(false);

  readonly form = this.formBuilder.nonNullable.group({
    email: ['claimant@claims.demo', [Validators.required, Validators.email]],
    password: ['Claimant123!', [Validators.required, Validators.minLength(8)]]
  });

  constructor() {
    if (this.auth.isLoggedIn()) {
      void this.router.navigateByUrl(this.auth.home());
    }
  }

  useClaimant(): void {
    this.form.setValue({ email: 'claimant@claims.demo', password: 'Claimant123!' });
  }

  useOfficer(): void {
    this.form.setValue({ email: 'officer@claims.demo', password: 'Officer123!' });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.error.set('');
    const { email, password } = this.form.getRawValue();
    this.auth.login(email, password).subscribe({
      next: () => {
        this.submitting.set(false);
        void this.router.navigateByUrl(this.auth.home());
      },
      error: (error: unknown) => {
        this.submitting.set(false);
        this.error.set(readError(error));
      }
    });
  }
}
