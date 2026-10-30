using IpsAsociados.Agile;
using IpsAsociados.Compiler;
using IpsAsociados.Core;

namespace IpsAsociados.App;

public class MainForm : Form
{
    readonly IpsService _svc;
    readonly TabControl _tabs = new() { Dock = DockStyle.Fill };
    readonly StatusStrip _status = new();
    readonly ToolStripStatusLabel _statusLabel = new();

    // Pacientes
    readonly DataGridView _gridPacientes = Ui.Grid();
    readonly TextBox _pNombre = Ui.Text(180), _pCarne = Ui.Text(90), _pCedula = Ui.Text(100), _pCiudad = Ui.Text(110), _pTel = Ui.Text(100);

    // Terapeutas
    readonly DataGridView _gridTerapeutas = Ui.Grid();
    readonly TextBox _tNombre = Ui.Text(180), _tCiudad = Ui.Text(110), _tValor = Ui.Text(80);
    readonly ComboBox _tTipo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };

    // Órdenes y sesiones
    readonly DataGridView _gridOrdenes = Ui.Grid(), _gridSesiones = Ui.Grid();
    readonly ComboBox _oPaciente = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
    readonly ComboBox _oTipo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
    readonly ComboBox _oTerapeuta = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
    readonly TextBox _oMedico = Ui.Text(150), _oDiag = Ui.Text(70), _oValor = Ui.Text(70);
    readonly NumericUpDown _oSesiones = new() { Minimum = 1, Maximum = 200, Value = 5, Width = 60 };
    readonly DateTimePicker _sFecha = new() { Format = DateTimePickerFormat.Short, Width = 110 };

    // Planilla y honorarios
    readonly DateTimePicker _desde = new() { Format = DateTimePickerFormat.Short, Width = 110, Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1) };
    readonly DateTimePicker _hasta = new() { Format = DateTimePickerFormat.Short, Width = 110 };
    readonly DataGridView _gridPlanilla = Ui.Grid(), _gridHonorarios = Ui.Grid();
    readonly Label _lblTotal = new() { AutoSize = true, Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold), Margin = new Padding(3, 8, 3, 3) };
    PlanillaGeneral? _planillaActual;

    // Compilador
    readonly TextBox _codigo = new() { Multiline = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Both, WordWrap = false, Font = new Font("Consolas", 10f) };
    readonly TextBox _salida = new() { Multiline = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical, ReadOnly = true, Font = new Font("Consolas", 10f) };

    // Plan ágil
    readonly PlanScrum _plan = PlanScrum.IpsAsociados();
    readonly TextBox _informe = new() { Multiline = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Both, ReadOnly = true, Font = new Font("Consolas", 10f), WordWrap = false };

    public MainForm()
    {
        Text = "IPS Asociados - Gestión de terapias domiciliarias";
        Width = 1100; Height = 720; StartPosition = FormStartPosition.CenterScreen;

        try { _svc = IpsService.Load(); }
        catch (Exception ex) { Ui.Error(new Exception($"No se pudieron cargar los datos guardados: {ex.Message}")); _svc = new IpsService(); }

        _tTipo.DataSource = Enum.GetValues<TipoTerapia>();
        _oTipo.DataSource = Enum.GetValues<TipoTerapia>();
        _status.Items.Add(_statusLabel);

        _tabs.TabPages.Add(BuildPacientes());
        _tabs.TabPages.Add(BuildTerapeutas());
        _tabs.TabPages.Add(BuildOrdenes());
        _tabs.TabPages.Add(BuildPlanilla());
        _tabs.TabPages.Add(BuildCompilador());
        _tabs.TabPages.Add(BuildAgil());
        Controls.Add(_tabs);
        Controls.Add(_status);

        _codigo.Text = EjemploScript;
        _informe.Text = _plan.GenerarInformeMarkdown().Replace("\n", Environment.NewLine);
        FormClosing += (_, _) => Guardar();
        RefrescarTodo();
    }

    const string EjemploScript = """
        # TerapiaScript: define pacientes, terapeutas y órdenes
        paciente "Ana Ruiz" carne C-100 cedula 12345678 ciudad Bogotá telefono 3001234567;
        terapeuta "Luis Pérez" tipo fisica valor 40000 ciudad Bogotá;
        orden paciente C-100 medico "Dr. Gómez" diagnostico M54.5 sesiones 5 valor 60000 terapeuta "Luis Pérez";
        """;

    void Guardar()
    {
        try { _svc.Save(); }
        catch (Exception ex) { Ui.Error(new Exception($"No se pudieron guardar los datos: {ex.Message}")); }
    }

    void Info(string msg) => _statusLabel.Text = msg;

    // ---------- Pacientes ----------
    TabPage BuildPacientes()
    {
        var page = new TabPage("Pacientes");
        var row = Ui.Row();
        row.Controls.AddRange([Ui.Lbl("Nombre"), _pNombre, Ui.Lbl("Carné/HC"), _pCarne, Ui.Lbl("Cédula"), _pCedula,
            Ui.Lbl("Ciudad"), _pCiudad, Ui.Lbl("Teléfono"), _pTel, Ui.Btn("Registrar paciente", (_, _) => RegistrarPaciente())]);
        page.Controls.Add(_gridPacientes);
        page.Controls.Add(row);
        return page;
    }

    void RegistrarPaciente()
    {
        try
        {
            var p = _svc.RegistrarPaciente(_pNombre.Text, _pCarne.Text, _pCedula.Text, _pCiudad.Text, "", _pTel.Text);
            foreach (var t in new[] { _pNombre, _pCarne, _pCedula, _pCiudad, _pTel }) t.Clear();
            Guardar(); RefrescarTodo(); Info($"Paciente '{p.Nombre}' registrado.");
        }
        catch (IpsException ex) { Ui.Error(ex); }
    }

    // ---------- Terapeutas ----------
    TabPage BuildTerapeutas()
    {
        var page = new TabPage("Terapeutas");
        var row = Ui.Row();
        row.Controls.AddRange([Ui.Lbl("Nombre"), _tNombre, Ui.Lbl("Ciudad"), _tCiudad, Ui.Lbl("Especialidad"), _tTipo,
            Ui.Lbl("Valor por sesión"), _tValor, Ui.Btn("Registrar terapeuta", (_, _) => RegistrarTerapeuta())]);
        page.Controls.Add(_gridTerapeutas);
        page.Controls.Add(row);
        return page;
    }

    void RegistrarTerapeuta()
    {
        try
        {
            if (!decimal.TryParse(_tValor.Text, out var valor)) throw new IpsException("Valor por sesión inválido.");
            var t = _svc.RegistrarTerapeuta(_tNombre.Text, _tCiudad.Text, (TipoTerapia)_tTipo.SelectedItem!, valor);
            foreach (var c in new[] { _tNombre, _tCiudad, _tValor }) c.Clear();
            Guardar(); RefrescarTodo(); Info($"Terapeuta '{t.Nombre}' registrado.");
        }
        catch (IpsException ex) { Ui.Error(ex); }
    }

    // ---------- Órdenes y sesiones ----------
    TabPage BuildOrdenes()
    {
        var page = new TabPage("Órdenes y sesiones");
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 230 };
        var top = Ui.Row();
        top.Controls.AddRange([Ui.Lbl("Paciente"), _oPaciente, Ui.Lbl("Médico"), _oMedico, Ui.Lbl("Dx"), _oDiag,
            Ui.Lbl("Tipo"), _oTipo, Ui.Lbl("Sesiones"), _oSesiones, Ui.Lbl("Valor sesión"), _oValor,
            Ui.Btn("Crear orden", (_, _) => CrearOrden())]);
        var assign = Ui.Row();
        assign.Controls.AddRange([Ui.Lbl("Terapeuta"), _oTerapeuta, Ui.Btn("Asignar a orden", (_, _) => AsignarTerapeuta())]);
        split.Panel1.Controls.Add(_gridOrdenes);
        split.Panel1.Controls.Add(assign);
        split.Panel1.Controls.Add(top);

        var sess = Ui.Row();
        sess.Controls.AddRange([Ui.Lbl("Fecha"), _sFecha, Ui.Btn("Programar sesión", (_, _) => ProgramarSesion()),
            Ui.Btn("Marcar realizada (firmada)", (_, _) => MarcarRealizada())]);
        split.Panel2.Controls.Add(_gridSesiones);
        split.Panel2.Controls.Add(sess);
        page.Controls.Add(split);
        _gridOrdenes.SelectionChanged += (_, _) => RefrescarSesiones();
        return page;
    }

    Orden? OrdenSeleccionada() => _gridOrdenes.CurrentRow?.DataBoundItem is OrdenVista v ? _svc.Data.Ordenes.FirstOrDefault(o => o.Id == v.Id) : null;

    void CrearOrden()
    {
        try
        {
            if (_oPaciente.SelectedItem is not Paciente p) throw new IpsException("Seleccione un paciente.");
            if (!decimal.TryParse(_oValor.Text, out var valor)) throw new IpsException("Valor por sesión inválido.");
            var o = _svc.CrearOrden(p.Id, _oMedico.Text, _oDiag.Text, (TipoTerapia)_oTipo.SelectedItem!, (int)_oSesiones.Value, valor);
            Guardar(); RefrescarTodo(); Info($"Orden #{o.Id} creada.");
        }
        catch (IpsException ex) { Ui.Error(ex); }
    }

    void AsignarTerapeuta()
    {
        try
        {
            var o = OrdenSeleccionada() ?? throw new IpsException("Seleccione una orden.");
            if (_oTerapeuta.SelectedItem is not Terapeuta t) throw new IpsException("Seleccione un terapeuta.");
            _svc.AsignarTerapeuta(o.Id, t.Id);
            Guardar(); RefrescarTodo(); Info($"Terapeuta '{t.Nombre}' asignado a la orden #{o.Id}.");
        }
        catch (IpsException ex) { Ui.Error(ex); }
    }

    void ProgramarSesion()
    {
        try
        {
            var o = OrdenSeleccionada() ?? throw new IpsException("Seleccione una orden.");
            _svc.ProgramarSesion(o.Id, _sFecha.Value);
            Guardar(); RefrescarTodo(); SeleccionarOrden(o.Id);
        }
        catch (IpsException ex) { Ui.Error(ex); }
    }

    void MarcarRealizada()
    {
        try
        {
            var o = OrdenSeleccionada() ?? throw new IpsException("Seleccione una orden.");
            if (_gridSesiones.CurrentRow?.DataBoundItem is not SesionVista s) throw new IpsException("Seleccione una sesión.");
            if (MessageBox.Show("¿El paciente firmó la planilla de esta sesión?", "Confirmar", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            _svc.RegistrarSesionRealizada(o.Id, s.Numero, DateTime.Today);
            Guardar(); RefrescarTodo(); SeleccionarOrden(o.Id);
        }
        catch (IpsException ex) { Ui.Error(ex); }
    }

    void SeleccionarOrden(int id)
    {
        foreach (DataGridViewRow r in _gridOrdenes.Rows)
            if (r.DataBoundItem is OrdenVista v && v.Id == id) { _gridOrdenes.CurrentCell = r.Cells[0]; break; }
    }

    // ---------- Planilla y honorarios ----------
    TabPage BuildPlanilla()
    {
        var page = new TabPage("Planilla y honorarios");
        var row = Ui.Row();
        row.Controls.AddRange([Ui.Lbl("Desde"), _desde, Ui.Lbl("Hasta"), _hasta,
            Ui.Btn("Generar planilla", (_, _) => GenerarPlanilla()),
            Ui.Btn("Exportar CSV y marcar facturadas", (_, _) => ExportarPlanilla()),
            Ui.Btn("Calcular honorarios", (_, _) => CalcularHonorarios()), _lblTotal]);
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 250 };
        split.Panel1.Controls.Add(_gridPlanilla);
        split.Panel2.Controls.Add(_gridHonorarios);
        page.Controls.Add(split);
        page.Controls.Add(row);
        return page;
    }

    void GenerarPlanilla()
    {
        _planillaActual = _svc.GenerarPlanilla(_desde.Value, _hasta.Value);
        _gridPlanilla.DataSource = _planillaActual.Lineas.Select(l => new { l.OrdenId, Diagnostico = l.CodigoDiagnostico, Fecha = l.FechaSesion.ToShortDateString(), Valor = l.ValorUnitario, l.Cedula, l.Carne }).ToList();
        _lblTotal.Text = $"Total planilla: {_planillaActual.Total:N0}";
        Info($"{_planillaActual.Lineas.Count} sesiones por facturar.");
    }

    void ExportarPlanilla()
    {
        try
        {
            var planilla = _svc.GenerarPlanilla(_desde.Value, _hasta.Value);
            if (planilla.Lineas.Count == 0) throw new IpsException("No hay sesiones realizadas pendientes de facturar en el rango.");
            using var dlg = new SaveFileDialog { Filter = "CSV|*.csv", FileName = $"planilla-{DateTime.Today:yyyyMMdd}.csv" };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            File.WriteAllText(dlg.FileName, IpsService.ExportarPlanillaCsv(planilla), System.Text.Encoding.UTF8);
            _svc.GenerarPlanilla(_desde.Value, _hasta.Value, marcarFacturadas: true);
            Guardar(); GenerarPlanilla();
            Info($"Planilla exportada: {dlg.FileName}");
        }
        catch (Exception ex) when (ex is IpsException or IOException or UnauthorizedAccessException) { Ui.Error(ex); }
    }

    void CalcularHonorarios()
    {
        var h = _svc.CalcularHonorarios(_desde.Value, _hasta.Value);
        _gridHonorarios.DataSource = h.Select(x => new { x.Terapeuta, x.Sesiones, Total = x.Total }).ToList();
        Info($"Honorarios totales: {h.Sum(x => x.Total):N0}");
    }

    // ---------- Compilador ----------
    TabPage BuildCompilador()
    {
        var page = new TabPage("Compilador TerapiaScript");
        var row = Ui.Row();
        row.Controls.AddRange([Ui.Btn("Compilar", (_, _) => Compilar(false)), Ui.Btn("Compilar y ejecutar", (_, _) => Compilar(true))]);
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 350 };
        split.Panel1.Controls.Add(_codigo);
        split.Panel2.Controls.Add(_salida);
        page.Controls.Add(split);
        page.Controls.Add(row);
        return page;
    }

    void Compilar(bool ejecutar)
    {
        var prog = TerapiaCompiler.Compilar(_codigo.Text, _svc);
        var lines = new List<string>();
        if (!prog.Exito)
        {
            lines.Add($"Compilación fallida: {prog.Errores.Count} error(es).");
            lines.AddRange(prog.Errores.Select(e => "  " + e));
        }
        else
        {
            lines.Add($"Compilación correcta: {prog.Instrucciones.Count} instrucción(es).");
            if (ejecutar)
            {
                lines.AddRange(prog.Ejecutar(_svc));
                Guardar(); RefrescarTodo();
            }
        }
        _salida.Text = string.Join(Environment.NewLine, lines);
    }

    // ---------- Plan ágil ----------
    TabPage BuildAgil()
    {
        var page = new TabPage("Plan ágil (Scrum)");
        var row = Ui.Row();
        row.Controls.Add(Ui.Btn("Guardar informe .md", (_, _) =>
        {
            using var dlg = new SaveFileDialog { Filter = "Markdown|*.md", FileName = "informe-procedimientos-agiles.md" };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            try { File.WriteAllText(dlg.FileName, _plan.GenerarInformeMarkdown(), System.Text.Encoding.UTF8); Info($"Informe guardado: {dlg.FileName}"); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { Ui.Error(ex); }
        }));
        page.Controls.Add(_informe);
        page.Controls.Add(row);
        return page;
    }

    // ---------- Refresco ----------
    record OrdenVista(int Id, string Paciente, string Medico, string Dx, TipoTerapia Tipo, string Terapeuta, string Progreso);
    record SesionVista(int Numero, string Programada, string Estado);

    void RefrescarTodo()
    {
        _gridPacientes.DataSource = _svc.Data.Pacientes.Select(p => new { p.Id, p.Nombre, Carne = p.Carne, p.Cedula, p.Ciudad, p.Telefono }).ToList();
        _gridTerapeutas.DataSource = _svc.Data.Terapeutas.Select(t => new { t.Id, t.Nombre, t.Ciudad, Especialidad = t.Especialidad.ToString(), Valor = t.ValorSesion }).ToList();

        int? selOrden = OrdenSeleccionada()?.Id;
        _gridOrdenes.DataSource = _svc.Data.Ordenes.Select(o => new OrdenVista(o.Id,
            _svc.Data.Pacientes.FirstOrDefault(p => p.Id == o.PacienteId)?.Nombre ?? "?", o.Medico, o.CodigoDiagnostico, o.Tipo,
            _svc.Data.Terapeutas.FirstOrDefault(t => t.Id == o.TerapeutaId)?.Nombre ?? "(sin asignar)",
            $"{o.SesionesRealizadas}/{o.TotalSesiones}")).ToList();
        if (selOrden is int id) SeleccionarOrden(id);

        _oPaciente.DataSource = _svc.Data.Pacientes.ToList();
        _oPaciente.DisplayMember = nameof(Paciente.Nombre);
        _oTerapeuta.DataSource = _svc.Data.Terapeutas.ToList();
        _oTerapeuta.DisplayMember = nameof(Terapeuta.Nombre);
        RefrescarSesiones();
    }

    void RefrescarSesiones()
    {
        var o = OrdenSeleccionada();
        _gridSesiones.DataSource = o is null ? new List<SesionVista>() : o.Sesiones.Select(s => new SesionVista(s.Numero,
            s.FechaProgramada.ToShortDateString(),
            s.Facturada ? "Facturada" : s.Realizada ? $"Realizada {s.FechaRealizada:d}" : "Programada")).ToList();
    }
}
