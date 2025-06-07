import { Component } from '@angular/core';
import { AuthService } from '../auth.service';
import { FormsModule } from '@angular/forms';
import { NgClass } from '@angular/common';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.scss'],
  imports: [FormsModule, NgClass],
  standalone: true,
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

    this.authService.update(this.username, this.password).subscribe({
      next: () => {
        this.success = true;
        this.saving = false;
      },
      error: (err) => {
        this.error = err.error;
        this.success = false;
        this.saving = false;
      },
    });
  }
}
