import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-register-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <section class="auth-card">
      <h1>Register your shop</h1>
      <p>Create a tenant and owner account. The owner role is assigned server-side.</p>

      <form [formGroup]="form" (ngSubmit)="submit()">
        <label>
          Business name
          <input formControlName="businessName" />
        </label>
        <label>
          Business slug
          <input formControlName="slug" placeholder="colombo-tech" />
        </label>
        <label>
          Owner name
          <input formControlName="ownerDisplayName" />
        </label>
        <label>
          Owner email
          <input type="email" formControlName="ownerEmail" autocomplete="username" />
        </label>
        <label>
          Owner password
          <input type="password" formControlName="ownerPassword" autocomplete="new-password" />
        </label>

        @if (error()) {
          <p class="error" role="alert">{{ error() }}</p>
        }
        @if (success()) {
          <p class="success" role="status">{{ success() }}</p>
        }

        <button type="submit" [disabled]="form.invalid || busy()">Create business</button>
      </form>

      <p class="footer">
        Already registered?
        <a routerLink="/login">Sign in</a>
      </p>
    </section>
  `,
  styleUrl: './auth-pages.scss'
})
export class RegisterPage {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly success = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    businessName: ['', [Validators.required, Validators.maxLength(200)]],
    slug: ['', [Validators.required, Validators.pattern(/^[a-z0-9]+(?:-[a-z0-9]+)*$/)]],
    ownerDisplayName: ['', Validators.required],
    ownerEmail: ['', [Validators.required, Validators.email]],
    ownerPassword: ['', [Validators.required, Validators.minLength(10)]]
  });

  async submit(): Promise<void> {
    if (this.form.invalid) {
      return;
    }

    this.busy.set(true);
    this.error.set(null);
    this.success.set(null);

    try {
      const value = this.form.getRawValue();
      await this.auth.register({
        businessName: value.businessName,
        slug: value.slug,
        ownerDisplayName: value.ownerDisplayName,
        ownerEmail: value.ownerEmail,
        ownerPassword: value.ownerPassword,
        contactEmail: value.ownerEmail
      });
      this.success.set('Business created. You can sign in now.');
      await this.router.navigateByUrl('/login');
    } catch {
      this.error.set('Registration failed. Check slug/email uniqueness and password rules.');
    } finally {
      this.busy.set(false);
    }
  }
}
