# Příklad: Order a OrderDetail s UI

## 1. Definice entit (@model)

### 1.1 Order Entity

```singularis
@model Order {
    meta {
        table: "Orders";
        schema: "sales";
        caption: "Objednávka";
        icon: "shopping-cart";
        auditable: true;
    }

    props {
        Id: guid, key, auto;
        OrderNumber: string(50), required, unique;
        OrderDate: datetime, required, default(now());
        DeliveryDate: datetime;
        Amount: decimal(18,2), range(0, 999999);
        Status: OrderStatus, required, default(Draft);
        Notes: string(1000);
        
        // Computed properties
        Total: decimal(18,2), computed(sum(Lines, Amount));
        LineCount: int, computed(count(Lines));
    }

    relations {
        Customer -> Customer, many-to-one, required;
        Lines -> OrderLine[], one-to-many, cascade-delete;
        CreatedBy -> User, many-to-one;
    }

    indexes {
        idx_order_number: [OrderNumber], unique;
        idx_customer_date: [Customer, OrderDate];
        idx_status: [Status];
    }
}
```

### 1.2 OrderLine Entity (OrderDetail)

```singularis
@model OrderLine {
    meta {
        table: "OrderLines";
        schema: "sales";
        caption: "Řádek objednávky";
        icon: "list";
    }

    props {
        Id: guid, key, auto;
        LineNumber: int, required;
        Quantity: decimal(10,2), required, range(0.01, 9999);
        UnitPrice: decimal(18,2), required, range(0, 999999);
        Amount: decimal(18,2), computed(Quantity * UnitPrice);
        Description: string(500);
        ProductCode: string(50);
    }

    relations {
        Order -> Order, many-to-one, required, cascade-delete;
        Product -> Product, many-to-one;
    }

    indexes {
        idx_order_line: [Order, LineNumber], unique;
    }
}
```

### 1.3 Enum pro Status

```singularis
@model OrderStatus {
    meta {
        type: enum;
    }

    values {
        Draft: "Návrh";
        Submitted: "Odesláno";
        Approved: "Schváleno";
        Rejected: "Zamítnuto";
        InProgress: "Zpracovává se";
        Completed: "Dokončeno";
        Cancelled: "Zrušeno";
    }
}
```

---

## 2. UI definice (@ui)

### 2.1 OrderList - List View s řádky

```singularis
@ui OrderList {
    meta {
        type: list-view;
        caption: "Seznam objednávek";
        route: "/orders";
    }

    config {
        model: Order;
        page-size: 20;
        selectable: multi;
        default-sort: OrderDate, desc;
    }

    layout {
        toolbar {
            button New {
                action: create;
                icon: "plus";
                style: primary;
                label: "Nová objednávka";
            }
            
            button Export {
                action: export(xlsx);
                icon: "download";
                label: "Exportovat";
            }
            
            separator;
            
            search {
                fields: [OrderNumber, Customer.Name, Customer.Email];
                placeholder: "Hledat objednávky...";
            }
            
            filter Status {
                field: Status;
                type: multi-select;
                label: "Stav";
            }
            
            filter DateRange {
                field: OrderDate;
                type: date-range;
                label: "Datum";
            }
        }

        grid {
            selectable: multi;
            row-click: navigate-detail;
            
            columns {
                OrderNumber {
                    width: 150;
                    sortable: true;
                    link: detail;
                    frozen: true;
                    label: "Číslo";
                }
                
                OrderDate {
                    width: 120;
                    format: date;
                    sortable: true;
                    label: "Datum";
                }
                
                Customer {
                    width: 200;
                    field: Customer.Name;
                    sortable: true;
                    label: "Zákazník";
                }
                
                CustomerEmail {
                    width: 180;
                    field: Customer.Email;
                    label: "Email";
                }
                
                Amount {
                    width: 120;
                    format: currency;
                    align: right;
                    sortable: true;
                    label: "Částka";
                }
                
                Total {
                    width: 120;
                    format: currency;
                    align: right;
                    sortable: true;
                    label: "Celkem";
                    computed: true;
                }
                
                LineCount {
                    width: 80;
                    align: center;
                    label: "Řádků";
                }
                
                Status {
                    width: 120;
                    display: badge;
                    sortable: true;
                    label: "Stav";
                    colors {
                        Draft: gray;
                        Submitted: blue;
                        Approved: green;
                        Rejected: red;
                        InProgress: orange;
                        Completed: darkgreen;
                        Cancelled: darkred;
                    }
                }
            }

            row-actions {
                action View {
                    icon: "eye";
                    action: navigate-detail;
                    label: "Zobrazit";
                }
                
                action Edit {
                    icon: "edit";
                    action: edit;
                    label: "Upravit";
                    when: Status == Draft || Status == Submitted;
                }
                
                action Delete {
                    icon: "trash";
                    action: delete;
                    confirm: true;
                    label: "Smazat";
                    when: Status == Draft;
                }
            }
        }
    }
}
```

### 2.2 OrderDetail - Detail View s properties

```singularis
@ui OrderDetail {
    meta {
        type: detail-view;
        caption: "Detail objednávky";
        route: "/orders/{id}";
    }

    config {
        model: Order;
        mode: view;  // view | edit | create
    }

    layout {
        header {
            title: "Objednávka ${OrderNumber}";
            subtitle: "Zákazník: ${Customer.Name}";
            actions {
                button Edit {
                    icon: "edit";
                    action: edit;
                    when: Status == Draft || Status == Submitted;
                }
                button Approve {
                    icon: "check";
                    action: approve;
                    style: success;
                    when: Status == Submitted;
                }
                button Print {
                    icon: "print";
                    action: print;
                }
            }
        }

        tabs {
            tab General {
                label: "Obecné";
                icon: "info";
                
                sections {
                    section BasicInfo {
                        label: "Základní informace";
                        columns: 2;
                        
                        fields {
                            field OrderNumber {
                                label: "Číslo objednávky";
                                width: 1;
                                readonly: true;
                            }
                            
                            field OrderDate {
                                label: "Datum objednávky";
                                width: 1;
                                format: date;
                            }
                            
                            field DeliveryDate {
                                label: "Datum dodání";
                                width: 1;
                                format: date;
                            }
                            
                            field Status {
                                label: "Stav";
                                width: 1;
                                display: badge;
                                colors {
                                    Draft: gray;
                                    Submitted: blue;
                                    Approved: green;
                                    Rejected: red;
                                    InProgress: orange;
                                    Completed: darkgreen;
                                    Cancelled: darkred;
                                }
                            }
                        }
                    }
                    
                    section CustomerInfo {
                        label: "Zákazník";
                        columns: 2;
                        
                        fields {
                            field Customer {
                                label: "Zákazník";
                                width: 2;
                                display: link;
                                target: "/customers/${Customer.Id}";
                            }
                            
                            field CustomerEmail {
                                label: "Email";
                                field: Customer.Email;
                                width: 1;
                                display: email-link;
                            }
                            
                            field CustomerPhone {
                                label: "Telefon";
                                field: Customer.Phone;
                                width: 1;
                                display: phone-link;
                            }
                        }
                    }
                    
                    section FinancialInfo {
                        label: "Finanční údaje";
                        columns: 2;
                        
                        fields {
                            field Amount {
                                label: "Částka";
                                width: 1;
                                format: currency;
                                align: right;
                            }
                            
                            field Total {
                                label: "Celkem";
                                width: 1;
                                format: currency;
                                align: right;
                                style: bold;
                                computed: true;
                            }
                            
                            field LineCount {
                                label: "Počet řádků";
                                width: 1;
                                align: center;
                            }
                        }
                    }
                    
                    section Notes {
                        label: "Poznámky";
                        columns: 1;
                        
                        fields {
                            field Notes {
                                label: "Poznámky";
                                width: 1;
                                display: textarea;
                                rows: 4;
                            }
                        }
                    }
                }
            }
            
            tab Lines {
                label: "Řádky objednávky";
                icon: "list";
                
                sections {
                    section OrderLines {
                        label: "Řádky";
                        
                        grid {
                            data-source: Lines;
                            model: OrderLine;
                            
                            columns {
                                LineNumber {
                                    width: 60;
                                    label: "Ř.";
                                    align: center;
                                }
                                
                                ProductCode {
                                    width: 120;
                                    label: "Kód produktu";
                                }
                                
                                Description {
                                    width: 300;
                                    label: "Popis";
                                }
                                
                                Quantity {
                                    width: 100;
                                    label: "Množství";
                                    format: number;
                                    align: right;
                                }
                                
                                UnitPrice {
                                    width: 120;
                                    label: "Jednotková cena";
                                    format: currency;
                                    align: right;
                                }
                                
                                Amount {
                                    width: 120;
                                    label: "Částka";
                                    format: currency;
                                    align: right;
                                    computed: true;
                                }
                            }
                            
                            actions {
                                action AddLine {
                                    icon: "plus";
                                    action: add-line;
                                    label: "Přidat řádek";
                                }
                                
                                action EditLine {
                                    icon: "edit";
                                    action: edit-line;
                                    label: "Upravit";
                                }
                                
                                action DeleteLine {
                                    icon: "trash";
                                    action: delete-line;
                                    confirm: true;
                                    label: "Smazat";
                                }
                            }
                        }
                    }
                }
            }
            
            tab History {
                label: "Historie";
                icon: "clock";
                
                sections {
                    section AuditLog {
                        label: "Audit záznamy";
                        
                        timeline {
                            data-source: AuditLog;
                            fields {
                                Timestamp {
                                    label: "Čas";
                                    format: datetime;
                                }
                                
                                User {
                                    label: "Uživatel";
                                    field: User.Name;
                                }
                                
                                Action {
                                    label: "Akce";
                                }
                                
                                Changes {
                                    label: "Změny";
                                    display: diff;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
```

---

## 3. Kompletní příklad s propojením

```singularis
// === ROOT SCOPE ===
:root {
    --app-name: "OrderManagement";
    --namespace: "Company.OrderSystem";
}

// === MODELS ===
@model Order {
    meta {
        table: "Orders";
        caption: "Objednávka";
    }

    props {
        Id: guid, key, auto;
        OrderNumber: string(50), required, unique;
        OrderDate: datetime, required, default(now());
        Amount: decimal(18,2);
        Status: OrderStatus, required, default(Draft);
        Total: decimal(18,2), computed(sum(Lines, Amount));
    }

    relations {
        Customer -> Customer, many-to-one, required;
        Lines -> OrderLine[], one-to-many, cascade-delete;
    }
}

@model OrderLine {
    meta {
        table: "OrderLines";
        caption: "Řádek objednávky";
    }

    props {
        Id: guid, key, auto;
        LineNumber: int, required;
        Quantity: decimal(10,2), required;
        UnitPrice: decimal(18,2), required;
        Amount: decimal(18,2), computed(Quantity * UnitPrice);
        Description: string(500);
    }

    relations {
        Order -> Order, many-to-one, required, cascade-delete;
    }
}

// === UI ===
@ui OrderList {
    meta {
        type: list-view;
        route: "/orders";
    }

    config {
        model: Order;
        page-size: 20;
    }

    layout {
        grid {
            row-click: navigate-detail;
            
            columns {
                OrderNumber {
                    width: 150;
                    link: detail;
                }
                OrderDate { width: 120; format: date; }
                Customer { width: 200; field: Customer.Name; }
                Amount { width: 120; format: currency; }
                Status { width: 120; display: badge; }
            }
        }
    }
}

@ui OrderDetail {
    meta {
        type: detail-view;
        route: "/orders/{id}";
    }

    config {
        model: Order;
    }

    layout {
        tabs {
            tab General {
                sections {
                    section BasicInfo {
                        fields {
                            field OrderNumber { label: "Číslo"; }
                            field OrderDate { label: "Datum"; format: date; }
                            field Status { label: "Stav"; display: badge; }
                            field Customer { label: "Zákazník"; }
                            field Amount { label: "Částka"; format: currency; }
                            field Total { label: "Celkem"; format: currency; }
                        }
                    }
                }
            }
            
            tab Lines {
                sections {
                    section OrderLines {
                        grid {
                            data-source: Lines;
                            columns {
                                LineNumber { width: 60; }
                                Description { width: 300; }
                                Quantity { width: 100; format: number; }
                                UnitPrice { width: 120; format: currency; }
                                Amount { width: 120; format: currency; }
                            }
                        }
                    }
                }
            }
        }
    }
}
```

---

## 4. Klíčové prvky propojení

### 4.1 Navigace z List do Detail

```singularis
// V OrderList
grid {
    row-click: navigate-detail;  // Kliknutí na řádek
    columns {
        OrderNumber {
            link: detail;  // Link na detail
        }
    }
}
```

### 4.2 Zobrazení properties v Detail

```singularis
// V OrderDetail
sections {
    section BasicInfo {
        fields {
            field OrderNumber { label: "Číslo objednávky"; }
            field OrderDate { label: "Datum"; format: date; }
            field Customer { label: "Zákazník"; }
            // ... další properties
        }
    }
}
```

### 4.3 Zobrazení related entities (OrderLines)

```singularis
// V OrderDetail, tab Lines
grid {
    data-source: Lines;  // Vztah k OrderLine[]
    model: OrderLine;
    columns {
        // Properties z OrderLine
        LineNumber { ... }
        Description { ... }
        Quantity { ... }
    }
}
```

---

## 5. Výsledek

1. **OrderList** - zobrazí seznam objednávek s hlavními sloupci
2. **Kliknutí na řádek** - navigace na `/orders/{id}`
3. **OrderDetail** - zobrazí všechny properties Order v sekcích
4. **Tab Lines** - zobrazí related OrderLines v gridu
5. **Properties** - všechny fields z Order jsou zobrazeny s labels a formáty

---

## 6. Poznámky k implementaci

- `row-click: navigate-detail` - automaticky naviguje na detail view
- `link: detail` - vytvoří klikatelný link na detail
- `data-source: Lines` - načte related entities přes vztah
- `field: Customer.Name` - zobrazí nested property
- `format: currency` - formátování hodnot
- `display: badge` - vizuální zobrazení (např. pro Status)







