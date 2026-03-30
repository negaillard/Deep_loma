#!/usr/bin/env python3
"""
Fix: set PARENT_ID in DIAGRAM_ELEMENT for all class/dependency shapes.
The _parent field in each Class definition already contains the package view ID —
we just need to extract it and write it into the PARENT_ID column.
"""
import sqlite3
import re

VPP_FILE = r"c:\Users\1\source\repos\NewDiplom\class.vpp"
DGRAM = "kt2mDNmFYEzgAQro"

conn = sqlite3.connect(VPP_FILE)
c = conn.cursor()

# Fetch all diagram elements
c.execute("SELECT ID, SHAPE_TYPE, DEFINITION FROM DIAGRAM_ELEMENT WHERE DIAGRAM_ID=?", (DGRAM,))
rows = c.fetchall()

updated = 0
for (eid, shape_type, defn) in rows:
    if shape_type not in ("Class",):
        continue
    # Extract _parent=<DIAGRAM_ID:PKG_VIEW_ID> from definition
    m = re.search(r'_parent=<[^:]+:([^>]+)>', defn or "")
    if m:
        pkg_view_id = m.group(1)
        c.execute("UPDATE DIAGRAM_ELEMENT SET PARENT_ID=? WHERE ID=?", (pkg_view_id, eid))
        updated += 1

conn.commit()
conn.close()
print(f"Updated PARENT_ID for {updated} class diagram elements.")
