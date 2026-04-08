import { loggUser } from '../../shared/models/loggUser';

export class LocalStorageUtils {
  user: loggUser = new loggUser();

  //shared
  //gets
  public getUser() {
    const userJson = localStorage.getItem('AGROVYS.user');
    return userJson ? JSON.parse(userJson) : null;
  }

  public getUserToken(): string | null {
    return localStorage.getItem('AGROVYS.token');
  }

  public getMenuAllowed(): string | null {
    const userStorage = localStorage.getItem('AGROVYS.user');

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
    localStorage.setItem('AGROVYS.token', token);
  }

  public saveUser(response: any) {
    this.user.name = String(response.person.name);
    this.user.id = String(response.user.id);
    this.user.email = String(response.user.email);
    this.user.userName = String(response.user.email);
    this.user.companyId = String(response.user.companyId);

    localStorage.setItem('AGROVYS.user', JSON.stringify(this.user) || '');
  }

  //clear
  public clearLocaleUserData() {
    localStorage.removeItem('AGROVYS.token');
    localStorage.removeItem('AGROVYS.refreshtoken');
    localStorage.removeItem('AGROVYS.user');
    localStorage.removeItem('AGROVYS.claims');
  }
  //
}
