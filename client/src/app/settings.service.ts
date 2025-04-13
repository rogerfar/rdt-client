import { HttpClient } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Profile } from './models/profile.model';
import { Setting } from './models/setting.model';
import { APP_BASE_HREF } from '@angular/common';
import { Version } from './models/version.model';

@Injectable({
  providedIn: 'root',
})
export class SettingsService {
  constructor(
    private http: HttpClient,
    @Inject(APP_BASE_HREF) private baseHref: string,
  ) {}

  public get(): Observable<Setting[]> {
    return this.http.get<Setting[]>(`${this.baseHref}Api/Settings`);
  }

  public update(settings: Setting[]): Observable<void> {
    return this.http.put<void>(`${this.baseHref}Api/Settings`, settings);
  }

  public getProfile(): Observable<Profile> {
    return this.http.get<Profile>(`${this.baseHref}Api/Settings/Profile`);
  }

  public getVersion(): Observable<Version> {
    return this.http.get<Version>(`${this.baseHref}Api/Settings/Version`);
  }

  public testPath(path: string): Observable<void> {
    return this.http.post<void>(`${this.baseHref}Api/Settings/TestPath`, { path });
  }

  public testDownloadSpeed(): Observable<number> {
    return this.http.get<number>(`${this.baseHref}Api/Settings/TestDownloadSpeed`);
  }

  public testWriteSpeed(): Observable<number> {
    return this.http.get<number>(`${this.baseHref}Api/Settings/TestWriteSpeed`);
  }

  public testAria2cConnection(url: string, secret: string): Observable<{ version: string }> {
    return this.http.post<{ version: string }>(`${this.baseHref}Api/Settings/TestAria2cConnection`, {
      url,
      secret,
    });
  }
}
