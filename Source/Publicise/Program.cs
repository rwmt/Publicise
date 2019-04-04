using System;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using dnlib.DotNet;

namespace Publicise
{
	class Program
	{
		const string hashFile = "publicise_hash.txt";

		static void Main(string[] args)
		{
			if(args.Length == 0)
			{
				Console.WriteLine($"Usage: {Path.GetFileName(args[0])} path-to-assembly [output path]");
				return;
			}

			
			string assemblyPath = args[0];
			string outputPath = args.Length == 2 ? args[1] : "";

			MakePublic(assemblyPath, outputPath);
		}

		public static void MakePublic(string assemblyPath, string outputPath)
		{
			if (!File.Exists(assemblyPath))
			{
				Console.WriteLine($"Invalid path {assemblyPath}");
				return;
			}

			string lastHash = null;
			string curHash = ComputeHash(assemblyPath);

			string hashPath = Path.Combine(outputPath, hashFile);

			if (File.Exists(hashPath))
				lastHash = File.ReadAllText(hashPath);

			//Console.WriteLine($"{ComputeHash(assemblyPath)} {lastHash}");

			if (curHash == lastHash)
			{
				Console.WriteLine("Public assembly is up to date.");
				return;
			}

			Console.WriteLine($"Making a public assembly from {assemblyPath}");

			RewriteAssembly(assemblyPath).Write($"{Path.Combine(outputPath, Path.GetFileNameWithoutExtension(assemblyPath))}_public.dll");

			File.WriteAllText(hashPath, curHash);
		}

		private static string ComputeHash(string assemblyPath)
		{
			StringBuilder res = new StringBuilder();

			using(var hash = SHA1.Create())
			{
				using (FileStream file = File.Open(assemblyPath, FileMode.Open, FileAccess.Read))
				{
					hash.ComputeHash(file);
					file.Close();
				}

				foreach (byte b in hash.Hash)
					res.Append(b.ToString("X2"));
			}

			return res.ToString();
		}

		// Based on https://gist.github.com/Zetrith/d86b1d84e993c8117983c09f1a5dcdcd
		private static ModuleDef RewriteAssembly(string assemblyPath)
		{
			ModuleDef assembly = ModuleDefMD.Load(assemblyPath);

			foreach (TypeDef type in assembly.GetTypes())
			{
				type.Attributes &= ~TypeAttributes.VisibilityMask;

				if (type.IsNested)
				{
					type.Attributes |= TypeAttributes.NestedPublic;
				}
				else
				{
					type.Attributes |= TypeAttributes.Public;
				}

				foreach (MethodDef method in type.Methods)
				{
					method.Attributes &= ~MethodAttributes.MemberAccessMask;
					method.Attributes |= MethodAttributes.Public;
				}

				foreach (FieldDef field in type.Fields)
				{
					field.Attributes &= ~FieldAttributes.FieldAccessMask;
					field.Attributes |= FieldAttributes.Public;
				}
			}

			return assembly;
		}
	}
}
