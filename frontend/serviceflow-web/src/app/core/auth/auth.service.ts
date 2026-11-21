import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AcceptInvitationRequest,
  CurrentUser,
  InviteEmployeeRequest,
  InviteEmployeeResponse,
  LoginRequest,
  RegisterTenantRequest
} from './auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly api = environment.apiBaseUrl;

  private readonly currentUserSignal = signal<CurrentUser | null>(null);
  private readonly initializedSignal = signal(false);

  readonly currentUser = this.currentUserSignal.asReadonly();
  readonly initialized = this.initializedSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.currentUserSignal() !== null);

  async initialize(): Promise<void> {
    if (this.initializedSignal()) {
      return;
    }

    try {
      await this.refreshCsrf();
      const user = await firstValueFrom(this.http.get<CurrentUser>(`${this.api}/me`));
      this.currentUserSignal.set(user);
    } catch {
      this.currentUserSignal.set(null);
    } finally {
      this.initializedSignal.set(true);
    }
  }

  async refreshCsrf(): Promise<void> {
    await firstValueFrom(this.http.get<{ token: string }>(`${this.api}/auth/csrf`));
  }

  async register(request: RegisterTenantRequest): Promise<void> {
    await firstValueFrom(this.http.post(`${this.api}/tenants/register`, request));
  }

  async login(request: LoginRequest): Promise<CurrentUser> {
    await firstValueFrom(this.http.post(`${this.api}/auth/login`, request, { observe: 'response' }));
    await this.refreshCsrf();
    const user = await firstValueFrom(this.http.get<CurrentUser>(`${this.api}/me`));
    this.currentUserSignal.set(user);
    return user;
  }

  async logout(): Promise<void> {
    try {
      await this.refreshCsrf();
      await firstValueFrom(this.http.post(`${this.api}/auth/logout`, {}));
    } finally {
      this.currentUserSignal.set(null);
      await this.router.navigateByUrl('/login');
    }
  }

  async invite(request: InviteEmployeeRequest): Promise<InviteEmployeeResponse> {
    await this.refreshCsrf();
    return await firstValueFrom(
      this.http.post<InviteEmployeeResponse>(`${this.api}/users/invitations`, request)
    );
  }

  async acceptInvitation(request: AcceptInvitationRequest): Promise<void> {
    await firstValueFrom(this.http.post(`${this.api}/users/invitations/accept`, request));
  }

  hasRole(...roles: string[]): boolean {
    const user = this.currentUserSignal();
    if (!user) {
      return false;
    }

    return roles.some((role) => user.roles.includes(role));
  }
}
