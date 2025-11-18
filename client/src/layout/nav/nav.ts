import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { popResultSelector } from 'rxjs/internal/util/args';
import { AccountService } from '../../core/services/account-service';
import { single } from 'rxjs';

@Component({
  selector: 'app-nav',
  imports: [FormsModule],
  templateUrl: './nav.html',
  styleUrl: './nav.css',
})
export class Nav {
  protected accountService = inject(AccountService);
  protected creds: any = {}

  login() {
    this.accountService.login(this.creds).subscribe({
      next: result => {
        console.log(result);
        this.creds = {};
      },
      error: error => {
        alert(error.message);
      }
    })
  }

  logout() {
    this.accountService.logout();
  }
}
