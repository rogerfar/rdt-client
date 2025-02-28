import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
    name: 'decodeURI',
    standalone: false
})
export class DecodeURIPipe implements PipeTransform {
  transform(input: any) {
    return decodeURI(input);
  }
}
