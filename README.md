# illness

Live C#/MSIL disassembler

## Usage
Expects `monodis` and `peverify` commands to be available on your PATH.

```
$ mono path/to/Illness.exe assembly.dll path/to/other/assembly/directories...
```

The two basic inputs are: the assembly to disassemble and the directories in which to search for references assembly. The current directory and the directory of the target assembly are always included in the search path.

Naviate browser to [localhost:2718](http://localhost:2718/) to see C#/MSIL. Contents update when the assembly changes.

## Options
```
  -v, --verbose              print information while running
  -p, --port=VALUE           port to listen on
  -h, --help                 show help message
```

## Building
Depends on [Mono MDK](http://www.mono-project.com/download/) and [NuGet](https://www.nuget.org/).

```
$ git clone https://github.com/nasser/illness.git
$ cd illness
$ nuget restore
$ xbuild Illness.csproj
```

## Next Steps
* Installation script/instructions

## Legal
Illness Copyright (c) 2016 Ramsey Nasser. Provided under the [MIT License](https://opensource.org/licenses/MIT)

Mono.Cecil Copyright (c) 2008 - 2011, Jb Evain, used under the [MIT License](https://opensource.org/licenses/MIT)

ICSharpCode.Decompiler Copyright 2011-2014 AlphaSierraPapa for the SharpDevelop team, used under the [MIT License](https://opensource.org/licenses/MIT)
