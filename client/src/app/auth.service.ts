import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs/internal/Observable';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  constructor(private http: HttpClient) {}

  public isLoggedIn(): Observable<void> {
    return this.http.get<void>(`/Api/Authentication/IsLoggedIn`);
  }

  public create(userName: string, password: string): Observable<void> {
    return this.http.post<void>(`/Api/Authentication/Create`, {
      userName,
      password,
    });
  }

  public setupProvider(provider: string, token: string): Observable<void> {
    return this.http.post<void>(`/Api/Authentication/SetupProvider`, {
      provider,
      token,
    });
  }

  public login(userName: string, password: string): Observable<void> {
    return this.http.post<void>(`/Api/Authentication/Login`, {
      userName,
      password,
    });
  }

  public logout() {
    return this.http.post<void>(`/Api/Authentication/Logout`, {});
  }

  public update(userName: string, password: string): Observable<void> {
    return this.http.post<void>(`/Api/Authentication/Update`, {
      userName,
      password,
    });
  }
}
