import { Component, OnInit, ViewChild } from "@angular/core";
import {
  FormBuilder,
  FormGroup,
  Validators,
  ReactiveFormsModule,
} from "@angular/forms";
import { HttpClient } from "@angular/common/http";
import { environment } from "../../environments/environment";
import { ReCaptcha2Component, NgxCaptchaModule } from "ngx-captcha";
import { CommonModule } from "@angular/common";
import { MatCardModule } from "@angular/material/card";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";

@Component({
  selector: "app-signup",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    NgxCaptchaModule,
  ],
  templateUrl: "./signup.component.html",
  styleUrls: ["./signup.component.css"],
})
export class SignupComponent implements OnInit {
  signupForm: FormGroup;
  isSubmitting = false;
  isSubmitted = false; // Track if form has been submitted
  errorMessage = "";
  successMessage = "";
  reCaptchaKey = environment.reCaptchaSiteKey;
  captchaResponse?: string;
  hasError = false; // Track if there was an error to trigger captcha reset

  @ViewChild("captchaRef") captchaRef!: ReCaptcha2Component;

  // Password validation states
  passwordRequirements = {
    minLength: false,
    hasLowercase: false,
    hasUppercase: false,
    hasNumber: false,
  };

  constructor(private fb: FormBuilder, private http: HttpClient) {
    this.signupForm = this.fb.group({
      username: [
        "",
        [
          Validators.required,
          Validators.minLength(3),
          Validators.maxLength(50),
          Validators.pattern("^[a-zA-Z0-9_]+$"),
        ],
      ],
      email: ["", [Validators.required, Validators.email]],
      password: [
        "",
        [
          Validators.required,
          Validators.minLength(6),
          Validators.pattern("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d).*$"),
        ],
      ],
    });

    // Subscribe to password changes to update requirements
    this.signupForm.get("password")?.valueChanges.subscribe((value) => {
      this.updatePasswordRequirements(value);
    });

    // Subscribe to form changes to reset captcha if there was an error
    this.signupForm.valueChanges.subscribe(() => {
      this.onFormChanged();
    });
  }

  updatePasswordRequirements(password: string): void {
    if (!password) {
      this.passwordRequirements = {
        minLength: false,
        hasLowercase: false,
        hasUppercase: false,
        hasNumber: false,
      };
      return;
    }

    this.passwordRequirements = {
      minLength: password.length >= 6,
      hasLowercase: /[a-z]/.test(password),
      hasUppercase: /[A-Z]/.test(password),
      hasNumber: /\d/.test(password),
    };
  }

  ngOnInit(): void {}

  onCaptchaSuccess(captchaResponse: string): void {
    this.captchaResponse = captchaResponse;
  }

  onCaptchaExpired(): void {
    this.captchaResponse = undefined; // Clear the token when expired
  }

  onCaptchaExpire(): void {
    this.captchaResponse = undefined;
  }

  onFormChanged(): void {
    // If there was an error and the user modifies the form, reset the captcha
    if (this.hasError && this.captchaResponse) {
      // Clear error message immediately to provide quick feedback
      this.errorMessage = "";
      this.hasError = false;

      // Reset captcha after a small delay to avoid jarring experience
      setTimeout(() => {
        this.resetCaptcha();
      }, 500);
    }
  }

  resetCaptcha(): void {
    this.captchaResponse = undefined;
    if (this.captchaRef) {
      this.captchaRef.resetCaptcha();
    }
  }

  onSubmit(): void {
    this.isSubmitted = true; // Set form as submitted to show validation errors

    if (this.signupForm.valid && this.captchaResponse) {
      this.isSubmitting = true;
      this.errorMessage = "";
      this.successMessage = "";

      const signupData = {
        ...this.signupForm.value,
        recaptchaToken: this.captchaResponse,
      };

      this.http
        .post(`${environment.apiUrl}/api/auth/signup`, signupData)
        .subscribe({
          next: (response: any) => {
            this.isSubmitting = false;
            if (response.success) {
              this.successMessage = response.message;
              // Reset form and clear all validation states
              this.signupForm.reset();
              this.isSubmitted = false; // Reset submitted state after successful submission
              Object.keys(this.signupForm.controls).forEach((key) => {
                const control = this.signupForm.get(key);
                control?.setErrors(null); // Clear any validation errors
              });
              this.resetCaptcha(); // Reset captcha
            } else {
              this.errorMessage = response.message || "Registration failed";
              this.hasError = true; // Mark that there was an error
            }
          },
          error: (error) => {
            this.isSubmitting = false;
            this.errorMessage =
              error.error?.message || "An error occurred during registration";
            this.hasError = true; // Mark that there was an error
          },
        });
    } else {
      if (!this.captchaResponse) {
        this.errorMessage = "Please complete the reCAPTCHA verification";
      }
      this.markFormGroupTouched();
    }
  }

  private markFormGroupTouched(): void {
    Object.keys(this.signupForm.controls).forEach((key) => {
      const control = this.signupForm.get(key);
      control?.markAsTouched();
    });
  }
}
