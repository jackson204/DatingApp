import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { Nav } from "../layout/nav/nav";
import { AccountService } from '../core/services/account-service';
import { lastValueFrom } from 'rxjs';
import { User } from '../types/user';
import { Router, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [Nav, RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  private accountService = inject(AccountService);
  protected router = inject(Router)
  private http = inject(HttpClient);
  protected title = 'Dating App';
  protected members = signal<User[]>([]);

  async ngOnInit() {
    this.setCurrentUser();
    this.members.set(await this.getMembers());
  }

  setCurrentUser() {
    const userString = localStorage.getItem('user');
    if (!userString)  return; 
    const user = JSON.parse(userString);
    this.accountService.currentUser.set(user);
  }

  async getMembers() {
    try {
      return lastValueFrom(this.http.get<User[]>('https://localhost:5001/api/Members'));
    } catch (error) {
      console.error(error);
      throw error;
    }
  }
}
