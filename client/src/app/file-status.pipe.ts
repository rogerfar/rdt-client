import { Pipe, PipeTransform } from '@angular/core';
import { FileSizePipe } from 'ngx-filesize';
import { TorrentFile } from './models/torrent.model';

@Pipe({
  name: 'fileStatus',
})
export class FileStatusPipe implements PipeTransform {
  constructor(private pipe: FileSizePipe) {}

  transform(value: TorrentFile): string {
    if (!value.download) {
      return 'Pending download';
    }

    if (value.download.error) {
      return `Error: ${value.download.error}`;
    }

    if (value.download.completed != null) {
      return 'Finished';
    }

    if (value.download.unpackingFinished) {
      return 'Unpacking finished';
    }

    if (value.download.unpackingStarted) {
      const progress = ((value.download.bytesDone / value.download.bytesTotal) * 100).toFixed(2);
      return `Unpacking ${progress || 0}%`;
    }

    if (value.download.unpackingQueued) {
      return 'Unpacking queued';
    }

    if (value.download.downloadFinished) {
      return 'Download finished';
    }

    if (value.download.downloadStarted) {
      const progress = ((value.download.bytesDone / value.download.bytesTotal) * 100).toFixed(2);
      const speed = this.pipe.transform(value.download.speed, 'filesize');

      return `Downloading ${progress || 0}% (${speed}/s)`;
    }

    if (value.download.downloadQueued) {
      return 'Download queued';
    }

    if (value.download.added) {
      return 'Pending';
    }

    return '';
  }
}
