/* "Beállítások" fül: havi kötelező óraszám tábla + munkaszüneti napok kezelése. */
(function (global) {
  function render() {
    const UI = global.App.UI;
    const DB = global.App.DB;
    const state = DB.getState();

    renderMonthHours(UI, DB, state);
    renderHolidays(UI, DB, state);
  }

  function renderMonthHours(UI, DB, state) {
    const container = document.getElementById('monthHoursTable');
    container.innerHTML = '';
    const table = document.createElement('table');
    table.innerHTML = '<thead><tr><th>Hónap</th><th>Kötelező óraszám</th><th>Munkanapok száma</th></tr></thead>';
    const tbody = document.createElement('tbody');
    for (let m = 1; m <= 12; m++) {
      const rec = state.monthHours[m];
      const tr = document.createElement('tr');
      const tdName = document.createElement('td');
      tdName.textContent = DB.MONTH_NAMES[m - 1];
      const tdHours = document.createElement('td');
      const hoursInput = UI.el('input', {
        type: 'number', min: '0', value: rec.hours, style: 'width:6em',
        onchange: (e) => { rec.hours = Number(e.target.value) || 0; DB.save(); }
      });
      tdHours.appendChild(hoursInput);
      const tdDays = document.createElement('td');
      const daysInput = UI.el('input', {
        type: 'number', min: '0', value: rec.days, style: 'width:6em',
        onchange: (e) => { rec.days = Number(e.target.value) || 0; DB.save(); }
      });
      tdDays.appendChild(daysInput);
      tr.appendChild(tdName); tr.appendChild(tdHours); tr.appendChild(tdDays);
      tbody.appendChild(tr);
    }
    table.appendChild(tbody);
    container.appendChild(table);
  }

  function currentHolidayYear() {
    const input = document.getElementById('holidayYear');
    return Number(input.value) || new Date().getFullYear();
  }

  function renderHolidays(UI, DB, state) {
    const yearInput = document.getElementById('holidayYear');
    if (!yearInput.value) yearInput.value = new Date().getFullYear();
    const year = currentHolidayYear();

    const list = document.getElementById('holidayList');
    list.innerHTML = '';
    const Calc = global.App.Calc;
    const map = Calc.getHolidayMap(state, year);
    const removed = new Set((state.holidaysRemoved && state.holidaysRemoved[year]) || []);
    const extra = (state.holidaysExtra && state.holidaysExtra[year]) || {};
    const autoDefaults = global.App.Holidays.defaultHolidays(year);

    const dates = Object.keys(Object.assign({}, autoDefaults, extra)).sort();
    const table = document.createElement('table');
    table.innerHTML = '<thead><tr><th>Dátum</th><th>Név</th><th>Típus</th><th></th></tr></thead>';
    const tbody = document.createElement('tbody');
    dates.forEach(date => {
      const isAuto = date in autoDefaults;
      const isRemovedAuto = isAuto && removed.has(date);
      const name = isRemovedAuto ? autoDefaults[date] : (map[date] || autoDefaults[date] || extra[date]);
      const tr = document.createElement('tr');
      if (isRemovedAuto) tr.style.opacity = '.45';
      tr.appendChild(UI.el('td', {}, [date]));
      tr.appendChild(UI.el('td', {}, [name]));
      tr.appendChild(UI.el('td', {}, [isAuto ? 'automatikus' : 'egyéni']));
      const tdAction = document.createElement('td');
      if (isAuto) {
        const btn = UI.el('button', {
          class: 'small',
          onclick: () => {
            if (!state.holidaysRemoved[year]) state.holidaysRemoved[year] = [];
            if (isRemovedAuto) {
              state.holidaysRemoved[year] = state.holidaysRemoved[year].filter(d => d !== date);
            } else {
              state.holidaysRemoved[year].push(date);
            }
            DB.save(); renderHolidays(UI, DB, state);
          }
        }, [isRemovedAuto ? 'Visszaállítás' : 'Kikapcsolás']);
        tdAction.appendChild(btn);
      } else {
        const btn = UI.el('button', {
          class: 'small danger',
          onclick: () => {
            delete state.holidaysExtra[year][date];
            DB.save(); renderHolidays(UI, DB, state);
          }
        }, ['Törlés']);
        tdAction.appendChild(btn);
      }
      tr.appendChild(tdAction);
      tbody.appendChild(tr);
    });
    table.appendChild(tbody);
    list.appendChild(table);
  }

  function wireEvents() {
    const UI = global.App.UI;
    const DB = global.App.DB;

    document.getElementById('btnResetMonthHours').addEventListener('click', () => {
      UI.confirmDialog('Visszaállítod az alapértelmezett havi óraszám-táblát?', () => {
        const state = DB.getState();
        state.monthHours = JSON.parse(JSON.stringify(DB.DEFAULT_MONTH_HOURS));
        DB.save();
        render();
        UI.toast('Alapértékek visszaállítva.', 'success');
      });
    });

    document.getElementById('holidayYear').addEventListener('change', render);

    document.getElementById('btnAddHoliday').addEventListener('click', () => {
      const state = DB.getState();
      const year = currentHolidayYear();
      const dateInput = document.getElementById('newHolidayDate');
      const nameInput = document.getElementById('newHolidayName');
      if (!dateInput.value || !nameInput.value.trim()) {
        UI.toast('Add meg a dátumot és a nevet is.', 'error');
        return;
      }
      if (!state.holidaysExtra[year]) state.holidaysExtra[year] = {};
      state.holidaysExtra[year][dateInput.value] = nameInput.value.trim();
      DB.save();
      dateInput.value = ''; nameInput.value = '';
      render();
      UI.toast('Ünnepnap hozzáadva.', 'success');
    });
  }

  global.App = global.App || {};
  global.App.UISettings = { render, wireEvents };
})(window);
