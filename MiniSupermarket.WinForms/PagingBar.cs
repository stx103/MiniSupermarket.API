namespace MiniSupermarket.WinForms
{
    public sealed class PagingBar : FlowLayoutPanel
    {
        private readonly Button _previous, _next;
        private readonly Label _label;
        private readonly ComboBox _size;
        private int _current = 1, _pages;
        public event Action<int>? PageRequested;
        public int PageSize => _size.SelectedItem is int value ? value : 5;

        public PagingBar()
        {
            Name = "pagingBar";
            Height = 46;
            WrapContents = false;
            Dock = DockStyle.Bottom;
            _previous = WorkspaceUi.Button("btnPrev", "Trang trước", (_, _) => PageRequested?.Invoke(_current - 1));
            _next = WorkspaceUi.Button("btnNext", "Trang sau", (_, _) => PageRequested?.Invoke(_current + 1));
            _label = new Label { Name = "lblPaging", Width = 190, Height = 38, TextAlign = ContentAlignment.MiddleCenter, Margin = Padding.Empty };
            _size = new ComboBox { Name = "cboPageSize", Width = 64, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(4, 8, 0, 0) };
            _size.Items.AddRange(new object[] { 5, 10, 20, 50 });
            _size.SelectedIndex = 0;
            _size.SelectedIndexChanged += (_, _) => PageRequested?.Invoke(1);
            Controls.AddRange(new Control[] { _previous, _label, _next, _size });
            Render(1, 0, 0);
        }

        public void Render(int current, int pages, int total)
        {
            _current = current; _pages = pages;
            _label.Text = pages == 0 ? "Không có kết quả" : $"Trang {current}/{pages} · {total:N0} dòng";
            _previous.Enabled = current > 1;
            _next.Enabled = current < _pages;
        }
    }

    public class PagedViewResult<T>
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public List<T> Items { get; set; } = new();
    }
}
