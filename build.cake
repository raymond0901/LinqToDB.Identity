#addin "MagicChunks"

var target          = Argument("target", "Default");
var configuration   = Argument<string>("configuration", "Release");

///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////
var isLocalBuild        = !AppVeyor.IsRunningOnAppVeyor;
var packPath            = Directory("./src/LinqToDB.Identity");
var sourcePath          = Directory("./src");
var testsPath           = Directory("test");
var buildArtifacts      = Directory("./artifacts/packages");
var solutionName        = "./LinqToDB.Identity.sln";
var nugetProject        = "./src/LinqToDB.Identity/LinqToDB.Identity.csproj";
var envPackageVersion   = EnvironmentVariable("nugetVersion");
var argRelease          = Argument<string>("Release", null);

var packageSuffix       = "";
var packageVersion      = "";
var fullPackageVersion  = "";

Task("Build")
	.IsDependentOn("Clean")
	.IsDependentOn("Restore")
	.Does(() =>
{

	// Patch Version for CI builds
	if (!isLocalBuild || envPackageVersion != null)
	{
		    packageVersion  = envPackageVersion;
		var assemblyVersion = packageVersion + ".0";

		if (AppVeyor.Environment.Repository.Branch.ToLower() != "release" && argRelease == null)
		{
			packageSuffix      = "rc" + AppVeyor.Environment.Build.Number.ToString();
			fullPackageVersion = packageVersion + "-" + packageSuffix;
		}

		Console.WriteLine("Package  Version: {0}", packageVersion);
		Console.WriteLine("Package  Suffix : {0}", packageSuffix);
		Console.WriteLine("Assembly Version: {0}", assemblyVersion);


		TransformConfig(nugetProject, nugetProject,
		new TransformationCollection {
			{ "Project/PropertyGroup/Version",         fullPackageVersion },
			{ "Project/PropertyGroup/VersionPrefix",   packageVersion },
			{ "Project/PropertyGroup/VersionSuffix",   packageSuffix },
			{ "Project/PropertyGroup/AssemblyVersion", assemblyVersion },
			{ "Project/PropertyGroup/FileVersion",     assemblyVersion },
		 });

	}

	var settings = new DotNetCoreBuildSettings 
	{
		Configuration = configuration
		// Runtime = IsRunningOnWindows() ? null : "unix-x64"
	};

	DotNetCoreBuild(solutionName, settings); 
});

Task("RunTests")
	.IsDependentOn("Restore")
	.IsDependentOn("Clean")
	.Does(() =>
{
	var projects = GetFiles("./test/**/*.csproj");

	foreach(var project in projects)
	{
		var settings = new DotNetCoreTestSettings
		{
			Configuration = configuration,
			NoBuild = true
		};

		Console.WriteLine(project.FullPath);

		DotNetCoreTest(project.FullPath, settings);
	}
});

Task("Pack")
	.IsDependentOn("Restore")
	.IsDependentOn("Clean")
	.Does(() =>
{
	var settings = new DotNetCorePackSettings
	{
		Configuration = configuration,
		OutputDirectory = buildArtifacts,
		NoBuild = true,
		VersionSuffix = packageSuffix
	};

/*	
	if (!string.IsNullOrEmpty(packageVersion))
		settings.ArgumentCustomization = b => 
		{
			Console.WriteLine("Package  Version: {0}", packageVersion);

			b.Append(" /p:VersionSuffix=" + "rc10");
			return b;
		};
*/

	DotNetCorePack(packPath, settings);
});

Task("Clean")
	.Does(() =>
{
	CleanDirectories(new DirectoryPath[] { buildArtifacts });
});

Task("Restore")
	.Does(() =>
{
	var settings = new DotNetCoreRestoreSettings
	{
		//Sources = new [] { "https://api.nuget.org/v3/index.json" }
	};

	DotNetCoreRestore(solutionName, settings);
});

Task("Default")
  .IsDependentOn("Build")
  .IsDependentOn("RunTests")
  .IsDependentOn("Pack");

RunTarget(target);