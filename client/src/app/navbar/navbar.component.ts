import { Component, OnInit } from '@angular/core';
import { TorrentService } from '../torrent.service';
import { SettingsService } from '../settings.service';
import { Profile } from '../models/profile.model';

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

  constructor(private settingsService: SettingsService) {}

  ngOnInit(): void {
    this.settingsService.getProfile().subscribe((result) => {
      this.profile = result;
    });
  }
}
