using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.Decompiler.Disassembler;
using Mono.Cecil;

namespace Illness
{
    public class Illness
    {
        public static string ShellCommand(string cmd, string args, IDictionary<string, string> environment = null)
        {
            // http://stackoverflow.com/questions/15234448/run-shell-commands-using-c-sharp-and-get-the-info-into-string
            var startInfo = new ProcessStartInfo
            {
                FileName = cmd,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            if (environment != null)
            {
                foreach (var kv in environment)
                {
                    startInfo.EnvironmentVariables.Add(kv.Key, kv.Value);
                }
            }

            var proc = new Process();
            proc.StartInfo = startInfo;
            proc.Start();

            var sb = new StringBuilder();
            while (!proc.StandardOutput.EndOfStream)
            {
                string line = proc.StandardOutput.ReadLine();
                sb.AppendLine(line);
            }

            return sb.ToString();
        }

        Dictionary<string, string> environment = new Dictionary<string, string>();
        DefaultAssemblyResolver AssemblyResolver = new DefaultAssemblyResolver();

        public string ToMSIL(string assembly)
        {
            return ToMSIL(assembly, AssemblyResolver);
        }

        public static string ToMSIL(string assembly, DefaultAssemblyResolver resolver)
        {
            try
            {
                var assemblyFile = new FileInfo(assembly);
                AssemblyDefinition assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyFile.FullName, new ReaderParameters { AssemblyResolver = resolver });

                var output = new StringWriter();
                var disassembler = new ReflectionDisassembler(new PlainTextOutput(output), false, new CancellationToken());
                disassembler.WriteModuleContents(assemblyDefinition.MainModule);

                return output.ToString();
            }
            catch (DecompilerException ex)
            {
                throw new Exception("Could not decompile " + assembly + " to MSIL", ex);
            }
        }

        public string ToVerification(string assembly)
        {
            return ToVerification(assembly, environment);
        }

        public static string ToVerification(string assembly, IDictionary<string, string> environment)
        {
            return ShellCommand("peverify", assembly, environment);
        }

        public string ToCSharp(string assembly)
        {
            return ToCSharp(assembly, AssemblyResolver);
        }

        public static string ToCSharp(string assembly, DefaultAssemblyResolver resolver)
        {
            var assemblyFile = new FileInfo(assembly);
            AssemblyDefinition assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyFile.FullName, new ReaderParameters { AssemblyResolver = resolver });

            var astBuilder = new AstBuilder(new DecompilerContext(assemblyDefinition.MainModule));
            astBuilder.AddAssembly(assemblyDefinition);
            var output = new StringWriter();
            astBuilder.GenerateCode(new PlainTextOutput(output));

            return output.ToString();
        }

        public void AddAssemblySearchPath(string path)
        {
            AssemblyResolver.AddSearchDirectory(path);
        }

        public void AddRemoveSearchPath(string path)
        {
            AssemblyResolver.RemoveSearchDirectory(path);
        }

        public void AddEnvironmentVariable(string name, string value)
        {
            environment.Add(name, value);
        }

        public void RemoveEnvironmentVariable(string name)
        {
            environment.Remove(name);
        }

    }
}
