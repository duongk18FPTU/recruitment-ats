// "Phạm Thu Hà" → "TH": the given name is last in Vietnamese names.
export function initials(fullName: string): string {
  const words = fullName.trim().split(/\s+/);
  return words
    .slice(-2)
    .map((word) => word.charAt(0).toLocaleUpperCase("vi"))
    .join("");
}
