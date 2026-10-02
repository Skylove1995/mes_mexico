(function () {
  var REFRESH_MS = 60000;

  var dateInput = document.getElementById('pr-date');
  var monthInput = document.getElementById('pr-month');
  var updatedEl = document.getElementById('pr-updated');
  var dayTable = document.getElementById('pr-table-day');
  var nightTable = document.getElementById('pr-table-night');
  var tabs = document.querySelectorAll('.pr-tabs button');

  // KPI Table Elements
  var kpiSummaryTbody = document.getElementById('pr-kpi-summary-tbody');

  // Monthly Chart Elements
  var chartMonthlySvg = document.getElementById('pr-chart-monthly');
  var chartMonthlyAxis = document.getElementById('pr-chart-monthly-axis');
  var chartMonthlyTitle = document.getElementById('pr-monthly-chart-title');

  var currentType = 'SPI';
  var lastData = null;
  var timer = null;

  function todayIso() {
    var d = new Date();
    var tz = d.getTimezoneOffset() * 60000;
    return new Date(d - tz).toISOString().slice(0, 10);
  }

  function currentMonthIso() {
    var d = new Date();
    var yyyy = d.getFullYear();
    var mm = String(d.getMonth() + 1).padStart(2, '0');
    return yyyy + '-' + mm;
  }

  if (dateInput) {
    dateInput.value = todayIso();
  }

  if (monthInput) {
    monthInput.value = currentMonthIso();
  }

  function formatNumber(num) {
    return (num || 0).toLocaleString('en-US');
  }

  function renderCell(cell, isTotalColumn) {
    var td = document.createElement('td');
    if (!cell || cell.total === 0) {
      var emptyClass = 'pr-cell is-empty' + (isTotalColumn ? ' pr-cell-total' : '');
      td.innerHTML = '<div class="' + emptyClass + '"><span class="pr-dash">&ndash;</span></div>';
      return td;
    }


    var isExceeded = cell.pct > 5.0;
    var isOk = !isExceeded && cell.total > 0;

    var cellClass = 'pr-cell' +
      (isExceeded ? ' is-exceeded' : '') +
      (isOk ? ' is-ok' : '') +
      (isTotalColumn ? ' pr-cell-total' + (isExceeded ? ' is-total-exceeded' : ' is-total-ok') : '');

    var pctClass = 'pr-pct' + (isExceeded ? ' is-danger' : (isOk ? ' is-success' : ''));
    var alertBadge = isExceeded ? ' <span class="pr-alert-icon">⚠️</span>' : (isOk ? ' <span class="pr-ok-icon">✓</span>' : '');

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

    // 🏆 Add Line Total Column Header
    var totalTh = document.createElement('th');
    totalTh.className = 'pr-th-total';
    totalTh.textContent = 'LINE TOTAL';
    headRow.appendChild(totalTh);

    thead.appendChild(headRow);
    table.appendChild(thead);

    var tbody = document.createElement('tbody');
    shiftData.lines.forEach(function (lineRow) {
      var tr = document.createElement('tr');
      var th = document.createElement('th');
      th.textContent = lineRow.line;
      tr.appendChild(th);

      lineRow.hours.forEach(function (cell) {
        tr.appendChild(renderCell(cell, false));
      });

      // 🏆 Render Line Total Cell at the end of matrix
      tr.appendChild(renderCell(lineRow.totalCell, true));

      tbody.appendChild(tr);
    });
    table.appendChild(tbody);
  }

  function renderKpis(typeData) {
    if (!kpiSummaryTbody) return;
    kpiSummaryTbody.innerHTML = '';

    if (!typeData || !typeData.lineSummaries || typeData.lineSummaries.length === 0) {
      kpiSummaryTbody.innerHTML = '<tr><td colspan="4" class="pr-empty-kpi">No Line Performance Data Available</td></tr>';
      return;
    }

    typeData.lineSummaries.forEach(function (ls) {
      var tr = document.createElement('tr');

      // Line Name
      var tdLine = document.createElement('td');
      tdLine.className = 'pr-kpi-line-name';
      tdLine.innerHTML = '<strong>' + ls.line + '</strong>';
      tr.appendChild(tdLine);

      // Shift 1 (Day)
      var tdDay = document.createElement('td');
      var dayExc = ls.daySummary.pct > 5.0;
      var dayOk = !dayExc && ls.daySummary.total > 0;
      tdDay.innerHTML =
        '<div class="pr-kpi-subcell">' +
        '<span class="pr-kpi-subval">UP ' + formatNumber(ls.daySummary.userPass) + ' / ' + formatNumber(ls.daySummary.total) + '</span>' +
        '<span class="pr-kpi-subpct ' + (dayExc ? 'is-danger' : (dayOk ? 'is-success' : 'is-muted')) + '">' +
        ls.daySummary.pct.toFixed(1) + '%' + (dayExc ? ' ⚠️' : (dayOk ? ' ✓' : '')) +
        '</span>' +
        '</div>';
      tr.appendChild(tdDay);

      // Shift 2 (Night)
      var tdNight = document.createElement('td');
      var nightExc = ls.nightSummary.pct > 5.0;
      var nightOk = !nightExc && ls.nightSummary.total > 0;
      tdNight.innerHTML =
        '<div class="pr-kpi-subcell">' +
        '<span class="pr-kpi-subval">UP ' + formatNumber(ls.nightSummary.userPass) + ' / ' + formatNumber(ls.nightSummary.total) + '</span>' +
        '<span class="pr-kpi-subpct ' + (nightExc ? 'is-danger' : (nightOk ? 'is-success' : 'is-muted')) + '">' +
        ls.nightSummary.pct.toFixed(1) + '%' + (nightExc ? ' ⚠️' : (nightOk ? ' ✓' : '')) +
        '</span>' +
        '</div>';
      tr.appendChild(tdNight);

      // Overall Efficiency
      var tdOverall = document.createElement('td');
      var overExc = ls.overallSummary.pct > 5.0;
      var overOk = !overExc && ls.overallSummary.total > 0;
      tdOverall.innerHTML =
        '<div class="pr-kpi-subcell is-accent">' +
        '<span class="pr-kpi-subval">UP ' + formatNumber(ls.overallSummary.userPass) + ' / ' + formatNumber(ls.overallSummary.total) + '</span>' +
        '<span class="pr-kpi-subpct ' + (overExc ? 'is-danger' : (overOk ? 'is-success' : 'is-muted')) + '">' +
        ls.overallSummary.pct.toFixed(1) + '%' + (overExc ? ' ⚠️' : (overOk ? ' ✓' : '')) +
        '</span>' +
        '</div>';
      tr.appendChild(tdOverall);

      kpiSummaryTbody.appendChild(tr);
    });
  }

  function renderMonthlyTrendChart(data) {
    if (!chartMonthlySvg || !chartMonthlyAxis) return;
    chartMonthlyAxis.innerHTML = '';

    if (!data || !data.points || data.points.length === 0) {
      chartMonthlySvg.innerHTML = '<text x="600" y="180" text-anchor="middle" fill="var(--text-muted)">No monthly data available</text>';
      return;
    }

    if (chartMonthlyTitle) {
      chartMonthlyTitle.textContent = 'Monthly Overall Performance Trend - ' + (data.machineType || currentType) + ' (' + data.month + ')';
    }

    var points = data.points;
    var left = 60, right = 40, top = 65, bottom = 45;
    var width = 1200, height = 420;
    var plotW = width - left - right;
    var plotH = height - top - bottom;

    var maxPct = points.reduce(function (m, p) { return Math.max(m, p.pct); }, 0);
    // Y-axis steps: 0, 5, 10, 15, 20...
    var yMax = Math.max(20, Math.ceil(maxPct / 5) * 5);

    function xAt(i) {
      return points.length <= 1 ? left + plotW / 2 : left + (i / (points.length - 1)) * plotW;
    }

    function yAt(pct) {
      return top + plotH - (Math.min(pct, yMax) / yMax) * plotH;
    }

    var linePts = points.map(function (p, i) { return xAt(i) + ',' + yAt(p.pct); });
    var linePath = 'M' + linePts.join(' L');
    var areaPath = linePath + ' L' + xAt(points.length - 1) + ',' + (top + plotH) +
      ' L' + xAt(0) + ',' + (top + plotH) + ' Z';
    var limitY = yAt(5.0);

    // 1. Grid lines (Vertical day grid & Horizontal 0-5-10-15-20 grid)
    var gridHtml = '';
    
    // Vertical grid lines for each day column
    points.forEach(function (p, i) {
      var cx = xAt(i);
      gridHtml += '<line class="pr-chart-vgrid" x1="' + cx + '" y1="' + top + '" x2="' + cx + '" y2="' + (top + plotH) + '"></line>';
    });

    // Horizontal grid lines for 0%, 5%, 10%, 15%, 20%
    var yStepVals = [];
    for (var v = 0; v <= yMax; v += 5) {
      yStepVals.push(v);
    }
    yStepVals.forEach(function (val) {
      var y = yAt(val);
      gridHtml += '<line class="pr-chart-hgrid" x1="' + left + '" y1="' + y + '" x2="' + (width - right) + '" y2="' + y + '"></line>';
      gridHtml += '<text class="pr-chart-ytick" x="' + (left - 12) + '" y="' + (y + 4) + '">' + val + '%</text>';
    });

    // 2. ONLY 1 Dashed Red Threshold Line at 5.0% Max Limit (No average line!)
    var limitLine =
      '<line class="pr-chart-limit" x1="' + left + '" y1="' + limitY + '" x2="' + (width - right) + '" y2="' + limitY + '"></line>' +
      '<text class="pr-chart-limit-text" x="' + (width - right - 10) + '" y="' + (limitY - 8) + '" text-anchor="end">5.0% Max Limit</text>';

    // 3. Find Global Max Peak & Local Peaks (> 5.0%)
    var maxIdx = -1;
    var highestVal = 0;
    points.forEach(function (p, i) {
      if (p.pct > highestVal) {
        highestVal = p.pct;
        maxIdx = i;
      }
    });

    var localPeakIndices = [];
    points.forEach(function (p, i) {
      if (p.pct > 5.0) {
        var prevPct = i > 0 ? points[i - 1].pct : 0;
        var nextPct = i < points.length - 1 ? points[i + 1].pct : 0;
        if (p.pct >= prevPct && p.pct >= nextPct) {
          localPeakIndices.push(i);
        }
      }
    });

    // Render Data Dots & Speech Bubble Callout Badges
    var dotsHtml = '';
    var calloutsHtml = '';
    var lastCalloutX = -999;

    points.forEach(function (p, i) {
      var cx = xAt(i);
      var cy = yAt(p.pct);
      var isExceeded = p.pct > 5.0;
      var hasData = p.total > 0;
      var isOk = !isExceeded && hasData;
      var isGlobalMax = (i === maxIdx && highestVal > 5.0);
      var isLocalPeak = localPeakIndices.includes(i);

      var titleText = 'Day ' + String(p.day).padStart(2, '0') + ' (' + p.dateStr + ')\n20:00 Prev Day - 20:00 Today\nPass Rate: ' + p.pct.toFixed(1) + '%' +
        (isExceeded ? ' ⚠️ EXCEEDED' : ' ✓ OK') + '\n(' + formatNumber(p.userPass) + ' / ' + formatNumber(p.total) + ' Inspected)';

      var dotClass = 'pr-chart-dot' + (isExceeded ? ' is-exceeded' : '') + (isOk ? ' is-ok' : '') + (!hasData ? ' is-nodata' : '');
      var rRadius = isGlobalMax ? '6' : (isExceeded ? '4.5' : (hasData ? '3.5' : '2'));

      dotsHtml += '<circle class="' + dotClass + '" cx="' + cx + '" cy="' + cy + '" r="' + rRadius + '">' +
        '<title>' + titleText + '</title></circle>';

      // Render Callout speech bubble ONLY for Global Peak or Local Peaks!
      if (isGlobalMax) {
        // Global Peak Callout: "Peak: 13.1%"
        var bw = 105, bh = 28;
        var bx = cx - bw / 2;
        var by = cy - bh - 14;
        if (bx < left + 5) bx = left + 5;
        if (bx + bw > width - right - 5) bx = width - right - 5 - bw;

        calloutsHtml += '<g class="pr-chart-callout-box">' +
          '<rect x="' + bx + '" y="' + by + '" width="' + bw + '" height="' + bh + '" rx="6" class="pr-callout-bg is-peak"></rect>' +
          '<path d="M' + (cx - 4) + ',' + (by + bh) + ' L' + cx + ',' + (cy - 6) + ' L' + (cx + 4) + ',' + (by + bh) + ' Z" class="pr-callout-pointer is-peak"></path>' +
          '<text x="' + (bx + bw / 2) + '" y="' + (by + 18) + '" text-anchor="middle" class="pr-callout-text is-peak">Peak: ' + p.pct.toFixed(1) + '%</text>' +
        '</g>';
        lastCalloutX = cx;
      } else if (isLocalPeak && (cx - lastCalloutX >= 35)) {
        // Local Peak Callout: "21: 7.8%"
        var bw = 85, bh = 26;
        var bx = cx - bw / 2;
        var by = cy - bh - 12;
        if (bx < left + 5) bx = left + 5;
        if (bx + bw > width - right - 5) bx = width - right - 5 - bw;

        calloutsHtml += '<g class="pr-chart-callout-box">' +
          '<rect x="' + bx + '" y="' + by + '" width="' + bw + '" height="' + bh + '" rx="6" class="pr-callout-bg"></rect>' +
          '<path d="M' + (cx - 4) + ',' + (by + bh) + ' L' + cx + ',' + (cy - 5) + ' L' + (cx + 4) + ',' + (by + bh) + ' Z" class="pr-callout-pointer"></path>' +
          '<text x="' + (bx + bw / 2) + '" y="' + (by + 17) + '" text-anchor="middle" class="pr-callout-text">' +
            String(p.day).padStart(2, '0') + ': <tspan class="pr-callout-alert">' + p.pct.toFixed(1) + '%</tspan>' +
          '</text>' +
        '</g>';
        lastCalloutX = cx;
      }
    });


    var gId = 'grad-monthly-' + Math.random().toString(36).substring(2, 7);

    chartMonthlySvg.setAttribute('viewBox', '0 0 ' + width + ' ' + height);
    chartMonthlySvg.innerHTML =
      '<defs><linearGradient id="' + gId + '" x1="0" y1="0" x2="0" y2="1">' +
        '<stop offset="0%" style="stop-color:#94a3b8;stop-opacity:.25"></stop>' +
        '<stop offset="100%" style="stop-color:#94a3b8;stop-opacity:0.02"></stop>' +
      '</linearGradient></defs>' +
      '<g class="pr-chart-grid">' + gridHtml + '</g>' +
      limitLine +
      '<path class="pr-chart-area" d="' + areaPath + '" fill="url(#' + gId + ')"></path>' +
      '<path class="pr-chart-line" d="' + linePath + '"></path>' +
      dotsHtml +
      calloutsHtml;

    // Render X-axis ticks (Day 01, Day 02, ...)
    points.forEach(function (p) {
      var span = document.createElement('span');
      span.textContent = String(p.day).padStart(2, '0');
      chartMonthlyAxis.appendChild(span);
    });
  }

  function fetchMonthlyTrend() {
    var month = (monthInput && monthInput.value) ? monthInput.value : currentMonthIso();
    fetch('/api/smt/production-rate/monthly-trend?month=' + encodeURIComponent(month) + '&type=' + encodeURIComponent(currentType), {
      headers: { Accept: 'application/json' }
    })
      .then(function (res) {
        if (!res.ok) throw new Error('monthly trend request failed');
        return res.json();
      })
      .then(function (data) {
        renderMonthlyTrendChart(data);
      })
      .catch(function (err) {
        console.error(err);
        if (chartMonthlySvg) {
          chartMonthlySvg.innerHTML = '<text x="600" y="140" text-anchor="middle" fill="var(--danger)">Error loading monthly trend</text>';
        }
      });
  }

  function renderActiveTab() {
    if (!lastData) return;
    var typeData = lastData.machineTypes[currentType];
    renderTable(dayTable, typeData ? typeData.day : null);
    renderTable(nightTable, typeData ? typeData.night : null);
    renderKpis(typeData);
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
      fetchMonthlyTrend();
    });
  });

  if (dateInput) {
    dateInput.addEventListener('change', function () {
      fetchData();
    });
  }

  if (monthInput) {
    monthInput.addEventListener('change', function () {
      fetchMonthlyTrend();
    });
  }

  fetchData();
  fetchMonthlyTrend();
  timer = setInterval(function () {
    fetchData();
    fetchMonthlyTrend();
  }, REFRESH_MS);

  window.addEventListener('beforeunload', function () { clearInterval(timer); });
})();
