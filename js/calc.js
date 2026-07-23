/* Óraszám-számítási motor: kötelező havi óraszám, ledolgozott óra, maradvány. */
(function (global) {
  const DB = () => global.App.DB;
  const Holidays = () => global.App.Holidays;

  function daysInMonth(year, month) {
    return new Date(year, month, 0).getDate();
  }

  function isWeekend(year, month, day) {
    const dow = new Date(year, month - 1, day).getDay(); // 0=vasárnap,6=szombat
    return dow === 0 || dow === 6;
  }

  function getHolidayMap(state, year) {
    const auto = Holidays().defaultHolidays(year);
    const removed = (state.holidaysRemoved && state.holidaysRemoved[year]) || [];
    removed.forEach(d => delete auto[d]);
    const extra = (state.holidaysExtra && state.holidaysExtra[year]) || {};
    return Object.assign({}, auto, extra);
  }

  function isHoliday(state, year, month, day) {
    const iso = Holidays().toISO(year, month, day);
    const map = getHolidayMap(state, year);
    return !!map[iso];
  }

  // Egy adott csoport-tagra (iroda) az adott napon van-e alapértelmezett munkanap
  function isOfficeWorkday(state, year, month, day) {
    return !isWeekend(year, month, day) && !isHoliday(state, year, month, day);
  }

  function getShiftTypeByCode(group, code) {
    return (group.shiftTypes || []).find(s => s.code === code);
  }

  function getCell(state, year, month, employeeId, day) {
    const key = DB().scheduleKey(year, month);
    const monthData = state.schedule[key];
    if (!monthData) return '';
    const empData = monthData[employeeId];
    if (!empData) return '';
    return empData[day] || '';
  }

  function setCell(state, year, month, employeeId, day, code) {
    const key = DB().scheduleKey(year, month);
    if (!state.schedule[key]) state.schedule[key] = {};
    if (!state.schedule[key][employeeId]) state.schedule[key][employeeId] = {};
    if (!code) {
      delete state.schedule[key][employeeId][day];
    } else {
      state.schedule[key][employeeId][day] = code;
    }
  }

  function getCarryIn(state, year, month, employeeId) {
    const key = DB().scheduleKey(year, month);
    const m = state.carryOver[key];
    if (!m) return 0;
    return Number(m[employeeId] || 0);
  }

  function setCarryIn(state, year, month, employeeId, value) {
    const key = DB().scheduleKey(year, month);
    if (!state.carryOver[key]) state.carryOver[key] = {};
    state.carryOver[key][employeeId] = Number(value) || 0;
  }

  /* Egy dolgozó adott havi cellájának effektív órája (munkaóra) és típusa. */
  function cellHours(state, group, employee, year, month, day) {
    if (group.type === 'iroda') {
      if (!isOfficeWorkday(state, year, month, day)) return { hours: 0, kind: 'nonwork' };
      const code = getCell(state, year, month, employee.id, day);
      if (code === 'SZ') return { hours: 0, kind: 'vacation' };
      if (code === 'H' || code === 'BSZ') return { hours: 0, kind: 'absence' };
      return { hours: group.dailyHours, kind: 'work', code: 'M' };
    }
    const code = getCell(state, year, month, employee.id, day);
    if (!code) return { hours: 0, kind: 'empty' };
    if (code === 'SZ') return { hours: 0, kind: 'vacation' };
    if (code === 'H' || code === 'BSZ') return { hours: 0, kind: 'absence' };
    if (code === 'P') return { hours: 0, kind: 'rest' };
    const st = getShiftTypeByCode(group, code);
    if (st) return { hours: st.hours, kind: 'work', code: st.code };
    return { hours: 0, kind: 'empty' };
  }

  /* Egy dolgozó adott havi összesítése. */
  function summarizeEmployeeMonth(state, group, employee, year, month) {
    const dim = daysInMonth(year, month);
    let actualHours = 0, vacationDays = 0, absenceDays = 0, workDays = 0;
    for (let d = 1; d <= dim; d++) {
      const c = cellHours(state, group, employee, year, month, d);
      if (c.kind === 'work') { actualHours += c.hours; workDays++; }
      else if (c.kind === 'vacation') vacationDays++;
      else if (c.kind === 'absence') absenceDays++;
    }
    const baseHours = (state.monthHours[month] && state.monthHours[month].hours) || 0;
    const employmentFactor = employee.employmentFactor != null ? employee.employmentFactor : 1;
    const requiredFull = baseHours * employmentFactor;
    const vacationHours = vacationDays * group.dailyHours;
    const absenceHours = absenceDays * group.dailyHours;
    const carryIn = getCarryIn(state, year, month, employee.id);
    const effectiveRequired = requiredFull - vacationHours - absenceHours - carryIn;
    const balance = actualHours - effectiveRequired;
    return {
      baseHours, employmentFactor, requiredFull, vacationDays, absenceDays, workDays,
      vacationHours, absenceHours, carryIn, effectiveRequired, actualHours, balance
    };
  }

  /* Éves szabadság-felhasználás (az összes hónapon át, adott évben). */
  function yearVacationUsed(state, employeeId, year) {
    let total = 0;
    for (let m = 1; m <= 12; m++) {
      const key = DB().scheduleKey(year, m);
      const monthData = state.schedule[key];
      if (!monthData || !monthData[employeeId]) continue;
      const days = monthData[employeeId];
      for (const d in days) {
        if (days[d] === 'SZ') total++;
      }
    }
    return total;
  }

  /* Műszak-lefedettség: adott napon, adott shiftType kódon hány dolgozó van beosztva. */
  function shiftCoverage(state, group, employees, year, month, day) {
    const counts = {};
    (group.shiftTypes || []).forEach(st => counts[st.code] = 0);
    employees.forEach(emp => {
      const code = getCell(state, year, month, emp.id, day);
      if (code && counts.hasOwnProperty(code)) counts[code]++;
    });
    return counts;
  }

  /**
   * Hirtelen beteg szabadság esetén automatikus helyettes-keresés: az adott napon szabad
   * (aznapra még be nem osztott) dolgozók közül azt választja, akinek eddig a legkevesebb
   * ledolgozott órája van ebben a hónapban (méltányos terheléselosztás), és őt állítja be
   * a beteg dolgozó műszakjára. Csak azoknál a csoportoknál releváns, ahol meg van adva az
   * egy műszakban szükséges létszám (pl. egymást váltó 12 órás ápolók) - ott hívandó, ahol
   * egy dolgozó munkanapja beteg szabadságra (BSZ) változik.
   * @returns a helyettesítő dolgozó objektuma, vagy null, ha nincs elérhető helyettes.
   */
  function findSickSubstitute(state, group, employees, year, month, day, sickEmployeeId, shiftCode) {
    let best = null;
    let bestHours = Infinity;
    employees.forEach(emp => {
      if (emp.id === sickEmployeeId) return;
      const code = getCell(state, year, month, emp.id, day);
      if (code) return; // aznap már be van osztva valamire (munka, szabadság, pihenő stb.)
      const summary = summarizeEmployeeMonth(state, group, emp, year, month);
      if (summary.actualHours < bestHours) {
        bestHours = summary.actualHours;
        best = emp;
      }
    });
    if (best) {
      setCell(state, year, month, best.id, day, shiftCode);
    }
    return best;
  }

  global.App = global.App || {};
  global.App.Calc = {
    daysInMonth, isWeekend, getHolidayMap, isHoliday, isOfficeWorkday,
    getShiftTypeByCode, getCell, setCell, getCarryIn, setCarryIn,
    cellHours, summarizeEmployeeMonth, yearVacationUsed, shiftCoverage,
    findSickSubstitute
  };
})(window);
