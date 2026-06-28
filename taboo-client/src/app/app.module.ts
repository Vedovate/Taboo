import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HttpClientModule } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';

import { App } from './app';
import { routes } from './app.routes';

@NgModule({
  imports: [
    BrowserModule,
    RouterModule.forRoot(routes, { initialNavigation: 'enabledBlocking' }),
    App,
  ],
  providers: [
    provideTranslateService({
      lang: 'pt-BR',
      fallbackLang: 'en-US',
    }),
    ...provideTranslateHttpLoader({
      prefix: '/assets/i18n/',
      suffix: '.json',
    }),
  ],
  bootstrap: [App],
})
export class AppModule {}
