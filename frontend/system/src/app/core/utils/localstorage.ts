import { loggUser } from '../../shared/models/loggUser';

export class LocalStorageUtils {
  user: loggUser = new loggUser();

  //shared
  //gets
  public getUser() {
    const userJson = localStorage.getItem('BITADMIN.user');
    return userJson ? JSON.parse(userJson) : null;
  }

  public getUserToken(): string | null {
    return localStorage.getItem('BITADMIN.token');
  }

  public getMenuAllowed(): string | null {
    const userStorage = localStorage.getItem('BITADMIN.user');

    if (!userStorage) return null;

    try {
      const userParsed = JSON.parse(userStorage);

      if (!userParsed?.menuAllowed) return null;

      return JSON.stringify(userParsed.menuAllowed);
    } catch {
      return null;
    }
  }

  //saves
  public saveLocaleDataUser(response: any) {
    this.saveUserToken(response.token);

    this.saveUser(response);
  }

  public saveUserToken(token: string) {
    localStorage.setItem('BITADMIN.token', token);
  }

  public saveUser(response: any) {
    this.user.name = String(response.user.name);
    this.user.id = String(response.user.id);
    this.user.email = String(response.user.email);
    this.user.userName = String(response.user.userName);
    this.user.roles = response.user.roles;
    this.user.menuAllowed = response.menus;

    localStorage.setItem('BITADMIN.user', JSON.stringify(this.user) || '');
  }

  //clear
  public clearLocaleUserData() {
    localStorage.removeItem('BITADMIN.token');
    localStorage.removeItem('BITADMIN.refreshtoken');
    localStorage.removeItem('BITADMIN.user');
    localStorage.removeItem('BITADMIN.claims');
  }
  //
}
