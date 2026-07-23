/* "Beosztás" fül: hónapválasztó, naptár rács, óraszám-összesítés, lefedettség. */
(function (global) {
  let selYear = new Date().getFullYear();
  let selMonth = new Date().getMonth() + 1;

  function dowLetter(year, month, day) {
    return ['V', 'H', 'K', 'Sze', 'Cs', 'P', 'Szo'][new Date(year, month - 1, day).getDay()];
  }

  function sortedGroupsWithEmployees(state) {
    return state.groups.slice().sort((a, b) => a.name.localeCompare(b.name, 'hu')).map(group => ({
      group,
      employees: state.employees.filter(e => e.groupId === group.id).sort((a, b) => a.name.localeCompare(b.name, 'hu'))
    }));
  }

  function cellOptionsFor(group) {
    if (group.type === 'iroda') {
      return [
        { value: '', label: 'Munka' },
        { value: 'SZ', label: 'SZ – Szabadság' },
        { value: 'BSZ', label: 'BSZ – Beteg szabadság' },
        { value: 'H', label: 'H – Hiányzás' }
      ];
    }
    const opts = [{ value: '', label: '—' }];
    (group.shiftTypes || []).forEach(st => opts.push({ value: st.code, label: st.code + ' – ' + st.label }));
    opts.push({ value: 'SZ', label: 'SZ – Szabadság' });
    opts.push({ value: 'BSZ', label: 'BSZ – Beteg szabadság' });
    opts.push({ value: 'H', label: 'H – Hiányzás' });
    opts.push({ value: 'P', label: 'P – Pihenőnap' });
    return opts;
  }

  function renderMonthList() {
    const DB = global.App.DB;
    const yearInput = document.getElementById('schYear');
    if (!yearInput.value) yearInput.value = selYear;
    const wrap = document.getElementById('monthList');
    wrap.innerHTML = '';
    for (let m = 1; m <= 12; m++) {
      const btn = document.createElement('button');
      btn.textContent = DB.MONTH_NAMES[m - 1];
      if (m === selMonth) btn.classList.add('active');
      btn.addEventListener('click', () => { selMonth = m; render(); });
      wrap.appendChild(btn);
    }
  }

  function tryApplyVacation(state, Calc, employee, group, day, year, month, oldCode, newCode) {
    if (newCode !== 'SZ' || oldCode === 'SZ') return true;
    const used = Calc.yearVacationUsed(state, employee.id, year);
    if (used + 1 > employee.maxVacationDays) {
      global.App.UI.toast(
        employee.name + ' már elérte a max. kiadható szabadság napok számát (' + employee.maxVacationDays + ' nap, ' + year + '). Nem jelölhető ki több szabadnap.',
        'error'
      );
      return false;
    }
    return true;
  }

  function renderScheduleTable() {
    const UI = global.App.UI;
    const DB = global.App.DB;
    const Calc = global.App.Calc;
    const state = DB.getState();

    document.getElementById('scheduleTitle').textContent =
      'Beosztás – ' + DB.MONTH_NAMES[selMonth - 1] + ' ' + selYear + ' (' + selMonth + '. hónap)';

    const dim = Calc.daysInMonth(selYear, selMonth);
    const groups = sortedGroupsWithEmployees(state);

    const table = document.createElement('table');
    table.className = 'schedule';

    const thead = document.createElement('thead');
    const headRow = document.createElement('tr');
    headRow.appendChild(UI.el('th', { class: 'name-col' }, ['Dolgozó']));
    for (let d = 1; d <= dim; d++) {
      const dl = dowLetter(selYear, selMonth, d);
      const th = UI.el('th', { class: (dl === 'Szo' ? 'dow-sat' : dl === 'V' ? 'dow-sun' : '') }, [String(d) + ' ' + dl]);
      headRow.appendChild(th);
    }
    ['Kötelező ó.', 'Ledolg. ó.', 'Szab. nap', 'Bejövő ó.', 'Egyenleg/köv.hó'].forEach(h => headRow.appendChild(UI.el('th', { class: 'stat-col' }, [h])));
    thead.appendChild(headRow);
    table.appendChild(thead);

    const tbody = document.createElement('tbody');

    groups.forEach(({ group, employees }) => {
      if (employees.length === 0) return;
      const gr = document.createElement('tr');
      gr.className = 'group-row';
      const gtd = document.createElement('td');
      gtd.colSpan = 1 + dim + 5;
      gtd.textContent = group.name;
      gr.appendChild(gtd);
      tbody.appendChild(gr);

      employees.forEach(emp => {
        const tr = document.createElement('tr');
        tr.appendChild(UI.el('td', { class: 'name-col' }, [emp.name]));

        for (let d = 1; d <= dim; d++) {
          const td = document.createElement('td');
          td.className = 'day-cell';
          const isHoliday = Calc.isHoliday(state, selYear, selMonth, d);
          const isWeekend = Calc.isWeekend(selYear, selMonth, d);

          if (group.type === 'iroda' && !Calc.isOfficeWorkday(state, selYear, selMonth, d)) {
            td.classList.add('nonwork');
            if (isHoliday) td.classList.add('holiday'); else if (isWeekend) td.classList.add('weekend');
            td.textContent = isHoliday ? 'Ü' : '·';
            tr.appendChild(td);
            continue;
          }
          if (isHoliday) td.classList.add('holiday');
          else if (isWeekend) td.classList.add('weekend');

          const code = Calc.getCell(state, selYear, selMonth, emp.id, d);
          if (code) { td.classList.add('code-' + code.replace(/[^A-Za-zÀ-ž]/g, '_')); td.textContent = code; }
          else td.textContent = '';

          td.title = emp.name + ' – ' + d + '. nap';
          td.addEventListener('click', () => openCellPicker(td, group, emp, d));
          tr.appendChild(td);
        }

        const summary = Calc.summarizeEmployeeMonth(state, group, emp, selYear, selMonth);
        tr.appendChild(UI.el('td', { class: 'stat-col' }, [summary.requiredFull.toFixed(1)]));
        tr.appendChild(UI.el('td', { class: 'stat-col' }, [summary.actualHours.toFixed(1)]));
        tr.appendChild(UI.el('td', { class: 'stat-col' }, [String(summary.vacationDays)]));

        const carryTd = document.createElement('td');
        const carryInput = UI.el('input', {
          type: 'number', class: 'carry-in', value: summary.carryIn,
          onchange: (e) => {
            Calc.setCarryIn(state, selYear, selMonth, emp.id, Number(e.target.value) || 0);
            DB.save();
            renderScheduleTable();
          }
        });
        carryTd.appendChild(carryInput);
        tr.appendChild(carryTd);

        const balTd = UI.el('td', { class: 'stat-col ' + (summary.balance < 0 ? 'balance-neg' : 'balance-pos') }, [summary.balance.toFixed(1)]);
        tr.appendChild(balTd);

        tbody.appendChild(tr);
      });
    });

    table.appendChild(tbody);
    const wrap = document.getElementById('scheduleTableWrap');
    wrap.innerHTML = '';
    wrap.appendChild(table);

    renderLegend();
    renderCoverage(groups);
  }

  function openCellPicker(td, group, employee, day) {
    const UI = global.App.UI;
    const DB = global.App.DB;
    const Calc = global.App.Calc;
    const state = DB.getState();
    const oldCode = Calc.getCell(state, selYear, selMonth, employee.id, day);
    const opts = cellOptionsFor(group);

    const select = document.createElement('select');
    opts.forEach(o => {
      const optEl = document.createElement('option');
      optEl.value = o.value; optEl.textContent = o.label;
      if (o.value === oldCode) optEl.selected = true;
      select.appendChild(optEl);
    });
    td.textContent = '';
    td.appendChild(select);
    select.focus();

    function commit() {
      const newCode = select.value;
      if (!tryApplyVacation(state, Calc, employee, group, day, selYear, selMonth, oldCode, newCode)) {
        renderScheduleTable();
        return;
      }
      Calc.setCell(state, selYear, selMonth, employee.id, day, newCode);

      // Hirtelen beteg szabadság: ha egy műszakban dolgozó (egymást váltó) csoporttag
      // munkanapja beteg szabadságra vált, automatikusan keresünk rá helyettest.
      const wasWorkingShift = (group.shiftTypes || []).some(st => st.code === oldCode);
      if (newCode === 'BSZ' && oldCode !== 'BSZ' && group.staffPerShift > 0 && wasWorkingShift) {
        const groupEmployees = state.employees.filter(e => e.groupId === group.id);
        const substitute = Calc.findSickSubstitute(state, group, groupEmployees, selYear, selMonth, day, employee.id, oldCode);
        DB.save();
        if (substitute) {
          UI.toast(
            employee.name + ' beteg szabadságra került (' + day + '. nap). Automatikus helyettes: ' +
            substitute.name + ' (' + oldCode + ' műszak).',
            'success'
          );
        } else {
          UI.toast(
            employee.name + ' beteg szabadságra került (' + day + '. nap), de nincs elérhető szabad helyettes - a műszak létszáma emiatt a szükséges alá csökkenhet!',
            'error'
          );
        }
      } else {
        DB.save();
      }
      renderScheduleTable();
    }
    select.addEventListener('change', commit);
    select.addEventListener('blur', () => renderScheduleTable());
  }

  function renderLegend() {
    const DB = global.App.DB;
    const wrap = document.getElementById('scheduleLegend');
    wrap.innerHTML = '';
    const items = [
      ['#dcf5df', 'SZ – Szabadság'], ['#f8d7d5', 'BSZ – Beteg szabadság'],
      ['#efe6e0', 'H – Egyéb hiányzás'], ['#e7ebee', 'P – Pihenőnap'],
      ['#fde3e3', 'Ünnepnap'], ['#f5f5f5', 'Hétvége'], ['#f0f0f0', 'Nem munkanap (iroda)']
    ];
    items.forEach(([color, label]) => {
      const span = document.createElement('span');
      span.innerHTML = '<span class="swatch" style="background:' + color + '"></span>' + label;
      wrap.appendChild(span);
    });
  }

  function renderCoverage(groups) {
    const UI = global.App.UI;
    const Calc = global.App.Calc;
    const DB = global.App.DB;
    const state = DB.getState();
    const dim = Calc.daysInMonth(selYear, selMonth);
    const wrap = document.getElementById('coverageWrap');
    wrap.innerHTML = '';

    const relevant = groups.filter(({ group }) => group.staffPerShift > 0 && group.shiftTypes && group.shiftTypes.length);
    if (relevant.length === 0) {
      wrap.appendChild(UI.el('p', { class: 'hint' }, ['Nincs olyan munkacsoport, amelyhez létszám-elvárás lenne beállítva (lásd Munkacsoportok fül – "Szükséges létszám egy műszakban").']));
      return;
    }

    relevant.forEach(({ group, employees }) => {
      wrap.appendChild(UI.el('h3', {}, [group.name + ' (elvárt létszám/műszak: ' + group.staffPerShift + ' fő)']));
      const table = document.createElement('table');
      table.className = 'coverage-table';
      const thead = document.createElement('tr');
      thead.appendChild(UI.el('th', {}, ['Műszak']));
      for (let d = 1; d <= dim; d++) thead.appendChild(UI.el('th', {}, [String(d)]));
      table.appendChild(thead);

      group.shiftTypes.forEach(st => {
        const tr = document.createElement('tr');
        tr.appendChild(UI.el('td', {}, [st.label + ' (' + st.code + ')']));
        for (let d = 1; d <= dim; d++) {
          const counts = Calc.shiftCoverage(state, group, employees, selYear, selMonth, d);
          const n = counts[st.code] || 0;
          const cls = n < group.staffPerShift ? 'under' : n > group.staffPerShift ? 'over' : 'ok';
          tr.appendChild(UI.el('td', { class: cls }, [String(n)]));
        }
        table.appendChild(tr);
      });
      wrap.appendChild(table);
    });
  }

  function render() {
    renderMonthList();
    renderScheduleTable();
  }

  function copyPrevMonthCarry() {
    const UI = global.App.UI;
    const DB = global.App.DB;
    const Calc = global.App.Calc;
    const state = DB.getState();
    let prevYear = selYear, prevMonth = selMonth - 1;
    if (prevMonth < 1) { prevMonth = 12; prevYear = selYear - 1; }

    state.employees.forEach(emp => {
      const group = state.groups.find(g => g.id === emp.groupId);
      if (!group) return;
      const prevSummary = Calc.summarizeEmployeeMonth(state, group, emp, prevYear, prevMonth);
      Calc.setCarryIn(state, selYear, selMonth, emp.id, Number(prevSummary.balance.toFixed(2)));
    });
    DB.save();
    renderScheduleTable();
    UI.toast('Előző havi (' + DB.MONTH_NAMES[prevMonth - 1] + ' ' + prevYear + ') egyenleg átmásolva bejövő óraként.', 'success');
  }

  function autoFillMonth() {
    const UI = global.App.UI;
    const DB = global.App.DB;
    const Calc = global.App.Calc;
    const state = DB.getState();

    UI.confirmDialog(
      'Automatikusan kitölti a(z) ' + DB.MONTH_NAMES[selMonth - 1] + ' ' + selYear + ' hónap ÜRES celláit: ' +
      'irodai csoportoknál minden munkanapra munkaórát ír, az egymást váltó (létszám-figyelt) csoportoknál pedig ' +
      'a beállított pihenőidőt betartva, méltányosan elosztva jelöl ki dolgozókat műszakra. A már kitöltött cellákat nem érinti. Folytatod?',
      () => {
        const result = Calc.autoFillMonth(state, selYear, selMonth);
        DB.save();
        renderScheduleTable();
        if (result.filledCells === 0 && result.shortfalls.length === 0) {
          UI.toast('Nem volt kitöltendő üres cella ebben a hónapban.', 'success');
        } else if (result.shortfalls.length === 0) {
          UI.toast(result.filledCells + ' cella automatikusan kitöltve.', 'success');
        } else {
          const details = result.shortfalls.slice(0, 5).map(s =>
            s.group + ' – ' + s.day + '. nap, ' + s.shiftLabel + ': ' + s.assigned + '/' + s.needed + ' fő'
          ).join('; ');
          UI.toast(
            result.filledCells + ' cella kitöltve, de ' + result.shortfalls.length + ' esetben nem volt elég szabad/pihent dolgozó (' +
            details + (result.shortfalls.length > 5 ? '; …' : '') + ').',
            'error'
          );
        }
      }
    );
  }

  function wireEvents() {
    document.getElementById('schYear').addEventListener('change', (e) => {
      selYear = Number(e.target.value) || selYear;
      render();
    });
    document.getElementById('btnCopyPrevCarry').addEventListener('click', copyPrevMonthCarry);
    document.getElementById('btnAutoFill').addEventListener('click', autoFillMonth);
  }

  function getSelection() { return { year: selYear, month: selMonth }; }

  global.App = global.App || {};
  global.App.UISchedule = { render, wireEvents, getSelection, sortedGroupsWithEmployees, dowLetter };
})(window);
