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

  public isActive = false;

  public saving = false;
  public error: string;
  public testFolderError: string;
  public testFolderSuccess: boolean;

  public settingRealDebridApiKey: string;
  public settingDownloadFolder: string;
  public settingDownloadLimit: number;
  public settingUnpackLimit: number;

  constructor(private settingsService: SettingsService) {}

  ngOnInit(): void {}

  public reset(): void {
    this.saving = false;
    this.error = null;

    this.settingsService.get().subscribe(
      (results) => {
        this.settingRealDebridApiKey = this.getSetting(results, 'RealDebridApiKey');
        this.settingDownloadFolder = this.getSetting(results, 'DownloadFolder');
        this.settingDownloadLimit = parseInt(this.getSetting(results, 'DownloadLimit'), 10);
        this.settingUnpackLimit = parseInt(this.getSetting(results, 'UnpackLimit'), 10);
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
        settingId: 'DownloadFolder',
        value: this.settingDownloadFolder,
      },
      {
        settingId: 'DownloadLimit',
        value: (this.settingDownloadLimit ?? 10).toString(),
      },
      {
        settingId: 'UnpackLimit',
        value: (this.settingUnpackLimit ?? 1).toString(),
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

  public test(): void {
    this.saving = true;
    this.testFolderError = null;
    this.testFolderSuccess = false;

    this.settingsService.testFolder(this.settingDownloadFolder).subscribe(
      () => {
        this.saving = false;
        this.testFolderSuccess = true;
      },
      (err) => {
        console.log(err);
        this.testFolderError = err.error;
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
