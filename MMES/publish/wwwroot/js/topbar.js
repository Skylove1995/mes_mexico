(function () {
  document.querySelectorAll('.dept-dropdown').forEach(function (dropdown) {
    var trigger = dropdown.querySelector('.dept-dropdown-trigger');
    if (!trigger) return;

    trigger.addEventListener('click', function (e) {
      e.stopPropagation();
      var isOpen = dropdown.classList.contains('is-open');
      document.querySelectorAll('.dept-dropdown.is-open').forEach(function (d) {
        d.classList.remove('is-open');
        d.querySelector('.dept-dropdown-trigger')?.setAttribute('aria-expanded', 'false');
      });
      if (!isOpen) {
        dropdown.classList.add('is-open');
        trigger.setAttribute('aria-expanded', 'true');
      }
    });
  });

  document.addEventListener('click', function () {
    document.querySelectorAll('.dept-dropdown.is-open').forEach(function (d) {
      d.classList.remove('is-open');
      d.querySelector('.dept-dropdown-trigger')?.setAttribute('aria-expanded', 'false');
    });
  });

  document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') {
      document.querySelectorAll('.dept-dropdown.is-open').forEach(function (d) {
        d.classList.remove('is-open');
        d.querySelector('.dept-dropdown-trigger')?.setAttribute('aria-expanded', 'false');
      });
    }
  });
})();
