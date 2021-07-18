import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';
import { Torrent, TorrentFileAvailability } from './models/torrent.model';

@Injectable({
  providedIn: 'root',
})
export class TorrentService {
  constructor(private http: HttpClient) {}

  public update$: Subject<Torrent[]> = new Subject();

  private connection: signalR.HubConnection;

  public connect(): void {
    this.connection = new signalR.HubConnectionBuilder().withUrl('/hub').withAutomaticReconnect().build();
    this.connection.start().catch((err) => console.error(err));

    this.connection.on('update', (torrents: Torrent[]) => {
      this.update$.next(torrents);
    });
  }

  public disconnect(): void {
    this.connection?.stop();
  }

  public getList(): Observable<Torrent[]> {
    return this.http.get<Torrent[]>(`/Api/Torrents`);
  }

  public uploadMagnet(
    magnetLink: string,
    category: string,
    downloadAction: number,
    finishedAction: number,
    downloadMinSize: number,
    downloadManualFiles: string
  ): Observable<void> {
    return this.http.post<void>(`/Api/Torrents/UploadMagnet`, {
      magnetLink,
      category,
      downloadAction,
      finishedAction,
      downloadMinSize,
      downloadManualFiles,
    });
  }

  public uploadFile(
    file: File,
    category: string,
    downloadAction: number,
    finishedAction: number,
    downloadMinSize: number,
    downloadManualFiles: string
  ): Observable<void> {
    const formData: FormData = new FormData();
    formData.append('file', file);
    formData.append(
      'formData',
      JSON.stringify({ category, downloadAction, finishedAction, downloadMinSize, downloadManualFiles })
    );
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

  public retry(torrentId: string, retry: number): Observable<void> {
    return this.http.post<void>(`/Api/Torrents/Retry/${torrentId}`, {
      retry,
    });
  }
}
