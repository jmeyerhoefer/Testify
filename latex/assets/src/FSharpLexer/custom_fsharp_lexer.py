from pygments.lexer import RegexLexer
from pygments.token import Text, Number, Keyword
from pygments.lexers.dotnet import FSharpLexer


class CustomFSharpLexer(FSharpLexer):
    tokens = {
        'root': [
            (r'\b\d+N\b', Number)
        ] + FSharpLexer.tokens['root']
    }