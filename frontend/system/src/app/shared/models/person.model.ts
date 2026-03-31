export class PersonModel {
  id!: string;
  name!: string;
  document!: string;
  birthDate!: string;
  phone!: string;
  phoneSecondary?: string; 
  emailSecondary?: string;
  position!: string;    
  userId!: string;
  user!: string;
  address!: AddressModel[];
}

export class AddressModel {
  id!: string;
  zipCode!: string;
  street!: string;
  number!: string;
  complement?: string;
  city!: string;
  state!: string;
  neighborhood!: string;
  personId!: string;
}