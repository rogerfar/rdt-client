import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../auth.service';
import { Setting } from '../models/setting.model';
import { SettingsService } from '../settings.service';

@Component({
  selector: 'app-setup',
  templateUrl: './setup.component.html',
  styleUrls: ['./setup.component.scss'],
})
export class SetupComponent implements OnInit {
  public userName: string;
  public password: string;
  public token: string;

  public error: string;
  public working: boolean;

  public step: number = 1;

  constructor(private authService: AuthService, private settingsService: SettingsService, private router: Router) {}

  ngOnInit(): void {}

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
      }
    );
  }

  public setToken(): void {
    const setting = new Setting();
    setting.settingId = 'RealDebridApiKey';
    setting.value = this.token;

    this.settingsService.update([setting]).subscribe(
      () => {
        this.step = 3;
        this.working = false;
      },
      (err) => {
        this.working = false;
        this.error = err.error;
      }
    );
  }

  public close(): void {
    this.router.navigate(['/']);
  }
}
