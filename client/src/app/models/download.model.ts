export class Download {
  public downloadId: string;

  public torrentId: string;

  public link: string;

  public added: Date;

  public status: DownloadStatus;

  public progress: number;
}

export enum DownloadStatus {
  PendingDownload = 0,
  Downloading,
  Finished,
}
