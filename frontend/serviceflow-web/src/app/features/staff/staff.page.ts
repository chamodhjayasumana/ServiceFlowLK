import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-staff-page',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <section>
      <h1>Staff invitations</h1>
      <p>Owners and managers can invite employees. Owner role cannot be invited.</p>

      <form [formGroup]="form" (ngSubmit)="submit()">
        <label>
          Email
          <input type="email" formControlName="email" />
        </label>
        <label>
          Role
          <select formControlName="roleName">
            <option value="Manager">Manager</option>
            <option value="Receptionist">Receptionist</option>
            <option value="Technician">Technician</option>
          </select>
        </label>
        <button type="submit" [disabled]="form.invalid || busy()">Send invitation</button>
      </form>

      @if (error()) {
        <p class="error">{{ error() }}</p>
      }

      @if (token()) {
        <div class="token-box">
          <p>Share this one-time invitation token securely (not by storing passwords):</p>
          <code>{{ token() }}</code>
        </div>
      }
    </section>
  `,
  styles: `
    form {
      display: grid;
      gap: 0.75rem;
      max-width: 24rem;
      margin: 1rem 0;
    }

    label {
      display: grid;
      gap: 0.35rem;
    }

    input,
    select,
    button {
      font: inherit;
      padding: 0.6rem 0.75rem;
    }

    button {
      background: #0b6e4f;
      color: #fff;
      border: 0;
      border-radius: 0.35rem;
    }

    .error {
      color: #9b1c1c;
    }

    .token-box {
      margin-top: 1rem;
      padding: 1rem;
      background: rgba(11, 110, 79, 0.08);
      border-radius: 0.35rem;
      word-break: break-all;
    }
  `
})
export class StaffPage {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);

  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly token = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    roleName: ['Technician', Validators.required]
  });

  async submit(): Promise<void> {
    if (this.form.invalid) {
      return;
    }

    this.busy.set(true);
    this.error.set(null);
    this.token.set(null);

    try {
      const result = await this.auth.invite(this.form.getRawValue());
      this.token.set(result.invitationToken);
      this.form.patchValue({ email: '' });
    } catch {
      this.error.set('Unable to create invitation. Check permissions and email uniqueness.');
    } finally {
      this.busy.set(false);
    }
  }
}
