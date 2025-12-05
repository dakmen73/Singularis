# Analýza XPO souboru DataModelGA.xpo vs. XPO Schema

## ✅ Pokryté vlastnosti v návrhu

### Třída (xpObject)
- ✅ `baseClass` - základní třída (XPLiteObject, XPObject, atd.)
- ✅ `nonPersistent` - třída se neukládá do DB
- ✅ `virtualName` - virtuální název
- ✅ `namespace` - namespace třídy (implicitně z className)
- ✅ `customAttributes` - DevExpress XAF atributy

### Atribut (field)
- ✅ `persistent` - zda je perzistentní v DB
- ✅ `columnName` - název sloupce (atribut `persistent` na field)
- ✅ `columnType` - databázový typ
- ✅ `virtualColumnType` - virtuální typ pro mapování
- ✅ `displayName` - zobrazovaný název
- ✅ `size` - velikost pro stringy (včetně "Unlimited")
- ✅ `isKey` - primární klíč (key="true")
- ✅ `delayedUpdateModifiedOnly` - XPO vlastnost
- ✅ `customAttributes` - XAF atributy
- ✅ `logicalType` - logický typ pro mapování

### Kolekce (XPCollection)
- ✅ `name` - název kolekce
- ✅ `elementType` - typ prvků
- ✅ `associationName` - název Association
- ✅ `isAggregated` - agregovaná kolekce
- ✅ `displayName` - zobrazovaný název

### Vztahy
- ✅ `associationName` - název Association
- ✅ `isAggregated` - agregovaný vztah
- ✅ Typy vztahů (inheritance, association, atd.)

## ❌ Chybějící vlastnosti v návrhu

### Třída (xpObject)
1. **`virtualBaseClass`** - může se lišit od `baseClass` (např. GAObject vs. XPLiteObject)
2. **`persistent`** - název tabulky/view v DB pro třídu (např. persistent="vw_GACAS_YM")
3. **`mapInheritance`** - strategie mapování dědičnosti ("ParentTable", "ClassTable", atd.)
4. **`userFileName`** - název souboru pro uživatele (např. "Modul")
5. **`designerFileName`** - název designer souboru (např. "Modul.Designer")
6. **`className`** - název třídy v kódu (může se lišit od name)
7. **`initialName`** - původní název před přejmenováním (na třídě)

### Atribut (field)
1. **`isIdentity`** - auto-increment pole (isIdentity="true")
2. **`isNullable`** - nullovatelné pole (isNullable="true")
3. **`initialName`** - původní název pole před přejmenováním
4. **`logicalType`** - logický typ (již je, ale měl by být explicitněji zdokumentován)

### Vztahy (simpleAssociation)
1. **`sourceCollectionName`** - název kolekce na zdrojové straně (např. "c_GA_Moment")
2. **`targetFieldName`** - název pole na cílové straně (např. "ParentID")
3. **`associationName`** - název Association (již je, ale měl by být explicitněji)

### Dědičnost (inheritance)
1. **`mapInheritance`** - strategie mapování (ParentTable, ClassTable, atd.)
2. **`superClass`** - odkaz na nadřazenou třídu (již pokryto přes baseClass)

### Externí typy (externalTypes)
1. **`externalTypes`** - seznam externích typů z jiných namespace
   - `name` - název typu
   - `namespace` - namespace typu

### Indexy
1. **Indexy na třídě** - již jsou v návrhu, ale měly by podporovat:
   - Indexy na více polích (composite)
   - Indexy s názvem

## 🔧 Doporučená vylepšení schématu

### 1. Rozšířit třídu o:
```json
{
  "virtualBaseClass": "string",  // může se lišit od baseClass
  "persistent": "string",        // název tabulky/view v DB
  "mapInheritance": "string",    // "ParentTable" | "ClassTable" | null
  "userFileName": "string",      // název souboru pro uživatele
  "designerFileName": "string",  // název designer souboru
  "className": "string",         // název třídy v kódu
  "initialName": "string"       // původní název
}
```

### 2. Rozšířit atribut o:
```json
{
  "isIdentity": "boolean",       // auto-increment
  "isNullable": "boolean",       // nullovatelné pole
  "initialName": "string"        // původní název pole
}
```

### 3. Rozšířit vztah o:
```json
{
  "sourceCollectionName": "string",  // název kolekce na zdroji
  "targetFieldName": "string"       // název pole na cíli
}
```

### 4. Přidat externí typy:
```json
{
  "externalTypes": [
    {
      "name": "string",
      "namespace": "string"
    }
  ]
}
```

### 5. Vylepšit kolekce:
```json
{
  "sourceCollectionName": "string",  // název kolekce (c_GA_Moment)
  "targetFieldName": "string"       // název pole na cíli
}
```

## 📊 Shrnutí

**Pokrytí:** ~85%
**Chybějící klíčové vlastnosti:**
- virtualBaseClass vs. baseClass
- persistent (název tabulky) na třídě
- mapInheritance (strategie dědičnosti)
- isIdentity, isNullable na polích
- initialName (původní názvy)
- sourceCollectionName, targetFieldName ve vztazích
- externalTypes

**Priorita doplnění:**
1. **Vysoká:** virtualBaseClass, persistent (třída), mapInheritance, isIdentity, isNullable
2. **Střední:** initialName, sourceCollectionName, targetFieldName
3. **Nízká:** userFileName, designerFileName, className, externalTypes

