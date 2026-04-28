import { Pipe, PipeTransform } from '@angular/core';

export type SortDirection = 'asc' | 'desc';

export function getSortFieldValue(item: unknown, field: string): unknown {
  if (item == null || !field) {
    return undefined;
  }

  return field.split('.').reduce<unknown>((value, key) => {
    if (value == null || typeof value !== 'object') {
      return undefined;
    }

    return (value as Record<string, unknown>)[key];
  }, item);
}

function compareSortValues(left: unknown, right: unknown): number {
  if (left === right) {
    return 0;
  }

  if (left == null) {
    return 1;
  }

  if (right == null) {
    return -1;
  }

  if (typeof left === 'string' && typeof right === 'string') {
    return left.localeCompare(right, undefined, { sensitivity: 'base' });
  }

  if (left instanceof Date && right instanceof Date) {
    return left.getTime() - right.getTime();
  }

  if (typeof left === 'boolean' && typeof right === 'boolean') {
    return Number(left) - Number(right);
  }

  const comparableLeft = left as string | number | bigint;
  const comparableRight = right as string | number | bigint;

  if (comparableLeft < comparableRight) {
    return -1;
  }

  if (comparableLeft > comparableRight) {
    return 1;
  }

  return 0;
}

export function sortItems<T>(
  array: readonly T[],
  field: string,
  order: SortDirection = 'asc',
  accessor: (item: T, field: string) => unknown = getSortFieldValue,
): T[] {
  if (!Array.isArray(array)) {
    return [];
  }

  const direction = order === 'asc' ? 1 : -1;

  return [...array].sort((left, right) => compareSortValues(accessor(left, field), accessor(right, field)) * direction);
}

@Pipe({ name: 'sort' })
export class SortPipe implements PipeTransform {
  transform(array: unknown[], field: string, order: SortDirection = 'asc'): unknown[] {
    return sortItems(array, field, order);
  }
}
