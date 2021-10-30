import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';
import { Torrent, TorrentFileAvailability } from './models/torrent.model';

@Injectable({
  providedIn: 'root',
})
export class TorrentService {
  public update$: Subject<Torrent[]> = new Subject();

  private connection: signalR.HubConnection;

  constructor(private http: HttpClient) {
    this.connect();
  }

  public connect(): void {
    if (this.connection != null) {
      return;
    }

    this.connection = new signalR.HubConnectionBuilder().withUrl('/hub').withAutomaticReconnect().build();
    this.connection.start().catch((err) => console.error(err));

    this.connection.on('update', (torrents: Torrent[]) => {
      this.update$.next(torrents);
    });
  }

  public getList(): Observable<Torrent[]> {
    return this.http.get<Torrent[]>(`/Api/Torrents`);
  }

  public get(torrentId: string): Observable<Torrent> {
    return this.http.get<Torrent>(`/Api/Torrents/Get/${torrentId}`);
  }

  public uploadMagnet(magnetLink: string, torrent: Torrent): Observable<void> {
    return this.http.post<void>(`/Api/Torrents/UploadMagnet`, {
      magnetLink,
      torrent,
    });
  }

  public uploadFile(file: File, torrent: Torrent): Observable<void> {
    const formData: FormData = new FormData();
    formData.append('file', file);
    formData.append('formData', JSON.stringify(torrent));
    return this.http.post<void>(`/Api/Torrents/UploadFile`, formData);
  }

  public checkFilesMagnet(magnetLink: string): Observable<TorrentFileAvailability[]> {
    return this.http.post<TorrentFileAvailability[]>(`/Api/Torrents/CheckFilesMagnet`, {
      magnetLink,
    });
  }

  public checkFiles(file: File): Observable<TorrentFileAvailability[]> {
    const formData: FormData = new FormData();
    formData.append('file', file);
    return this.http.post<TorrentFileAvailability[]>(`/Api/Torrents/CheckFiles`, formData);
  }

  public delete(
    torrentId: string,
    deleteData: boolean,
    deleteRdTorrent: boolean,
    deleteLocalFiles: boolean
  ): Observable<void> {
    return this.http.post<void>(`/Api/Torrents/Delete/${torrentId}`, {
      deleteData,
      deleteRdTorrent,
      deleteLocalFiles,
    });
  }

  public retry(torrentId: string): Observable<void> {
    return this.http.post<void>(`/Api/Torrents/Retry/${torrentId}`, {});
  }

  public retryDownload(downloadId: string): Observable<void> {
    return this.http.post<void>(`/Api/Torrents/RetryDownload/${downloadId}`, {});
  }

  public update(torrent: Torrent): Observable<void> {
    return this.http.put<void>(`/Api/Torrents/Update`, torrent);
  }
}
