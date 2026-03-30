#!/usr/bin/env python3
"""
Update class.vpp diagram:
1. Add fields/methods to existing classes (Logic, Auth, Messaging, API, Contracts)
2. Add BindingModels, ViewModels, SearchModels packages with all their classes
"""
import sqlite3, shutil, re

VPP   = r"c:\Users\1\source\repos\NewDiplom\class.vpp"
DGRAM = "kt2mDNmFYEzgAQro"
TS    = "1774550000000"
TS_S  = 1774550000

shutil.copy(VPP, VPP + ".bak2")

_cnt = {}
def gid(p):
    p = (p + "0000")[:4]
    _cnt[p] = _cnt.get(p, 0) + 1
    return f"{p}{_cnt[p]:012d}"

def attr_def(name, type_str=""):
    aid = gid("nmat")
    t = f'\ttype_string="{type_str}";\n' if type_str else ""
    return (f'{{{aid}:"{name}":Attribute {{\n'
            f'\t_modelEditable=T;\n{t}'
            f'\tpmLastModified="{TS}";\n\tpmAuthor="1";\n'
            f'\tpmCreateDateTime="{TS}";\n\t_modelViews=NULL;\n'
            f'\tlastModifiedTime={TS};\n}}}}')

def replace_children(defn, attr_list):
    """Replace or insert Child=() block in a VP class definition."""
    child_text = ", \n\t\t".join(attr_list)
    new_child = f"\n\tChild=(\n\t\t{child_text}\n\t);"
    if "\tChild=(" in defn:
        # find start
        start = defn.find("\n\tChild=(")
        if start == -1: start = defn.find("\tChild=(") - 1
        # track depth to find matching );
        i = defn.find("Child=(", start) + 7
        depth = 1
        while i < len(defn) and depth > 0:
            if defn[i] == '(': depth += 1
            elif defn[i] == ')': depth -= 1
            i += 1
        end = i
        if end < len(defn) and defn[end] == ';': end += 1
        defn = defn[:start] + new_child + defn[end:]
    else:
        ins = defn.rfind("\n\tpmLastModified")
        if ins == -1: ins = len(defn) - 1
        defn = defn[:ins] + new_child + defn[ins:]
    return defn

def _fill(r, g, b):
    return (f"(\n\t\t@gradientStyle=1;, \n\t\t@transparency=0;, \n\t\t@type=1;, \n"
            f"\t\t@color1=(\n\t\t\t{r}, \n\t\t\t{g}, \n\t\t\t{b}, \n\t\t\t255\n\t\t);\n\t)")
def _font():
    return '(\n\t\t@name="Dialog";, \n\t\t@color=(0, 0, 0, 255);, \n\t\t@size=11;, \n\t\t@style=0;\n\t)'
def _line():
    return '(\n\t\t@cap=0;, \n\t\t@transparency=0;, \n\t\t@weight=1.0;, \n\t\t@color=(0, 0, 0, 255);, \n\t\t@hasStroke=T;\n\t)'

# ──────────────────────────────────────────────────────────────────────────
conn = sqlite3.connect(VPP)
c = conn.cursor()

def get_id(name, mtype="Class"):
    c.execute("SELECT ID FROM MODEL_ELEMENT WHERE NAME=? AND MODEL_TYPE=?", (name, mtype))
    r = c.fetchone()
    return r[0] if r else None

def get_view_id(model_id):
    c.execute("SELECT ID FROM DIAGRAM_ELEMENT WHERE MODEL_ELEMENT_ID=? AND SHAPE_TYPE='Class'", (model_id,))
    r = c.fetchone()
    return r[0] if r else None

def update_class(name, attrs):
    cid = get_id(name)
    if not cid:
        print(f"  SKIP (not found): {name}")
        return
    c.execute("SELECT DEFINITION FROM MODEL_ELEMENT WHERE ID=?", (cid,))
    blob = c.fetchone()[0]
    defn = blob.decode("utf-8") if isinstance(blob, bytes) else blob
    attr_defs = [attr_def(n, t) for n, t in attrs]
    new_defn = replace_children(defn, attr_defs)
    c.execute("UPDATE MODEL_ELEMENT SET DEFINITION=? WHERE ID=?",
              (sqlite3.Binary(new_defn.encode("utf-8")), cid))
    # Update DIAGRAM_ELEMENT height
    n_attrs = len(attrs)
    new_h = max(55, 20 + n_attrs * 16 + 10)
    vid = get_view_id(cid)
    if vid:
        c.execute("SELECT DEFINITION FROM DIAGRAM_ELEMENT WHERE ID=?", (vid,))
        vblob = c.fetchone()[0]
        vdefn = vblob.decode("utf-8") if isinstance(vblob, bytes) else vblob
        # Replace height=OLD with height=NEW
        vdefn = re.sub(r'\bheight=\d+;', f'height={new_h};', vdefn, count=1)
        # Replace captionUIModel height
        vdefn = re.sub(r'(@height=)\d+(;, @side=12)', rf'\g<1>{new_h}\2', vdefn)
        # Replace width in captionUIModel  
        c.execute("UPDATE DIAGRAM_ELEMENT SET DEFINITION=? WHERE ID=?",
                  (sqlite3.Binary(vdefn.encode("utf-8")), vid))
    print(f"  Updated: {name} ({n_attrs} attrs, h={new_h})")

# ════════════════════════════════════════════════════════════════════
# 1. UPDATE EXISTING CLASSES
# ════════════════════════════════════════════════════════════════════
print("\n=== Updating API Controllers ===")

update_class("AuthController", [
    ("-_userLogic", "IUserLogic"),
    ("-_codeVerificationLogic", "ICodeVerificationLogic"),
    ("-_sessionService", "ISessionService"),
    ("+SendLoginCode(request: LoginRequest): Task<IActionResult>", ""),
    ("+VerifyLogin(request: VerifyLoginRequest): Task<IActionResult>", ""),
    ("+Logout(request: LogoutRequest): Task<IActionResult>", ""),
    ("+ValidateSession(authorization: string): Task<IActionResult>", ""),
])

update_class("UsersController", [
    ("-_userLogic", "IUserLogic"),
    ("+GetAll(): Task<IActionResult>", ""),
    ("+GetById(id: int): Task<IActionResult>", ""),
    ("+Create(model: UserBindingModel): Task<IActionResult>", ""),
    ("+Update(model: UserBindingModel): Task<IActionResult>", ""),
    ("+Delete(id: int): Task<IActionResult>", ""),
    ("+FilterByFullname(fullname: string): Task<IActionResult>", ""),
    ("+GetPaged(pageNumber: int, pageSize: int): Task<IActionResult>", ""),
])

update_class("DocumentsController", [
    ("-_documentLogic", "IDocumentLogic"),
    ("-_documentUserLogic", "IDocumentUserLogic"),
    ("-_signatureStorage", "ISignatureStorage"),
    ("-_userLogic", "IUserLogic"),
    ("-_fileStorage", "IFileStorage"),
    ("-_antivirus", "IAntivirusService"),
    ("-_filePolicy", "FileUploadPolicy"),
    ("-_publishEndpoint", "IPublishEndpoint"),
    ("+GetAll(): Task<IActionResult>", ""),
    ("+GetById(id: int): Task<IActionResult>", ""),
    ("+Create(title, description, userIds, isSequential, file): Task<IActionResult>", ""),
    ("+Update(model: DocumentBindingModel): Task<IActionResult>", ""),
    ("+Delete(id: int): Task<IActionResult>", ""),
    ("+Filter(title?, createdByUserId?, status?, isDeleted?): Task<IActionResult>", ""),
    ("+GetPaged(pageNumber, pageSize): Task<IActionResult>", ""),
    ("+GetFile(id: int): Task<IActionResult>", ""),
    ("+GetDocumentsForSign(signingStatus?, pageNumber, pageSize): Task<IActionResult>", ""),
    ("+GetVerificationPackage(id: int): Task<IActionResult>", ""),
])

update_class("SigningController", [
    ("-_documentUserLogic", "IDocumentUserLogic"),
    ("-_userLogic", "IUserLogic"),
    ("-_publishEndpoint", "IPublishEndpoint"),
    ("-_signatureStorage", "ISignatureStorage"),
    ("-_fileStorage", "IFileStorage"),
    ("+GetSigners(id: int): Task<IActionResult>", ""),
    ("+SignIntent(id: int): Task<IActionResult>", ""),
    ("+Reject(id: int): Task<IActionResult>", ""),
    ("+SubmitSignature(id: int, request: SubmitSignatureRequest): Task<IActionResult>", ""),
])

update_class("AuthMiddleware", [
    ("+Invoke(ctx: HttpContext, sessionSvc: ISessionService, userLogic: IUserLogic): Task", ""),
])

print("\n=== Updating Logic classes ===")

update_class("UserLogic", [
    ("-_userStorage", "IUserStorage"),
    ("+ReadListAsync(model: UserSearchModel?): Task<List<UserViewModel>?>", ""),
    ("+ReadPagedListAsync(model: UserSearchModel): Task<List<UserViewModel>?>", ""),
    ("+ReadListByFullnameContainsAsync(model: UserSearchModel): Task<List<UserViewModel>?>", ""),
    ("+ReadElementAsync(model: UserSearchModel): Task<UserViewModel?>", ""),
    ("+CreateAsync(model: UserBindingModel): Task<bool>", ""),
    ("+UpdateAsync(model: UserBindingModel): Task<bool>", ""),
    ("+DeleteAsync(model: UserBindingModel): Task<bool>", ""),
])

update_class("RoleLogic", [
    ("-_roleStorage", "IRoleStorage"),
    ("-_userStorage", "IUserStorage"),
    ("+ReadListAsync(model: RoleSearchModel?): Task<List<RoleViewModel>?>", ""),
    ("+ReadPagedListAsync(model: RoleSearchModel): Task<List<RoleViewModel>?>", ""),
    ("+ReadListByNameContainsAsync(model: RoleSearchModel): Task<List<RoleViewModel>?>", ""),
    ("+ReadElementAsync(model: RoleSearchModel): Task<RoleViewModel?>", ""),
    ("+CreateAsync(model: RoleBindingModel): Task<bool>", ""),
    ("+UpdateAsync(model: RoleBindingModel): Task<bool>", ""),
    ("+DeleteAsync(model: RoleBindingModel): Task<bool>", ""),
])

update_class("DocumentLogic", [
    ("-_documentStorage", "IDocumentStorage"),
    ("-_fileStorage", "IFileStorage"),
    ("+ReadListAsync(model: DocumentSearchModel?): Task<List<DocumentViewModel>?>", ""),
    ("+ReadPagedListAsync(model: DocumentSearchModel): Task<List<DocumentViewModel>?>", ""),
    ("+ReadElementAsync(model: DocumentSearchModel): Task<DocumentViewModel?>", ""),
    ("+CreateAsync(model: DocumentBindingModel, file: Stream, ext: string): Task<bool>", ""),
    ("+UpdateAsync(model: DocumentBindingModel): Task<bool>", ""),
    ("+DeleteAsync(model: DocumentBindingModel): Task<bool>", ""),
])

update_class("DocumentUserLogic", [
    ("-_documentUserStorage", "IDocumentUserStorage"),
    ("+ReadListAsync(model: DocumentUserSearchModel?): Task<List<DocumentUserViewModel>?>", ""),
    ("+ReadElementAsync(model: DocumentUserSearchModel): Task<DocumentUserViewModel?>", ""),
    ("+CreateAsync(model: DocumentUserBindingModel): Task<bool>", ""),
    ("+UpdateAsync(model: DocumentUserBindingModel): Task<bool>", ""),
    ("+DeleteAsync(model: DocumentUserBindingModel): Task<bool>", ""),
    ("+GetPagedForSignAsync(userId: int, status: SigningStatus?, page: int, size: int): Task<PagedResult<DocumentForSignViewModel>>", ""),
])

update_class("CertificateLogic", [
    ("-_certificateStorage", "ICertificateStorage"),
    ("-_generator", "ICertificateGeneratorLogic"),
    ("+ReadListAsync(model: CertificateSearchModel?): Task<List<CertificateViewModel>?>", ""),
    ("+ReadElementAsync(model: CertificateSearchModel): Task<CertificateViewModel?>", ""),
    ("+CreateAsync(model: CertificateBindingModel): Task<bool>", ""),
    ("+UpdateAsync(model: CertificateBindingModel): Task<bool>", ""),
    ("+DeleteAsync(model: CertificateBindingModel): Task<bool>", ""),
    ("+GenerateSelfSignedAsync(userId: int, owner: string, publisher: string): Task<CertificateViewModel?>", ""),
])

update_class("SignatureLogic", [
    ("-_signatureStorage", "ISignatureStorage"),
    ("-_fileStorage", "IFileStorage"),
    ("+ReadListAsync(model: SignatureSearchModel?): Task<List<SignatureViewModel>?>", ""),
    ("+ReadElementAsync(model: SignatureSearchModel): Task<SignatureViewModel?>", ""),
    ("+CreateAsync(model: SignatureBindingModel, file: Stream): Task<bool>", ""),
    ("+UpdateAsync(model: SignatureBindingModel): Task<bool>", ""),
    ("+DeleteAsync(model: SignatureBindingModel): Task<bool>", ""),
])

update_class("ClamAvService", [
    ("-_host", "string"),
    ("-_port", "int"),
    ("+IsFileCleanAsync(stream: Stream): Task<bool>", ""),
])

update_class("SelfSignedCertificateGenerator", [
    ("-_fileStorage", "IFileStorage"),
    ("+GenerateSelfSignedAsync(userId: int, owner: string, publisher: string): Task<CertificateBindingModel>", ""),
])

update_class("InternalDocumentSigner", [
    ("-_fileStorage", "IFileStorage"),
    ("+SignAsync(documentBytes: byte[], certificate: CertificateViewModel): Task<byte[]>", ""),
])

update_class("LocalFileStorage", [
    ("-_configuration", "IConfiguration"),
    ("+SaveOriginalAsync(stream: Stream, docId: int, ext: string): Task<string>", ""),
    ("+SaveSignatureAsync(stream: Stream, docId: int, signId: int): Task<string>", ""),
    ("+SaveCertificateAsync(stream: Stream, userId: int): Task<string>", ""),
    ("+GetFileAsync(path: string): Task<Stream?>", ""),
    ("+GetCertificateBytesAsync(path: string): Task<byte[]>", ""),
    ("+DeleteDocumentFolderAsync(docId: int): Task", ""),
    ("+DeleteCertificateAsync(path: string): Task", ""),
])

print("\n=== Updating Auth classes ===")

update_class("SessionService", [
    ("-_cache", "IDistributedCache"),
    ("-_sessionPrefix", "string"),
    ("+CreateSessionAsync(userId: int, username: string): Task<string>", ""),
    ("+GetSessionAsync(sessionId: string): Task<UserSession>", ""),
    ("+ValidateSessionAsync(sessionId: string): Task<(bool, string)>", ""),
    ("+DeleteSessionAsync(sessionId: string): Task<bool>", ""),
])

update_class("EmailService", [
    ("-_emailSettings", "EmailSettings"),
    ("+SendVerificationCodeAsync(email: string, code: string): Task<bool>", ""),
])

update_class("CodeVerificationLogic", [
    ("-_cache", "IDistributedCache"),
    ("-_emailService", "IEmailService"),
    ("-_settings", "RedisSettings"),
    ("+GenerateCode(): string", ""),
    ("+SendCodeAsync(email: string): Task<(bool, string)>", ""),
    ("+VerifyCodeAsync(email: string, code: string): Task<(bool, string)>", ""),
])

print("\n=== Updating Messaging classes ===")

update_class("SignDocumentConsumer", [
    ("-_documentStorage", "IDocumentStorage"),
    ("-_certificateStorage", "ICertificateStorage"),
    ("-_documentUserStorage", "IDocumentUserStorage"),
    ("-_signatureStorage", "ISignatureStorage"),
    ("-_fileStorage", "IFileStorage"),
    ("-_documentSigner", "IDocumentSigner"),
    ("-_publishEndpoint", "IPublishEndpoint"),
    ("+Consume(context: ConsumeContext<SigningRequestMessage>): Task", ""),
])

update_class("NotificationConsumer", [
    ("-_userStorage", "IUserStorage"),
    ("-_emailSettings", "EmailSettings"),
    ("+Consume(context: ConsumeContext<NotificationMessage>): Task", ""),
    ("+SendNotificationMessage(email: string, title: string): Task<bool>", ""),
])

update_class("SigningRequestMessage", [
    ("+DocumentId", "int"),
    ("+UserId", "int"),
    ("+RequestedAt", "DateTime"),
])

update_class("NotificationMessage", [
    ("+UserId", "int"),
    ("+Title", "string"),
    ("+RequestedAt", "DateTime"),
])

print("\n=== Updating Contracts interfaces ===")

update_class("IUserLogic", [
    ("+ReadListAsync(model: UserSearchModel?): Task<List<UserViewModel>?>", ""),
    ("+ReadPagedListAsync(model: UserSearchModel): Task<List<UserViewModel>?>", ""),
    ("+ReadListByFullnameContainsAsync(model: UserSearchModel): Task<List<UserViewModel>?>", ""),
    ("+ReadElementAsync(model: UserSearchModel): Task<UserViewModel?>", ""),
    ("+CreateAsync(model: UserBindingModel): Task<bool>", ""),
    ("+UpdateAsync(model: UserBindingModel): Task<bool>", ""),
    ("+DeleteAsync(model: UserBindingModel): Task<bool>", ""),
])

update_class("IRoleLogic", [
    ("+ReadListAsync(model: RoleSearchModel?): Task<List<RoleViewModel>?>", ""),
    ("+ReadPagedListAsync(model: RoleSearchModel): Task<List<RoleViewModel>?>", ""),
    ("+ReadListByNameContainsAsync(model: RoleSearchModel): Task<List<RoleViewModel>?>", ""),
    ("+ReadElementAsync(model: RoleSearchModel): Task<RoleViewModel?>", ""),
    ("+CreateAsync(model: RoleBindingModel): Task<bool>", ""),
    ("+UpdateAsync(model: RoleBindingModel): Task<bool>", ""),
    ("+DeleteAsync(model: RoleBindingModel): Task<bool>", ""),
])

update_class("IDocumentLogic", [
    ("+ReadListAsync(model: DocumentSearchModel?): Task<List<DocumentViewModel>?>", ""),
    ("+ReadPagedListAsync(model: DocumentSearchModel): Task<List<DocumentViewModel>?>", ""),
    ("+ReadElementAsync(model: DocumentSearchModel): Task<DocumentViewModel?>", ""),
    ("+CreateAsync(model: DocumentBindingModel, file: Stream, ext: string): Task<bool>", ""),
    ("+UpdateAsync(model: DocumentBindingModel): Task<bool>", ""),
    ("+DeleteAsync(model: DocumentBindingModel): Task<bool>", ""),
])

update_class("IDocumentUserLogic", [
    ("+ReadListAsync(model: DocumentUserSearchModel?): Task<List<DocumentUserViewModel>?>", ""),
    ("+ReadElementAsync(model: DocumentUserSearchModel): Task<DocumentUserViewModel?>", ""),
    ("+CreateAsync(model: DocumentUserBindingModel): Task<bool>", ""),
    ("+UpdateAsync(model: DocumentUserBindingModel): Task<bool>", ""),
    ("+DeleteAsync(model: DocumentUserBindingModel): Task<bool>", ""),
    ("+GetPagedForSignAsync(userId: int, status: SigningStatus?, page: int, size: int): Task<PagedResult<DocumentForSignViewModel>>", ""),
])

update_class("ICertificateLogic", [
    ("+ReadListAsync(model: CertificateSearchModel?): Task<List<CertificateViewModel>?>", ""),
    ("+ReadElementAsync(model: CertificateSearchModel): Task<CertificateViewModel?>", ""),
    ("+CreateAsync(model: CertificateBindingModel): Task<bool>", ""),
    ("+UpdateAsync(model: CertificateBindingModel): Task<bool>", ""),
    ("+DeleteAsync(model: CertificateBindingModel): Task<bool>", ""),
    ("+GenerateSelfSignedAsync(userId: int, owner: string, publisher: string): Task<CertificateViewModel?>", ""),
])

update_class("ISignatureLogic", [
    ("+ReadListAsync(model: SignatureSearchModel?): Task<List<SignatureViewModel>?>", ""),
    ("+ReadElementAsync(model: SignatureSearchModel): Task<SignatureViewModel?>", ""),
    ("+CreateAsync(model: SignatureBindingModel, file: Stream): Task<bool>", ""),
    ("+UpdateAsync(model: SignatureBindingModel): Task<bool>", ""),
    ("+DeleteAsync(model: SignatureBindingModel): Task<bool>", ""),
])

update_class("IAntivirusService", [
    ("+IsFileCleanAsync(stream: Stream): Task<bool>", ""),
])

update_class("IDocumentSigner", [
    ("+SignAsync(documentBytes: byte[], certificate: CertificateViewModel): Task<byte[]>", ""),
])

update_class("ICertificateGeneratorLogic", [
    ("+GenerateSelfSignedAsync(userId: int, owner: string, publisher: string): Task<CertificateBindingModel>", ""),
])

update_class("IFileStorage", [
    ("+SaveOriginalAsync(stream: Stream, docId: int, ext: string): Task<string>", ""),
    ("+SaveSignatureAsync(stream: Stream, docId: int, signId: int): Task<string>", ""),
    ("+SaveCertificateAsync(stream: Stream, userId: int): Task<string>", ""),
    ("+GetFileAsync(path: string): Task<Stream?>", ""),
    ("+GetCertificateBytesAsync(path: string): Task<byte[]>", ""),
    ("+DeleteDocumentFolderAsync(docId: int): Task", ""),
    ("+DeleteCertificateAsync(path: string): Task", ""),
])

update_class("IUserStorage", [
    ("+GetFullListAsync(model: UserSearchModel?): Task<List<UserViewModel>>", ""),
    ("+GetFilteredListAsync(model: UserSearchModel): Task<List<UserViewModel>>", ""),
    ("+GetPagedListAsync(model: UserSearchModel): Task<(List<UserViewModel>, int)>", ""),
    ("+GetElementAsync(model: UserSearchModel): Task<UserViewModel?>", ""),
    ("+InsertAsync(model: UserBindingModel): Task<UserViewModel?>", ""),
    ("+UpdateAsync(model: UserBindingModel): Task<UserViewModel?>", ""),
    ("+DeleteAsync(model: UserBindingModel): Task<bool>", ""),
])

update_class("IRoleStorage", [
    ("+GetFullListAsync(model: RoleSearchModel?): Task<List<RoleViewModel>>", ""),
    ("+GetFilteredListAsync(model: RoleSearchModel): Task<List<RoleViewModel>>", ""),
    ("+GetPagedListAsync(model: RoleSearchModel): Task<(List<RoleViewModel>, int)>", ""),
    ("+GetElementAsync(model: RoleSearchModel): Task<RoleViewModel?>", ""),
    ("+InsertAsync(model: RoleBindingModel): Task<RoleViewModel?>", ""),
    ("+UpdateAsync(model: RoleBindingModel): Task<RoleViewModel?>", ""),
    ("+DeleteAsync(model: RoleBindingModel): Task<bool>", ""),
])

update_class("IDocumentStorage", [
    ("+GetFullListAsync(model: DocumentSearchModel?): Task<List<DocumentViewModel>>", ""),
    ("+GetFilteredListAsync(model: DocumentSearchModel): Task<List<DocumentViewModel>>", ""),
    ("+GetPagedListAsync(model: DocumentSearchModel): Task<(List<DocumentViewModel>, int)>", ""),
    ("+GetElementAsync(model: DocumentSearchModel): Task<DocumentViewModel?>", ""),
    ("+InsertAsync(model: DocumentBindingModel): Task<DocumentViewModel?>", ""),
    ("+UpdateAsync(model: DocumentBindingModel): Task<DocumentViewModel?>", ""),
    ("+DeleteAsync(model: DocumentBindingModel): Task<bool>", ""),
])

update_class("IDocumentUserStorage", [
    ("+GetFullListAsync(model: DocumentUserSearchModel?): Task<List<DocumentUserViewModel>>", ""),
    ("+GetFilteredListAsync(model: DocumentUserSearchModel): Task<List<DocumentUserViewModel>>", ""),
    ("+GetPagedListAsync(model: DocumentUserSearchModel): Task<(List<DocumentUserViewModel>, int)>", ""),
    ("+GetElementAsync(model: DocumentUserSearchModel): Task<DocumentUserViewModel?>", ""),
    ("+InsertAsync(model: DocumentUserBindingModel): Task<DocumentUserViewModel?>", ""),
    ("+UpdateAsync(model: DocumentUserBindingModel): Task<DocumentUserViewModel?>", ""),
    ("+DeleteAsync(model: DocumentUserBindingModel): Task<bool>", ""),
    ("+GetPagedForSignAsync(userId: int, status: SigningStatus?, page: int, size: int): Task<(List<DocumentForSignViewModel>, int)>", ""),
])

update_class("ICertificateStorage", [
    ("+GetFullListAsync(model: CertificateSearchModel?): Task<List<CertificateViewModel>>", ""),
    ("+GetFilteredListAsync(model: CertificateSearchModel): Task<List<CertificateViewModel>>", ""),
    ("+GetElementAsync(model: CertificateSearchModel): Task<CertificateViewModel?>", ""),
    ("+InsertAsync(model: CertificateBindingModel): Task<CertificateViewModel?>", ""),
    ("+UpdateAsync(model: CertificateBindingModel): Task<CertificateViewModel?>", ""),
    ("+DeleteAsync(model: CertificateBindingModel): Task<bool>", ""),
])

update_class("ISignatureStorage", [
    ("+GetFullListAsync(model: SignatureSearchModel?): Task<List<SignatureViewModel>>", ""),
    ("+GetFilteredListAsync(model: SignatureSearchModel): Task<List<SignatureViewModel>>", ""),
    ("+GetPagedListAsync(model: SignatureSearchModel): Task<(List<SignatureViewModel>, int)>", ""),
    ("+GetElementAsync(model: SignatureSearchModel): Task<SignatureViewModel?>", ""),
    ("+InsertAsync(model: SignatureBindingModel): Task<SignatureViewModel?>", ""),
    ("+UpdateAsync(model: SignatureBindingModel): Task<bool>", ""),
    ("+DeleteAsync(model: SignatureBindingModel): Task<bool>", ""),
])

update_class("ISessionService", [
    ("+CreateSessionAsync(userId: int, username: string): Task<string>", ""),
    ("+GetSessionAsync(sessionId: string): Task<UserSession>", ""),
    ("+ValidateSessionAsync(sessionId: string): Task<(bool, string)>", ""),
    ("+DeleteSessionAsync(sessionId: string): Task<bool>", ""),
])

update_class("IEmailService", [
    ("+SendVerificationCodeAsync(email: string, code: string): Task<bool>", ""),
])

update_class("ICodeVerificationLogic", [
    ("+GenerateCode(): string", ""),
    ("+SendCodeAsync(email: string): Task<(bool, string)>", ""),
    ("+VerifyCodeAsync(email: string, code: string): Task<(bool, string)>", ""),
])

# ════════════════════════════════════════════════════════════════════
# 2. ADD NEW PACKAGES: BindingModels, ViewModels, SearchModels
# ════════════════════════════════════════════════════════════════════
print("\n=== Adding new packages and classes ===")

RGB_BIND = (255, 230, 200)  # BindingModels — light orange
RGB_VIEW = (200, 240, 220)  # ViewModels — light mint
RGB_SRCH = (240, 220, 255)  # SearchModels — light lavender

new_model_rows    = []   # (id, parent_id, mtype, name, defn)
new_diagram_rows  = []   # (id, shape_type, model_el_id, parent_view_id, x, y, w, h, rgb)
new_pkg_rows      = []   # (pkg_id, view_id, name, x, y, w, h, rgb, cls_view_ids)

def make_class(name, pkg_id, attrs, abstract=False):
    cid  = gid("nmc2")
    vid  = gid("nvc2")
    mvid = gid("nmm2")
    attr_defs = [attr_def(n, t) for n, t in attrs]
    child_text = ", \n\t\t".join(attr_defs) if attr_defs else ""
    child_block = f"\n\tChild=(\n\t\t{child_text}\n\t);" if child_text else ""
    abst = "\n\tisAbstract=T;" if abstract else ""
    defn = (f'{cid}:"{name}":Class {{\n'
            f'\t_modelEditable=T;{abst}\n'
            f'\t_masterViewId="{vid}";\n'
            f'\tpmAuthor="1";\n\tlastModifiedTime={TS};\n'
            f'\tpmCreateDateTime="{TS}";\n'
            f'\t_modelViews=(\n'
            f'\t\t{{{mvid}:"View":ModelView {{\n'
            f'\t\t\tcontainer=<{DGRAM}>;\n\t\t\tview="{vid}";\n'
            f'\t\t}}}}\n\t);{child_block}\n'
            f'\tpmLastModified="{TS}";\n}}')
    new_model_rows.append((cid, pkg_id, "Class", name, defn))
    return cid, vid, len(attrs)

def make_pkg(name, x, y, w, h, rgb, cls_data):
    pid  = gid("nmp2")
    vid  = gid("nvp2")
    mvid = gid("nmm2")
    cls_ids  = []
    cls_vids = []
    # layout classes inside package
    cx = 20; cy = 35
    row_h = 0
    max_w_in_row = 0
    for (cname, cw, ch, attrs) in cls_data:
        if cx + cw + 20 > w and cx > 20:
            cy += row_h + 10
            cx = 20
            row_h = 0
        cid, cvid, _ = make_class(cname, pid, attrs)
        cls_ids.append(cid)
        cls_vids.append((cvid, cname, cid, cx, cy, cw, ch))
        cx += cw + 10
        row_h = max(row_h, ch)
    # pkg model def
    children = ", \n\t\t".join(f"<{pid}:{c}>" for c in cls_ids)
    pkg_defn = (f'{pid}:"{name}":Package {{\n'
                f'\t_modelEditable=T;\n\t_masterViewId="{vid}";\n'
                f'\tpmAuthor="1";\n\tlastModifiedTime={TS};\n'
                f'\tpmCreateDateTime="{TS}";\n'
                f'\t_modelViews=(\n'
                f'\t\t{{{mvid}:"View":ModelView {{\n'
                f'\t\t\tcontainer=<{DGRAM}>;\n\t\t\tview="{vid}";\n'
                f'\t\t}}}}\n\t);\n'
                f'\tChild=(\n\t\t{children}\n\t);\n'
                f'\tpmLastModified="{TS}";\n}}')
    new_model_rows.append((pid, None, "Package", name, pkg_defn))
    # Store for diagram element generation
    new_pkg_rows.append((pid, vid, name, x, y, w, h, rgb, cls_vids))
    return pid, vid

# ─── BindingModels ─────────────────────────────────────────────────
bind_classes = [
    ("UserBindingModel",         210, 185, [
        ("+Id","int"),("+Fullname","string"),("+Login","string"),
        ("+Email","string"),("+CertificateId","int"),("+RoleId","int"),
        ("+SystemRole","SystemRole"),("+Created","DateTime"),("+IsActive","bool")]),
    ("RoleBindingModel",         185, 105, [
        ("+Id","int"),("+Name","string"),("+Description","string")]),
    ("DocumentBindingModel",     220, 185, [
        ("+Id","int"),("+Title","string"),("+Description","string"),
        ("+CreatedAt","DateTime"),("+CreatedByUserId","int"),
        ("+Path","string"),("+Status","DocumentStatus"),
        ("+IsDeleted","bool"),("+IsSequential","bool"),("+UserIds","List<int>")]),
    ("DocumentUserBindingModel", 230, 153, [
        ("+Id","int"),("+UserId","int"),("+DocumentId","int"),
        ("+SigningStatus","SigningStatus"),("+AssignedAt","DateTime?"),
        ("+Order","int")]),
    ("CertificateBindingModel",  220, 201, [
        ("+Id","int"),("+StartDate","DateTime"),("+FinishDate","DateTime"),
        ("+PublicKey","string"),("+Publisher","string"),("+Owner","string"),
        ("+Number","string"),("+UserId","int"),("+IsActual","bool"),
        ("+Mode","CertificateMode"),("+FilePath","string")]),
    ("SignatureBindingModel",    220, 169, [
        ("+Id","int"),("+SignatureValue","string"),("+CerificateId","int"),
        ("+SignedAt","DateTime"),("+UserId","int"),("+DocumentId","int"),
        ("+Path","string"),("+CertificatePath","string"),("+IsDeleted","bool")]),
    ("UserSession",              200, 121, [
        ("+SessionId","string"),("+UserId","int"),("+Username","string"),
        ("+CreatedAt","DateTime"),("+ExpiresAt","DateTime"),("+IsActive","bool")]),
    ("EmailSettings",            195, 121, [
        ("+SmtpClientHost","string"),("+SmtpClientPort","int"),
        ("+MailLogin","string"),("+MailPassword","string"),
        ("+SenderName","string"),("+EnableSsl","bool")]),
    ("CodeInfo",                 175, 89, [
        ("+Code","string"),("+Email","string"),
        ("+CreatedAt","DateTime"),("+Attempts","int")]),
]
make_pkg("BindingModels", 10, 2000, 2000, 440, RGB_BIND, bind_classes)

# ─── ViewModels ────────────────────────────────────────────────────
view_classes = [
    ("UserViewModel",         205, 169, [
        ("+Id","int"),("+Fullname","string"),("+Login","string"),
        ("+Email","string"),("+CertificateId","int"),("+RoleId","int"),
        ("+SystemRole","SystemRole"),("+Created","DateTime"),("+IsActive","bool")]),
    ("RoleViewModel",         185, 89, [
        ("+Id","int"),("+Name","string"),("+Description","string")]),
    ("DocumentViewModel",     210, 169, [
        ("+Id","int"),("+Title","string"),("+Description","string"),
        ("+CreatedAt","DateTime"),("+CreatedByUserId","int"),
        ("+Path","string"),("+Status","DocumentStatus"),
        ("+IsDeleted","bool"),("+IsSequential","bool")]),
    ("DocumentUserViewModel", 225, 137, [
        ("+Id","int"),("+UserId","int"),("+DocumentId","int"),
        ("+SigningStatus","SigningStatus"),("+AssignedAt","DateTime?"),
        ("+UserFullname","string?"),("+Order","int")]),
    ("CertificateViewModel",  215, 185, [
        ("+Id","int"),("+StartDate","DateTime"),("+FinishDate","DateTime"),
        ("+PublicKey","string"),("+Publisher","string"),("+Owner","string"),
        ("+Number","string"),("+UserId","int"),("+IsActual","bool"),
        ("+Mode","CertificateMode"),("+FilePath","string")]),
    ("SignatureViewModel",     215, 169, [
        ("+Id","int"),("+SignatureValue","string"),("+CerificateId","int"),
        ("+SignedAt","DateTime"),("+UserId","int"),("+DocumentId","int"),
        ("+Path","string"),("+CertificatePath","string"),("+IsDeleted","bool")]),
    ("DocumentForSignViewModel",235, 169, [
        ("+Id","int"),("+Title","string"),("+Description","string?"),
        ("+CreatedAt","DateTime"),("+DocumentStatus","DocumentStatus"),
        ("+IsSequential","bool"),("+UserSigningStatus","SigningStatus"),
        ("+AssignedAt","DateTime?"),("+Order","int")]),
    ("PagedResult<T>",         215, 153, [
        ("+Items","List<T>"),("+TotalCount","int"),("+PageNumber","int"),
        ("+PageSize","int"),("+TotalPages","int"),
        ("+HasPrevious","bool"),("+HasNext","bool")]),
]
make_pkg("ViewModels", 2020, 2000, 970, 440, RGB_VIEW, view_classes)

# ─── SearchModels ──────────────────────────────────────────────────
search_classes = [
    ("UserSearchModel",         200, 185, [
        ("+Id","int?"),("+Fullname","string?"),("+Login","string?"),
        ("+Email","string?"),("+CertificateId","int?"),("+RoleId","int?"),
        ("+SystemRole","SystemRole?"),("+IsActive","bool?"),
        ("+PageNumber","int?"),("+PageSize","int?")]),
    ("RoleSearchModel",         185, 105, [
        ("+Id","int?"),("+Name","string?"),("+Description","string?"),
        ("+PageNumber","int?"),("+PageSize","int?")]),
    ("DocumentSearchModel",     215, 153, [
        ("+Id","int?"),("+Title","string?"),("+Description","string?"),
        ("+CreatedAt","DateTime?"),("+CreatedByUserId","int?"),
        ("+Status","DocumentStatus?"),("+IsDeleted","bool?"),
        ("+PageNumber","int?"),("+PageSize","int?")]),
    ("DocumentUserSearchModel", 230, 137, [
        ("+Id","int?"),("+UserId","int?"),("+DocumentId","int?"),
        ("+SigningStatus","SigningStatus?"),("+AssignedAt","DateTime?"),
        ("+PageNumber","int?"),("+PageSize","int?")]),
    ("CertificateSearchModel",  225, 185, [
        ("+Id","int?"),("+StartDate","DateTime?"),("+FinishDate","DateTime?"),
        ("+PublicKey","string?"),("+Publisher","string?"),("+Owner","string?"),
        ("+Number","string?"),("+UserId","int?"),("+IsActual","bool?"),
        ("+PageNumber","int?"),("+PageSize","int?")]),
    ("SignatureSearchModel",    215, 169, [
        ("+Id","int?"),("+SignatureValue","string?"),("+CerificateId","int?"),
        ("+SignedAt","DateTime?"),("+UserId","int?"),("+DocumentId","int?"),
        ("+IsDeleted","bool?"),("+PageNumber","int?"),("+PageSize","int?")]),
]
make_pkg("SearchModels", 10, 2460, 2000, 330, RGB_SRCH, search_classes)

# ════════════════════════════════════════════════════════════════════
# 3. INSERT NEW MODEL_ELEMENT ROWS
# ════════════════════════════════════════════════════════════════════
for (eid, parent_id, mtype, name, defn) in new_model_rows:
    c.execute("INSERT INTO MODEL_ELEMENT "
              "(ID, USER_ID, USER_ID_PARENT, MODEL_TYPE, PARENT_ID, NAME, DEFINITION, AUTHOR, CREATE_AT, LAST_MOD_AT) "
              "VALUES (?, NULL, NULL, ?, ?, ?, ?, '1', ?, ?)",
              (eid, mtype, parent_id, name, sqlite3.Binary(defn.encode("utf-8")), TS_S, TS_S))
print(f"Inserted {len(new_model_rows)} new MODEL_ELEMENT rows")

# ════════════════════════════════════════════════════════════════════
# 4. INSERT NEW DIAGRAM_ELEMENT ROWS
# ════════════════════════════════════════════════════════════════════
def _cap(w, h):
    return (f"(\n\t\t@x=0;, @y=0;, @width={w+1};, @height={h};, "
            f"@side=12;, @visible=T;, @internalWidth=-2147483648;, @internalHeight=-2147483648;\n\t)")
def _pcap(w, h):
    return (f"(\n\t\t@x=0;, @y=20;, @width={w};, @height={h-20};, "
            f"@side=12;, @visible=T;, @internalWidth=-2147483648;, @internalHeight=-2147483648;\n\t)")

new_diag_elements = []  # (vid, shape_type, model_el_id, parent_id, defn)

for (pkg_id, pkg_vid, pkg_name, px, py, pw, ph, rgb, cls_vids) in new_pkg_rows:
    r, g, b = rgb
    # Build contained list
    contained = ", \n\t\t".join(f"<{DGRAM}:{cv[0]}>" for cv in cls_vids)
    pkg_defn = (f'{pkg_vid}:"{pkg_name}":Package {{\n'
                f'\tforeground=(\n\t\t0, \n\t\t0, \n\t\t0, \n\t\t255\n\t);\n'
                f'\tmodelElementNameAlignment=1;\n\tconnectToPoint=T;\n'
                f'\ty={py};\n\tx={px};\n'
                f'\tmetaModelElement=<{pkg_id}>;\n'
                f'\toverrideAppearanceWithStereotypeIcon=T;\n'
                f'\tparentConnectorHeaderLength=40;\n\tparentConnectorLineLength=10;\n'
                f'\theight={ph};\n'
                f'\t_fillColor={_fill(r,g,b)};\n'
                f'\twidth={pw};\n'
                f'\tbackground=(\n\t\t{r}, \n\t\t{g}, \n\t\t{b}, \n\t\t255\n\t);\n'
                f'\t_elementFont={_font()};\n'
                f'\t_captionUIModel={_pcap(pw, ph)};\n'
                f'\tContainedDiagramElements=(\n\t\t{contained}\n\t);\n'
                f'\t_lineModel={_line()};\n}}')
    new_diag_elements.append((pkg_vid, "Package", pkg_id, None, pkg_defn))
    # Class elements
    for (cvid, cname, cid, cx, cy, cw, ch) in cls_vids:
        cls_defn = (f'{cvid}:"{cname}":Class {{\n'
                    f'\tshowOperationType=1;\n'
                    f'\tforeground=(\n\t\t0, \n\t\t0, \n\t\t0, \n\t\t255\n\t);\n'
                    f'\tshowAttributeType=1;\n\tdisplayAsRobustnessAnalysisIcon=T;\n'
                    f'\tshowReceptionType=1;\n\tshowEnumerationLiteralType=1;\n'
                    f'\tshowParameterNameInOperationSignature=T;\n\tconnectToPoint=T;\n'
                    f'\ty={cy};\n\tx={cx};\n'
                    f'\tmetaModelElement=<{pkg_id}:{cid}>;\n'
                    f'\tlShCmMl=F;\n\toverrideAppearanceWithStereotypeIcon=T;\n'
                    f'\tmSwTpPts=T;\n\tparentConnectorHeaderLength=40;\n'
                    f'\tparentConnectorLineLength=10;\n\theight={ch};\n\twpMbs=F;\n'
                    f'\t_fillColor={_fill(r,g,b)};\n'
                    f'\twidth={cw};\n\tshowOperationSignature=T;\n\tkSwCsMbSt=T;\n'
                    f'\tvisibilityStyle=1;\n\tshowInitialAttributeValue=T;\n'
                    f'\tbackground=(\n\t\t{r}, \n\t\t{g}, \n\t\t{b}, \n\t\t255\n\t);\n'
                    f'\t_parent=<{DGRAM}:{pkg_vid}>;\n'
                    f'\t_elementFont={_font()};\n'
                    f'\t_captionUIModel={_cap(cw, ch)};\n'
                    f'\tinterfaceBall=F;\n'
                    f'\t_lineModel={_line()};\n}}')
        new_diag_elements.append((cvid, "Class", cid, pkg_vid, cls_defn))

for (vid, stype, mel_id, par_id, defn) in new_diag_elements:
    c.execute("INSERT INTO DIAGRAM_ELEMENT "
              "(ID, SHAPE_TYPE, DIAGRAM_ID, MODEL_ELEMENT_ID, PARENT_ID, DEFINITION) "
              "VALUES (?, ?, ?, ?, ?, ?)",
              (vid, stype, DGRAM, mel_id, par_id, sqlite3.Binary(defn.encode("utf-8"))))
print(f"Inserted {len(new_diag_elements)} new DIAGRAM_ELEMENT rows")

# ════════════════════════════════════════════════════════════════════
# 5. UPDATE DIAGRAM.Child to include ALL elements
# ════════════════════════════════════════════════════════════════════
c.execute("SELECT ID FROM DIAGRAM_ELEMENT WHERE DIAGRAM_ID=?", (DGRAM,))
all_vids = [r[0] for r in c.fetchall()]

child_list = ", \n\t\t".join(f"<{DGRAM}:{v}>" for v in all_vids)

c.execute("SELECT DEFINITION FROM DIAGRAM WHERE ID=?", (DGRAM,))
diag_blob = c.fetchone()[0]
diag_text = diag_blob.decode("utf-8") if isinstance(diag_blob, bytes) else diag_blob

# Replace Child block in diagram
start = diag_text.find("\n\tChild=(")
i = diag_text.find("Child=(", start) + 7
depth = 1
while i < len(diag_text) and depth > 0:
    if diag_text[i] == '(': depth += 1
    elif diag_text[i] == ')': depth -= 1
    i += 1
end = i
if end < len(diag_text) and diag_text[end] == ';': end += 1
new_child = f"\n\tChild=(\n\t\t{child_list}\n\t);"
diag_text = diag_text[:start] + new_child + diag_text[end:]

c.execute("UPDATE DIAGRAM SET DEFINITION=? WHERE ID=?",
          (sqlite3.Binary(diag_text.encode("utf-8")), DGRAM))
print(f"Updated DIAGRAM.Child: {len(all_vids)} total elements")

conn.commit()
conn.close()

# Quick verify
conn2 = sqlite3.connect(VPP)
c2 = conn2.cursor()
c2.execute("SELECT COUNT(*) FROM DIAGRAM_ELEMENT WHERE DIAGRAM_ID=?", (DGRAM,))
total = c2.fetchone()[0]
c2.execute("SELECT COUNT(*) FROM DIAGRAM_ELEMENT WHERE typeof(DEFINITION)='text'")
text_left = c2.fetchone()[0]
conn2.close()
print(f"\nFinal: {total} diagram elements, {text_left} still as text (should be 0)")
print("Done!")
