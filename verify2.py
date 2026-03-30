import sqlite3

conn = sqlite3.connect(r'c:\Users\1\source\repos\NewDiplom\class.vpp')
c = conn.cursor()

c.execute('PRAGMA integrity_check')
print('Integrity:', c.fetchone()[0])

c.execute("SELECT COUNT(*) FROM MODEL_ELEMENT WHERE DEFINITION LIKE '%:Operation {%'")
print('Operation entries:', c.fetchone()[0])

c.execute("SELECT COUNT(*) FROM MODEL_ELEMENT WHERE DEFINITION LIKE '%:Attribute {%'")
print('Attribute entries:', c.fetchone()[0])

# Check for remaining +/- in names
c.execute("SELECT NAME, SUBSTR(DEFINITION,1,200) FROM MODEL_ELEMENT WHERE DEFINITION LIKE '%:\"+%' OR DEFINITION LIKE '%:\"-%'")
rows = c.fetchall()
print(f'Classes with remaining +/- in names: {len(rows)}')
for name, defn in rows[:3]:
    d = defn.decode('utf-8') if isinstance(defn, bytes) else defn
    print(f'  {name}: {d[:100]}')

# Sample Operation entries
c.execute("SELECT NAME, DEFINITION FROM MODEL_ELEMENT WHERE NAME='IDocumentUserLogic'")
row = c.fetchone()
if row:
    name, defn = row
    d = defn.decode('utf-8') if isinstance(defn, bytes) else defn
    print(f'\n=== IDocumentUserLogic sample ===')
    print(d[:800])

conn.close()
