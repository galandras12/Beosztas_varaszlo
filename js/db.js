/* Adattároló réteg: minden adat egyetlen JSON objektumban él, ami
 * localStorage-ban perzisztálódik, és fájlba is ki-/visszatölthető
 * (ez a "belső, titkosítatlan, szerver nélküli adatbázis"). */
(function (global) {
  const STORAGE_KEY = 'beosztasVarazslo.db.v1';

  const MONTH_NAMES = ['Január', 'Február', 'Március', 'Április', 'Május', 'Június',
    'Július', 'Augusztus', 'Szeptember', 'Október', 'November', 'December'];

  const DEFAULT_MONTH_HOURS = {
    1: { hours: 176, days: 22 },
    2: { hours: 168, days: 21 },
    3: { hours: 152, days: 19 },
    4: { hours: 168, days: 21 },
    5: { hours: 168, days: 21 },
    6: { hours: 160, days: 20 },
    7: { hours: 184, days: 23 },
    8: { hours: 168, days: 21 },
    9: { hours: 168, days: 21 },
    10: { hours: 176, days: 22 },
    11: { hours: 160, days: 20 },
    12: { hours: 160, days: 20 }
  };

  // Univerzális, minden munkacsoportra érvényes speciális kódok.
  const SPECIAL_CODES = {
    SZ: { label: 'Szabadság', short: 'SZ', color: '#2e7d32', vacation: true },
    BSZ: { label: 'Beteg szabadság', short: 'BSZ', color: '#c62828', sick: true },
    H: { label: 'Egyéb hiányzás', short: 'H', color: '#8d6e63' },
    P: { label: 'Pihenőnap', short: 'P', color: '#78909c' }
  };

  function uid(prefix) {
    return (prefix || 'id') + '_' + Date.now().toString(36) + Math.random().toString(36).slice(2, 8);
  }

  function defaultGroups() {
    return [
      {
        id: uid('grp'), name: 'Ápolók', type: 'altalanos', dailyHours: 12, staffPerShift: 2,
        shiftTypes: [
          { code: 'N', label: 'Nappal', hours: 12 },
          { code: 'É', label: 'Éjszaka', hours: 12 }
        ]
      },
      {
        id: uid('grp'), name: 'Takarítók', type: 'altalanos', dailyHours: 8, staffPerShift: 0,
        shiftTypes: [{ code: 'M', label: 'Munka', hours: 8 }]
      },
      {
        id: uid('grp'), name: 'Részmunkaidősök', type: 'altalanos', dailyHours: 4, staffPerShift: 0,
        shiftTypes: [{ code: 'M', label: 'Munka', hours: 4 }]
      },
      {
        id: uid('grp'), name: 'Irodai dolgozók', type: 'iroda', dailyHours: 8, staffPerShift: 0,
        shiftTypes: [{ code: 'M', label: 'Munka', hours: 8 }]
      }
    ];
  }

  function defaultState() {
    return {
      meta: { version: 1 },
      monthHours: JSON.parse(JSON.stringify(DEFAULT_MONTH_HOURS)),
      groups: defaultGroups(),
      employees: [],
      holidaysExtra: {},   // { year: { 'YYYY-MM-DD': 'név' } } - kézzel hozzáadott ünnepek
      holidaysRemoved: {}, // { year: ['YYYY-MM-DD', ...] } - automata ünnep kikapcsolva
      schedule: {},        // { 'year-month': { employeeId: { day: code } } }
      carryOver: {}        // { 'year-month': { employeeId: number } }
    };
  }

  let state = null;

  function load() {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (raw) {
        state = JSON.parse(raw);
        // hiányzó mezők pótlása visszamenőleg (frissítés esetére)
        const def = defaultState();
        for (const k of Object.keys(def)) {
          if (!(k in state)) state[k] = def[k];
        }
        if (!state.groups || state.groups.length === 0) state.groups = defaultGroups();
      } else {
        state = defaultState();
      }
    } catch (e) {
      console.error('Adatbázis betöltési hiba, alaphelyzet.', e);
      state = defaultState();
    }
    return state;
  }

  function save() {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
  }

  function getState() {
    if (!state) load();
    return state;
  }

  function scheduleKey(year, month) {
    return year + '-' + month;
  }

  const EXPORT_FORMAT = 'beosztas-varazslo-v1';

  // A csoporttípus a belső állapotban 'altalanos'/'iroda' (kisbetűs), a közös
  // exportformátumban - hogy az Android/Windows verziókkal is egyezzen - 'ALTALANOS'/'IRODA'.
  function typeToCanonical(type) { return type === 'iroda' ? 'IRODA' : 'ALTALANOS'; }
  function typeFromCanonical(type) { return String(type).toUpperCase() === 'IRODA' ? 'iroda' : 'altalanos'; }

  /**
   * A belső (localStorage-beli, egymásba ágyazott szótárakból álló) állapotot az Android/
   * Windows verzióval is közös, lapos tömbökből álló "közös exportformátumra" alakítja -
   * ez teszi lehetővé, hogy egy itt exportált fájl bármelyik platformon importálható legyen,
   * és fordítva.
   */
  function toCanonicalExport(s) {
    const monthHours = [];
    for (let m = 1; m <= 12; m++) {
      const rec = s.monthHours[m];
      if (rec) monthHours.push({ month: m, hours: rec.hours, days: rec.days });
    }

    const groups = (s.groups || []).map(g => ({
      id: String(g.id), name: g.name, type: typeToCanonical(g.type),
      dailyHours: g.dailyHours, staffPerShift: g.staffPerShift || 0,
      minRestHours: g.minRestHours != null ? g.minRestHours : 24,
      shiftTypes: (g.shiftTypes || []).map(st => ({ code: st.code, label: st.label, hours: st.hours }))
    }));

    const employees = (s.employees || []).map(e => ({
      id: String(e.id), name: e.name, groupId: String(e.groupId),
      employmentFactor: e.employmentFactor != null ? e.employmentFactor : 1,
      maxVacationDays: e.maxVacationDays, notes: e.notes || '',
      excludedShiftCodes: e.excludedShiftCodes || []
    }));

    const schedule = [];
    Object.keys(s.schedule || {}).forEach(key => {
      const [year, month] = key.split('-').map(Number);
      const monthData = s.schedule[key];
      Object.keys(monthData).forEach(employeeId => {
        const days = monthData[employeeId];
        Object.keys(days).forEach(day => {
          schedule.push({ employeeId: String(employeeId), year, month, day: Number(day), code: days[day] });
        });
      });
    });

    const carryOvers = [];
    Object.keys(s.carryOver || {}).forEach(key => {
      const [year, month] = key.split('-').map(Number);
      const monthData = s.carryOver[key];
      Object.keys(monthData).forEach(employeeId => {
        carryOvers.push({ employeeId: String(employeeId), year, month, hours: monthData[employeeId] });
      });
    });

    const holidaysExtra = [];
    Object.keys(s.holidaysExtra || {}).forEach(year => {
      const dates = s.holidaysExtra[year];
      Object.keys(dates).forEach(date => holidaysExtra.push({ year: Number(year), date, name: dates[date] }));
    });

    const holidaysRemoved = [];
    Object.keys(s.holidaysRemoved || {}).forEach(year => {
      (s.holidaysRemoved[year] || []).forEach(date => holidaysRemoved.push({ year: Number(year), date }));
    });

    return {
      exportFormat: EXPORT_FORMAT, generatedBy: 'web', generatedAt: new Date().toISOString(),
      monthHours, groups, employees, schedule, carryOvers, holidaysExtra, holidaysRemoved
    };
  }

  /** A közös exportformátumból építi fel a belső állapotot (lásd toCanonicalExport). */
  function fromCanonicalExport(data) {
    const s = defaultState();

    (data.monthHours || []).forEach(mh => { s.monthHours[mh.month] = { hours: mh.hours, days: mh.days }; });

    s.groups = (data.groups || []).map(g => ({
      id: String(g.id), name: g.name, type: typeFromCanonical(g.type),
      dailyHours: g.dailyHours, staffPerShift: g.staffPerShift || 0,
      minRestHours: g.minRestHours != null ? g.minRestHours : 24,
      shiftTypes: (g.shiftTypes || []).map(st => ({ code: st.code, label: st.label, hours: st.hours }))
    }));

    s.employees = (data.employees || []).map(e => ({
      id: String(e.id), name: e.name, groupId: String(e.groupId),
      employmentFactor: e.employmentFactor != null ? e.employmentFactor : 1,
      maxVacationDays: e.maxVacationDays, notes: e.notes || '',
      excludedShiftCodes: e.excludedShiftCodes || []
    }));

    (data.schedule || []).forEach(entry => {
      const key = scheduleKey(entry.year, entry.month);
      if (!s.schedule[key]) s.schedule[key] = {};
      const empId = String(entry.employeeId);
      if (!s.schedule[key][empId]) s.schedule[key][empId] = {};
      s.schedule[key][empId][entry.day] = entry.code;
    });

    (data.carryOvers || []).forEach(entry => {
      const key = scheduleKey(entry.year, entry.month);
      if (!s.carryOver[key]) s.carryOver[key] = {};
      s.carryOver[key][String(entry.employeeId)] = entry.hours;
    });

    (data.holidaysExtra || []).forEach(entry => {
      if (!s.holidaysExtra[entry.year]) s.holidaysExtra[entry.year] = {};
      s.holidaysExtra[entry.year][entry.date] = entry.name;
    });

    (data.holidaysRemoved || []).forEach(entry => {
      if (!s.holidaysRemoved[entry.year]) s.holidaysRemoved[entry.year] = [];
      s.holidaysRemoved[entry.year].push(entry.date);
    });

    if (s.groups.length === 0) s.groups = defaultGroups();
    return s;
  }

  function exportToFile() {
    const data = JSON.stringify(toCanonicalExport(getState()), null, 2);
    const blob = new Blob([data], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    const now = new Date();
    const stamp = now.toISOString().slice(0, 10);
    a.href = url;
    a.download = 'beosztas-adatbazis-' + stamp + '.json';
    document.body.appendChild(a);
    a.click();
    a.remove();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }

  function importFromFile(file, cb) {
    const reader = new FileReader();
    reader.onload = function (e) {
      try {
        const parsed = JSON.parse(e.target.result);
        if (!parsed || typeof parsed !== 'object' || !Array.isArray(parsed.employees) || !Array.isArray(parsed.groups)) {
          throw new Error('A fájl formátuma nem megfelelő (nem a közös Beosztás Varázsló exportformátum).');
        }
        state = fromCanonicalExport(parsed);
        save();
        cb && cb(null);
      } catch (err) {
        cb && cb(err);
      }
    };
    reader.onerror = function () { cb && cb(new Error('Nem sikerült beolvasni a fájlt.')); };
    reader.readAsText(file, 'utf-8');
  }

  function resetAll() {
    state = defaultState();
    save();
  }

  global.App = global.App || {};
  global.App.DB = {
    STORAGE_KEY, MONTH_NAMES, DEFAULT_MONTH_HOURS, SPECIAL_CODES,
    uid, load, save, getState, scheduleKey,
    exportToFile, importFromFile, resetAll
  };
})(window);
