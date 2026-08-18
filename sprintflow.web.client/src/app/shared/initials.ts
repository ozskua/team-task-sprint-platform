/**
 * "Ayşe Demir" -> "AD". Purely visual, derived from the name already being shown, so an avatar
 * never becomes the only place a person is identified.
 *
 * Turkish casing on purpose: the invariant rule turns 'İ' into an 'i' plus a combining dot.
 */
export function initialsOf(fullName: string): string {
  return fullName
    .split(' ')
    .filter(part => part.length > 0)
    .slice(0, 2)
    .map(part => part.charAt(0).toLocaleUpperCase('tr'))
    .join('');
}
