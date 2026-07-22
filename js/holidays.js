/* Magyar munkaszüneti napok (fix + húsvéthez kötött mozgó ünnepek) számítása. */
(function (global) {
  function pad(n) { return String(n).padStart(2, '0'); }

  function toISO(y, m, d) {
    return y + '-' + pad(m) + '-' + pad(d);
  }

  function addDays(y, m, d, delta) {
    const dt = new Date(Date.UTC(y, m - 1, d));
    dt.setUTCDate(dt.getUTCDate() + delta);
    return { y: dt.getUTCFullYear(), m: dt.getUTCMonth() + 1, d: dt.getUTCDate() };
  }

  // Gauss-algoritmus a húsvétvasárnap kiszámítására (Gergely-naptár)
  function easterSunday(year) {
    const a = year % 19;
    const b = Math.floor(year / 100);
    const c = year % 100;
    const d = Math.floor(b / 4);
    const e = b % 4;
    const f = Math.floor((b + 8) / 25);
    const g = Math.floor((b - f + 1) / 3);
    const h = (19 * a + b - d - g + 15) % 30;
    const i = Math.floor(c / 4);
    const k = c % 4;
    const l = (32 + 2 * e + 2 * i - h - k) % 7;
    const m = Math.floor((a + 11 * h + 22 * l) / 451);
    const month = Math.floor((h + l - 7 * m + 114) / 31);
    const day = ((h + l - 7 * m + 114) % 31) + 1;
    return { y: year, m: month, d: day };
  }

  /**
   * Visszaadja egy adott év alapértelmezett (automatikusan generált) magyar
   * munkaszüneti napjait: { 'YYYY-MM-DD': 'Ünnep neve' }
   */
  function defaultHolidays(year) {
    const easter = easterSunday(year);
    const goodFriday = addDays(easter.y, easter.m, easter.d, -2);
    const easterMonday = addDays(easter.y, easter.m, easter.d, 1);
    const whitMonday = addDays(easter.y, easter.m, easter.d, 50);

    const map = {};
    map[toISO(year, 1, 1)] = 'Újév';
    map[toISO(year, 3, 15)] = 'Nemzeti ünnep (1848)';
    map[toISO(goodFriday.y, goodFriday.m, goodFriday.d)] = 'Nagypéntek';
    map[toISO(easterMonday.y, easterMonday.m, easterMonday.d)] = 'Húsvéthétfő';
    map[toISO(year, 5, 1)] = 'A munka ünnepe';
    map[toISO(whitMonday.y, whitMonday.m, whitMonday.d)] = 'Pünkösdhétfő';
    map[toISO(year, 8, 20)] = 'Az államalapítás ünnepe';
    map[toISO(year, 10, 23)] = 'Az 1956-os forradalom ünnepe';
    map[toISO(year, 11, 1)] = 'Mindenszentek';
    map[toISO(year, 12, 25)] = 'Karácsony';
    map[toISO(year, 12, 26)] = 'Karácsony másnapja';
    return map;
  }

  global.App = global.App || {};
  global.App.Holidays = { defaultHolidays: defaultHolidays, toISO: toISO };
})(window);
