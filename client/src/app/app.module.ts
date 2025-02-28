import { ClipboardModule } from '@angular/cdk/clipboard';
import { APP_BASE_HREF } from '@angular/common';
import { HTTP_INTERCEPTORS, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BrowserModule } from '@angular/platform-browser';
import { curray } from 'curray';
import { FileSizePipe, NgxFilesizeModule } from 'ngx-filesize';
import { AddNewTorrentComponent } from './add-new-torrent/add-new-torrent.component';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { AuthInterceptor } from './auth.interceptor';
import { DecodeURIPipe } from './decode-uri.pipe';
import { DownloadStatusPipe } from './download-status.pipe';
import { LoginComponent } from './login/login.component';
import { MainLayoutComponent } from './main-layout/main-layout.component';
import { NavbarComponent } from './navbar/navbar.component';
import { Nl2BrPipe } from './nl2br.pipe';
import { ProfileComponent } from './profile/profile.component';
import { SettingsComponent } from './settings/settings.component';
import { SetupComponent } from './setup/setup.component';
import { TorrentStatusPipe } from './torrent-status.pipe';
import { TorrentTableComponent } from './torrent-table/torrent-table.component';
import { TorrentComponent } from './torrent/torrent.component';
import { SortPipe } from './sort.pipe';

curray();

@NgModule({
  declarations: [
    AppComponent,
    MainLayoutComponent,
    NavbarComponent,
    AddNewTorrentComponent,
    TorrentTableComponent,
    SettingsComponent,
    TorrentStatusPipe,
    DownloadStatusPipe,
    LoginComponent,
    SetupComponent,
    TorrentComponent,
    DecodeURIPipe,
    ProfileComponent,
    Nl2BrPipe,
    SortPipe,
  ],
  bootstrap: [AppComponent],
  imports: [BrowserModule, AppRoutingModule, FormsModule, NgxFilesizeModule, ClipboardModule],
  providers: [
    FileSizePipe,
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
    { provide: APP_BASE_HREF, useValue: (window as any)['_app_base'] || '/' },
    provideHttpClient(withInterceptorsFromDi()),
  ],
})
export class AppModule {}
