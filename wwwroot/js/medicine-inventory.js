// DỮ LIỆU MINH HỌA - CHƯA KẾT NỐI DATABASE
const medicines = [
    {
        code: "TH001",
        name: "Paracetamol 500mg",
        unit: "Hộp",
        quantity: 120,
        price: 25000
    },
    {
        code: "TH002",
        name: "Amoxicillin 500mg",
        unit: "Hộp",
        quantity: 8,
        price: 45000
    },
    {
        code: "TH003",
        name: "Vitamin C 500mg",
        unit: "Hộp",
        quantity: 65,
        price: 30000
    },
    {
        code: "TH004",
        name: "Oresol",
        unit: "Hộp",
        quantity: 0,
        price: 12000
    },
    {
        code: "TH005",
        name: "Loratadin 10mg",
        unit: "Hộp",
        quantity: 10,
        price: 18000
    },
    {
        code: "TH006",
        name: "Berberin",
        unit: "Hộp",
        quantity: 42,
        price: 22000
    }
];

const searchInput = document.getElementById("medicineSearch");
const stockFilter = document.getElementById("stockFilter");
const tableBody = document.getElementById("medicineTableBody");

function formatMoney(value) {
    return new Intl.NumberFormat("vi-VN", {
        style: "currency",
        currency: "VND",
        maximumFractionDigits: 0
    }).format(value);
}

function getStockStatus(quantity) {
    if (quantity === 0) {
        return {
            text: "Hết hàng",
            className: "stock-out",
            key: "out"
        };
    }

    if (quantity <= 10) {
        return {
            text: "Sắp hết hàng",
            className: "stock-low",
            key: "low"
        };
    }

    return {
        text: "Còn hàng",
        className: "stock-available",
        key: "available"
    };
}

function renderStatistics() {
    document.getElementById("totalMedicines").textContent =
        medicines.length;

    document.getElementById("totalQuantity").textContent =
        medicines.reduce((sum, medicine) => sum + medicine.quantity, 0)
            .toLocaleString("vi-VN");

    document.getElementById("lowStockCount").textContent =
        medicines.filter(medicine => medicine.quantity <= 10).length;
}

function renderMedicines() {
    const keyword = searchInput.value.trim().toLocaleLowerCase("vi-VN");
    const selectedStatus = stockFilter.value;

    const filteredMedicines = medicines.filter(medicine => {
        const matchesKeyword =
            medicine.code.toLocaleLowerCase("vi-VN").includes(keyword) ||
            medicine.name.toLocaleLowerCase("vi-VN").includes(keyword);

        const status = getStockStatus(medicine.quantity);
        const matchesStatus =
            selectedStatus === "" || status.key === selectedStatus;

        return matchesKeyword && matchesStatus;
    });

    if (filteredMedicines.length === 0) {
        tableBody.innerHTML = `
            <tr>
                <td colspan="7" class="inventory-empty">
                    Không tìm thấy thuốc phù hợp.
                </td>
            </tr>
        `;
    } else {
        tableBody.innerHTML = filteredMedicines.map(medicine => {
            const status = getStockStatus(medicine.quantity);
            const inventoryValue = medicine.quantity * medicine.price;

            return `
                <tr>
                    <td class="medicine-code">${medicine.code}</td>
                    <td class="medicine-name">${medicine.name}</td>
                    <td>${medicine.unit}</td>
                    <td>${medicine.quantity.toLocaleString("vi-VN")}</td>
                    <td class="medicine-price">${formatMoney(medicine.price)}</td>
                    <td class="medicine-price">${formatMoney(inventoryValue)}</td>
                    <td>
                        <span class="stock-badge ${status.className}">
                            ${status.text}
                        </span>
                    </td>
                </tr>
            `;
        }).join("");
    }

    document.getElementById("resultCount").textContent =
        `Đang hiển thị ${filteredMedicines.length} / ${medicines.length} loại thuốc`;
}

document.getElementById("searchButton").addEventListener("click", renderMedicines);

searchInput.addEventListener("input", renderMedicines);
stockFilter.addEventListener("change", renderMedicines);

document.getElementById("resetButton").addEventListener("click", () => {
    searchInput.value = "";
    stockFilter.value = "";
    renderMedicines();
});

renderStatistics();
renderMedicines();