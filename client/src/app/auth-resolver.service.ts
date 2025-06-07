import { inject } from '@angular/core';
import { AuthService } from './auth.service';
import { ResolveFn } from '@angular/router';

export const authResolver: ResolveFn<boolean> = () => {
  return inject(AuthService).isLoggedIn();
};
