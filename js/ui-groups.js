/* "Munkacsoportok" fül: munkarendek (ápolók, takarítók, irodaiak stb.) kezelése. */
(function (global) {
  function render() {
    const UI = global.App.UI;
    const DB = global.App.DB;
    const state = DB.getState();
    const container = document.getElementById('groupsList');
    container.innerHTML = '';

    state.groups.slice().sort((a, b) => a.name.localeCompare(b.name, 'hu')).forEach(group => {
      const empCount = state.employees.filter(e => e.groupId === group.id).length;
      const card = UI.el('div', { class: 'card' });
      card.appendChild(UI.el('h2', {}, [group.name + ' ', UI.el('span', { class: 'badge' }, [
        group.type === 'iroda' ? 'Iroda (hétfő-péntek)' : 'Általános (váltásos/napi)'
      ])]));
      const shiftDesc = (group.shiftTypes || []).map(s => s.label + ' (' + s.code + ', ' + s.hours + ' óra)').join(', ');
      card.appendChild(UI.el('p', { class: 'hint' }, [
        'Napi óraszám: ' + group.dailyHours + ' óra · Műszakok: ' + (shiftDesc || '—') +
        (group.staffPerShift ? (' · Egy műszakban szükséges létszám: ' + group.staffPerShift + ' fő') : '') +
        (group.type === 'altalanos' ? (' · Min. pihenőidő 12 órás műszak után: ' + (group.minRestHours != null ? group.minRestHours : 24) + ' óra') : '') +
        ' · Dolgozók: ' + empCount + ' fő'
      ]));
      const row = UI.el('div', { class: 'row' });
      row.appendChild(UI.el('button', { onclick: () => openGroupForm(group) }, ['Szerkesztés']));
      const delBtn = UI.el('button', {
        class: 'danger',
        onclick: () => {
          if (empCount > 0) { UI.toast('Nem törölhető: vannak hozzá rendelt dolgozók.', 'error'); return; }
          UI.confirmDialog('Biztosan törlöd a(z) "' + group.name + '" munkacsoportot?', () => {
            state.groups = state.groups.filter(g => g.id !== group.id);
            DB.save(); render();
            UI.toast('Munkacsoport törölve.', 'success');
          });
        }
      }, ['Törlés']);
      row.appendChild(delBtn);
      card.appendChild(row);
      container.appendChild(card);
    });
  }

  function shiftTypesEditorHtml(shiftTypes) {
    return shiftTypes.map((s, i) =>
      '<div class="row shift-type-row" data-idx="' + i + '">' +
      '<input type="text" class="st-code" value="' + s.code + '" placeholder="Kód" style="width:4em">' +
      '<input type="text" class="st-label" value="' + s.label + '" placeholder="Megnevezés" style="width:9em">' +
      '<input type="number" class="st-hours" value="' + s.hours + '" placeholder="Óra" style="width:5em">' +
      '<button type="button" class="small danger st-remove">Törlés</button>' +
      '</div>'
    ).join('');
  }

  function readShiftTypesFromForm(box) {
    return Array.from(box.querySelectorAll('.shift-type-row')).map(row => ({
      code: row.querySelector('.st-code').value.trim(),
      label: row.querySelector('.st-label').value.trim(),
      hours: Number(row.querySelector('.st-hours').value) || 0
    })).filter(s => s.code);
  }

  function openGroupForm(group) {
    const UI = global.App.UI;
    const DB = global.App.DB;
    const state = DB.getState();
    const isNew = !group;
    const g = group || { id: DB.uid('grp'), name: '', type: 'altalanos', dailyHours: 8, staffPerShift: 0, minRestHours: 24, shiftTypes: [{ code: 'M', label: 'Munka', hours: 8 }] };

    const body = document.createElement('div');
    body.innerHTML =
      '<div class="form-grid">' +
      '<label class="full">Munkacsoport neve<input type="text" id="gName" value="' + (g.name || '').replace(/"/g, '&quot;') + '"></label>' +
      '<label>Típus<select id="gType">' +
      '<option value="altalanos"' + (g.type === 'altalanos' ? ' selected' : '') + '>Általános (bármely nap, váltásos is)</option>' +
      '<option value="iroda"' + (g.type === 'iroda' ? ' selected' : '') + '>Iroda (hétfő-péntek, ünnepnap kivétel)</option>' +
      '</select></label>' +
      '<label>Napi óraszám (egy műszak/munkanap hossza)<input type="number" id="gDailyHours" value="' + g.dailyHours + '"></label>' +
      '<label>Szükséges létszám egy műszakban (0 = nincs figyelve)<input type="number" id="gStaffPerShift" value="' + (g.staffPerShift || 0) + '"></label>' +
      '<label id="gMinRestWrap">Min. pihenőidő egy műszak után (óra) - automatikus kitöltéshez<input type="number" id="gMinRestHours" min="0" step="1" value="' + (g.minRestHours != null ? g.minRestHours : 24) + '"></label>' +
      '</div>' +
      '<h3 style="margin-top:1em">Műszaktípusok (iroda esetén automatikusan 1 típus)</h3>' +
      '<div id="shiftTypesEditor">' + shiftTypesEditorHtml(g.shiftTypes && g.shiftTypes.length ? g.shiftTypes : [{ code: 'M', label: 'Munka', hours: g.dailyHours }]) + '</div>' +
      '<button type="button" id="btnAddShiftType" class="small">+ Új műszaktípus</button>';

    UI.openModal({
      title: isNew ? 'Új munkacsoport' : 'Munkacsoport szerkesztése',
      bodyNode: body,
      onMount: (box) => {
        const typeSelect = box.querySelector('#gType');
        const editor = box.querySelector('#shiftTypesEditor');
        const addBtn = box.querySelector('#btnAddShiftType');
        const minRestWrap = box.querySelector('#gMinRestWrap');

        function syncForType() {
          const isOffice = typeSelect.value === 'iroda';
          addBtn.style.display = isOffice ? 'none' : '';
          minRestWrap.style.display = isOffice ? 'none' : '';
          box.querySelectorAll('.shift-type-row .st-remove').forEach(b => b.style.display = isOffice ? 'none' : '');
          if (isOffice) {
            const dh = Number(box.querySelector('#gDailyHours').value) || 8;
            editor.innerHTML = shiftTypesEditorHtml([{ code: 'M', label: 'Munka', hours: dh }]);
          }
        }
        typeSelect.addEventListener('change', syncForType);
        box.querySelector('#gDailyHours').addEventListener('change', syncForType);

        editor.addEventListener('click', (ev) => {
          if (ev.target.classList.contains('st-remove')) {
            ev.target.closest('.shift-type-row').remove();
          }
        });
        addBtn.addEventListener('click', () => {
          editor.insertAdjacentHTML('beforeend', shiftTypesEditorHtml([{ code: '', label: '', hours: g.dailyHours }]));
        });

        syncForType();
      },
      actions: [
        { label: 'Mégse' },
        {
          label: 'Mentés', primary: true,
          onClick: (close) => {
            const name = document.getElementById('gName').value.trim();
            if (!name) { UI.toast('A munkacsoport neve kötelező.', 'error'); return; }
            const type = document.getElementById('gType').value;
            const dailyHours = Number(document.getElementById('gDailyHours').value) || 0;
            const staffPerShift = Number(document.getElementById('gStaffPerShift').value) || 0;
            const minRestHours = Math.max(0, Number(document.getElementById('gMinRestHours').value) || 0);
            const box = document.querySelector('.modal-box');
            let shiftTypes = readShiftTypesFromForm(box);
            if (type === 'iroda') shiftTypes = [{ code: 'M', label: 'Munka', hours: dailyHours }];
            if (!shiftTypes.length) { UI.toast('Legalább egy műszaktípus szükséges.', 'error'); return; }

            g.name = name; g.type = type; g.dailyHours = dailyHours;
            g.staffPerShift = staffPerShift; g.minRestHours = minRestHours; g.shiftTypes = shiftTypes;
            if (isNew) state.groups.push(g);
            DB.save();
            close();
            render();
            UI.toast('Munkacsoport elmentve.', 'success');
          }
        }
      ]
    });
  }

  function wireEvents() {
    document.getElementById('btnAddGroup').addEventListener('click', () => openGroupForm(null));
  }

  global.App = global.App || {};
  global.App.UIGroups = { render, wireEvents };
})(window);
