function getLocalHour() {
  return new Date().getHours();
}

function computeTheme(hour) {
  return (hour >= 6 && hour < 18) ? 'light' : 'dark';
}

function applyTheme() {
  document.documentElement.setAttribute('data-theme', computeTheme(getLocalHour()));
}

if (typeof document !== 'undefined') {
  applyTheme();
  setInterval(applyTheme, 60000);
}

if (typeof module !== 'undefined' && module.exports) {
  module.exports = { computeTheme, getLocalHour };
}
