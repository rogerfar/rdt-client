import { Component, OnInit } from '@angular/core';
import { Setting } from 'src/app/models/setting.model';
import { SettingsService } from 'src/app/settings.service';

@Component({
  selector: 'app-settings',
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.scss'],
})
export class SettingsComponent implements OnInit {
  public activeTab = 0;

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

  public settingLogLevel: string;
  public settingProvider: string;
  public settingProviderAutoImport: boolean;
  public settingProviderAutoImportCategory: string;
  public settingProviderAutoDelete: boolean;
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
  public settingAria2cUrl: string;
  public settingAria2cSecret: string;
  public settingDownloadRetryAttempts: number;
  public settingTorrentRetryAttempts: number;
  public settingDeleteOnError: number;
  public settingTorrentLifetime: number;

  constructor(private settingsService: SettingsService) {}

  ngOnInit(): void {
    this.reset();
  }

  public reset(): void {
    this.saving = false;
    this.error = null;

    this.settingsService.get().subscribe(
      (results) => {
        this.settingProvider = this.getSetting(results, 'Provider');
        this.settingProviderAutoImport = this.getSetting(results, 'ProviderAutoImport') === '1';
        this.settingProviderAutoImportCategory = this.getSetting(results, 'ProviderAutoImportCategory');
        this.settingProviderAutoDelete = this.getSetting(results, 'ProviderAutoDelete') === '1';
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
        this.settingAria2cUrl = this.getSetting(results, 'Aria2cUrl');
        this.settingAria2cSecret = this.getSetting(results, 'Aria2cSecret');
        this.settingDownloadRetryAttempts = parseInt(this.getSetting(results, 'DownloadRetryAttempts'), 10);
        this.settingTorrentRetryAttempts = parseInt(this.getSetting(results, 'TorrentRetryAttempts'), 10);
        this.settingDeleteOnError = parseInt(this.getSetting(results, 'DeleteOnError'), 10);
        this.settingTorrentLifetime = parseInt(this.getSetting(results, 'TorrentLifetime'), 10);
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
        settingId: 'Provider',
        value: this.settingProvider,
      },
      {
        settingId: 'ProviderAutoImport',
        value: this.settingProviderAutoImport ? '1' : '0',
      },
      {
        settingId: 'ProviderAutoImportCategory',
        value: this.settingProviderAutoImportCategory,
      },
      {
        settingId: 'ProviderAutoDelete',
        value: this.settingProviderAutoDelete ? '1' : '0',
      },
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
      {
        settingId: 'Aria2cUrl',
        value: this.settingAria2cUrl,
      },
      {
        settingId: 'Aria2cSecret',
        value: this.settingAria2cSecret,
      },
      {
        settingId: 'DownloadRetryAttempts',
        value: (this.settingDownloadRetryAttempts ?? 0).toString(),
      },
      {
        settingId: 'TorrentRetryAttempts',
        value: (this.settingTorrentRetryAttempts ?? 0).toString(),
      },
      {
        settingId: 'DeleteOnError',
        value: (this.settingDeleteOnError ?? 0).toString(),
      },
      {
        settingId: 'TorrentLifetime',
        value: (this.settingTorrentLifetime ?? 0).toString(),
      },
    ];

    this.settingsService.update(settings).subscribe(
      () => {
        setTimeout(() => {
          this.saving = false;
        }, 1000);
      },
      (err) => {
        this.saving = false;
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

  public testAria2cConnection(): void {
    this.saving = true;
    this.testAria2cConnectionError = null;
    this.testAria2cConnectionSuccess = null;

    this.settingsService.testAria2cConnection(this.settingAria2cUrl, this.settingAria2cSecret).subscribe(
      (result) => {
        this.saving = false;
        this.testAria2cConnectionSuccess = result.version;
      },
      (err) => {
        this.testAria2cConnectionError = err.error;
        this.saving = false;
      }
    );
  }

  private getSetting(settings: Setting[], key: string): string {
    const setting = settings.filter((m) => m.settingId === key);

    if (setting.length !== 1) {
      throw new Error(`Unable to find setting with key ${key}`);
    }

    return setting[0].value;
  }
}
