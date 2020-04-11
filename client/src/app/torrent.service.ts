import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, Subject } from 'rxjs';
import { Torrent } from './models/torrent.model';

@Injectable({
  providedIn: 'root',
})
export class TorrentService {
  constructor(private http: HttpClient) {}

  public getList(): Observable<Torrent[]> {
    return this.http.get<Torrent[]>(`/Api/Torrents`);
  }

  public getDetails(torrentId: string): Observable<Torrent> {
    return this.http.get<Torrent>(`/Api/Torrents/${torrentId}`);
  }

  public uploadMagnet(
    magnetLink: string,
    autoDownload: boolean,
    autoDelete: boolean
  ): Observable<void> {
    return this.http.post<void>(`/Api/Torrents/UploadMagnet`, {
      magnetLink,
      autoDownload,
      autoDelete,
    });
  }

  public uploadFile(
    file: File,
    autoDownload: boolean,
    autoDelete: boolean
  ): Observable<void> {
    const formData: FormData = new FormData();
    formData.append('file', file);
    formData.append('formData', JSON.stringify({ autoDownload, autoDelete }));
    return this.http.post<void>(`/Api/Torrents/UploadFile`, formData);
  }

  public download(torrentId: string): Observable<void> {
    return this.http.get<void>(`/Api/Torrents/Download/${torrentId}`);
  }

  public delete(torrentId: string): Observable<void> {
    return this.http.delete<void>(`/Api/Torrents/${torrentId}`);
  }
}
