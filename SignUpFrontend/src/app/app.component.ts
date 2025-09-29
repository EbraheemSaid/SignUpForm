import { Component } from "@angular/core";
import { MatToolbarModule } from "@angular/material/toolbar";
import { SignupComponent } from "./signup/signup.component";

@Component({
  selector: "app-root",
  standalone: true,
  imports: [MatToolbarModule, SignupComponent],
  templateUrl: "./app.component.html",
  styleUrls: ["./app.component.css"],
})
export class AppComponent {
  title = "signup-frontend";
}
