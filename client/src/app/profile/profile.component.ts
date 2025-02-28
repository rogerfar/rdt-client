import { Component } from '@angular/core';
import { AuthService } from '../auth.service';

@Component({
    selector: 'app-profile',
    templateUrl: './profile.component.html',
    styleUrls: ['./profile.component.scss'],
    standalone: false
})
export class ProfileComponent {
  constructor(private authService: AuthService) {}

  public username: string;
  public password: string;

  public saving = false;
  public success: boolean;
  public error: string;

  public save(): void {
    this.success = false;
    this.error = null;
    this.saving = true;

    this.authService.update(this.username, this.password).subscribe(
      () => {
        this.success = true;
        this.saving = false;
      },
      (err) => {
        this.error = err.error;
        this.success = false;
        this.saving = false;
      },
    );
  }
}
