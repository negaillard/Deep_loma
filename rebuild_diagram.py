#!/usr/bin/env python3
"""
Rebuilds the class.vpp class diagram for the NewDiplom project.
Replaces the old prototype diagram with the actual project architecture.
"""
import sqlite3
import shutil

VPP_FILE = r"c:\Users\1\source\repos\NewDiplom\class.vpp"
shutil.copy(VPP_FILE, VPP_FILE + ".bak")
print("Backup created.")

DGRAM   = "kt2mDNmFYEzgAQro"   # existing Class Diagram 1
DEP_C   = "Cd3ODNmFYEzgAQzO"   # Dependency container
ASSOC_C = "z.ShDNmFYEzgAQ9B"   # Association container
RELS    = "_itWDNmFYEzgAQtV"    # relationships root
TS      = "1774550000000"

# ── deterministic ID generator ─────────────────────────────────────────────
_cnt = {}
def gid(prefix):
    """Return a unique 16-char ID.  prefix is 4 chars (padded/truncated)."""
    p = (prefix + "0000")[:4]
    _cnt[p] = _cnt.get(p, 0) + 1
    return f"{p}{_cnt[p]:012d}"

# ── VP text-format helpers ─────────────────────────────────────────────────
def _fill(r, g, b):
    return (f"(\n\t\t@gradientStyle=1;, \n\t\t@transparency=0;, \n\t\t@type=1;, \n"
            f"\t\t@color1=(\n\t\t\t{r}, \n\t\t\t{g}, \n\t\t\t{b}, \n\t\t\t255\n\t\t);\n\t)")

def _font():
    return '(\n\t\t@name="Dialog";, \n\t\t@color=(0, 0, 0, 255);, \n\t\t@size=11;, \n\t\t@style=0;\n\t)'

def _line():
    return '(\n\t\t@cap=0;, \n\t\t@transparency=0;, \n\t\t@weight=1.0;, \n\t\t@color=(0, 0, 0, 255);, \n\t\t@hasStroke=T;\n\t)'

def attr_def(aid, name, type_str):
    return (f'{{{aid}:"{name}":Attribute {{\n'
            f'\ttype_string="{type_str}";\n'
            f'\tpmLastModified="{TS}";\n\tpmAuthor="1";\n'
            f'\tpmCreateDateTime="{TS}";\n\t_modelViews=NULL;\n'
            f'\tlastModifiedTime={TS};\n\t_modelEditable=T;\n}}}}')

def class_model_def(cid, vid, mvid, name, pkg_id, attrs, abstract=False):
    child_block = ""
    if attrs:
        child_block = "\n\tChild=(\n\t\t" + ",\n\t\t".join(attrs) + "\n\t);"
    abst = "\n\tisAbstract=T;" if abstract else ""
    return (f'{cid}:"{name}":Class {{\n'
            f'\t_modelEditable=T;{abst}\n'
            f'\t_masterViewId="{vid}";\n'
            f'\tpmAuthor="1";\n\tlastModifiedTime={TS};\n'
            f'\tpmCreateDateTime="{TS}";\n'
            f'\t_modelViews=(\n'
            f'\t\t{{{mvid}:"View":ModelView {{\n'
            f'\t\t\tcontainer=<{DGRAM}>;\n\t\t\tview="{vid}";\n'
            f'\t\t}}}}\n\t);{child_block}\n'
            f'\tpmLastModified="{TS}";\n}}')

def pkg_model_def(pid, vid, mvid, name, child_ids):
    children = ",\n\t\t".join(f"<{pid}:{c}>" for c in child_ids)
    return (f'{pid}:"{name}":Package {{\n'
            f'\t_modelEditable=T;\n\t_masterViewId="{vid}";\n'
            f'\tpmAuthor="1";\n\tlastModifiedTime={TS};\n'
            f'\tpmCreateDateTime="{TS}";\n'
            f'\t_modelViews=(\n'
            f'\t\t{{{mvid}:"View":ModelView {{\n'
            f'\t\t\tcontainer=<{DGRAM}>;\n\t\t\tview="{vid}";\n'
            f'\t\t}}}}\n\t);\n'
            f'\tChild=(\n\t\t{children}\n\t);\n'
            f'\tpmLastModified="{TS}";\n}}')

def dep_model_def(did, dvid, mvid, from_pkg, from_cls, to_pkg, to_cls):
    return (f'{did}:NULL:Dependency {{\n'
            f'\t_modelEditable=T;\n\ttoModel=<{to_pkg}:{to_cls}>;\n'
            f'\t_masterViewId="{dvid}";\n\tpmAuthor="1";\n'
            f'\tlastModifiedTime={TS};\n\tpmCreateDateTime="{TS}";\n'
            f'\t_modelViews=(\n'
            f'\t\t{{{mvid}:"View":ModelView {{\n'
            f'\t\t\tcontainer=<{DGRAM}>;\n\t\t\tview="{dvid}";\n'
            f'\t\t}}}}\n\t);\n'
            f'\tfromModel=<{from_pkg}:{from_cls}>;\n'
            f'\tpmLastModified="{TS}";\n}}')

def class_view_def(vid, name, pid, cid, pkg_vid, x, y, w, h, rgb):
    r, g, b = rgb
    return (f'{vid}:"{name}":Class {{\n'
            f'\tshowOperationType=1;\n\tforeground=(0, 0, 0, 255);\n'
            f'\tshowAttributeType=1;\n\tdisplayAsRobustnessAnalysisIcon=T;\n'
            f'\tshowReceptionType=1;\n\tshowEnumerationLiteralType=1;\n'
            f'\tshowParameterNameInOperationSignature=T;\n\tconnectToPoint=T;\n'
            f'\ty={y};\n\tx={x};\n'
            f'\tmetaModelElement=<{pid}:{cid}>;\n'
            f'\tlShCmMl=F;\n\toverrideAppearanceWithStereotypeIcon=T;\n'
            f'\tmSwTpPts=T;\n\tparentConnectorHeaderLength=40;\n'
            f'\tparentConnectorLineLength=10;\n\theight={h};\n\twpMbs=F;\n'
            f'\t_fillColor={_fill(r,g,b)};\n'
            f'\twidth={w};\n\tshowOperationSignature=T;\n\tkSwCsMbSt=T;\n'
            f'\tvisibilityStyle=1;\n\tshowInitialAttributeValue=T;\n'
            f'\tbackground=({r}, {g}, {b}, 255);\n'
            f'\t_parent=<{DGRAM}:{pkg_vid}>;\n'
            f'\t_elementFont={_font()};\n'
            f'\t_captionUIModel=(\n'
            f'\t\t@x=0;, @y=0;, @width={w+1};, @height={h};, '
            f'@side=12;, @visible=T;, @internalWidth=-2147483648;, @internalHeight=-2147483648;\n\t);\n'
            f'\tinterfaceBall=F;\n'
            f'\t_lineModel={_line()};\n}}')

def pkg_view_def(vid, name, pid, x, y, w, h, contained_vids, rgb):
    r, g, b = rgb
    contained = ",\n\t\t".join(f"<{DGRAM}:{v}>" for v in contained_vids)
    return (f'{vid}:"{name}":Package {{\n'
            f'\tforeground=(0, 0, 0, 255);\n\tmodelElementNameAlignment=1;\n'
            f'\tconnectToPoint=T;\n\ty={y};\n\tx={x};\n'
            f'\tmetaModelElement=<{pid}>;\n'
            f'\toverrideAppearanceWithStereotypeIcon=T;\n'
            f'\tparentConnectorHeaderLength=40;\n\tparentConnectorLineLength=10;\n'
            f'\theight={h};\n'
            f'\t_fillColor={_fill(r,g,b)};\n'
            f'\twidth={w};\n\tbackground=({r}, {g}, {b}, 255);\n'
            f'\t_elementFont={_font()};\n'
            f'\t_captionUIModel=(\n'
            f'\t\t@x=0;, @y=20;, @width={w};, @height={h-20};, '
            f'@side=12;, @visible=T;, @internalWidth=-2147483648;, @internalHeight=-2147483648;\n\t);\n'
            f'\tContainedDiagramElements=(\n\t\t{contained}\n\t);\n'
            f'\t_lineModel={_line()};\n}}')

def dep_view_def(vid, did, from_vid, to_vid):
    return (f'{vid}:NULL:Dependency {{\n'
            f'\tbackground=(122, 207, 245, 255);\n\twidth=10;\n'
            f'\ttoPinType=1;\n\tuseToShapeCenter=T;\n'
            f'\t_captionUIModel=(\n'
            f'\t\t@x=0;, @y=-2;, @width=20;, @height=0;, '
            f'@side=1;, @visible=T;, @internalWidth=-2147483648;, @internalHeight=-2147483648;\n\t);\n'
            f'\t_toShape=<{DGRAM}:{to_vid}>;\n'
            f'\t_elementFont={_font()};\n'
            f'\t_points="0,0;10,0;";\n'
            f'\tmetaModelElement=<{RELS}:{DEP_C}:{did}>;\n'
            f'\tfromPinType=1;\n\tforeground=(0, 0, 0, 255);\n'
            f'\ty=0;\n\t_fromShape=<{DGRAM}:{from_vid}>;\n\tx=0;\n'
            f'\t_lineModel={_line()};\n'
            f'\theight=10;\n\tuseFromShapeCenter=T;\n}}')

# ═══════════════════════════════════════════════════════════════════════════
# DATA MODEL DEFINITIONS
# ═══════════════════════════════════════════════════════════════════════════

# Package IDs
PID = {n: gid("nmpk") for n in ["Models","Storage","Contracts","Logic","Auth","Messaging","API"]}
PVID = {n: gid("nvpk") for n in PID}
PMVID = {n: gid("nmmv") for n in PID}

# ── Models package elements ────────────────────────────────────────────────
# Interfaces
I_IID    = gid("nmcl"); I_IID_V    = gid("nvcl"); I_IID_MV    = gid("nmmv")
I_USER   = gid("nmcl"); I_USER_V   = gid("nvcl"); I_USER_MV   = gid("nmmv")
I_ROLE   = gid("nmcl"); I_ROLE_V   = gid("nvcl"); I_ROLE_MV   = gid("nmmv")
I_DOC    = gid("nmcl"); I_DOC_V    = gid("nvcl"); I_DOC_MV    = gid("nmmv")
I_DOCU   = gid("nmcl"); I_DOCU_V   = gid("nvcl"); I_DOCU_MV   = gid("nmmv")
I_CERT   = gid("nmcl"); I_CERT_V   = gid("nvcl"); I_CERT_MV   = gid("nmmv")
I_SIG    = gid("nmcl"); I_SIG_V    = gid("nvcl"); I_SIG_MV    = gid("nmmv")
# Enums
E_DOCST  = gid("nmcl"); E_DOCST_V  = gid("nvcl"); E_DOCST_MV  = gid("nmmv")
E_SIGST  = gid("nmcl"); E_SIGST_V  = gid("nvcl"); E_SIGST_MV  = gid("nmmv")
E_SYSRL  = gid("nmcl"); E_SYSRL_V  = gid("nvcl"); E_SYSRL_MV  = gid("nmmv")
E_CERTM  = gid("nmcl"); E_CERTM_V  = gid("nvcl"); E_CERTM_MV  = gid("nmmv")
E_APPTY  = gid("nmcl"); E_APPTY_V  = gid("nvcl"); E_APPTY_MV  = gid("nmmv")

# ── Storage package elements ───────────────────────────────────────────────
S_USER   = gid("nmcl"); S_USER_V   = gid("nvcl"); S_USER_MV   = gid("nmmv")
S_ROLE   = gid("nmcl"); S_ROLE_V   = gid("nvcl"); S_ROLE_MV   = gid("nmmv")
S_DOC    = gid("nmcl"); S_DOC_V    = gid("nvcl"); S_DOC_MV    = gid("nmmv")
S_DOCU   = gid("nmcl"); S_DOCU_V   = gid("nvcl"); S_DOCU_MV   = gid("nmmv")
S_CERT   = gid("nmcl"); S_CERT_V   = gid("nvcl"); S_CERT_MV   = gid("nmmv")
S_SIG    = gid("nmcl"); S_SIG_V    = gid("nvcl"); S_SIG_MV    = gid("nmmv")
S_CTX    = gid("nmcl"); S_CTX_V    = gid("nvcl"); S_CTX_MV    = gid("nmmv")

# ── Contracts package elements ─────────────────────────────────────────────
# Logic contracts
C_IULOG  = gid("nmcl"); C_IULOG_V  = gid("nvcl"); C_IULOG_MV  = gid("nmmv")
C_IRLOG  = gid("nmcl"); C_IRLOG_V  = gid("nvcl"); C_IRLOG_MV  = gid("nmmv")
C_IDLOG  = gid("nmcl"); C_IDLOG_V  = gid("nvcl"); C_IDLOG_MV  = gid("nmmv")
C_IDULOG = gid("nmcl"); C_IDULOG_V = gid("nvcl"); C_IDULOG_MV = gid("nmmv")
C_ICLOG  = gid("nmcl"); C_ICLOG_V  = gid("nvcl"); C_ICLOG_MV  = gid("nmmv")
C_ISLOG  = gid("nmcl"); C_ISLOG_V  = gid("nvcl"); C_ISLOG_MV  = gid("nmmv")
C_IANTIV = gid("nmcl"); C_IANTIV_V = gid("nvcl"); C_IANTIV_MV = gid("nmmv")
C_IDSIGN = gid("nmcl"); C_IDSIGN_V = gid("nvcl"); C_IDSIGN_MV = gid("nmmv")
C_ICGEN  = gid("nmcl"); C_ICGEN_V  = gid("nvcl"); C_ICGEN_MV  = gid("nmmv")
C_IFILE  = gid("nmcl"); C_IFILE_V  = gid("nvcl"); C_IFILE_MV  = gid("nmmv")
# Storage contracts
C_IUSTO  = gid("nmcl"); C_IUSTO_V  = gid("nvcl"); C_IUSTO_MV  = gid("nmmv")
C_IRSTO  = gid("nmcl"); C_IRSTO_V  = gid("nvcl"); C_IRSTO_MV  = gid("nmmv")
C_IDSTO  = gid("nmcl"); C_IDSTO_V  = gid("nvcl"); C_IDSTO_MV  = gid("nmmv")
C_IDUSTO = gid("nmcl"); C_IDUSTO_V = gid("nvcl"); C_IDUSTO_MV = gid("nmmv")
C_ICSTO  = gid("nmcl"); C_ICSTO_V  = gid("nvcl"); C_ICSTO_MV  = gid("nmmv")
C_ISSTO  = gid("nmcl"); C_ISSTO_V  = gid("nvcl"); C_ISSTO_MV  = gid("nmmv")
# Auth contracts
C_ISESS  = gid("nmcl"); C_ISESS_V  = gid("nvcl"); C_ISESS_MV  = gid("nmmv")
C_IMAIL  = gid("nmcl"); C_IMAIL_V  = gid("nvcl"); C_IMAIL_MV  = gid("nmmv")
C_ICODE  = gid("nmcl"); C_ICODE_V  = gid("nvcl"); C_ICODE_MV  = gid("nmmv")

# ── Logic package elements ─────────────────────────────────────────────────
L_ULOG   = gid("nmcl"); L_ULOG_V   = gid("nvcl"); L_ULOG_MV   = gid("nmmv")
L_RLOG   = gid("nmcl"); L_RLOG_V   = gid("nvcl"); L_RLOG_MV   = gid("nmmv")
L_DLOG   = gid("nmcl"); L_DLOG_V   = gid("nvcl"); L_DLOG_MV   = gid("nmmv")
L_DULOG  = gid("nmcl"); L_DULOG_V  = gid("nvcl"); L_DULOG_MV  = gid("nmmv")
L_CLOG   = gid("nmcl"); L_CLOG_V   = gid("nvcl"); L_CLOG_MV   = gid("nmmv")
L_SLOG   = gid("nmcl"); L_SLOG_V   = gid("nvcl"); L_SLOG_MV   = gid("nmmv")
L_CLAM   = gid("nmcl"); L_CLAM_V   = gid("nvcl"); L_CLAM_MV   = gid("nmmv")
L_SELFCG = gid("nmcl"); L_SELFCG_V = gid("nvcl"); L_SELFCG_MV = gid("nmmv")
L_INTSIG = gid("nmcl"); L_INTSIG_V = gid("nvcl"); L_INTSIG_MV = gid("nmmv")
L_LFILE  = gid("nmcl"); L_LFILE_V  = gid("nvcl"); L_LFILE_MV  = gid("nmmv")

# ── Auth package elements ──────────────────────────────────────────────────
A_SESS   = gid("nmcl"); A_SESS_V   = gid("nvcl"); A_SESS_MV   = gid("nmmv")
A_MAIL   = gid("nmcl"); A_MAIL_V   = gid("nvcl"); A_MAIL_MV   = gid("nmmv")
A_CODE   = gid("nmcl"); A_CODE_V   = gid("nvcl"); A_CODE_MV   = gid("nmmv")

# ── Messaging package elements ─────────────────────────────────────────────
M_SREQ   = gid("nmcl"); M_SREQ_V   = gid("nvcl"); M_SREQ_MV   = gid("nmmv")
M_NOTI   = gid("nmcl"); M_NOTI_V   = gid("nvcl"); M_NOTI_MV   = gid("nmmv")
M_SCONS  = gid("nmcl"); M_SCONS_V  = gid("nvcl"); M_SCONS_MV  = gid("nmmv")
M_NCONS  = gid("nmcl"); M_NCONS_V  = gid("nvcl"); M_NCONS_MV  = gid("nmmv")

# ── API package elements ───────────────────────────────────────────────────
P_AUTHC  = gid("nmcl"); P_AUTHC_V  = gid("nvcl"); P_AUTHC_MV  = gid("nmmv")
P_USRC   = gid("nmcl"); P_USRC_V   = gid("nvcl"); P_USRC_MV   = gid("nmmv")
P_DOCC   = gid("nmcl"); P_DOCC_V   = gid("nvcl"); P_DOCC_MV   = gid("nmmv")
P_SIGC   = gid("nmcl"); P_SIGC_V   = gid("nvcl"); P_SIGC_MV   = gid("nmmv")
P_AUTHMW = gid("nmcl"); P_AUTHMW_V = gid("nvcl"); P_AUTHMW_MV = gid("nmmv")

# ── Dependency IDs ─────────────────────────────────────────────────────────
def new_dep():
    return gid("nmde"), gid("nvde"), gid("nmmv")

# Storage → Models
D_UIMPL = new_dep()   # User → IUserModel
D_RIMPL = new_dep()   # Role → IRoleModel
D_DIMPL = new_dep()   # Document → IDocumentModel
D_DUIMP = new_dep()   # DocumentUser → IDocumentUserModel
D_CIMPL = new_dep()   # Certificate → ICertificateModel
D_SIMPL = new_dep()   # Signature → ISignatureModel

# Logic → Contracts (logic interfaces)
D_ULIMP = new_dep()   # UserLogic → IUserLogic
D_RLIMP = new_dep()   # RoleLogic → IRoleLogic
D_DLIMP = new_dep()   # DocumentLogic → IDocumentLogic
D_DULIMP= new_dep()   # DocumentUserLogic → IDocumentUserLogic
D_CLIMP = new_dep()   # CertificateLogic → ICertificateLogic
D_SLIMP = new_dep()   # SignatureLogic → ISignatureLogic
D_CLAMI = new_dep()   # ClamAvService → IAntivirusService
D_SCGI  = new_dep()   # SelfSignedCertificateGenerator → ICertificateGeneratorLogic
D_ISIMP = new_dep()   # InternalDocumentSigner → IDocumentSigner
D_LFIMP = new_dep()   # LocalFileStorage → IFileStorage

# Logic → Storage contracts (dependencies on storage interfaces)
D_ULSTU = new_dep()   # UserLogic → IUserStorage
D_RLSTU = new_dep()   # RoleLogic → IRoleStorage
D_DLSTO = new_dep()   # DocumentLogic → IDocumentStorage
D_DLFSF = new_dep()   # DocumentLogic → IFileStorage
D_DULST = new_dep()   # DocumentUserLogic → IDocumentUserStorage
D_CLSTO = new_dep()   # CertificateLogic → ICertificateStorage
D_SLSTO = new_dep()   # SignatureLogic → ISignatureStorage

# Auth implementations
D_SESIMP= new_dep()   # SessionService → ISessionService
D_MAIMP = new_dep()   # EmailService → IEmailService
D_CODIMP= new_dep()   # CodeVerificationLogic → ICodeVerificationLogic

# Consumers use message contracts
D_SCMSG = new_dep()   # SignDocumentConsumer → SigningRequestMessage
D_NCMSG = new_dep()   # NotificationConsumer → NotificationMessage

# API → Logic contracts
D_AUCAPI= new_dep()   # AuthController → ISessionService (uses)
D_USAPI = new_dep()   # UsersController → IUserLogic
D_DCAPI = new_dep()   # DocumentsController → IDocumentLogic
D_SCAPI = new_dep()   # SigningController → IDocumentUserLogic

# ═══════════════════════════════════════════════════════════════════════════
# BUILD MODEL ELEMENTS (MODEL_ELEMENT table rows)
# ═══════════════════════════════════════════════════════════════════════════

model_rows = []   # (id, user_id, parent_id, model_type, name, definition)
diagram_rows = [] # (id, shape_type, diagram_id, model_el_id, definition)

def add_class(cid, vid, mvid, name, pkg_id, attrs, abstract=False, parent_in_db=None):
    defn = class_model_def(cid, vid, mvid, name, pkg_id, attrs, abstract)
    model_rows.append((cid, None, parent_in_db or pkg_id, "Class", name, defn))

def add_pkg(pid, vid, mvid, name, child_ids):
    defn = pkg_model_def(pid, vid, mvid, name, child_ids)
    model_rows.append((pid, None, None, "Package", name, defn))

def add_dep(ids_tuple, from_pkg, from_cls, to_pkg, to_cls):
    did, dvid, mvid = ids_tuple
    defn = dep_model_def(did, dvid, mvid, from_pkg, from_cls, to_pkg, to_cls)
    model_rows.append((did, None, DEP_C, "Dependency", None, defn))

def add_class_view(vid, name, pid, cid, pkg_vid, x, y, w, h, rgb):
    defn = class_view_def(vid, name, pid, cid, pkg_vid, x, y, w, h, rgb)
    # (vid, shape_type, diagram_id, model_el_id, parent_id, defn)
    diagram_rows.append((vid, "Class", DGRAM, cid, pkg_vid, defn))

def add_pkg_view(vid, name, pid, x, y, w, h, contained_vids, rgb):
    defn = pkg_view_def(vid, name, pid, x, y, w, h, contained_vids, rgb)
    diagram_rows.append((vid, "Package", DGRAM, pid, None, defn))

def add_dep_view(ids_tuple, from_vid, to_vid):
    did, dvid, mvid = ids_tuple
    defn = dep_view_def(dvid, did, from_vid, to_vid)
    diagram_rows.append((dvid, "Dependency", DGRAM, did, None, defn))

# ── Colors ─────────────────────────────────────────────────────────────────
RGB_MOD  = (180, 220, 255)   # Models — light blue
RGB_STO  = (180, 255, 205)   # Storage — light green
RGB_CON  = (225, 200, 255)   # Contracts — light purple
RGB_LOG  = (255, 250, 180)   # Logic — light yellow
RGB_AUTH = (255, 220, 180)   # Auth — light orange
RGB_MSG  = (255, 200, 220)   # Messaging — light pink
RGB_API  = (210, 220, 210)   # API — light sage

# ──────────────────────────────────────────────────────────────────────────
# MODELS PACKAGE
# ──────────────────────────────────────────────────────────────────────────
MODS = PID["Models"]

add_class(I_IID, I_IID_V, I_IID_MV, "IId", MODS, [
    attr_def(gid("nmat"), "+Id", "int"),
], abstract=True)

add_class(I_USER, I_USER_V, I_USER_MV, "IUserModel", MODS, [
    attr_def(gid("nmat"), "+Fullname", "string"),
    attr_def(gid("nmat"), "+Login", "string"),
    attr_def(gid("nmat"), "+Email", "string"),
    attr_def(gid("nmat"), "+RoleId", "int"),
    attr_def(gid("nmat"), "+SystemRole", "SystemRole"),
    attr_def(gid("nmat"), "+IsActive", "bool"),
], abstract=True)

add_class(I_ROLE, I_ROLE_V, I_ROLE_MV, "IRoleModel", MODS, [
    attr_def(gid("nmat"), "+Name", "string"),
    attr_def(gid("nmat"), "+Description", "string"),
], abstract=True)

add_class(I_DOC, I_DOC_V, I_DOC_MV, "IDocumentModel", MODS, [
    attr_def(gid("nmat"), "+Title", "string"),
    attr_def(gid("nmat"), "+Description", "string"),
    attr_def(gid("nmat"), "+Status", "DocumentStatus"),
    attr_def(gid("nmat"), "+IsSequential", "bool"),
    attr_def(gid("nmat"), "+CreatedByUserId", "int"),
    attr_def(gid("nmat"), "+IsDeleted", "bool"),
], abstract=True)

add_class(I_DOCU, I_DOCU_V, I_DOCU_MV, "IDocumentUserModel", MODS, [
    attr_def(gid("nmat"), "+UserId", "int"),
    attr_def(gid("nmat"), "+DocumentId", "int"),
    attr_def(gid("nmat"), "+SigningStatus", "SigningStatus"),
    attr_def(gid("nmat"), "+Order", "int"),
], abstract=True)

add_class(I_CERT, I_CERT_V, I_CERT_MV, "ICertificateModel", MODS, [
    attr_def(gid("nmat"), "+StartDate", "DateTime"),
    attr_def(gid("nmat"), "+FinishDate", "DateTime"),
    attr_def(gid("nmat"), "+PublicKey", "string"),
    attr_def(gid("nmat"), "+UserId", "int"),
    attr_def(gid("nmat"), "+Mode", "CertificateMode"),
    attr_def(gid("nmat"), "+IsActual", "bool"),
], abstract=True)

add_class(I_SIG, I_SIG_V, I_SIG_MV, "ISignatureModel", MODS, [
    attr_def(gid("nmat"), "+SignatureValue", "string"),
    attr_def(gid("nmat"), "+UserId", "int"),
    attr_def(gid("nmat"), "+DocumentId", "int"),
    attr_def(gid("nmat"), "+IsDeleted", "bool"),
], abstract=True)

add_class(E_DOCST, E_DOCST_V, E_DOCST_MV, "DocumentStatus", MODS, [
    attr_def(gid("nmat"), "NOT_SIGNED", ""),
    attr_def(gid("nmat"), "PARTLY_SIGNED", ""),
    attr_def(gid("nmat"), "SIGNED", ""),
    attr_def(gid("nmat"), "DECLINED", ""),
])

add_class(E_SIGST, E_SIGST_V, E_SIGST_MV, "SigningStatus", MODS, [
    attr_def(gid("nmat"), "NOT_SIGNED", ""),
    attr_def(gid("nmat"), "SIGNED", ""),
    attr_def(gid("nmat"), "DECLINED", ""),
    attr_def(gid("nmat"), "PENDING", ""),
])

add_class(E_SYSRL, E_SYSRL_V, E_SYSRL_MV, "SystemRole", MODS, [
    attr_def(gid("nmat"), "SystemAdmin", ""),
    attr_def(gid("nmat"), "DocumentManager", ""),
    attr_def(gid("nmat"), "Signer", ""),
])

add_class(E_CERTM, E_CERTM_V, E_CERTM_MV, "CertificateMode", MODS, [
    attr_def(gid("nmat"), "Internal", ""),
    attr_def(gid("nmat"), "Local", ""),
])

add_class(E_APPTY, E_APPTY_V, E_APPTY_MV, "AppType", MODS, [
    attr_def(gid("nmat"), "SIGNER_APP", ""),
    attr_def(gid("nmat"), "DOCUMENT_APP", ""),
    attr_def(gid("nmat"), "ADMIN_APP", ""),
])

add_pkg(MODS, PVID["Models"], PMVID["Models"], "Models",
    [I_IID,I_USER,I_ROLE,I_DOC,I_DOCU,I_CERT,I_SIG,
     E_DOCST,E_SIGST,E_SYSRL,E_CERTM,E_APPTY])

# ──────────────────────────────────────────────────────────────────────────
# STORAGE PACKAGE
# ──────────────────────────────────────────────────────────────────────────
STOR = PID["Storage"]

add_class(S_USER, S_USER_V, S_USER_MV, "User", STOR, [
    attr_def(gid("nmat"), "+Id", "int"),
    attr_def(gid("nmat"), "+Fullname", "string"),
    attr_def(gid("nmat"), "+Login", "string"),
    attr_def(gid("nmat"), "+Email", "string"),
    attr_def(gid("nmat"), "+SystemRole", "SystemRole"),
    attr_def(gid("nmat"), "+IsActive", "bool"),
    attr_def(gid("nmat"), "+Role", "Role"),
    attr_def(gid("nmat"), "+Documents", "List<Document>"),
    attr_def(gid("nmat"), "+Create(m): User", ""),
    attr_def(gid("nmat"), "+GetViewModel(): UserViewModel", ""),
])

add_class(S_ROLE, S_ROLE_V, S_ROLE_MV, "Role", STOR, [
    attr_def(gid("nmat"), "+Id", "int"),
    attr_def(gid("nmat"), "+Name", "string"),
    attr_def(gid("nmat"), "+Description", "string"),
    attr_def(gid("nmat"), "+Users", "List<User>"),
    attr_def(gid("nmat"), "+Create(m): Role", ""),
    attr_def(gid("nmat"), "+GetViewModel(): RoleViewModel", ""),
])

add_class(S_DOC, S_DOC_V, S_DOC_MV, "Document", STOR, [
    attr_def(gid("nmat"), "+Id", "int"),
    attr_def(gid("nmat"), "+Title", "string"),
    attr_def(gid("nmat"), "+Path", "string"),
    attr_def(gid("nmat"), "+Status", "DocumentStatus"),
    attr_def(gid("nmat"), "+IsSequential", "bool"),
    attr_def(gid("nmat"), "+CreatedByUserId", "int"),
    attr_def(gid("nmat"), "+IsDeleted", "bool"),
    attr_def(gid("nmat"), "+GetViewModel(): DocumentViewModel", ""),
])

add_class(S_DOCU, S_DOCU_V, S_DOCU_MV, "DocumentUser", STOR, [
    attr_def(gid("nmat"), "+Id", "int"),
    attr_def(gid("nmat"), "+UserId", "int"),
    attr_def(gid("nmat"), "+DocumentId", "int"),
    attr_def(gid("nmat"), "+SigningStatus", "SigningStatus"),
    attr_def(gid("nmat"), "+Order", "int"),
    attr_def(gid("nmat"), "+AssignedAt", "DateTime?"),
])

add_class(S_CERT, S_CERT_V, S_CERT_MV, "Certificate", STOR, [
    attr_def(gid("nmat"), "+Id", "int"),
    attr_def(gid("nmat"), "+StartDate", "DateTime"),
    attr_def(gid("nmat"), "+FinishDate", "DateTime"),
    attr_def(gid("nmat"), "+PublicKey", "string"),
    attr_def(gid("nmat"), "+UserId", "int"),
    attr_def(gid("nmat"), "+Mode", "CertificateMode"),
    attr_def(gid("nmat"), "+IsActual", "bool"),
    attr_def(gid("nmat"), "+FilePath", "string"),
])

add_class(S_SIG, S_SIG_V, S_SIG_MV, "Signature", STOR, [
    attr_def(gid("nmat"), "+Id", "int"),
    attr_def(gid("nmat"), "+SignatureValue", "string"),
    attr_def(gid("nmat"), "+UserId", "int"),
    attr_def(gid("nmat"), "+DocumentId", "int"),
    attr_def(gid("nmat"), "+Path", "string"),
    attr_def(gid("nmat"), "+CertificatePath", "string"),
    attr_def(gid("nmat"), "+IsDeleted", "bool"),
])

add_class(S_CTX, S_CTX_V, S_CTX_MV, "StorageContext", STOR, [
    attr_def(gid("nmat"), "+Users", "DbSet<User>"),
    attr_def(gid("nmat"), "+Roles", "DbSet<Role>"),
    attr_def(gid("nmat"), "+Documents", "DbSet<Document>"),
    attr_def(gid("nmat"), "+DocumentUsers", "DbSet<DocumentUser>"),
    attr_def(gid("nmat"), "+Certificates", "DbSet<Certificate>"),
    attr_def(gid("nmat"), "+Signatures", "DbSet<Signature>"),
])

add_pkg(STOR, PVID["Storage"], PMVID["Storage"], "Storage",
    [S_USER,S_ROLE,S_DOC,S_DOCU,S_CERT,S_SIG,S_CTX])

# ──────────────────────────────────────────────────────────────────────────
# CONTRACTS PACKAGE
# ──────────────────────────────────────────────────────────────────────────
CONT = PID["Contracts"]

for (cid,vid,mvid,name) in [
    (C_IULOG,  C_IULOG_V,  C_IULOG_MV,  "IUserLogic"),
    (C_IRLOG,  C_IRLOG_V,  C_IRLOG_MV,  "IRoleLogic"),
    (C_IDLOG,  C_IDLOG_V,  C_IDLOG_MV,  "IDocumentLogic"),
    (C_IDULOG, C_IDULOG_V, C_IDULOG_MV, "IDocumentUserLogic"),
    (C_ICLOG,  C_ICLOG_V,  C_ICLOG_MV,  "ICertificateLogic"),
    (C_ISLOG,  C_ISLOG_V,  C_ISLOG_MV,  "ISignatureLogic"),
    (C_IANTIV, C_IANTIV_V, C_IANTIV_MV, "IAntivirusService"),
    (C_IDSIGN, C_IDSIGN_V, C_IDSIGN_MV, "IDocumentSigner"),
    (C_ICGEN,  C_ICGEN_V,  C_ICGEN_MV,  "ICertificateGeneratorLogic"),
    (C_IFILE,  C_IFILE_V,  C_IFILE_MV,  "IFileStorage"),
    (C_IUSTO,  C_IUSTO_V,  C_IUSTO_MV,  "IUserStorage"),
    (C_IRSTO,  C_IRSTO_V,  C_IRSTO_MV,  "IRoleStorage"),
    (C_IDSTO,  C_IDSTO_V,  C_IDSTO_MV,  "IDocumentStorage"),
    (C_IDUSTO, C_IDUSTO_V, C_IDUSTO_MV, "IDocumentUserStorage"),
    (C_ICSTO,  C_ICSTO_V,  C_ICSTO_MV,  "ICertificateStorage"),
    (C_ISSTO,  C_ISSTO_V,  C_ISSTO_MV,  "ISignatureStorage"),
    (C_ISESS,  C_ISESS_V,  C_ISESS_MV,  "ISessionService"),
    (C_IMAIL,  C_IMAIL_V,  C_IMAIL_MV,  "IEmailService"),
    (C_ICODE,  C_ICODE_V,  C_ICODE_MV,  "ICodeVerificationLogic"),
]:
    add_class(cid, vid, mvid, name, CONT, [], abstract=True)

add_pkg(CONT, PVID["Contracts"], PMVID["Contracts"], "Contracts",
    [C_IULOG,C_IRLOG,C_IDLOG,C_IDULOG,C_ICLOG,C_ISLOG,
     C_IANTIV,C_IDSIGN,C_ICGEN,C_IFILE,
     C_IUSTO,C_IRSTO,C_IDSTO,C_IDUSTO,C_ICSTO,C_ISSTO,
     C_ISESS,C_IMAIL,C_ICODE])

# ──────────────────────────────────────────────────────────────────────────
# LOGIC PACKAGE
# ──────────────────────────────────────────────────────────────────────────
LOGI = PID["Logic"]

add_class(L_ULOG,L_ULOG_V,L_ULOG_MV,"UserLogic",LOGI,[
    attr_def(gid("nmat"),"+ReadListAsync(): Task<List<UserViewModel>>",""),
    attr_def(gid("nmat"),"+ReadPagedListAsync(): Task<PagedResult<UserViewModel>>",""),
    attr_def(gid("nmat"),"+CreateAsync(m): Task<UserViewModel?>",""),
    attr_def(gid("nmat"),"+UpdateAsync(m): Task<UserViewModel?>",""),
    attr_def(gid("nmat"),"+DeleteAsync(id): Task<bool>",""),
])
add_class(L_RLOG,L_RLOG_V,L_RLOG_MV,"RoleLogic",LOGI,[
    attr_def(gid("nmat"),"+ReadListAsync(): Task<List<RoleViewModel>>",""),
    attr_def(gid("nmat"),"+CreateAsync(m): Task<RoleViewModel?>",""),
    attr_def(gid("nmat"),"+UpdateAsync(m): Task<RoleViewModel?>",""),
    attr_def(gid("nmat"),"+DeleteAsync(id): Task<bool>",""),
])
add_class(L_DLOG,L_DLOG_V,L_DLOG_MV,"DocumentLogic",LOGI,[
    attr_def(gid("nmat"),"+ReadListAsync(): Task<List<DocumentViewModel>>",""),
    attr_def(gid("nmat"),"+CreateAsync(m,stream): Task<DocumentViewModel?>",""),
    attr_def(gid("nmat"),"+UpdateAsync(m): Task<DocumentViewModel?>",""),
    attr_def(gid("nmat"),"+DeleteAsync(id): Task<bool>",""),
])
add_class(L_DULOG,L_DULOG_V,L_DULOG_MV,"DocumentUserLogic",LOGI,[
    attr_def(gid("nmat"),"+GetPagedForSignAsync(userId,status): Task<PagedResult>",""),
    attr_def(gid("nmat"),"+CreateAsync(m): Task<DocumentUserViewModel?>",""),
    attr_def(gid("nmat"),"+UpdateAsync(m): Task<DocumentUserViewModel?>",""),
])
add_class(L_CLOG,L_CLOG_V,L_CLOG_MV,"CertificateLogic",LOGI,[
    attr_def(gid("nmat"),"+ReadListAsync(): Task<List<CertificateViewModel>>",""),
    attr_def(gid("nmat"),"+GenerateSelfSignedAsync(userId): Task<CertificateViewModel?>",""),
    attr_def(gid("nmat"),"+DeleteAsync(id): Task<bool>",""),
])
add_class(L_SLOG,L_SLOG_V,L_SLOG_MV,"SignatureLogic",LOGI,[
    attr_def(gid("nmat"),"+ReadListAsync(): Task<List<SignatureViewModel>>",""),
    attr_def(gid("nmat"),"+CreateAsync(m,stream): Task<SignatureViewModel?>",""),
])
add_class(L_CLAM,L_CLAM_V,L_CLAM_MV,"ClamAvService",LOGI,[
    attr_def(gid("nmat"),"+IsFileCleanAsync(file): Task<bool>",""),
    attr_def(gid("nmat"),"-host: string",""),
    attr_def(gid("nmat"),"-port: int",""),
])
add_class(L_SELFCG,L_SELFCG_V,L_SELFCG_MV,"SelfSignedCertificateGenerator",LOGI,[
    attr_def(gid("nmat"),"+GenerateSelfSignedAsync(userId,owner): Task<CertificateBindingModel>",""),
])
add_class(L_INTSIG,L_INTSIG_V,L_INTSIG_MV,"InternalDocumentSigner",LOGI,[
    attr_def(gid("nmat"),"+SignAsync(bytes,cert): Task<byte[]>",""),
])
add_class(L_LFILE,L_LFILE_V,L_LFILE_MV,"LocalFileStorage",LOGI,[
    attr_def(gid("nmat"),"+SaveOriginalAsync(stream,docId): Task<string>",""),
    attr_def(gid("nmat"),"+SaveSignatureAsync(stream,docId): Task<string>",""),
    attr_def(gid("nmat"),"+GetFileAsync(path): Task<Stream?>",""),
    attr_def(gid("nmat"),"+DeleteDocumentFolderAsync(docId): Task",""),
    attr_def(gid("nmat"),"+SaveCertificateAsync(stream,userId): Task<string>",""),
])

add_pkg(LOGI,PVID["Logic"],PMVID["Logic"],"Logic",
    [L_ULOG,L_RLOG,L_DLOG,L_DULOG,L_CLOG,L_SLOG,
     L_CLAM,L_SELFCG,L_INTSIG,L_LFILE])

# ──────────────────────────────────────────────────────────────────────────
# AUTH PACKAGE
# ──────────────────────────────────────────────────────────────────────────
AUTH = PID["Auth"]

add_class(A_SESS,A_SESS_V,A_SESS_MV,"SessionService",AUTH,[
    attr_def(gid("nmat"),"+CreateSessionAsync(userId,login): Task<UserSession>",""),
    attr_def(gid("nmat"),"+GetSessionAsync(token): Task<UserSession?>",""),
    attr_def(gid("nmat"),"+ValidateSessionAsync(token): Task<bool>",""),
    attr_def(gid("nmat"),"+DeleteSessionAsync(token): Task",""),
])
add_class(A_MAIL,A_MAIL_V,A_MAIL_MV,"EmailService",AUTH,[
    attr_def(gid("nmat"),"+SendVerificationCodeAsync(email,code): Task",""),
])
add_class(A_CODE,A_CODE_V,A_CODE_MV,"CodeVerificationLogic",AUTH,[
    attr_def(gid("nmat"),"+GenerateCode(): string",""),
    attr_def(gid("nmat"),"+SendCodeAsync(email): Task",""),
    attr_def(gid("nmat"),"+VerifyCodeAsync(email,code): Task<bool>",""),
])

add_pkg(AUTH,PVID["Auth"],PMVID["Auth"],"Auth",[A_SESS,A_MAIL,A_CODE])

# ──────────────────────────────────────────────────────────────────────────
# MESSAGING PACKAGE
# ──────────────────────────────────────────────────────────────────────────
MSGI = PID["Messaging"]

add_class(M_SREQ,M_SREQ_V,M_SREQ_MV,"SigningRequestMessage",MSGI,[
    attr_def(gid("nmat"),"+DocumentId", "int"),
    attr_def(gid("nmat"),"+UserId", "int"),
    attr_def(gid("nmat"),"+RequestedAt", "DateTime"),
])
add_class(M_NOTI,M_NOTI_V,M_NOTI_MV,"NotificationMessage",MSGI,[
    attr_def(gid("nmat"),"+UserId", "int"),
    attr_def(gid("nmat"),"+Title", "string"),
    attr_def(gid("nmat"),"+RequestedAt", "DateTime"),
])
add_class(M_SCONS,M_SCONS_V,M_SCONS_MV,"SignDocumentConsumer",MSGI,[
    attr_def(gid("nmat"),"+Consume(context): Task",""),
])
add_class(M_NCONS,M_NCONS_V,M_NCONS_MV,"NotificationConsumer",MSGI,[
    attr_def(gid("nmat"),"+Consume(context): Task",""),
])

add_pkg(MSGI,PVID["Messaging"],PMVID["Messaging"],"Messaging",
    [M_SREQ,M_NOTI,M_SCONS,M_NCONS])

# ──────────────────────────────────────────────────────────────────────────
# API PACKAGE
# ──────────────────────────────────────────────────────────────────────────
APIP = PID["API"]

add_class(P_AUTHC,P_AUTHC_V,P_AUTHC_MV,"AuthController",APIP,[
    attr_def(gid("nmat"),"+SendLoginCode(request): Task<IActionResult>",""),
    attr_def(gid("nmat"),"+VerifyLogin(request): Task<IActionResult>",""),
    attr_def(gid("nmat"),"+Logout(request): Task<IActionResult>",""),
    attr_def(gid("nmat"),"+ValidateSession(): Task<IActionResult>",""),
])
add_class(P_USRC,P_USRC_V,P_USRC_MV,"UsersController",APIP,[
    attr_def(gid("nmat"),"+GetAll(): Task<IActionResult>",""),
    attr_def(gid("nmat"),"+GetPaged(): Task<IActionResult>",""),
    attr_def(gid("nmat"),"+Create(m): Task<IActionResult>",""),
    attr_def(gid("nmat"),"+Update(m): Task<IActionResult>",""),
    attr_def(gid("nmat"),"+Delete(id): Task<IActionResult>",""),
])
add_class(P_DOCC,P_DOCC_V,P_DOCC_MV,"DocumentsController",APIP,[
    attr_def(gid("nmat"),"+GetAll(): Task<IActionResult>",""),
    attr_def(gid("nmat"),"+Create(file): Task<IActionResult>",""),
    attr_def(gid("nmat"),"+GetDocumentsForSign(): Task<IActionResult>",""),
    attr_def(gid("nmat"),"+GetVerificationPackage(id): Task<IActionResult>",""),
    attr_def(gid("nmat"),"+Delete(id): Task<IActionResult>",""),
])
add_class(P_SIGC,P_SIGC_V,P_SIGC_MV,"SigningController",APIP,[
    attr_def(gid("nmat"),"+GetSigners(docId): Task<IActionResult>",""),
    attr_def(gid("nmat"),"+SignIntent(docId): Task<IActionResult>",""),
    attr_def(gid("nmat"),"+SubmitSignature(request): Task<IActionResult>",""),
    attr_def(gid("nmat"),"+Reject(docId): Task<IActionResult>",""),
])
add_class(P_AUTHMW,P_AUTHMW_V,P_AUTHMW_MV,"AuthMiddleware",APIP,[
    attr_def(gid("nmat"),"+Invoke(ctx,sessionSvc,userLogic): Task",""),
])

add_pkg(APIP,PVID["API"],PMVID["API"],"API",
    [P_AUTHC,P_USRC,P_DOCC,P_SIGC,P_AUTHMW])

# ──────────────────────────────────────────────────────────────────────────
# DEPENDENCIES
# ──────────────────────────────────────────────────────────────────────────
# Storage → Models
add_dep(D_UIMPL, STOR, S_USER, MODS, I_USER)
add_dep(D_RIMPL, STOR, S_ROLE, MODS, I_ROLE)
add_dep(D_DIMPL, STOR, S_DOC,  MODS, I_DOC)
add_dep(D_DUIMP, STOR, S_DOCU, MODS, I_DOCU)
add_dep(D_CIMPL, STOR, S_CERT, MODS, I_CERT)
add_dep(D_SIMPL, STOR, S_SIG,  MODS, I_SIG)

# Logic implementations → Logic contracts
add_dep(D_ULIMP,  LOGI, L_ULOG,   CONT, C_IULOG)
add_dep(D_RLIMP,  LOGI, L_RLOG,   CONT, C_IRLOG)
add_dep(D_DLIMP,  LOGI, L_DLOG,   CONT, C_IDLOG)
add_dep(D_DULIMP, LOGI, L_DULOG,  CONT, C_IDULOG)
add_dep(D_CLIMP,  LOGI, L_CLOG,   CONT, C_ICLOG)
add_dep(D_SLIMP,  LOGI, L_SLOG,   CONT, C_ISLOG)
add_dep(D_CLAMI,  LOGI, L_CLAM,   CONT, C_IANTIV)
add_dep(D_SCGI,   LOGI, L_SELFCG, CONT, C_ICGEN)
add_dep(D_ISIMP,  LOGI, L_INTSIG, CONT, C_IDSIGN)
add_dep(D_LFIMP,  LOGI, L_LFILE,  CONT, C_IFILE)

# Logic → Storage contracts
add_dep(D_ULSTU, LOGI, L_ULOG,  CONT, C_IUSTO)
add_dep(D_RLSTU, LOGI, L_RLOG,  CONT, C_IRSTO)
add_dep(D_DLSTO, LOGI, L_DLOG,  CONT, C_IDSTO)
add_dep(D_DLFSF, LOGI, L_DLOG,  CONT, C_IFILE)
add_dep(D_DULST, LOGI, L_DULOG, CONT, C_IDUSTO)
add_dep(D_CLSTO, LOGI, L_CLOG,  CONT, C_ICSTO)
add_dep(D_SLSTO, LOGI, L_SLOG,  CONT, C_ISSTO)

# Auth → Auth contracts
add_dep(D_SESIMP, AUTH, A_SESS, CONT, C_ISESS)
add_dep(D_MAIMP,  AUTH, A_MAIL, CONT, C_IMAIL)
add_dep(D_CODIMP, AUTH, A_CODE, CONT, C_ICODE)

# Consumers → message contracts
add_dep(D_SCMSG, MSGI, M_SCONS, MSGI, M_SREQ)
add_dep(D_NCMSG, MSGI, M_NCONS, MSGI, M_NOTI)

# API → Logic/Auth contracts
add_dep(D_AUCAPI, APIP, P_AUTHC, CONT, C_ISESS)
add_dep(D_USAPI,  APIP, P_USRC,  CONT, C_IULOG)
add_dep(D_DCAPI,  APIP, P_DOCC,  CONT, C_IDLOG)
add_dep(D_SCAPI,  APIP, P_SIGC,  CONT, C_IDULOG)

# ═══════════════════════════════════════════════════════════════════════════
# DIAGRAM VISUAL ELEMENTS
# ═══════════════════════════════════════════════════════════════════════════

# Layout constants
DX = 10   # diagram left margin

# ── Models Package (y=10) ─────────────────────────────────────────────────
PKG_MOD_Y = 10; PKG_MOD_W = 2980; PKG_MOD_H = 420
# Row 1: interfaces (y=38 within pkg), h=200 each
INT_Y = 38; INT_H = 200
int_items = [
    (I_IID_V, "IId",             I_IID,  MODS, 20,   INT_Y,  80,  70),
    (I_USER_V,"IUserModel",      I_USER, MODS, 115,  INT_Y, 200, 200),
    (I_ROLE_V,"IRoleModel",      I_ROLE, MODS, 330,  INT_Y, 175, 115),
    (I_DOC_V, "IDocumentModel",  I_DOC,  MODS, 520,  INT_Y, 200, 200),
    (I_DOCU_V,"IDocumentUserModel",I_DOCU,MODS,735,  INT_Y, 215, 155),
    (I_CERT_V,"ICertificateModel",I_CERT,MODS, 965,  INT_Y, 215, 185),
    (I_SIG_V, "ISignatureModel", I_SIG,  MODS, 1195, INT_Y, 205, 155),
]
# Row 2: enums
ENUM_Y = 268
enum_items = [
    (E_DOCST_V,"DocumentStatus",E_DOCST,MODS, 20,  ENUM_Y, 170, 120),
    (E_SIGST_V,"SigningStatus",  E_SIGST,MODS, 210, ENUM_Y, 160, 120),
    (E_SYSRL_V,"SystemRole",     E_SYSRL,MODS, 390, ENUM_Y, 155, 100),
    (E_CERTM_V,"CertificateMode",E_CERTM,MODS, 565, ENUM_Y, 165, 85),
    (E_APPTY_V,"AppType",        E_APPTY,MODS, 750, ENUM_Y, 140, 100),
]
mod_items = int_items + enum_items
mod_vids = [v for v,*_ in mod_items]
for v, name, cid, pid, x, y, w, h in mod_items:
    add_class_view(v, name, pid, cid, PVID["Models"], x, y, w, h, RGB_MOD)
add_pkg_view(PVID["Models"],"Models",MODS, DX, PKG_MOD_Y, PKG_MOD_W, PKG_MOD_H, mod_vids, (200,230,255))

# ── Storage Package (y=450) ───────────────────────────────────────────────
PKG_STO_Y = 450
sto_items = [
    (S_USER_V,"User",         S_USER,STOR,  20,35, 250,215),
    (S_ROLE_V,"Role",         S_ROLE,STOR, 290,35, 215,140),
    (S_DOC_V, "Document",     S_DOC, STOR, 525,35, 240,200),
    (S_DOCU_V,"DocumentUser", S_DOCU,STOR, 785,35, 215,155),
    (S_CERT_V,"Certificate",  S_CERT,STOR,1020,35, 230,185),
    (S_SIG_V, "Signature",    S_SIG, STOR,1270,35, 225,185),
    (S_CTX_V, "StorageContext",S_CTX,STOR,1515,35, 250,155),
]
sto_vids = [v for v,*_ in sto_items]
for v, name, cid, pid, x, y, w, h in sto_items:
    add_class_view(v, name, pid, cid, PVID["Storage"], x, y, w, h, RGB_STO)
add_pkg_view(PVID["Storage"],"Storage",STOR, DX, PKG_STO_Y, PKG_MOD_W, 280, sto_vids, (195,255,215))

# ── Contracts Package (y=750) ─────────────────────────────────────────────
PKG_CON_Y = 750
IFACE_H = 50; IFACE_W = 240
# Row 1: Logic contracts  (y=35)
# Row 2: Storage contracts (y=115)
# Row 3: Auth contracts (y=195)
con_items = [
    # row 1 - logic contracts
    (C_IULOG_V, "IUserLogic",              C_IULOG, CONT,  20, 35,IFACE_W,IFACE_H),
    (C_IRLOG_V, "IRoleLogic",              C_IRLOG, CONT, 275, 35,IFACE_W,IFACE_H),
    (C_IDLOG_V, "IDocumentLogic",          C_IDLOG, CONT, 530, 35,IFACE_W,IFACE_H),
    (C_IDULOG_V,"IDocumentUserLogic",      C_IDULOG,CONT, 785, 35,IFACE_W,IFACE_H),
    (C_ICLOG_V, "ICertificateLogic",       C_ICLOG, CONT,1040, 35,IFACE_W,IFACE_H),
    (C_ISLOG_V, "ISignatureLogic",         C_ISLOG, CONT,1295, 35,IFACE_W,IFACE_H),
    (C_IANTIV_V,"IAntivirusService",       C_IANTIV,CONT,1550, 35,IFACE_W,IFACE_H),
    (C_IDSIGN_V,"IDocumentSigner",         C_IDSIGN,CONT,1805, 35,IFACE_W,IFACE_H),
    (C_ICGEN_V, "ICertificateGeneratorLogic",C_ICGEN,CONT,2060,35,265,    IFACE_H),
    (C_IFILE_V, "IFileStorage",            C_IFILE, CONT,2340, 35,200,    IFACE_H),
    # row 2 - storage contracts
    (C_IUSTO_V, "IUserStorage",            C_IUSTO, CONT,  20,105,IFACE_W,IFACE_H),
    (C_IRSTO_V, "IRoleStorage",            C_IRSTO, CONT, 275,105,IFACE_W,IFACE_H),
    (C_IDSTO_V, "IDocumentStorage",        C_IDSTO, CONT, 530,105,IFACE_W,IFACE_H),
    (C_IDUSTO_V,"IDocumentUserStorage",    C_IDUSTO,CONT, 785,105,260,    IFACE_H),
    (C_ICSTO_V, "ICertificateStorage",     C_ICSTO, CONT,1060,105,250,    IFACE_H),
    (C_ISSTO_V, "ISignatureStorage",       C_ISSTO, CONT,1325,105,240,    IFACE_H),
    # row 3 - auth contracts
    (C_ISESS_V, "ISessionService",         C_ISESS, CONT,  20,175,220,    IFACE_H),
    (C_IMAIL_V, "IEmailService",           C_IMAIL, CONT, 255,175,190,    IFACE_H),
    (C_ICODE_V, "ICodeVerificationLogic",  C_ICODE, CONT, 460,175,260,    IFACE_H),
]
con_vids = [v for v,*_ in con_items]
for v, name, cid, pid, x, y, w, h in con_items:
    add_class_view(v, name, pid, cid, PVID["Contracts"], x, y, w, h, RGB_CON)
add_pkg_view(PVID["Contracts"],"Contracts",CONT, DX, PKG_CON_Y, PKG_MOD_W, 255, con_vids, (230,210,255))

# ── Logic Package (y=1025) ────────────────────────────────────────────────
PKG_LOG_Y = 1025
log_items = [
    # row 1 - main logic (y=35)
    (L_ULOG_V,  "UserLogic",            L_ULOG,  LOGI,  20,35, 260,135),
    (L_RLOG_V,  "RoleLogic",            L_RLOG,  LOGI, 295,35, 245,120),
    (L_DLOG_V,  "DocumentLogic",        L_DLOG,  LOGI, 555,35, 260,120),
    (L_DULOG_V, "DocumentUserLogic",    L_DULOG, LOGI, 830,35, 275,105),
    (L_CLOG_V,  "CertificateLogic",     L_CLOG,  LOGI,1120,35, 250,105),
    (L_SLOG_V,  "SignatureLogic",       L_SLOG,  LOGI,1385,35, 240, 90),
    # row 2 - auxiliary (y=190)
    (L_CLAM_V,  "ClamAvService",        L_CLAM,  LOGI,  20,195, 220, 90),
    (L_SELFCG_V,"SelfSignedCertificateGenerator",L_SELFCG,LOGI,260,195,335,75),
    (L_INTSIG_V,"InternalDocumentSigner",L_INTSIG,LOGI,615,195,260, 75),
    (L_LFILE_V, "LocalFileStorage",     L_LFILE, LOGI, 895,195,280,120),
]
log_vids = [v for v,*_ in log_items]
for v, name, cid, pid, x, y, w, h in log_items:
    add_class_view(v, name, pid, cid, PVID["Logic"], x, y, w, h, RGB_LOG)
add_pkg_view(PVID["Logic"],"Logic",LOGI, DX, PKG_LOG_Y, PKG_MOD_W, 340, log_vids, (255,255,200))

# ── Auth Package (y=1385) ─────────────────────────────────────────────────
PKG_AUTH_Y = 1385
auth_items = [
    (A_SESS_V,"SessionService",       A_SESS,AUTH, 20,35,250,120),
    (A_MAIL_V,"EmailService",         A_MAIL,AUTH,290,35,220, 75),
    (A_CODE_V,"CodeVerificationLogic",A_CODE,AUTH,530,35,255, 90),
]
auth_vids = [v for v,*_ in auth_items]
for v, name, cid, pid, x, y, w, h in auth_items:
    add_class_view(v, name, pid, cid, PVID["Auth"], x, y, w, h, RGB_AUTH)
add_pkg_view(PVID["Auth"],"Auth",AUTH, DX, PKG_AUTH_Y, 815, 190, auth_vids, (255,225,195))

# ── Messaging Package (y=1385, x=835) ────────────────────────────────────
PKG_MSG_Y = 1385; PKG_MSG_X = 835
msg_items = [
    (M_SREQ_V, "SigningRequestMessage", M_SREQ, MSGI, 20,35,230, 90),
    (M_NOTI_V, "NotificationMessage",   M_NOTI, MSGI,265,35,220, 90),
    (M_SCONS_V,"SignDocumentConsumer",  M_SCONS,MSGI,505,35,240, 75),
    (M_NCONS_V,"NotificationConsumer",  M_NCONS,MSGI,765,35,230, 75),
]
msg_vids = [v for v,*_ in msg_items]
for v, name, cid, pid, x, y, w, h in msg_items:
    add_class_view(v, name, pid, cid, PVID["Messaging"], x, y, w, h, RGB_MSG)
add_pkg_view(PVID["Messaging"],"Messaging",MSGI, PKG_MSG_X, PKG_MSG_Y, 1010, 190, msg_vids, (255,210,225))

# ── API Package (y=1385, x=1855) ─────────────────────────────────────────
PKG_API_Y = 1385; PKG_API_X = 1855
api_items = [
    (P_AUTHC_V, "AuthController",       P_AUTHC, APIP,  20,35, 240,120),
    (P_USRC_V,  "UsersController",      P_USRC,  APIP, 275,35, 240,135),
    (P_DOCC_V,  "DocumentsController",  P_DOCC,  APIP, 530,35, 265,150),
    (P_SIGC_V,  "SigningController",    P_SIGC,  APIP, 810,35, 245,120),
    (P_AUTHMW_V,"AuthMiddleware",       P_AUTHMW,APIP,1070,35, 220, 75),
]
api_vids = [v for v,*_ in api_items]
for v, name, cid, pid, x, y, w, h in api_items:
    add_class_view(v, name, pid, cid, PVID["API"], x, y, w, h, RGB_API)
add_pkg_view(PVID["API"],"API",APIP, PKG_API_X, PKG_API_Y, 1310, 190, api_vids, (215,225,215))

# ── Dependency connectors ─────────────────────────────────────────────────
dep_pairs = [
    # Storage → Models
    (D_UIMPL, S_USER_V, I_USER_V),
    (D_RIMPL, S_ROLE_V, I_ROLE_V),
    (D_DIMPL, S_DOC_V,  I_DOC_V),
    (D_DUIMP, S_DOCU_V, I_DOCU_V),
    (D_CIMPL, S_CERT_V, I_CERT_V),
    (D_SIMPL, S_SIG_V,  I_SIG_V),
    # Logic → Logic contracts
    (D_ULIMP,  L_ULOG_V,   C_IULOG_V),
    (D_RLIMP,  L_RLOG_V,   C_IRLOG_V),
    (D_DLIMP,  L_DLOG_V,   C_IDLOG_V),
    (D_DULIMP, L_DULOG_V,  C_IDULOG_V),
    (D_CLIMP,  L_CLOG_V,   C_ICLOG_V),
    (D_SLIMP,  L_SLOG_V,   C_ISLOG_V),
    (D_CLAMI,  L_CLAM_V,   C_IANTIV_V),
    (D_SCGI,   L_SELFCG_V, C_ICGEN_V),
    (D_ISIMP,  L_INTSIG_V, C_IDSIGN_V),
    (D_LFIMP,  L_LFILE_V,  C_IFILE_V),
    # Logic → Storage contracts
    (D_ULSTU, L_ULOG_V,  C_IUSTO_V),
    (D_RLSTU, L_RLOG_V,  C_IRSTO_V),
    (D_DLSTO, L_DLOG_V,  C_IDSTO_V),
    (D_DLFSF, L_DLOG_V,  C_IFILE_V),
    (D_DULST, L_DULOG_V, C_IDUSTO_V),
    (D_CLSTO, L_CLOG_V,  C_ICSTO_V),
    (D_SLSTO, L_SLOG_V,  C_ISSTO_V),
    # Auth
    (D_SESIMP, A_SESS_V, C_ISESS_V),
    (D_MAIMP,  A_MAIL_V, C_IMAIL_V),
    (D_CODIMP, A_CODE_V, C_ICODE_V),
    # Consumers
    (D_SCMSG, M_SCONS_V, M_SREQ_V),
    (D_NCMSG, M_NCONS_V, M_NOTI_V),
    # API
    (D_AUCAPI, P_AUTHC_V, C_ISESS_V),
    (D_USAPI,  P_USRC_V,  C_IULOG_V),
    (D_DCAPI,  P_DOCC_V,  C_IDLOG_V),
    (D_SCAPI,  P_SIGC_V,  C_IDULOG_V),
]
for dep_tuple, from_v, to_v in dep_pairs:
    add_dep_view(dep_tuple, from_v, to_v)

# ═══════════════════════════════════════════════════════════════════════════
# COLLECT ALL IDs FOR DIAGRAM DEFINITION
# ═══════════════════════════════════════════════════════════════════════════

all_pkg_view_ids = list(PVID.values())
all_cls_view_ids = [v for (_, shape, _, _, _, _) in diagram_rows for v in [] if shape == "Class"]
# Collect all class view IDs from diagram_rows
_cls_vids = [vid for (vid, stype, _, _, _, _) in diagram_rows if stype == "Class"]
dep_view_ids = [t[1] for t in [D_UIMPL,D_RIMPL,D_DIMPL,D_DUIMP,D_CIMPL,D_SIMPL,
    D_ULIMP,D_RLIMP,D_DLIMP,D_DULIMP,D_CLIMP,D_SLIMP,D_CLAMI,D_SCGI,D_ISIMP,D_LFIMP,
    D_ULSTU,D_RLSTU,D_DLSTO,D_DLFSF,D_DULST,D_CLSTO,D_SLSTO,
    D_SESIMP,D_MAIMP,D_CODIMP,D_SCMSG,D_NCMSG,
    D_AUCAPI,D_USAPI,D_DCAPI,D_SCAPI]]
all_dep_ids = [t[0] for t in [D_UIMPL,D_RIMPL,D_DIMPL,D_DUIMP,D_CIMPL,D_SIMPL,
    D_ULIMP,D_RLIMP,D_DLIMP,D_DULIMP,D_CLIMP,D_SLIMP,D_CLAMI,D_SCGI,D_ISIMP,D_LFIMP,
    D_ULSTU,D_RLSTU,D_DLSTO,D_DLFSF,D_DULST,D_CLSTO,D_SLSTO,
    D_SESIMP,D_MAIMP,D_CODIMP,D_SCMSG,D_NCMSG,
    D_AUCAPI,D_USAPI,D_DCAPI,D_SCAPI]]
# DIAGRAM.Child must include ALL elements: packages + classes + connectors
all_child_view_ids = all_pkg_view_ids + _cls_vids + dep_view_ids

# ═══════════════════════════════════════════════════════════════════════════
# WRITE TO DATABASE
# ═══════════════════════════════════════════════════════════════════════════

conn = sqlite3.connect(VPP_FILE)
c = conn.cursor()

# 1. Remove old user content
old_pkg_ids   = ["Ua7WDNmFYEzgAQtj","sV72DNmFYEzgAQwT","U2vWDNmFYEzgAQuE"]
old_class_ids = ["GzqODNmFYEzgAQxl","xf.uDNmFYEzgAQ1I","p8uODNmFYEzgAQx6",
                 "B7GODNmFYEzgAQxy","E6cuDNmFYEzgAQ0d","Gm8ODNmFYEzgAQxd",
                 "gjc2DNmFYEzgAQu8",".k1mDNmFYEzgAQsS","GNZmDNmFYEzgAQsK",
                 "sXOmDNmFYEzgAQr0","OoBmDNmFYEzgAQr6","q8pmDNmFYEzgAQsC"]
old_dep_ids   = ["jKca4tmFYEzgARix","E7za4tmFYEzgARlH","kyPa4tmFYEzgARlZ",
                 "SdK64tmFYEzgARmL","2aje4tmFYEzgARwN","6wze4tmFYEzgARwT",
                 "BCne4tmFYEzgARwd","ilve4tmFYEzgARwl","hOI.4tmFYEzgARwx",
                 "mOE.4tmFYEzgARw3","XnG.4tmFYEzgARxV","aex.4tmFYEzgARxf",
                 "5V9.4tmFYEzgARx1","ZXD.4tmFYEzgARx5"]
old_assoc_ids = ["5raa4tmFYEzgARjr","JTBa4tmFYEzgARkT","_wpa4tmFYEzgARkf",
                 "u05a4tmFYEzgARkp","Pic64tmFYEzgARl9"]

c.execute("DELETE FROM DIAGRAM_ELEMENT WHERE DIAGRAM_ID=?", (DGRAM,))
all_old = old_pkg_ids + old_class_ids + old_dep_ids + old_assoc_ids
for eid in all_old:
    c.execute("DELETE FROM MODEL_ELEMENT WHERE ID=? OR PARENT_ID=?", (eid, eid))

# 2. Insert new model elements
for (eid, uid, parent_id, mtype, name, defn) in model_rows:
    c.execute(
        "INSERT INTO MODEL_ELEMENT (ID, USER_ID, USER_ID_PARENT, MODEL_TYPE, PARENT_ID, NAME, DEFINITION) "
        "VALUES (?, NULL, NULL, ?, ?, ?, ?)",
        (eid, mtype, parent_id, name, sqlite3.Binary(defn.encode('utf-8')))
    )

# 3. Insert new diagram elements
for (vid, shape_type, diagram_id, model_el_id, parent_id, defn) in diagram_rows:
    c.execute(
        "INSERT INTO DIAGRAM_ELEMENT (ID, SHAPE_TYPE, DIAGRAM_ID, MODEL_ELEMENT_ID, PARENT_ID, DEFINITION) "
        "VALUES (?, ?, ?, ?, ?, ?)",
        (vid, shape_type, diagram_id, model_el_id, parent_id, sqlite3.Binary(defn.encode('utf-8')))
    )

# 4. Update Dependency container definition
dep_children = ",\n\t\t".join(
    f"<{RELS}:{DEP_C}:{did}>" for did in all_dep_ids
)
new_dep_container = (
    f'{DEP_C}:"Dependency":ModelRelationshipContainer {{\n'
    f'\tpmLastModified="{TS}";\n\tpmAuthor="1";\n'
    f'\tChild=(\n\t\t{dep_children}\n\t);\n'
    f'\tpmCreateDateTime="{TS}";\n\t_modelViews=NULL;\n'
    f'\tlastModifiedTime={TS};\n\t_modelEditable=T;\n}}'
)
c.execute("UPDATE MODEL_ELEMENT SET DEFINITION=? WHERE ID=?", (sqlite3.Binary(new_dep_container.encode('utf-8')), DEP_C))

# Clear old Association container
new_assoc_container = (
    f'{ASSOC_C}:"Association":ModelRelationshipContainer {{\n'
    f'\tpmLastModified="{TS}";\n\tpmAuthor="1";\n'
    f'\tChild=();\n'
    f'\tpmCreateDateTime="{TS}";\n\t_modelViews=NULL;\n'
    f'\tlastModifiedTime={TS};\n\t_modelEditable=T;\n}}'
)
c.execute("UPDATE MODEL_ELEMENT SET DEFINITION=? WHERE ID=?", (sqlite3.Binary(new_assoc_container.encode('utf-8')), ASSOC_C))

# 5. Update DIAGRAM definition (Child list)
child_list = ",\n\t\t".join(f"<{DGRAM}:{v}>" for v in all_child_view_ids)
new_diagram_def = (
    f'{DGRAM}:"Class Diagram":ClassDiagram {{\n'
    f'\tpaintConnectorThroughLabel=1;\n\t_shapeGroups=NULL;\n'
    f'\tdiagramBackground=(255, 255, 255, 255);\n'
    f'\tconnectorLabelOrientation=0;\n\tshowEllipsisForUnshownClassMembers=2;\n'
    f'\tshowPackageNameStyle=0;\n\tshowDefaultPackage=T;\n'
    f'\tpointConnectorEndToCompartmentMember=T;\n'
    f'\tshowAttributeGetterSetter=F;\n\tautoFitShapesSize=F;\n'
    f'\tconnectorStyle=1;\n\tgeneralizationSetNotation=2;\n'
    f'\t_globalPaletteOption=T;\n\tconnectionPointStyle=0;\n'
    f'\tgridWidth=10;\n\tpmCreateDateTime="{TS}";\n'
    f'\talignToGrid=F;\n'
    f'\tsuppressImplied1MultiplicityForAttributeAndAssociationEnd=F;\n'
    f'\tpmLastModified="{TS}";\n\thiddenDiagramElementIds=NULL;\n'
    f'\tgridVisible=F;\n\tzoomRatio=0.4;\n\tpmAuthor="1";\n'
    f'\tconnectorLineJumpsSize=0;\n\tshowStereotypes=T;\n'
    f'\tshowClassEmptyCompartments=2;\n\tshapePresentationOption=0;\n'
    f'\tconnectorModelElementNameAlignment=4;\n\tvoiceIds=NULL;\n'
    f'\tshowAssociationNavigationArrows=3;\n\tinitializeDiagramForCreate=T;\n'
    f'\treferenceMappingReferencedElementIds=NULL;\n'
    f'\tChild=(\n\t\t{child_list}\n\t);\n'
    f'\treferenceMappingElementIds=NULL;\n'
    f'\tmodelElementNameAlignment=4;\n'
    f'\tshowActivityStateNodeCaption=524287;\n'
    f'\tconnectorLineJumps=0;\n\tgridHeight=10;\n'
    f'\tgridColor=(192, 192, 192, 255);\n'
    f'\tshowModelElementIdModelTypes=NULL;\n}}'
)
c.execute("UPDATE DIAGRAM SET DEFINITION=?, NAME=? WHERE ID=?",
          (sqlite3.Binary(new_diagram_def.encode('utf-8')), "Class Diagram", DGRAM))

# 6. Update PROJECT_INFO root model elements to include new packages
new_pkg_refs = "\n\t\t".join(f"<{pid}>," for pid in PID.values())

# Read and update the ROOT_MODEL_ELEMENTS_DEFINITION
c.execute("SELECT ROOT_MODEL_ELEMENTS_DEFINITION FROM PROJECT_INFO LIMIT 1")
row = c.fetchone()
if row:
    rme_def = row[0]
    # Remove old package refs and add new ones
    for old_id in old_pkg_ids:
        rme_def = rme_def.replace(f"<{old_id}>, \n\t\t", "")
        rme_def = rme_def.replace(f"<{old_id}>", "")
    # Insert new package IDs before the closing );
    insert_str = "\n\t\t" + "\n\t\t".join(f"<{pid}>," for pid in PID.values())
    rme_def = rme_def.replace(")\n}", insert_str + "\n\t)\n}")
    c.execute("UPDATE PROJECT_INFO SET ROOT_MODEL_ELEMENTS_DEFINITION=?", (rme_def,))

conn.commit()
conn.close()

print(f"Done! Inserted {len(model_rows)} model elements and {len(diagram_rows)} diagram elements.")
print(f"Packages: {list(PID.keys())}")
print("Class diagram rebuilt successfully.")
