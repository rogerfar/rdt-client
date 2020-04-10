import {
  Component,
  OnInit,
  OnDestroy,
  HostListener,
  ElementRef,
} from '@angular/core';
import { Torrent } from '../models/torrent.model';
import { TorrentService } from '../torrent.service';
import { DownloadStatus } from '../models/download.model';

@Component({
  selector: 'app-torrent-table',
  templateUrl: './torrent-table.component.html',
  styleUrls: ['./torrent-table.component.scss'],
})
export class TorrentTableComponent implements OnInit, OnDestroy {
  public torrents: Torrent[] = [];
  public error: string;
  public showFiles: { [key: string]: boolean } = {};

  private timer: any;

  constructor(private torrentService: TorrentService) {}

  ngOnInit(): void {
    this.timer = setInterval(() => {
      this.torrentService.getList().subscribe(
        (result) => {
          this.torrents = result;
        },
        (err) => {
          this.error = err.error;
          clearInterval(this.timer);
        }
      );
    }, 1000);
  }

  ngOnDestroy(): void {
    clearInterval(this.timer);
  }

  public selectTorrent(torrent: Torrent): void {
    this.showFiles[torrent.torrentId] = !this.showFiles[torrent.torrentId];

    if (this.showFiles[torrent.torrentId]) {
      this.torrentService.getDetails(torrent.torrentId).subscribe((result) => {
        torrent.files = result.files;
        torrent.downloads = result.downloads;

        torrent.files.forEach((file) => {
          const downloads = torrent.downloads.filter(
            (m) => m.link === file.path
          );

          if (downloads.length > 0) {
            file.download = downloads[0];
          }
        });
      });
    }
  }

  public trackByMethod(index: number, el: Torrent): string {
    return el.torrentId;
  }
}
