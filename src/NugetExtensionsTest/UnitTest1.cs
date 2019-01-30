using Microsoft.VisualStudio.TestTools.UnitTesting;
using NugetExtensions;
using System;
using System.Linq;

namespace NugetExtensionsTest
{
	[TestClass]
	public class UnitTest1
	{
		[TestMethod]
		public void Get_By_CSPROJ_File()
		{
			var packages = Extensions.GetPackages(@"C:\dev\jungheinrich\src\JH.SharePoint.Functions.Api3");
			Assert.AreNotEqual(0, packages.Count());
			foreach (var package in packages)
			{
				PrintPackageRecursive(package);
				foreach (var dependency in package.Dependencies)
				{
					PrintPackageRecursive(dependency, i: 1);
				}
			}
		}
		[TestMethod]
		public void Get_Dependencies()
		{
			var dependencies = Extensions.GetPackageDependencies(@"Microsoft.AspNetCore.App", "2.2.0");
			Assert.AreEqual(150, dependencies.Count());
		}
		private void PrintPackageRecursive(Package package, int i = 0)
		{
			Console.WriteLine(new string(' ', i * 4) + package);
			//foreach (var sub in package.Dependencies)
			//	PrintPackageRecursive(sub, i+1);
		}
	}
}
