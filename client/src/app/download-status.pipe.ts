import { Pipe, PipeTransform } from '@angular/core';
import { FileSizePipe } from 'ngx-filesize';
import { Download } from './models/download.model';

@Pipe({
  name: 'downloadStatus',
})
export class DownloadStatusPipe implements PipeTransform {
  constructor(private pipe: FileSizePipe) {}

  transform(value: Download): string {
    if (!value) {
      return 'Pending';
    }

    if (value.error) {
      return `Error: ${value.error}`;
    }

    if (value.completed != null) {
      return 'Finished';
    }

    if (value.unpackingFinished) {
      return 'Unpacking finished';
    }

    if (value.unpackingStarted) {
      const progress = ((value.bytesDone / value.bytesTotal) * 100).toFixed(2);
      return `Unpacking ${progress || 0}%`;
    }

    if (value.unpackingQueued) {
      return 'Unpacking queued';
    }

    if (value.downloadFinished) {
      return 'Download finished';
    }

    if (value.downloadStarted) {
      const progress = ((value.bytesDone / value.bytesTotal) * 100).toFixed(2);
      const speed = this.pipe.transform(value.speed, 'filesize');

      return `Downloading ${progress || 0}% (${speed}/s)`;
    }

    if (value.downloadQueued) {
      return 'Download queued';
    }

    return 'Pending';
  }
}
