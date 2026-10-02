/* ============================================================
   Document Management System (DMS) Dashboard - JavaScript Engine
   Handles AJAX Data Fetching, Filtering, Uploads, and Modals
   ============================================================ */

let dmsState = {
    selectedTypeId: null,
    selectedDeptId: null,
    selectedStatus: null,
    selectedProcesses: [],
    selectedMachines: [],
    selectedModels: [],
    searchKeyword: '',
    page: 1,
    pageSize: 20
};

let dmsMasterData = {
    categories: [],
    departments: [],
    processes: [],
    machines: [],
    models: []
};

document.addEventListener('DOMContentLoaded', function () {
    initDmsDashboard();
});

async function initDmsDashboard() {
    await Promise.all([
        loadCategories(),
        loadDepartments(),
        loadAdvancedFilters(),
        loadDocuments()
    ]);

    setupEventListeners();
}

function setupEventListeners() {
    const searchInput = document.getElementById('dmsSearchInput');
    if (searchInput) {
        let timer = null;
        searchInput.addEventListener('input', function () {
            clearTimeout(timer);
            timer = setTimeout(() => {
                dmsState.searchKeyword = this.value;
                dmsState.page = 1;
                loadDocuments();
            }, 300);
        });
    }

    document.addEventListener('click', function (e) {
        if (!e.target.closest('.dms-action-container') && !e.target.closest('.dms-action-menu')) {
            closeAllActionDropdowns();
        }
    });

    window.addEventListener('scroll', function () {
        closeAllActionDropdowns();
    }, true);
}

function closeAllActionDropdowns() {
    document.querySelectorAll('.dms-action-menu').forEach(menu => {
        menu.classList.add('hidden');
    });
}

function toggleActionDropdown(event, docId) {
    event.stopPropagation();
    const btn = event.currentTarget;
    const targetMenu = document.getElementById(`actionDropdown_${docId}`);
    if (!targetMenu) return;

    const isHidden = targetMenu.classList.contains('hidden');
    closeAllActionDropdowns();

    if (isHidden) {
        targetMenu.classList.remove('hidden');
        const rect = btn.getBoundingClientRect();
        targetMenu.style.position = 'fixed';
        targetMenu.style.top = `${rect.bottom + 4}px`;
        targetMenu.style.left = `${Math.max(10, rect.right - 190)}px`;
        targetMenu.style.zIndex = '99999';
    }
}

async function loadCategories() {
    try {
        const res = await fetch('/api/dms/categories');
        if (!res.ok) return;
        const data = await res.json();
        dmsMasterData.categories = data;

        const container = document.getElementById('dmsCategoryList');
        if (!container) return;

        let totalDocs = data.reduce((sum, item) => sum + item.count, 0);

        let html = `
            <li class="dms-category-item ${dmsState.selectedTypeId === null ? 'active' : ''}" onclick="selectCategory(null)">
                <span class="cat-label">
                    <i class="bi bi-grid-fill"></i>
                    <span>All Categories</span>
                </span>
                <span class="cat-count">(${totalDocs})</span>
            </li>
        `;

        data.forEach(c => {
            const icon = c.iconClass || 'bi-file-earmark-text';
            html += `
                <li class="dms-category-item ${dmsState.selectedTypeId === c.typeId ? 'active' : ''}" onclick="selectCategory(${c.typeId})">
                    <span class="cat-label">
                        <i class="bi ${icon}"></i>
                        <span>${escapeHtml(c.typeName)}</span>
                    </span>
                    <span class="cat-count">(${c.count})</span>
                </li>
            `;
        });

        container.innerHTML = html;
    } catch (err) {
        console.error("Error loading document categories:", err);
    }
}

async function loadDepartments() {
    try {
        const res = await fetch('/api/dms/departments');
        if (!res.ok) return;
        const data = await res.json();
        dmsMasterData.departments = data;

        const container = document.getElementById('dmsDeptPills');
        if (!container) return;

        let html = `
            <div class="dms-dept-pill ${dmsState.selectedDeptId === null ? 'active' : ''}" onclick="selectDepartment(null)">
                All Departments
            </div>
        `;

        data.forEach(d => {
            const isActive = dmsState.selectedDeptId === d.id;
            const color = d.badgeColor || '#0B72B9';

            let pillStyle = '';
            if (isActive) {
                pillStyle = `background-color: ${color} !important; color: #FFFFFF !important; border-color: ${color} !important; box-shadow: 0 2px 8px ${color}60;`;
            } else {
                pillStyle = `background-color: ${color}18 !important; color: ${color} !important; border: 1px solid ${color}45 !important;`;
            }

            html += `
                <div class="dms-dept-pill ${isActive ? 'active' : ''}" style="${pillStyle}" onclick="selectDepartment(${d.id})">
                    ${escapeHtml(d.deptCode)}
                </div>
            `;
        });

        container.innerHTML = html;
    } catch (err) {
        console.error("Error loading departments:", err);
    }
}

async function loadAdvancedFilters() {
    try {
        const [procRes, machRes, modelRes] = await Promise.all([
            fetch('/api/dms/processes'),
            fetch('/api/dms/machines'),
            fetch('/api/dms/models')
        ]);

        if (procRes.ok) {
            dmsMasterData.processes = await procRes.json();
            renderCheckboxFilter('filterProcesses', dmsMasterData.processes, 'processCode', 'processName');
        }

        if (machRes.ok) {
            dmsMasterData.machines = await machRes.json();
            renderCheckboxFilter('filterMachines', dmsMasterData.machines, 'machineCode', 'machineName');
        }

        if (modelRes.ok) {
            dmsMasterData.models = await modelRes.json();
            renderCheckboxFilter('filterModels', dmsMasterData.models, 'modelCode', 'modelName');
        }
    } catch (err) {
        console.error("Error loading advanced filters:", err);
    }
}

function renderCheckboxFilter(containerId, items, codeKey, nameKey) {
    const container = document.getElementById(containerId);
    if (!container) return;

    let html = '';
    items.forEach(item => {
        html += `
            <label class="dms-checkbox-label">
                <input type="checkbox" value="${item.id}" onchange="applyAdvancedFilters()" />
                <span>${escapeHtml(item[nameKey] || item[codeKey])}</span>
            </label>
        `;
    });

    container.innerHTML = html;
}

async function loadDocuments() {
    try {
        const tbody = document.getElementById('dmsTableBody');
        if (tbody) {
            tbody.innerHTML = `<tr><td colspan="8" class="text-center p-4 text-slate-400">Loading document data...</td></tr>`;
        }

        let url = `/api/dms/documents?page=${dmsState.page}&pageSize=${dmsState.pageSize}`;
        if (dmsState.searchKeyword) url += `&search=${encodeURIComponent(dmsState.searchKeyword)}`;
        if (dmsState.selectedTypeId) url += `&typeId=${dmsState.selectedTypeId}`;
        if (dmsState.selectedDeptId) url += `&deptId=${dmsState.selectedDeptId}`;
        if (dmsState.selectedStatus) url += `&status=${encodeURIComponent(dmsState.selectedStatus)}`;
        if (dmsState.selectedProcesses.length > 0) url += `&processIds=${dmsState.selectedProcesses.join(',')}`;
        if (dmsState.selectedMachines.length > 0) url += `&machineIds=${dmsState.selectedMachines.join(',')}`;
        if (dmsState.selectedModels.length > 0) url += `&modelIds=${dmsState.selectedModels.join(',')}`;

        const res = await fetch(url);
        if (!res.ok) throw new Error("Error loading document list");

        const data = await res.json();
        renderDocumentsTable(data.items);
        renderPagination(data);
    } catch (err) {
        console.error(err);
        const tbody = document.getElementById('dmsTableBody');
        if (tbody) {
            tbody.innerHTML = `<tr><td colspan="8" class="text-center p-4 text-rose-500">Failed to load documents. Please try again.</td></tr>`;
        }
    }
}

function renderDocumentsTable(items) {
    const tbody = document.getElementById('dmsTableBody');
    if (!tbody) return;

    if (!items || items.length === 0) {
        tbody.innerHTML = `<tr><td colspan="8" class="text-center p-6 text-slate-400">No matching documents found.</td></tr>`;
        return;
    }

    let html = '';
    items.forEach(doc => {
        const statusClass = doc.status === 'Approved' ? 'status-approved' : (doc.status === 'Pending' ? 'status-pending' : '');
        const deptColor = doc.deptBadgeColor || '#0B72B9';
        const updatedDateStr = doc.updatedAt ? doc.updatedAt.split('T')[0] : '';
        const titleSafe = escapeHtml(doc.title).replace(/'/g, "\\'");
        const numberSafe = escapeHtml(doc.docNumber).replace(/'/g, "\\'");

        html += `
            <tr>
                <td class="dms-doc-number">${escapeHtml(doc.docNumber)}</td>
                <td class="dms-doc-title" title="${escapeHtml(doc.title)}">
                    <button type="button" onclick="openDocPreviewModal(${doc.id}, '${titleSafe}', '${numberSafe}')" class="hover:underline flex items-center gap-1.5 text-left text-blue-600 dark:text-blue-400">
                        <i class="bi bi-file-earmark-pdf text-blue-500"></i>
                        <span>${escapeHtml(doc.title)}</span>
                    </button>
                </td>
                <td><span class="dms-badge-type">${escapeHtml(doc.typeName || 'Document')}</span></td>
                <td><span class="dms-badge-version">${escapeHtml(doc.currentVersion || 'v1.0')}</span></td>
                <td><span class="dms-badge-status ${statusClass}"><i class="bi bi-circle-fill text-[8px]"></i> ${escapeHtml(doc.status)}</span></td>
                <td><span class="dms-badge-dept" style="background-color: ${deptColor};">${escapeHtml(doc.deptCode)}</span></td>
                <td class="text-slate-400 font-mono text-xs">${escapeHtml(updatedDateStr)}</td>
                <td class="text-right">
                    <div class="dms-action-container" id="actionContainer_${doc.id}">
                        <button type="button" onclick="toggleActionDropdown(event, ${doc.id})" class="dms-action-btn">
                            <i class="bi bi-three-dots-vertical text-slate-300"></i>
                            <i class="bi bi-caret-down-fill text-[8px] text-slate-400"></i>
                        </button>
                        <div id="actionDropdown_${doc.id}" class="dms-action-menu hidden">
                            <button type="button" onclick="openDocPreviewModal(${doc.id}, '${titleSafe}', '${numberSafe}'); closeAllActionDropdowns();" class="dms-action-menu-item">
                                <i class="bi bi-file-earmark-pdf text-blue-400"></i>
                                <span>View Preview</span>
                            </button>
                            <button type="button" onclick="openDocDetailsModal(${doc.id}); closeAllActionDropdowns();" class="dms-action-menu-item">
                                <i class="bi bi-eye text-slate-400"></i>
                                <span>View Details</span>
                            </button>
                            <button type="button" onclick="openRevisionHistoryModal(${doc.id}); closeAllActionDropdowns();" class="dms-action-menu-item">
                                <i class="bi bi-clock-history text-slate-400"></i>
                                <span>Revision History</span>
                            </button>
                            ${window.isDmsAdmin ? `
                                <button type="button" onclick="openUpdateVersionModal(${doc.id}, '${numberSafe}', '${escapeHtml(doc.currentVersion)}'); closeAllActionDropdowns();" class="dms-action-menu-item">
                                    <i class="bi bi-pencil text-slate-400"></i>
                                    <span>Edit</span>
                                </button>
                            ` : ''}
                            ${doc.status === 'Draft' || doc.status === 'Pending' ? `
                                <button type="button" onclick="approveDoc(${doc.id}); closeAllActionDropdowns();" class="dms-action-menu-item">
                                    <i class="bi bi-check-circle text-emerald-400"></i>
                                    <span>Approve</span>
                                </button>
                            ` : ''}
                            <a href="/api/dms/documents/${doc.id}/download" target="_blank" onclick="closeAllActionDropdowns();" class="dms-action-menu-item">
                                <i class="bi bi-download text-slate-400"></i>
                                <span>Download PDF</span>
                            </a>
                        </div>
                    </div>
                </td>
            </tr>
        `;
    });

    tbody.innerHTML = html;
}

function renderPagination(data) {
    const container = document.getElementById('dmsPagination');
    if (!container) return;

    if (data.totalPages <= 1) {
        container.innerHTML = '';
        return;
    }

    let html = `<div class="flex items-center justify-between text-xs text-slate-400 mt-3">`;
    html += `<span>Showing ${data.items.length} of ${data.totalItems} documents</span>`;
    html += `<div class="flex items-center gap-1">`;

    for (let i = 1; i <= data.totalPages; i++) {
        const active = i === data.page ? 'bg-blue-600 text-white' : 'bg-slate-800 text-slate-300 hover:bg-slate-700';
        html += `<button onclick="changePage(${i})" class="px-2.5 py-1 rounded font-bold ${active}">${i}</button>`;
    }

    html += `</div></div>`;
    container.innerHTML = html;
}

function changePage(page) {
    dmsState.page = page;
    loadDocuments();
}

function selectCategory(typeId) {
    dmsState.selectedTypeId = typeId;
    dmsState.page = 1;
    loadCategories();
    loadDocuments();
}

function selectDepartment(deptId) {
    dmsState.selectedDeptId = deptId;
    dmsState.page = 1;
    loadDepartments();
    loadDocuments();
}

function applyAdvancedFilters() {
    dmsState.selectedProcesses = getCheckedValues('filterProcesses');
    dmsState.selectedMachines = getCheckedValues('filterMachines');
    dmsState.selectedModels = getCheckedValues('filterModels');
    dmsState.selectedStatus = getCheckedValues('filterStatuses')[0] || null;

    dmsState.page = 1;
    loadDocuments();
}

function getCheckedValues(containerId) {
    const container = document.getElementById(containerId);
    if (!container) return [];
    const checkboxes = container.querySelectorAll('input[type="checkbox"]:checked');
    return Array.from(checkboxes).map(c => c.value);
}

// Modals & Auto-Generation
function openUploadModal() {
    populateUploadModalOptions();
    const modal = document.getElementById('uploadDocModal');
    if (modal) modal.classList.remove('hidden');
}

function populateUploadModalOptions() {
    // Populate Document Type select dropdown
    const typeSelect = document.getElementById('uploadTypeId');
    if (typeSelect) {
        let html = '<option value="">-- Choose Category Type --</option>';
        dmsMasterData.categories.forEach(c => {
            html += `<option value="${c.typeId}">${escapeHtml(c.typeName)}</option>`;
        });
        typeSelect.innerHTML = html;
        if (dmsMasterData.categories.length > 0) {
            typeSelect.selectedIndex = 1;
        }
    }

    // Populate Department Owner select dropdown
    const deptSelect = document.getElementById('uploadDeptId');
    if (deptSelect) {
        let html = '<option value="">-- Choose Department --</option>';
        dmsMasterData.departments.forEach(d => {
            html += `<option value="${d.id}">${escapeHtml(d.deptCode)} - ${escapeHtml(d.deptName)}</option>`;
        });
        deptSelect.innerHTML = html;
        if (dmsMasterData.departments.length > 0) {
            deptSelect.selectedIndex = 1;
        }
    }

    // Populate Applicable Processes
    const procContainer = document.getElementById('uploadProcessList');
    if (procContainer) {
        let html = '';
        dmsMasterData.processes.forEach(p => {
            html += `
                <label class="dms-checkbox-label">
                    <input type="checkbox" name="ProcessIds" value="${p.id}" />
                    <span>${escapeHtml(p.processName || p.processCode)}</span>
                </label>
            `;
        });
        procContainer.innerHTML = html || '<span class="text-xs text-slate-400">No processes available</span>';
    }

    // Populate Applicable Machines
    const machContainer = document.getElementById('uploadMachineList');
    if (machContainer) {
        let html = '';
        dmsMasterData.machines.forEach(m => {
            html += `
                <label class="dms-checkbox-label">
                    <input type="checkbox" name="MachineIds" value="${m.id}" />
                    <span>${escapeHtml(m.machineName || m.machineCode)}</span>
                </label>
            `;
        });
        machContainer.innerHTML = html || '<span class="text-xs text-slate-400">No machines available</span>';
    }

    // Populate Applicable Models
    const modelContainer = document.getElementById('uploadModelList');
    if (modelContainer) {
        let html = '';
        dmsMasterData.models.forEach(m => {
            html += `
                <label class="dms-checkbox-label">
                    <input type="checkbox" name="ModelIds" value="${m.id}" />
                    <span>${escapeHtml(m.modelName || m.modelCode)}</span>
                </label>
            `;
        });
        modelContainer.innerHTML = html || '<span class="text-xs text-slate-400">No models available</span>';
    }

    // Trigger initial Auto Document ID generation
    generateAutoDocNumber();
}

function generateAutoDocNumber() {
    const company = 'HSM';
    const factory = 'IVI';

    const deptId = document.getElementById('uploadDeptId')?.value;
    const typeId = document.getElementById('uploadTypeId')?.value;
    const seqInput = document.getElementById('uploadDocSeq');
    const seqNum = seqInput ? seqInput.value.trim() : '021';

    let deptCode = 'LQC';
    if (deptId && dmsMasterData.departments) {
        const foundDept = dmsMasterData.departments.find(d => d.id == deptId);
        if (foundDept) {
            deptCode = foundDept.deptCode.split(' ')[0].toUpperCase();
        }
    }

    let typeCode = 'WI';
    if (typeId && dmsMasterData.categories) {
        const foundType = dmsMasterData.categories.find(c => c.typeId == typeId);
        if (foundType) {
            typeCode = getTypeCodeAbbreviation(foundType.typeCode || foundType.typeName);
        }
    }

    let formattedSeq = seqNum || '021';
    if (/^\d+$/.test(formattedSeq)) {
        formattedSeq = formattedSeq.padStart(3, '0');
    }

    const autoDocNum = `${company}-${factory}-${deptCode}-${typeCode}-${formattedSeq}`;

    const docNumInput = document.getElementById('uploadDocNumber');
    if (docNumInput) {
        docNumInput.value = autoDocNum;
    }

    const titleInput = document.getElementById('uploadDocTitle');
    if (titleInput && (!titleInput.value || titleInput.value.startsWith('HSM-') || titleInput.value.startsWith('HSEVN-'))) {
        titleInput.value = `${autoDocNum}_MOI.AOI PROGRAM INSTRUCTION`;
    }

    checkDocNumberAvailability(autoDocNum);
}

let checkDocTimer = null;

function checkDocNumberAvailability(docNumber) {
    const statusDiv = document.getElementById('docNumberCheckStatus');
    const submitBtn = document.getElementById('uploadSubmitBtn');
    if (!statusDiv) return;

    if (!docNumber || docNumber.trim().length === 0) {
        statusDiv.classList.add('hidden');
        if (submitBtn) submitBtn.disabled = false;
        return;
    }

    clearTimeout(checkDocTimer);
    checkDocTimer = setTimeout(async () => {
        try {
            statusDiv.classList.remove('hidden');
            statusDiv.className = "text-xs mt-1 font-medium text-slate-400 flex items-center gap-1";
            statusDiv.innerHTML = `<i class="bi bi-arrow-repeat animate-spin text-blue-500"></i> Checking availability...`;

            const res = await fetch(`/api/dms/documents/check-exists?docNumber=${encodeURIComponent(docNumber.trim())}`);
            if (!res.ok) return;

            const data = await res.json();
            if (data.exists) {
                statusDiv.className = "text-xs mt-1 font-medium text-rose-600 dark:text-rose-400 flex items-center gap-1 font-bold";
                statusDiv.innerHTML = `<i class="bi bi-exclamation-triangle-fill text-rose-500"></i> <span>Document ID "${escapeHtml(docNumber)}" already exists! Please change sequence number.</span>`;
                if (submitBtn) submitBtn.disabled = true;
            } else {
                statusDiv.className = "text-xs mt-1 font-medium text-emerald-600 dark:text-emerald-400 flex items-center gap-1 font-bold";
                statusDiv.innerHTML = `<i class="bi bi-check-circle-fill text-emerald-500"></i> <span>Document ID is available</span>`;
                if (submitBtn) submitBtn.disabled = false;
            }
        } catch (err) {
            console.error("Error checking document ID:", err);
            statusDiv.classList.add('hidden');
            if (submitBtn) submitBtn.disabled = false;
        }
    }, 300);
}

function getTypeCodeAbbreviation(typeStr) {
    if (!typeStr) return 'WI';
    const cleanStr = typeStr.toUpperCase().trim();
    if (cleanStr.includes('WORK_INSTRUCTION') || cleanStr.includes('WORK INSTRUCTION') || cleanStr === 'WI') return 'WI';
    if (cleanStr.includes('PROCESS')) return 'PROC';
    if (cleanStr.includes('CHECKSHEET')) return 'CS';
    if (cleanStr.includes('STANDARD')) return 'STD';
    if (cleanStr.includes('COMMON')) return 'CM';
    if (cleanStr.includes('CSR')) return 'CSR';
    if (cleanStr.includes('IFP')) return 'IFP';
    if (cleanStr.includes('PFD')) return 'PFD';
    if (cleanStr.includes('PFMEA')) return 'PFMEA';
    if (cleanStr.includes('MAINTENANCE')) return 'MAINT';
    if (cleanStr.includes('CONTROL_PLAN') || cleanStr.includes('CONTROL PLAN')) return 'CP';
    return cleanStr.replace(/[^A-Z0-9]/g, '') || 'WI';
}

function toggleVersionDropdown(e) {
    if (e) e.stopPropagation();
    const dropdown = document.getElementById('versionDropdownMenu');
    if (dropdown) dropdown.classList.toggle('hidden');
}

function setInitialVersion(ver) {
    const input = document.getElementById('uploadVersionInput');
    if (input) input.value = ver;
    const dropdown = document.getElementById('versionDropdownMenu');
    if (dropdown) dropdown.classList.add('hidden');
}

function incrementInitialVersion(step) {
    const input = document.getElementById('uploadVersionInput');
    if (input) {
        let curVal = parseFloat(input.value.replace('v', '')) || 1.0;
        let newVal = (curVal + step).toFixed(1);
        input.value = 'v' + newVal;
    }
}

document.addEventListener('click', function (e) {
    const dropdown = document.getElementById('versionDropdownMenu');
    const btn = e.target ? e.target.closest('button') : null;
    if (dropdown && !dropdown.contains(e.target) && (!btn || !btn.getAttribute('onclick')?.includes('toggleVersionDropdown'))) {
        dropdown.classList.add('hidden');
    }
});

function closeUploadModal() {
    const modal = document.getElementById('uploadDocModal');
    if (modal) modal.classList.add('hidden');
}

function openUpdateVersionModal(docId, docNum, curVer) {
    document.getElementById('updateDocId').value = docId;
    document.getElementById('updateDocNum').innerText = docNum;
    document.getElementById('updateCurVer').innerText = curVer;

    // Suggest next ver
    const verNum = parseFloat(curVer.replace('v', '')) || 1.0;
    document.getElementById('newVerInput').value = 'v' + (verNum + 0.1).toFixed(1);

    const modal = document.getElementById('updateVerModal');
    if (modal) modal.classList.remove('hidden');
}

function closeUpdateVersionModal() {
    const modal = document.getElementById('updateVerModal');
    if (modal) modal.classList.add('hidden');
}

async function submitUploadForm(e) {
    e.preventDefault();
    const form = document.getElementById('uploadDocForm');
    const formData = new FormData(form);

    try {
        const res = await fetch('/api/dms/documents', {
            method: 'POST',
            body: formData
        });

        const data = await res.json();
        if (data.success) {
            alert(data.message);
            closeUploadModal();
            form.reset();
            loadCategories();
            loadDocuments();
        } else {
            alert("Error: " + data.message);
        }
    } catch (err) {
        console.error(err);
        alert("Error uploading new document.");
    }
}

async function submitUpdateVersionForm(e) {
    e.preventDefault();
    const form = document.getElementById('updateVerForm');
    const formData = new FormData(form);

    try {
        const res = await fetch('/api/dms/documents/update-version', {
            method: 'POST',
            body: formData
        });

        const data = await res.json();
        if (data.success) {
            alert(data.message);
            closeUpdateVersionModal();
            form.reset();
            loadDocuments();
        } else {
            alert("Error: " + data.message);
        }
    } catch (err) {
        console.error(err);
        alert("Error updating document version.");
    }
}

async function approveDoc(id) {
    if (!confirm("Are you sure you want to approve this document?")) return;
    try {
        const res = await fetch(`/api/dms/documents/${id}/approve?approvedBy=Admin`, { method: 'POST' });
        const data = await res.json();
        if (data.success) {
            alert(data.message);
            loadDocuments();
        } else {
            alert(data.message);
        }
    } catch (err) {
        console.error(err);
        alert("Error approving document.");
    }
}

function escapeHtml(str) {
    if (!str) return '';
    return String(str).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}

/* ============================================================
   MASTER DATA MANAGEMENT (ADMIN ONLY)
   ============================================================ */

let currentMasterTab = 'processes';

function openMasterDataModal() {
    const modal = document.getElementById('masterDataModal');
    if (modal) {
        modal.classList.remove('hidden');
        switchMasterTab(currentMasterTab);
    }
}

function closeMasterDataModal() {
    const modal = document.getElementById('masterDataModal');
    if (modal) modal.classList.add('hidden');
}

function switchMasterTab(tabName) {
    currentMasterTab = tabName;
    const tabBtns = document.querySelectorAll('.master-tab-btn');
    tabBtns.forEach(btn => {
        btn.className = "master-tab-btn px-3 py-1.5 rounded bg-slate-200 dark:bg-slate-700 text-slate-700 dark:text-slate-200 font-bold";
    });

    const activeBtn = document.getElementById(`masterTab-${tabName}`);
    if (activeBtn) {
        activeBtn.className = "master-tab-btn px-3 py-1.5 rounded bg-blue-600 text-white font-bold shadow-sm";
    }

    if (tabName === 'processes') renderMasterProcessTab();
    else if (tabName === 'machines') renderMasterMachineTab();
    else if (tabName === 'models') renderMasterModelTab();
    else if (tabName === 'departments') renderMasterDeptTab();
    else if (tabName === 'types') renderMasterTypeTab();
}

async function refreshAllMasterData() {
    await Promise.all([
        loadCategories(),
        loadDepartments(),
        loadAdvancedFilters()
    ]);
    populateUploadModalOptions();
}

/* --- 1. PROCESSES --- */
function renderMasterProcessTab() {
    const container = document.getElementById('masterTabContent');
    if (!container) return;

    let html = `
        <div class="mb-3 p-3 bg-slate-50 dark:bg-slate-800/60 border border-slate-200 dark:border-slate-700 rounded-lg">
            <div class="text-xs font-bold text-slate-600 dark:text-slate-300 mb-2 flex items-center gap-1">
                <i class="bi bi-plus-circle-fill text-blue-500"></i> Add New Process
            </div>
            <div class="flex flex-wrap items-center gap-2">
                <input type="text" id="newProcCode" placeholder="Process Code (e.g. AOI)" class="dms-form-control text-xs flex-1" />
                <input type="text" id="newProcName" placeholder="Process Name (e.g. Automated Optical Inspection)" class="dms-form-control text-xs flex-1" />
                <button onclick="saveNewProcess()" class="px-3 py-1.5 bg-blue-600 hover:bg-blue-500 text-white rounded text-xs font-bold shadow">Add Process</button>
            </div>
        </div>
        <div class="max-h-60 overflow-y-auto border border-slate-200 dark:border-slate-700 rounded-lg">
            <table class="w-full text-xs text-left">
                <thead class="bg-slate-100 dark:bg-slate-800 font-bold uppercase text-[10px] text-slate-500">
                    <tr>
                        <th class="p-2">Code</th>
                        <th class="p-2">Name</th>
                        <th class="p-2">Status</th>
                        <th class="p-2 text-right">Actions</th>
                    </tr>
                </thead>
                <tbody>
    `;

    dmsMasterData.processes.forEach(p => {
        html += `
            <tr class="border-t border-slate-200 dark:border-slate-700/60 hover:bg-slate-50 dark:hover:bg-slate-800/50">
                <td class="p-2 font-mono font-bold">${escapeHtml(p.processCode)}</td>
                <td class="p-2">${escapeHtml(p.processName)}</td>
                <td class="p-2"><span class="px-1.5 py-0.5 rounded text-[10px] font-bold ${p.isActive ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-400' : 'bg-slate-100 text-slate-500'}">${p.isActive ? 'Active' : 'Disabled'}</span></td>
                <td class="p-2 text-right">
                    <button onclick="editProcess(${p.id}, '${escapeHtml(p.processCode)}', '${escapeHtml(p.processName)}')" class="text-blue-500 hover:underline mr-2">Edit</button>
                    ${p.isActive ? `<button onclick="deleteProcess(${p.id})" class="text-rose-500 hover:underline">Disable</button>` : ''}
                </td>
            </tr>
        `;
    });

    html += `</tbody></table></div>`;
    container.innerHTML = html;
}

async function saveNewProcess() {
    const code = document.getElementById('newProcCode')?.value.trim();
    const name = document.getElementById('newProcName')?.value.trim();
    if (!code || !name) { alert("Please enter both Process Code and Name."); return; }

    const res = await fetch('/api/dms/master/process', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ processCode: code, processName: name })
    });
    if (res.ok) {
        alert("Process added successfully!");
        await refreshAllMasterData();
        renderMasterProcessTab();
    }
}

async function editProcess(id, curCode, curName) {
    const code = prompt("Enter new Process Code:", curCode);
    if (!code) return;
    const name = prompt("Enter new Process Name:", curName);
    if (!name) return;

    const res = await fetch(`/api/dms/master/process/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ processCode: code, processName: name, isActive: true })
    });
    if (res.ok) {
        alert("Process updated successfully!");
        await refreshAllMasterData();
        renderMasterProcessTab();
    }
}

async function deleteProcess(id) {
    if (!confirm("Are you sure you want to disable this Process?")) return;
    const res = await fetch(`/api/dms/master/process/${id}`, { method: 'DELETE' });
    if (res.ok) {
        alert("Process disabled!");
        await refreshAllMasterData();
        renderMasterProcessTab();
    }
}

/* --- 2. MACHINES --- */
function renderMasterMachineTab() {
    const container = document.getElementById('masterTabContent');
    if (!container) return;

    let procOptions = '<option value="">-- Parent Process (Optional) --</option>';
    dmsMasterData.processes.forEach(p => {
        procOptions += `<option value="${p.id}">${escapeHtml(p.processName)}</option>`;
    });

    let html = `
        <div class="mb-3 p-3 bg-slate-50 dark:bg-slate-800/60 border border-slate-200 dark:border-slate-700 rounded-lg">
            <div class="text-xs font-bold text-slate-600 dark:text-slate-300 mb-2 flex items-center gap-1">
                <i class="bi bi-plus-circle-fill text-blue-500"></i> Add New Machine
            </div>
            <div class="flex flex-wrap items-center gap-2">
                <input type="text" id="newMachCode" placeholder="Machine Code (e.g. AOI_01)" class="dms-form-control text-xs flex-1" />
                <input type="text" id="newMachName" placeholder="Machine Name (e.g. AOI Machine 1)" class="dms-form-control text-xs flex-1" />
                <select id="newMachProcId" class="dms-form-control text-xs flex-1">${procOptions}</select>
                <button onclick="saveNewMachine()" class="px-3 py-1.5 bg-blue-600 hover:bg-blue-500 text-white rounded text-xs font-bold shadow">Add Machine</button>
            </div>
        </div>
        <div class="max-h-60 overflow-y-auto border border-slate-200 dark:border-slate-700 rounded-lg">
            <table class="w-full text-xs text-left">
                <thead class="bg-slate-100 dark:bg-slate-800 font-bold uppercase text-[10px] text-slate-500">
                    <tr>
                        <th class="p-2">Code</th>
                        <th class="p-2">Name</th>
                        <th class="p-2">Status</th>
                        <th class="p-2 text-right">Actions</th>
                    </tr>
                </thead>
                <tbody>
    `;

    dmsMasterData.machines.forEach(m => {
        html += `
            <tr class="border-t border-slate-200 dark:border-slate-700/60 hover:bg-slate-50 dark:hover:bg-slate-800/50">
                <td class="p-2 font-mono font-bold">${escapeHtml(m.machineCode)}</td>
                <td class="p-2">${escapeHtml(m.machineName)}</td>
                <td class="p-2"><span class="px-1.5 py-0.5 rounded text-[10px] font-bold ${m.isActive ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-400' : 'bg-slate-100 text-slate-500'}">${m.isActive ? 'Active' : 'Disabled'}</span></td>
                <td class="p-2 text-right">
                    <button onclick="editMachine(${m.id}, '${escapeHtml(m.machineCode)}', '${escapeHtml(m.machineName)}')" class="text-blue-500 hover:underline mr-2">Edit</button>
                    ${m.isActive ? `<button onclick="deleteMachine(${m.id})" class="text-rose-500 hover:underline">Disable</button>` : ''}
                </td>
            </tr>
        `;
    });

    html += `</tbody></table></div>`;
    container.innerHTML = html;
}

async function saveNewMachine() {
    const code = document.getElementById('newMachCode')?.value.trim();
    const name = document.getElementById('newMachName')?.value.trim();
    const procIdVal = document.getElementById('newMachProcId')?.value;
    const processId = procIdVal ? parseInt(procIdVal) : null;

    if (!code || !name) { alert("Please enter Machine Code and Name."); return; }

    const res = await fetch('/api/dms/master/machine', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ machineCode: code, machineName: name, processId: processId })
    });
    if (res.ok) {
        alert("Machine added successfully!");
        await refreshAllMasterData();
        renderMasterMachineTab();
    }
}

async function editMachine(id, curCode, curName) {
    const code = prompt("Enter new Machine Code:", curCode);
    if (!code) return;
    const name = prompt("Enter new Machine Name:", curName);
    if (!name) return;

    const res = await fetch(`/api/dms/master/machine/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ machineCode: code, machineName: name, isActive: true })
    });
    if (res.ok) {
        alert("Machine updated successfully!");
        await refreshAllMasterData();
        renderMasterMachineTab();
    }
}

async function deleteMachine(id) {
    if (!confirm("Are you sure you want to disable this Machine?")) return;
    const res = await fetch(`/api/dms/master/machine/${id}`, { method: 'DELETE' });
    if (res.ok) {
        alert("Machine disabled!");
        await refreshAllMasterData();
        renderMasterMachineTab();
    }
}

/* --- 3. MODELS --- */
function renderMasterModelTab() {
    const container = document.getElementById('masterTabContent');
    if (!container) return;

    let html = `
        <div class="mb-3 p-3 bg-slate-50 dark:bg-slate-800/60 border border-slate-200 dark:border-slate-700 rounded-lg">
            <div class="text-xs font-bold text-slate-600 dark:text-slate-300 mb-2 flex items-center gap-1">
                <i class="bi bi-plus-circle-fill text-blue-500"></i> Add New Model
            </div>
            <div class="flex flex-wrap items-center gap-2">
                <input type="text" id="newModelCode" placeholder="Model Code (e.g. AUDI_CID)" class="dms-form-control text-xs flex-1" />
                <input type="text" id="newModelName" placeholder="Model Name (e.g. AUDI CID Display)" class="dms-form-control text-xs flex-1" />
                <input type="text" id="newModelCustomer" placeholder="Customer (e.g. AUDI AG)" class="dms-form-control text-xs flex-1" />
                <button onclick="saveNewModel()" class="px-3 py-1.5 bg-blue-600 hover:bg-blue-500 text-white rounded text-xs font-bold shadow">Add Model</button>
            </div>
        </div>
        <div class="max-h-60 overflow-y-auto border border-slate-200 dark:border-slate-700 rounded-lg">
            <table class="w-full text-xs text-left">
                <thead class="bg-slate-100 dark:bg-slate-800 font-bold uppercase text-[10px] text-slate-500">
                    <tr>
                        <th class="p-2">Code</th>
                        <th class="p-2">Name</th>
                        <th class="p-2">Customer</th>
                        <th class="p-2">Status</th>
                        <th class="p-2 text-right">Actions</th>
                    </tr>
                </thead>
                <tbody>
    `;

    dmsMasterData.models.forEach(m => {
        html += `
            <tr class="border-t border-slate-200 dark:border-slate-700/60 hover:bg-slate-50 dark:hover:bg-slate-800/50">
                <td class="p-2 font-mono font-bold">${escapeHtml(m.modelCode)}</td>
                <td class="p-2">${escapeHtml(m.modelName)}</td>
                <td class="p-2 text-slate-400">${escapeHtml(m.customerName || '-')}</td>
                <td class="p-2"><span class="px-1.5 py-0.5 rounded text-[10px] font-bold ${m.isActive ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-400' : 'bg-slate-100 text-slate-500'}">${m.isActive ? 'Active' : 'Disabled'}</span></td>
                <td class="p-2 text-right">
                    <button onclick="editModel(${m.id}, '${escapeHtml(m.modelCode)}', '${escapeHtml(m.modelName)}', '${escapeHtml(m.customerName || '')}')" class="text-blue-500 hover:underline mr-2">Edit</button>
                    ${m.isActive ? `<button onclick="deleteModel(${m.id})" class="text-rose-500 hover:underline">Disable</button>` : ''}
                </td>
            </tr>
        `;
    });

    html += `</tbody></table></div>`;
    container.innerHTML = html;
}

async function saveNewModel() {
    const code = document.getElementById('newModelCode')?.value.trim();
    const name = document.getElementById('newModelName')?.value.trim();
    const cust = document.getElementById('newModelCustomer')?.value.trim();
    if (!code || !name) { alert("Please enter Model Code and Name."); return; }

    const res = await fetch('/api/dms/master/model', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ modelCode: code, modelName: name, customerName: cust })
    });
    if (res.ok) {
        alert("Model added successfully!");
        await refreshAllMasterData();
        renderMasterModelTab();
    }
}

async function editModel(id, curCode, curName, curCust) {
    const code = prompt("Enter new Model Code:", curCode);
    if (!code) return;
    const name = prompt("Enter new Model Name:", curName);
    if (!name) return;
    const cust = prompt("Enter Customer Name:", curCust);

    const res = await fetch(`/api/dms/master/model/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ modelCode: code, modelName: name, customerName: cust, isActive: true })
    });
    if (res.ok) {
        alert("Model updated successfully!");
        await refreshAllMasterData();
        renderMasterModelTab();
    }
}

async function deleteModel(id) {
    if (!confirm("Are you sure you want to disable this Model?")) return;
    const res = await fetch(`/api/dms/master/model/${id}`, { method: 'DELETE' });
    if (res.ok) {
        alert("Model disabled!");
        await refreshAllMasterData();
        renderMasterModelTab();
    }
}

/* --- 4. DEPARTMENTS --- */
function renderMasterDeptTab() {
    const container = document.getElementById('masterTabContent');
    if (!container) return;

    let html = `
        <div class="mb-3 p-3 bg-slate-50 dark:bg-slate-800/60 border border-slate-200 dark:border-slate-700 rounded-lg">
            <div class="text-xs font-bold text-slate-600 dark:text-slate-300 mb-2 flex items-center gap-1">
                <i class="bi bi-plus-circle-fill text-blue-500"></i> Add New Department
            </div>
            <div class="flex flex-wrap items-center gap-2">
                <input type="text" id="newDeptCode" placeholder="Code (e.g. LQC SMT)" class="dms-form-control text-xs flex-1" />
                <input type="text" id="newDeptName" placeholder="Name (e.g. Line QC SMT)" class="dms-form-control text-xs flex-1" />
                <input type="color" id="newDeptColor" value="#EA580C" class="h-8 w-12 p-0.5 border rounded cursor-pointer" title="Badge Color" />
                <button onclick="saveNewDept()" class="px-3 py-1.5 bg-blue-600 hover:bg-blue-500 text-white rounded text-xs font-bold shadow">Add Dept</button>
            </div>
        </div>
        <div class="max-h-60 overflow-y-auto border border-slate-200 dark:border-slate-700 rounded-lg">
            <table class="w-full text-xs text-left">
                <thead class="bg-slate-100 dark:bg-slate-800 font-bold uppercase text-[10px] text-slate-500">
                    <tr>
                        <th class="p-2">Code</th>
                        <th class="p-2">Name</th>
                        <th class="p-2">Badge Color</th>
                        <th class="p-2 text-right">Actions</th>
                    </tr>
                </thead>
                <tbody>
    `;

    dmsMasterData.departments.forEach(d => {
        html += `
            <tr class="border-t border-slate-200 dark:border-slate-700/60 hover:bg-slate-50 dark:hover:bg-slate-800/50">
                <td class="p-2 font-mono font-bold">${escapeHtml(d.deptCode)}</td>
                <td class="p-2">${escapeHtml(d.deptName)}</td>
                <td class="p-2"><span class="px-2 py-0.5 rounded text-white text-[10px] font-bold" style="background-color: ${d.badgeColor}">${escapeHtml(d.badgeColor)}</span></td>
                <td class="p-2 text-right">
                    <button onclick="editDept(${d.id}, '${escapeHtml(d.deptCode)}', '${escapeHtml(d.deptName)}', '${escapeHtml(d.badgeColor)}')" class="text-blue-500 hover:underline mr-2">Edit</button>
                    <button onclick="deleteDept(${d.id})" class="text-rose-500 hover:underline">Disable</button>
                </td>
            </tr>
        `;
    });

    html += `</tbody></table></div>`;
    container.innerHTML = html;
}

async function saveNewDept() {
    const code = document.getElementById('newDeptCode')?.value.trim();
    const name = document.getElementById('newDeptName')?.value.trim();
    const color = document.getElementById('newDeptColor')?.value || '#0B72B9';
    if (!code || !name) { alert("Please enter Department Code and Name."); return; }

    const res = await fetch('/api/dms/master/department', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ deptCode: code, deptName: name, badgeColor: color })
    });
    if (res.ok) {
        alert("Department added successfully!");
        await refreshAllMasterData();
        renderMasterDeptTab();
    }
}

async function editDept(id, curCode, curName, curColor) {
    const code = prompt("Enter new Dept Code:", curCode);
    if (!code) return;
    const name = prompt("Enter new Dept Name:", curName);
    if (!name) return;
    const color = prompt("Enter Badge Color Hex:", curColor);

    const res = await fetch(`/api/dms/master/department/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ deptCode: code, deptName: name, badgeColor: color || curColor, isActive: true })
    });
    if (res.ok) {
        alert("Department updated successfully!");
        await refreshAllMasterData();
        renderMasterDeptTab();
    }
}

async function deleteDept(id) {
    if (!confirm("Are you sure you want to disable this Department?")) return;
    const res = await fetch(`/api/dms/master/department/${id}`, { method: 'DELETE' });
    if (res.ok) {
        alert("Department disabled!");
        await refreshAllMasterData();
        renderMasterDeptTab();
    }
}

/* --- 5. DOCUMENT TYPES --- */
function renderMasterTypeTab() {
    const container = document.getElementById('masterTabContent');
    if (!container) return;

    let html = `
        <div class="mb-3 p-3 bg-slate-50 dark:bg-slate-800/60 border border-slate-200 dark:border-slate-700 rounded-lg">
            <div class="text-xs font-bold text-slate-600 dark:text-slate-300 mb-2 flex items-center gap-1">
                <i class="bi bi-plus-circle-fill text-blue-500"></i> Add New Document Type
            </div>
            <div class="flex flex-wrap items-center gap-2">
                <input type="text" id="newTypeCode" placeholder="Code (e.g. WI)" class="dms-form-control text-xs flex-1" />
                <input type="text" id="newTypeName" placeholder="Name (e.g. Work Instruction)" class="dms-form-control text-xs flex-1" />
                <button onclick="saveNewType()" class="px-3 py-1.5 bg-blue-600 hover:bg-blue-500 text-white rounded text-xs font-bold shadow">Add Type</button>
            </div>
        </div>
        <div class="max-h-60 overflow-y-auto border border-slate-200 dark:border-slate-700 rounded-lg">
            <table class="w-full text-xs text-left">
                <thead class="bg-slate-100 dark:bg-slate-800 font-bold uppercase text-[10px] text-slate-500">
                    <tr>
                        <th class="p-2">Code</th>
                        <th class="p-2">Name</th>
                        <th class="p-2 text-right">Actions</th>
                    </tr>
                </thead>
                <tbody>
    `;

    dmsMasterData.categories.forEach(t => {
        html += `
            <tr class="border-t border-slate-200 dark:border-slate-700/60 hover:bg-slate-50 dark:hover:bg-slate-800/50">
                <td class="p-2 font-mono font-bold">${escapeHtml(t.typeCode)}</td>
                <td class="p-2">${escapeHtml(t.typeName)}</td>
                <td class="p-2 text-right">
                    <button onclick="editType(${t.typeId}, '${escapeHtml(t.typeCode)}', '${escapeHtml(t.typeName)}')" class="text-blue-500 hover:underline mr-2">Edit</button>
                    <button onclick="deleteType(${t.typeId})" class="text-rose-500 hover:underline">Disable</button>
                </td>
            </tr>
        `;
    });

    html += `</tbody></table></div>`;
    container.innerHTML = html;
}

async function saveNewType() {
    const code = document.getElementById('newTypeCode')?.value.trim();
    const name = document.getElementById('newTypeName')?.value.trim();
    if (!code || !name) { alert("Please enter Document Type Code and Name."); return; }

    const res = await fetch('/api/dms/master/type', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ typeCode: code, typeName: name })
    });
    if (res.ok) {
        alert("Document Type added successfully!");
        await refreshAllMasterData();
        renderMasterTypeTab();
    }
}

async function editType(id, curCode, curName) {
    const code = prompt("Enter new Type Code:", curCode);
    if (!code) return;
    const name = prompt("Enter new Type Name:", curName);
    if (!name) return;

    const res = await fetch(`/api/dms/master/type/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ typeCode: code, typeName: name, isActive: true })
    });
    if (res.ok) {
        alert("Document Type updated successfully!");
        await refreshAllMasterData();
        renderMasterTypeTab();
    }
}

async function deleteType(id) {
    if (!confirm("Are you sure you want to disable this Document Type?")) return;
    const res = await fetch(`/api/dms/master/type/${id}`, { method: 'DELETE' });
    if (res.ok) {
        alert("Document Type disabled!");
        await refreshAllMasterData();
        renderMasterTypeTab();
    }
}

// MODAL 4: VIEW DOCUMENT DETAILS
async function openDocDetailsModal(docId) {
    const modal = document.getElementById('docDetailsModal');
    const body = document.getElementById('docDetailsBody');
    if (!modal || !body) return;

    modal.classList.remove('hidden');
    body.innerHTML = `<div class="text-center py-6 text-slate-400"><i class="bi bi-arrow-repeat animate-spin text-lg"></i> Loading details...</div>`;

    try {
        const res = await fetch(`/api/dms/documents/${docId}`);
        if (!res.ok) throw new Error("Failed to load document details.");
        const doc = await res.json();

        const procNames = doc.applicableProcesses && doc.applicableProcesses.length > 0 ? doc.applicableProcesses.map(p => p.processName || p.processCode).join(', ') : 'All Processes';
        const machNames = doc.applicableMachines && doc.applicableMachines.length > 0 ? doc.applicableMachines.map(m => m.machineName || m.machineCode).join(', ') : 'All Machines';
        const modelNames = doc.applicableModels && doc.applicableModels.length > 0 ? doc.applicableModels.map(m => m.modelName || m.modelCode).join(', ') : 'All Models';
        const updatedStr = doc.updatedAt ? new Date(doc.updatedAt).toLocaleString() : 'N/A';
        const createdStr = doc.createdAt ? new Date(doc.createdAt).toLocaleString() : 'N/A';

        body.innerHTML = `
            <div class="space-y-3 text-slate-700 dark:text-slate-200">
                <div class="p-3 bg-slate-100 dark:bg-slate-900/60 rounded-lg border border-slate-200 dark:border-slate-700/70 flex items-center justify-between">
                    <div>
                        <div class="text-[10px] uppercase font-bold text-slate-400">Document ID</div>
                        <div class="font-mono font-bold text-sm text-blue-600 dark:text-blue-400">${escapeHtml(doc.docNumber)}</div>
                    </div>
                    <div class="text-right">
                        <div class="text-[10px] uppercase font-bold text-slate-400">Current Version</div>
                        <span class="dms-badge-version font-mono font-bold">${escapeHtml(doc.currentVersion || 'v1.0')}</span>
                    </div>
                </div>

                <div>
                    <div class="text-[10px] uppercase font-bold text-slate-400 mb-0.5">Document Title</div>
                    <div class="font-semibold text-sm text-slate-800 dark:text-slate-100">${escapeHtml(doc.title)}</div>
                </div>

                <div class="grid grid-cols-2 gap-3">
                    <div>
                        <div class="text-[10px] uppercase font-bold text-slate-400">Category / Type</div>
                        <div><span class="dms-badge-type mt-1">${escapeHtml(doc.typeName || 'N/A')}</span></div>
                    </div>
                    <div>
                        <div class="text-[10px] uppercase font-bold text-slate-400">Department</div>
                        <div><span class="dms-badge-dept mt-1" style="background-color: ${doc.deptBadgeColor || '#0B72B9'}">${escapeHtml(doc.deptCode)}</span></div>
                    </div>
                    <div>
                        <div class="text-[10px] uppercase font-bold text-slate-400">Status</div>
                        <div class="font-bold text-slate-600 dark:text-slate-300 mt-1">${escapeHtml(doc.status)}</div>
                    </div>
                    <div>
                        <div class="text-[10px] uppercase font-bold text-slate-400">Owner / Uploaded By</div>
                        <div class="font-medium mt-1">${escapeHtml(doc.ownerName || 'System Admin')}</div>
                    </div>
                </div>

                <div class="border-t border-slate-200 dark:border-slate-700/60 pt-2 space-y-2">
                    <div><strong>Applicable Process(es):</strong> <span class="text-slate-500 dark:text-slate-400">${escapeHtml(procNames)}</span></div>
                    <div><strong>Applicable Machine(s):</strong> <span class="text-slate-500 dark:text-slate-400">${escapeHtml(machNames)}</span></div>
                    <div><strong>Applicable Model(s):</strong> <span class="text-slate-500 dark:text-slate-400">${escapeHtml(modelNames)}</span></div>
                </div>

                <div class="border-t border-slate-200 dark:border-slate-700/60 pt-2">
                    <div class="text-[10px] uppercase font-bold text-slate-400 mb-1">Description / Purpose</div>
                    <div class="p-2.5 bg-slate-50 dark:bg-slate-900/40 rounded border border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-300 min-h-[50px] whitespace-pre-wrap">${escapeHtml(doc.description || 'No detailed description provided.')}</div>
                </div>

                <div class="grid grid-cols-2 gap-2 text-[11px] text-slate-400 pt-2 border-t border-slate-200 dark:border-slate-700/60">
                    <div>Created At: ${createdStr}</div>
                    <div class="text-right">Last Updated: ${updatedStr}</div>
                </div>
            </div>
        `;
    } catch (err) {
        body.innerHTML = `<div class="text-center py-6 text-rose-500">Failed to load document details.</div>`;
    }
}

function closeDocDetailsModal() {
    const modal = document.getElementById('docDetailsModal');
    if (modal) modal.classList.add('hidden');
}

// MODAL 5: REVISION HISTORY
async function openRevisionHistoryModal(docId) {
    const modal = document.getElementById('revisionHistoryModal');
    const body = document.getElementById('revisionHistoryBody');
    if (!modal || !body) return;

    modal.classList.remove('hidden');
    body.innerHTML = `<div class="text-center py-6 text-slate-400"><i class="bi bi-arrow-repeat animate-spin text-lg"></i> Loading revision history...</div>`;

    try {
        const res = await fetch(`/api/dms/documents/${docId}/versions`);
        if (!res.ok) throw new Error("Failed to load revision history.");
        const versions = await res.json();

        if (!versions || versions.length === 0) {
            body.innerHTML = `<div class="text-center py-6 text-slate-400">No revision history recorded for this document.</div>`;
            return;
        }

        let html = `
            <div class="overflow-x-auto">
                <table class="w-full text-left border-collapse">
                    <thead>
                        <tr class="border-b border-slate-200 dark:border-slate-700 text-slate-400 font-bold uppercase text-[10px] tracking-wider">
                            <th class="p-2">Version</th>
                            <th class="p-2">File Name</th>
                            <th class="p-2">Uploaded By</th>
                            <th class="p-2">Uploaded At</th>
                            <th class="p-2">Change Summary</th>
                            <th class="p-2 text-right">Action</th>
                        </tr>
                    </thead>
                    <tbody class="divide-y divide-slate-200 dark:divide-slate-700/60">
        `;

        versions.forEach(v => {
            const dateStr = v.uploadedAt ? new Date(v.uploadedAt).toLocaleString() : '-';
            html += `
                <tr class="hover:bg-slate-50 dark:hover:bg-slate-800/50">
                    <td class="p-2 font-mono font-bold text-blue-500">${escapeHtml(v.versionNumber)}</td>
                    <td class="p-2 font-mono text-[11px] max-w-[160px] truncate" title="${escapeHtml(v.fileName)}">${escapeHtml(v.fileName)}</td>
                    <td class="p-2 text-slate-400">${escapeHtml(v.uploadedBy || 'System Admin')}</td>
                    <td class="p-2 text-slate-400">${dateStr}</td>
                    <td class="p-2 text-slate-300">${escapeHtml(v.changeSummary || '-')}</td>
                    <td class="p-2 text-right">
                        <a href="/api/dms/documents/${docId}/download?versionId=${v.id}" target="_blank" class="px-2 py-1 bg-blue-600 hover:bg-blue-500 text-white font-bold rounded text-[11px] inline-flex items-center gap-1 shadow">
                            <i class="bi bi-download"></i> Download
                        </a>
                    </td>
                </tr>
            `;
        });

        html += `
                    </tbody>
                </table>
            </div>
        `;

        body.innerHTML = html;
    } catch (err) {
        body.innerHTML = `<div class="text-center py-6 text-rose-500">Failed to load revision history.</div>`;
    }
}

function closeRevisionHistoryModal() {
    const modal = document.getElementById('revisionHistoryModal');
    if (modal) modal.classList.add('hidden');
}

// MODAL 6: DOCUMENT PREVIEW MODAL (POWERED BY LIBREOFFICE)
function openDocPreviewModal(docId, title, docNumber) {
    const modal = document.getElementById('docPreviewModal');
    const iframe = document.getElementById('docPreviewFrame');
    const spinner = document.getElementById('previewLoadingSpinner');
    const titleEl = document.getElementById('previewModalDocTitle');
    const numEl = document.getElementById('previewModalDocNumber');
    const dlBtn = document.getElementById('previewDownloadBtn');

    if (!modal || !iframe) return;

    if (titleEl) titleEl.textContent = title || 'Document Preview';
    if (numEl) numEl.textContent = docNumber || '';
    if (dlBtn) dlBtn.href = `/api/dms/documents/${docId}/download`;

    if (spinner) spinner.classList.remove('hidden');
    iframe.src = `/api/dms/documents/${docId}/preview`;
    modal.classList.remove('hidden');
}

function onPreviewFrameLoaded() {
    const spinner = document.getElementById('previewLoadingSpinner');
    if (spinner) spinner.classList.add('hidden');
}

function closeDocPreviewModal() {
    const modal = document.getElementById('docPreviewModal');
    const iframe = document.getElementById('docPreviewFrame');
    if (modal) modal.classList.add('hidden');
    if (iframe) iframe.src = 'about:blank';
}
