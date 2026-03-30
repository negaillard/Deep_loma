"""
Fix VP class.vpp:
  - Remove +/- prefix from all Attribute/Operation names
  - Change method entries from Attribute to Operation type
  - Fields stay as Attribute (VP shows - automatically)
  - Methods become Operation (VP shows + automatically)
"""
import sqlite3
import re
import shutil

SRC = r'c:\Users\1\source\repos\NewDiplom\class.vpp'
BAK = r'c:\Users\1\source\repos\NewDiplom\class.vpp.bak_fix'
TS = "1774550000000"

shutil.copy2(SRC, BAK)
print(f"Backup saved to {BAK}")

conn = sqlite3.connect(SRC)
c = conn.cursor()


def build_attr(attr_id, name, type_str):
    return (f'{{{attr_id}:"{name}":Attribute {{\n'
            f'\t\t_modelEditable=T;\n'
            f'\t\ttype_string="{type_str}";\n'
            f'\t\tpmLastModified="{TS}";\n'
            f'\t\tpmAuthor="1";\n'
            f'\t\tpmCreateDateTime="{TS}";\n'
            f'\t\t_modelViews=NULL;\n'
            f'\t\tlastModifiedTime={TS};\n'
            f'\t}}}}')


def build_op(op_id, name, return_type):
    return (f'{{{op_id}:"{name}":Operation {{\n'
            f'\t\t_modelEditable=T;\n'
            f'\t\treturnTypeString="{return_type}";\n'
            f'\t\tpmLastModified="{TS}";\n'
            f'\t\tpmAuthor="1";\n'
            f'\t\tpmCreateDateTime="{TS}";\n'
            f'\t\t_modelViews=NULL;\n'
            f'\t\tlastModifiedTime={TS};\n'
            f'\t}}}}')


def parse_return_type(raw_name):
    """Extract method signature and return type from '+MethodName(params): ReturnType'"""
    # Find last '): ' which separates params from return type
    idx = raw_name.rfind('): ')
    if idx >= 0:
        method_sig = raw_name[:idx + 1]  # includes closing )
        return_type = raw_name[idx + 3:]
        return method_sig.strip(), return_type.strip()
    # No return type separator found
    return raw_name.strip(), ''


# Match child entries: {<ID>:"[+|-]<name>":Attribute { <props> }}
# Note: [^}]* matches everything except } (including newlines)
ENTRY_PATTERN = re.compile(
    r'\{(nmat\w+):"([+\-])([^"]*)":(Attribute)\s*\{([^}]*)\}\}',
    re.DOTALL
)


def fix_entry(match):
    entry_id = match.group(1)   # e.g. nmat000000000016
    # prefix = match.group(2)   # + or -  (not needed)
    raw_name = match.group(3)   # name without the prefix
    props_block = match.group(5)  # everything inside { }

    # Extract type_string if present (for fields)
    ts_match = re.search(r'type_string="([^"]*)"', props_block)
    type_str = ts_match.group(1) if ts_match else ''

    # Decide: method (has parentheses) or field
    if '(' in raw_name:
        method_sig, return_type = parse_return_type(raw_name)
        return build_op(entry_id, method_sig, return_type)
    else:
        return build_attr(entry_id, raw_name, type_str)


# Process all MODEL_ELEMENT rows that have Child blocks with +/- entries
c.execute("SELECT ID, NAME, DEFINITION FROM MODEL_ELEMENT WHERE DEFINITION LIKE '%Child=%'")
rows = c.fetchall()

updated_classes = []
for elem_id, name, defn in rows:
    d = defn.decode('utf-8') if isinstance(defn, bytes) else defn

    # Skip if no +/- prefixed entries
    if not re.search(r'"[+\-]', d):
        continue

    new_d = ENTRY_PATTERN.sub(fix_entry, d)

    if new_d != d:
        c.execute(
            "UPDATE MODEL_ELEMENT SET DEFINITION=? WHERE ID=?",
            (sqlite3.Binary(new_d.encode('utf-8')), elem_id)
        )
        updated_classes.append(name)

conn.commit()

print(f"\nFixed {len(updated_classes)} classes:")
for n in sorted(updated_classes):
    print(f"  - {n}")

# Verify a sample
c.execute("SELECT NAME, DEFINITION FROM MODEL_ELEMENT WHERE NAME='DocumentsController'")
row = c.fetchone()
if row:
    name2, defn2 = row
    d2 = defn2.decode('utf-8') if isinstance(defn2, bytes) else defn2
    print(f"\n=== Sample: {name2} (first 2000 chars) ===")
    print(d2[:2000])

c.execute("SELECT NAME, DEFINITION FROM MODEL_ELEMENT WHERE NAME='IDocumentLogic'")
row = c.fetchone()
if row:
    name2, defn2 = row
    d2 = defn2.decode('utf-8') if isinstance(defn2, bytes) else defn2
    print(f"\n=== Sample: {name2} (first 1500 chars) ===")
    print(d2[:1500])

# Final check: are there any remaining +/- in names?
c.execute("SELECT COUNT(*) FROM MODEL_ELEMENT WHERE DEFINITION LIKE '%:\"-%' OR DEFINITION LIKE '%:\"+%'")
remaining = c.fetchone()[0]
print(f"\nRemaining entries with +/- in names: {remaining}")

conn.close()
print("\nDone.")
