using System;
using System.Linq;
using System.IO;

using ICSharpCode;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;

using System.Reflection;
using Mono.Cecil;

using System.Diagnostics;
using System.Text;
using System.Net;
using System.Threading;

using System.Collections.Generic;


namespace Illness
{
	public class Routes : Dictionary<string, Func<HttpListenerContext, string>> { }
		
	class MainClass
	{
		public static string ShellCommand (string cmd, string args)
		{
			// http://stackoverflow.com/questions/15234448/run-shell-commands-using-c-sharp-and-get-the-info-into-string
			Process proc = new Process {
				StartInfo = new ProcessStartInfo {
					FileName = cmd,
					Arguments = args,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					CreateNoWindow = true
				}
			};
			proc.Start();
			StringBuilder sb = new StringBuilder ();
			while (!proc.StandardOutput.EndOfStream) {
				string line = proc.StandardOutput.ReadLine();
				sb.AppendLine (line);
			}

			return sb.ToString ();
		}

		public static string ToMSIL (string assembly)
		{
			return ShellCommand ("monodis", assembly);
		}

		public static string ToVerification (string assembly)
		{
			return ShellCommand ("peverify", assembly);
		}

		public static string ToCSharp (string assembly)
		{
			FileInfo assemblyFile = new FileInfo (assembly);
			var resolver = new DefaultAssemblyResolver ();
			resolver.AddSearchDirectory (assemblyFile.DirectoryName);
			AssemblyDefinition assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyFile.FullName, new ReaderParameters { AssemblyResolver = resolver });

			AstBuilder astBuilder = new AstBuilder (new DecompilerContext (assemblyDefinition.MainModule));
			astBuilder.AddAssembly (assemblyDefinition);
			StringWriter output = new StringWriter ();
			astBuilder.GenerateCode (new PlainTextOutput (output));

			return output.ToString ();
		}
			
		public static void Serve (Dictionary<string, Func<HttpListenerContext, string>> routes)
		{
			// https://gist.github.com/joeandaverde/3994603
			var listener = new HttpListener();

			listener.Prefixes.Add("http://localhost:2718/");
			listener.Prefixes.Add("http://127.0.0.1:2718/");

			Console.WriteLine("Listening on " + listener.Prefixes.First());

			listener.Start();

			while (true)
			{
				var context = listener.GetContext(); //Block until a connection comes in
				var request = context.Request;
				var response = context.Response;
				var outputStream = response.OutputStream;

				response.AddHeader("Server", "Illness v0.2");

				if(!routes.ContainsKey(request.RawUrl)) {
					response.StatusCode = 404;
					var bytes = Encoding.UTF8.GetBytes(string.Format("No route for {0}", request.RawUrl));
					outputStream.Write(bytes, 0, bytes.Length);
					outputStream.Close();

				} else {
					var route = routes[request.RawUrl];
					var bytes = Encoding.UTF8.GetBytes(route.Invoke(context));
					outputStream.Write(bytes, 0, bytes.Length);
					outputStream.Close();
				}
			}
		}

		public static void Main (string[] args)
		{
			string file = args [0];
			Console.WriteLine("Illness - MSIL Visualizer");
			Console.WriteLine("Ramsey Nasser, Jan 2016");
			Console.WriteLine("Watching " + file);

			string binDirectory = Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location);

			Serve (new Routes {
				["/"] 			= ctx => File.ReadAllText(Path.Combine (binDirectory, "public", "index.html")),
				["/msil"] 		= ctx => ToMSIL(file),
				["/cs"] 		= ctx => ToCSharp(file),
				["/peverify"] 	= ctx => ToVerification(file),
				["/file"] 		= ctx => file
			});

		}
	}
}