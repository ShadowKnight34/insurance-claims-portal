import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { LoginResponse, UserProfile } from './models';

const TOKEN_KEY = 'claimsportal.token';
const USER_KEY = 'claimsportal.user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly userState = signal<UserProfile | null>(readStoredUser());

  readonly user = this.userState.asReadonly();
  readonly token = signal<string | null>(localStorage.getItem(TOKEN_KEY));
  readonly isOfficer = computed(() => this.userState()?.role === 'ClaimsOfficer');
  readonly isClaimant = computed(() => this.userState()?.role === 'Claimant');

  isLoggedIn(): boolean {
    return !!this.token() && !!this.userState();
  }

  login(email: string, password: string) {
    return this.http.post<LoginResponse>(`${environment.apiUrl}/api/auth/login`, { email, password }).pipe(
      tap(response => this.persist(response))
    );
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this.token.set(null);
    this.userState.set(null);
  }

  home(): string {
    return this.isOfficer() ? '/officer/dashboard' : '/claimant/dashboard';
  }

  private persist(response: LoginResponse): void {
    localStorage.setItem(TOKEN_KEY, response.accessToken);
    localStorage.setItem(USER_KEY, JSON.stringify(response.user));
    this.token.set(response.accessToken);
    this.userState.set(response.user);
  }
}

function readStoredUser(): UserProfile | null {
  const raw = localStorage.getItem(USER_KEY);
  if (!raw) {
    return null;
  }

  try {
    return JSON.parse(raw) as UserProfile;
  } catch {
    return null;
  }
}
