import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import {
  FormArray,
  FormBuilder,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';

@Component({
  selector: 'app-register-company-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './register-company-form.component.html',
  styleUrl: './register-company-form.component.scss',
})
export class RegisterCompanyFormComponent{
  formRegisterCompany: FormGroup;
  submitted = false;

  constructor(private fb: FormBuilder) {
    this.formRegisterCompany = this.fb.group({
      businessType: ['Fazenda', Validators.required],
      organizationName: ['', Validators.required],
      description: [''],
      cep: ['', Validators.required],
      street: ['', Validators.required],
      number: ['', Validators.required],
      complement: [''],
      city: ['', Validators.required],
      state: ['', Validators.required],
    });
  }

  get f() {
    return this.formRegisterCompany.controls;
  }



  public getFormData() {
    this.submitted = true;
    return {
      isValid: this.formRegisterCompany.valid,
      data: this.formRegisterCompany.getRawValue(),
    };
  }
}
