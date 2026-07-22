/* "Dolgozók" fül: dolgozók listázása (munkakör szerint ABC sorrendben), CRUD. */
(function (global) {
  function vacationYear() {
    const input = document.getElementById('empVacationYear');
    return Number(input.value) || new Date().getFullYear();
  }

  function render() {
    const UI = global.App.UI;
    const DB = global.App.DB;
    const Calc = global.App.Calc;
    const state = DB.getState();

    const yearInput = document.getElementById('empVacationYear');
    if (!yearInput.value) yearInput.value = new Date().getFullYear();
    const year = vacationYear();

    const container = document.getElementById('employeesList');
    container.innerHTML = '';

    if (state.groups.length === 0) {
      container.appendChild(UI.el('p', { class: 'hint' }, ['Előbb hozz létre legalább egy munkacsoportot a "Munkacsoportok" fülön.']));
      return;
    }

    const groupsSorted = state.groups.slice().sort((a, b) => a.name.localeCompare(b.name, 'hu'));
    groupsSorted.forEach(group => {
      const emps = state.employees
        .filter(e => e.groupId === group.id)
        .sort((a, b) => a.name.localeCompare(b.name, 'hu'));
      const card = UI.el('div', { class: 'card' });
      card.appendChild(UI.el('h2', {}, [group.name]));
      if (emps.length === 0) {
        card.appendChild(UI.el('p', { class: 'hint' }, ['Nincs még dolgozó ebben a csoportban.']));
      } else {
        const table = document.createElement('table');
        table.innerHTML = '<thead><tr><th>Név</th><th>Munkaidő-arány</th>' +
          '<th>Max. szabadság/év</th><th>Kivett szabadság (' + year + ')</th><th>Hátralévő</th><th></th></tr></thead>';
        const tbody = document.createElement('tbody');
        emps.forEach(emp => {
          const used = Calc.yearVacationUsed(state, emp.id, year);
          const remaining = emp.maxVacationDays - used;
          const tr = document.createElement('tr');
          tr.appendChild(UI.el('td', {}, [emp.name]));
          tr.appendChild(UI.el('td', {}, [String(emp.employmentFactor != null ? emp.employmentFactor : 1)]));
          tr.appendChild(UI.el('td', {}, [String(emp.maxVacationDays)]));
          tr.appendChild(UI.el('td', {}, [String(used)]));
          const remCell = UI.el('td', {}, [String(remaining)]);
          if (remaining < 0) remCell.className = 'badge bad';
          tr.appendChild(remCell);
          const tdAction = document.createElement('td');
          tdAction.appendChild(UI.el('button', { class: 'small', onclick: () => openEmployeeForm(emp) }, ['Szerkesztés']));
          tdAction.appendChild(document.createTextNode(' '));
          tdAction.appendChild(UI.el('button', {
            class: 'small danger',
            onclick: () => UI.confirmDialog('Biztosan törlöd "' + emp.name + '" dolgozót? Ez a beosztási adatait is érvényteleníti.', () => {
              state.employees = state.employees.filter(x => x.id !== emp.id);
              DB.save(); render();
              UI.toast('Dolgozó törölve.', 'success');
            })
          }, ['Törlés']));
          tr.appendChild(tdAction);
          tbody.appendChild(tr);
        });
        table.appendChild(tbody);
        card.appendChild(table);
      }
      container.appendChild(card);
    });
  }

  function openEmployeeForm(employee) {
    const UI = global.App.UI;
    const DB = global.App.DB;
    const state = DB.getState();
    const isNew = !employee;
    const emp = employee || { id: DB.uid('emp'), name: '', groupId: state.groups[0] ? state.groups[0].id : '', employmentFactor: 1, maxVacationDays: 20, notes: '' };

    const groupOptions = state.groups.slice().sort((a, b) => a.name.localeCompare(b.name, 'hu'))
      .map(g => '<option value="' + g.id + '"' + (g.id === emp.groupId ? ' selected' : '') + '>' + g.name + '</option>').join('');

    const body = document.createElement('div');
    body.innerHTML =
      '<div class="form-grid">' +
      '<label class="full">Név<input type="text" id="eName" value="' + (emp.name || '').replace(/"/g, '&quot;') + '"></label>' +
      '<label>Munkacsoport<select id="eGroup">' + groupOptions + '</select></label>' +
      '<label>Munkaidő-arány (1 = teljes, 0.5 = fél)<input type="number" id="eFactor" step="0.05" min="0" max="2" value="' + (emp.employmentFactor != null ? emp.employmentFactor : 1) + '"></label>' +
      '<label>Max. kiadható szabadság (nap/év) <span style="color:#dc2626">*kötelező</span><input type="number" id="eMaxVac" min="0" required value="' + emp.maxVacationDays + '"></label>' +
      '<label class="full">Megjegyzés<textarea id="eNotes" rows="2">' + (emp.notes || '') + '</textarea></label>' +
      '</div>';

    UI.openModal({
      title: isNew ? 'Új dolgozó' : 'Dolgozó szerkesztése',
      bodyNode: body,
      actions: [
        { label: 'Mégse' },
        {
          label: 'Mentés', primary: true,
          onClick: (close) => {
            const name = document.getElementById('eName').value.trim();
            const groupId = document.getElementById('eGroup').value;
            const factor = Number(document.getElementById('eFactor').value);
            const maxVacRaw = document.getElementById('eMaxVac').value;
            const notes = document.getElementById('eNotes').value.trim();
            if (!name) { UI.toast('A név megadása kötelező.', 'error'); return; }
            if (!groupId) { UI.toast('Válassz munkacsoportot.', 'error'); return; }
            if (maxVacRaw === '' || Number(maxVacRaw) < 0 || isNaN(Number(maxVacRaw))) {
              UI.toast('A max. kiadható szabadság megadása kötelező (0 vagy több).', 'error');
              return;
            }
            emp.name = name; emp.groupId = groupId;
            emp.employmentFactor = isNaN(factor) || factor <= 0 ? 1 : factor;
            emp.maxVacationDays = Number(maxVacRaw);
            emp.notes = notes;
            if (isNew) state.employees.push(emp);
            DB.save();
            close();
            render();
            if (global.App.UISchedule) global.App.UISchedule.render();
            UI.toast('Dolgozó elmentve.', 'success');
          }
        }
      ]
    });
  }

  function wireEvents() {
    document.getElementById('btnAddEmployee').addEventListener('click', () => openEmployeeForm(null));
    document.getElementById('empVacationYear').addEventListener('change', render);
  }

  global.App = global.App || {};
  global.App.UIEmployees = { render, wireEvents };
})(window);
