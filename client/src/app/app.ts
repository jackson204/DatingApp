import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit } from '@angular/core';

@Component({
  selector: 'app-root',
  imports: [],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  private http = inject(HttpClient);
  protected  title = 'Dating App';
  protected members: any ;

  ngOnInit(): void {
    this.http.get('https://localhost:5001/api/members').subscribe({
      next: response => this.members=response,
      error: err => console.error(err),
      complete: () => console.log('Request complete')
    });
  }
}
