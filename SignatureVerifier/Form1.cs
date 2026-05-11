namespace SignatureVerifier;

public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();
    }

    private void BtnBrowseSignature_Click(object? sender, EventArgs e)
    {
        if (_openSignature.ShowDialog(this) == DialogResult.OK)
            _txtSignaturePath.Text = _openSignature.FileName;
    }

    private void BtnBrowseDocument_Click(object? sender, EventArgs e)
    {
        if (_openDocument.ShowDialog(this) == DialogResult.OK)
            _txtDocumentPath.Text = _openDocument.FileName;
    }

    private void BtnVerify_Click(object? sender, EventArgs e)
    {
        var sig = _txtSignaturePath.Text.Trim();
        var doc = _txtDocumentPath.Text.Trim();

        if (string.IsNullOrEmpty(sig))
        {
            _txtOutput.Text = "Ошибка: не выбран файл подписи.";
            return;
        }

        if (string.IsNullOrEmpty(doc))
        {
            _txtOutput.Text = "Ошибка: не выбран файл документа.";
            return;
        }

        if (!File.Exists(sig))
        {
            _txtOutput.Text = "Ошибка: файл подписи не найден.";
            return;
        }

        if (!File.Exists(doc))
        {
            _txtOutput.Text = "Ошибка: файл документа не найден.";
            return;
        }

        var openssl = OpenSslSignatureInspector.TryResolveOpenSslPath();
        if (openssl == null)
        {
            _txtOutput.Text =
                "Ошибка: не найден openssl.exe. Установите OpenSSL и добавьте его в PATH " +
                "или в стандартную папку (например, C:\\Program Files\\OpenSSL-Win64\\bin).";
            return;
        }

        _btnVerify.Enabled = false;
        try
        {
            _txtOutput.Text = "Выполняется проверка…";
            Application.DoEvents();

            var report = OpenSslSignatureInspector.BuildReport(openssl, sig, doc);
            _txtOutput.Text = report;
        }
        catch (Exception ex)
        {
            _txtOutput.Text = "Ошибка: " + ex.Message;
        }
        finally
        {
            _btnVerify.Enabled = true;
        }
    }
}
