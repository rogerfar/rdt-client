import { APP_BASE_HREF } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs/internal/Observable';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  constructor(
    private http: HttpClient,
    @Inject(APP_BASE_HREF) private baseHref: string,
  ) {}

  public isLoggedIn(): Observable<boolean> {
    return this.http.get<boolean>(`${this.baseHref}Api/Authentication/IsLoggedIn`);
  }

  public create(userName: string, password: string): Observable<void> {
    return this.http.post<void>(`${this.baseHref}Api/Authentication/Create`, {
      userName,
      password,
    });
  }

  public setupProvider(provider: number, token: string): Observable<void> {
    return this.http.post<void>(`${this.baseHref}Api/Authentication/SetupProvider`, {
      provider,
      token,
    });
  }

  public login(userName: string, password: string): Observable<void> {
    return this.http.post<void>(`${this.baseHref}Api/Authentication/Login`, {
      userName,
      password,
    });
  }

  public logout() {
    return this.http.post<void>(`${this.baseHref}Api/Authentication/Logout`, {});
  }

  public update(userName: string, password: string): Observable<void> {
    return this.http.post<void>(`${this.baseHref}Api/Authentication/Update`, {
      userName,
      password,
    });
  }
}
