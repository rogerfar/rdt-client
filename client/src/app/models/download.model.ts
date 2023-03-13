export class Download {
  public downloadId: string;
  public torrentId: string;
  public path: string;
  public link: string;
  public added: Date;
  public downloadQueued: Date;
  public downloadStarted: Date;
  public downloadFinished: Date;
  public unpackingQueued: Date;
  public unpackingStarted: Date;
  public unpackingFinished: Date;
  public completed: Date;
  public error: string;
  public bytesTotal: number;
  public bytesDone: number;
  public speed: number;
  public retryCount: number;
}
