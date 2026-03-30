import sqlite3

VPP = r"c:\Users\1\source\repos\NewDiplom\class.vpp"
conn = sqlite3.connect(VPP)
c = conn.cursor()

print("=== CHECKS ===")

# 1. DIAGRAM.Child count
c.execute("SELECT DEFINITION FROM DIAGRAM WHERE ID='kt2mDNmFYEzgAQro'")
d = c.fetchone()[0]
text = d.decode("utf-8") if isinstance(d, bytes) else d
count = text.count("<kt2mDNmFYEzgAQro:")
print(f"1. DIAGRAM.Child entries: {count} (expected 99)")

# 2. DIAGRAM_ELEMENT all blobs?
c.execute("SELECT COUNT(*) FROM DIAGRAM_ELEMENT WHERE typeof(DEFINITION)='blob'")
blob_count = c.fetchone()[0]
c.execute("SELECT COUNT(*) FROM DIAGRAM_ELEMENT")
total = c.fetchone()[0]
print(f"2. DIAGRAM_ELEMENT blobs: {blob_count}/{total}")

# 3. PARENT_ID set for all classes?
c.execute("SELECT COUNT(*) FROM DIAGRAM_ELEMENT WHERE SHAPE_TYPE='Class' AND PARENT_ID IS NOT NULL")
with_parent = c.fetchone()[0]
c.execute("SELECT COUNT(*) FROM DIAGRAM_ELEMENT WHERE SHAPE_TYPE='Class'")
total_cls = c.fetchone()[0]
print(f"3. Class PARENT_ID set: {with_parent}/{total_cls}")

# 4. MODEL_ELEMENT blobs?
c.execute("SELECT COUNT(*) FROM MODEL_ELEMENT WHERE typeof(DEFINITION)='blob' AND MODEL_TYPE='Class'")
me_blob = c.fetchone()[0]
c.execute("SELECT COUNT(*) FROM MODEL_ELEMENT WHERE MODEL_TYPE='Class'")
me_total = c.fetchone()[0]
print(f"4. Class MODEL_ELEMENT blobs: {me_blob}/{me_total}")

# 5. Timestamps?
c.execute("SELECT COUNT(*) FROM MODEL_ELEMENT WHERE MODEL_TYPE='Class' AND AUTHOR IS NOT NULL")
with_ts = c.fetchone()[0]
print(f"5. CLASS MODEL_ELEMENT with AUTHOR: {with_ts}/{me_total}")

# 6. Check Package ContainedDiagramElements
c.execute("SELECT DEFINITION FROM DIAGRAM_ELEMENT WHERE SHAPE_TYPE='Package' LIMIT 1")
pkg_def = c.fetchone()[0]
pkg_text = pkg_def.decode("utf-8") if isinstance(pkg_def, bytes) else pkg_def
cont_count = pkg_text.count("<kt2mDNmFYEzgAQro:")
idx = pkg_text.find("ContainedDiagramElements")
print(f"6. Package ContainedDiagramElements count: {cont_count}")
print(f"   Sample: {pkg_text[idx:idx+120]}")

# 7. Package MODEL_ELEMENT children
c.execute("SELECT NAME, DEFINITION FROM MODEL_ELEMENT WHERE MODEL_TYPE='Package' LIMIT 1")
row = c.fetchone()
me_pkg_text = row[1].decode("utf-8") if isinstance(row[1], bytes) else row[1]
child_count = me_pkg_text.count("<nmpk")
print(f"7. Package '{row[0]}' model children: {child_count}")

conn.close()
print("Done.")
