namespace IpsAsociados.Compiler;

public enum TokenKind { Palabra, Texto, PuntoYComa, Fin }

public record Token(TokenKind Kind, string Value, int Line, int Column);

public record Diagnostico(int Line, int Column, string Message)
{
    public override string ToString() => $"({Line},{Column}) {Message}";
}

/// <summary>Analizador léxico de TerapiaScript. Comentarios con '#', textos entre comillas dobles.</summary>
public class Lexer(string source, List<Diagnostico> errors)
{
    int _pos, _line = 1, _col = 1;

    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();
        while (_pos < source.Length)
        {
            char c = source[_pos];
            if (c == '\n') { Advance(); _line++; _col = 1; }
            else if (char.IsWhiteSpace(c)) Advance();
            else if (c == '#') { while (_pos < source.Length && source[_pos] != '\n') Advance(); }
            else if (c == ';') { tokens.Add(new Token(TokenKind.PuntoYComa, ";", _line, _col)); Advance(); }
            else if (c == '"') tokens.Add(ReadString());
            else tokens.Add(ReadWord());
        }
        tokens.Add(new Token(TokenKind.Fin, "", _line, _col));
        return tokens;
    }

    Token ReadString()
    {
        int line = _line, col = _col;
        Advance();
        int start = _pos;
        while (_pos < source.Length && source[_pos] != '"' && source[_pos] != '\n') Advance();
        string text = source[start.._pos];
        if (_pos < source.Length && source[_pos] == '"') Advance();
        else errors.Add(new Diagnostico(line, col, "Texto sin cerrar: falta la comilla de cierre."));
        return new Token(TokenKind.Texto, text, line, col);
    }

    Token ReadWord()
    {
        int line = _line, col = _col, start = _pos;
        while (_pos < source.Length && !char.IsWhiteSpace(source[_pos]) && source[_pos] != ';' && source[_pos] != '"' && source[_pos] != '#')
            Advance();
        return new Token(TokenKind.Palabra, source[start.._pos], line, col);
    }

    void Advance() { _pos++; _col++; }
}
