using System.Text;

namespace IpsAsociados.Agile;

public record Rol(string Nombre, string Responsabilidad, string Persona);

public record HistoriaUsuario(string Id, string Epica, string Titulo, string Descripcion, int Puntos, int Prioridad, int Sprint, string Responsable);

public record Sprint(int Numero, string Objetivo, int SemanaInicio, int SemanaFin, string Entregable);

public record Ceremonia(string Nombre, string Frecuencia, string Duracion, string Participantes, string Proposito);

public record Riesgo(string Descripcion, string Mitigacion);

/// <summary>Plan Scrum para el caso IPS Asociados (6 meses = 12 sprints de 2 semanas, presupuesto máx. USD 10.000).</summary>
public class PlanScrum
{
    public string Proyecto { get; init; } = "Sistema de gestión de terapias domiciliarias - IPS Asociados";
    public decimal PresupuestoUsd { get; init; } = 10_000m;
    public int SemanasPorSprint { get; init; } = 2;
    public List<Rol> Roles { get; init; } = new();
    public List<HistoriaUsuario> Backlog { get; init; } = new();
    public List<Sprint> Sprints { get; init; } = new();
    public List<Ceremonia> Ceremonias { get; init; } = new();
    public List<Riesgo> Riesgos { get; init; } = new();

    public int TotalPuntos => Backlog.Sum(h => h.Puntos);
    public int DuracionSemanas => Sprints.Count == 0 ? 0 : Sprints.Max(s => s.SemanaFin);

    public static PlanScrum IpsAsociados()
    {
        var plan = new PlanScrum();

        plan.Roles.AddRange([
            new Rol("Product Owner", "Prioriza el backlog y acepta entregables", "Coordinador de operaciones regional (IPS)"),
            new Rol("Scrum Master", "Facilita ceremonias, remueve impedimentos, gestiona la resistencia al cambio", "Gerente de proyectos (innova-soft)"),
            new Rol("Arquitecto de software", "Define arquitectura, integración RIPS y estándares de calidad", "Arquitecto (innova-soft)"),
            new Rol("DBA", "Modelo de datos de pacientes, órdenes y sesiones; migración de MySQL", "Administrador de base de datos (innova-soft)"),
            new Rol("Equipo de desarrollo", "Construye, prueba y documenta los incrementos", "Desarrolladores (innova-soft)"),
            new Rol("Usuarios clave", "Validan usabilidad en las revisiones de sprint", "Coordinadores regionales, terapeutas y representantes sindicales")
        ]);

        plan.Backlog.AddRange([
            new("HU-01", "Base de datos", "Base de datos de pacientes", "Como coordinador quiero registrar pacientes con carné, cédula y ciudad para tener un control actualizado.", 5, 1, 1, "DBA"),
            new("HU-02", "Base de datos", "Registro de terapeutas", "Como coordinador quiero registrar terapeutas con ciudad, especialidad y valor por sesión.", 3, 2, 1, "Equipo de desarrollo"),
            new("HU-03", "Órdenes", "Registro de remisiones", "Como coordinador quiero registrar una orden con médico, diagnóstico y número de sesiones (1, 5, 10 o las que ordene el médico).", 8, 3, 2, "Equipo de desarrollo"),
            new("HU-04", "Órdenes", "Asignación de terapeuta", "Como coordinador quiero asignar un terapeuta a la orden según especialidad y ciudad.", 5, 4, 3, "Equipo de desarrollo"),
            new("HU-05", "Sesiones", "Programación de sesiones", "Como terapeuta quiero acordar y registrar las fechas de las sesiones domiciliarias.", 8, 5, 4, "Equipo de desarrollo"),
            new("HU-06", "Sesiones", "Registro de asistencia firmada", "Como terapeuta quiero reportar cada sesión realizada con la firma del paciente.", 8, 6, 5, "Equipo de desarrollo"),
            new("HU-13", "Calidad", "Pruebas automatizadas e integración continua", "Como equipo quiero pruebas automáticas y un pipeline de integración continua para detectar fallas temprano.", 5, 6, 6, "Equipo de desarrollo"),
            new("HU-07", "Facturación", "Planilla general", "Como administrador quiero generar la planilla con diagnóstico, fecha, valor, cédula, carné y total.", 13, 7, 7, "Arquitecto de software"),
            new("HU-08", "Facturación", "Honorarios por terapeuta", "Como administrador quiero calcular automáticamente lo que se debe pagar a cada terapeuta.", 8, 8, 8, "Equipo de desarrollo"),
            new("HU-09", "Integración", "Cruce RIPS con la petrolera", "Como funcionario quiero exportar el cruce mensual RIPS sin proceso manual.", 13, 9, 9, "Arquitecto de software"),
            new("HU-10", "Web", "Corrección de navegación del sitio PHP", "Como usuario quiero menús sin enlaces rotos y concertados conmigo.", 5, 10, 10, "Equipo de desarrollo"),
            new("HU-11", "Calidad", "Manuales y pruebas", "Como organización quiero manual técnico, manual de usuario y plan de pruebas documentados.", 8, 11, 11, "Equipo de desarrollo"),
            new("HU-12", "Cambio", "Capacitación y acompañamiento", "Como empleado quiero capacitación para usar el sistema y entender que apoya mi trabajo.", 5, 12, 12, "Scrum Master")
        ]);

        string[] objetivos = [
            "Modelo de datos y registro de pacientes/terapeutas", "Registro de remisiones (órdenes)", "Asignación de terapeutas",
            "Programación de sesiones", "Asistencia firmada", "Refactor, pruebas e integración continua",
            "Planilla general", "Honorarios", "Cruce RIPS", "Corrección del sitio web", "Documentación y pruebas finales", "Capacitación y despliegue en 4 regiones" ];
        for (int i = 0; i < 12; i++)
            plan.Sprints.Add(new Sprint(i + 1, objetivos[i], i * 2 + 1, i * 2 + 2, $"Incremento funcional del sprint {i + 1} validado en revisión"));

        plan.Ceremonias.AddRange([
            new("Planeación del sprint", "Inicio de cada sprint", "4 h", "Equipo, PO, SM", "Seleccionar historias y acordar el objetivo del sprint"),
            new("Scrum diario", "Diaria", "15 min", "Equipo de desarrollo, SM", "Sincronizar avance e impedimentos"),
            new("Revisión del sprint", "Fin de cada sprint", "2 h", "Equipo, PO, usuarios clave", "Demostrar el incremento y recibir retroalimentación"),
            new("Retrospectiva", "Fin de cada sprint", "1 h", "Equipo, SM", "Mejorar el proceso y la colaboración"),
            new("Refinamiento del backlog", "Semanal", "1 h", "PO, equipo", "Detallar y estimar historias futuras")
        ]);

        plan.Riesgos.AddRange([
            new("Resistencia de empleados sindicalizados por temor a perder su puesto", "Involucrarlos como usuarios clave, comunicar que el sistema elimina trabajo manual repetitivo, capacitación"),
            new("Presupuesto limitado (USD 10.000)", "Backlog priorizado, alcance por incrementos, control de velocidad por sprint"),
            new("Formato RIPS sin especificación técnica", "Sprint de descubrimiento con la petrolera y pruebas de cruce tempranas"),
            new("Cobertura en 7 capitales y 20+ ciudades", "Despliegue web centralizado y coordinadores regionales como puntos de contacto"),
            new("Entrega anterior sin documentación ni pruebas", "Definición de terminado (DoD) que exige pruebas y documentación en cada historia")
        ]);

        return plan;
    }

    public string[] Validar()
    {
        var errores = new List<string>();
        foreach (var h in Backlog)
        {
            if (Sprints.All(s => s.Numero != h.Sprint)) errores.Add($"{h.Id}: sprint {h.Sprint} inexistente.");
            if (Roles.All(r => r.Nombre != h.Responsable)) errores.Add($"{h.Id}: responsable '{h.Responsable}' no es un rol definido.");
            if (h.Puntos <= 0) errores.Add($"{h.Id}: puntos de historia inválidos.");
        }
        foreach (var g in Backlog.GroupBy(h => h.Id).Where(g => g.Count() > 1)) errores.Add($"Id duplicado {g.Key}.");
        return errores.ToArray();
    }

    /// <summary>Informe en Markdown con la estructura de procedimientos técnicos del modelo ágil.</summary>
    public string GenerarInformeMarkdown()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Procedimientos técnicos en un modelo ágil (Scrum)\n\n**Proyecto:** {Proyecto}  ");
        sb.AppendLine($"**Duración:** {DuracionSemanas} semanas ({Sprints.Count} sprints de {SemanasPorSprint} semanas)  ");
        sb.AppendLine($"**Presupuesto máximo:** USD {PresupuestoUsd:N0}  \n**Puntos de historia totales:** {TotalPuntos}\n");

        sb.AppendLine("## 1. Roles y responsables\n\n| Rol | Responsabilidad | Responsable |\n|---|---|---|");
        foreach (var r in Roles) sb.AppendLine($"| {r.Nombre} | {r.Responsabilidad} | {r.Persona} |");

        sb.AppendLine("\n## 2. Product Backlog\n\n| Id | Épica | Historia | Puntos | Prioridad | Sprint | Responsable |\n|---|---|---|---|---|---|---|");
        foreach (var h in Backlog.OrderBy(h => h.Prioridad)) sb.AppendLine($"| {h.Id} | {h.Epica} | {h.Titulo} | {h.Puntos} | {h.Prioridad} | {h.Sprint} | {h.Responsable} |");

        sb.AppendLine("\n## 3. Cronograma de sprints\n\n| Sprint | Semanas | Objetivo | Historias | Puntos |\n|---|---|---|---|---|");
        foreach (var s in Sprints)
        {
            var hs = Backlog.Where(h => h.Sprint == s.Numero).ToList();
            sb.AppendLine($"| {s.Numero} | {s.SemanaInicio}-{s.SemanaFin} | {s.Objetivo} | {string.Join(", ", hs.Select(h => h.Id))} | {hs.Sum(h => h.Puntos)} |");
        }

        sb.AppendLine("\n## 4. Ceremonias\n\n| Ceremonia | Frecuencia | Duración | Participantes | Propósito |\n|---|---|---|---|---|");
        foreach (var c in Ceremonias) sb.AppendLine($"| {c.Nombre} | {c.Frecuencia} | {c.Duracion} | {c.Participantes} | {c.Proposito} |");

        sb.AppendLine("\n## 5. Riesgos y mitigación\n\n| Riesgo | Mitigación |\n|---|---|");
        foreach (var r in Riesgos) sb.AppendLine($"| {r.Descripcion} | {r.Mitigacion} |");

        sb.AppendLine("\n## 6. Definición de terminado (DoD)\n\n- Código revisado y con pruebas automatizadas aprobadas.\n- Documentación técnica y de usuario actualizada.\n- Validado por el Product Owner en la revisión del sprint.\n- Sin enlaces rotos ni opciones de menú no concertadas.");

        sb.AppendLine("\n## 7. Calidad y uso de recursos\n\nLas entregas cortas y la retroalimentación de usuarios reales reducen el retrabajo y el riesgo de repetir el desarrollo anterior (sin pruebas ni manuales). El presupuesto se controla por velocidad de sprint y se prioriza el valor de negocio (control de pacientes, planilla y honorarios) antes que funciones secundarias.");
        return sb.ToString();
    }
}
