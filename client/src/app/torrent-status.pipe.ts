import { Pipe, PipeTransform } from '@angular/core';
import { Torrent, TorrentStatus } from './models/torrent.model';
import { FileSizePipe } from 'ngx-filesize';

@Pipe({
  name: 'status',
})
export class TorrentStatusPipe implements PipeTransform {
  constructor(private pipe: FileSizePipe) {}

  transform(torrent: Torrent): string {
    switch (torrent.status) {
      case TorrentStatus.RealDebrid: {
        const speed = this.pipe.transform(torrent.rdSpeed, 'filesize');
        return `Downloading from RD (${torrent.rdProgress}% - ${speed}/s)`;
      }
      case TorrentStatus.WaitingForDownload:
        return `Waiting to download`;
      case TorrentStatus.Downloading: {
        if (torrent.activeDownload != null) {
          const speed = this.pipe.transform(
            torrent.activeDownload.speed,
            'filesize'
          );
          return `Downloading (${torrent.activeDownload.progress}% - ${speed}/s)`;
        }
        return `Downloading`;
      }
      case TorrentStatus.Finished:
        return `Finished`;
      case TorrentStatus.Error:
        return `Error`;
      default:
        return 'Unknown status';
    }
  }
}
