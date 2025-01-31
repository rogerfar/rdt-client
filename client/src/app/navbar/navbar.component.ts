import { Component, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { AuthService } from '../auth.service';
import { Profile } from '../models/profile.model';
import { SettingsService } from '../settings.service';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss'],
  standalone: false,
})
export class NavbarComponent implements OnInit {
  public showMobileMenu = false;

  public profile: Profile;
  public providerLink: string;
  public version: string;

  constructor(
    private settingsService: SettingsService,
    private authService: AuthService,
    private router: Router,
  ) {
    this.router.events.subscribe((event) => {
      if (event instanceof NavigationEnd) {
        this.showMobileMenu = false;
      }
    });
  }

  ngOnInit(): void {
    this.settingsService.getProfile().subscribe((result) => {
      this.profile = result;

      switch (result.provider) {
        case 'RealDebrid':
          this.providerLink = 'https://real-debrid.com/?id=1348683';
          break;
        case 'AllDebrid':
          this.providerLink = 'https://alldebrid.com/?uid=2v91l&lang=en';
          break;
        case 'Premiumize':
          this.providerLink = 'https://www.premiumize.me/';
          break;
        case 'TorBox':
          this.providerLink = 'https://torbox.app/';
          break;
        case 'DebridLink':
          this.providerLink = 'https://debrid-link.com/';
          break;
      }
    });

    this.settingsService.getVersion().subscribe((result) => {
      this.version = result.version;
    });
  }

  public registerMagnetHandler(): void {
    if (window.location.protocol !== "https:") {
      alert("Magnet link registration requires a secure connection. Please ensure your site is being served over HTTPS to enable this feature.");
      return;
    }

    const handlerUrl = `${window.location.origin}/add?magnet=%s`;

    if (navigator.registerProtocolHandler) {
      navigator.registerProtocolHandler("magnet", handlerUrl);
      alert("Your browser will display a prompt asking if you'd like to add the client as the default application for magnet links. Please confirm to complete the setup.");
    } else {
      alert("Magnet link registration failed. Your browser does not support registering custom protocol handlers.");
    }
  }

  public logout(): void {
    this.authService.logout().subscribe(
      () => {
        this.router.navigate(['/login']);
      },
      (err) => {},
    );
  }
}
