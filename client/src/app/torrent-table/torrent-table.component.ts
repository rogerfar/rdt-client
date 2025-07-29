import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Torrent } from '../models/torrent.model';
import { TorrentService } from '../torrent.service';
import { forkJoin, Observable } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { NgClass, DecimalPipe, DatePipe } from '@angular/common';
import { TorrentStatusPipe } from '../torrent-status.pipe';
import { SortPipe } from '../sort.pipe';
import { FileSizePipe } from '../filesize.pipe';

@Component({
  selector: 'app-torrent-table',
  templateUrl: './torrent-table.component.html',
  styleUrls: ['./torrent-table.component.scss'],
  imports: [FormsModule, NgClass, DecimalPipe, DatePipe, TorrentStatusPipe, SortPipe, FileSizePipe],
  standalone: true,
})
export class TorrentTableComponent implements OnInit {
  public torrents: Torrent[] = [];
  public selectedTorrents: string[] = [];
  public error: string;
  public sortProperty = 'rdName';
  public sortDirection: 'asc' | 'desc' = 'asc';

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

  public isChangeSettingsModalActive: boolean;
  public changeSettingsError: string;
  public changingSettings: boolean;

  public updateSettingsDownloadClient: number;
  public updateSettingsHostDownloadAction: number;
  public updateSettingsCategory: string;
  public updateSettingsPriority: number;
  public updateSettingsDownloadRetryAttempts: number;
  public updateSettingsTorrentRetryAttempts: number;
  public updateSettingsDeleteOnError: number;
  public updateSettingsTorrentLifetime: number;

  constructor(
    private router: Router,
    private torrentService: TorrentService,
  ) {}

  ngOnInit(): void {
    this.torrentService.getList().subscribe({
      next: (result) => {
        this.torrents = result;

        this.torrentService.update$.subscribe((result2) => {
          this.torrents = result2;
        });
      },
      error: (err) => {
        this.error = err.error;
      },
    });
  }

  public sort(property: string): void {
    this.sortProperty = property;
    this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
  }

  public openTorrent(torrentId: string): void {
    this.router.navigate([`/torrent/${torrentId}`]);
  }

  public toggleDeleteSelectAll(event: any) {
    this.selectedTorrents = [];

    if (event.target.checked) {
      this.torrents.map((torrent) => {
        this.selectedTorrents.push(torrent.torrentId);
      });
    }
  }

  public toggleSelect(torrentId: string) {
    const index = this.selectedTorrents.indexOf(torrentId);

    if (index > -1) {
      this.selectedTorrents.splice(index, 1);
    } else {
      this.selectedTorrents.push(torrentId);
    }
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

    const calls: Observable<void>[] = [];

    this.selectedTorrents.forEach((torrentId) => {
      calls.push(this.torrentService.delete(torrentId, this.deleteData, this.deleteRdTorrent, this.deleteLocalFiles));
    });

    forkJoin(calls).subscribe({
      complete: () => {
        this.isDeleteModalActive = false;
        this.deleting = false;

        this.selectedTorrents = [];
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

    const calls: Observable<void>[] = [];

    this.selectedTorrents.forEach((torrentId) => {
      calls.push(this.torrentService.retry(torrentId));
    });

    forkJoin(calls).subscribe({
      complete: () => {
        this.isRetryModalActive = false;
        this.retrying = false;

        this.selectedTorrents = [];
      },
      error: (err) => {
        this.retryError = err.error;
        this.retrying = false;
      },
    });
  }

  public changeSettingsModal(): void {
    this.changeSettingsError = null;

    const selectedTorrents = this.torrents.filter((m) => this.selectedTorrents.indexOf(m.torrentId) > -1);

    this.updateSettingsDownloadClient = selectedTorrents.every(
      (m, _, arr) => m.downloadClient === arr[0].downloadClient,
    )
      ? selectedTorrents[0].downloadClient
      : null;
    this.updateSettingsHostDownloadAction = selectedTorrents.every(
      (m, _, arr) => m.hostDownloadAction === arr[0].hostDownloadAction,
    )
      ? selectedTorrents[0].hostDownloadAction
      : null;
    this.updateSettingsCategory = selectedTorrents.every((m, _, arr) => m.category === arr[0].category)
      ? selectedTorrents[0].category
      : null;
    this.updateSettingsPriority = selectedTorrents.every((m, _, arr) => m.priority === arr[0].priority)
      ? selectedTorrents[0].priority
      : null;
    this.updateSettingsDownloadRetryAttempts = selectedTorrents.every(
      (m, _, arr) => m.downloadRetryAttempts === arr[0].downloadRetryAttempts,
    )
      ? selectedTorrents[0].downloadRetryAttempts
      : null;
    this.updateSettingsTorrentRetryAttempts = selectedTorrents.every(
      (m, _, arr) => m.torrentRetryAttempts === arr[0].torrentRetryAttempts,
    )
      ? selectedTorrents[0].torrentRetryAttempts
      : null;
    this.updateSettingsDeleteOnError = selectedTorrents.every((m, _, arr) => m.deleteOnError === arr[0].deleteOnError)
      ? selectedTorrents[0].deleteOnError
      : null;
    this.updateSettingsTorrentLifetime = selectedTorrents.every((m, _, arr) => m.lifetime === arr[0].lifetime)
      ? selectedTorrents[0].lifetime
      : null;

    this.isChangeSettingsModalActive = true;
  }

  public changeSettingsCancel(): void {
    this.isChangeSettingsModalActive = false;
  }

  public changeSettingsOk(): void {
    this.changingSettings = true;

    const calls: Observable<void>[] = [];

    const selectedTorrents = this.torrents.filter((m) => this.selectedTorrents.indexOf(m.torrentId) > -1);

    selectedTorrents.forEach((torrent) => {
      if (this.updateSettingsDownloadClient != null) {
        torrent.downloadClient = this.updateSettingsDownloadClient;
      }
      if (this.updateSettingsHostDownloadAction != null) {
        torrent.hostDownloadAction = this.updateSettingsHostDownloadAction;
      }
      if (this.updateSettingsCategory != null) {
        torrent.category = this.updateSettingsCategory;
      }
      if (this.updateSettingsPriority != null) {
        torrent.priority = this.updateSettingsPriority;
      }
      if (this.updateSettingsDownloadRetryAttempts != null) {
        torrent.retryCount = this.updateSettingsDownloadRetryAttempts;
      }
      if (this.updateSettingsTorrentRetryAttempts != null) {
        torrent.torrentRetryAttempts = this.updateSettingsTorrentRetryAttempts;
      }
      if (this.updateSettingsDeleteOnError != null) {
        torrent.deleteOnError = this.updateSettingsDeleteOnError;
      }
      if (this.updateSettingsTorrentLifetime != null) {
        torrent.lifetime = this.updateSettingsTorrentLifetime;
      }

      calls.push(this.torrentService.update(torrent));
    });

    forkJoin(calls).subscribe({
      complete: () => {
        this.isChangeSettingsModalActive = false;
        this.changingSettings = false;

        this.selectedTorrents = [];
      },
      error: (err) => {
        this.changeSettingsError = err.error;
        this.changingSettings = false;
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
