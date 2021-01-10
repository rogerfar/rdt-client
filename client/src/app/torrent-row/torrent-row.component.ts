import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { Torrent } from 'src/app/models/torrent.model';
import { TorrentService } from 'src/app/torrent.service';

@Component({
  selector: '[app-torrent-row]',
  templateUrl: './torrent-row.component.html',
  styleUrls: ['./torrent-row.component.scss'],
})
export class TorrentRowComponent implements OnInit {
  @Input()
  public torrent: Torrent;

  @Output('delete')
  public delete = new EventEmitter();

  public loading = false;

  constructor(private torrentService: TorrentService) {}

  ngOnInit(): void {}

  public download(event: Event): void {
    event.stopPropagation();

    this.loading = true;
    this.torrentService.download(this.torrent.torrentId).subscribe(() => {
      this.loading = false;
    });
  }

  public delete1(event: Event): void {
    event.stopPropagation();
    this.delete.emit(this.torrent.torrentId);
  }
}
