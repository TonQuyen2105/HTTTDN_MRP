### Bảng `bom` (7 cột)
| Tên cột | Kiểu dữ liệu | Cho phép Null | Khóa chính | Mặc định |
|---|---|:---:|:---:|---|
| `bomid` | `integer` | NOT NULL | **PK** | `` |
| `bomname` | `character varying` | NOT NULL |  | `` |
| `productid` | `integer` | NOT NULL |  | `` |
| `ingrid` | `integer` | NOT NULL |  | `` |
| `itemquantity` | `integer` | NOT NULL |  | `` |
| `materialquantity` | `numeric` | NOT NULL |  | `` |
| `bomcode` | `character varying` | NULL |  | `` |

### Bảng `importreceipt` (6 cột)
| Tên cột | Kiểu dữ liệu | Cho phép Null | Khóa chính | Mặc định |
|---|---|:---:|:---:|---|
| `ipreceiptid` | `integer` | NOT NULL | **PK** | `` |
| `supplierid` | `integer` | NULL |  | `` |
| `createdby` | `integer` | NULL |  | `` |
| `createddate` | `timestamp without time zone` | NULL |  | `CURRENT_TIMESTAMP` |
| `ipreceiptcode` | `character varying` | NULL |  | `` |
| `status` | `character varying` | NULL |  | `` |

### Bảng `importreceiptdetail` (6 cột)
| Tên cột | Kiểu dữ liệu | Cho phép Null | Khóa chính | Mặc định |
|---|---|:---:|:---:|---|
| `ipreceiptid` | `integer` | NOT NULL | **PK** | `` |
| `ingrid` | `integer` | NOT NULL | **PK** | `` |
| `quantity` | `numeric` | NOT NULL |  | `` |
| `cost` | `numeric` | NULL |  | `` |
| `createddate` | `timestamp without time zone` | NULL |  | `CURRENT_TIMESTAMP` |
| `expireddate` | `timestamp without time zone` | NULL |  | `` |

### Bảng `ingredient` (8 cột)
| Tên cột | Kiểu dữ liệu | Cho phép Null | Khóa chính | Mặc định |
|---|---|:---:|:---:|---|
| `ingrid` | `integer` | NOT NULL | **PK** | `` |
| `ingrname` | `character varying` | NOT NULL |  | `` |
| `quantity` | `numeric` | NULL |  | `0` |
| `unit` | `character varying` | NULL |  | `` |
| `price` | `numeric` | NULL |  | `` |
| `imageurl` | `character varying` | NULL |  | `` |
| `description` | `character varying` | NULL |  | `` |
| `shelflifemonths` | `integer` | NULL |  | `0` |

### Bảng `materialreceipt` (7 cột)
| Tên cột | Kiểu dữ liệu | Cho phép Null | Khóa chính | Mặc định |
|---|---|:---:|:---:|---|
| `expreceiptid` | `integer` | NOT NULL | **PK** | `` |
| `requestedby` | `integer` | NULL |  | `` |
| `approvedby` | `integer` | NULL |  | `` |
| `createddate` | `timestamp without time zone` | NULL |  | `CURRENT_TIMESTAMP` |
| `status` | `character varying` | NULL |  | `'Pending'::character varying` |
| `expreceiptcode` | `character varying` | NULL |  | `` |
| `requestid` | `integer` | NULL |  | `` |

### Bảng `materialreceiptdetail` (8 cột)
| Tên cột | Kiểu dữ liệu | Cho phép Null | Khóa chính | Mặc định |
|---|---|:---:|:---:|---|
| `expreceiptid` | `integer` | NOT NULL | **PK** | `` |
| `ingrid` | `integer` | NOT NULL | **PK** | `` |
| `quantity` | `numeric` | NOT NULL |  | `` |
| `status` | `character varying` | NULL |  | `` |
| `note` | `character varying` | NULL |  | `` |
| `createddate` | `timestamp without time zone` | NULL |  | `CURRENT_TIMESTAMP` |
| `exportdate` | `timestamp without time zone` | NULL |  | `` |
| `unitcost` | `numeric` | NULL |  | `` |

### Bảng `product` (8 cột)
| Tên cột | Kiểu dữ liệu | Cho phép Null | Khóa chính | Mặc định |
|---|---|:---:|:---:|---|
| `productid` | `integer` | NOT NULL | **PK** | `` |
| `productname` | `character varying` | NOT NULL |  | `` |
| `quantity` | `integer` | NULL |  | `0` |
| `unit` | `character varying` | NULL |  | `` |
| `cost` | `numeric` | NULL |  | `` |
| `imageurl` | `character varying` | NULL |  | `` |
| `description` | `character varying` | NULL |  | `` |
| `shelflifemonths` | `integer` | NULL |  | `0` |

### Bảng `productexportreceipt` (7 cột)
| Tên cột | Kiểu dữ liệu | Cho phép Null | Khóa chính | Mặc định |
|---|---|:---:|:---:|---|
| `exportreceiptid` | `integer` | NOT NULL | **PK** | `` |
| `requestedby` | `integer` | NULL |  | `` |
| `approvedby` | `integer` | NULL |  | `` |
| `createddate` | `timestamp without time zone` | NULL |  | `` |
| `status` | `character varying` | NULL |  | `` |
| `expreceiptcode` | `character varying` | NULL |  | `` |
| `orderid` | `integer` | NULL |  | `` |

### Bảng `productexportreceiptdetail` (7 cột)
| Tên cột | Kiểu dữ liệu | Cho phép Null | Khóa chính | Mặc định |
|---|---|:---:|:---:|---|
| `exportreceiptid` | `integer` | NOT NULL | **PK** | `` |
| `productid` | `integer` | NOT NULL | **PK** | `` |
| `quantity` | `integer` | NOT NULL |  | `` |
| `price` | `numeric` | NULL |  | `` |
| `status` | `character varying` | NULL |  | `` |
| `note` | `character varying` | NULL |  | `` |
| `createddate` | `timestamp without time zone` | NULL |  | `` |

### Bảng `production_log` (10 cột)
| Tên cột | Kiểu dữ liệu | Cho phép Null | Khóa chính | Mặc định |
|---|---|:---:|:---:|---|
| `logid` | `integer` | NOT NULL | **PK** | `` |
| `requestid` | `integer` | NOT NULL |  | `` |
| `productid` | `integer` | NOT NULL |  | `` |
| `producedquantity` | `integer` | NOT NULL |  | `` |
| `producedat` | `timestamp with time zone` | NOT NULL |  | `` |
| `createdat` | `timestamp with time zone` | NOT NULL |  | `CURRENT_TIMESTAMP` |
| `createdby` | `integer` | NOT NULL |  | `` |
| `operationkey` | `uuid` | NOT NULL |  | `` |
| `note` | `character varying` | NULL |  | `` |
| `expireddate` | `date` | NULL |  | `` |

### Bảng `productionrequest` (8 cột)
| Tên cột | Kiểu dữ liệu | Cho phép Null | Khóa chính | Mặc định |
|---|---|:---:|:---:|---|
| `requestid` | `integer` | NOT NULL | **PK** | `` |
| `managerid` | `integer` | NOT NULL |  | `` |
| `productid` | `integer` | NOT NULL |  | `` |
| `quantityrequired` | `integer` | NOT NULL |  | `` |
| `status` | `character varying` | NULL |  | `'Pending'::character varying` |
| `createddate` | `timestamp without time zone` | NULL |  | `CURRENT_TIMESTAMP` |
| `requestcode` | `character varying` | NULL |  | `` |
| `assignedto` | `integer` | NULL |  | `` |

### Bảng `productreceipt` (7 cột)
| Tên cột | Kiểu dữ liệu | Cho phép Null | Khóa chính | Mặc định |
|---|---|:---:|:---:|---|
| `productreceiptid` | `integer` | NOT NULL | **PK** | `` |
| `requestedby` | `integer` | NULL |  | `` |
| `approvedby` | `integer` | NULL |  | `` |
| `createddate` | `timestamp without time zone` | NULL |  | `CURRENT_TIMESTAMP` |
| `status` | `character varying` | NULL |  | `'Pending'::character varying` |
| `requestid` | `integer` | NULL |  | `` |
| `receiptcode` | `character varying` | NULL |  | `` |

### Bảng `productreceiptdetail` (9 cột)
| Tên cột | Kiểu dữ liệu | Cho phép Null | Khóa chính | Mặc định |
|---|---|:---:|:---:|---|
| `productreceiptid` | `integer` | NOT NULL | **PK** | `` |
| `productid` | `integer` | NOT NULL | **PK** | `` |
| `quantity` | `integer` | NOT NULL |  | `` |
| `status` | `character varying` | NULL |  | `` |
| `note` | `character varying` | NULL |  | `` |
| `createddate` | `timestamp without time zone` | NULL |  | `CURRENT_TIMESTAMP` |
| `expireddate` | `timestamp without time zone` | NULL |  | `` |
| `cost` | `numeric` | NULL |  | `` |
| `productionlogid` | `integer` | NULL |  | `` |

### Bảng `saleorder` (9 cột)
| Tên cột | Kiểu dữ liệu | Cho phép Null | Khóa chính | Mặc định |
|---|---|:---:|:---:|---|
| `orderid` | `integer` | NOT NULL | **PK** | `` |
| `createdby` | `integer` | NULL |  | `` |
| `customername` | `character varying` | NULL |  | `` |
| `customerphone` | `character varying` | NULL |  | `` |
| `shippingaddress` | `character varying` | NULL |  | `` |
| `totalamount` | `numeric` | NULL |  | `` |
| `orderdate` | `timestamp without time zone` | NULL |  | `` |
| `status` | `character varying` | NULL |  | `` |
| `ordercode` | `character varying` | NULL |  | `` |

### Bảng `saleorderdetail` (4 cột)
| Tên cột | Kiểu dữ liệu | Cho phép Null | Khóa chính | Mặc định |
|---|---|:---:|:---:|---|
| `orderid` | `integer` | NOT NULL | **PK** | `` |
| `productid` | `integer` | NOT NULL | **PK** | `` |
| `quantity` | `integer` | NOT NULL |  | `` |
| `unitprice` | `numeric` | NULL |  | `` |

### Bảng `supplier` (6 cột)
| Tên cột | Kiểu dữ liệu | Cho phép Null | Khóa chính | Mặc định |
|---|---|:---:|:---:|---|
| `supplierid` | `integer` | NOT NULL | **PK** | `` |
| `suppliername` | `character varying` | NOT NULL |  | `` |
| `phone` | `character varying` | NULL |  | `` |
| `address` | `character varying` | NULL |  | `` |
| `mail` | `character varying` | NULL |  | `` |
| `import_cost` | `numeric` | NULL |  | `0` |

### Bảng `useraccount` (7 cột)
| Tên cột | Kiểu dữ liệu | Cho phép Null | Khóa chính | Mặc định |
|---|---|:---:|:---:|---|
| `userid` | `integer` | NOT NULL | **PK** | `` |
| `username` | `character varying` | NOT NULL |  | `` |
| `password` | `character varying` | NOT NULL |  | `` |
| `fullname` | `character varying` | NOT NULL |  | `` |
| `phone` | `character varying` | NULL |  | `` |
| `role` | `character varying` | NULL |  | `` |
| `status` | `character varying` | NULL |  | `'Active'::character varying` |

### Bảng `wip_inventory` (6 cột)
| Tên cột | Kiểu dữ liệu | Cho phép Null | Khóa chính | Mặc định |
|---|---|:---:|:---:|---|
| `wipid` | `integer` | NOT NULL | **PK** | `` |
| `requestid` | `integer` | NOT NULL |  | `` |
| `ingrid` | `integer` | NULL |  | `` |
| `productid` | `integer` | NULL |  | `` |
| `quantity` | `numeric` | NULL |  | `0` |
| `lastupdated` | `timestamp without time zone` | NULL |  | `CURRENT_TIMESTAMP` |
