import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../auth.service';

@Component({
    selector: 'app-login',
    templateUrl: './login.component.html',
    styleUrls: ['./login.component.scss'],
    standalone: false
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
    this.authService.login(this.userName, this.password).subscribe(
      () => {
        this.router.navigate(['/']);
      },
      (err) => {
        this.loggingIn = false;
        this.error = err.error;
      },
    );
  }
}
