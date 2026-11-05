using System.Globalization;
using System.Text;

namespace MiniSupermarket.WinForms;

public static class CsvExport
{
    // UTF-8 BOM khi ghi file để Excel đọc đúng dấu tiếng Việt. Mỗi ô được quote theo CSV.
    public static string Build(IEnumerable<string> headers, IEnumerable<IEnumerable<object?>> rows)
    {
        string Cell(object? value)
        {
            var text = value switch
            {
                null => "",
                DateTime date => date.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                IFormattable number => number.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString() ?? ""
            };
            if (value is string && text.TrimStart().StartsWithAny('=', '+', '-', '@')) text = "'" + text;
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }
        var output = new StringBuilder();
        output.AppendLine(string.Join(",", headers.Select(h => Cell(h))));
        foreach (var row in rows) output.AppendLine(string.Join(",", row.Select(Cell)));
        return output.ToString();
    }

    private static bool StartsWithAny(this string value, params char[] chars) => value.Length > 0 && chars.Contains(value[0]);

    public static void Save(IWin32Window owner, string fileName, IEnumerable<string> headers, IEnumerable<IEnumerable<object?>> rows)
    {
        using var dialog = new SaveFileDialog { Title = "Xuất CSV (mở bằng Excel)", Filter = "CSV UTF-8 (*.csv)|*.csv", FileName = fileName, DefaultExt = "csv", AddExtension = true };
        if (dialog.ShowDialog(owner) != DialogResult.OK) return;
        File.WriteAllText(dialog.FileName, Build(headers, rows), new UTF8Encoding(true));
        MessageBox.Show(owner, $"Đã xuất tệp:\n{dialog.FileName}", "Xuất CSV", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
