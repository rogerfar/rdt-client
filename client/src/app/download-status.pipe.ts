import { Pipe, PipeTransform, inject } from '@angular/core';
import { Download } from './models/download.model';
import { FileSizePipe } from './filesize.pipe';

@Pipe({ name: 'downloadStatus' })
export class DownloadStatusPipe implements PipeTransform {
  private pipe = inject(FileSizePipe);

  transform(value: Download, retryAttempts?: number): string {
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
      const speed = this.pipe.transform(value.speed, 'filesize');

      // Some debrid servers send no Content-Length, so the total is unknown until the file is complete.
      if (!(value.bytesTotal > 0)) {
        return `Downloading ${this.pipe.transform(value.bytesDone, 'filesize')} (${speed}/s)`;
      }

      const progress = (value.bytesDone / value.bytesTotal) * 100;

      return `Downloading ${progress.toFixed(2)}% (${speed}/s)`;
    }

    if (value.downloadQueued) {
      const retryAt = new Date(value.downloadQueued);

      if (retryAt.getTime() > Date.now()) {
        const time = retryAt.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        // Temporary errors (host maintenance, busy debrid servers) get more attempts than the item's setting; the
        // server says how many this download gets.
        const max = value.retryAttempts || retryAttempts;
        const attempt = max ? ` (${value.retryCount}/${max})` : ` (retry ${value.retryCount})`;

        return value.lastError
          ? `Retrying at ${time}${attempt} · ${value.lastError}`
          : `Retrying at ${time}${attempt}`;
      }

      return value.lastError ? `Download queued · last error: ${value.lastError}` : 'Download queued';
    }

    return 'Pending';
  }
}
