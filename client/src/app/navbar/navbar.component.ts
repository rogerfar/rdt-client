import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { AuthService } from '../auth.service';
import { Profile } from '../models/profile.model';
import { SettingsService } from '../settings.service';
import { NgClass, DatePipe } from '@angular/common';
import { filter } from 'rxjs';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss'],
  imports: [RouterLink, NgClass, DatePipe],
  standalone: true,
})
export class NavbarComponent implements OnInit {
  private destroyRef = inject(DestroyRef);
  private settingsService = inject(SettingsService);
  private authService = inject(AuthService);
  private router = inject(Router);

  public showMobileMenu = false;

  public profile: Profile;
  public providerLink: string;
  public version: string;

  constructor() {
    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => {
        this.showMobileMenu = false;
      });
  }

  ngOnInit(): void {
    this.settingsService
      .getProfile()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
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

    this.settingsService
      .getVersion()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.version = result.version;
      });
  }

  public logout(): void {
    this.authService.logout().subscribe({ next: () => this.router.navigate(['/login']), error: console.error });
  }
}
