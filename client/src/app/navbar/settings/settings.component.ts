import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { Setting } from 'src/app/models/setting.model';
import { SettingsService } from 'src/app/settings.service';

@Component({
  selector: 'app-settings',
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.scss'],
})
export class SettingsComponent implements OnInit {
  @Input()
  public get open(): boolean {
    return this.isActive;
  }

  public set open(val: boolean) {
    this.reset();
    this.isActive = val;
  }

  @Output()
  public openChange = new EventEmitter<boolean>();

  public activeTab = 0;

  public isActive = false;

  public saving = false;
  public error: string;

  public testPathError: string;
  public testPathSuccess: boolean;

  public testDownloadSpeedError: string;
  public testDownloadSpeedSuccess: number;

  public testWriteSpeedError: string;
  public testWriteSpeedSuccess: number;

  public settingLogLevel: string;
  public settingRealDebridApiKey: string;
  public settingDownloadPath: string;
  public settingMappedPath: string;
  public settingTempPath: string;
  public settingDownloadClient: string;
  public settingDownloadLimit: number;
  public settingDownloadChunkCount: number;
  public settingDownloadMaxSpeed: number;
  public settingUnpackLimit: number;
  public settingMinFileSize: number;
  public settingOnlyDownloadAvailableFiles: boolean;
  public settingProxyServer: string;

  constructor(private settingsService: SettingsService) {}

  ngOnInit(): void {}

  public reset(): void {
    this.saving = false;
    this.error = null;

    this.settingsService.get().subscribe(
      (results) => {
        this.settingRealDebridApiKey = this.getSetting(results, 'RealDebridApiKey');
        this.settingLogLevel = this.getSetting(results, 'LogLevel');
        this.settingDownloadPath = this.getSetting(results, 'DownloadPath');
        this.settingMappedPath = this.getSetting(results, 'MappedPath');
        this.settingTempPath = this.getSetting(results, 'TempPath');
        this.settingDownloadClient = this.getSetting(results, 'DownloadClient');
        this.settingDownloadLimit = parseInt(this.getSetting(results, 'DownloadLimit'), 10);
        this.settingDownloadChunkCount = parseInt(this.getSetting(results, 'DownloadChunkCount'), 10);
        this.settingDownloadMaxSpeed = parseInt(this.getSetting(results, 'DownloadMaxSpeed'), 10);
        this.settingUnpackLimit = parseInt(this.getSetting(results, 'UnpackLimit'), 10);
        this.settingMinFileSize = parseInt(this.getSetting(results, 'MinFileSize'), 10);
        this.settingOnlyDownloadAvailableFiles = this.getSetting(results, 'OnlyDownloadAvailableFiles') === '1';
        this.settingProxyServer = this.getSetting(results, 'ProxyServer');
      },
      (err) => {
        this.error = err.error;
        this.saving = true;
      }
    );
  }

  public ok(): void {
    this.saving = true;

    const settings: Setting[] = [
      {
        settingId: 'RealDebridApiKey',
        value: this.settingRealDebridApiKey,
      },
      {
        settingId: 'LogLevel',
        value: this.settingLogLevel,
      },
      {
        settingId: 'DownloadPath',
        value: this.settingDownloadPath,
      },
      {
        settingId: 'MappedPath',
        value: this.settingMappedPath,
      },
      {
        settingId: 'TempPath',
        value: this.settingTempPath,
      },
      {
        settingId: 'DownloadClient',
        value: this.settingDownloadClient,
      },
      {
        settingId: 'DownloadLimit',
        value: (this.settingDownloadLimit ?? 10).toString(),
      },
      {
        settingId: 'DownloadChunkCount',
        value: (this.settingDownloadChunkCount ?? 8).toString(),
      },
      {
        settingId: 'DownloadMaxSpeed',
        value: (this.settingDownloadMaxSpeed ?? 0).toString(),
      },
      {
        settingId: 'UnpackLimit',
        value: (this.settingUnpackLimit ?? 1).toString(),
      },
      {
        settingId: 'MinFileSize',
        value: (this.settingMinFileSize ?? 0).toString(),
      },
      {
        settingId: 'OnlyDownloadAvailableFiles',
        value: (this.settingOnlyDownloadAvailableFiles ? '1' : '0').toString(),
      },
      {
        settingId: 'ProxyServer',
        value: this.settingProxyServer,
      },
    ];

    this.settingsService.update(settings).subscribe(
      () => {
        this.isActive = false;
        this.openChange.emit(this.open);
      },
      (err) => {
        this.error = err;
      }
    );
  }

  public testDownloadPath(): void {
    this.saving = true;
    this.testPathError = null;
    this.testPathSuccess = false;

    this.settingsService.testPath(this.settingDownloadPath).subscribe(
      () => {
        this.saving = false;
        this.testPathSuccess = true;
      },
      (err) => {
        this.testPathError = err.error;
        this.saving = false;
      }
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
      }
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
      }
    );
  }

  public cancel(): void {
    this.isActive = false;
    this.openChange.emit(this.open);
  }

  private getSetting(settings: Setting[], key: string): string {
    const setting = settings.filter((m) => m.settingId === key);

    if (setting.length !== 1) {
      throw new Error(`Unable to find setting with key ${key}`);
    }

    return setting[0].value;
  }
}
