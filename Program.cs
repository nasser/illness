using System;
using System.Linq;
using System.IO;
using System.Diagnostics.CodeAnalysis;

using System.Text;
using System.Net;

using System.Reflection;
using System.Collections.Generic;

using Mono.Options;
using Mono.Cecil;

namespace Illness
{
	public class Routes : Dictionary<string, Func<HttpListenerContext, string>> { }

	class MainClass
	{
        public static Illness illness = new Illness();
		public static DateTime lastWriteTime = new DateTime(0);

        static string cachedMSIL;
        static string cachedVerification;
        static string cachedCSharp;

		public static string HTMLEncode(string s)
		{
			return s.Replace("<", "&lt;").Replace(">", "&gt;");
		}

		public static void UpdateCaches(string assembly)
		{
			var fileInfo = new FileInfo(assembly);
			if (fileInfo.LastWriteTime > lastWriteTime)
			{
				try
				{

					Log("Disassembling " + assembly);
					lastWriteTime = fileInfo.LastWriteTime;
                    cachedVerification = HTMLEncode(illness.ToVerification(assembly));
					cachedMSIL = HTMLEncode(illness.ToMSIL(assembly));
					cachedCSharp = HTMLEncode(illness.ToCSharp(assembly));
				}
                catch (Exception e)
				{
					Console.Write(e.Message);
					Console.WriteLine(e.InnerException.Message);
					//Environment.Exit(1);
				}
			}
		}


		[SuppressMessage("Potential Code Quality Issues", "RECS0135", Justification = "Long running server task")]
		public static void Serve(int port, Dictionary<string, Func<HttpListenerContext, string>> routes)
		{
			// https://gist.github.com/joeandaverde/3994603
			var listener = new HttpListener();

			listener.Prefixes.Add(string.Format("http://localhost:{0}/", port));
			listener.Prefixes.Add(string.Format("http://127.0.0.1:{0}/", port));

			Console.WriteLine("Listening on " + listener.Prefixes.First());

			listener.Start();

			while (true)
			{
				var context = listener.GetContext(); //Block until a connection comes in
				var request = context.Request;
				var response = context.Response;
				var outputStream = response.OutputStream;

				response.AddHeader("Server", "Illness " + Assembly.GetEntryAssembly().GetName().Version);

				if (!routes.ContainsKey(request.RawUrl))
				{
					response.StatusCode = 404;
					var bytes = Encoding.UTF8.GetBytes(string.Format("No route for {0}", request.RawUrl));
					outputStream.Write(bytes, 0, bytes.Length);
					outputStream.Close();

				}
				else {
					var route = routes[request.RawUrl];
					string result = route.Invoke(context);
					if (result == null)
						result = "";
					var bytes = Encoding.UTF8.GetBytes(result);
					outputStream.Write(bytes, 0, bytes.Length);
					outputStream.Close();
				}
			}
		}

		static OptionSet options;
		static bool verbose = false;
		static int port = 2718;

		public static void Usage()
		{
			Console.WriteLine("illness [OPTIONS] assembly.dll [directory ...]");
			options.WriteOptionDescriptions(Console.Out);
			Environment.Exit(0);
		}

		public static void Log(string message)
		{
			if (verbose)
				Console.WriteLine(message);
		}

		public static void Main(string[] args)
		{
			Console.WriteLine(string.Format("Illness {0}", Assembly.GetExecutingAssembly().GetName().Version));
			Console.WriteLine("Ramsey Nasser, Jan 2016\n");

			options = new OptionSet
			{
				{ "v|verbose", "print information while running", v => verbose = true },
				{ "p|port=", "port to listen on", p => port = int.Parse(p) },
				{ "h|help", "show help message", h => Usage() }
			};
			var pathArgs = options.Parse(args).ToArray();

			if (args.Length == 0)
			{
				Usage();
				return;
			}

			string file = pathArgs[0];
			var fileInfo = new FileInfo(file);
			string fileDirectory = fileInfo.DirectoryName;
			string monoPath = ".";
			Log("Watching " + file);

            illness.AddAssemblySearchPath(fileDirectory);
			Log("Resolving from " + fileDirectory);
			monoPath += Path.PathSeparator + fileDirectory;

			for (int i = 1; i < pathArgs.Length; i++)
			{
				string dir = pathArgs[i];
				illness.AddAssemblySearchPath(dir);
				Log("Resolving from " + dir);
				monoPath += Path.PathSeparator + dir;
			}

            illness.AddEnvironmentVariable("MONO_PATH", monoPath);
			Log("MONO_PATH=" + monoPath);

			var fsw = new FileSystemWatcher(fileDirectory, fileInfo.Name);
			fsw.EnableRaisingEvents = true;
			fsw.IncludeSubdirectories = false;
			fsw.Created += (sender, evt) => { UpdateCaches(file); };

			UpdateCaches(file);

			string binDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

			Serve(port, new Routes
			{
				["/"] = ctx => File.ReadAllText(Path.Combine(binDirectory, "public", "index.html")),
				["/msil"] = ctx => cachedMSIL,
				["/cs"] = ctx => cachedCSharp,
				["/peverify"] = ctx => cachedVerification,
				["/file"] = ctx => file,
				["/last-write"] = ctx => lastWriteTime.ToString()
			});

		}
	}
}