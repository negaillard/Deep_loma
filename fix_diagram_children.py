#!/usr/bin/env python3
"""
Fix: DIAGRAM.Child must include ALL diagram elements (classes + packages + connectors).
Also ensure timestamps are set in MODEL_ELEMENT rows.
"""
import sqlite3

VPP = r"c:\Users\1\source\repos\NewDiplom\class.vpp"
DGRAM = "kt2mDNmFYEzgAQro"
TS = "1774550000000"
TS_SEC = 1774550000

conn = sqlite3.connect(VPP)
c = conn.cursor()

# 1. Collect ALL diagram element IDs in correct order:
#    packages first, then classes, then connectors
c.execute("SELECT ID FROM DIAGRAM_ELEMENT WHERE DIAGRAM_ID=? AND SHAPE_TYPE='Package'", (DGRAM,))
pkg_ids = [r[0] for r in c.fetchall()]

c.execute("SELECT ID FROM DIAGRAM_ELEMENT WHERE DIAGRAM_ID=? AND SHAPE_TYPE='Class'", (DGRAM,))
cls_ids = [r[0] for r in c.fetchall()]

c.execute("SELECT ID FROM DIAGRAM_ELEMENT WHERE DIAGRAM_ID=? AND SHAPE_TYPE='Dependency'", (DGRAM,))
dep_ids = [r[0] for r in c.fetchall()]

all_view_ids = pkg_ids + cls_ids + dep_ids
print(f"Packages: {len(pkg_ids)}, Classes: {len(cls_ids)}, Deps: {len(dep_ids)}")
print(f"Total: {len(all_view_ids)}")

# 2. Build new DIAGRAM definition with all child IDs
child_list = ", \n\t\t".join(f"<{DGRAM}:{v}>" for v in all_view_ids)

new_diagram_def = (
    f'{DGRAM}:"Class Diagram":ClassDiagram {{\n'
    f'\tpaintConnectorThroughLabel=1;\n'
    f'\t_shapeGroups=NULL;\n'
    f'\tdiagramBackground=(\n\t\t255, \n\t\t255, \n\t\t255, \n\t\t255\n\t);\n'
    f'\tconnectorLabelOrientation=0;\n'
    f'\tshowEllipsisForUnshownClassMembers=2;\n'
    f'\tshowPackageNameStyle=0;\n'
    f'\tshowDefaultPackage=T;\n'
    f'\tpointConnectorEndToCompartmentMember=T;\n'
    f'\tshowAttributeGetterSetter=F;\n'
    f'\tautoFitShapesSize=F;\n'
    f'\tconnectorStyle=1;\n'
    f'\tdiagramPreviewData_name=NULL;\n'
    f'\tgeneralizationSetNotation=2;\n'
    f'\t_globalPaletteOption=T;\n'
    f'\tconnectionPointStyle=0;\n'
    f'\tgridWidth=10;\n'
    f'\tpmCreateDateTime="{TS}";\n'
    f'\talignToGrid=F;\n'
    f'\tsuppressImplied1MultiplicityForAttributeAndAssociationEnd=F;\n'
    f'\tpmLastModified="{TS}";\n'
    f'\thiddenDiagramElementIds=NULL;\n'
    f'\tgridVisible=F;\n'
    f'\tzoomRatio=0.4;\n'
    f'\tpmAuthor="1";\n'
    f'\tconnectorLineJumpsSize=0;\n'
    f'\tshowStereotypes=T;\n'
    f'\tshowClassEmptyCompartments=2;\n'
    f'\tshapePresentationOption=0;\n'
    f'\tconnectorModelElementNameAlignment=4;\n'
    f'\tvoiceIds=NULL;\n'
    f'\tshowAssociationNavigationArrows=3;\n'
    f'\tinitializeDiagramForCreate=T;\n'
    f'\treferenceMappingReferencedElementIds=NULL;\n'
    f'\tChild=(\n\t\t{child_list}\n\t);\n'
    f'\treferenceMappingElementIds=NULL;\n'
    f'\tmodelElementNameAlignment=4;\n'
    f'\tshowActivityStateNodeCaption=524287;\n'
    f'\tconnectorLineJumps=0;\n'
    f'\tgridHeight=10;\n'
    f'\tgridColor=(\n\t\t192, \n\t\t192, \n\t\t192, \n\t\t255\n\t);\n'
    f'\tshowModelElementIdModelTypes=NULL;\n'
    f'}}'
)

c.execute("UPDATE DIAGRAM SET DEFINITION=?, NAME=? WHERE ID=?",
          (sqlite3.Binary(new_diagram_def.encode('utf-8')), "Class Diagram", DGRAM))
print("Updated DIAGRAM definition")

# 3. Fix MODEL_ELEMENT: add missing timestamps and author
c.execute("SELECT ID FROM MODEL_ELEMENT WHERE ID LIKE 'nmp%' OR ID LIKE 'nmc%' OR ID LIKE 'nmd%'")
new_ids = [r[0] for r in c.fetchall()]
for eid in new_ids:
    c.execute("UPDATE MODEL_ELEMENT SET AUTHOR=?, CREATE_AT=?, LAST_MOD_AT=? WHERE ID=?",
              ('1', TS_SEC, TS_SEC, eid))
print(f"Fixed timestamps for {len(new_ids)} MODEL_ELEMENT rows")

conn.commit()
conn.close()
print("Done!")
