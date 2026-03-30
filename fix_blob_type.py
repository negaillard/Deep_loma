#!/usr/bin/env python3
"""
Fix: convert DEFINITION columns from TEXT to BLOB (bytes).
Visual Paradigm only reads BLOB-typed definitions.
"""
import sqlite3

VPP_FILE = r"c:\Users\1\source\repos\NewDiplom\class.vpp"
conn = sqlite3.connect(VPP_FILE)
c = conn.cursor()

# Fix DIAGRAM_ELEMENT
c.execute("SELECT ID, DEFINITION FROM DIAGRAM_ELEMENT WHERE typeof(DEFINITION)='text'")
rows = c.fetchall()
print(f"DIAGRAM_ELEMENT text rows: {len(rows)}")
for (eid, defn) in rows:
    c.execute("UPDATE DIAGRAM_ELEMENT SET DEFINITION=? WHERE ID=?",
              (sqlite3.Binary(defn.encode('utf-8')), eid))

# Fix MODEL_ELEMENT
c.execute("SELECT ID, DEFINITION FROM MODEL_ELEMENT WHERE typeof(DEFINITION)='text'")
rows = c.fetchall()
print(f"MODEL_ELEMENT text rows: {len(rows)}")
for (eid, defn) in rows:
    c.execute("UPDATE MODEL_ELEMENT SET DEFINITION=? WHERE ID=?",
              (sqlite3.Binary(defn.encode('utf-8')), eid))

# Fix DIAGRAM table too
c.execute("SELECT ID, DEFINITION FROM DIAGRAM WHERE typeof(DEFINITION)='text'")
rows = c.fetchall()
print(f"DIAGRAM text rows: {len(rows)}")
for (eid, defn) in rows:
    c.execute("UPDATE DIAGRAM SET DEFINITION=? WHERE ID=?",
              (sqlite3.Binary(defn.encode('utf-8')), eid))

# Fix PROJECT_INFO
c.execute("SELECT ID, DEFINITION FROM PROJECT_INFO WHERE typeof(DEFINITION)='text'")
rows = c.fetchall()
print(f"PROJECT_INFO text rows: {len(rows)}")
for (eid, defn) in rows:
    c.execute("UPDATE PROJECT_INFO SET DEFINITION=? WHERE ID=?",
              (sqlite3.Binary(defn.encode('utf-8')), eid))

conn.commit()
conn.close()

# Verify
conn2 = sqlite3.connect(VPP_FILE)
c2 = conn2.cursor()
c2.execute("SELECT typeof(DEFINITION) FROM DIAGRAM_ELEMENT LIMIT 5")
types = [r[0] for r in c2.fetchall()]
print(f"After fix, DIAGRAM_ELEMENT types: {types}")
c2.execute("SELECT typeof(DEFINITION) FROM MODEL_ELEMENT WHERE ID LIKE 'nmc%' LIMIT 5")
types2 = [r[0] for r in c2.fetchall()]
print(f"After fix, MODEL_ELEMENT types: {types2}")
conn2.close()
print("Done.")
