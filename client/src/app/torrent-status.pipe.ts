import { Pipe, PipeTransform } from '@angular/core';
import { FileSizePipe } from 'ngx-filesize';
import { RealDebridStatus, Torrent } from './models/torrent.model';

@Pipe({
  name: 'status',
})
export class TorrentStatusPipe implements PipeTransform {
  constructor(private pipe: FileSizePipe) {}

  transform(torrent: Torrent): string {
    if (torrent.completed) {
      return 'Finished';
    }

    const allFinished = torrent.downloads.all((m) => m.completed != null);

    if (allFinished) {
      return 'Finished';
    }

    const errors = torrent.downloads.where((m) => m.error != null);

    if (errors.length > 0) {
      return 'Error';
    }

    const downloading = torrent.downloads.where((m) => m.downloadStarted && !m.downloadFinished);

    if (downloading.length > 0) {
      const bytesDone = downloading.sum((m) => m.bytesDone);
      const bytesTotal = downloading.sum((m) => m.bytesTotal);
      let progress = (bytesDone / bytesTotal) * 100;
      let allSpeeds = downloading.sum((m) => m.speed) / downloading.length;

      let speed: string | string[] = '0';
      if (allSpeeds > 0) {
        speed = this.pipe.transform(allSpeeds, 'filesize');

        return `Downloading (${progress.toFixed(2)}% - ${speed}/s)`;
      }
    }

    const unpacking = torrent.downloads.where((m) => m.unpackingStarted && !m.unpackingFinished);

    if (unpacking.length > 0) {
      const bytesDone = unpacking.sum((m) => m.bytesDone);
      const bytesTotal = unpacking.sum((m) => m.bytesTotal);
      let progress = (bytesDone / bytesTotal) * 100;
      let allSpeeds = unpacking.sum((m) => m.speed) / unpacking.length;

      if (allSpeeds > 0) {
        return `Extracting (${progress.toFixed(2)}%)`;
      }
    }

    const queuedForUnpacking = torrent.downloads.where((m) => m.unpackingQueued && !m.unpackingStarted);

    if (queuedForUnpacking.length > 0) {
      return `Queued for unpacking`;
    }

    const queuedForDownload = torrent.downloads.where((m) => m.downloadQueued && !m.downloadStarted);

    if (queuedForDownload.length > 0) {
      return `Queued for downloading`;
    }

    const unpacked = torrent.downloads.where((m) => m.unpackingFinished != null);

    if (unpacked.length > 0) {
      return `Files unpacked`;
    }

    const downloaded = torrent.downloads.where((m) => m.downloadFinished != null);

    if (downloaded.length > 0) {
      return `Files downloaded to host`;
    }

    switch (torrent.rdStatus) {
      case RealDebridStatus.Downloading:
        const speed = this.pipe.transform(torrent.rdSpeed, 'filesize');
        return `Torrent downloading (${torrent.rdProgress}% - ${speed}/s)`;
      case RealDebridStatus.Processing:
        return `Torrent processing`;
      case RealDebridStatus.WaitingForFileSelection:
        return `Torrent waiting for file selection`;
      case RealDebridStatus.Error:
        return `Torrent error: ${torrent.rdStatusRaw}`;
      case RealDebridStatus.Finished:
        return `Torrent finished`;
      default:
        return 'Unknown status';
    }
  }
}
