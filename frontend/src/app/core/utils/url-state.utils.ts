import { ActivatedRoute, Params, Router } from '@angular/router';
import { effect } from '@angular/core';

/**
 * The filters the screen was opened with, taken from the query string.
 *
 * Every value arrives as a string, which is what the filter types already
 * hold, so the object goes straight into the filters signal.
 *
 * @param route The screen's route.
 * @returns The query parameters, without the page.
 */
export function filtersFromUrl<T>(route: ActivatedRoute): T {
  const params = route.snapshot.queryParamMap;
  const filters: Params = {};

  for (const key of params.keys) {
    if (key !== 'page') {
      filters[key] = params.get(key);
    }
  }

  return filters as T;
}

/**
 * @param route The screen's route.
 * @returns The page the URL opened with, or the first one.
 */
export function pageFromUrl(route: ActivatedRoute): number {
  const page = Number(route.snapshot.queryParamMap.get('page'));

  return Number.isInteger(page) && page > 0 ? page : 1;
}

/**
 * Reads one end of a date range off the query string. A date with no time
 * parses as UTC, so the midnight is spelled out to keep it local.
 *
 * @param route The screen's route.
 * @param key Which query parameter to read.
 * @returns That day, or today when the URL does not say.
 */
export function dateFromUrl(route: ActivatedRoute, key: 'from' | 'to'): Date {
  const value = route.snapshot.queryParamMap.get(key);
  const day = value ? new Date(`${value}T00:00:00`) : null;

  return day && !Number.isNaN(day.getTime()) ? day : new Date();
}

/**
 * Mirrors a screen's filters and page into the query string, so a reload or a
 * shared link reopens the same view.
 *
 * Empty values are written as null, which is how the router drops a parameter
 * rather than leaving `?fullname=` behind.
 *
 * @param router The router to navigate with.
 * @param route The screen's route.
 * @param state Returns everything the URL should carry.
 */
export function syncUrl(router: Router, route: ActivatedRoute, state: () => Params): void {
  effect(() => {
    const queryParams: Params = {};

    for (const [key, value] of Object.entries(state())) {
      queryParams[key] = value === '' || value === undefined ? null : value;
    }

    router.navigate([], { queryParams, relativeTo: route, replaceUrl: true });
  });
}
