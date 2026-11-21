import { Component, inject } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  template: `
    <section>
      <h1>Dashboard</h1>
      @if (auth.currentUser(); as user) {
        <p>Signed in as {{ user.displayName }} ({{ user.email }})</p>
        <p>{{ user.tenantName }} · {{ user.roles.join(', ') }}</p>
      }
      <p>Owner metrics and workload views will be implemented in Phase 7.</p>
    </section>
  `
})
export class DashboardPage {
  readonly auth = inject(AuthService);
}
