from pygments.lexers.dotnet import FSharpLexer
from pygments.token import Name


class CustomFSharpLexer(FSharpLexer):
    name = 'CustomFSharpLexer'
    aliases = ['customfsharp']
    filenames = ['*.fs', '*.fsx', '*.fsi', '*.fsscript']

    def __init__(self, **options):
        super().__init__(**options)
        self.tokens['root'].insert(0, (r"'[A-Za-z_][A-Za-z0-9_]*", Name.Type))