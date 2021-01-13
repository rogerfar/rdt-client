import { Component, Input, OnInit } from '@angular/core';
import { Download } from '../models/download.model';

@Component({
  selector: '[app-torrent-download]',
  templateUrl: './torrent-download.component.html',
  styleUrls: ['./torrent-download.component.scss'],
})
export class TorrentDownloadComponent implements OnInit {
  @Input()
  public download: Download;

  constructor() {}

  ngOnInit(): void {

  }
}
