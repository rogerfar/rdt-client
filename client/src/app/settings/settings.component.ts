import { Component, OnInit } from '@angular/core';
import { SettingsService } from 'src/app/settings.service';
import { Setting } from '../models/setting.model';
import { NgClass, KeyValuePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Nl2BrPipe } from '../nl2br.pipe';
import { FileSizePipe } from '../filesize.pipe';

@Component({
  selector: 'app-settings',
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.scss'],
  imports: [NgClass, FormsModule, KeyValuePipe, Nl2BrPipe, FileSizePipe],
  standalone: true,
})
export class SettingsComponent implements OnInit {
  public activeTab = 0;

  public tabs: Setting[] = [];

  public saving = false;
  public error: string;

  public testPathError: string;
  public testPathSuccess: boolean;

  public testDownloadSpeedError: string;
  public testDownloadSpeedSuccess: number;

  public testWriteSpeedError: string;
  public testWriteSpeedSuccess: number;

  public testAria2cConnectionError: string = null;
  public testAria2cConnectionSuccess: string = null;

  public canRegisterMagnetHandler = false;

  constructor(private settingsService: SettingsService) {}

  ngOnInit(): void {
    this.reset();
    this.canRegisterMagnetHandler = !!(window.isSecureContext && 'registerProtocolHandler' in navigator);
  }

  public reset(): void {
    this.settingsService.get().subscribe((settings) => {
      this.tabs = settings.filter((m) => m.key.indexOf(':') === -1);

      for (let tab of this.tabs) {
        tab.settings = settings.filter((m) => m.key.indexOf(`${tab.key}:`) > -1);
      }
    });
  }

  public ok(): void {
    this.saving = true;

    const settingsToSave = this.tabs.flatMap((m) => m.settings).filter((m) => m.type !== 'Object');

    this.settingsService.update(settingsToSave).subscribe({
      next: () =>
        setTimeout(() => {
          this.saving = false;
        }, 1000),
      error: (err) => {
        this.saving = false;
        this.error = err;
      },
    });
  }

  public testDownloadPath(): void {
    const settingDownloadPath = this.tabs
      .find((m) => m.key === 'DownloadClient')
      .settings.find((m) => m.key === 'DownloadClient:DownloadPath').value as string;

    this.saving = true;
    this.testPathError = null;
    this.testPathSuccess = false;

    this.settingsService.testPath(settingDownloadPath).subscribe({
      next: () => {
        this.saving = false;
        this.testPathSuccess = true;
      },
      error: (err) => {
        this.testPathError = err.error;
        this.saving = false;
      },
    });
  }

  public testDownloadSpeed(): void {
    this.saving = true;
    this.testDownloadSpeedError = null;
    this.testDownloadSpeedSuccess = 0;

    this.settingsService.testDownloadSpeed().subscribe({
      next: (result) => {
        this.saving = false;
        this.testDownloadSpeedSuccess = result;
      },
      error: (err) => {
        this.testDownloadSpeedError = err.error;
        this.saving = false;
      },
    });
  }
  public testWriteSpeed(): void {
    this.saving = true;
    this.testWriteSpeedError = null;
    this.testWriteSpeedSuccess = 0;

    this.settingsService.testWriteSpeed().subscribe({
      next: (result) => {
        this.saving = false;
        this.testWriteSpeedSuccess = result;
      },
      error: (err) => {
        this.testWriteSpeedError = err.error;
        this.saving = false;
      },
    });
  }

  public testAria2cConnection(): void {
    const settingAria2cUrl = this.tabs
      .find((m) => m.key === 'DownloadClient')
      .settings.find((m) => m.key === 'DownloadClient:Aria2cUrl').value as string;
    const settingAria2cSecret = this.tabs
      .find((m) => m.key === 'DownloadClient')
      .settings.find((m) => m.key === 'DownloadClient:Aria2cSecret').value as string;

    this.saving = true;
    this.testAria2cConnectionError = null;
    this.testAria2cConnectionSuccess = null;

    this.settingsService.testAria2cConnection(settingAria2cUrl, settingAria2cSecret).subscribe({
      next: (result) => {
        this.saving = false;
        this.testAria2cConnectionSuccess = result.version;
      },
      error: (err) => {
        this.testAria2cConnectionError = err.error;
        this.saving = false;
      },
    });
  }

  public registerMagnetHandler(): void {
    try {
      navigator.registerProtocolHandler('magnet', `${window.location.origin}/add?magnet=%s`);
      alert(
        'Success! Your browser will now prompt you to confirm and add the client as the default handler for magnet links.',
      );
    } catch (error) {
      alert('Magnet link registration failed.');
    }
  }
}
