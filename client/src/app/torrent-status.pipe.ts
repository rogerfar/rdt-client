import { Pipe, PipeTransform } from '@angular/core';
import { FileSizePipe } from 'ngx-filesize';
import { DownloadStatus } from './models/download.model';
import { Torrent, TorrentStatus } from './models/torrent.model';

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

      const allBytesDownloaded = torrent.downloads.sum(
        (m) => m.bytesDownloaded
      );
      const allBytesSize = torrent.downloads.sum((m) => m.bytesSize);

      let progress = 0;
      let allSpeeds = 0;

      if (allBytesSize > 0) {
        progress = (allBytesDownloaded / allBytesSize) * 100;
        allSpeeds = downloading.sum((m) => m.speed) / downloading.length;
      }

      let speed: string | string[] = '0';
      if (allSpeeds > 0) {
        speed = this.pipe.transform(allSpeeds, 'filesize');
      }

      if (downloading.length > 0) {
        if (allBytesSize > 0) {
          return `Downloading (${progress.toFixed(2)}% - ${speed}/s)`;
        }

        return `Preparing download`;
      }

      if (unpacking.length > 0) {
        return `Unpacking (${progress.toFixed(2)}% - ${speed}/s)`;
      }

      return 'Pending download';
    }

    switch (torrent.status) {
      case TorrentStatus.RealDebrid:
        const speed = this.pipe.transform(torrent.rdSpeed, 'filesize');
        return `Torrent downloading (${torrent.rdProgress}% - ${speed}/s)`;
      case TorrentStatus.WaitingForDownload:
        return `Ready to download, press the download icon to start`;
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
