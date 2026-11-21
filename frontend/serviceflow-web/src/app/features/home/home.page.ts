import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-home-page',
  standalone: true,
  imports: [RouterLink],
  template: `
    <section class="home">
      <p class="eyebrow">Repair management SaaS</p>
      <h1>ServiceFlow LK</h1>
      <p class="lead">
        Multi-tenant repair management for Sri Lankan device-repair shops.
        Register your business or sign in to continue.
      </p>
      <nav class="actions">
        <a routerLink="/register">Register business</a>
        <a routerLink="/login">Sign in</a>
      </nav>
    </section>
  `,
  styles: `
    .home {
      max-width: 40rem;
    }

    .eyebrow {
      margin: 0 0 0.5rem;
      letter-spacing: 0.08em;
      text-transform: uppercase;
      font-size: 0.75rem;
      color: #5c6b73;
    }

    h1 {
      margin: 0 0 0.75rem;
      font-family: "Segoe UI Semibold", "Trebuchet MS", sans-serif;
      font-size: clamp(2rem, 4vw, 3rem);
      color: #0f2a33;
    }

    .lead {
      margin: 0 0 1.5rem;
      line-height: 1.55;
      color: #314247;
    }

    .actions {
      display: flex;
      flex-wrap: wrap;
      gap: 0.75rem;
    }

    .actions a {
      color: #0b6e4f;
      font-weight: 600;
    }
  `
})
export class HomePage {}
