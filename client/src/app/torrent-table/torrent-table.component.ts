import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Torrent } from '../models/torrent.model';
import { TorrentService } from '../torrent.service';
import { forkJoin, Observable } from 'rxjs';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';

@Component({
  selector: 'app-torrent-table',
  templateUrl: './torrent-table.component.html',
  styleUrls: ['./torrent-table.component.scss'],
})
export class TorrentTableComponent implements OnInit {
  public torrents: Torrent[] = [];
  public selectedTorrents: Torrent[] = [];
  public error: string;

  public isDeleteModalActive: boolean;
  public deleteError: string;
  public deleting: boolean;
  public deleteData: boolean;
  public deleteRdTorrent: boolean;
  public deleteLocalFiles: boolean;

  public isRetryModalActive: boolean;
  public retryError: string;
  public retrying: boolean;
  public isUpdateSettingsModalActive: boolean;

  public updateError: string;
  public updating: boolean;

  public getSameValue(key: string): any {
    let previousValue: any = null;

    this.selectedTorrents.forEach((torrent: Torrent, i: number) => {
      type ObjectKey = keyof typeof torrent;
      const str = key as ObjectKey;
      const currentValue = torrent[str];

      if (i == 0) {
        previousValue = currentValue;
      } else {
        if (previousValue != currentValue) {
          previousValue = null;
        }
      }
    });
    return previousValue;
  }

  public updateTorrentSettingsForm: FormGroup = this.fb.group({
    category: [this.getSameValue('category') || '', [Validators.maxLength(100)]],
    priority: [this.getSameValue('priority') || '', [Validators.min(0), Validators.min(0)]],
    downloadRetryAttempts: [
      this.getSameValue('downloadRetryAttempts') || '0',
      [Validators.required, Validators.min(0), Validators.max(1000)],
    ],
    torrentRetryAttempts: [
      this.getSameValue('torrentRetryAttempts') || '0',
      [Validators.required, Validators.min(0), Validators.max(1000)],
    ],
    deleteOnError: [
      this.getSameValue('deleteOnError') || '0',
      [Validators.required, Validators.min(0), Validators.max(1000)],
    ],
    lifetime: [this.getSameValue('lifetime') || '0', [Validators.required, Validators.min(0), Validators.max(100000)]],
  });

  constructor(private router: Router, private torrentService: TorrentService, private fb: FormBuilder) {}

  ngOnInit(): void {
    this.torrentService.getList().subscribe(
      (result) => {
        this.torrents = result;

        this.torrentService.update$.subscribe((result2) => {
          this.torrents = result2;
        });
      },
      (err) => {
        this.error = err.error;
      }
    );
  }

  public openTorrent(torrent: Torrent): void {
    this.router.navigate([`/torrent/${torrent.torrentId}`]);
  }

  public trackByMethod(index: number, el: Torrent): string {
    return el.torrentId;
  }

  public getChecked(torrent: Torrent) {
    const index = this.selectedTorrents.findIndex((selected) => selected.torrentId === torrent.torrentId);
    if (index > -1) {
      return true;
    }
    return false;
  }

  public toggleSelectAll(event: any) {
    this.selectedTorrents = [];

    if (event.target.checked) {
      this.torrents.map((torrent) => {
        this.selectedTorrents.push(torrent);
      });
    }
  }

  public toggleSelect(torrent: Torrent) {
    const index = this.selectedTorrents.findIndex((selected) => selected.torrentId === torrent.torrentId);

    if (index > -1) {
      this.selectedTorrents.splice(index, 1);
    } else {
      this.selectedTorrents.push(torrent);
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

    let calls: Observable<void>[] = [];

    this.selectedTorrents.forEach((torrent) => {
      calls.push(
        this.torrentService.delete(torrent.torrentId, this.deleteData, this.deleteRdTorrent, this.deleteLocalFiles)
      );
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

    let calls: Observable<void>[] = [];

    this.selectedTorrents.forEach((torrent) => {
      calls.push(this.torrentService.retry(torrent.torrentId));
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

  public updateSettingsResetAll(): void {
    this.updateTorrentSettingsForm.reset();
    this.updateTorrentSettingsForm.patchValue({
      category: this.getSameValue('category'),
      priority: this.getSameValue('priority'),
      downloadRetryAttempts: this.getSameValue('downloadRetryAttempts'),
      torrentRetryAttempts: this.getSameValue('torrentRetryAttempts'),
      deleteOnError: this.getSameValue('deleteOnError'),
      lifetime: this.getSameValue('lifetime'),
    });
  }

  public showUpdateSettingsModal(): void {
    this.updateSettingsResetAll();
    this.isUpdateSettingsModalActive = true;
  }

  public updateSettingsCancel(): void {
    this.isUpdateSettingsModalActive = false;
    this.updateTorrentSettingsForm.reset();
  }

  public getUpdatedValue(original: any, key: string): string {
    type ObjectKey = keyof typeof original;
    const str: any = key as ObjectKey;
    const fieldIsPristine = this.updateTorrentSettingsForm.controls[str].pristine;
    const originalValue = original[str];
    const updatedValue = this.updateTorrentSettingsForm.value[str];

    if (fieldIsPristine) {
      return originalValue;
    }
    return updatedValue;
  }

  public updateSettingsOk(): void {
    this.updating = true;

    let calls: Observable<void>[] = [];

    this.selectedTorrents.forEach((torrent) => {
      (torrent.category = this.getUpdatedValue(torrent, 'category')),
        (torrent.priority = parseInt(this.getUpdatedValue(torrent, 'priority'))),
        (torrent.downloadRetryAttempts = parseInt(this.getUpdatedValue(torrent, 'downloadRetryAttempts'))),
        (torrent.torrentRetryAttempts = parseInt(this.getUpdatedValue(torrent, 'torrentRetryAttempts'))),
        (torrent.deleteOnError = parseInt(this.getUpdatedValue(torrent, 'deleteOnError'))),
        (torrent.lifetime = parseInt(this.getUpdatedValue(torrent, 'lifetime')));

      calls.push(this.torrentService.update(torrent));
    });

    forkJoin(calls).subscribe({
      complete: () => {
        this.isUpdateSettingsModalActive = false;
        this.updating = false;
        this.updateTorrentSettingsForm.reset();

        this.selectedTorrents = [];
      },
      error: (err) => {
        this.updateError = err.error;
        this.updating = false;
      },
    });
  }
}
