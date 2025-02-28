import { Pipe, PipeTransform } from '@angular/core';
import { FileSizePipe } from 'ngx-filesize';
import { Download } from './models/download.model';

@Pipe({
    name: 'downloadStatus',
    standalone: false
})
export class DownloadStatusPipe implements PipeTransform {
  constructor(private pipe: FileSizePipe) {}

  transform(value: Download): string {
    if (!value) {
      return 'Pending';
    }

    if (value.error) {
      return value.error;
    }

    if (value.completed != null) {
      return 'Finished';
    }

    if (value.unpackingFinished) {
      return 'Unpacking finished';
    }

    if (value.unpackingStarted) {
      let progress = (value.bytesDone / value.bytesTotal) * 100;

      if (isNaN(progress)) {
        progress = 0;
      }

      return `Unpacking ${progress.toFixed(2)}%`;
    }

    if (value.unpackingQueued) {
      return 'Unpacking queued';
    }

    if (value.downloadFinished) {
      return 'Download finished';
    }

    if (value.downloadStarted) {
      let progress = (value.bytesDone / value.bytesTotal) * 100;

      if (isNaN(progress)) {
        progress = 0;
      }

      const speed = this.pipe.transform(value.speed, 'filesize');

      return `Downloading ${progress.toFixed(2)}% (${speed}/s)`;
    }

    if (value.downloadQueued) {
      return 'Download queued';
    }

    return 'Pending';
  }
}
