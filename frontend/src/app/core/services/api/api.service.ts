import { HttpClient, HttpParams, httpResource, HttpResponse } from '@angular/common/http';
import { Params } from '@angular/router';
import { inject, Service } from '@angular/core';

import { map, Observable } from 'rxjs';

@Service()
export class ApiService {
  httpClient = inject(HttpClient);

  _baseUrl = '/api';

  /**
   * Sends a GET request.
   *
   * @param path Route below /api.
   * @param params Query-string values; unset entries are dropped.
   * @returns The response body.
   */
  get<T>(path: string, params?: Params): Observable<T> {
    return this.httpClient.get<T>(`${this._baseUrl}${path}`, { params: this._toHttpParams(params) });
  }

  /**
   * Sends a GET request as reactive state rather than a one-shot call.
   *
   * Returns a resource exposing `value()`, `isLoading()`, `error()` and
   * `reload()`, so a screen does not hand-write those signals itself. Use it
   * for lists a filter drives; use `get` when a caller needs the Observable.
   *
   * @param path Route below /api.
   * @param params Query-string values; unset entries are dropped.
   * @returns The resource holding the response body.
   */
  getResource = <T>(path: string, params: Params = {}) => {
    return httpResource<T>(() => ({
      url: `${this._baseUrl}${path}`,
      params: this._toHttpParams(params),
    }));
  }

  /**
   * Sends a POST request.
   *
   * @param path Route below /api.
   * @param body Request payload.
   * @returns The created resource, or void when the API answers 201 with no body.
   */
  post<T>(path: string, body: Params): Observable<T | null> {
    return this.httpClient.post<T>(`${this._baseUrl}${path}`, body, { observe: 'response' }).pipe(map(this.extractBody));
  }

  /**
   * Sends a PUT request.
   *
   * @param path Route below /api.
   * @param body Replacement payload.
   * @returns Nothing; updates answer 204.
   */
  put(path: string, body: Params): Observable<void> {
    return this.httpClient.put<void>(`${this._baseUrl}${path}`, body);
  }

  /**
   * Sends a PATCH request.
   *
   * @param path Route below /api.
   * @param body Request payload; the state-transition endpoints take none.
   * @returns Nothing; transitions answer 204.
   */
  patch(path: string, body: Params | null = null): Observable<void> {
    return this.httpClient.patch<void>(`${this._baseUrl}${path}`, body);
  }

  /**
   * Sends a DELETE request.
   *
   * @param path Route below /api.
   * @returns Nothing; deletes answer 204.
   */
  delete(path: string): Observable<void> {
    return this.httpClient.delete<void>(`${this._baseUrl}${path}`);
  }

  /**
   * Converts a plain object to HttpParams, skipping unset entries.
   *
   * @param params Query-string values, if any.
   * @returns The populated HttpParams.
   */
  _toHttpParams(params?: Params): HttpParams {
    let httpParams = new HttpParams();

    if (!params) {
      return httpParams;
    }

    for (const [key, value] of Object.entries(params)) {
      if (!value) {
        continue;
      }

      httpParams = httpParams.set(key, String(value));
    }

    return httpParams;
  }

  /**
   * Extract body out of Http Response
   *
   * @param response
   * @returns
   */
  extractBody = <T>(response: HttpResponse<T>) => response.body;
}
