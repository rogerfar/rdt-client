export class Download {
  public downloadId: string;

  public torrentId: string;

  public link: string;

  public added: Date;

  public status: DownloadStatus;

  public bytesDownloaded: number;

  public bytesSize: number;

  public speed: number;
}

export enum DownloadStatus {
  PendingDownload = 0,
  Downloading = 1,
  Unpacking = 2,
  Finished = 3,
}
