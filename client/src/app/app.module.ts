import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { FlexLayoutModule } from '@angular/flex-layout';
import { FormsModule } from '@angular/forms';
import { BrowserModule } from '@angular/platform-browser';
import { curray } from 'curray';
import { FileSizePipe, NgxFilesizeModule } from 'ngx-filesize';
import { AddNewTorrentComponent } from './add-new-torrent/add-new-torrent.component';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { AuthInterceptor } from './auth.interceptor';
import { DownloadStatusPipe } from './download-status.pipe';
import { LoginComponent } from './login/login.component';
import { MainLayoutComponent } from './main-layout/main-layout.component';
import { NavbarComponent } from './navbar/navbar.component';
import { SettingsComponent } from './settings/settings.component';
import { SetupComponent } from './setup/setup.component';
import { TorrentDownloadComponent } from './torrent-download/torrent-download.component';
import { TorrentFileComponent } from './torrent-file/torrent-file.component';
import { TorrentRowComponent } from './torrent-row/torrent-row.component';
import { TorrentStatusPipe } from './torrent-status.pipe';
import { TorrentTableComponent } from './torrent-table/torrent-table.component';

curray();

@NgModule({
  declarations: [
    AppComponent,
    MainLayoutComponent,
    NavbarComponent,
    AddNewTorrentComponent,
    TorrentTableComponent,
    TorrentRowComponent,
    TorrentFileComponent,
    SettingsComponent,
    TorrentStatusPipe,
    DownloadStatusPipe,
    LoginComponent,
    SetupComponent,
    TorrentDownloadComponent,
  ],
  imports: [BrowserModule, AppRoutingModule, FormsModule, HttpClientModule, NgxFilesizeModule, FlexLayoutModule],
  providers: [FileSizePipe, { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true }],
  bootstrap: [AppComponent],
})
export class AppModule {}
