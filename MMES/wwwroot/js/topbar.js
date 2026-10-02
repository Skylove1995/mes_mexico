(function () {
  function closeAll() {
    document.querySelectorAll('.dept-dropdown.is-open').forEach(function (d) {
      d.classList.remove('is-open');
      d.querySelector('.dept-dropdown-trigger')?.setAttribute('aria-expanded', 'false');
    });
  }

  function positionMenu(dropdown, trigger) {
    var menu = dropdown.querySelector('.dept-dropdown-menu');
    if (!menu) return;
    var triggerRect = trigger.getBoundingClientRect();
    var menuRect = menu.getBoundingClientRect();
    var margin = 8;
    var left = Math.min(
      triggerRect.left,
      window.innerWidth - menuRect.width - margin
    );
    left = Math.max(margin, left);
    menu.style.top = (triggerRect.bottom + 2) + 'px';
    menu.style.left = left + 'px';
  }

  document.querySelectorAll('.dept-dropdown').forEach(function (dropdown) {
    var trigger = dropdown.querySelector('.dept-dropdown-trigger');
    if (!trigger) return;

    trigger.addEventListener('click', function (e) {
      e.stopPropagation();
      var isOpen = dropdown.classList.contains('is-open');
      closeAll();
      if (!isOpen) {
        dropdown.classList.add('is-open');
        trigger.setAttribute('aria-expanded', 'true');
        positionMenu(dropdown, trigger);
      }
    });
  });

  document.addEventListener('click', closeAll);

  document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') closeAll();
  });

  // The trigger moves under horizontal scroll (mobile nav) or viewport
  // resize while a menu is open — close it rather than let it drift away
  // from its trigger.
  var headerNav = document.querySelector('.header-nav');
  headerNav?.addEventListener('scroll', closeAll);
  window.addEventListener('resize', closeAll);
})();
