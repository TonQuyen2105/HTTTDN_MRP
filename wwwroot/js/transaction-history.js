
const transactions = [
    {
        id: "PN001",
        type: "Nhập kho",
        date: "2026-10-20",
        partner: "Công ty Dược ABC",
        items: 3,
        quantity: 500,
        status: "Hoàn tất",
        details: [
            { name: "Paracetamol 500mg", quantity: 200 },
            { name: "Vitamin C 500mg", quantity: 150 },
            { name: "Amoxicillin 500mg", quantity: 150 }
        ]
    },
    {
        id: "PX001",
        type: "Xuất kho",
        date: "2026-10-21",
        partner: "Nhà thuốc Minh Tâm",
        items: 2,
        quantity: 120,
        status: "Hoàn tất",
        details: [
            { name: "Paracetamol 500mg", quantity: 80 },
            { name: "Vitamin C 500mg", quantity: 40 }
        ]
    },
    {
        id: "PN002",
        type: "Nhập kho",
        date: "2026-10-22",
        partner: "Công ty Dược XYZ",
        items: 2,
        quantity: 300,
        status: "Đang xử lý",
        details: [
            { name: "Amoxicillin 500mg", quantity: 200 },
            { name: "Vitamin C 500mg", quantity: 100 }
        ]
    },
    {
        id: "PX002",
        type: "Xuất kho",
        date: "2026-10-23",
        partner: "Nhà thuốc An Khang",
        items: 2,
        quantity: 90,
        status: "Đã hủy",
        details: [
            { name: "Paracetamol 500mg", quantity: 50 },
            { name: "Amoxicillin 500mg", quantity: 40 }
        ]
    }
];

const tbody = document.getElementById("historyTableBody");

function escapeHtml(value) {
    return String(value).replace(/[&<>"']/g, char => ({
        "&": "&amp;",
        "<": "&lt;",
        ">": "&gt;",
        '"': "&quot;",
        "'": "&#39;"
    })[char]);
}

function formatDate(date) {
    if (!date) return "";
    const [year, month, day] = date.split("-");
    return `${day}/${month}/${year}`;
}

function statusClass(status) {
    if (status === "Hoàn tất") return "status-complete";
    if (status === "Đang xử lý") return "status-pending";
    return "status-cancelled";
}

function getFilteredTransactions() {
    const keyword = document.getElementById("searchInput")
        .value.trim().toLowerCase();
    const type = document.getElementById("typeFilter").value;
    const status = document.getElementById("statusFilter").value;
    const fromDate = document.getElementById("fromDate").value;
    const toDate = document.getElementById("toDate").value;

    return transactions.filter(item => {
        const searchable = [
            item.id,
            item.type,
            item.partner,
            ...item.details.map(detail => detail.name)
        ].join(" ").toLowerCase();

        return (!keyword || searchable.includes(keyword))
            && (!type || item.type === type)
            && (!status || item.status === status)
            && (!fromDate || item.date >= fromDate)
            && (!toDate || item.date <= toDate);
    });
}

function renderHistory() {
    const filtered = getFilteredTransactions();

    document.getElementById("totalCount").textContent =
        transactions.length;

    document.getElementById("importCount").textContent =
        transactions.filter(x => x.type === "Nhập kho").length;

    document.getElementById("exportCount").textContent =
        transactions.filter(x => x.type === "Xuất kho").length;

    document.getElementById("pendingCount").textContent =
        transactions.filter(x => x.status === "Đang xử lý").length;

    document.getElementById("resultCount").textContent =
        `Hiển thị ${filtered.length} / ${transactions.length} giao dịch`;

    if (filtered.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="8" class="empty-state">
                    Không tìm thấy giao dịch phù hợp.
                </td>
            </tr>`;
        return;
    }

    tbody.innerHTML = filtered.map(item => `
        <tr>
            <td><strong>${escapeHtml(item.id)}</strong></td>
            <td>
                <span class="type-badge ${
                    item.type === "Nhập kho" ? "type-import" : "type-export"
                }">${escapeHtml(item.type)}</span>
            </td>
            <td>${formatDate(item.date)}</td>
            <td>${escapeHtml(item.partner)}</td>
            <td>${item.items}</td>
            <td>${item.quantity.toLocaleString("vi-VN")}</td>
            <td>
                <span class="status-badge ${statusClass(item.status)}">
                    ${escapeHtml(item.status)}
                </span>
            </td>
            <td>
                <button class="btn-detail" type="button"
                        data-id="${escapeHtml(item.id)}">
                    Chi tiết
                </button>
            </td>
        </tr>
    `).join("");
}

function showDetail(id) {
    const item = transactions.find(x => x.id === id);
    if (!item) return;

    document.getElementById("detailSubtitle").textContent =
        `${item.id} · ${item.type}`;

    const rows = item.details.map((detail, index) => `
        <tr>
            <td>${index + 1}</td>
            <td>${escapeHtml(detail.name)}</td>
            <td>${detail.quantity.toLocaleString("vi-VN")}</td>
        </tr>
    `).join("");

    document.getElementById("detailContent").innerHTML = `
        <div class="detail-info">
            <p><strong>Mã phiếu:</strong> ${escapeHtml(item.id)}</p>
            <p><strong>Loại:</strong> ${escapeHtml(item.type)}</p>
            <p><strong>Ngày:</strong> ${formatDate(item.date)}</p>
            <p><strong>Đối tác:</strong> ${escapeHtml(item.partner)}</p>
            <p><strong>Trạng thái:</strong> ${escapeHtml(item.status)}</p>
        </div>
        <div class="table-wrapper">
            <table class="history-table">
                <thead>
                    <tr>
                        <th>STT</th>
                        <th>Tên thuốc</th>
                        <th>Số lượng</th>
                    </tr>
                </thead>
                <tbody>${rows}</tbody>
            </table>
        </div>
    `;

    document.getElementById("detailModal").hidden = false;
}

function closeDetail() {
    document.getElementById("detailModal").hidden = true;
}

function resetFilters() {
    document.getElementById("searchInput").value = "";
    document.getElementById("typeFilter").value = "";
    document.getElementById("statusFilter").value = "";
    document.getElementById("fromDate").value = "";
    document.getElementById("toDate").value = "";
    renderHistory();
}

function exportHistory() {
    const rows = [
        ["Mã phiếu", "Loại giao dịch", "Ngày", "Đối tác",
         "Số mặt hàng", "Tổng số lượng", "Trạng thái"],
        ...getFilteredTransactions().map(item => [
            item.id, item.type, formatDate(item.date), item.partner,
            item.items, item.quantity, item.status
        ])
    ];

    const csv = "\uFEFF" + rows.map(row =>
        row.map(value =>
            '"' + String(value).replace(/"/g, '""') + '"'
        ).join(",")
    ).join("\r\n");

    const blob = new Blob([csv], {
        type: "text/csv;charset=utf-8;"
    });

    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = "lich-su-nhap-xuat.csv";
    link.click();
    URL.revokeObjectURL(url);
}

document.getElementById("searchInput")
    .addEventListener("input", renderHistory);

document.getElementById("typeFilter")
    .addEventListener("change", renderHistory);

document.getElementById("statusFilter")
    .addEventListener("change", renderHistory);

document.getElementById("fromDate")
    .addEventListener("change", renderHistory);

document.getElementById("toDate")
    .addEventListener("change", renderHistory);

tbody.addEventListener("click", event => {
    const button = event.target.closest(".btn-detail");
    if (button) showDetail(button.dataset.id);
});

document.getElementById("closeModal")
    .addEventListener("click", closeDetail);

document.getElementById("closeDetailButton")
    .addEventListener("click", closeDetail);

document.getElementById("resetButton")
    .addEventListener("click", resetFilters);

document.getElementById("exportButton")
    .addEventListener("click", exportHistory);

document.getElementById("detailModal")
    .addEventListener("click", event => {
        if (event.target.id === "detailModal") closeDetail();
    });


