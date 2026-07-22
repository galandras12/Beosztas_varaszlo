/* "Nyomtatás / Export" fül: A4 fekvő nyomtatható táblázat, PDF és JPG export. */
(function (global) {
  function buildPrintTable() {
    const UI = global.App.UI;
    const DB = global.App.DB;
    const Calc = global.App.Calc;
    const state = DB.getState();
    const { year, month } = global.App.UISchedule.getSelection();
    const dim = Calc.daysInMonth(year, month);
    const groups = global.App.UISchedule.sortedGroupsWithEmployees(state);

    const wrap = document.getElementById('printArea');
    wrap.innerHTML = '';
    wrap.appendChild(UI.el('h2', {}, [DB.MONTH_NAMES[month - 1] + ' ' + year + ' – ' + month + '. hónap – havi munkabeosztás']));

    const table = document.createElement('table');
    table.className = 'print-table';
    const thead = document.createElement('tr');
    thead.appendChild(UI.el('th', { class: 'name-col' }, ['Dolgozó (munkakör szerint, ABC sorrendben)']));
    for (let d = 1; d <= dim; d++) {
      thead.appendChild(UI.el('th', {}, [String(d) + '\n' + global.App.UISchedule.dowLetter(year, month, d)]));
    }
    thead.appendChild(UI.el('th', {}, ['Köv. hóra átvitt óra']));
    table.appendChild(thead);

    groups.forEach(({ group, employees }) => {
      if (employees.length === 0) return;
      const gr = document.createElement('tr');
      gr.className = 'group-row';
      const gtd = document.createElement('td');
      gtd.colSpan = 2 + dim;
      gtd.textContent = group.name;
      gr.appendChild(gtd);
      table.appendChild(gr);

      employees.forEach(emp => {
        const tr = document.createElement('tr');
        tr.appendChild(UI.el('td', { class: 'name-col' }, [emp.name]));
        for (let d = 1; d <= dim; d++) {
          let text = '';
          if (group.type === 'iroda') {
            if (!Calc.isOfficeWorkday(state, year, month, d)) text = '·';
            else {
              const code = Calc.getCell(state, year, month, emp.id, d);
              text = code || '';
            }
          } else {
            text = Calc.getCell(state, year, month, emp.id, d) || '';
          }
          tr.appendChild(UI.el('td', {}, [text]));
        }
        const summary = Calc.summarizeEmployeeMonth(state, group, emp, year, month);
        tr.appendChild(UI.el('td', { class: 'balance-col' }, [summary.balance.toFixed(1)]));
        table.appendChild(tr);
      });
    });

    wrap.appendChild(table);
  }

  function downloadDataUrl(dataUrl, filename) {
    const a = document.createElement('a');
    a.href = dataUrl; a.download = filename;
    document.body.appendChild(a); a.click(); a.remove();
  }

  function exportPdf() {
    const UI = global.App.UI;
    const DB = global.App.DB;
    const { year, month } = global.App.UISchedule.getSelection();
    const target = document.getElementById('printArea');
    UI.toast('PDF készítése folyamatban…');
    html2canvas(target, { scale: 2, backgroundColor: '#ffffff' }).then(canvas => {
      const { jsPDF } = window.jspdf;
      const pdf = new jsPDF({ orientation: 'landscape', unit: 'mm', format: 'a4' });
      const pageW = pdf.internal.pageSize.getWidth();
      const pageH = pdf.internal.pageSize.getHeight();
      const imgRatio = canvas.height / canvas.width;
      let w = pageW - 10, h = w * imgRatio;
      if (h > pageH - 10) { h = pageH - 10; w = h / imgRatio; }
      const x = (pageW - w) / 2, y = (pageH - h) / 2;
      pdf.addImage(canvas.toDataURL('image/jpeg', 0.95), 'JPEG', x, y, w, h);
      pdf.save('beosztas-' + DB.MONTH_NAMES[month - 1] + '-' + year + '.pdf');
      UI.toast('PDF elmentve.', 'success');
    }).catch(err => {
      console.error(err);
      UI.toast('Hiba a PDF létrehozásakor: ' + err.message, 'error');
    });
  }

  function exportJpg() {
    const UI = global.App.UI;
    const DB = global.App.DB;
    const { year, month } = global.App.UISchedule.getSelection();
    const target = document.getElementById('printArea');
    UI.toast('JPG készítése folyamatban…');
    html2canvas(target, { scale: 2, backgroundColor: '#ffffff' }).then(canvas => {
      downloadDataUrl(canvas.toDataURL('image/jpeg', 0.95), 'beosztas-' + DB.MONTH_NAMES[month - 1] + '-' + year + '.jpg');
      UI.toast('JPG elmentve.', 'success');
    }).catch(err => {
      console.error(err);
      UI.toast('Hiba a JPG létrehozásakor: ' + err.message, 'error');
    });
  }

  function render() {
    buildPrintTable();
  }

  function wireEvents() {
    document.getElementById('btnPrint').addEventListener('click', () => { buildPrintTable(); window.print(); });
    document.getElementById('btnExportPdf').addEventListener('click', () => { buildPrintTable(); exportPdf(); });
    document.getElementById('btnExportJpg').addEventListener('click', () => { buildPrintTable(); exportJpg(); });
  }

  global.App = global.App || {};
  global.App.UIPrint = { render, wireEvents };
})(window);
