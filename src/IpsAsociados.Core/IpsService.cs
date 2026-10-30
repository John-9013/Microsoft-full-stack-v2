using System.Globalization;
using System.Text;
using System.Text.Json;

namespace IpsAsociados.Core;

public class IpsException(string message) : Exception(message);

/// <summary>Lógica de negocio de IPS Asociados: pacientes, terapeutas, órdenes, sesiones, planilla y honorarios.</summary>
public class IpsService
{
    static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public IpsData Data { get; private set; }

    public IpsService(IpsData? data = null) => Data = data ?? new IpsData();

    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IpsAsociados", "data.json");

    public static IpsService Load(string? path = null)
    {
        path ??= DefaultPath;
        if (!File.Exists(path)) return new IpsService();
        var data = JsonSerializer.Deserialize<IpsData>(File.ReadAllText(path), JsonOptions);
        return new IpsService(data);
    }

    public void Save(string? path = null)
    {
        path ??= DefaultPath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(Data, JsonOptions));
    }

    public Paciente RegistrarPaciente(string nombre, string carne, string cedula, string ciudad,
        string direccion = "", string telefono = "")
    {
        if (string.IsNullOrWhiteSpace(nombre)) throw new IpsException("El nombre del paciente es obligatorio.");
        if (string.IsNullOrWhiteSpace(carne)) throw new IpsException("El número de historia clínica o carné es obligatorio.");
        if (Data.Pacientes.Any(p => p.Carne == carne)) throw new IpsException($"Ya existe un paciente con carné {carne}.");

        var p = new Paciente
        {
            Id = Data.SiguientePacienteId++, Nombre = nombre.Trim(), Carne = carne.Trim(), Cedula = cedula.Trim(),
            Ciudad = ciudad.Trim(), Direccion = direccion.Trim(), Telefono = telefono.Trim()
        };
        Data.Pacientes.Add(p);
        return p;
    }

    public Terapeuta RegistrarTerapeuta(string nombre, string ciudad, TipoTerapia especialidad, decimal valorSesion)
    {
        if (string.IsNullOrWhiteSpace(nombre)) throw new IpsException("El nombre del terapeuta es obligatorio.");
        if (valorSesion <= 0) throw new IpsException("El valor por sesión debe ser mayor que cero.");

        var t = new Terapeuta
        {
            Id = Data.SiguienteTerapeutaId++, Nombre = nombre.Trim(), Ciudad = ciudad.Trim(),
            Especialidad = especialidad, ValorSesion = valorSesion
        };
        Data.Terapeutas.Add(t);
        return t;
    }

    public Orden CrearOrden(int pacienteId, string medico, string codigoDiagnostico, TipoTerapia tipo,
        int totalSesiones, decimal valorUnitario)
    {
        if (Data.Pacientes.All(p => p.Id != pacienteId)) throw new IpsException("Paciente no encontrado.");
        if (string.IsNullOrWhiteSpace(medico)) throw new IpsException("El nombre del médico es obligatorio.");
        if (string.IsNullOrWhiteSpace(codigoDiagnostico)) throw new IpsException("El código de diagnóstico es obligatorio.");
        if (totalSesiones < 1) throw new IpsException("La orden debe tener al menos una sesión.");
        if (valorUnitario < 0) throw new IpsException("El valor unitario no puede ser negativo.");

        var o = new Orden
        {
            Id = Data.SiguienteOrdenId++, PacienteId = pacienteId, Medico = medico.Trim(),
            CodigoDiagnostico = codigoDiagnostico.Trim().ToUpperInvariant(), Tipo = tipo,
            TotalSesiones = totalSesiones, ValorUnitario = valorUnitario
        };
        Data.Ordenes.Add(o);
        return o;
    }

    public void AsignarTerapeuta(int ordenId, int terapeutaId)
    {
        var o = ObtenerOrden(ordenId);
        var t = Data.Terapeutas.FirstOrDefault(x => x.Id == terapeutaId) ?? throw new IpsException("Terapeuta no encontrado.");
        if (t.Especialidad != o.Tipo)
            throw new IpsException($"El terapeuta es de terapia {t.Especialidad} y la orden requiere {o.Tipo}.");
        o.TerapeutaId = t.Id;
    }

    public Sesion ProgramarSesion(int ordenId, DateTime fecha)
    {
        var o = ObtenerOrden(ordenId);
        if (o.TerapeutaId is null) throw new IpsException("Asigne un terapeuta antes de programar sesiones.");
        if (o.Sesiones.Count >= o.TotalSesiones) throw new IpsException("La orden ya tiene todas sus sesiones programadas.");
        var s = new Sesion { Numero = o.Sesiones.Count + 1, FechaProgramada = fecha.Date };
        o.Sesiones.Add(s);
        return s;
    }

    /// <summary>Registra la asistencia de una sesión (planilla firmada por el paciente).</summary>
    public void RegistrarSesionRealizada(int ordenId, int numeroSesion, DateTime fecha, bool firmada = true)
    {
        var s = ObtenerOrden(ordenId).Sesiones.FirstOrDefault(x => x.Numero == numeroSesion)
                ?? throw new IpsException("Sesión no encontrada.");
        if (!firmada) throw new IpsException("La sesión solo se reporta si el paciente firmó la planilla.");
        if (s.Realizada) throw new IpsException("La sesión ya fue reportada.");
        s.Realizada = true;
        s.FirmadaPorPaciente = true;
        s.FechaRealizada = fecha.Date;
    }

    /// <summary>Planilla general con las sesiones realizadas y aún no facturadas. Marca como facturadas si se pide.</summary>
    public PlanillaGeneral GenerarPlanilla(DateTime desde, DateTime hasta, bool marcarFacturadas = false)
    {
        var planilla = new PlanillaGeneral();
        foreach (var o in Data.Ordenes)
        {
            var p = Data.Pacientes.First(x => x.Id == o.PacienteId);
            foreach (var s in o.Sesiones.Where(s => s.Realizada && !s.Facturada
                         && s.FechaRealizada!.Value.Date >= desde.Date && s.FechaRealizada.Value.Date <= hasta.Date))
            {
                planilla.Lineas.Add(new PlanillaLinea
                {
                    OrdenId = o.Id, CodigoDiagnostico = o.CodigoDiagnostico, FechaSesion = s.FechaRealizada!.Value,
                    ValorUnitario = o.ValorUnitario, Cedula = p.Cedula, Carne = p.Carne
                });
                if (marcarFacturadas) s.Facturada = true;
            }
        }
        planilla.Lineas = planilla.Lineas.OrderBy(l => l.FechaSesion).ThenBy(l => l.OrdenId).ToList();
        return planilla;
    }

    /// <summary>Honorarios por terapeuta: sesiones realizadas en el rango, pagadas por sesión cumplida y reportada.</summary>
    public List<HonorarioTerapeuta> CalcularHonorarios(DateTime desde, DateTime hasta)
    {
        return Data.Terapeutas.Select(t =>
        {
            var n = Data.Ordenes.Where(o => o.TerapeutaId == t.Id)
                .SelectMany(o => o.Sesiones)
                .Count(s => s.Realizada && s.FechaRealizada!.Value.Date >= desde.Date && s.FechaRealizada.Value.Date <= hasta.Date);
            return new HonorarioTerapeuta(t.Id, t.Nombre, n, n * t.ValorSesion);
        }).Where(h => h.Sesiones > 0).ToList();
    }

    /// <summary>CSV de la planilla general (formato de cruce tipo RIPS: diagnóstico, fecha, valor, cédula, carné).</summary>
    public static string ExportarPlanillaCsv(PlanillaGeneral planilla)
    {
        var sb = new StringBuilder();
        sb.AppendLine("CodigoDiagnostico;FechaSesion;ValorUnitario;Cedula;Carne");
        foreach (var l in planilla.Lineas)
            sb.AppendLine(string.Join(';', l.CodigoDiagnostico, l.FechaSesion.ToString("yyyy-MM-dd"),
                l.ValorUnitario.ToString("0.##", CultureInfo.InvariantCulture), l.Cedula, l.Carne));
        sb.AppendLine($"TOTAL;{planilla.FechaEnvio:yyyy-MM-dd};{planilla.Total.ToString("0.##", CultureInfo.InvariantCulture)};;");
        return sb.ToString();
    }

    public Orden ObtenerOrden(int id) =>
        Data.Ordenes.FirstOrDefault(o => o.Id == id) ?? throw new IpsException($"Orden {id} no encontrada.");
}
