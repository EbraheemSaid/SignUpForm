import { bootstrapApplication } from "@angular/platform-browser";
import { importProvidersFrom } from "@angular/core";
import { BrowserAnimationsModule } from "@angular/platform-browser/animations";
import { HttpClientModule } from "@angular/common/http";

import { AppComponent } from "./app/app.component";

bootstrapApplication(AppComponent, {
  providers: [importProvidersFrom(BrowserAnimationsModule, HttpClientModule)],
}).catch((err) => console.error(err));
