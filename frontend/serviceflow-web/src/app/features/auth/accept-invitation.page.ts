import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-accept-invitation-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <section class="auth-card">
      <h1>Accept invitation</h1>
      <p>Set your display name and password. No password is sent by email.</p>

      <form [formGroup]="form" (ngSubmit)="submit()">
        <label>
          Invitation token
          <input formControlName="token" />
        </label>
        <label>
          Display name
          <input formControlName="displayName" />
        </label>
        <label>
          Password
          <input type="password" formControlName="password" autocomplete="new-password" />
        </label>

        @if (error()) {
          <p class="error" role="alert">{{ error() }}</p>
        }

        <button type="submit" [disabled]="form.invalid || busy()">Create account</button>
      </form>

      <p class="footer"><a routerLink="/login">Back to sign in</a></p>
    </section>
  `,
  styleUrl: './auth-pages.scss'
})
export class AcceptInvitationPage {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly busy = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    token: [this.route.snapshot.queryParamMap.get('token') ?? '', Validators.required],
    displayName: ['', Validators.required],
    password: ['', [Validators.required, Validators.minLength(10)]]
  });

  async submit(): Promise<void> {
    if (this.form.invalid) {
      return;
    }

    this.busy.set(true);
    this.error.set(null);

    try {
      await this.auth.acceptInvitation(this.form.getRawValue());
      await this.router.navigateByUrl('/login');
    } catch {
      this.error.set('Invitation is invalid, expired, or already used.');
    } finally {
      this.busy.set(false);
    }
  }
}
