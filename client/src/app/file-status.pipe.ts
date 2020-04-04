import { Pipe, PipeTransform } from '@angular/core';
import { TorrentFile } from './models/torrent.model';
import { DownloadStatus } from './models/download.model';

@Pipe({
  name: 'fileStatus',
})
export class FileStatusPipe implements PipeTransform {
  transform(value: TorrentFile): string {
    if (
      !value.download ||
      value.download.status === DownloadStatus.PendingDownload
    ) {
      return 'Pending';
    }

    if (value.download.status === DownloadStatus.Downloading) {
      return `${value.download.progress}%`;
    }

    if (value.download.status === DownloadStatus.Finished) {
      return `Finished`;
    }
  }
}
