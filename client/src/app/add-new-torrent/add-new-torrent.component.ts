import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { TorrentService } from 'src/app/torrent.service';
import { Torrent, TorrentFileAvailability } from '../models/torrent.model';
import { SettingsService } from '../settings.service';
import { ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { NgClass } from '@angular/common';

@Component({
  selector: 'app-add-new-torrent',
  templateUrl: './add-new-torrent.component.html',
  styleUrls: ['./add-new-torrent.component.scss'],
  imports: [FormsModule, NgClass],
  standalone: true,
})
export class AddNewTorrentComponent implements OnInit {
  public fileName: string;
  public magnetLink: string;
  private currentTorrentFile: string;

  public provider: string;
  public downloadClient: number;

  public category: string;
  public hostDownloadAction: number = 0;
  public downloadAction: number = 0;
  public finishedAction: number = 0;
  public finishedActionDelay: number = 0;
  public downloadMinSize: number = 0;
  public includeRegex: string = '';
  public excludeRegex: string = '';
  public torrentRetryAttempts: number = 1;
  public downloadRetryAttempts: number = 3;
  public torrentDeleteOnError: number = 0;
  public torrentLifetime: number = 0;
  public priority: number;

  public availableFiles: TorrentFileAvailability[];
  public downloadFiles: { [key: string]: boolean } = {};
  public allSelected: boolean;

  public saving = false;
  public error: string;

  public includeRegexError: string;
  public excludeRegexError: string;
  public regexSelected: TorrentFileAvailability[];

  private selectedFile: File;

  constructor(
    private router: Router,
    private torrentService: TorrentService,
    private settingsService: SettingsService,
    private activatedRoute: ActivatedRoute,
  ) {}

  ngOnInit(): void {
    this.activatedRoute.queryParams.subscribe((params) => {
      if (params['magnet']) {
        this.magnetLink = decodeURIComponent(params['magnet']);
      }
    });
    this.settingsService.get().subscribe((settings) => {
      const providerSetting = settings.find((m) => m.key === 'Provider:Provider');
      this.provider = providerSetting.enumValues[providerSetting.value as number];
      this.downloadClient = settings.find((m) => m.key === 'DownloadClient:Client')?.value as number;

      this.category = settings.find((m) => m.key === 'Gui:Default:Category')?.value as string;
      this.hostDownloadAction = this.downloadAction = settings.find((m) => m.key === 'Gui:Default:HostDownloadAction')
        ?.value as number;
      this.downloadAction =
        settings.find((m) => m.key === 'Gui:Default:OnlyDownloadAvailableFiles')?.value === true ? 1 : 0;
      this.finishedAction = settings.find((m) => m.key === 'Gui:Default:FinishedAction')?.value as number;
      this.finishedActionDelay = settings.find((m) => m.key == 'Gui:Default:FinishedActionDelay')?.value as number;
      this.downloadMinSize = settings.find((m) => m.key === 'Gui:Default:MinFileSize')?.value as number;
      this.includeRegex = settings.find((m) => m.key === 'Gui:Default:IncludeRegex')?.value as string;
      this.excludeRegex = settings.find((m) => m.key === 'Gui:Default:ExcludeRegex')?.value as string;
      this.torrentRetryAttempts = settings.find((m) => m.key === 'Gui:Default:TorrentRetryAttempts')?.value as number;
      this.downloadRetryAttempts = settings.find((m) => m.key === 'Gui:Default:DownloadRetryAttempts')?.value as number;
      this.torrentDeleteOnError = settings.find((m) => m.key === 'Gui:Default:DeleteOnError')?.value as number;
      this.torrentLifetime = settings.find((m) => m.key === 'Gui:Default:TorrentLifetime')?.value as number;
      this.priority = settings.find((m) => m.key === 'Gui:Default:Priority')?.value as number;

      this.setFinishAction();
    });
  }

  public setFinishAction() {
    if (this.downloadClient === 2) {
      if (this.finishedAction === 1) {
        this.finishedAction = 3;
      } else if (this.finishedAction === 2) {
        this.finishedAction = 0;
      }
    }
  }

  public pickFile(evt: Event): void {
    const files = (evt.target as HTMLInputElement).files;

    if (files.length === 0) {
      return;
    }

    const file = files[0];

    this.fileName = file.name;

    this.selectedFile = file;

    this.checkFiles();
  }

  public ok(): void {
    this.saving = true;
    this.error = null;

    let downloadManualFiles: string = null;

    if (this.downloadAction === 2) {
      const selectedFiles = [];
      for (const filePath in this.downloadFiles) {
        if (this.downloadFiles[filePath] === true) {
          selectedFiles.push(filePath);
        }
      }

      if (selectedFiles.length === 0) {
        this.error = 'No files have been selected to download';
        return;
      }

      downloadManualFiles = selectedFiles.join(',');
    }

    const torrent = new Torrent();
    torrent.category = this.category;
    torrent.hostDownloadAction = this.hostDownloadAction;
    torrent.downloadAction = this.downloadAction;
    torrent.finishedAction = this.finishedAction;
    torrent.finishedActionDelay = this.finishedActionDelay;
    torrent.downloadMinSize = this.downloadMinSize;
    torrent.includeRegex = this.includeRegex;
    torrent.excludeRegex = this.excludeRegex;
    torrent.downloadManualFiles = downloadManualFiles;
    torrent.priority = this.priority;
    torrent.torrentRetryAttempts = this.torrentRetryAttempts;
    torrent.downloadRetryAttempts = this.downloadRetryAttempts;
    torrent.deleteOnError = this.torrentDeleteOnError;
    torrent.lifetime = this.torrentLifetime;
    torrent.downloadClient = this.downloadClient;

    if (this.magnetLink) {
      this.torrentService.uploadMagnet(this.magnetLink, torrent).subscribe({
        next: () => this.router.navigate(['/']),
        error: (err) => {
          this.error = err.error;
          this.saving = false;
        },
      });
    } else if (this.selectedFile) {
      this.torrentService.uploadFile(this.selectedFile, torrent).subscribe({
        next: () => this.router.navigate(['/']),
        error: (err) => {
          this.error = err.error;
          this.saving = false;
        },
      });
    } else {
      this.error = 'No magnet or file uploaded';
      this.saving = false;
    }
  }

  public onPaste(): void {
    setTimeout(() => {
      this.checkFiles();
    }, 100);
  }

  public checkFiles(): void {
    if (this.magnetLink && this.magnetLink === this.currentTorrentFile) {
      return;
    }

    this.saving = true;
    this.error = null;
    this.availableFiles = null;
    this.downloadFiles = {};
    this.allSelected = true;

    if (this.magnetLink) {
      this.torrentService.checkFilesMagnet(this.magnetLink).subscribe({
        next: (result) => {
          this.saving = false;
          this.availableFiles = result;
          this.currentTorrentFile = this.magnetLink;
          result.forEach((file) => {
            this.downloadFiles[file.filename] = true;
          });
        },
        error: (err) => {
          this.error = err.error;
          this.saving = false;
        },
      });
    } else if (this.selectedFile) {
      this.torrentService.checkFiles(this.selectedFile).subscribe({
        next: (result) => {
          this.saving = false;
          this.availableFiles = result;
          result.forEach((file) => {
            this.downloadFiles[file.filename] = true;
          });
        },
        error: (err) => {
          this.error = err.error;
          this.saving = false;
        },
      });
    } else {
      this.saving = false;
    }
  }

  public verifyRegex(): void {
    this.includeRegexError = null;
    this.excludeRegexError = null;
    this.regexSelected = null;

    this.torrentService.verifyRegex(this.includeRegex, this.excludeRegex, this.magnetLink).subscribe((result) => {
      this.includeRegexError = result.includeError;
      this.excludeRegexError = result.excludeError;
      this.regexSelected = result.selectedFiles;
    });
  }
}
