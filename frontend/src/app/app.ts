import { Component, effect, inject, untracked } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatToolbarModule } from '@angular/material/toolbar';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, MatToolbarModule, MatButtonModule, MatIconModule],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  protected readonly auth = inject(AuthService);

  constructor() {
    // After a page reload the token is restored from storage, but the user's name has to be fetched again.
    effect(() => {
      if (this.auth.isAuthenticated() && !this.auth.currentUser()) {
        untracked(() => this.auth.loadCurrentUser().subscribe({ error: () => undefined }));
      }
    });
  }
}
