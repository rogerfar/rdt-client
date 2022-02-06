import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { TorrentService } from 'src/app/torrent.service';
import { Torrent, TorrentFileAvailability } from '../models/torrent.model';
import { SettingsService } from '../settings.service';

@Component({
  selector: 'app-add-new-torrent',
  templateUrl: './add-new-torrent.component.html',
  styleUrls: ['./add-new-torrent.component.scss'],
})
export class AddNewTorrentComponent implements OnInit {
  public fileName: string;
  public magnetLink: string;
  private currentTorrentFile: string;

  public provider: string;

  public category: string;
  public priority: number;

  public downloadAction: number = 0;
  public finishedAction: number = 0;

  public downloadMinSize: number = 0;

  public downloadRetryAttempts: number = 3;
  public torrentRetryAttempts: number = 1;
  public torrentDeleteOnError: number = 0;
  public torrentLifetime: number = 0;

  public availableFiles: TorrentFileAvailability[];
  public downloadFiles: { [key: string]: boolean } = {};
  public allSelected: boolean;

  public saving = false;
  public error: string;

  private selectedFile: File;

  constructor(
    private router: Router,
    private torrentService: TorrentService,
    private settingsService: SettingsService
  ) {
    this.settingsService.get().subscribe((settings) => {
      this.provider = settings.firstOrDefault((m) => m.settingId === 'Provider')?.value;
    });
  }

  ngOnInit(): void {}

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

  public downloadFileChecked(file: string): void {
    this.downloadFiles[file] = !this.downloadFiles[file];

    this.allSelected = true;
    this.availableFiles.forEach((file) => {
      if (!this.downloadFiles[file.filename]) {
        this.allSelected = false;
      }
    });
  }

  public downloadFileCheckedAll(): void {
    this.allSelected = !this.allSelected;

    this.availableFiles.forEach((file) => {
      this.downloadFiles[file.filename] = this.allSelected;
    });
  }

  public ok(): void {
    this.saving = true;
    this.error = null;

    let downloadManualFiles: string = null;

    if (this.downloadAction === 2) {
      const selectedFiles = [];
      for (let filePath in this.downloadFiles) {
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
    torrent.downloadAction = this.downloadAction;
    torrent.finishedAction = this.finishedAction;
    torrent.downloadMinSize = this.downloadMinSize;
    torrent.downloadManualFiles = downloadManualFiles;
    torrent.priority = this.priority;
    torrent.torrentRetryAttempts = this.torrentRetryAttempts;
    torrent.downloadRetryAttempts = this.downloadRetryAttempts;
    torrent.deleteOnError = this.torrentDeleteOnError;
    torrent.lifetime = this.torrentLifetime;

    if (this.magnetLink) {
      this.torrentService.uploadMagnet(this.magnetLink, torrent).subscribe(
        () => {
          this.router.navigate(['/']);
        },
        (err) => {
          this.error = err.error;
          this.saving = false;
        }
      );
    } else if (this.selectedFile) {
      this.torrentService.uploadFile(this.selectedFile, torrent).subscribe(
        () => {
          this.router.navigate(['/']);
        },
        (err) => {
          this.error = err.error;
          this.saving = false;
        }
      );
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
      this.torrentService.checkFilesMagnet(this.magnetLink).subscribe(
        (result) => {
          this.saving = false;
          this.availableFiles = result;
          this.currentTorrentFile = this.magnetLink;
          result.forEach((file) => {
            this.downloadFiles[file.filename] = true;
          });
        },
        (err) => {
          this.error = err.error;
          this.saving = false;
        }
      );
    } else if (this.selectedFile) {
      this.torrentService.checkFiles(this.selectedFile).subscribe(
        (result) => {
          this.saving = false;
          this.availableFiles = result;
          result.forEach((file) => {
            this.downloadFiles[file.filename] = true;
          });
        },
        (err) => {
          this.error = err.error;
          this.saving = false;
        }
      );
    } else {
      this.saving = false;
    }
  }
}
