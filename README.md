# illness

MSIL disassembler and verifier

## libillness

This is an experimental branch with the goal of packaging up illness's disassembling and verification functionality into a library that is useable from CLR languages. Specifically, the goal of `libillness` it to operate on *in-memory* types and methods, as opposed to relying on the compiled DLL files and command line tools.

Currently the three tools print their results to `stdout`, but the goal of this library is to return useful values to CLR languages to support better compiler tooling.

### MSIL Disassembler

Illness exposed the innerards of the `monodis` commandline tool via the `libmonodis.a` static library to the runtime, allowing it to operate on CLR methods accessed by reflection.  `libmonodis.a` is built from the Mono source tree in the process of compiling `monodis` itself, though some of the code requires minor changes to conform to Mono's external embedding API. These changes will be published shortly. This is the most involved part of illness due to the complexity of bytecode metadata token resolution.

There is currently an issue where user string tokens encoded along side `ldstr` op codes cannot be found for dynamic methods and trigger a crash. Illness's implementation stringifies these as their hex values for now.

### Verifier

`peverify` uses the C function `mono_method_verify` internally, which happens to be part of Mono's embedding API. This expected to be robust and stable.

### Native Disassembler

A pointer to the JIT compiled native method can be derived from the embedding API. This is passed to [Zydis](https://github.com/zyantific/zydis) for a nicely formatted disassembly. This is expected to be robust and stable.

## Usage

```
$ make
$ mono Program.exe
```

Right now things are a mess while I figure things out. Eventually this will be a NuGet package that will be useable from CLR languages and REPLs.

## Legal

Illness Copyright (c) 2016-2018 Ramsey Nasser. Provided under the [MIT License](https://opensource.org/licenses/MIT)

Zydis Copyright (c) 2018 Florian Bernd and Joel Höner, used under the [MIT License](https://opensource.org/licenses/MIT)