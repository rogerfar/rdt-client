import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { Torrent } from 'src/app/models/torrent.model';

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

  @Output('retry')
  public retry = new EventEmitter();

  public loading = false;

  constructor() {}

  ngOnInit(): void {}

  public deleteClick(event: Event): void {
    event.stopPropagation();
    this.delete.emit(this.torrent.torrentId);
  }

  public retryClick(event: Event): void {
    event.stopPropagation();
    this.retry.emit(this.torrent.torrentId);
  }
}
