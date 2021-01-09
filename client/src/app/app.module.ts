import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { NgxFilesizeModule, FileSizePipe } from 'ngx-filesize';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { MainLayoutComponent } from './main-layout/main-layout.component';
import { NavbarComponent } from './navbar/navbar.component';
import { AddNewTorrentComponent } from './navbar/add-new-torrent/add-new-torrent.component';
import { TorrentTableComponent } from './torrent-table/torrent-table.component';
import { TorrentRowComponent } from './torrent-row/torrent-row.component';
import { TorrentFileComponent } from './torrent-file/torrent-file.component';
import { SettingsComponent } from './navbar/settings/settings.component';
import { TorrentStatusPipe } from './torrent-status.pipe';
import { FileStatusPipe } from './file-status.pipe';
import { LoginComponent } from './login/login.component';
import { AuthInterceptor } from './auth.interceptor';
import { curray } from 'curray';
import { SetupComponent } from './setup/setup.component';

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
    FileStatusPipe,
    LoginComponent,
    SetupComponent,
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    FormsModule,
    HttpClientModule,
    NgxFilesizeModule,
  ],
  providers: [
    FileSizePipe,
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
  ],
  bootstrap: [AppComponent],
})
export class AppModule {}
