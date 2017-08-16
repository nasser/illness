using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Collections.Generic;

using System.Linq;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.Decompiler.Disassembler;

using Mono.Cecil;
using Mono.Cecil.Cil;

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
        DefaultAssemblyResolver assemblyResolver = new DefaultAssemblyResolver();

        public static string AssemblyLocation(Assembly assembly)
        {
            if (assembly is AssemblyBuilder)
            {
                return assembly.GetModules()[0].FullyQualifiedName; // HACK!!!
            }
            else
            {
                return assembly.Location;
            }
        }

        #region ToDefinition

        public static AssemblyDefinition ToDefinition(Assembly assembly, DefaultAssemblyResolver resolver)
        {
            return AssemblyDefinition.ReadAssembly(AssemblyLocation(assembly), new ReaderParameters { AssemblyResolver = resolver });
        }

        public static AssemblyDefinition ToDefinition(string assembly, DefaultAssemblyResolver resolver)
        {
            var assemblyFile = new FileInfo(assembly);
            return AssemblyDefinition.ReadAssembly(assemblyFile.FullName, new ReaderParameters { AssemblyResolver = resolver });
        }

        public static TypeDefinition ToDefinition(TypeInfo typeInfo, DefaultAssemblyResolver assemblyResolver)
        {
            var assemblyDefinition = ToDefinition(typeInfo.Assembly, assemblyResolver);
            return assemblyDefinition.MainModule
                                     .Types
                                     .Where(t => t.FullName == typeInfo.FullName)
                                     .Single();
        }

        public static MethodDefinition ToDefinition(MethodInfo methodInfo, DefaultAssemblyResolver assemblyResolver)
        {
            var assemblyDefinition = ToDefinition(methodInfo.DeclaringType.GetTypeInfo().Assembly, assemblyResolver);
            return assemblyDefinition.MainModule
                                     .Types
                                     .Where(t => t.FullName == methodInfo.DeclaringType.FullName)
                                     .Single()
                                     .Methods
                                     .Where(m => m.Name == methodInfo.Name)
                                     .Single();
        }

        public static FieldDefinition ToDefinition(FieldInfo fieldInfo, DefaultAssemblyResolver assemblyResolver)
        {
            var assemblyDefinition = ToDefinition(fieldInfo.DeclaringType.GetTypeInfo().Assembly, assemblyResolver);
            return assemblyDefinition.MainModule
                                     .Types
                                     .Where(t => t.FullName == fieldInfo.DeclaringType.FullName)
                                     .Single()
                                     .Fields
                                     .Where(f => f.Name == fieldInfo.Name)
                                     .Single();
        }

        #endregion

        #region ToMSIL

        public static PropertyDefinition ToDefinition(PropertyInfo propertyInfo, DefaultAssemblyResolver assemblyResolver)
        {
            var assemblyDefinition = ToDefinition(propertyInfo.DeclaringType.GetTypeInfo().Assembly, assemblyResolver);
            return assemblyDefinition.MainModule
                                     .Types
                                     .Where(t => t.FullName == propertyInfo.DeclaringType.FullName)
                                     .Single()
                                     .Properties
                                     .Where(p => p.Name == propertyInfo.Name)
                                     .Single();
        }

        public string ToMSIL(string assembly)
        {
            return ToMSIL(ToDefinition(assembly, assemblyResolver));
        }

        public static string ToMSIL(AssemblyDefinition assemblyDefinition)
        {
            var output = new StringWriter();
            var disassembler = new ReflectionDisassembler(new PlainTextOutput(output), false, new CancellationToken());
            disassembler.WriteModuleContents(assemblyDefinition.MainModule);
            return output.ToString();
        }

        public string ToMSIL(Assembly assembly)
        {
            return ToMSIL(ToDefinition(assembly, assemblyResolver));
        }

        public static string ToMSIL(TypeDefinition typeDefinition)
        {
            var output = new StringWriter();
            var disassembler = new ReflectionDisassembler(new PlainTextOutput(output), false, new CancellationToken());
            disassembler.DisassembleType(typeDefinition);
            return output.ToString();
        }

        public string ToMSIL(TypeInfo typeInfo)
        {
            return ToMSIL(ToDefinition(typeInfo, assemblyResolver));
        }

        public string ToMSIL(Type type)
        {
            return ToMSIL(ToDefinition(type.GetTypeInfo(), assemblyResolver));
        }

        public static string ToMSIL(MethodDefinition methodDefinition)
        {
            var output = new StringWriter();
            var disassembler = new ReflectionDisassembler(new PlainTextOutput(output), false, new CancellationToken());
            disassembler.DisassembleMethod(methodDefinition);
            return output.ToString();
        }

        public string ToMSIL(MethodInfo methodInfo)
        {
            return ToMSIL(ToDefinition(methodInfo, assemblyResolver));
        }

        public static string ToMSIL(FieldDefinition fieldDefinition)
        {
            var output = new StringWriter();
            var disassembler = new ReflectionDisassembler(new PlainTextOutput(output), false, new CancellationToken());
            disassembler.DisassembleField(fieldDefinition);
            return output.ToString();
        }

        public string ToMSIL(FieldInfo fieldInfo)
        {
            return ToMSIL(ToDefinition(fieldInfo, assemblyResolver));
        }

        public static string ToMSIL(PropertyDefinition propertyDefinition)
        {
            var output = new StringWriter();
            var disassembler = new ReflectionDisassembler(new PlainTextOutput(output), false, new CancellationToken());
            disassembler.DisassembleProperty(propertyDefinition);
            return output.ToString();
        }

        public string ToMSIL(PropertyInfo propertyInfo)
        {
            return ToMSIL(ToDefinition(propertyInfo, assemblyResolver));
        }

        #endregion

        #region ToCSharp

        public static string ToCSharp(AssemblyDefinition assemblyDefinition)
        {
            var astBuilder = new AstBuilder(new DecompilerContext(assemblyDefinition.MainModule));
            astBuilder.AddAssembly(assemblyDefinition);
            var output = new StringWriter();
            astBuilder.GenerateCode(new PlainTextOutput(output));
            return output.ToString();
        }

        public string ToCSharp(Assembly assembly)
        {
            return ToCSharp(ToDefinition(assembly, assemblyResolver));
        }

        public string ToCSharp(string assembly)
        {
            return ToCSharp(ToDefinition(assembly, assemblyResolver));
        }

        public static string ToCSharp(TypeDefinition typeDefinition)
        {
            var astBuilder = new AstBuilder(new DecompilerContext(typeDefinition.Module));
            astBuilder.AddType(typeDefinition);
            var output = new StringWriter();
            astBuilder.GenerateCode(new PlainTextOutput(output));
            return output.ToString();
        }

        public string ToCSharp(TypeInfo typeInfo)
        {
            return ToCSharp(ToDefinition(typeInfo, assemblyResolver));
        }

        public string ToCSharp(Type type)
        {
            return ToCSharp(type.GetTypeInfo());
        }

        public static string ToCSharp(MethodDefinition methodDefinition)
        {
            var astBuilder = new AstBuilder(new DecompilerContext(methodDefinition.Module));
            astBuilder.AddMethod(methodDefinition);
            var output = new StringWriter();
            astBuilder.GenerateCode(new PlainTextOutput(output));
            return output.ToString();
        }

        public string ToCSharp(MethodInfo methodInfo)
        {
            return ToCSharp(ToDefinition(methodInfo, assemblyResolver));
        }

        public static string ToCSharp(FieldDefinition fieldDefinition)
        {
            var astBuilder = new AstBuilder(new DecompilerContext(fieldDefinition.Module));
            astBuilder.AddField(fieldDefinition);
            var output = new StringWriter();
            astBuilder.GenerateCode(new PlainTextOutput(output));
            return output.ToString();
        }

        public string ToCSharp(FieldInfo fieldInfo)
        {
            return ToCSharp(ToDefinition(fieldInfo, assemblyResolver));
        }

        #endregion

        public string ToVerification(string assembly)
        {
            return ToVerification(assembly, environment);
        }

        public static string ToVerification(string assembly, IDictionary<string, string> environment)
        {
            return ShellCommand("peverify", assembly, environment);
        }

        public void AddAssemblySearchPath(string path)
        {
            assemblyResolver.AddSearchDirectory(path);
        }

        public void AddAssemblySearchPath(Assembly assembly)
        {
            var assemblyPath = new FileInfo(AssemblyLocation(assembly));
            assemblyResolver.AddSearchDirectory(assemblyPath.DirectoryName);
        }

        public void AddRemoveSearchPath(string path)
        {
            assemblyResolver.RemoveSearchDirectory(path);
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
