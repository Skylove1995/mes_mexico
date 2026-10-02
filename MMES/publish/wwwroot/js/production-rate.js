(function () {
  var REFRESH_MS = 60000;

  var dateInput = document.getElementById('pr-date');
  var updatedEl = document.getElementById('pr-updated');
  var dayTable = document.getElementById('pr-table-day');
  var nightTable = document.getElementById('pr-table-night');
  var tabs = document.querySelectorAll('.pr-tabs button');

  // KPI Elements
  var kpiDayValue = document.getElementById('pr-kpi-day-value');
  var kpiDayRate = document.getElementById('pr-kpi-day-rate');
  var kpiDaySub = document.getElementById('pr-kpi-day-sub');

  var kpiNightValue = document.getElementById('pr-kpi-night-value');
  var kpiNightRate = document.getElementById('pr-kpi-night-rate');
  var kpiNightSub = document.getElementById('pr-kpi-night-sub');

  var kpiOverallValue = document.getElementById('pr-kpi-overall-value');
  var kpiOverallRate = document.getElementById('pr-kpi-overall-rate');
  var kpiOverallSub = document.getElementById('pr-kpi-overall-sub');

  // Chart Elements
  var chartDaySvg = document.getElementById('pr-chart-day');
  var chartDayAxis = document.getElementById('pr-chart-day-axis');

  var chartNightSvg = document.getElementById('pr-chart-night');
  var chartNightAxis = document.getElementById('pr-chart-night-axis');

  var chartOverallSvg = document.getElementById('pr-chart');
  var chartOverallAxis = document.getElementById('pr-chart-axis');

  var currentType = 'SPI';
  var lastData = null;
  var timer = null;

  function todayIso() {
    var d = new Date();
    var tz = d.getTimezoneOffset() * 60000;
    return new Date(d - tz).toISOString().slice(0, 10);
  }

  if (dateInput) {
    dateInput.value = todayIso();
  }

  function formatNumber(num) {
    return (num || 0).toLocaleString('en-US');
  }

  function renderCell(cell) {
    var td = document.createElement('td');
    if (!cell) {
      td.innerHTML = '<div class="pr-cell is-empty"></div>';
      return td;
    }
    
    var isExceeded = cell.pct > 5.0;
    var cellClass = 'pr-cell' + (isExceeded ? ' is-exceeded' : '');
    var pctClass = 'pr-pct' + (isExceeded ? ' is-danger' : '');
    var alertBadge = isExceeded ? ' <span class="pr-alert-icon">⚠️</span>' : '';

    td.innerHTML =
      '<div class="' + cellClass + '">' +
        '<span class="pr-total">' + formatNumber(cell.total) + '</span>' +
        '<span class="pr-pass">UP ' + formatNumber(cell.userPass) + '</span>' +
        '<span class="' + pctClass + '">' + cell.pct.toFixed(1) + '%' + alertBadge + '</span>' +
      '</div>';
    return td;
  }


  function renderTable(table, shiftData) {
    if (!table) return;
    table.innerHTML = '';
    if (!shiftData || !shiftData.lines) return;

    var thead = document.createElement('thead');
    var headRow = document.createElement('tr');
    headRow.appendChild(document.createElement('th')).textContent = 'Line';
    shiftData.hourLabels.forEach(function (label) {
      var th = document.createElement('th');
      th.textContent = label;
      headRow.appendChild(th);
    });
    thead.appendChild(headRow);
    table.appendChild(thead);

    var tbody = document.createElement('tbody');
    shiftData.lines.forEach(function (lineRow) {
      var tr = document.createElement('tr');
      var th = document.createElement('th');
      th.textContent = lineRow.line;
      tr.appendChild(th);
      lineRow.hours.forEach(function (cell) {
        tr.appendChild(renderCell(cell));
      });
      tbody.appendChild(tr);
    });
    table.appendChild(tbody);
  }

  function renderSingleKpiCard(valEl, rateEl, subEl, summary) {
    if (!valEl || !rateEl || !subEl) return;
    if (!summary || summary.total === 0) {
      valEl.textContent = '–';
      rateEl.textContent = '0.0%';
      subEl.textContent = 'No Inspection Data';
      return;
    }

    valEl.textContent = formatNumber(summary.userPass) + ' UP';
    rateEl.textContent = summary.pct.toFixed(1) + '%';
    subEl.textContent = formatNumber(summary.userPass) + ' / ' + formatNumber(summary.total) + ' Total Inspected';
  }

  function renderKpis(typeData) {
    renderSingleKpiCard(kpiDayValue, kpiDayRate, kpiDaySub, typeData ? typeData.daySummary : null);
    renderSingleKpiCard(kpiNightValue, kpiNightRate, kpiNightSub, typeData ? typeData.nightSummary : null);
    renderSingleKpiCard(kpiOverallValue, kpiOverallRate, kpiOverallSub, typeData ? typeData.overallSummary : null);
  }

  function extractShiftHourlyPoints(shiftTable) {
    if (!shiftTable || !shiftTable.hourLabels || !shiftTable.lines) return [];
    var points = [];

    shiftTable.hourLabels.forEach(function (label, hIndex) {
      var total = 0;
      var userPass = 0;
      shiftTable.lines.forEach(function (line) {
        var cell = line.hours[hIndex];
        if (cell) {
          total += cell.total || 0;
          userPass += cell.userPass || 0;
        }
      });
      var pct = total > 0 ? (userPass * 100.0) / total : 0;
      points.push({
        hourLabel: label,
        total: total,
        userPass: userPass,
        pct: pct
      });
    });

    return points;
  }

  function renderChartSvg(chartSvg, chartAxis, trend, viewBoxW, gradientId) {
    if (!chartAxis || !chartSvg) return;
    chartAxis.innerHTML = '';
    if (!trend || trend.length === 0) {
      chartSvg.innerHTML = '';
      return;
    }

    var left = 30, right = 10, top = 14, bottom = 14;
    var width = viewBoxW || 450, height = 160;
    var plotW = width - left - right;
    var plotH = height - top - bottom;

    var maxPct = trend.reduce(function (m, p) { return Math.max(m, p.pct); }, 0);
    var yMax = Math.max(20, Math.ceil((maxPct * 1.25) / 10) * 10);

    var withData = trend.filter(function (p) { return p.total > 0; });
    var avgPct = withData.length
      ? withData.reduce(function (s, p) { return s + p.pct; }, 0) / withData.length
      : 0;

    function xAt(i) {
      return trend.length === 1 ? left + plotW / 2 : left + (i / (trend.length - 1)) * plotW;
    }
    function yAt(pct) {
      return top + plotH - (Math.min(pct, yMax) / yMax) * plotH;
    }

    var linePts = trend.map(function (p, i) { return xAt(i) + ',' + yAt(p.pct); });
    var linePath = 'M' + linePts.join(' L');
    var areaPath = linePath + ' L' + xAt(trend.length - 1) + ',' + (top + plotH) +
      ' L' + xAt(0) + ',' + (top + plotH) + ' Z';
    var avgY = yAt(avgPct);

    var limitY = yAt(5.0);

    var dots = trend.map(function (p, i) {
      var isExceeded = p.pct > 5.0;
      var titleText = p.hourLabel + ': ' + p.pct.toFixed(1) + '%' + (isExceeded ? ' ⚠️ HIGH DEFECT' : '') + ' (' + formatNumber(p.userPass) + '/' + formatNumber(p.total) + ' UP)';
      var dotClass = 'pr-chart-dot' + (isExceeded ? ' is-exceeded' : '');
      var rRadius = isExceeded ? '4.5' : '3';
      return '<circle class="' + dotClass + '" cx="' + xAt(i) + '" cy="' + yAt(p.pct) + '" r="' + rRadius + '">' +
        '<title>' + titleText + '</title></circle>';
    }).join('');

    var grid = [0, 0.5, 1].map(function (f) {
      var y = top + plotH * f;
      return '<line x1="' + left + '" y1="' + y + '" x2="' + (width - right) + '" y2="' + y + '"></line>' +
        '<text class="pr-chart-ytick" x="2" y="' + (y + 3) + '">' + Math.round(yMax * (1 - f)) + '%</text>';
    }).join('');

    var limitLine = yMax >= 5.0 ?
      '<line class="pr-chart-limit" x1="' + left + '" y1="' + limitY + '" x2="' + (width - right) + '" y2="' + limitY + '"></line>' +
      '<text class="pr-chart-limit-text" x="' + (width - right - 50) + '" y="' + (limitY - 4) + '">5.0% Max</text>' : '';

    var gId = gradientId || 'chart-grad-' + Math.random().toString(36).substring(2, 7);

    chartSvg.innerHTML =
      '<defs><linearGradient id="' + gId + '" x1="0" y1="0" x2="0" y2="1">' +
        '<stop offset="0%" style="stop-color:var(--brass-strong);stop-opacity:.38"></stop>' +
        '<stop offset="100%" style="stop-color:var(--brass-strong);stop-opacity:0.02"></stop>' +
      '</linearGradient></defs>' +
      '<g class="pr-chart-grid">' + grid + '</g>' +
      '<line class="pr-chart-avg" x1="' + left + '" y1="' + avgY + '" x2="' + (width - right) + '" y2="' + avgY + '"></line>' +
      limitLine +
      '<path class="pr-chart-area" d="' + areaPath + '" fill="url(#' + gId + ')"></path>' +
      '<path class="pr-chart-line" d="' + linePath + '"></path>' +
      dots;


    var step = trend.length > 15 ? 2 : 1;
    trend.forEach(function (p, i) {
      if (i % step !== 0) return;
      var span = document.createElement('span');
      span.textContent = p.hourLabel.split('-')[0] + 'h';
      chartAxis.appendChild(span);
    });
  }

  function renderAllCharts(typeData) {
    if (!typeData) return;

    // 1. Day Shift Chart
    var dayTrend = extractShiftHourlyPoints(typeData.day);
    renderChartSvg(chartDaySvg, chartDayAxis, dayTrend, 450, 'grad-day-shift');

    // 2. Night Shift Chart
    var nightTrend = extractShiftHourlyPoints(typeData.night);
    renderChartSvg(chartNightSvg, chartNightAxis, nightTrend, 450, 'grad-night-shift');

    // 3. 24h Overall Flow Chart
    renderChartSvg(chartOverallSvg, chartOverallAxis, typeData.trend, 900, 'grad-overall-shift');
  }

  function renderActiveTab() {
    if (!lastData) return;
    var typeData = lastData.machineTypes[currentType];
    renderTable(dayTable, typeData ? typeData.day : null);
    renderTable(nightTable, typeData ? typeData.night : null);
    renderKpis(typeData);
    renderAllCharts(typeData);
  }

  function fetchData() {
    var date = (dateInput && dateInput.value) ? dateInput.value : todayIso();
    fetch('/api/smt/production-rate?date=' + encodeURIComponent(date), { headers: { Accept: 'application/json' } })
      .then(function (res) {
        if (!res.ok) throw new Error('request failed');
        return res.json();
      })
      .then(function (data) {
        lastData = data;
        if (data && data.date && dateInput) {
          dateInput.value = data.date;
        }
        renderActiveTab();
        if (updatedEl) {
          updatedEl.textContent = 'Updated ' + new Date().toLocaleTimeString();
        }
      })
      .catch(function () {
        if (dayTable) {
          dayTable.innerHTML = '<tbody><tr><td class="pr-error">Could not load production data.</td></tr></tbody>';
        }
        if (nightTable) {
          nightTable.innerHTML = '';
        }
      });
  }


  tabs.forEach(function (btn) {
    btn.addEventListener('click', function () {
      tabs.forEach(function (b) { b.setAttribute('aria-selected', 'false'); });
      btn.setAttribute('aria-selected', 'true');
      currentType = btn.getAttribute('data-type');
      renderActiveTab();
    });
  });

  if (dateInput) {
    dateInput.addEventListener('change', function () {
      fetchData();
    });
  }

  fetchData();
  timer = setInterval(fetchData, REFRESH_MS);
  window.addEventListener('beforeunload', function () { clearInterval(timer); });
})();

