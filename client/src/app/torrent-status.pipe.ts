import { Pipe, PipeTransform } from '@angular/core';
import { RealDebridStatus, Torrent } from './models/torrent.model';
import { FileSizePipe } from './filesize.pipe';

const fileSizePipe = new FileSizePipe();

export function getTorrentStatus(torrent: Torrent): string {
  if (torrent.error) {
    return torrent.error;
  }

  const downloads = torrent.downloads ?? [];

  if (downloads.length > 0) {
    let allFinished = true;
    let downloadingCount = 0;
    let downloadedCount = 0;
    let downloadingBytesDone = 0;
    let downloadingBytesTotal = 0;
    let downloadingSpeed = 0;
    let unpackingCount = 0;
    let unpackedCount = 0;
    let unpackingBytesDone = 0;
    let unpackingBytesTotal = 0;
    let queuedForUnpackingCount = 0;
    let queuedForDownloadingCount = 0;

    for (const download of downloads) {
      if (download.completed == null) {
        allFinished = false;
      }

      if (download.downloadFinished != null) {
        downloadedCount += 1;
      }

      if (download.downloadStarted && !download.downloadFinished && download.bytesDone > 0) {
        downloadingCount += 1;
        downloadingBytesDone += download.bytesDone;
        downloadingBytesTotal += download.bytesTotal;
        downloadingSpeed += download.speed;
      }

      if (download.unpackingFinished != null) {
        unpackedCount += 1;
      }

      if (download.unpackingStarted && !download.unpackingFinished && download.bytesDone > 0) {
        unpackingCount += 1;
        unpackingBytesDone += download.bytesDone;
        unpackingBytesTotal += download.bytesTotal;
      }

      if (download.unpackingQueued && !download.unpackingStarted) {
        queuedForUnpackingCount += 1;
      }

      if (!download.downloadStarted && !download.downloadFinished) {
        queuedForDownloadingCount += 1;
      }
    }

    if (allFinished) {
      return 'Finished';
    }

    if (downloadingCount > 0) {
      const progress = ((downloadingBytesDone / downloadingBytesTotal) || 0) * 100;
      const speed = fileSizePipe.transform(downloadingSpeed, 'filesize') as string;

      return `Downloading file ${downloadingCount + downloadedCount}/${downloads.length} (${progress.toFixed(2)}% - ${speed}/s)`;
    }

    if (unpackingCount > 0) {
      const progress = ((unpackingBytesDone / unpackingBytesTotal) || 0) * 100;

      return `Extracting file ${unpackingCount + unpackedCount}/${downloads.length} (${progress.toFixed(2)}%)`;
    }

    if (queuedForUnpackingCount > 0) {
      return 'Queued for unpacking';
    }

    if (queuedForDownloadingCount > 0) {
      return 'Queued for downloading';
    }

    if (unpackedCount > 0) {
      return 'Files unpacked';
    }

    if (downloadedCount > 0) {
      return 'Files downloaded to host';
    }
  }

  if (torrent.completed) {
    return 'Finished';
  }

  switch (torrent.rdStatus) {
    case RealDebridStatus.Queued:
      return 'Not Yet Added to Provider';
    case RealDebridStatus.Downloading:
      if (torrent.rdSeeders < 1 && torrent.type !== 1) {
        return 'Torrent stalled';
      }

      return `Torrent downloading (${torrent.rdProgress}% - ${fileSizePipe.transform(torrent.rdSpeed, 'filesize') as string}/s)`;
    case RealDebridStatus.Processing:
      return 'Torrent processing';
    case RealDebridStatus.WaitingForFileSelection:
      return 'Torrent waiting for file selection';
    case RealDebridStatus.Error:
      return `Torrent error: ${torrent.rdStatusRaw}`;
    case RealDebridStatus.Finished:
      return 'Torrent finished, waiting for download links';
    case RealDebridStatus.Uploading:
      return 'Torrent uploading';
    default:
      return 'Unknown status';
  }
}

@Pipe({ name: 'status' })
export class TorrentStatusPipe implements PipeTransform {
  transform(torrent: Torrent): string {
    return getTorrentStatus(torrent);
  }
}
