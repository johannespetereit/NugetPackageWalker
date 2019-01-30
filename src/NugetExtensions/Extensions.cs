using IEnumeratorCache;
using NuGet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;

namespace NugetExtensions
{
	public class Package
	{
		private readonly IPackage _nuget;

		public string Name { get; set; }
		public string Version { get; set; }

		private readonly IPackageRepository _repo;
		private IEnumerable<Package> _dependencies;
		public IEnumerable<Package> Dependencies
		{
			get
			{
				if (_dependencies == null)
					BuildDependencies();
				return _dependencies;
			}
		}

		private void BuildDependencies()
		{
			_dependencies = _nuget.DependencySets
							   .SelectMany(s => s.Dependencies)
							   .Select(d =>
							   {
								   Debug.WriteLine($"Building package {d.Id}...");

								   var match = _repo.FindPackage(d.Id, d.VersionSpec, false, false);
								   if (match == null)
									   Console.WriteLine($"did not find package for package {d.Id} and version {d.VersionSpec}");
								   return match;
							   })
							   .Select(p => new Package(p, _repo))
							   .CacheGeneratedResults();
		}
		internal Package(IPackage nuget, IPackageRepository repo)
		{
			_nuget = nuget;
			Name = nuget.Id;
			Version = nuget.Version.ToString();
			_repo = repo;
		}

		public override string ToString()
		{
			return $"{Name} - {Version}";
		}
	}


	internal class PackageWalker
	{
		public string CsProjectFile { get; }
		public PackageWalker(string csProjectFile)
		{
			CsProjectFile = csProjectFile;
		}
		public IEnumerable<Package> GetPackages()
		{
			string url = "https://www.nuget.org/api/v2/";
			IPackageRepository repo = PackageRepositoryFactory.Default.CreateRepository(url);
			var xml = new XmlDocument();
			xml.Load(CsProjectFile);
			var elements = xml.GetElementsByTagName("PackageReference").Cast<XmlElement>();
			return GetPackges(repo, elements);
		}

		public IEnumerable<Package> GetDependencies(string name, string version = null)
		{
			string url = "https://www.nuget.org/api/v2/";
			IPackageRepository repo = PackageRepositoryFactory.Default.CreateRepository(url);
			var package = GetPackage(repo, name, version);
			foreach (var dependency in package.Dependencies)
				yield return GetPackage(repo, dependency.Name, dependency.Version);
		}




		private IEnumerable<Package> GetPackges(IPackageRepository repository, IEnumerable<XmlElement> packages)
		{
			foreach (var xmlElement in packages)
			{
				var package = GetPackage(repository, xmlElement.Attributes["Include"].Value, xmlElement.Attributes["Version"]?.Value);
				yield return package;
			}
		}
		private Package GetPackage(IPackageRepository repository, string name, string version = null)
		{
			var package = new
			{
				Name = name,
				Version = version
			};
			IEnumerable<IPackage> matches =
					repository.GetPackages()
					.Where(p => p.Id == package.Name);

			if (package.Version != null)
			{
				var ver = new SemanticVersion(package.Version);
				matches = matches.Where(p => p.Version.Equals(ver)).ToArray();
			}
			else
			{
				matches = matches.Where(p => p.IsLatestVersion);
			}
			var nugetPackage = matches.First();
			return new Package(nugetPackage, repository);
		}
	}

	public static class Extensions
	{
		public static IEnumerable<Package> GetPackages(string path)
		{
			string csproj;
			if (File.Exists(path))
				csproj = path;
			else
				csproj = Directory.GetFiles(path, "*.csproj").First();
			var walker = new PackageWalker(csproj);
			return walker.GetPackages();
		}
		public static IEnumerable<Package> GetPackageDependencies(string packageName, string version = null)
		{
			var walker = new PackageWalker(null);
			return walker.GetDependencies(packageName, version);
		}

	}
}
