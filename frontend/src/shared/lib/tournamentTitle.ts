import { z } from 'zod';

// Mirrors the backend title rules (CreateTournamentRequestValidator / TournamentService):
// 5-50 chars; only Latin/Cyrillic letters, digits, single spaces and single hyphens;
// must start and end with a letter or digit.
const ALPHANUMERIC = 'A-Za-z0-9\\u0400-\\u04FF';
const DISALLOWED_CHAR = new RegExp(`[^${ALPHANUMERIC} -]`);
const STARTS_WITH_ALPHANUMERIC = new RegExp(`^[${ALPHANUMERIC}]`);
const ENDS_WITH_ALPHANUMERIC = new RegExp(`[${ALPHANUMERIC}]$`);

export const tournamentTitleSchema = z.string().superRefine((value, ctx) => {
  const fail = (message: string) => ctx.addIssue({ code: z.ZodIssueCode.custom, message });

  if (value.length < 5) {
    fail('Минимум 5 символов');
    return;
  }
  if (value.length > 50) {
    fail('Максимум 50 символов');
    return;
  }
  if (DISALLOWED_CHAR.test(value)) {
    fail('Допустимы только буквы, цифры, пробел и дефис');
    return;
  }
  if (/^ +$/.test(value)) {
    fail('Название не может состоять только из пробелов');
    return;
  }
  if (/^-+$/.test(value)) {
    fail('Название не может состоять только из дефисов');
    return;
  }
  if (!STARTS_WITH_ALPHANUMERIC.test(value)) {
    fail('Название должно начинаться с буквы или цифры');
    return;
  }
  if (!ENDS_WITH_ALPHANUMERIC.test(value)) {
    fail('Название должно заканчиваться буквой или цифрой');
    return;
  }
  if (/ {2,}/.test(value)) {
    fail('Не более одного пробела подряд');
    return;
  }
  if (/-{2,}/.test(value)) {
    fail('Не более одного дефиса подряд');
  }
});
