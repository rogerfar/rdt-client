export class Download {
  public downloadId: string;

  public torrentId: string;

  public link: string;

  public added: Date;

  public status: DownloadStatus;

  public progress: number;

  public speed: number;
}

export enum DownloadStatus {
  PendingDownload = 0,
  Downloading,
  Finished,
}
