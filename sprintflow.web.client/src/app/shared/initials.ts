export function initialsOf(fullName: string): string {
  return fullName
    .split(' ')
    .filter(part => part.length > 0)
    .slice(0, 2)
    .map(part => part.charAt(0).toLocaleUpperCase('tr'))
    .join('');
}
