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
		static DefaultAssemblyResolver AssemblyResolver = new DefaultAssemblyResolver ();

		public static string ShellCommand (string cmd, string args, IDictionary<string, string> environment = null)
		{
			// http://stackoverflow.com/questions/15234448/run-shell-commands-using-c-sharp-and-get-the-info-into-string
			var startInfo = new ProcessStartInfo {
				FileName = cmd,
				Arguments = args,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				CreateNoWindow = true,
			};

			if (environment != null) {
				foreach (var kv in environment) {
					startInfo.EnvironmentVariables.Add (kv.Key, kv.Value);
				}
			}

			Process proc = new Process ();
			proc.StartInfo = startInfo;
			proc.Start();

			StringBuilder sb = new StringBuilder ();
			while (!proc.StandardOutput.EndOfStream) {
				string line = proc.StandardOutput.ReadLine();
				sb.AppendLine (line);
			}

			return sb.ToString ();
		}

		public static Dictionary<string, string> environment = new Dictionary<string, string>();

		static string cachedMSIL;

		public static string ToMSIL (string assembly)
		{
			return ShellCommand ("monodis", assembly, environment);
		}

		static string cachedVerification;

		public static string ToVerification (string assembly)
		{
			return ShellCommand ("peverify", assembly, environment);
		}

		static string cachedCSharp;

		public static string ToCSharp (string assembly)
		{
			return ToCSharp (assembly, AssemblyResolver);
		}

		public static string ToCSharp (string assembly, DefaultAssemblyResolver resolver)
		{
			FileInfo assemblyFile = new FileInfo (assembly);
			AssemblyDefinition assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyFile.FullName, new ReaderParameters { AssemblyResolver = resolver });

			AstBuilder astBuilder = new AstBuilder (new DecompilerContext (assemblyDefinition.MainModule));
			astBuilder.AddAssembly (assemblyDefinition);
			StringWriter output = new StringWriter ();
			astBuilder.GenerateCode (new PlainTextOutput (output));

			return output.ToString ();
		}

		public static DateTime lastWriteTime = new DateTime (0);

		public static void UpdateCaches(string assembly) {
			var fileInfo = new FileInfo (assembly);
			if (fileInfo.LastWriteTime > lastWriteTime) {
				Console.WriteLine ("Disassembling " + assembly);
				lastWriteTime = fileInfo.LastWriteTime;
				cachedMSIL = ToMSIL (assembly);
				cachedCSharp = ToCSharp (assembly);
				cachedVerification = ToVerification (assembly);
			}
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

		public static void Usage() {
			Console.WriteLine ("USAGE illness assembly.dll [directory ...]");
		}

		public static void Main (string[] args)
		{
			Console.WriteLine("Illness - MSIL Visualizer");
			Console.WriteLine("Ramsey Nasser, Jan 2016");
			if (args.Length == 0) {
				Usage ();
				return;
			}
				
			string file = args [0];
			FileInfo fileInfo = new FileInfo (file);
			string fileDirectory = fileInfo.DirectoryName;
			string monoPath = ".";
			Console.WriteLine("Watching " + file);

			AssemblyResolver.AddSearchDirectory (fileDirectory);
			Console.WriteLine("Resolving from " + fileDirectory);
			monoPath += fileDirectory;

			for (int i = 1; i < args.Length; i++) {
				string dir = new FileInfo(args [i]).DirectoryName;
				AssemblyResolver.AddSearchDirectory (dir);
				Console.WriteLine("Resolving from " + dir);
				monoPath += Path.PathSeparator + dir;
			}

			environment.Add ("MONO_PATH", monoPath);

			var fsw = new FileSystemWatcher (fileDirectory, file);
			fsw.EnableRaisingEvents = true;
			fsw.IncludeSubdirectories = false;
			fsw.Created += (sender, evt) => { UpdateCaches (file); };

			UpdateCaches (file);

			string binDirectory = Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location);

			Serve (new Routes {
				["/"] 			= ctx => File.ReadAllText(Path.Combine (binDirectory, "public", "index.html")),
				["/msil"] 		= ctx => cachedMSIL,
				["/cs"] 		= ctx => cachedCSharp,
				["/peverify"] 	= ctx => cachedVerification,
				["/file"] 		= ctx => file,
				["/last-write"] = ctx => lastWriteTime.ToString()
			});

		}
	}
}