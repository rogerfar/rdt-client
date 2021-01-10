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

    if (torrent.downloads && torrent.downloads.length > 0) {
      const allFinished = torrent.downloads.all((m) => m.completed != null);
      if (allFinished) {
        return 'Finished';
      }

      const downloading = torrent.downloads.where((m) => m.downloadStarted && !m.downloadFinished);
      const unpacking = torrent.downloads.where((m) => m.unpackingStarted && !m.unpackingFinished);

      let downloadText = '';
      let unpackText = '';

      if (downloading.length > 0) {
        const bytesDone = downloading.sum((m) => m.bytesDone);
        const bytesTotal = downloading.sum((m) => m.bytesTotal);
        let progress = (bytesDone / bytesTotal) * 100;
        let allSpeeds = downloading.sum((m) => m.speed) / downloading.length;

        let speed: string | string[] = '0';
        if (allSpeeds > 0) {
          speed = this.pipe.transform(allSpeeds, 'filesize');

          downloadText = `Downloading (${progress.toFixed(2)}% - ${speed}/s)`;
        }
      }

      if (unpacking.length > 0) {
        const bytesDone = unpacking.sum((m) => m.bytesDone);
        const bytesTotal = unpacking.sum((m) => m.bytesTotal);
        let progress = (bytesDone / bytesTotal) * 100;
        let allSpeeds = unpacking.sum((m) => m.speed) / unpacking.length;

        if (allSpeeds > 0) {
          downloadText = `Extracting (${progress.toFixed(2)}%)`;
        }
      }

      let result: string[] = [];
      if (downloadText) {
        result.push(downloadText);
      }
      if (unpackText) {
        result.push(unpackText);
      }

      if (result.length > 0) {
        return result.join('\r\n');
      }
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
