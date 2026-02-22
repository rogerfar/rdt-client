import { HttpClient } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';
import { Torrent, TorrentFileAvailability } from './models/torrent.model';
import { DiskSpaceStatus } from './models/disk-space-status.model';
import { RateLimitStatus } from './models/rate-limit-status.model';
import { APP_BASE_HREF } from '@angular/common';

@Injectable({
  providedIn: 'root',
})
export class TorrentService {
  public update$: Subject<Torrent[]> = new Subject();
  public diskSpaceStatus$: Subject<DiskSpaceStatus> = new Subject();
  public rateLimitStatus$: Subject<RateLimitStatus> = new Subject();

  private connection: signalR.HubConnection;

  constructor(
    private http: HttpClient,
    @Inject(APP_BASE_HREF) private baseHref: string,
  ) {
    this.connect();
  }

  public connect(): void {
    if (this.connection != null) {
      return;
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${this.baseHref}hub`)
      .withAutomaticReconnect()
      .build();

    this.connection.on('update', (torrents: Torrent[]) => {
      this.update$.next(torrents);
    });

    this.connection.on('diskSpaceStatus', (status: any) => {
      this.diskSpaceStatus$.next(status);
    });

    this.connection.on('rateLimitStatus', (status: any) => {
      this.rateLimitStatus$.next(status);
    });

    this.connection.onreconnected(() => {
      this.getDiskSpaceStatus().subscribe({
        next: (status) => {
          if (status) {
            this.diskSpaceStatus$.next(status);
          }
        },
      });
    });

    this.connection.start().catch((err) => console.error(err));
  }

  public getList(): Observable<Torrent[]> {
    return this.http.get<Torrent[]>(`${this.baseHref}Api/Torrents`);
  }

  public get(torrentId: string): Observable<Torrent> {
    return this.http.get<Torrent>(`${this.baseHref}Api/Torrents/Get/${torrentId}`);
  }

  public getDiskSpaceStatus(): Observable<DiskSpaceStatus | null> {
    return this.http.get<DiskSpaceStatus | null>(`${this.baseHref}Api/Torrents/DiskSpaceStatus`);
  }

  public getRateLimitStatus(): Observable<RateLimitStatus | null> {
    return this.http.get<RateLimitStatus | null>(`${this.baseHref}Api/Torrents/RateLimitStatus`);
  }

  public uploadMagnet(magnetLink: string, torrent: Torrent): Observable<void> {
    return this.http.post<void>(`${this.baseHref}Api/Torrents/UploadMagnet`, {
      magnetLink,
      torrent,
    });
  }

  public uploadFile(file: File, torrent: Torrent): Observable<void> {
    const formData: FormData = new FormData();
    formData.append('file', file);
    formData.append('formData', JSON.stringify({ torrent }));
    return this.http.post<void>(`${this.baseHref}Api/Torrents/UploadFile`, formData);
  }

  public uploadNzbLink(nzbLink: string, torrent: Torrent): Observable<void> {
    return this.http.post<void>(`${this.baseHref}Api/Torrents/UploadNzbLink`, {
      nzbLink,
      torrent,
    });
  }

  public uploadNzbFile(file: File, torrent: Torrent): Observable<void> {
    const formData: FormData = new FormData();
    formData.append('file', file);
    formData.append('formData', JSON.stringify({ torrent }));
    return this.http.post<void>(`${this.baseHref}Api/Torrents/UploadNzbFile`, formData);
  }

  public checkFilesMagnet(magnetLink: string): Observable<TorrentFileAvailability[]> {
    return this.http.post<TorrentFileAvailability[]>(`${this.baseHref}Api/Torrents/CheckFilesMagnet`, {
      magnetLink,
    });
  }

  public checkFiles(file: File): Observable<TorrentFileAvailability[]> {
    const formData: FormData = new FormData();
    formData.append('file', file);
    return this.http.post<TorrentFileAvailability[]>(`${this.baseHref}Api/Torrents/CheckFiles`, formData);
  }

  public delete(
    torrentId: string,
    deleteData: boolean,
    deleteRdTorrent: boolean,
    deleteLocalFiles: boolean,
  ): Observable<void> {
    return this.http.post<void>(`${this.baseHref}Api/Torrents/Delete/${torrentId}`, {
      deleteData,
      deleteRdTorrent,
      deleteLocalFiles,
    });
  }

  public retry(torrentId: string): Observable<void> {
    return this.http.post<void>(`${this.baseHref}Api/Torrents/Retry/${torrentId}`, {});
  }

  public retryDownload(downloadId: string): Observable<void> {
    return this.http.post<void>(`${this.baseHref}Api/Torrents/RetryDownload/${downloadId}`, {});
  }

  public update(torrent: Torrent): Observable<void> {
    return this.http.put<void>(`${this.baseHref}Api/Torrents/Update`, torrent);
  }

  public verifyRegex(
    includeRegex: string,
    excludeRegex: string,
    magnetLink: string,
  ): Observable<{ includeError: string; excludeError: string; selectedFiles: TorrentFileAvailability[] }> {
    return this.http.post<{ includeError: string; excludeError: string; selectedFiles: TorrentFileAvailability[] }>(
      `${this.baseHref}Api/Torrents/VerifyRegex`,
      {
        includeRegex,
        excludeRegex,
        magnetLink,
      },
    );
  }
}
