namespace MiniSupermarket.WinForms
{
    public partial class FormLogin : Form
    {
        public FormLogin()
        {
            InitializeComponent();
            AcceptButton = btnLogin;
        }

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUser.Text) || string.IsNullOrWhiteSpace(txtPass.Text))
            {
                WorkspaceUi.Invalid("Vui lòng nhập tài khoản và mật khẩu.");
                return;
            }
            btnLogin.Enabled = false;
            try
            {
                if (!await ApiClientService.LoginAsync(txtUser.Text.Trim(), txtPass.Text))
                {
                    MessageBox.Show("401 - Sai tài khoản, mật khẩu hoặc tài khoản đã bị khóa.", "Đăng nhập thất bại");
                    return;
                }
                using var shell = new FormMainShell();
                Hide();
                var result = shell.ShowDialog();
                SessionManager.Clear();
                if (result == DialogResult.Retry)
                {
                    txtPass.Clear();
                    Show();
                    txtPass.Focus();
                }
                else Close();
            }
            catch (Exception ex)
            {
                SessionManager.Clear();
                Show();
                MessageBox.Show(ApiFeedback.ErrorMessage(ex), "Không thể đăng nhập", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { if (!IsDisposed) btnLogin.Enabled = true; }
        }
    }
}
