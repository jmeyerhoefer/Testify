from setuptools import setup

setup(
    name='custom_fsharp_lexer',
    version='1.0',
    py_modules=['custom_fsharp_lexer'],
    install_requires=['Pygments'],
    entry_points={
        'pygments.lexers': [
            'custom_fsharp = custom_fsharp_lexer:CustomFSharpLexer',
        ],
    }
)
