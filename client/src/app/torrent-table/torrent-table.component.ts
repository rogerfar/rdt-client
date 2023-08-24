import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Torrent } from '../models/torrent.model';
import { TorrentService } from '../torrent.service';
import { forkJoin, Observable } from 'rxjs';

@Component({
  selector: 'app-torrent-table',
  templateUrl: './torrent-table.component.html',
  styleUrls: ['./torrent-table.component.scss'],
})
export class TorrentTableComponent implements OnInit {
  public torrent: Torrent;
  public torrents: Torrent[] = [];
  public selectedTorrents: string[] = [];
  public selectedTorrentsAsObjects: Torrent[] = []
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

  public updateSettingsCategory: string;
  public updateSettingsPriority: number;
  public updateSettingsDownloadRetryAttempts: number;
  public updateSettingsTorrentRetryAttempts: number;
  public updateSettingsDeleteOnError: number;
  public updateSettingsTorrentLifetime: number;
  
  public updateError: string;
  public updating: boolean;

  constructor(private router: Router, private torrentService: TorrentService) {}

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

  public openTorrent(torrentId: string): void {
    this.router.navigate([`/torrent/${torrentId}`]);
  }

  public trackByMethod(index: number, el: Torrent): string {
    return el.torrentId;
  }

  public toggleSelectAll(event: any) {
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
    
    let calls: Observable<void>[] = [];
    
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
 
    let calls: Observable<void>[] = [];
 
     this.selectedTorrents.forEach((torrentId) => {
     calls.push(this.torrentService.retry(torrentId));
     })
  
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
  
  public getSameValueOrNull(key: string): any {
    let newValue: any = null
    
    this.selectedTorrentsAsObjects.forEach((t, i) => {
      type ObjectKey = keyof typeof t;
      const myVar = key as ObjectKey;
      const currentValue = t[myVar]
      
      if (i == 0) {
        newValue = currentValue
      } else {
        if (newValue != currentValue) {
          newValue = null
        }
      }
    })
    return newValue
  }
  
  public showUpdateSettingsModal(): void {
    this.selectedTorrentsAsObjects = this.torrents.filter(t => this.selectedTorrents.includes(t.torrentId))
    
    this.updateSettingsCategory = this.getSameValueOrNull('category')
    this.updateSettingsPriority = this.getSameValueOrNull('priority');
    this.updateSettingsDownloadRetryAttempts = this.getSameValueOrNull('downloadRetryAttempts');
    this.updateSettingsTorrentRetryAttempts = this.getSameValueOrNull('torrentRetryAttempts');
    this.updateSettingsDeleteOnError = this.getSameValueOrNull('deleteOnError');
    this.updateSettingsTorrentLifetime = this.getSameValueOrNull('lifetime');
    
    this.isUpdateSettingsModalActive = true;
  }
  
  public updateSettingsCancel(): void {
    this.isUpdateSettingsModalActive = false;
  }
  
  public updateSettingsOk(): void {
    this.updating = true;
    
    let calls: Observable<void>[] = [];
    
    this.selectedTorrentsAsObjects.forEach((torrent) => {
    torrent.category = this.updateSettingsCategory;
    torrent.priority = this.updateSettingsPriority;
    torrent.downloadRetryAttempts = this.updateSettingsDownloadRetryAttempts;
    torrent.torrentRetryAttempts = this.updateSettingsTorrentRetryAttempts;
    torrent.deleteOnError = this.updateSettingsDeleteOnError;
    torrent.lifetime = this.updateSettingsTorrentLifetime;

      calls.push(this.torrentService.update(torrent));
    })
  

    forkJoin(calls).subscribe({
      complete: () => {
        this.isUpdateSettingsModalActive = false;
        this.updating = false;

        this.selectedTorrents = [];
      },
      error: (err) => {
        this.updateError = err.error;
        this.updating = false;
      },
    });
  }
}
