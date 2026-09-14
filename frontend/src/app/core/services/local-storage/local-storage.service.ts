import { inject, PLATFORM_ID, Service } from '@angular/core';
import { isPlatformServer } from '@angular/common';

@Service()
export class LocalStorageService {
  platformId = inject(PLATFORM_ID);

  get length() { return localStorage.length };

  /**
   * Get Value by Key from Local Storage.
   *
   * @param key Local storage record key
   * @returns Stored Value in Local Storage record if exist
   */
  get = (key: string): string | null => {
    if (isPlatformServer(this.platformId) || !key)
      return null;

    return localStorage.getItem(key);
  }

  /**
   * Set Value by Key in Local Storage
   *
   * @param key Local storage record key
   * @param value Value to be stored inside Local Storage
   */
  set = (key: string, value: string): boolean => {
    if (isPlatformServer(this.platformId) || !key || !value)
      return false;

    localStorage.setItem(key, value);
    return true;
  }

  /**
   * Remove Value by Key from Local Storage
   *
   * @param key
   */
  remove = (key: string): boolean => {
    if (isPlatformServer(this.platformId) || !key)
      return false;

    localStorage.removeItem(key);
    return true;
  }

  /**
   * Clear Local Storage
   *
   * @returns Whether Local Storage has been cleared or not.
   */
  clear = (): boolean => {
    if (isPlatformServer(this.platformId))
      return false;

    localStorage.clear();
    return true;
  }
}
