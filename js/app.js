/* Alkalmazás belépési pont: fülek kezelése, adatbázis műveletek, indítás. */
(function (global) {
  function setActiveTab(tabName) {
    document.querySelectorAll('.tab-btn').forEach(b => b.classList.toggle('active', b.dataset.tab === tabName));
    document.querySelectorAll('.tab-panel').forEach(p => p.classList.toggle('active', p.id === 'tab-' + tabName));
    if (tabName === 'schedule') global.App.UISchedule.render();
    if (tabName === 'print') global.App.UIPrint.render();
    if (tabName === 'employees') global.App.UIEmployees.render();
    if (tabName === 'groups') global.App.UIGroups.render();
    if (tabName === 'settings') global.App.UISettings.render();
  }

  function wireTabs() {
    document.querySelectorAll('.tab-btn').forEach(btn => {
      btn.addEventListener('click', () => setActiveTab(btn.dataset.tab));
    });
  }

  function wireDbActions() {
    const DB = global.App.DB;
    const UI = global.App.UI;

    document.getElementById('btnExportDb').addEventListener('click', () => {
      DB.exportToFile();
      UI.toast('Adatbázis fájlba mentve.', 'success');
    });

    const fileInput = document.getElementById('fileImportDb');
    document.getElementById('btnImportDb').addEventListener('click', () => fileInput.click());
    fileInput.addEventListener('change', () => {
      const file = fileInput.files[0];
      if (!file) return;
      UI.confirmDialog('A betöltés felülírja a jelenlegi adatokat. Folytatod?', () => {
        DB.importFromFile(file, (err) => {
          fileInput.value = '';
          if (err) { UI.toast('Hiba a betöltéskor: ' + err.message, 'error'); return; }
          UI.toast('Adatbázis betöltve.', 'success');
          global.App.UISettings.render();
          global.App.UIGroups.render();
          global.App.UIEmployees.render();
          global.App.UISchedule.render();
        });
      });
    });

    document.getElementById('btnResetDb').addEventListener('click', () => {
      UI.confirmDialog('Ez törli az ÖSSZES adatot (munkacsoportok, dolgozók, beosztások) és visszaáll az alapértelmezett állapotra. Biztosan folytatod?', () => {
        DB.resetAll();
        UI.toast('Alaphelyzet visszaállítva.', 'success');
        global.App.UISettings.render();
        global.App.UIGroups.render();
        global.App.UIEmployees.render();
        global.App.UISchedule.render();
      });
    });
  }

  function init() {
    global.App.DB.load();
    wireTabs();
    wireDbActions();
    global.App.UISettings.wireEvents();
    global.App.UIGroups.wireEvents();
    global.App.UIEmployees.wireEvents();
    global.App.UISchedule.wireEvents();
    global.App.UIPrint.wireEvents();

    global.App.UISettings.render();
    global.App.UIGroups.render();
    global.App.UIEmployees.render();
    setActiveTab('settings');
  }

  document.addEventListener('DOMContentLoaded', init);
})(window);
