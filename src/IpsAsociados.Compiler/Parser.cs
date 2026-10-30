namespace IpsAsociados.Compiler;

public record Argumento(string Value, int Line, int Column);

/// <summary>Sentencia: palabra clave, nombre posicional opcional y pares clave/valor.</summary>
public class Sentencia
{
    public required Token Keyword { get; init; }
    public Argumento? Nombre { get; set; }
    public Dictionary<string, Argumento> Args { get; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Analizador sintáctico: sentencia := palabra [texto] (clave valor)* ';'</summary>
public class Parser(List<Token> tokens, List<Diagnostico> errors)
{
    int _i;

    public List<Sentencia> Parse()
    {
        var result = new List<Sentencia>();
        while (Current.Kind != TokenKind.Fin)
        {
            var s = ParseSentencia();
            if (s is not null) result.Add(s);
        }
        return result;
    }

    Token Current => tokens[_i];

    Sentencia? ParseSentencia()
    {
        var kw = Current;
        if (kw.Kind != TokenKind.Palabra)
        {
            errors.Add(new Diagnostico(kw.Line, kw.Column, "Se esperaba una palabra clave (paciente, terapeuta, orden)."));
            Synchronize();
            return null;
        }
        _i++;
        var s = new Sentencia { Keyword = kw };

        if (Current.Kind == TokenKind.Texto)
        {
            s.Nombre = new Argumento(Current.Value, Current.Line, Current.Column);
            _i++;
        }

        while (Current.Kind is TokenKind.Palabra)
        {
            var key = Current;
            _i++;
            if (Current.Kind is TokenKind.PuntoYComa or TokenKind.Fin)
            {
                errors.Add(new Diagnostico(key.Line, key.Column, $"Falta el valor para '{key.Value}'."));
                break;
            }
            var val = Current;
            _i++;
            if (!s.Args.TryAdd(key.Value, new Argumento(val.Value, val.Line, val.Column)))
                errors.Add(new Diagnostico(key.Line, key.Column, $"El argumento '{key.Value}' está repetido."));
        }

        if (Current.Kind == TokenKind.PuntoYComa) _i++;
        else
        {
            errors.Add(new Diagnostico(Current.Line, Current.Column, "Se esperaba ';' al final de la sentencia."));
            Synchronize();
        }
        return s;
    }

    void Synchronize()
    {
        while (Current.Kind is not (TokenKind.PuntoYComa or TokenKind.Fin)) _i++;
        if (Current.Kind == TokenKind.PuntoYComa) _i++;
    }
}
