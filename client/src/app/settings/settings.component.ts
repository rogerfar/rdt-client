import { Component, OnInit } from '@angular/core';
import { SettingsService } from 'src/app/settings.service';
import { Setting } from '../models/setting.model';

@Component({
    selector: 'app-settings',
    templateUrl: './settings.component.html',
    styleUrls: ['./settings.component.scss'],
    standalone: false
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

  constructor(private settingsService: SettingsService) {}

  ngOnInit(): void {
    this.reset();
  }

  public reset(): void {
    this.settingsService.get().subscribe((settings) => {
      this.tabs = settings.where((m) => m.key.indexOf(':') === -1);

      for (let tab of this.tabs) {
        tab.settings = settings.where((m) => m.key.indexOf(`${tab.key}:`) > -1);
      }
    });
  }

  public ok(): void {
    this.saving = true;

    const settingsToSave = this.tabs.selectMany((m) => m.settings).where((m) => m.type !== 'Object');

    this.settingsService.update(settingsToSave).subscribe(
      () => {
        setTimeout(() => {
          this.saving = false;
        }, 1000);
      },
      (err) => {
        this.saving = false;
        this.error = err;
      },
    );
  }

  public testDownloadPath(): void {
    const settingDownloadPath = this.tabs
      .first((m) => m.key === 'DownloadClient')
      .settings.first((m) => m.key === 'DownloadClient:DownloadPath').value as string;

    this.saving = true;
    this.testPathError = null;
    this.testPathSuccess = false;

    this.settingsService.testPath(settingDownloadPath).subscribe(
      () => {
        this.saving = false;
        this.testPathSuccess = true;
      },
      (err) => {
        this.testPathError = err.error;
        this.saving = false;
      },
    );
  }

  public testDownloadSpeed(): void {
    this.saving = true;
    this.testDownloadSpeedError = null;
    this.testDownloadSpeedSuccess = 0;

    this.settingsService.testDownloadSpeed().subscribe(
      (result) => {
        this.saving = false;
        this.testDownloadSpeedSuccess = result;
      },
      (err) => {
        this.testDownloadSpeedError = err.error;
        this.saving = false;
      },
    );
  }
  public testWriteSpeed(): void {
    this.saving = true;
    this.testWriteSpeedError = null;
    this.testWriteSpeedSuccess = 0;

    this.settingsService.testWriteSpeed().subscribe(
      (result) => {
        this.saving = false;
        this.testWriteSpeedSuccess = result;
      },
      (err) => {
        this.testWriteSpeedError = err.error;
        this.saving = false;
      },
    );
  }

  public testAria2cConnection(): void {
    const settingAria2cUrl = this.tabs
      .first((m) => m.key === 'DownloadClient')
      .settings.first((m) => m.key === 'DownloadClient:Aria2cUrl').value as string;
    const settingAria2cSecret = this.tabs
      .first((m) => m.key === 'DownloadClient')
      .settings.first((m) => m.key === 'DownloadClient:Aria2cSecret').value as string;

    this.saving = true;
    this.testAria2cConnectionError = null;
    this.testAria2cConnectionSuccess = null;

    this.settingsService.testAria2cConnection(settingAria2cUrl, settingAria2cSecret).subscribe(
      (result) => {
        this.saving = false;
        this.testAria2cConnectionSuccess = result.version;
      },
      (err) => {
        this.testAria2cConnectionError = err.error;
        this.saving = false;
      },
    );
  }
}
