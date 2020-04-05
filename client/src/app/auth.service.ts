import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs/internal/Observable';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  constructor(private http: HttpClient) {}

  public login(userName: string, password: string): Observable<void> {
    return this.http.post<void>(`/Api/Authentication/Login`, {
      userName,
      password,
    });
  }

  public logout() {
    return this.http.post<void>(`/Api/Authentication/Logout`, {});
  }
}
