import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
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

  constructor(private router: Router, private torrentService: TorrentService) {}

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

  public selectTorrent(torrentId: string): void {
    this.router.navigate([`/torrent/${torrentId}`]);
  }

  public trackByMethod(index: number, el: Torrent): string {
    return el.torrentId;
  }
}
