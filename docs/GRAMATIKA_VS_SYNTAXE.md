# Gramatika vs Syntaxe - Vysvětlení

## 1. Základní rozdíl

### Syntaxe (Syntax)
**Syntaxe = jak to vypadá, jak to píšeme**

Syntaxe je **konkrétní zápis** v souboru. Je to to, co vidíte a píšete.

**Příklad syntaxe:**
```singularis
@model Order {
    props {
        Id: guid, key, auto;
    }
}
```

### Gramatika (Grammar)
**Gramatika = formální pravidla, co je povolené**

Gramatika je **formální specifikace** (obvykle v EBNF), která říká parseru, co je validní a co ne. Je to "recept" pro parser.

**Příklad gramatiky (EBNF):**
```ebnf
domain_rule := "@" domain_name IDENT block
block := "{" { statement | declaration } "}"
declaration := IDENT ":" value ";"
```

---

## 2. Vztah mezi gramatikou a syntaxí

```
GRAMATIKA (EBNF)          →    PARSER    →    SYNTAXE (soubor)
"Co je povolené"          →    "Kontrola" →    "Jak to píšeme"
Formální pravidla         →    Validace   →    Konkrétní zápis
```

**Příklad:**
- **Gramatika říká**: `declaration := IDENT ":" value ";"`
- **Parser kontroluje**: Je to `identifikátor : hodnota ;`?
- **Syntaxe je**: `Id: guid, key, auto;`

---

## 3. Původní gramatika (před sjednocením)

### Problém: Různé syntaxe = různé gramatické pravidla

**Příklad 1: @model**
```singularis
@model Order {
    props {
        Id: guid, key, auto;  // ← constraints jako seznam
    }
    relations {
        Customer -> Customer, many-to-one;  // ← šipka pro vztah
    }
}
```

**Gramatika pro @model:**
```ebnf
model_rule := "@model" IDENT block
model_block := props_block | relations_block | indexes_block
props_block := "props" "{" { prop_decl } "}"
prop_decl := IDENT ":" type_ref "," constraint_list
constraint_list := constraint { "," constraint }
```

**Příklad 2: @workflow**
```singularis
@workflow OrderApproval {
    flow {
        start -> CheckAmount;  // ← šipka pro přechod
        CheckAmount: gateway(exclusive) {  // ← uzel s dvojtečkou
            when (Amount < 10000) -> AutoApprove;
        }
    }
}
```

**Gramatika pro @workflow:**
```ebnf
workflow_rule := "@workflow" IDENT block
workflow_block := flow_block
flow_block := "flow" "{" { flow_node | flow_transition } "}"
flow_node := IDENT ":" node_type "(" options ")" block
flow_transition := IDENT "->" IDENT
                 | "when" "(" expr ")" "->" IDENT
```

**Příklad 3: @api**
```singularis
@api OrderAPI {
    endpoints {
        GET / {  // ← HTTP metoda jako selektor
            operation: list;
        }
    }
}
```

**Gramatika pro @api:**
```ebnf
api_rule := "@api" IDENT block
api_block := endpoints_block
endpoints_block := "endpoints" "{" { endpoint_decl } "}"
endpoint_decl := HTTP_METHOD path block
HTTP_METHOD := "GET" | "POST" | "PUT" | "DELETE"
```

### Problém: Každá doména má jiná gramatická pravidla!

- `@model` používá: `IDENT ":" type "," constraints`
- `@workflow` používá: `IDENT ":" type "(" options ")" block` a `->`
- `@api` používá: `HTTP_METHOD path block`

**Parser musí mít 12 různých sad pravidel!**

---

## 4. Sjednocená gramatika (po sjednocení)

### Řešení: Všechny domény používají stejná základní pravidla

**Klíčová změna:** Všechny domény používají **stejnou strukturu bloků a deklarací**

### Sjednocená gramatika:

```ebnf
// === ZÁKLADNÍ STRUKTURA (stejná pro všechny domény) ===
domain_rule := "@" domain_name IDENT block
domain_name := "model" | "ui" | "workflow" | "security" | "api"
               | "report" | "validation" | "locale" | "audit"
               | "notify" | "visual" | "import"

// === BLOKY (stejné pro všechny domény) ===
block := "{" { statement | declaration | section } "}"

// === DEKLARACE (stejné pro všechny domény) ===
declaration := IDENT ":" value ";"
              | IDENT block                    // nested block
              | IDENT ":" value block         // value with nested

// === SEKCE (stejné pro všechny domény) ===
section := section_name block
section_name := IDENT

// === HODNOTY (stejné pro všechny domény) ===
value := string | number | boolean | null
       | array | object | var_func
       | type_ref | expression
```

### Co je sjednocené:

#### 1. **Struktura bloků** - stejná pro všechny
```ebnf
block := "{" { statement | declaration | section } "}"
```
- Všechny domény používají `{ ... }` pro bloky
- Všechny domény mohou mít `meta { ... }`, `config { ... }`
- Všechny domény mají doménově specifické sekce

#### 2. **Formát deklarací** - stejný pro všechny
```ebnf
declaration := IDENT ":" value ";"
              | IDENT block
              | IDENT ":" value block
```
- Všechny domény používají `key: value;`
- Všechny domény mohou mít `key { nested }`
- Všechny domény mohou mít `key: value { nested }`

#### 3. **Typový systém** - stejný pro všechny
```ebnf
type_ref := IDENT [ "(" type_params ")" ]
value := string | number | boolean | null | array | object
```
- Všechny domény používají stejný formát typů
- Všechny domény používají stejné hodnoty

#### 4. **Výrazy** - stejné pro všechny
```ebnf
expression := log_or
log_or := log_and { "||" log_and }
// ... stejná precedence pro všechny domény
```
- Všechny domény používají stejné operátory
- Všechná domény mají stejnou precedenci

---

## 5. Konkrétní příklady sjednocení

### Před sjednocením:

**@model:**
```singularis
@model Order {
    props {
        Id: guid, key, auto;  // ← speciální syntaxe pro constraints
    }
}
```
**Gramatika:** `prop_decl := IDENT ":" type "," constraint_list`

**@workflow:**
```singularis
@workflow OrderApproval {
    flow {
        start -> CheckAmount;  // ← speciální syntaxe pro přechody
    }
}
```
**Gramatika:** `flow_transition := IDENT "->" IDENT`

**@api:**
```singularis
@api OrderAPI {
    endpoints {
        GET / {  // ← HTTP metoda jako selektor
        }
    }
}
```
**Gramatika:** `endpoint_decl := HTTP_METHOD path block`

### Po sjednocení:

**@model:**
```singularis
@model Order {
    meta { }  // ← stejná struktura
    props {
        Id: guid, key, auto;  // ← stejný formát deklarace
    }
}
```
**Gramatika:** `declaration := IDENT ":" value ";"` (stejné jako všude)

**@workflow:**
```singularis
@workflow OrderApproval {
    meta { }  // ← stejná struktura
    flow {
        node start {  // ← jednotný formát
            -> CheckAmount;  // ← speciální syntaxe zůstává, ale v rámci bloku
        }
    }
}
```
**Gramatika:** `section := section_name block` (stejné jako všude)
- Speciální syntaxe `->` je uvnitř `flow` sekce, ale parser používá stejná základní pravidla

**@api:**
```singularis
@api OrderAPI {
    meta { }  // ← stejná struktura
    endpoints {
        endpoint GET / {  // ← jednotný formát
        }
    }
}
```
**Gramatika:** `section := section_name block` (stejné jako všude)
- `endpoint` je deklarace uvnitř `endpoints` sekce

---

## 6. Co zůstává specifické (ale v rámci jednotného rámce)

### Speciální syntaxe uvnitř sekcí

Některé domény mají **speciální syntaxe**, ale jsou **uvnitř jednotných sekcí**:

#### @workflow - flow syntaxe
```singularis
flow {
    node start { -> CheckAmount; }  // ← speciální, ale v rámci "flow" sekce
}
```
**Gramatika:**
```ebnf
section := section_name block  // ← jednotné
// Ale uvnitř "flow" sekce:
flow_content := { flow_node | flow_transition }
flow_transition := "->" IDENT  // ← specifické pro flow
```

#### @api - endpoint syntaxe
```singularis
endpoints {
    endpoint GET / { }  // ← speciální, ale v rámci "endpoints" sekce
}
```
**Gramatika:**
```ebnf
section := section_name block  // ← jednotné
// Ale uvnitř "endpoints" sekce:
endpoint_decl := "endpoint" HTTP_METHOD path block  // ← specifické pro api
```

### Klíčový princip:

1. **Základní struktura** je stejná: `@domain Name { sections }`
2. **Formát sekcí** je stejný: `section_name { content }`
3. **Formát deklarací** je stejný: `key: value;` nebo `key { nested }`
4. **Speciální syntaxe** je uvnitř sekcí, ale parser používá stejná základní pravidla

---

## 7. Výhody sjednocené gramatiky

### Před:
```ebnf
// 12 různých sad pravidel
model_rule := "@model" IDENT model_specific_block
workflow_rule := "@workflow" IDENT workflow_specific_block
api_rule := "@api" IDENT api_specific_block
// ... atd.
```

### Po:
```ebnf
// 1 jednotná základní struktura
domain_rule := "@" domain_name IDENT block
block := "{" { statement | declaration | section } "}"
declaration := IDENT ":" value ";" | IDENT block
section := section_name block

// Speciální syntaxe jsou uvnitř sekcí
flow_section := "flow" "{" flow_content "}"  // ← specifické, ale v rámci jednotného rámce
endpoints_section := "endpoints" "{" endpoint_content "}"  // ← specifické, ale v rámci jednotného rámce
```

### Výhody:

1. **Jeden parser** místo 12 různých
2. **Stejná logika** pro všechny domény
3. **Snadné přidání** nové domény (přidá se jen do `domain_name`)
4. **Konzistentní chování** napříč doménami
5. **Lepší IDE support** - jednotná struktura = lepší autocomplete

---

## 8. Shrnutí

### Syntaxe (co píšeme):
```singularis
@model Order {
    props {
        Id: guid, key, auto;
    }
}
```

### Gramatika (co parser kontroluje):
```ebnf
domain_rule := "@" domain_name IDENT block
block := "{" { declaration | section } "}"
declaration := IDENT ":" value ";"
```

### Sjednocení znamená:

1. **Všechny domény** používají stejnou základní gramatiku
2. **Stejná struktura** bloků: `{ ... }`
3. **Stejný formát** deklarací: `key: value;` nebo `key { ... }`
4. **Stejný typový systém** a výrazy
5. **Speciální syntaxe** (jako `->` v workflow) je uvnitř sekcí, ale parser používá stejná základní pravidla

**Výsledek:** Parser má **jednu sadu základních pravidel** místo 12 různých sad!







