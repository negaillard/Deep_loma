import sqlite3

# Check original VP-generated content in bak_000f
conn = sqlite3.connect(r'c:\Users\1\source\repos\NewDiplom\class.vpp.bak_000f')
c = conn.cursor()

# Look at IUserModel which should have been user-created with proper VP format
c.execute("SELECT NAME, DEFINITION FROM MODEL_ELEMENT WHERE NAME='IUserModel'")
rows = c.fetchall()
for name, defn in rows:
    if defn:
        d = defn.decode('utf-8') if isinstance(defn, bytes) else defn
        print(f"=== {name} (bak_000f) ===\n{d[:5000]}\n")

# Also look at what ALL model elements were in original file
c.execute("SELECT NAME, DEFINITION FROM MODEL_ELEMENT WHERE DEFINITION NOT LIKE '%nmcl%' AND DEFINITION LIKE '%Operation%' LIMIT 3")
rows = c.fetchall()
for name, defn in rows:
    if defn:
        d = defn.decode('utf-8') if isinstance(defn, bytes) else defn
        print(f"=== {name} (with Operation) ===\n{d[:2000]}\n")

conn.close()
