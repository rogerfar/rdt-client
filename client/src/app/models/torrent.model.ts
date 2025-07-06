import { Download } from './download.model';

export class Torrent {
  public torrentId: string;
  public hash: string;
  public category: string;
  public downloadClient: number;
  public hostDownloadAction: number;
  public downloadAction: number;
  public finishedAction: number;
  public finishedActionDelay: number;
  public downloadMinSize: number;
  public includeRegex: string;
  public excludeRegex: string;
  public downloadManualFiles: string;

  public added: Date;
  public filesSelected: Date;
  public completed: Date;

  public fileOrMagnet: string;
  public isFile: boolean;

  public retryCount: number;
  public downloadRetryAttempts: number;
  public torrentRetryAttempts: number;
  public deleteOnError: number;
  public lifetime: number;

  public priority: number;
  public error: string;

  public rdId: string;
  public rdName: string;
  public rdSize: number;
  public rdHost: string;
  public rdSplit: number;
  public rdProgress: number;
  public rdStatus: RealDebridStatus;
  public rdStatusRaw: string;
  public rdAdded: Date;
  public rdEnded: Date;
  public rdSpeed: number;
  public rdSeeders: number;
  public rdFiles: string;

  public files: TorrentFile[];
  public downloads: Download[];
}

export class TorrentFile {
  public id: string;
  public path: string;
  public bytes: number;
  public selected: boolean;

  public download: Download;
}

export class TorrentFileAvailability {
  public filename: string;
  public filesize: number;
}

export enum RealDebridStatus {
  Queued = 0,

  Processing = 1,
  WaitingForFileSelection = 2,
  Downloading = 3,
  Finished = 4,
  Uploading = 5,

  Error = 99,
}
