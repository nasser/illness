# illness

Live C#/MSIL disassembler

## Usage
Expects `monodis` and `peverify` to be available.

```
$ mono path/to/Illness.exe assembly.dll path/to/other/assemblies...
```

Naviate browser to [localhost:2718](http://localhost:2718/) to see C#/MSIL. Contents update when the assembly changes. If the assembly depends on other assemblies, their directories can be passed in as well. 

## Building
Depends on [Mono MDK](http://www.mono-project.com/download/).

```
$ git clone https://github.com/nasser/illness.git
$ cd illness
$ xbuild Illness.csproj
```

## Next Steps
* Installation script/instructions

## Legal
Illness Copyright (c) 2016 Ramsey Nasser. Provided under the [MIT License](https://opensource.org/licenses/MIT)

Mono.Cecil Copyright (c) 2008 - 2011, Jb Evain, used under the [MIT License](https://opensource.org/licenses/MIT)

ICSharpCode.Decompiler Copyright 2011-2014 AlphaSierraPapa for the SharpDevelop team, used under the [MIT License](https://opensource.org/licenses/MIT)
