import { Injectable } from '@angular/core';
import { Resolve } from '@angular/router';
import { Observable } from 'rxjs';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root',
})
export class AuthResolverService implements Resolve<Observable<any>> {
  constructor(private authService: AuthService) {}

  resolve() {
    return this.authService.isLoggedIn();
  }
}
