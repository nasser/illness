# illness

Live C#/MSIL disassembler

## Usage
Expects `monodis` and `peverify` to be available.

```
$ mono path/to/Illness.exe assemly.dll
```

Naviate browser to [localhost:2718](http://localhost:2718/) to see C#/MSIL. Contents update when the assemly changes.

## Building
Depends on [Mono MDK](http://www.mono-project.com/download/).

```
$ git clone https://github.com/nasser/illness.git
$ cs illness
$ xbuild Illness.csproj
```

## Next Steps
* Cache disassembly results
* Installation script/instructions

## Legal
Illness Copyright (c) 2016 Ramsey Nasser. Provided under the [MIT License](https://opensource.org/licenses/MIT)

Mono.Cecil Copyright (c) 2008 - 2011, Jb Evain, used under the [MIT License](https://opensource.org/licenses/MIT)

ICSharpCode.Decompiler Copyright 2011-2014 AlphaSierraPapa for the SharpDevelop team, used under the [MIT License](https://opensource.org/licenses/MIT)