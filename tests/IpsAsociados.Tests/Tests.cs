using IpsAsociados.Agile;
using IpsAsociados.Compiler;
using IpsAsociados.Core;

namespace IpsAsociados.Tests;

public class CoreTests
{
    static (IpsService svc, Paciente p, Terapeuta t, Orden o) Escenario(int sesiones = 3)
    {
        var svc = new IpsService();
        var p = svc.RegistrarPaciente("Ana Ruiz", "C-100", "123456", "Bogotá");
        var t = svc.RegistrarTerapeuta("Luis Pérez", "Bogotá", TipoTerapia.Fisica, 40m);
        var o = svc.CrearOrden(p.Id, "Dr. Gómez", "m54.5", TipoTerapia.Fisica, sesiones, 60m);
        svc.AsignarTerapeuta(o.Id, t.Id);
        return (svc, p, t, o);
    }

    [Fact]
    public void CarneDuplicado_Falla()
    {
        var svc = new IpsService();
        svc.RegistrarPaciente("A", "1", "1", "X");
        Assert.Throws<IpsException>(() => svc.RegistrarPaciente("B", "1", "2", "Y"));
    }

    [Fact]
    public void Terapeuta_DebeSerDeLaEspecialidadDeLaOrden()
    {
        var (svc, _, _, o) = Escenario();
        var resp = svc.RegistrarTerapeuta("Rosa", "Cali", TipoTerapia.Respiratoria, 50m);
        Assert.Throws<IpsException>(() => svc.AsignarTerapeuta(o.Id, resp.Id));
    }

    [Fact]
    public void NoSePuedeProgramarMasSesionesQueLasOrdenadas()
    {
        var (svc, _, _, o) = Escenario(2);
        svc.ProgramarSesion(o.Id, new DateTime(2026, 1, 5));
        svc.ProgramarSesion(o.Id, new DateTime(2026, 1, 7));
        Assert.Throws<IpsException>(() => svc.ProgramarSesion(o.Id, new DateTime(2026, 1, 9)));
    }

    [Fact]
    public void PlanillaYHonorarios_SoloContanSesionesRealizadas()
    {
        var (svc, _, t, o) = Escenario(3);
        for (int i = 1; i <= 3; i++) svc.ProgramarSesion(o.Id, new DateTime(2026, 1, 5 + i));
        svc.RegistrarSesionRealizada(o.Id, 1, new DateTime(2026, 1, 6));
        svc.RegistrarSesionRealizada(o.Id, 2, new DateTime(2026, 1, 7));

        var planilla = svc.GenerarPlanilla(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31), marcarFacturadas: true);
        Assert.Equal(2, planilla.Lineas.Count);
        Assert.Equal(120m, planilla.Total);
        Assert.Equal("M54.5", planilla.Lineas[0].CodigoDiagnostico);

        Assert.Empty(svc.GenerarPlanilla(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31)).Lineas);

        var h = Assert.Single(svc.CalcularHonorarios(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31)));
        Assert.Equal(t.Id, h.TerapeutaId);
        Assert.Equal(80m, h.Total);
    }

    [Fact]
    public void SesionSinFirma_NoSeReporta()
    {
        var (svc, _, _, o) = Escenario();
        svc.ProgramarSesion(o.Id, DateTime.Today);
        Assert.Throws<IpsException>(() => svc.RegistrarSesionRealizada(o.Id, 1, DateTime.Today, firmada: false));
    }

    [Fact]
    public void Persistencia_GuardaYCarga()
    {
        var (svc, _, _, _) = Escenario();
        var path = Path.Combine(Path.GetTempPath(), $"ips-{Guid.NewGuid():N}.json");
        try
        {
            svc.Save(path);
            var cargado = IpsService.Load(path);
            Assert.Single(cargado.Data.Pacientes);
            Assert.Single(cargado.Data.Ordenes);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void CsvPlanilla_IncluyeTotal()
    {
        var (svc, _, _, o) = Escenario(1);
        svc.ProgramarSesion(o.Id, new DateTime(2026, 2, 2));
        svc.RegistrarSesionRealizada(o.Id, 1, new DateTime(2026, 2, 2));
        var csv = IpsService.ExportarPlanillaCsv(svc.GenerarPlanilla(new DateTime(2026, 2, 1), new DateTime(2026, 2, 28)));
        Assert.Contains("M54.5;2026-02-02;60;123456;C-100", csv);
        Assert.Contains("TOTAL;", csv);
    }
}

public class CompilerTests
{
    const string Valido = """
        # datos iniciales
        paciente "Ana Ruiz" carne C-100 cedula 123 ciudad "Bogotá";
        terapeuta "Luis Pérez" tipo fisica valor 40.5 ciudad Bogotá;
        orden paciente C-100 medico "Dr. Gómez" diagnostico M54.5 sesiones 5 valor 60 terapeuta "Luis Pérez";
        """;

    [Fact]
    public void ProgramaValido_CompilaYEjecuta()
    {
        var prog = TerapiaCompiler.Compilar(Valido);
        Assert.True(prog.Exito, string.Join("\n", prog.Errores));
        Assert.Equal(3, prog.Instrucciones.Count);

        var svc = new IpsService();
        var log = prog.Ejecutar(svc);
        Assert.Equal(4, log.Count);
        Assert.Single(svc.Data.Pacientes);
        Assert.Equal(5, svc.Data.Ordenes[0].TotalSesiones);
        Assert.NotNull(svc.Data.Ordenes[0].TerapeutaId);
        Assert.Equal(40.5m, svc.Data.Terapeutas[0].ValorSesion);
    }

    [Fact]
    public void FaltaPuntoYComa_ReportaError()
    {
        var prog = TerapiaCompiler.Compilar("paciente \"A\" carne 1");
        Assert.Contains(prog.Errores, e => e.Message.Contains(';'));
    }

    [Fact]
    public void PacienteNoDefinido_ErrorSemantico()
    {
        var prog = TerapiaCompiler.Compilar("orden paciente X medico M diagnostico D sesiones 1;");
        Assert.Contains(prog.Errores, e => e.Message.Contains("no definido"));
        Assert.Throws<InvalidOperationException>(() => prog.Ejecutar(new IpsService()));
    }

    [Theory]
    [InlineData("orden paciente 1 medico M diagnostico D sesiones 0;", "sesiones")]
    [InlineData("paciente \"A\" carne 1 color rojo;", "desconocido")]
    [InlineData("hola;", "desconocida")]
    [InlineData("paciente \"A;", "sin cerrar")]
    public void ErroresComunes_SeDetectan(string fuente, string fragmento)
    {
        var prog = TerapiaCompiler.Compilar("paciente \"P\" carne 1;\n" + fuente);
        Assert.Contains(prog.Errores, e => e.Message.Contains(fragmento, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RecuperaDeErrores_YSigueAnalizando()
    {
        var prog = TerapiaCompiler.Compilar("paciente carne 1;\nhola;\nterapeuta \"T\" valor -3;");
        Assert.True(prog.Errores.Count >= 3);
    }

    [Fact]
    public void UsaContextoExistente()
    {
        var svc = new IpsService();
        svc.RegistrarPaciente("Ana", "C-9", "1", "X");
        var prog = TerapiaCompiler.Compilar("orden paciente C-9 medico M diagnostico D sesiones 2;", svc);
        Assert.True(prog.Exito);
    }
}

public class AgileTests
{
    [Fact]
    public void PlanIps_EsConsistente()
    {
        var plan = PlanScrum.IpsAsociados();
        Assert.Empty(plan.Validar());
        Assert.Equal(24, plan.DuracionSemanas);
        Assert.All(plan.Sprints, s => Assert.Contains(plan.Backlog, h => h.Sprint == s.Numero));
    }

    [Fact]
    public void Informe_ContieneSeccionesClave()
    {
        var md = PlanScrum.IpsAsociados().GenerarInformeMarkdown();
        foreach (var s in new[] { "Roles y responsables", "Product Backlog", "Cronograma de sprints", "Ceremonias", "Riesgos", "sindicalizados" })
            Assert.Contains(s, md);
    }
}
