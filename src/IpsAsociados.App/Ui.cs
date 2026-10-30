using IpsAsociados.Core;

namespace IpsAsociados.App;

static class Ui
{
    public static DataGridView Grid() => new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells, RowHeadersVisible = false,
        BackgroundColor = SystemColors.Window, AutoGenerateColumns = true
    };

    public static TextBox Text(int width = 150) => new() { Width = width, Margin = new Padding(3, 3, 12, 3) };

    public static Label Lbl(string text) => new() { Text = text, AutoSize = true, Margin = new Padding(3, 8, 3, 3) };

    public static Button Btn(string text, EventHandler onClick)
    {
        var b = new Button { Text = text, AutoSize = true, Padding = new Padding(8, 2, 8, 2), Margin = new Padding(3, 3, 12, 3) };
        b.Click += onClick;
        return b;
    }

    public static FlowLayoutPanel Row() => new()
    {
        Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
        WrapContents = true, Padding = new Padding(6)
    };

    public static void Error(Exception ex) =>
        MessageBox.Show(ex.Message, "IPS Asociados", MessageBoxButtons.OK, MessageBoxIcon.Warning);
}
