import { Component, OnInit } from '@angular/core';
import { SettingsService } from 'src/app/settings.service';
import { Setting } from '../models/setting.model';

type Value = string | number | boolean;

interface Tab {
  key: string;
  settings: Setting[];
}

@Component({
  selector: 'app-settings',
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.scss'],
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

  readonly DOWNLOAD_CLIENT = 'DownloadClient';
  readonly DOWNLOAD_CLIENT_KEY = 'DownloadClient:Client';
  readonly SETTINGS_SECTION_NAMES = ["Provider", "Integrations", "Gui", "Watch"];
  readonly FINISHED_ACTION_KEY = ":Default:FinishedAction";
  readonly NO_ACTION = "0";
  readonly SYMLINK_VALUE = "2";
  readonly POST_PROCESS_CLIENT_VALUE = "3";
  readonly REMOVE_PROVIDER_VALUE = "1";
  readonly REMOVE_PROVIDER_AND_CLIENT_VALUE = "2";
  readonly HIDDEN_VALUES_ARRAY = [this.REMOVE_PROVIDER_VALUE, this.REMOVE_PROVIDER_AND_CLIENT_VALUE];

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
      this.updateDownloadClientValue();
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
      }
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
      }
    );
  }

  private getProviderDefaultTab(sectionName:string){
    return this.tabs.find((m) => m.key === sectionName);
  }

  private getPostDownloadAction(providerDefaultTab: Tab | undefined, sectionName:string): Setting | undefined {
    return providerDefaultTab?.settings.find((m) => m.key === `${sectionName}${this.FINISHED_ACTION_KEY}`);
  }

  private getAllPostDownloadActions(sectionName: string){
    const providerDefaultTab = this.getProviderDefaultTab(sectionName);
    return this.getPostDownloadAction(providerDefaultTab, sectionName);
  }

  public onDownloadClientChange(value: Value, key: string): void {
    if (this.isDownloadClientKey(key)) {
      this.toggleRemoveTorrentFromProvider(value);
      console.log("Value: ", value);
      this.updatePostDownloadAction(value);
    }
  }

  private updateDownloadClientValue(): void {
    const downloadClientValue = this.tabs
      .find((m) => m.key === this.DOWNLOAD_CLIENT)
      ?.settings.find((m) => m.key === this.DOWNLOAD_CLIENT_KEY)?.value;
    this.onDownloadClientChange(downloadClientValue.toString(), this.DOWNLOAD_CLIENT_KEY);
  }

  private isDownloadClientKey(key: string): boolean {
    return key === this.DOWNLOAD_CLIENT_KEY;
  }

  private toggleRemoveTorrentFromProvider(value: Value): void {
    if (value === this.SYMLINK_VALUE) {
      this.removeHiddenValues();
    } else {
      this.addHiddenValues();
    }
  }

  private handleEnumValues(postDownloadAction: Setting) {
    if (postDownloadAction.originalEnumValues === undefined) {
      postDownloadAction.originalEnumValues = {...postDownloadAction.enumValues};
    } else {
      postDownloadAction.enumValues = {...postDownloadAction.originalEnumValues};
    }
  }

  private addHiddenValues(): void {
    this.SETTINGS_SECTION_NAMES.forEach((sectionName) => {
      let postDownloadAction = this.getAllPostDownloadActions(sectionName);
      if (postDownloadAction != null){
        this.handleEnumValues(postDownloadAction);
      }
    });
  }

  private removeHiddenValues(): void {
    this.SETTINGS_SECTION_NAMES.forEach((sectionName) => {
      let postDownloadAction = this.getAllPostDownloadActions(sectionName);
      if (postDownloadAction != null) {
        postDownloadAction.originalEnumValues = { ...postDownloadAction.enumValues };
        this.HIDDEN_VALUES_ARRAY.forEach((hiddenValue) => {
          delete postDownloadAction.enumValues[hiddenValue];
        });
      }
    });
  }

  private updatePostDownloadAction(value: Value): void {
    this.SETTINGS_SECTION_NAMES.forEach((sectionName) => {
      let postDownloadAction = this.getAllPostDownloadActions(sectionName);
      if (postDownloadAction && value === this.SYMLINK_VALUE && postDownloadAction.value.toString() !== this.NO_ACTION) {
        postDownloadAction.value = this.POST_PROCESS_CLIENT_VALUE;
      }
    });
  }
}
