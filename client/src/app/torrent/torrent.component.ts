import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { saveAs } from 'file-saver-es';
import { Torrent } from '../models/torrent.model';
import { TorrentService } from '../torrent.service';
import { NgClass, DatePipe } from '@angular/common';
import { CdkCopyToClipboard } from '@angular/cdk/clipboard';
import { FormsModule } from '@angular/forms';
import { TorrentStatusPipe } from '../torrent-status.pipe';
import { DownloadStatusPipe } from '../download-status.pipe';
import { DecodeURIPipe } from '../decode-uri.pipe';
import { FileSizePipe } from '../filesize.pipe';

@Component({
  selector: 'app-torrent',
  templateUrl: './torrent.component.html',
  styleUrls: ['./torrent.component.scss'],
  imports: [
    NgClass,
    CdkCopyToClipboard,
    FormsModule,
    DatePipe,
    TorrentStatusPipe,
    DownloadStatusPipe,
    DecodeURIPipe,
    FileSizePipe,
  ],
  standalone: true,
})
export class TorrentComponent implements OnInit {
  public torrent: Torrent;

  public activeTab: number = 0;

  public copied: boolean = false;

  public downloadExpanded: { [downloadId: string]: boolean } = {};

  public isDeleteModalActive: boolean;
  public deleteError: string;
  public deleting: boolean;
  public deleteSelectAll: boolean;
  public deleteData: boolean;
  public deleteRdTorrent: boolean;
  public deleteLocalFiles: boolean;

  public isRetryModalActive: boolean;
  public retryError: string;
  public retrying: boolean;

  public isDownloadRetryModalActive: boolean;
  public downloadRetryError: string;
  public downloadRetrying: boolean;
  public downloadRetryId: string;

  public isUpdateSettingsModalActive: boolean;

  public updateSettingsDownloadClient: number;
  public updateSettingsHostDownloadAction: number;
  public updateSettingsCategory: string;
  public updateSettingsPriority: number;
  public updateSettingsDownloadRetryAttempts: number;
  public updateSettingsTorrentRetryAttempts: number;
  public updateSettingsDeleteOnError: number;
  public updateSettingsTorrentLifetime: number;

  public updating: boolean;

  constructor(
    private activatedRoute: ActivatedRoute,
    private router: Router,
    private torrentService: TorrentService,
  ) {}

  ngOnInit(): void {
    this.activatedRoute.params.subscribe((params) => {
      const torrentId = params['id'];

      this.torrentService.get(torrentId).subscribe({
        next: (torrent) => {
          this.torrent = torrent;

          this.torrentService.update$.subscribe((result) => {
            this.update(result);
          });
        },
        error: () => this.router.navigate(['/']),
      });
    });
  }

  public update(torrents: Torrent[]): void {
    const updatedTorrent = torrents.find((m) => m.torrentId === this.torrent.torrentId);

    if (updatedTorrent == null) {
      return;
    }

    this.torrent = updatedTorrent;
  }

  public download(): void {
    const byteArray = new Uint8Array(
      window
        .atob(this.torrent.fileOrMagnet)
        .split('')
        .map(function (c) {
          return c.charCodeAt(0);
        }),
    );

    const blob = new Blob([byteArray], { type: 'application/x-bittorrent' });
    saveAs(blob, `${this.torrent.rdName}.torrent`);
  }

  public showDeleteModal(): void {
    this.deleteData = false;
    this.deleteRdTorrent = false;
    this.deleteLocalFiles = false;
    this.deleteError = null;

    this.isDeleteModalActive = true;
  }

  public deleteCancel(): void {
    this.isDeleteModalActive = false;
  }

  public deleteOk(): void {
    this.deleting = true;

    this.torrentService
      .delete(this.torrent.torrentId, this.deleteData, this.deleteRdTorrent, this.deleteLocalFiles)
      .subscribe({
        next: () => {
          this.isDeleteModalActive = false;
          this.deleting = false;

          this.router.navigate(['/']);
        },
        error: (err) => {
          this.deleteError = err.error;
          this.deleting = false;
        },
      });
  }

  public showRetryModal(): void {
    this.retryError = null;

    this.isRetryModalActive = true;
  }

  public retryCancel(): void {
    this.isRetryModalActive = false;
  }

  public retryOk(): void {
    this.retrying = true;

    this.torrentService.retry(this.torrent.torrentId).subscribe({
      next: () => {
        this.isRetryModalActive = false;
        this.retrying = false;

        this.router.navigate(['/']);
      },
      error: (err) => {
        this.retryError = err.error;
        this.retrying = false;
      },
    });
  }

  public showDownloadRetryModal(downloadId: string): void {
    this.downloadRetryId = downloadId;
    this.downloadRetryError = null;

    this.isDownloadRetryModalActive = true;
  }

  public downloadRetryCancel(): void {
    this.isDownloadRetryModalActive = false;
  }

  public downloadRetryOk(): void {
    this.downloadRetrying = true;

    this.torrentService.retryDownload(this.downloadRetryId).subscribe({
      next: () => {
        this.isDownloadRetryModalActive = false;
        this.downloadRetrying = false;
      },
      error: (err) => {
        this.downloadRetryError = err.error;
        this.downloadRetrying = false;
      },
    });
  }

  public showUpdateSettingsModal(): void {
    this.updateSettingsDownloadClient = this.torrent.downloadClient;
    this.updateSettingsHostDownloadAction = this.torrent.hostDownloadAction;
    this.updateSettingsCategory = this.torrent.category;
    this.updateSettingsPriority = this.torrent.priority;
    this.updateSettingsDownloadRetryAttempts = this.torrent.downloadRetryAttempts;
    this.updateSettingsTorrentRetryAttempts = this.torrent.torrentRetryAttempts;
    this.updateSettingsDeleteOnError = this.torrent.deleteOnError;
    this.updateSettingsTorrentLifetime = this.torrent.lifetime;

    this.isUpdateSettingsModalActive = true;
  }

  public updateSettingsCancel(): void {
    this.isUpdateSettingsModalActive = false;
  }

  public updateSettingsOk(): void {
    this.updating = true;

    this.torrent.downloadClient = this.updateSettingsDownloadClient;
    this.torrent.hostDownloadAction = this.updateSettingsHostDownloadAction;
    this.torrent.category = this.updateSettingsCategory;
    this.torrent.priority = this.updateSettingsPriority;
    this.torrent.downloadRetryAttempts = this.updateSettingsDownloadRetryAttempts;
    this.torrent.torrentRetryAttempts = this.updateSettingsTorrentRetryAttempts;
    this.torrent.deleteOnError = this.updateSettingsDeleteOnError;
    this.torrent.lifetime = this.updateSettingsTorrentLifetime;

    this.torrentService.update(this.torrent).subscribe({
      next: () => {
        this.isUpdateSettingsModalActive = false;
        this.updating = false;
      },
      error: () => {
        this.isUpdateSettingsModalActive = false;
        this.updating = false;
      },
    });
  }
  toggleDeleteSelectAllOptions() {
    this.deleteData = this.deleteSelectAll;
    this.deleteRdTorrent = this.deleteSelectAll;
    this.deleteLocalFiles = this.deleteSelectAll;
  }

  updateDeleteSelectAll() {
    this.deleteSelectAll = this.deleteData && this.deleteRdTorrent && this.deleteLocalFiles;
  }
}
