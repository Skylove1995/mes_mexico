(function () {
  var modeToggle = document.getElementById('mode-toggle');
  var formLogin = document.getElementById('form-login');
  var formRegister = document.getElementById('form-register');

  function activate(which) {
    var isLogin = which === 'login';
    formLogin.classList.toggle('is-active', isLogin);
    formRegister.classList.toggle('is-active', !isLogin);
    modeToggle.innerHTML = isLogin
      ? "Don't have an account? <strong>Register</strong>"
      : 'Already have an account? <strong>Sign In</strong>';

    if (!isLogin) {
      loadDepartments();
    }
  }

  if (modeToggle && formLogin && formRegister) {
    modeToggle.addEventListener('click', function () {
      var isCurrentlyLogin = formLogin.classList.contains('is-active');
      activate(isCurrentlyLogin ? 'register' : 'login');
    });
  }

  document.querySelectorAll('.eye').forEach(function (btn) {
    btn.addEventListener('click', function () {
      var input = document.getElementById(btn.getAttribute('data-target'));
      var showing = input.type === 'text';
      input.type = showing ? 'password' : 'text';
      btn.setAttribute('aria-pressed', String(!showing));
      btn.setAttribute('aria-label', showing ? 'Show password' : 'Hide password');
    });
  });

  var deptLoaded = false;
  function loadDepartments() {
    if (deptLoaded) return;
    var select = document.getElementById('reg-dept');
    if (!select) return;

    fetch('/api/dept/selectable', { headers: { Accept: 'application/json' } })
      .then(function (res) {
        if (!res.ok) throw new Error('request failed');
        return res.json();
      })
      .then(function (depts) {
        var currentValue = select.getAttribute('data-selected') || '';
        depts.forEach(function (d) {
          var option = document.createElement('option');
          option.value = d.id;
          option.textContent = d.dept;
          if (String(d.id) === currentValue) {
            option.selected = true;
          }
          select.appendChild(option);
        });
        deptLoaded = true;
      })
      .catch(function () {
        var option = document.createElement('option');
        option.value = '';
        option.textContent = 'Unable to load departments';
        select.appendChild(option);
      });
  }

  // Neu form dang mo san o tab register (vi du sau khi submit loi), nap dept ngay.
  if (formRegister && formRegister.classList.contains('is-active')) {
    loadDepartments();
  }
})();
