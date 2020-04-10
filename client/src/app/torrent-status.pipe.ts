import { Pipe, PipeTransform } from '@angular/core';
import { Torrent, TorrentStatus } from './models/torrent.model';
import { FileSizePipe } from 'ngx-filesize';
import { DownloadStatus } from './models/download.model';

@Pipe({
  name: 'status',
})
export class TorrentStatusPipe implements PipeTransform {
  constructor(private pipe: FileSizePipe) {}

  transform(torrent: Torrent): string {
    if (torrent.downloads && torrent.downloads.length > 0) {
      const allFinished = torrent.downloads.all(
        (m) => m.status === DownloadStatus.Finished
      );
      if (allFinished) {
        return 'Finished';
      }

      const downloading = torrent.downloads.where(
        (m) => m.status === DownloadStatus.Downloading
      );
      const unpacking = torrent.downloads.where(
        (m) => m.status === DownloadStatus.Unpacking
      );

      if (downloading.length > 0) {
        const allBytesDownloaded = torrent.downloads.sum(
          (m) => m.bytesDownloaded
        );
        const allBytesSize = torrent.downloads.sum((m) => m.bytesSize);

        if (allBytesSize > 0) {
          const progress = ((allBytesDownloaded / allBytesSize) * 100).toFixed(
            2
          );

          const allSpeeds =
            downloading.sum((m) => m.speed) / downloading.length;
          const speed = this.pipe.transform(allSpeeds, 'filesize');

          return `Downloading (${progress || 0}% - ${speed}/s)`;
        }

        return `Preparing download`;
      }

      if (unpacking.length > 0) {
        return `Unpacking`;
      }

      return 'Pending download';
    }

    switch (torrent.status) {
      case TorrentStatus.RealDebrid:
        const speed = this.pipe.transform(torrent.rdSpeed, 'filesize');
        return `Torrent downloading (${torrent.rdProgress}% - ${speed}/s)`;
      case TorrentStatus.WaitingForDownload:
        return `Waiting to download`;
      case TorrentStatus.DownloadQueued:
        return `Download queued`;
      case TorrentStatus.Downloading:
        return `Downloading`;
      case TorrentStatus.Finished:
        return `Finished`;
      case TorrentStatus.Error:
        return `Error`;
      default:
        return 'Unknown status';
    }
  }
}
