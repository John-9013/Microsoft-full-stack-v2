using System.Globalization;
using IpsAsociados.Core;

namespace IpsAsociados.Compiler;

public abstract record Instruccion(int Line);
public record InstrPaciente(int Line, string Nombre, string Carne, string Cedula, string Ciudad, string Direccion, string Telefono) : Instruccion(Line);
public record InstrTerapeuta(int Line, string Nombre, string Ciudad, TipoTerapia Tipo, decimal Valor) : Instruccion(Line);
public record InstrOrden(int Line, string CarnePaciente, string Medico, string Diagnostico, TipoTerapia Tipo, int Sesiones, decimal Valor, string? Terapeuta) : Instruccion(Line);

public class ProgramaCompilado
{
    public List<Instruccion> Instrucciones { get; } = new();
    public List<Diagnostico> Errores { get; } = new();
    public bool Exito => Errores.Count == 0;

    /// <summary>Ejecuta las instrucciones sobre el servicio. Devuelve un log de lo realizado.</summary>
    public List<string> Ejecutar(IpsService svc)
    {
        if (!Exito) throw new InvalidOperationException("El programa tiene errores de compilación.");
        var log = new List<string>();
        foreach (var ins in Instrucciones)
        {
            try
            {
                switch (ins)
                {
                    case InstrPaciente p:
                        var pac = svc.RegistrarPaciente(p.Nombre, p.Carne, p.Cedula, p.Ciudad, p.Direccion, p.Telefono);
                        log.Add($"Paciente #{pac.Id} '{pac.Nombre}' registrado.");
                        break;
                    case InstrTerapeuta t:
                        var ter = svc.RegistrarTerapeuta(t.Nombre, t.Ciudad, t.Tipo, t.Valor);
                        log.Add($"Terapeuta #{ter.Id} '{ter.Nombre}' registrado.");
                        break;
                    case InstrOrden o:
                        var paciente = svc.Data.Pacientes.First(x => x.Carne == o.CarnePaciente);
                        var orden = svc.CrearOrden(paciente.Id, o.Medico, o.Diagnostico, o.Tipo, o.Sesiones, o.Valor);
                        log.Add($"Orden #{orden.Id} creada para '{paciente.Nombre}' ({o.Sesiones} sesiones).");
                        if (o.Terapeuta is not null)
                        {
                            var terapeuta = svc.Data.Terapeutas.First(x => x.Nombre.Equals(o.Terapeuta, StringComparison.OrdinalIgnoreCase));
                            svc.AsignarTerapeuta(orden.Id, terapeuta.Id);
                            log.Add($"  Terapeuta '{terapeuta.Nombre}' asignado a la orden #{orden.Id}.");
                        }
                        break;
                }
            }
            catch (IpsException ex)
            {
                log.Add($"Línea {ins.Line}: ERROR {ex.Message}");
            }
        }
        return log;
    }
}

/// <summary>Compilador de TerapiaScript: léxico, sintaxis y análisis semántico hacia instrucciones ejecutables.</summary>
public static class TerapiaCompiler
{
    static readonly HashSet<string> ClavesPaciente = new(StringComparer.OrdinalIgnoreCase) { "carne", "cedula", "ciudad", "direccion", "telefono" };
    static readonly HashSet<string> ClavesTerapeuta = new(StringComparer.OrdinalIgnoreCase) { "ciudad", "tipo", "valor" };
    static readonly HashSet<string> ClavesOrden = new(StringComparer.OrdinalIgnoreCase) { "paciente", "medico", "diagnostico", "sesiones", "tipo", "valor", "terapeuta" };

    /// <param name="contexto">Datos existentes, para resolver pacientes y terapeutas ya registrados.</param>
    public static ProgramaCompilado Compilar(string fuente, IpsService? contexto = null)
    {
        var prog = new ProgramaCompilado();
        var tokens = new Lexer(fuente, prog.Errores).Tokenize();
        var sentencias = new Parser(tokens, prog.Errores).Parse();

        var carnes = new HashSet<string>(contexto?.Data.Pacientes.Select(p => p.Carne) ?? []);
        var terapeutas = new HashSet<string>(contexto?.Data.Terapeutas.Select(t => t.Nombre) ?? [], StringComparer.OrdinalIgnoreCase);

        foreach (var s in sentencias)
        {
            switch (s.Keyword.Value.ToLowerInvariant())
            {
                case "paciente": CompilarPaciente(s, prog, carnes); break;
                case "terapeuta": CompilarTerapeuta(s, prog, terapeutas); break;
                case "orden": CompilarOrden(s, prog, carnes, terapeutas); break;
                default: Error(prog, s.Keyword, $"Sentencia desconocida '{s.Keyword.Value}'."); break;
            }
        }
        return prog;
    }

    static void CompilarPaciente(Sentencia s, ProgramaCompilado prog, HashSet<string> carnes)
    {
        int before = prog.Errores.Count;
        var nombre = Nombre(s, prog);
        Desconocidas(s, ClavesPaciente, prog);
        var carne = Requerido(s, "carne", prog);
        if (prog.Errores.Count > before || nombre is null || carne is null) return;
        if (!carnes.Add(carne.Value))
        {
            prog.Errores.Add(new Diagnostico(carne.Line, carne.Column, $"El carné {carne.Value} ya está registrado."));
            return;
        }
        prog.Instrucciones.Add(new InstrPaciente(s.Keyword.Line, nombre, carne.Value, Opcional(s, "cedula"),
            Opcional(s, "ciudad"), Opcional(s, "direccion"), Opcional(s, "telefono")));
    }

    static void CompilarTerapeuta(Sentencia s, ProgramaCompilado prog, HashSet<string> terapeutas)
    {
        int before = prog.Errores.Count;
        var nombre = Nombre(s, prog);
        Desconocidas(s, ClavesTerapeuta, prog);
        var valor = Decimal(s, "valor", prog, requerido: true);
        var tipo = Tipo(s, prog);
        if (prog.Errores.Count > before || nombre is null || valor is null) return;
        if (!terapeutas.Add(nombre))
        {
            prog.Errores.Add(new Diagnostico(s.Nombre!.Line, s.Nombre.Column, $"El terapeuta '{nombre}' ya está registrado."));
            return;
        }
        prog.Instrucciones.Add(new InstrTerapeuta(s.Keyword.Line, nombre, Opcional(s, "ciudad"), tipo, valor.Value));
    }

    static void CompilarOrden(Sentencia s, ProgramaCompilado prog, HashSet<string> carnes, HashSet<string> terapeutas)
    {
        int before = prog.Errores.Count;
        if (s.Nombre is not null) prog.Errores.Add(new Diagnostico(s.Nombre.Line, s.Nombre.Column, "'orden' no admite un nombre posicional."));
        Desconocidas(s, ClavesOrden, prog);
        var paciente = Requerido(s, "paciente", prog);
        var medico = Requerido(s, "medico", prog);
        var diag = Requerido(s, "diagnostico", prog);
        var sesiones = Requerido(s, "sesiones", prog);
        var valor = Decimal(s, "valor", prog, requerido: false);
        var tipo = Tipo(s, prog);

        int n = 0;
        if (sesiones is not null && (!int.TryParse(sesiones.Value, out n) || n < 1))
            prog.Errores.Add(new Diagnostico(sesiones.Line, sesiones.Column, "El número de sesiones debe ser un entero mayor o igual a 1."));
        if (paciente is not null && !carnes.Contains(paciente.Value))
            prog.Errores.Add(new Diagnostico(paciente.Line, paciente.Column, $"Paciente con carné {paciente.Value} no definido."));
        if (s.Args.TryGetValue("terapeuta", out var ter) && !terapeutas.Contains(ter.Value))
            prog.Errores.Add(new Diagnostico(ter.Line, ter.Column, $"Terapeuta '{ter.Value}' no definido."));

        if (prog.Errores.Count > before) return;
        prog.Instrucciones.Add(new InstrOrden(s.Keyword.Line, paciente!.Value, medico!.Value, diag!.Value, tipo, n,
            valor ?? 0m, s.Args.TryGetValue("terapeuta", out var t2) ? t2.Value : null));
    }

    static string? Nombre(Sentencia s, ProgramaCompilado prog)
    {
        if (s.Nombre is null || string.IsNullOrWhiteSpace(s.Nombre.Value))
        {
            Error(prog, s.Keyword, $"'{s.Keyword.Value}' requiere un nombre entre comillas.");
            return null;
        }
        return s.Nombre.Value;
    }

    static Argumento? Requerido(Sentencia s, string clave, ProgramaCompilado prog)
    {
        if (s.Args.TryGetValue(clave, out var a)) return a;
        Error(prog, s.Keyword, $"Falta el argumento obligatorio '{clave}'.");
        return null;
    }

    static string Opcional(Sentencia s, string clave) => s.Args.TryGetValue(clave, out var a) ? a.Value : "";

    static decimal? Decimal(Sentencia s, string clave, ProgramaCompilado prog, bool requerido)
    {
        if (!s.Args.TryGetValue(clave, out var a))
        {
            if (requerido) Error(prog, s.Keyword, $"Falta el argumento obligatorio '{clave}'.");
            return null;
        }
        if (!decimal.TryParse(a.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) || v < 0 || (requerido && v == 0))
        {
            prog.Errores.Add(new Diagnostico(a.Line, a.Column, $"'{clave}' debe ser un número positivo (use punto decimal)."));
            return null;
        }
        return v;
    }

    static TipoTerapia Tipo(Sentencia s, ProgramaCompilado prog)
    {
        if (!s.Args.TryGetValue("tipo", out var a)) return TipoTerapia.Fisica;
        switch (a.Value.ToLowerInvariant())
        {
            case "fisica": return TipoTerapia.Fisica;
            case "respiratoria": return TipoTerapia.Respiratoria;
            default:
                prog.Errores.Add(new Diagnostico(a.Line, a.Column, "El tipo debe ser 'fisica' o 'respiratoria'."));
                return TipoTerapia.Fisica;
        }
    }

    static void Desconocidas(Sentencia s, HashSet<string> validas, ProgramaCompilado prog)
    {
        foreach (var (k, v) in s.Args.Where(a => !validas.Contains(a.Key)))
            prog.Errores.Add(new Diagnostico(v.Line, v.Column, $"Argumento desconocido '{k}' en '{s.Keyword.Value}'."));
    }

    static void Error(ProgramaCompilado prog, Token t, string msg) => prog.Errores.Add(new Diagnostico(t.Line, t.Column, msg));
}
