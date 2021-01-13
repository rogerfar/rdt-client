import { Component, Input, OnInit } from '@angular/core';
import { TorrentFile } from '../models/torrent.model';

@Component({
  selector: '[app-torrent-file]',
  templateUrl: './torrent-file.component.html',
  styleUrls: ['./torrent-file.component.scss'],
})
export class TorrentFileComponent implements OnInit {
  @Input()
  public file: TorrentFile;

  constructor() {}

  ngOnInit(): void {}
}
