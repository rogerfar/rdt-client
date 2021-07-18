import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../auth.service';
import { Profile } from '../models/profile.model';
import { SettingsService } from '../settings.service';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss'],
})
export class NavbarComponent implements OnInit {
  public showMobileMenu = false;

  public profile: Profile;

  constructor(private settingsService: SettingsService, private authService: AuthService, private router: Router) {}

  ngOnInit(): void {
    this.settingsService.getProfile().subscribe((result) => {
      this.profile = result;
    });
  }

  public logout(): void {
    this.authService.logout().subscribe(
      () => {
        this.router.navigate(['/login']);
      },
      (err) => {}
    );
  }
}
