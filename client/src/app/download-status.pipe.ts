import { Pipe, PipeTransform } from '@angular/core';
import { Download } from './models/download.model';
import { FileSizePipe } from './filesize.pipe';

@Pipe({ name: 'downloadStatus' })
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
      const progress = (value.bytesDone / value.bytesTotal || 0) * 100;

      return `Unpacking ${progress.toFixed(2)}%`;
    }

    if (value.unpackingQueued) {
      return 'Unpacking queued';
    }

    if (value.downloadFinished) {
      return 'Download finished';
    }

    if (value.downloadStarted) {
      const progress = (value.bytesDone / value.bytesTotal || 0) * 100;

      const speed = this.pipe.transform(value.speed, 'filesize');

      return `Downloading ${progress.toFixed(2)}% (${speed}/s)`;
    }

    if (value.downloadQueued) {
      return 'Download queued';
    }

    return 'Pending';
  }
}
