import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../auth.service';
import { FormsModule } from '@angular/forms';
import { NgClass } from '@angular/common';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
  imports: [FormsModule, NgClass],
  standalone: true,
})
export class LoginComponent {
  public userName: string;
  public password: string;
  public error: string;
  public loggingIn: boolean;

  constructor(
    private authService: AuthService,
    private router: Router,
  ) {}

  public setUserName(event: Event): void {
    this.userName = (event.target as any).value;
  }

  public setPassword(event: Event): void {
    this.password = (event.target as any).value;
  }

  public login(): void {
    this.error = null;
    this.loggingIn = true;
    this.authService.login(this.userName, this.password).subscribe({
      next: () => this.router.navigate(['/']),
      error: (err) => {
        this.loggingIn = false;
        this.error = err.error;
      },
    });
  }
}
