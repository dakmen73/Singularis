# Návrh sjednocené syntaxe pro Singularis Unified DSL

## 1. Základní principy sjednocení

### 1.1 Jednotná struktura domén
Všechny domény používají stejný základní vzor:
```singularis
@domain Name {
    meta { ... }           // Metadata (volitelné)
    config { ... }         // Konfigurace (volitelné)
    sections { ... }       // Doménově specifické sekce
}
```

### 1.2 Jednotný formát sekcí
Všechny sekce používají stejný formát:
- **Deklarace**: `key: value;` nebo `key { nested-content }`
- **Seznamy**: `item-name { ... }` nebo `item-name: value;`
- **Vztahy**: `name -> target, options;`
- **Akce**: `action-name { config }`

### 1.3 Typový systém
Jednotný formát pro všechny typy:
- Primitivní: `string(n)`, `int`, `decimal(p,s)`, `bool`, `datetime`, `guid`
- Složené: `Type[]`, `Type?`, `Type<Param>`
- Reference: `-> Type`, `-> Type[]`

---

## 2. Sjednocená syntaxe pro všech 12 domén

### 2.1 @model - Datový model

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
        OrderDate: datetime, default(now());
        Amount: decimal(18,2), range(0, 999999);
        Status: OrderStatus;
        Total: decimal(18,2), computed(sum(Lines, Amount));
    }

    relations {
        Customer -> Customer, many-to-one, required;
        Lines -> OrderLine[], one-to-many, cascade-delete;
    }

    indexes {
        idx_order_number: [OrderNumber], unique;
        idx_customer_date: [Customer, OrderDate];
    }
}
```

**Sekce**: `meta`, `props`, `relations`, `indexes`

---

### 2.2 @ui - Uživatelské rozhraní

```singularis
@ui OrderList {
    meta {
        type: list-view;
        caption: "Seznam objednávek";
    }

    config {
        model: Order;
        page-size: 20;
        selectable: multi;
    }

    layout {
        toolbar {
            button New {
                action: create;
                icon: "plus";
                style: primary;
            }
            button Export {
                action: export(xlsx);
                icon: "download";
            }
            separator;
            search {
                fields: [OrderNumber, Customer.Name];
            }
            filter Status {
                field: Status;
                multi: true;
            }
        }

        grid {
            columns {
                OrderNumber {
                    width: 150;
                    sortable: true;
                    link: detail;
                    frozen: true;
                }
                OrderDate {
                    width: 120;
                    format: date;
                }
                Amount {
                    width: 120;
                    format: currency;
                    align: right;
                }
            }

            row-actions {
                action Edit {
                    icon: "edit";
                    action: edit;
                }
                action Delete {
                    icon: "trash";
                    action: delete;
                    confirm: true;
                }
            }
        }
    }
}
```

**Sekce**: `meta`, `config`, `layout`

---

### 2.3 @workflow - Workflow procesy

```singularis
@workflow OrderApproval {
    meta {
        caption: "Schvalování objednávek";
    }

    config {
        entity: Order;
    }

    trigger {
        on: Order;
        event: on-change(Status);
        when: Status == Submitted;
    }

    participants {
        requester: role(Sales);
        approver: role(Manager);
        director: role(Director);
    }

    flow {
        node start {
            -> CheckAmount;
        }

        node CheckAmount: gateway(exclusive) {
            when Amount < 10000 -> AutoApprove;
            when Amount < 50000 -> ManagerApproval;
            else -> DirectorApproval;
        }

        node AutoApprove: task(automatic) {
            actions {
                set(Status = Approved);
                set(ApprovedAt = now());
            }
            -> SendConfirmation;
        }

        node ManagerApproval: task(user) {
            assignee: approver;
            form: ApprovalForm;
            timeout: 48h;
            on-timeout -> Escalate;
            outcomes {
                approve -> SendConfirmation;
                reject -> NotifyRejection;
            }
        }

        node SendConfirmation: task(automatic) {
            actions {
                notify(requester, template: OrderApproved);
            }
            -> end;
        }
    }
}
```

**Sekce**: `meta`, `config`, `trigger`, `participants`, `flow`
**Flow syntaxe**: `node Name: type(options) { ... }` a `-> target`

---

### 2.4 @api - API definice

```singularis
@api OrderAPI {
    meta {
        caption: "Order Management API";
    }

    config {
        base-path: "/api/v1/orders";
        auth: bearer;
        version: "1.0";
    }

    endpoints {
        endpoint GET / {
            operation: list;
            query: [page, limit, status];
            returns: Order[];
            cache: 60s;
        }

        endpoint GET /{id} {
            operation: get;
            returns: Order;
            cache: 30s;
        }

        endpoint POST / {
            operation: create;
            body: Order;
            returns: Order;
            validate: CreateOrderValidator;
        }

        endpoint PUT /{id} {
            operation: update;
            body: Order;
            returns: Order;
        }

        endpoint DELETE /{id} {
            operation: delete;
            returns: void;
        }
    }
}
```

**Sekce**: `meta`, `config`, `endpoints`
**Endpoint syntaxe**: `endpoint METHOD path { ... }`

---

### 2.5 @security - Bezpečnost

```singularis
@security OrderSecurity {
    meta {
        caption: "Order Security Rules";
    }

    config {
        entity: Order;
    }

    roles {
        role Sales {
            permissions {
                create: true;
                read: own;
                update: own;
                delete: false;
            }
        }

        role Manager {
            permissions {
                create: true;
                read: department;
                update: department;
                delete: false;
            }
        }

        role Admin {
            permissions {
                create: true;
                read: all;
                update: all;
                delete: true;
            }
        }
    }

    rules {
        rule CanViewAmount {
            when: role == Sales;
            condition: Status != Draft;
            allow: read(Amount);
        }

        rule CanApprove {
            when: role == Manager;
            condition: Amount < 50000;
            allow: approve;
        }
    }
}
```

**Sekce**: `meta`, `config`, `roles`, `rules`

---

### 2.6 @validation - Validace

```singularis
@validation OrderValidation {
    meta {
        caption: "Order Validation Rules";
    }

    config {
        entity: Order;
    }

    rules {
        rule OrderNumberUnique {
            field: OrderNumber;
            type: unique;
            message: "Order number must be unique";
        }

        rule AmountPositive {
            field: Amount;
            type: range;
            min: 0;
            message: "Amount must be positive";
        }

        rule CustomerRequired {
            field: Customer;
            type: required;
            message: "Customer is required";
        }

        rule TotalMatchesLines {
            type: custom;
            expression: Total == sum(Lines, Amount);
            message: "Total must match sum of line amounts";
        }
    }
}
```

**Sekce**: `meta`, `config`, `rules`

---

### 2.7 @report - Reporty

```singularis
@report OrderSummary {
    meta {
        caption: "Order Summary Report";
        type: table;
    }

    config {
        data-source: Order;
        group-by: [Customer, Status];
    }

    layout {
        header {
            title: "Order Summary";
            date-range: true;
        }

        columns {
            Customer {
                field: Customer.Name;
                width: 200;
            }
            Status {
                field: Status;
                width: 100;
            }
            Count {
                field: count(Id);
                width: 80;
                aggregate: sum;
            }
            Total {
                field: sum(Amount);
                width: 120;
                format: currency;
            }
        }

        footer {
            totals: [Count, Total];
        }
    }

    filters {
        filter DateRange {
            field: OrderDate;
            type: date-range;
        }
        filter Status {
            field: Status;
            type: multi-select;
        }
    }
}
```

**Sekce**: `meta`, `config`, `layout`, `filters`

---

### 2.8 @locale - Lokalizace

```singularis
@locale OrderLocale {
    meta {
        caption: "Order Localization";
        entity: Order;
    }

    config {
        default-locale: "cs-CZ";
        supported: ["cs-CZ", "en-US", "de-DE"];
    }

    translations {
        locale cs-CZ {
            Order: "Objednávka";
            OrderNumber: "Číslo objednávky";
            OrderDate: "Datum objednávky";
            Amount: "Částka";
            Status: "Stav";
            Status.Draft: "Návrh";
            Status.Submitted: "Odesláno";
            Status.Approved: "Schváleno";
        }

        locale en-US {
            Order: "Order";
            OrderNumber: "Order Number";
            OrderDate: "Order Date";
            Amount: "Amount";
            Status: "Status";
            Status.Draft: "Draft";
            Status.Submitted: "Submitted";
            Status.Approved: "Approved";
        }
    }
}
```

**Sekce**: `meta`, `config`, `translations`

---

### 2.9 @audit - Auditování

```singularis
@audit OrderAudit {
    meta {
        caption: "Order Audit Configuration";
    }

    config {
        entity: Order;
        enabled: true;
    }

    tracked {
        field OrderNumber {
            on: [create, update];
            log-old-value: true;
        }

        field Amount {
            on: [create, update];
            log-old-value: true;
            threshold: 1000;
        }

        field Status {
            on: [create, update];
            log-old-value: true;
            log-new-value: true;
        }
    }

    rules {
        rule LogAllChanges {
            when: true;
            action: log;
            level: info;
        }

        rule AlertOnAmountChange {
            when: Amount changed and old(Amount) != new(Amount);
            action: notify(Admin);
            level: warning;
        }
    }
}
```

**Sekce**: `meta`, `config`, `tracked`, `rules`

---

### 2.10 @notify - Notifikace

```singularis
@notify OrderNotifications {
    meta {
        caption: "Order Notifications";
    }

    config {
        entity: Order;
    }

    templates {
        template OrderCreated {
            subject: "Order ${OrderNumber} created";
            body: "Order ${OrderNumber} for ${Customer.Name} has been created.";
            channels: [email, sms];
        }

        template OrderApproved {
            subject: "Order ${OrderNumber} approved";
            body: "Your order ${OrderNumber} has been approved.";
            channels: [email];
        }

        template OrderRejected {
            subject: "Order ${OrderNumber} rejected";
            body: "Your order ${OrderNumber} has been rejected. Reason: ${Reason}";
            channels: [email, sms];
        }
    }

    rules {
        rule NotifyOnCreate {
            when: Order created;
            template: OrderCreated;
            recipients: [Customer.Email];
        }

        rule NotifyOnApprove {
            when: Status == Approved;
            template: OrderApproved;
            recipients: [Customer.Email, Sales.Email];
        }
    }
}
```

**Sekce**: `meta`, `config`, `templates`, `rules`

---

### 2.11 @import - Import/Export

```singularis
@import OrderImport {
    meta {
        caption: "Order Import Configuration";
    }

    config {
        entity: Order;
    }

    sources {
        source Excel {
            type: excel;
            format: xlsx;
            mapping {
                column "Order Number" -> OrderNumber;
                column "Date" -> OrderDate;
                column "Amount" -> Amount;
                column "Customer" -> Customer.Name;
            }
        }

        source CSV {
            type: csv;
            delimiter: ",";
            encoding: utf-8;
            mapping {
                column 0 -> OrderNumber;
                column 1 -> OrderDate;
                column 2 -> Amount;
            }
        }

        source API {
            type: api;
            endpoint: "https://external-api.com/orders";
            auth: bearer;
            mapping {
                field "order_number" -> OrderNumber;
                field "date" -> OrderDate;
            }
        }
    }

    rules {
        rule ValidateOnImport {
            validate: OrderValidation;
            on-error: skip;
        }

        rule TransformAmount {
            expression: Amount = Amount * 1.21; // Add VAT
        }
    }
}
```

**Sekce**: `meta`, `config`, `sources`, `rules`

---

### 2.12 @visual - Vizualizace

```singularis
@visual OrderDashboard {
    meta {
        caption: "Order Dashboard";
        type: dashboard;
    }

    config {
        layout: grid;
        columns: 3;
    }

    widgets {
        widget OrdersByStatus {
            type: pie-chart;
            data-source: Order;
            group-by: Status;
            size: [2, 1];
        }

        widget OrdersByMonth {
            type: line-chart;
            data-source: Order;
            x-axis: OrderDate;
            y-axis: count(Id);
            size: [3, 2];
        }

        widget TopCustomers {
            type: bar-chart;
            data-source: Order;
            group-by: Customer;
            aggregate: sum(Amount);
            limit: 10;
            size: [2, 2];
        }

        widget RecentOrders {
            type: table;
            data-source: Order;
            columns: [OrderNumber, OrderDate, Amount, Status];
            limit: 10;
            size: [3, 2];
        }
    }

    filters {
        filter DateRange {
            field: OrderDate;
            type: date-range;
            apply-to: all;
        }
    }
}
```

**Sekce**: `meta`, `config`, `widgets`, `filters`

---

## 3. Sjednocená EBNF gramatika

```ebnf
// === ROOT ===
stylesheet    := { statement | comment }

statement     := domain_rule | root_rule | at_rule | path_rule | file_rule

// === ROOT SCOPE ===
root_rule     := ":root" block

// === DOMAIN RULES ===
domain_rule   := "@" domain_name IDENT [extends_clause] block
domain_name   := "model" | "ui" | "workflow" | "security" | "api"
               | "report" | "validation" | "locale" | "audit"
               | "notify" | "visual" | "import"
extends_clause := "extends" IDENT { "," IDENT }

// === BLOCKS ===
block         := "{" { statement | declaration | section } "}"

section       := section_name block
section_name  := IDENT

// === DECLARATIONS ===
declaration   := IDENT ":" value ";"
              | IDENT block                    // nested block
              | IDENT ":" value block         // value with nested content

// === VALUES ===
value         := string | number | boolean | null
               | array | object | var_func
               | type_ref | expression

type_ref      := IDENT [ "(" type_params ")" ]
type_params   := value { "," value }

// === EXPRESSIONS ===
expression    := log_or
log_or        := log_and { "||" log_and }
log_and       := equality { "&&" equality }
equality      := relational { ("==" | "!=") relational }
relational    := additive { ("<" | "<=" | ">" | ">=") additive }
additive      := multiplicative { ("+" | "-") multiplicative }
multiplicative:= unary { ("*" | "/" | "%") unary }
unary         := ("!" | "-") unary | primary

primary       := NUMBER | string | boolean | null
               | var_func | array | object
               | member_access | "(" expression ")"

var_func      := "var" "(" "--" IDENT ")"
member_access := IDENT { "." IDENT } [ "(" arg_list ")" ]

// === RELATIONS ===
relation      := IDENT "->" type_ref "," relation_options
relation_options := IDENT { "," IDENT }

// === FLOW SYNTAX (for @workflow) ===
flow_node     := "node" IDENT [ ":" node_type [ "(" node_options ")" ] ] block
node_type     := "task" | "gateway" | "start" | "end"
node_options  := IDENT { "," IDENT }
flow_transition := "->" IDENT
                 | "when" expression "->" IDENT
                 | "else" "->" IDENT

// === ENDPOINT SYNTAX (for @api) ===
endpoint      := "endpoint" HTTP_METHOD path block
HTTP_METHOD   := "GET" | "POST" | "PUT" | "DELETE" | "PATCH"
path          := "/" { path_segment }
path_segment  := IDENT | "{" IDENT "}"

// === LITERALS ===
string        := QUOTED_STRING | interpolated_string
interpolated_string := '"' { STRING_CHAR | "${" expression "}" } '"'

array         := "[" [ value { "," value } ] "]"
object        := "{" [ key_value { "," key_value } ] "}"
key_value     := IDENT ":" value

boolean       := "true" | "false"
null          := "null"

// === AT-RULES (SGL) ===
at_rule       := if_rule | foreach_rule | exec_rule
               | mixin_decl | mixin_include

if_rule       := "@if" "(" expression ")" block [ "@else" block ]
foreach_rule  := "@foreach" IDENT "in" expression block
exec_rule     := "@exec" block
mixin_decl    := "@mixin" IDENT [ "(" param_list ")" ] block
mixin_include := "@include" IDENT [ "(" arg_list ")" ] ";"

// === PATH & FILE RULES (SGL) ===
path_rule     := path_selector block
path_selector := "/" { path_segment } "/"
file_rule     := "file" string block

// === HELPERS ===
param_list    := IDENT { "," IDENT }
arg_list      := expression { "," expression }
comment       := "/*" ANY "*/" | "//" ANY_TO_EOL
```

---

## 4. Klíčové principy sjednocení

### 4.1 Jednotná struktura
Všechny domény používají:
- `meta { ... }` - metadata (volitelné)
- `config { ... }` - konfigurace (volitelné)
- Doménově specifické sekce

### 4.2 Jednotný formát deklarací
- `key: value;` - jednoduchá hodnota
- `key { ... }` - vnořený blok
- `key: value { ... }` - hodnota s vnořeným obsahem

### 4.3 Jednotný typový systém
- Všechny typy: `Type(params)`
- Reference: `-> Type` nebo `-> Type[]`
- Constraints: seznam za typem

### 4.4 Jednotná syntaxe pro seznamy
- `item-name { ... }` - blokový item
- `item-name: value;` - jednoduchý item
- `item-name: value { ... }` - item s hodnotou a blokem

### 4.5 Jednotná syntaxe pro vztahy
- `name -> target, options;` - pro všechny vztahy

### 4.6 Jednotná syntaxe pro akce
- `action-name { config }` - všechny akce

---

## 5. Výhody sjednocené syntaxe

1. **Snadné učení** - jeden vzor pro všechny domény
2. **Konzistentní parser** - stejná logika pro všechny domény
3. **Snadná rozšiřitelnost** - přidání nové domény = přidání nových sekcí
4. **Lepší IDE support** - jednotná struktura = lepší autocomplete
5. **Znovupoužitelnost** - stejné utility funkce pro všechny domény
6. **Snadnější refactoring** - jednotná struktura = snadnější transformace

---

## 6. Implementační poznámky

### 6.1 Parser struktura
```csharp
public interface IDomainParser
{
    DomainType Domain { get; }
    DomainBody Parse(Block block);
}

// Každá doména má svůj parser, ale všechny používají stejnou základní strukturu
public class ModelParser : IDomainParser
{
    public DomainBody Parse(Block block)
    {
        var meta = ExtractSection<MetaBlock>(block, "meta");
        var props = ExtractSection<PropsBlock>(block, "props");
        var relations = ExtractSection<RelationsBlock>(block, "relations");
        // ...
    }
}
```

### 6.2 AST struktura
```csharp
public record DomainRule(
    DomainType Domain,
    string Name,
    IReadOnlyDictionary<string, Section> Sections,
    string? Extends = null
) : Statement;

public abstract record Section;
public record MetaSection(IDictionary<string, Value> Properties) : Section;
public record ConfigSection(IDictionary<string, Value> Properties) : Section;
// Doménově specifické sekce dědí z Section
```

---

## 7. Migrace z původní syntaxe

Původní syntaxe zůstává kompatibilní, ale doporučuje se migrace na novou:

**Před:**
```singularis
@model Order {
    props {
        Id: guid, key, auto;
    }
}
```

**Po:**
```singularis
@model Order {
    meta { }  // explicitní, i když prázdné
    props {
        Id: guid, key, auto;
    }
}
```

Parser může podporovat obě varianty pro zpětnou kompatibilitu.

---

## 8. Závěr

Tento návrh sjednocuje syntaxi všech 12 domén pod jednotný vzor:
- **Stejná základní struktura** (`meta`, `config`, doménové sekce)
- **Jednotný formát deklarací** (`key: value;` nebo `key { ... }`)
- **Jednotný typový systém**
- **Jednotná syntaxe pro vztahy a akce**

Výsledkem je **konzistentní, předvídatelný a rozšiřitelný** DSL, který zachovává specifické potřeby každé domény, ale používá jednotné principy.







