export class LocalStorageUtils {
  //shared
  //gets
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

  //clear
  public clearLocaleUserData() {
    localStorage.removeItem('BITADMIN.token');
    localStorage.removeItem('BITADMIN.refreshtoken');
    localStorage.removeItem('BITADMIN.user');
    localStorage.removeItem('BITADMIN.claims');
  }
  //
}
