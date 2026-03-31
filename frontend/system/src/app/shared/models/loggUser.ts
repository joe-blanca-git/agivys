
export class loggUser {
  email?: string;
  name?: string;
  id?: string;
  userName?: string;
  roles?: roles[];
  menuAllowed?: menu[];
}

class menu {
  title?: string;
  icon?: string;
  route?: string;
  items?: submenus[];
}

class submenus {
  title?: string;
  description?: string;
  route?: string;
  icon?: string;

}

class roles {
  value?: string;
  type?: string;
}
