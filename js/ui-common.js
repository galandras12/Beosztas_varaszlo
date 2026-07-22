/* Közös UI segédeszközök: modális ablak, toast üzenetek, kis DOM helperek. */
(function (global) {
  function el(tag, attrs, children) {
    const node = document.createElement(tag);
    attrs = attrs || {};
    for (const k in attrs) {
      if (k === 'class') node.className = attrs[k];
      else if (k === 'html') node.innerHTML = attrs[k];
      else if (k.startsWith('on') && typeof attrs[k] === 'function') node.addEventListener(k.slice(2), attrs[k]);
      else node.setAttribute(k, attrs[k]);
    }
    (children || []).forEach(c => {
      if (c == null) return;
      node.appendChild(typeof c === 'string' ? document.createTextNode(c) : c);
    });
    return node;
  }

  function toast(msg, type) {
    const root = document.getElementById('toast');
    const item = el('div', { class: 'toast-item' + (type ? ' ' + type : '') }, [msg]);
    root.appendChild(item);
    setTimeout(() => item.remove(), 4200);
  }

  let currentBackdrop = null;

  function closeModal() {
    if (currentBackdrop) { currentBackdrop.remove(); currentBackdrop = null; }
  }

  /**
   * options: { title, bodyHtml or bodyNode, onMount(box), actions: [{label, primary, danger, onClick(closeFn)}] }
   */
  function openModal(options) {
    closeModal();
    const box = el('div', { class: 'modal-box' });
    box.appendChild(el('h2', {}, [options.title || '']));
    if (options.bodyNode) box.appendChild(options.bodyNode);
    else if (options.bodyHtml) box.insertAdjacentHTML('beforeend', options.bodyHtml);

    const actionsRow = el('div', { class: 'modal-actions' });
    (options.actions || [{ label: 'Bezárás' }]).forEach(a => {
      const btn = el('button', {
        class: (a.primary ? 'primary' : '') + (a.danger ? ' danger' : ''),
        onclick: () => a.onClick ? a.onClick(closeModal) : closeModal()
      }, [a.label]);
      actionsRow.appendChild(btn);
    });
    box.appendChild(actionsRow);

    const backdrop = el('div', {
      class: 'modal-backdrop', onclick: (ev) => { if (ev.target === backdrop) closeModal(); }
    }, [box]);
    document.body.appendChild(backdrop);
    currentBackdrop = backdrop;
    options.onMount && options.onMount(box);
    return { close: closeModal, box };
  }

  function confirmDialog(message, onYes) {
    openModal({
      title: 'Megerősítés',
      bodyHtml: '<p>' + message + '</p>',
      actions: [
        { label: 'Mégse' },
        { label: 'Igen', primary: true, onClick: (close) => { close(); onYes(); } }
      ]
    });
  }

  global.App = global.App || {};
  global.App.UI = { el, toast, openModal, closeModal, confirmDialog };
})(window);
