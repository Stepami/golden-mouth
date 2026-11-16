export type AgeBand = '14-17' | '18-24' | '25-34' | '35-44' | '45+'

/** Local preview bands; the server derives the current band from the saved birth year. */
export function ageBand(age: number): AgeBand | undefined {
  if (age < 14) return undefined
  if (age < 18) return '14-17'
  if (age < 25) return '18-24'
  if (age < 35) return '25-34'
  if (age < 45) return '35-44'
  return '45+'
}
