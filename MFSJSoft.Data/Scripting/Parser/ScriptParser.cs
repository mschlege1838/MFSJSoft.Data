
using System;
using System.Text;
using System.Collections.Generic;

using MFSJSoft.Data.Scripting.Model;

namespace MFSJSoft.Data.Scripting.Parser
{
    enum TokenType
    {
        LINE_COMMENT_TOK,
        BLOCK_START,
        BLOCK_STOP,
        REG_TOK,
        COMMA,
        COLON,
        HASH_TAG,
        DOUBLE_STAR,
        SINGLE_QUOTED_STRING,
        DOUBLE_QUOTED_STRING,
        MULTILINE_STRING,
        WS,
        STATEMENT_TERMINATOR,
        EOL,
        EOF
    }

    class Token
    {

        internal Token(TokenType type, int lineNumber, int columnNumber, string rawValue = null, string value = null)
        {
            Type = type;
            RawValue = rawValue;
            Value = value ?? rawValue;
            LineNubmer = lineNumber;
            ColumnNumber = columnNumber;
        }

        internal TokenType Type { get; }
        internal string RawValue { get; }
        internal string Value { get; }
        internal int LineNubmer { get; }
        internal int ColumnNumber { get; }

        public override string ToString()
        {
            return $"{Type}: {RawValue} ({Value}): {LineNubmer}, {ColumnNumber}";
        }
    }

    class ScriptParser
    {

        readonly ScriptLexer lexer;

        readonly IList<Token> tokenBuf = new List<Token>();
        int pos;
        bool filterWhitespace;

        internal ScriptParser(ScriptLexer lexer)
        {
            this.lexer = lexer;
        }

        internal IList<ScriptStatement> ScriptFile()
        {
            var result = new List<ScriptStatement>();
            ScriptStatement statement;
            while ((statement = Statement()) is not null)
                result.Add(statement);
            return result;
        }

        ScriptStatement Statement()
        {
            var text = new StringBuilder();
            var directives = new Dictionary<string, ScriptDirective>();
            var statementBegan = false;
            var lineNumber = -1;
            while (true)
            {
                var la = La(1);
                switch (la.Type)
                {
                    case TokenType.LINE_COMMENT_TOK:
                        var lineDirective = TryLineDirective();
                        if (lineDirective is not null)
                        {
                            var directiveId = Guid.NewGuid().ToString();
                            directives.Add(directiveId, lineDirective);
                            text.Append($"{{{directiveId}}}").Append(' ');
                            statementBegan = true;
                        }
                        else if (statementBegan)
                            text.Append(' ');
                        break;
                    case TokenType.BLOCK_START:
                        var blockDirective = TryBlockDirective();
                        if (blockDirective is not null)
                        {
                            var directiveId = Guid.NewGuid().ToString();
                            directives.Add(directiveId, blockDirective);
                            text.Append($"{{{directiveId}}}").Append(' ');
                            statementBegan = true;
                        }
                        else if (statementBegan)
                            text.Append(' ');
                        break;
                    
                    case TokenType.EOL:
                    case TokenType.REG_TOK:
                    case TokenType.COMMA:
                    case TokenType.COLON:
                    case TokenType.HASH_TAG:
                    case TokenType.DOUBLE_STAR:
                    case TokenType.SINGLE_QUOTED_STRING:
                    case TokenType.DOUBLE_QUOTED_STRING:
                    case TokenType.WS:
                        DoStatementText(text, statementBegan, ref lineNumber);
                        if (text.Length > 0 && !statementBegan)
                        {
                            statementBegan = true;
                        }
                        
                        break;
                    case TokenType.EOF:
                        NextToken();
                        return text.Length == 0 ? null : new ScriptStatement(text.ToString(), lexer.FileName, lineNumber, directives);
                    case TokenType.STATEMENT_TERMINATOR:
                        NextToken();
                        return new ScriptStatement(text.ToString(), lexer.FileName, lineNumber, directives);
                    default:
                        throw UnexpectedToken(la, TokenType.LINE_COMMENT_TOK, TokenType.BLOCK_START, TokenType.EOL, TokenType.REG_TOK, TokenType.COMMA, TokenType.COLON,
                            TokenType.HASH_TAG, TokenType.DOUBLE_STAR, TokenType.SINGLE_QUOTED_STRING, TokenType.DOUBLE_QUOTED_STRING, TokenType.EOL, TokenType.WS);
                }
            }
        }

        void DoStatementText(StringBuilder text, bool statementBegan, ref int lineNumber)
        {
            Token la;
            do
            {
                var first = true;
                la = La(1);
                while (la.Type == TokenType.WS || la.Type == TokenType.EOL)
                {
                    if (first)
                        first = false;
                    NextToken();
                    la = La(1);
                }

                if (!first && statementBegan)
                    text.Append(' ');

                if (la.Type == TokenType.REG_TOK || la.Type == TokenType.COMMA || la.Type == TokenType.HASH_TAG || la.Type == TokenType.DOUBLE_STAR
                    || la.Type == TokenType.SINGLE_QUOTED_STRING || la.Type == TokenType.DOUBLE_QUOTED_STRING)
                {
                    var tok = NextToken();
                    text.Append(tok.RawValue);
                    if (lineNumber == -1)
                    {
                        lineNumber = tok.LineNubmer;
                    }

                    la = La(1);
                }
            } while (la.Type == TokenType.REG_TOK || la.Type == TokenType.COMMA || la.Type == TokenType.HASH_TAG || la.Type == TokenType.DOUBLE_STAR
                    || la.Type == TokenType.SINGLE_QUOTED_STRING || la.Type == TokenType.DOUBLE_QUOTED_STRING);
        }

        ScriptDirective TryBlockDirective()
        {
            filterWhitespace = true;

            var tok = NextToken();
            if (tok.Type != TokenType.BLOCK_START)
                throw UnexpectedToken(tok, TokenType.BLOCK_START);

            while ((tok = NextToken()).Type == TokenType.EOL);
            if (tok.Type == TokenType.DOUBLE_STAR)
            {
                var lineNumber = tok.LineNubmer;

                while ((tok = NextToken()).Type == TokenType.EOL) ;
                if (tok.Type == TokenType.HASH_TAG)
                {
                    tok = NextToken();
                    if (tok.Type == TokenType.REG_TOK)
                    {
                        var directiveName = tok.Value;
                        var arguments = new List<string>();

                        tok = NextToken();
                        if (tok.Type == TokenType.COLON)
                        {
                            var state = 0;
                            var buf = new StringBuilder();

                            while (state != 2)
                            {
                                tok = NextToken();
                                if (tok.Type == TokenType.EOL)
                                    continue;

                                switch (state)
                                {
                                    case 0:
                                        if (tok.Type == TokenType.HASH_TAG)
                                        {
                                            buf.Append('#');
                                        }
                                        else if (tok.Type == TokenType.REG_TOK || tok.Type == TokenType.SINGLE_QUOTED_STRING || tok.Type == TokenType.DOUBLE_QUOTED_STRING || tok.Type == TokenType.MULTILINE_STRING)
                                        {
                                            state = 1;
                                            arguments.Add(buf.Append(tok.Value).ToString());
                                            buf.Clear();
                                        }
                                        else
                                            throw UnexpectedToken(tok, TokenType.REG_TOK, TokenType.SINGLE_QUOTED_STRING, TokenType.DOUBLE_QUOTED_STRING, TokenType.MULTILINE_STRING);
                                        break;
                                    case 1:
                                        if (tok.Type == TokenType.BLOCK_STOP)
                                            state = 2;
                                        else if (tok.Type == TokenType.COMMA)
                                            state = 0;
                                        else
                                            throw UnexpectedToken(tok, TokenType.COMMA);
                                        break;
                                }

                            }
                        }
                        else if (tok.Type != TokenType.BLOCK_STOP)
                            throw UnexpectedToken(tok, TokenType.BLOCK_STOP);

                        filterWhitespace = false;
                        return new ScriptDirective(directiveName, arguments, lexer.FileName, lineNumber);
                    }
                    
                }

            }

            do
            {
                tok = NextToken();
            } while (tok.Type != TokenType.BLOCK_STOP && tok.Type != TokenType.EOF);

            if (tok.Type == TokenType.EOF)
                throw UnexpectedToken(tok, TokenType.BLOCK_STOP);

            filterWhitespace = false;
            return null;
        }

        ScriptDirective TryLineDirective()
        {
            filterWhitespace = true;

            var tok = NextToken();
            if (tok.Type != TokenType.LINE_COMMENT_TOK)
                throw UnexpectedToken(tok, TokenType.LINE_COMMENT_TOK);

            tok = NextToken();
            if (tok.Type == TokenType.LINE_COMMENT_TOK)
            {
                var lineNumber = tok.LineNubmer;
                tok = NextToken();
                if (tok.Type == TokenType.HASH_TAG)
                {
                    tok = NextToken();
                    if (tok.Type == TokenType.REG_TOK)
                    {
                        var directiveName = tok.Value;
                        var arguments = new List<string>();
                        
                        tok = NextToken();
                        if (tok.Type == TokenType.COLON)
                        {
                            var state = 0;
                            while (state != 2)
                            {
                                tok = NextToken();
                                switch (state)
                                {
                                    case 0:
                                        if (tok.Type == TokenType.REG_TOK || tok.Type == TokenType.SINGLE_QUOTED_STRING || tok.Type == TokenType.DOUBLE_QUOTED_STRING)
                                        {
                                            state = 1;
                                            arguments.Add(tok.Value);
                                        }
                                        else
                                            throw UnexpectedToken(tok, TokenType.REG_TOK, TokenType.SINGLE_QUOTED_STRING, TokenType.DOUBLE_QUOTED_STRING);
                                        break;
                                    case 1:
                                        if (tok.Type == TokenType.EOL)
                                            state = 2;
                                        else if (tok.Type == TokenType.COMMA)
                                            state = 0;
                                        else
                                            throw UnexpectedToken(tok, TokenType.COMMA);
                                        break;
                                }

                            }
                        }
                        else if (tok.Type != TokenType.EOL)
                            throw UnexpectedToken(tok, TokenType.EOL);

                        filterWhitespace = false;
                        return new ScriptDirective(directiveName, arguments, lexer.FileName, lineNumber);
                    }
                }
            }

            do
            {
                tok = NextToken();
            } while (tok.Type != TokenType.EOL && tok.Type != TokenType.EOF);

            filterWhitespace = false;
            return null;
        }


        Token La(int k)
        {
            Token tok = null;
            var offset = pos;
            for (var i = 0; i < k; ++i)
            {
                if (offset < tokenBuf.Count)
                {
                    tok = tokenBuf[offset++];
                }
                else
                {
                    tok = GetNextToken();
                    tokenBuf.Add(tok);
                }
                if (tok.Type == TokenType.EOF)
                    return tok;
            }
            return tok;
        }

        Token NextToken()
        {
            if (pos < tokenBuf.Count)
            {
                return tokenBuf[pos++];
            }
            else
            {
                var res = GetNextToken();
                tokenBuf.Add(res);
                ++pos;
                return res;
            }
        }

        Token GetNextToken()
        {
            Token token;
            do
            {
                token = lexer.NextToken();
            } while (filterWhitespace && token.Type == TokenType.WS);

            return token;
        }

        Exception UnexpectedToken(Token token, params TokenType[] expected)
        {
            return new ScriptSyntaxError($"Unexpected Token: {token}; Expected: {string.Join(',', expected)}", lexer.FileName, token.LineNubmer, token.ColumnNumber);
        }
    }

    class ScriptLexer
    { 


        internal static readonly char[] SigChars = new char[] { ',', '#', '/', '*', '\'', '"', '-', ':' };

        // Reporting State
        int lineNumber = 1;
        int columnNumber;

        // Lexical State
        readonly string source;
        readonly StringBuilder buf = new();
        readonly StringBuilder valBuf = new();
        int state;
        int pos;
        char cur;
        char laCh;
        bool eof = false;
        bool tripleQuote = false;

        int terminatorPos;

        // Property Values
        readonly string statementTerminator;

        internal ScriptLexer(string source, string fileName, string statementTerminator)
        {
            this.source = source;
            FileName = fileName;
            this.statementTerminator = statementTerminator;
        }

        internal string FileName { get; }


        internal Token NextToken()
        {
            while (true)
            {
                eof = pos >= source.Length;
                
                if (eof)
                {
                    cur = default;
                }
                else
                {
                    cur = source[pos];
                    buf.Append(cur);
                    AdvPos();
                }
                

                switch (state)
                {
                    case 0:
                        if (eof)
                            return Emit(TokenType.EOF);

                        if (cur == ',')
                        {
                            return Emit(TokenType.COMMA, ",");
                        }
                        else if (cur == '#')
                        {
                            return Emit(TokenType.HASH_TAG, "#");
                        }
                        else if (cur == ':')
                        {
                            return Emit(TokenType.COLON, ":");
                        }
                        else if (cur == '/')
                        {
                            if (La(1))
                                return Emit(TokenType.REG_TOK, buf.ToString());
                            if (laCh == '*')
                            {
                                AdvPos();
                                return Emit(TokenType.BLOCK_START, "/*");
                            }
                            else
                            {
                                state = 5;
                                goto case 5;
                            }
                        }
                        else if (cur == '*')
                        {
                            if (La(1))
                                return Emit(TokenType.REG_TOK, buf.ToString());
                            if (laCh == '*')
                            {
                                AdvPos();
                                return Emit(TokenType.DOUBLE_STAR, "**");
                            }
                            else if (laCh == '/')
                            {
                                AdvPos();
                                return Emit(TokenType.BLOCK_STOP, "*/");
                            }
                            else
                            {
                                state = 5;
                                goto case 5;
                            }
                        }
                        else if (cur == '-')
                        {
                            if (La(1))
                                return Emit(TokenType.REG_TOK, buf.ToString());
                            if (laCh == '-')
                            {
                                AdvPos();
                                return Emit(TokenType.LINE_COMMENT_TOK);
                            }
                            else
                            {
                                state = 5;
                                goto case 5;
                            }
                        }
                        else if (cur == '\'')
                        {
                            state = 1;
                        }
                        else if (cur == '"')
                        {
                            state = 2;
                        }
                        else if (cur == '\n')
                        {
                            ++lineNumber;
                            columnNumber = 0;
                            return Emit(TokenType.EOL, "\n");
                        }
                        else if (cur == '\r')
                        {
                            bool crlf = false;
                            if (La(1) || (crlf = laCh == '\n'))
                                AdvPos();
                            ++lineNumber;
                            columnNumber = 1;
                            return Emit(TokenType.EOL, crlf ? "\r\n" : "\r");
                        }
                        else if (char.IsWhiteSpace(cur))
                        {
                            state = 3;
                            goto case 3;
                        }
                        else if (cur == statementTerminator[0])
                        {
                            if (statementTerminator.Length == 1)
                                return Emit(TokenType.STATEMENT_TERMINATOR, statementTerminator);
                            else
                            {
                                ++terminatorPos;
                                state = 4;
                            }
                        }
                        else
                        {
                            state = 5;
                            goto case 5;
                        }
                        break;

                    // Single Quoted String
                    case 1:
                        var squote = DoQuote('\'', TokenType.SINGLE_QUOTED_STRING);
                        if (squote is not null)
                            return squote;
                        break;

                    // Double Quoted String
                    case 2:
                        var dqote = DoQuote('"', TokenType.DOUBLE_QUOTED_STRING);
                        if (dqote is not null)
                            return dqote;
                        break;

                    // Whitespace
                    case 3:
                        if (eof || La(1) || !char.IsWhiteSpace(laCh))
                        {
                            state = 0;
                            return Emit(TokenType.WS, buf.ToString());
                        }
                        break;

                    // Statement Terminator
                    case 4:
                        if (eof)
                            return Emit(TokenType.REG_TOK, buf.ToString());
                        if (cur == statementTerminator[terminatorPos])
                        {
                            ++terminatorPos;
                            if (terminatorPos == statementTerminator.Length)
                            {
                                state = 0;
                                return Emit(TokenType.STATEMENT_TERMINATOR, statementTerminator);
                            }
                        }
                        else
                        {
                            state = 5;
                            goto case 5;
                        }
                        break;

                    // Regular
                    case 5:
                        if (eof || La(1) || !IsReg(laCh))
                        {
                            state = 0;
                            return Emit(TokenType.REG_TOK, buf.ToString());
                        }
                        break;

                }
            }
        }

        void AdvPos()
        {
            ++pos;
            ++columnNumber;
        }

        bool La(int k)
        {
            var target = k - 1 + pos;
            if (target < source.Length)
            {
                laCh = source[target];
                return false;
            }
            else
            {
                laCh = default;
                return true;
            }
        }

        Token Emit(TokenType type, string rawValue = null, string value = null)
        {
            var res = new Token(type, lineNumber, columnNumber, rawValue, value);

            buf.Clear();
            valBuf.Clear();
            terminatorPos = 0;

            return res;
        }

        Exception Error(string message)
        {
            return new ScriptSyntaxError(message, FileName, lineNumber, columnNumber);
        }

        Token DoQuote(char q, TokenType tokenType)
        {
            if (eof)
                throw Error("Unclosed qutoes. (Unexpected EOF)");
            if (cur == q)
            {
                if (La(1))
                {
                    state = 0;
                    return Emit(tokenType, buf.ToString(), valBuf.ToString());
                }

                if (tripleQuote)
                {
                    if (laCh == q)
                    {
                        if (!La(1) && laCh == q)
                        {
                            AdvPos();
                            AdvPos();
                            state = 0;
                            tripleQuote = false;
                            return Emit(TokenType.MULTILINE_STRING, buf.ToString(), valBuf.ToString());
                        }
                        else
                        {
                            valBuf.Append(q).Append(q);
                            AdvPos();
                        }
                    }
                    else
                    {
                        valBuf.Append(q);
                    }
                }
                else
                {
                    if (laCh == q)
                    {
                        if (valBuf.Length == 0)
                        {
                            tripleQuote = true;
                        }
                        else
                        {
                            valBuf.Append(q);
                        }
                        AdvPos();
                    }
                    else
                    {
                        state = 0;
                        return Emit(tokenType, buf.ToString(), valBuf.ToString());
                    }
                    
                }
            }
            else if (cur == '\\')
            {
                if (La(1))
                    throw Error("Unclosed qutoes. (Unexpected EOF)");
                AdvPos();
                valBuf.Append(laCh);
            }
            else
            {
                if (cur == '\n')
                {
                    if (!tripleQuote)
                        throw Error("Unexpected EOL.");
                    ++lineNumber;
                    columnNumber = 1;
                }
                if (cur == '\r')
                {
                    if (!tripleQuote)
                        throw Error("Unexpected EOL.");
                    if (!La(1) && laCh == '\n')
                    {
                        valBuf.Append('\n');
                        AdvPos();
                    }
                    ++lineNumber;
                    columnNumber = 1;
                }


                valBuf.Append(cur);
            }
            return null;
        }

        bool IsReg(char ch)
        {
            return Array.IndexOf(SigChars, ch) == -1 && ch != statementTerminator[0] && !char.IsWhiteSpace(ch);
        }
    }

}

