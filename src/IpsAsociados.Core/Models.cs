namespace IpsAsociados.Core;

public enum TipoTerapia { Fisica, Respiratoria }

public class Paciente
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Carne { get; set; } = "";
    public string Cedula { get; set; } = "";
    public string Ciudad { get; set; } = "";
    public string Direccion { get; set; } = "";
    public string Telefono { get; set; } = "";
}

public class Terapeuta
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Ciudad { get; set; } = "";
    public TipoTerapia Especialidad { get; set; }
    public decimal ValorSesion { get; set; }
}

public class Sesion
{
    public int Numero { get; set; }
    public DateTime FechaProgramada { get; set; }
    public bool Realizada { get; set; }
    public bool FirmadaPorPaciente { get; set; }
    public DateTime? FechaRealizada { get; set; }
    public bool Facturada { get; set; }
    public bool Pagada { get; set; }
}

public class Orden
{
    public int Id { get; set; }
    public int PacienteId { get; set; }
    public string Medico { get; set; } = "";
    public string CodigoDiagnostico { get; set; } = "";
    public TipoTerapia Tipo { get; set; }
    public int TotalSesiones { get; set; }
    public DateTime FechaEmision { get; set; } = DateTime.Today;
    public int? TerapeutaId { get; set; }
    public decimal ValorUnitario { get; set; }
    public List<Sesion> Sesiones { get; set; } = new();

    public int SesionesRealizadas => Sesiones.Count(s => s.Realizada);
    public bool Cumplida => SesionesRealizadas >= TotalSesiones;
}

public class PlanillaLinea
{
    public int OrdenId { get; set; }
    public string CodigoDiagnostico { get; set; } = "";
    public DateTime FechaSesion { get; set; }
    public decimal ValorUnitario { get; set; }
    public string Cedula { get; set; } = "";
    public string Carne { get; set; } = "";
}

public class PlanillaGeneral
{
    public DateTime FechaEnvio { get; set; } = DateTime.Today;
    public List<PlanillaLinea> Lineas { get; set; } = new();
    public decimal Total => Lineas.Sum(l => l.ValorUnitario);
}

public record HonorarioTerapeuta(int TerapeutaId, string Terapeuta, int Sesiones, decimal Total);

public class IpsData
{
    public int SiguientePacienteId { get; set; } = 1;
    public int SiguienteTerapeutaId { get; set; } = 1;
    public int SiguienteOrdenId { get; set; } = 1;
    public List<Paciente> Pacientes { get; set; } = new();
    public List<Terapeuta> Terapeutas { get; set; } = new();
    public List<Orden> Ordenes { get; set; } = new();
}
