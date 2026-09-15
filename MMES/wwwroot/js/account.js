(function () {
  var tabLogin = document.getElementById('tab-login');
  var tabRegister = document.getElementById('tab-register');
  var formLogin = document.getElementById('form-login');
  var formRegister = document.getElementById('form-register');
  var title = document.getElementById('heading-title');
  var sub = document.getElementById('heading-sub');

  function activate(which) {
    var isLogin = which === 'login';
    tabLogin.setAttribute('aria-selected', String(isLogin));
    tabRegister.setAttribute('aria-selected', String(!isLogin));
    formLogin.hidden = !isLogin;
    formRegister.hidden = isLogin;
    title.textContent = isLogin ? 'Sign in to your account' : 'Create a new account';
    sub.textContent = isLogin
      ? 'Enter your employee ID and password to access MMES.'
      : 'Register for MMES using your employee ID and department.';

    if (!isLogin) {
      loadDepartments();
    }
  }

  if (tabLogin && tabRegister) {
    tabLogin.addEventListener('click', function () { activate('login'); });
    tabRegister.addEventListener('click', function () { activate('register'); });
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
  if (formRegister && !formRegister.hidden) {
    loadDepartments();
  }
})();
