import { Component, OnInit, Input } from '@angular/core';
import { Torrent, TorrentStatus } from 'src/app/models/torrent.model';
import { TorrentService } from 'src/app/torrent.service';

@Component({
  selector: '[app-torrent-row]',
  templateUrl: './torrent-row.component.html',
  styleUrls: ['./torrent-row.component.scss'],
})
export class TorrentRowComponent implements OnInit {
  @Input()
  public torrent: Torrent;

  public loading = false;

  constructor(private torrentService: TorrentService) {}

  ngOnInit(): void {}

  public download(event: Event): void {
    event.stopPropagation();

    this.loading = true;
    this.torrentService.download(this.torrent.torrentId).subscribe(() => {
      this.loading = false;
      this.torrent.status = TorrentStatus.Downloading;
    });
  }

  public delete(event: Event): void {
    event.stopPropagation();

    this.loading = true;
    this.torrentService.delete(this.torrent.torrentId).subscribe(() => {});
  }
}
