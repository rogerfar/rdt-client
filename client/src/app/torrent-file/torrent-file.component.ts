import { Component, OnInit, Input } from '@angular/core';
import { Torrent, TorrentFile } from '../models/torrent.model';
import { TorrentService } from '../torrent.service';

@Component({
  selector: '[app-torrent-file]',
  templateUrl: './torrent-file.component.html',
  styleUrls: ['./torrent-file.component.scss'],
})
export class TorrentFileComponent implements OnInit {
  @Input()
  public file: TorrentFile;

  constructor(private torrentService: TorrentService) {}

  ngOnInit(): void {}
}
