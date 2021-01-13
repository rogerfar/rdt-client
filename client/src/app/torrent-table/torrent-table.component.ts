import { Component, OnDestroy, OnInit } from '@angular/core';
import { Torrent } from '../models/torrent.model';
import { TorrentService } from '../torrent.service';

@Component({
  selector: 'app-torrent-table',
  templateUrl: './torrent-table.component.html',
  styleUrls: ['./torrent-table.component.scss'],
})
export class TorrentTableComponent implements OnInit, OnDestroy {
  public torrents: Torrent[] = [];
  public error: string;
  public showFiles: { [key: string]: boolean } = {};

  public isDeleteModalActive: boolean;
  public deleteError: string;
  public deleting: boolean;
  public deleteTorrentId: string;
  public deleteData: boolean;
  public deleteRdTorrent: boolean;
  public deleteLocalFiles: boolean;

  constructor(private torrentService: TorrentService) {}

  ngOnInit(): void {
    this.torrentService.getList().subscribe(
      (result) => {
        this.torrents = result;

        this.torrentService.connect();

        this.torrentService.update$.subscribe((result2) => {
          this.torrents = result2;
        });
      },
      (err) => {
        this.error = err.error;
      }
    );
  }

  ngOnDestroy(): void {
    this.torrentService.disconnect();
  }

  public selectTorrent(torrent: Torrent): void {
    this.showFiles[torrent.torrentId] = !this.showFiles[torrent.torrentId];
  }

  public trackByMethod(index: number, el: Torrent): string {
    return el.torrentId;
  }

  public showDeleteModal(torrentId: string): void {
    this.deleteTorrentId = torrentId;
    this.isDeleteModalActive = true;
  }

  public deleteCancel(): void {
    this.isDeleteModalActive = false;
  }

  public deleteOk(): void {
    this.deleting = true;

    this.torrentService
      .delete(this.deleteTorrentId, this.deleteData, this.deleteRdTorrent, this.deleteLocalFiles)
      .subscribe(
        () => {
          this.isDeleteModalActive = false;
          this.deleting = false;
        },
        (err) => {
          this.deleteError = err.error;
          this.deleting = false;
        }
      );
  }
}
