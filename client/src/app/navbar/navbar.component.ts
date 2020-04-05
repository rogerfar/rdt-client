import { Component, OnInit } from '@angular/core';
import { TorrentService } from '../torrent.service';
import { SettingsService } from '../settings.service';
import { Profile } from '../models/profile.model';
import { AuthService } from '../auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss'],
})
export class NavbarComponent implements OnInit {
  public showMobileMenu = false;

  public showNewTorrent = false;
  public showSettings = false;

  public profile: Profile;

  constructor(
    private settingsService: SettingsService,
    private authService: AuthService,
    private router: Router
  ) {}

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
