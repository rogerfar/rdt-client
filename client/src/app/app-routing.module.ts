import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AddNewTorrentComponent } from './add-new-torrent/add-new-torrent.component';
import { AuthResolverService } from './auth-resolver.service';
import { LoginComponent } from './login/login.component';
import { MainLayoutComponent } from './main-layout/main-layout.component';
import { ProfileComponent } from './profile/profile.component';
import { SettingsComponent } from './settings/settings.component';
import { SetupComponent } from './setup/setup.component';
import { TorrentTableComponent } from './torrent-table/torrent-table.component';
import { TorrentComponent } from './torrent/torrent.component';

const routes: Routes = [
  {
    path: 'login',
    component: LoginComponent,
  },
  {
    path: 'setup',
    component: SetupComponent,
  },
  {
    path: '',
    component: MainLayoutComponent,
    resolve: {
      isLoggedIn: AuthResolverService,
    },
    children: [
      {
        path: '',
        component: TorrentTableComponent,
      },
      {
        path: 'torrent/:id',
        component: TorrentComponent,
      },
      {
        path: 'add',
        component: AddNewTorrentComponent,
      },
      {
        path: 'settings',
        component: SettingsComponent,
      },
      {
        path: 'profile',
        component: ProfileComponent,
      },
    ],
  },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule {}
