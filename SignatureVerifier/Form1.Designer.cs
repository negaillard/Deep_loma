namespace SignatureVerifier;

public partial class Form1
{
    private System.ComponentModel.IContainer components = null;
    private TableLayoutPanel _layout;
    private Label _lblSignature;
    private TextBox _txtSignaturePath;
    private Button _btnBrowseSignature;
    private Label _lblDocument;
    private TextBox _txtDocumentPath;
    private Button _btnBrowseDocument;
    private Button _btnVerify;
    private TextBox _txtOutput;
    private OpenFileDialog _openSignature;
    private OpenFileDialog _openDocument;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        _layout = new TableLayoutPanel();
        _lblSignature = new Label();
        _txtSignaturePath = new TextBox();
        _btnBrowseSignature = new Button();
        _lblDocument = new Label();
        _txtDocumentPath = new TextBox();
        _btnBrowseDocument = new Button();
        _btnVerify = new Button();
        _txtOutput = new TextBox();
        _openSignature = new OpenFileDialog();
        _openDocument = new OpenFileDialog();
        _layout.SuspendLayout();
        SuspendLayout();

        _layout.ColumnCount = 3;
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
        _layout.RowCount = 4;
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _layout.Dock = DockStyle.Fill;
        _layout.Padding = new Padding(10);

        _lblSignature.AutoSize = true;
        _lblSignature.Dock = DockStyle.Fill;
        _lblSignature.Text = "Файл подписи (.sig):";
        _lblSignature.TextAlign = ContentAlignment.MiddleLeft;

        _txtSignaturePath.Dock = DockStyle.Fill;
        _txtSignaturePath.ReadOnly = true;

        _btnBrowseSignature.Text = "Обзор…";
        _btnBrowseSignature.Dock = DockStyle.Fill;
        _btnBrowseSignature.Click += BtnBrowseSignature_Click;

        _lblDocument.AutoSize = true;
        _lblDocument.Dock = DockStyle.Fill;
        _lblDocument.Text = "Подписанный документ:";
        _lblDocument.TextAlign = ContentAlignment.MiddleLeft;

        _txtDocumentPath.Dock = DockStyle.Fill;
        _txtDocumentPath.ReadOnly = true;

        _btnBrowseDocument.Text = "Обзор…";
        _btnBrowseDocument.Dock = DockStyle.Fill;
        _btnBrowseDocument.Click += BtnBrowseDocument_Click;

        _btnVerify.Text = "Проверить подпись";
        _btnVerify.Dock = DockStyle.Fill;
        _btnVerify.Click += BtnVerify_Click;

        _layout.SetColumnSpan(_btnVerify, 3);

        _txtOutput.Dock = DockStyle.Fill;
        _txtOutput.Multiline = true;
        _txtOutput.ReadOnly = true;
        _txtOutput.ScrollBars = ScrollBars.Both;
        _txtOutput.Font = new Font("Consolas", 9F);
        _txtOutput.WordWrap = false;

        _openSignature.Title = "Выберите файл подписи";
        _openSignature.Filter = "Подпись|*.sig;*.p7s;*.pem;*.*|Все файлы|*.*";

        _openDocument.Title = "Выберите исходный документ";
        _openDocument.Filter = "Документы|*.pdf;*.doc;*.docx;*.*|Все файлы|*.*";

        _layout.Controls.Add(_lblSignature, 0, 0);
        _layout.Controls.Add(_txtSignaturePath, 1, 0);
        _layout.Controls.Add(_btnBrowseSignature, 2, 0);
        _layout.Controls.Add(_lblDocument, 0, 1);
        _layout.Controls.Add(_txtDocumentPath, 1, 1);
        _layout.Controls.Add(_btnBrowseDocument, 2, 1);
        _layout.Controls.Add(_btnVerify, 0, 2);
        _layout.Controls.Add(_txtOutput, 0, 3);
        _layout.SetColumnSpan(_txtOutput, 3);

        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(900, 600);
        MinimumSize = new Size(640, 400);
        Controls.Add(_layout);
        Text = "Проверка подписи (OpenSSL)";
        _layout.ResumeLayout(false);
        _layout.PerformLayout();
        ResumeLayout(false);
    }
}
