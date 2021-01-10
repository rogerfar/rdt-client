import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
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
    autoUnpack: boolean,
    autoDelete: boolean
  ): Observable<void> {
    return this.http.post<void>(`/Api/Torrents/UploadMagnet`, {
      magnetLink,
      autoDownload,
      autoUnpack,
      autoDelete,
    });
  }

  public uploadFile(file: File, autoDownload: boolean, autoUnpack: boolean, autoDelete: boolean): Observable<void> {
    const formData: FormData = new FormData();
    formData.append('file', file);
    formData.append('formData', JSON.stringify({ autoDownload, autoUnpack, autoDelete }));
    return this.http.post<void>(`/Api/Torrents/UploadFile`, formData);
  }

  public download(torrentId: string): Observable<void> {
    return this.http.get<void>(`/Api/Torrents/Download/${torrentId}`);
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
}
