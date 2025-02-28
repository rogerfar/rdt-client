import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../auth.service';

@Component({
    selector: 'app-setup',
    templateUrl: './setup.component.html',
    styleUrls: ['./setup.component.scss'],
    standalone: false
})
export class SetupComponent {
  public userName: string;
  public password: string;
  public provider = 0;
  public token: string;

  public error: string;
  public working: boolean;

  public step: number = 1;

  constructor(
    private authService: AuthService,
    private router: Router,
  ) {}

  public setup(): void {
    this.error = null;
    this.working = true;

    this.authService.create(this.userName, this.password).subscribe(
      () => {
        this.step = 2;
        this.working = false;
      },
      (err) => {
        this.working = false;
        this.error = err.error;
      },
    );
  }

  public setToken(): void {
    this.authService.setupProvider(this.provider, this.token).subscribe(
      () => {
        this.step = 3;
        this.working = false;
      },
      (err: any) => {
        this.working = false;
        this.error = err.error;
      },
    );
  }

  public close(): void {
    this.router.navigate(['/']);
  }
}
